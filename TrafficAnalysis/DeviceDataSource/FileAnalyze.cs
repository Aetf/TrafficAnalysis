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
        public void Load(string filepath)
        {
            OfflinePacketDevice dev = new OfflinePacketDevice(filepath);

            // Read all packets from file until EOF
            using (PacketCommunicator communicator = dev.Open())
            {
                communicator.ReceivePackets(0, OnPacketArrival);
            }

            plist.Sort();
            Analyze();
        }

        public StatisticsInfo Query(DateTime start, DateTime end)
        {
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
            public long Length;
            public DateTime Timestamp;
            public CategorizeInfo Cinfo;

            public MetaPacket(Packet pk)
            {
                Length = pk.Length;
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

        #region public List<MetaPacket> Packets
        public List<MetaPacket> Packets
        {
            get
            {
                return plist;
            }
        }
        List<MetaPacket> plist = new List<MetaPacket>();
        #endregion


        List<RangeMeta> presum = new List<RangeMeta>();

        private void Analyze()
        {
            if (plist.Count < 1)
                return;

            presum.Add(new RangeMeta(plist[0]));
            for (int i = 1; i != plist.Count; i++)
            {
                presum.Add(presum[i - 1] + plist[i]);
            }
        }

        #endregion
    }


}
