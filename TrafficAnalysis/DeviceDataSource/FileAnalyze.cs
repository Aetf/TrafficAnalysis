using System;
using System.Collections.Generic;
using PcapDotNet.Core;
using PcapDotNet.Packets;

namespace TrafficAnalysis.DeviceDataSource
{
    class FileAnalyze : IFileStatisticSource
    {
        public FileAnalyze()
        {
            Earliest = DateTime.MaxValue;
            Latest = DateTime.MinValue;
        }

        #region Members of IFileStatisticsSource

        #region public IList<KeyValuePair<TimeSpan, double>> BpsList
        private IList<KeyValuePair<TimeSpan, double>> bpsList = new List<KeyValuePair<TimeSpan, double>>();
        public IList<KeyValuePair<TimeSpan, double>> BpsList { get { return bpsList; } }
        #endregion

        #region public IList<KeyValuePair<TimeSpan, double>> PpsList
        private IList<KeyValuePair<TimeSpan, double>> ppsList = new List<KeyValuePair<TimeSpan, double>>();
        public IList<KeyValuePair<TimeSpan, double>> PpsList { get { return ppsList; } }
        #endregion

        #region public bool FileLoaded
        private bool fileLoaded = false;
        public bool FileLoaded { get { return fileLoaded; } }
        #endregion

        public void Load(string filepath)
        {
            // file validation
            if (false)
            {
                throw new ArgumentException("The file is nonexist or not valid");
            }

            Reset();

            OfflinePacketDevice dev = new OfflinePacketDevice(filepath);

            // Read all packets from file until EOF
            using (PacketCommunicator communicator = dev.Open())
            {
                communicator.ReceivePackets(0, OnPacketArrival);
            }

            plist.Sort();
            Analyze();
            fileLoaded = true;
        }

        public void Reset()
        {
            plist.Clear();
            presum.Clear();
            bpsList.Clear();
            ppsList.Clear();
            fileLoaded = false;
            Earliest = DateTime.MaxValue;
            Latest = DateTime.MinValue;
        }

        public StatisticsInfo Query(TimeSpan start, TimeSpan end)
        {
            return Query(Earliest + start, Earliest + end);
        }

        public StatisticsInfo Query(DateTime start, DateTime end)
        {
            if (!fileLoaded)
            {
                throw new InvalidOperationException("No file has been loaded");
            }
            // Check range
            if (start < Earliest || start > Latest || end < Earliest || end > Latest)
            {
                throw new ArgumentOutOfRangeException();
            }

            int b = plist.BinarySearch(new MetaPacket
            {
                Timestamp = start
            });
            if (b < 0)
            {
                b = ~b - 1; // assert: b >= 0
            }

            int e = plist.BinarySearch(new MetaPacket
            {
                Timestamp = end
            });
            if (e < 0)
            {
                e = (~e) - 1; // assert: e >= 0
            }

            RangeMeta rm = presum[e] - presum[b];

            double seconds = (plist[e].Timestamp - plist[b].Timestamp).TotalSeconds;

            StatisticsInfo info = new StatisticsInfo
            {
                Pps = (e - b) / seconds,
                Bps = rm.TotalLen / seconds,
                CInfo = rm.Cinfo
            };

            return info;
        }
        #endregion

        private void OnPacketArrival(Packet packet)
        {
            plist.Add(new MetaPacket(packet));

            if (packet.Timestamp < Earliest)
                Earliest = packet.Timestamp;

            if (packet.Timestamp > Latest)
                Latest = packet.Timestamp;
        }

        #region Analyze

        /// <summary>
        /// Only contains a packet's basic information
        /// </summary>
        public struct MetaPacket : IComparable<MetaPacket>, IEquatable<MetaPacket>
        {
            /// <summary>
            /// the number of bits in the packet
            /// </summary>
            public long Length;

            /// <summary>
            /// the time this packet was captured
            /// </summary>
            public DateTime Timestamp;

            /// <summary>
            /// this packet's categorized info
            /// </summary>
            public CategorizeInfo Cinfo;

            public MetaPacket(Packet pk)
            {
                Length = pk.Length * 8;
                Timestamp = pk.Timestamp;
                Cinfo = PacketAnalyze.SortPacket(pk);
            }

            public int CompareTo(MetaPacket other)
            {
                return Timestamp.CompareTo(other.Timestamp);
            }

            public bool Equals(MetaPacket other)
            {
                return Timestamp.Equals(other.Timestamp);
            }

            public static RangeMeta operator +(MetaPacket lhs, MetaPacket rhs)
            {
                RangeMeta res = new RangeMeta();
                res.TotalLen = lhs.Length + rhs.Length;
                res.Cinfo = lhs.Cinfo + rhs.Cinfo;

                return res;
            }
        }

        /// <summary>
        /// Represents basic information for some packets
        /// </summary>
        public struct RangeMeta
        {
            public long TotalLen;

            public CategorizeInfo Cinfo;

            #region Constructors
            public RangeMeta(RangeMeta other)
            {
                TotalLen = other.TotalLen;
                Cinfo = other.Cinfo;
            }

            public RangeMeta(MetaPacket pk)
            {
                TotalLen = pk.Length;
                Cinfo = pk.Cinfo;
            }
            #endregion

            #region Operators
            public static RangeMeta operator -(RangeMeta lhs, RangeMeta rhs)
            {
                RangeMeta res = new RangeMeta();
                res.TotalLen = lhs.TotalLen - rhs.TotalLen;
                res.Cinfo = lhs.Cinfo - rhs.Cinfo;
                return res;
            }

            public static RangeMeta operator +(RangeMeta lhs, MetaPacket rhs)
            {
                RangeMeta res = new RangeMeta();

                res.TotalLen = lhs.TotalLen + rhs.Length;
                res.Cinfo = lhs.Cinfo + rhs.Cinfo;

                return res;
            }
            #endregion
        }

        public DateTime Earliest { get; private set; }
        public DateTime Latest { get; private set; }

        List<MetaPacket> plist = new List<MetaPacket>();
        List<RangeMeta> presum = new List<RangeMeta>();

        private void Analyze()
        {
            if (plist.Count < 1)
                return;

            presum.Add(new RangeMeta(plist[0]));
            bpsList.Add(new KeyValuePair<TimeSpan, double>(TimeSpan.Zero, 0));
            ppsList.Add(new KeyValuePair<TimeSpan, double>(TimeSpan.Zero, 0));

            int delta = 15;
            for (int i = 1; i != plist.Count; i++)
            {
                // preprocess for range query
                presum.Add(presum[i - 1] + plist[i]);

                // instantaneous bps and pps calc
                if (i >= delta)
                {
                    TimeSpan dur = plist[i].Timestamp - plist[i - delta].Timestamp;
                    TimeSpan x = plist[i].Timestamp - Earliest;
                    double b = plist[i].Length / dur.TotalSeconds;
                    double p = delta / dur.TotalSeconds;
                    bpsList.Add(new KeyValuePair<TimeSpan, double>(x, b));
                    ppsList.Add(new KeyValuePair<TimeSpan, double>(x, p));
                }
                
            }
        }

        #endregion
    }


}
