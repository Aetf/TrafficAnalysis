using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PacketDotNet;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    public class TcpStream
    {
        /// <summary>
        /// Tcp fragments that haven't added to stream.
        /// Since we always traverse the whole list and ocassionnally
        /// remove a node, we need a data structure with O(1) when remove.
        /// So we choose linked list here.
        /// </summary>
        internal TcpFrag FragListHead { get; set; }

        private List<Byte> data;
        public Byte[] Data
        {
            get { return data.ToArray(); }
        }

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

        public TcpStream()
        {
            FragListHead = null;
            IsTrunced = false;
            IsEmpty = true;
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
        /// Actual captured data length
        /// </summary>
        public UInt32 data_len;

        /// <summary>
        /// Data array
        /// </summary>
        public Byte[] data;

        /// <summary>
        /// Next node in list
        /// </summary>
        public TcpFrag next;
    }
}
