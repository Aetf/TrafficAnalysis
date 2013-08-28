using System;
using System.Collections.Generic;
using System.IO;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using TrafficAnalysis.PacketsAnalyze;
using TrafficAnalysis.PacketsAnalyze.TCP;
using System.ComponentModel;
using TrafficAnalysis.PacketsAnalyze.HTTP;

namespace TrafficAnalysis.DeviceDataSource
{
    class FileAnalyze : IFileStatisticSource
    {
        public FileAnalyze()
        {
            Earliest = DateTime.MaxValue;
            Latest = DateTime.MinValue;
        }

        private OfflinePacketDevice dev;

        private Int64 loadedbytes;

        private Int64 totalbytes;

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

        // Long time operation
        public void Load(string filepath)
        {
            // file validation
            if (!Path.GetExtension(filepath).Equals(".pcap", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The file is nonexist or not valid");
            }

            Reset();

            dev = new OfflinePacketDevice(filepath);

            FileInfo info = new FileInfo(filepath);
            totalbytes = info.Length;

            ReportProgress(Prog_ReadFileStart, "读取文件...");


            // Read all packets from file until EOF
            using (PacketCommunicator communicator = dev.Open())
            {
                communicator.ReceivePackets(0, OnPacketArrival);
            }

            ReportProgress(Prog_SortPacketsStart, "对数据包排序...");
            plist.Sort();

            ReportProgress(Prog_AnalyzePacketsStart, "分析中...");
            Analyze();
            fileLoaded = true;

            ReportProgress(100, "完成");
        }

        public void Reset()
        {
            plist.Clear();
            presum.Clear();
            bpsList.Clear();
            ppsList.Clear();
            dev = null;
            loadedbytes = 0;
            totalbytes = 0;
            lastreport = -1;
            fileLoaded = false;
            Earliest = DateTime.MaxValue;
            Latest = DateTime.MinValue;
        }

        // Long time operation
        public void TcpStreamReassemble(string saveDir)
        {
            if (!fileLoaded)
            {
                throw new InvalidOperationException("No file has been loaded");
            }

            using (TcpReassembly tcpre = new TcpReassembly())
            {
                // Save complete connections to files
                ConnectionToFile ctf = new ConnectionToFile(saveDir);
                tcpre.ConnectionFinished += (o, e) => ctf.Save(e.Connection);

                // Read all packets from file until EOF
                using (PacketCommunicator communicator = dev.Open())
                {
                    communicator.SetFilter("tcp");
                    communicator.ReceivePackets(0, p =>
                    {
                        tcpre.AddPacket(p.Ethernet.IpV4);
                    });
                }
            }

            // Open folder
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = saveDir,
                Verb = "open"
            });
        }

        // Long time operation
        public void HttpReconstruct(string saveDir)
        {
            if (!fileLoaded)
            {
                throw new InvalidOperationException("No file has been loaded");
            }

            using (TcpReassembly tcpre = new TcpReassembly())
            {
                // Reconstruct http files
                HttpReconstructor httpRecon = new HttpReconstructor();
                tcpre.ConnectionFinished += (o, e) => httpRecon.OnConnectionFinished(e.Connection);

                // Read all packets from file until EOF
                using (PacketCommunicator communicator = dev.Open())
                {
                    communicator.SetFilter("tcp");
                    communicator.ReceivePackets(0, p =>
                    {
                        tcpre.AddPacket(p.Ethernet.IpV4);
                    });
                }

                // Save result to files
                HttpToFiles htf = new HttpToFiles(saveDir);
                foreach (var rpy in httpRecon.ResponseList)
                {
                    htf.OutputContent(rpy);
                }
            }

            // Open folder
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = saveDir,
                Verb = "open"
            });
        }

        public RangeStatisticsInfo Query(TimeSpan start, TimeSpan end)
        {
            return Query(Earliest + start, Earliest + end);
        }

        public RangeStatisticsInfo Query(DateTime start, DateTime end)
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

            RangeStatisticsInfo info = new RangeStatisticsInfo
            {
                Duration = plist[e].Timestamp - plist[b].Timestamp,
                TotalLen = rm.TotalLen,
                TotalCnt = e - b + 1,
                CInfo = rm.Cinfo
            };

            return info;
        }
        #endregion

        private void OnPacketArrival(Packet packet)
        {
            plist.Add(new MetaPacket(packet));

            loadedbytes += packet.Length;
            ReportProgress((int) (Prog_ReadFileStart + (double) loadedbytes / totalbytes * Prog_ReadFileLen),
                            "读取文件...");

            if (packet.Timestamp < Earliest)
                Earliest = packet.Timestamp;

            if (packet.Timestamp > Latest)
                Latest = packet.Timestamp;
        }

        #region Statistics Analyze

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
                Cinfo = SingleAnalyzer.SortPacket(pk);
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

            // Calculate progress info.
            double tot = Prog_AnalyzePacketsLen;
            double each = tot / plist.Count;

            presum.Add(new RangeMeta(plist[0]));
            bpsList.Add(new KeyValuePair<TimeSpan, double>(TimeSpan.Zero, 0));
            ppsList.Add(new KeyValuePair<TimeSpan, double>(TimeSpan.Zero, 0));
            ReportProgress((int) (tot + each), "分析中...");

            int delta = CalcDelta(plist.Count);
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
                ReportProgress((int) (tot + (i+1)*each), "分析中...");
            }
        }

        private int CalcDelta(int count)
        {
            if (count > 300)
            {
                return 15;
            }
            else if (count > 100)
            {
                return 10;
            }
            else if (count > 50)
            {
                return 5;
            }
            else if(count > 25)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        #endregion

        #region ProgressChanged Implement
        public event ProgressChangedEventHandler ProgressChanged;

        int lastreport = -1;
        private void ReportProgress(int progress, object userstate)
        {
            if (ProgressChanged != null && lastreport != progress)
            {
                ProgressChanged(this, new ProgressChangedEventArgs(progress, userstate));
                lastreport = progress;
            }
        }

        static readonly int Prog_ReadFileStart = 0;
        static readonly int Prog_ReadFileLen = 40;
        static readonly int Prog_SortPacketsStart = Prog_ReadFileStart + Prog_ReadFileLen;
        static readonly int Prog_SortPacketsLen = 10;
        static readonly int Prog_AnalyzePacketsStart = Prog_SortPacketsStart + Prog_SortPacketsLen;
        static readonly int Prog_AnalyzePacketsLen = 50;
        #endregion
    }


}
