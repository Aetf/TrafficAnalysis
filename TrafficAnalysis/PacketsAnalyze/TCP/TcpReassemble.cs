using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    public class TcpReassemble : IDisposable
    {
        #region Connection Pool
        private Dictionary<TcpPair, TcpConnection> connPool = new Dictionary<TcpPair, TcpConnection>();

        private TcpConnection GetConnection(TcpPair pair)
        {
            TcpConnection conn = null;
            if (!connPool.ContainsKey(pair))
            {
                connPool[pair] = new TcpConnection(pair);
            }
            conn = connPool[pair];

            return conn;
        }

        private void CloseConnection(TcpConnection conn)
        {
            connPool.Remove(conn.Pair);

            RaiseConnectionFinished(conn);

            for (int i = 0; i != 2; i++)
                conn.Stream(i).Data.Dispose();
        }
        #endregion

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="saveDir">The directory to save stream files</param>
        public TcpReassemble()
        {
            TcpConnection.SetNextID(0);
        }

        /// <summary>
        /// Add a packet to the Reassembly
        /// </summary>
        /// <param name="packet"></param>
        public void AddPacket(IpV4Datagram packet)
        {
            if (packet.Protocol != IpV4Protocol.Tcp)
                return;

            TcpDatagram tcpp = packet.Transport as TcpDatagram;
            int tcpLength = packet.TotalLength
                            - packet.HeaderLength;

            UInt32 origDataLength = (UInt32)(tcpLength - tcpp.HeaderLength);
            // packet.PayloadData.Length < origDataLength means not fully captured.

            IPAddress sip = IPAddress.Parse(packet.Source.ToString());
            IPAddress dip = IPAddress.Parse(packet.Destination.ToString());
            TcpPair pair = new TcpPair(new IPEndPoint(sip, tcpp.SourcePort),
                                       new IPEndPoint(dip, tcpp.DestinationPort));
            
            tcp_reassemble(tcpp.SequenceNumber, tcpp.AcknowledgmentNumber,
                            origDataLength, tcpp.Payload.ToArray(),
                            tcpp.ControlBits, pair);
        }

        /// <summary>
        /// Call when a flux file is ended, cause all unclosed connections to be closed
        /// </summary>
        public void Finish()
        {
            TcpConnection[] conns = connPool.Values.ToArray();
            foreach (var conn in conns)
            {
                CloseConnection(conn);
            }
        }

        /// <summary>
        /// Reassembly tcp stream
        /// </summary>
        /// <param name="sequence">Sequence number of the tcp packet</param>
        /// <param name="acknowledgement">Acknowledgement number of the tcp packet</param>
        /// <param name="length">Length of the original payload data</param>
        /// <param name="data">Actual captured data</param>
        /// <param name="flags">All control bits of the tcp packet</param>
        /// <param name="pair">Tcp end point pair, we use its AEP as source and BEP as destination</param>
        private void tcp_reassemble(UInt32 sequence, UInt32 acknowledgement, UInt32 length, Byte[] data,
                                TcpControlBits flags, TcpPair pair)
        {
            // First to get a TcpConnection object, which can either be seen before
            // or a newly created one.
            TcpConnection conn = GetConnection(pair);
            
            // Check our direction.
            int dir = conn.Pair.AEP.Equals(pair.AEP) ? 0 : 1;

            // Cache some useful object, this is packet receiver. (Only reference here.)
            TCB tcb = conn.ControlBlock(dir);
            TcpStream stream = conn.Stream(dir);

            // Whether we have trunced the packet
            if (data.Length < length)
            {
                stream.IsTrunced = true;
            }

            // Before adding data for this flow to the data_out_file, check whether
            // this frame acks fragments that were already seen. This happens when
            // frames are not in the capture file, but were actually seen by the 
            // receiving host.
            if (conn.Stream(1 - dir).FragListHead != null)
            {
                while (checkFragment(conn, 1 - dir, acknowledgement)) ;
            }

            // now lets get the sequence number stuff figured out
            if (tcb.State == TcpState.CLOSED)
            {
                // Transmit FSM state.
                tcb.Transit(flags, true);
                conn.ControlBlock(1 - dir).Transit(flags, false); // the other side.

                // This is the first time we see sequence number (in this direction)
                tcb.seq = sequence + length;
                if (flags.HasFlag(TcpControlBits.Synchronize))
                {
                    tcb.seq++; // we hold next seq, and in case of syn, next seq is sequence+1
                }
                else
                {
                    // A new conversation should start with three-way handshake, thus syn should be set
                    // but if not, it may means that we are dealing with a connection 
                    // which was captured half way.
                    stream.IsTrunced = true;
                }

                conn.WritePacketData(dir, data);
                return;
            }

            // If we got here, it means we've seen packets in this connection and direction

            // transit fsm state.
            tcb.Transit(flags, true);
            conn.ControlBlock(1 - dir).Transit(flags, false);

            // Warning: I didn't care about sequence number wrap-around in
            // all comparisons of sequence numbers. I'm not sure if this will
            // cause problems.
            // check if this packet is in right place
            if (sequence < tcb.seq)
            {
                // the sequence number seems dated,
                // but check the end to make sure it has no more
                // info than we have seen.
                UInt32 endseq = sequence + length;
                if (endseq > tcb.seq)
                {
                    // here's more than we have seen.
                    // Get the payload that we have not seen.
                    UInt32 new_offset = tcb.seq - sequence;

                    if (data.Length <= new_offset)
                    {
                        // Unfortunately, we didn't captured that.
                        // so only useless data left.
                        data = null;
                        stream.IsTrunced = true;
                    }
                    else
                    {
                        Byte[] tmp = new Byte[data.Length - new_offset];
                        Array.Copy(data, new_offset, tmp, 0, tmp.Length);
                        data = tmp;
                    }

                    // Make things appear to be right on time.
                    sequence = tcb.seq;
                    length = endseq - sequence;
                }
            }

            if (sequence == tcb.seq)
            {
                // right on time
                tcb.seq += length;
                if (flags.HasFlag(TcpControlBits.Synchronize)) tcb.seq++;

                if (data != null)
                    conn.WritePacketData(dir, data);

                if (flags.HasFlag(TcpControlBits.Fin))
                    if (conn.CloseStream(dir))
                        CloseConnection(conn);

                // done with the packet, see if it cause a fragment to fit.
                // pass 0 to ack because we don't need to check ack here.
                // should been checked above, before add data.
                while (checkFragment(conn, dir, 0)) ;
            }
            else
            {
                // out of order packet
                if (data.Length > 0 && sequence > tcb.seq)
                {
                    TcpFrag frag = new TcpFrag()
                    {
                        seq = sequence,
                        len = length,
                        data = data,
                        flags = flags
                    };
                    
                    // Add to fragment list
                    if (stream.FragListHead == null)
                    {
                        frag.next = null;
                        stream.FragListHead = frag;
                    }
                    else
                    {
                        frag.next = stream.FragListHead;
                        stream.FragListHead = frag;
                    }
                }
            }
        }

        /// <summary>
        /// Search through all fragments we've collected to
        /// see if one fits.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="dir"></param>
        /// <param name="ack"></param>
        /// <returns>Return true if a fragment is found match the stream and added to the stream.</returns>
        private bool checkFragment(TcpConnection conn, int dir, UInt32 ack)
        {
            // Handy object cache
            TcpStream stream = conn.Stream(dir);
            TCB tcb = conn.ControlBlock(dir);

            UInt32 lowestseq = UInt32.MaxValue;
            for (TcpFrag cur = stream.FragListHead, prev = null;
                cur != null;
                prev = cur, cur = cur.next)
            {
                lowestseq = Math.Min(lowestseq, cur.seq);
                if (cur.seq < tcb.seq)
                {
                    // the sequence number seems dated,
                    // but check the end to make sure it has no more
                    // info than we have seen.
                    UInt32 endseq = cur.seq + cur.len;
                    if (endseq > tcb.seq)
                    {
                        // here's more than we have seen.
                        // Get the payload that we have not seen.
                        UInt32 new_offset = tcb.seq - cur.seq;

                        // Only when we have captured the data
                        if (cur.data.Length > new_offset)
                        {
                            Byte[] tmp = new Byte[cur.data.Length - new_offset];
                            Array.Copy(cur.data, new_offset, tmp, 0, tmp.Length);
                            conn.WritePacketData(dir, tmp);
                        }

                        tcb.seq += (cur.len - new_offset);

                        if (cur.flags.HasFlag(TcpControlBits.Fin))
                            if (conn.CloseStream(dir))
                                CloseConnection(conn);
                    }


                    // Remove it from list.
                    if (prev != null)
                    {
                        prev.next = cur.next;
                    }
                    else // is list head
                    {
                        stream.FragListHead = cur.next;
                    }

                    cur.data = null;
                    cur = null;
                    return true;
                }
                if (cur.seq == tcb.seq)
                {
                    // Got one match the stream
                    tcb.seq += cur.len;
                    if (cur.data != null)
                    {
                        conn.WritePacketData(dir, cur.data);
                    }
                    if (cur.flags.HasFlag(TcpControlBits.Fin))
                        if (conn.CloseStream(dir))
                            CloseConnection(conn);

                    // Remove it from list.
                    if (prev != null)
                    {
                        prev.next = cur.next;
                    }
                    else // is list head
                    {
                        stream.FragListHead = cur.next;
                    }

                    cur.data = null;
                    cur = null;
                    return true;
                }
            }

            if (ack > lowestseq)
            {
                // There are frames missing in the capture file that were
                // seen by the receiving host.
                // Add a dummy string here.
                string dummy = String.Format("[%d bytes missing in capture file]", lowestseq - tcb.seq);
                conn.WritePacketData(dir, Encoding.Default.GetBytes(dummy));
                tcb.seq = lowestseq;
                return true;
            }

            return false;
        }


        #region Events

        #region ConnectionFinished
        /// <summary>
        /// Raised when a complete tcp connection is detected.
        /// </summary>
        public event ConnectionFinishedEventHandler ConnectionFinished;
        public class ConnectionFinishedEventArgs : EventArgs
        {
            public TcpConnection Connection { get; private set; }
            public ConnectionFinishedEventArgs(TcpConnection connection)
            {
                Connection = connection;
            }
        }
        public delegate void ConnectionFinishedEventHandler(object sendor, ConnectionFinishedEventArgs e);
        private void RaiseConnectionFinished(TcpConnection conn)
        {
            if (ConnectionFinished != null)
            {
                ConnectionFinished(this, new ConnectionFinishedEventArgs(conn));
            }
        }
        #endregion

        #endregion

        public void Dispose()
        {
            Finish();
        }
    }
}
