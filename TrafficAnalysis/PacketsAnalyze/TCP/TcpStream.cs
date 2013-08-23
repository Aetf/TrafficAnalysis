using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PcapDotNet.Packets.Transport;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    /// <summary>
    /// A TcpStream represents the data stream of a single direction
    /// during a tcp connection.
    /// </summary>
    public class TcpStream
    {
        /// <summary>
        /// Tcp fragments that haven't added to stream.
        /// Since we always traverse the whole list and ocassionnally
        /// remove a node, we need a data structure with O(1) when remove.
        /// So we choose linked list here.
        /// </summary>
        internal TcpFrag FragListHead { get; set; }

        /// <summary>
        /// true if data belonging to this stream
        /// wasn't fully captured.
        /// This is caused either by partial captured packets
        /// or not witness a full tcp conversition.
        /// </summary>
        public bool IsTrunced { get; internal set; }

        /// <summary>
        /// true if we haven't seen a single packet belonging to this stream.
        /// </summary>
        public bool IsEmpty { get; internal set; }

        /// <summary>
        /// Indicate whether the stream has finished. i.e. received a FIN
        /// </summary>
        public bool IsFinished { get; internal set; }

        private MemoryStream dataStream = new MemoryStream(65535);
        public MemoryStream Data
        {
            get { return dataStream; }
        }

        /// <summary>
        /// Called when the stream is closed. i.e. received a FIN
        /// </summary>
        public void OnClose()
        {
            IsFinished = true;
        }

        /// <summary>
        /// Write a block of bytes to the stream using data read from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        public void Write(Byte[] buffer, int offset, int count)
        {
            dataStream.Write(buffer, offset, count);
        }

        public TcpStream()
        {
            FragListHead = null;
            IsTrunced = false;
            IsEmpty = true;
            IsFinished = false;
        }
    }

    /// <summary>
    /// Represents a list node of a partial tcp stream fragments linklist.
    /// </summary>
    internal class TcpFrag
    {
        /// <summary>
        /// Sequence number
        /// </summary>
        public UInt32 seq;

        /// <summary>
        /// Original data length
        /// </summary>
        public UInt32 len;

        /// <summary>
        /// Flags assosiated
        /// </summary>
        public TcpControlBits flags;

        /// <summary>
        /// Actual captured data array
        /// </summary>
        public Byte[] data;

        /// <summary>
        /// Next node in list
        /// </summary>
        public TcpFrag next;
    }
}
