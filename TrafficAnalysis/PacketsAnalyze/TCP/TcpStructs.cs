using System;
using System.Net;
using PacketDotNet;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    public enum TcpState
    {
        CLOSED, SYN_RECEIVED, SYN_RECEIVED2, SYN_SENT,
        SYN_ACK_SENT, SYN_ACK_RECEIVED, ESTABLISHED,
        FIN_WAIT1, FIN_WAIT2, FIN_WAIT3, CLOSING1,
        CLOSING2, FIN_RECEIVED, CLOSE_WAIT, LAST_ACK, ERROR
    }

    /// <summary>
    /// A wrapper class around tcp flags bits.
    /// </summary>
    public struct TCPFlags
    {
        private Byte flags;

        public TCPFlags(TcpPacket packet)
        {
            flags = packet.AllFlags;
        }

        public bool FIN { get { return (flags & TcpFields.TCP_FIN_MASK) != 0; } }
        public bool SYN { get { return (flags & TcpFields.TCP_SYN_MASK) != 0; } }
        public bool RST { get { return (flags & TcpFields.TCP_RST_MASK) != 0; } }
        public bool PSH { get { return (flags & TcpFields.TCP_PSH_MASK) != 0; } }
        public bool ACK { get { return (flags & TcpFields.TCP_ACK_MASK) != 0; } }
        public bool URG { get { return (flags & TcpFields.TCP_URG_MASK) != 0; } }
        public bool ECN { get { return (flags & TcpFields.TCP_ECN_MASK) != 0; } }
        public bool CWR { get { return (flags & TcpFields.TCP_CWR_MASK) != 0; } }
    }

    /// <summary>
    /// Endpoint identification
    /// </summary>
    public class TcpPair : IEquatable<TcpPair>
    {
        private IPEndPoint a;
        private IPEndPoint b;

        public IPEndPoint AEP { get { return a; } }
        public IPEndPoint BEP { get { return b; } }

        #region IPEndPoint Wrapper
        public IPAddress AIP
        {
            get { return a.Address; }
        }

        public IPAddress BIP
        {
            get { return b.Address; }
        }

        public Int32 APort
        {
            get { return a.Port; }
        }

        public Int32 BPort
        {
            get { return b.Port; }
        }
        #endregion

        public TcpPair(IPEndPoint epa, IPEndPoint epb)
        {
            a = epa;
            b = epb;
        }

        public TcpPair(TcpPacket packet)
        {
            IPv4Packet parent = packet.ParentPacket as IPv4Packet;
            a = new IPEndPoint(parent.SourceAddress, packet.SourcePort);
            b = new IPEndPoint(parent.DestinationAddress, packet.DestinationPort);
        }

        public bool Equals(IPEndPoint epa, IPEndPoint epb)
        {
            return (a.Equals(epa) && b.Equals(epb))
                    || (a.Equals(epb) && b.Equals(epa));
        }

        public bool Equals(TcpPair other)
        {
            return (other.a.Equals(a) && other.b.Equals(b))
                || (other.a.Equals(b) && other.b.Equals(a));
        }

        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode();
        }
    }
}
