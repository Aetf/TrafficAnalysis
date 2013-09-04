using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using TrafficAnalysis.PacketsAnalyze;
using TrafficAnalysis.PacketsAnalyze.HTTP;
using TrafficAnalysis.PacketsAnalyze.HTTP.Constrains;
using TrafficAnalysis.PacketsAnalyze.TCP;

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

            ReportProgress(ProgressSource.Load, Prog_ReadFileStart, "读取文件...");


            // Read all packets from file until EOF
            using (PacketCommunicator communicator = dev.Open())
            {
                communicator.ReceivePackets(0, OnPacketArrival);
            }

            ReportProgress(ProgressSource.Load, Prog_SortPacketsStart, "对数据包排序...");
            plist.Sort();

            ReportProgress(ProgressSource.Load, Prog_AnalyzePacketsStart, "分析中...");
            Analyze();
            fileLoaded = true;

            ReportProgress(ProgressSource.Load, 100, "完成");
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


            ReportProgress(ProgressSource.TCPReassemble, 0, "分析中...");
            using (TcpReassemble tcpre = new TcpReassemble())
            {
                // Save complete connections to files
                ConnectionToFile ctf = new ConnectionToFile(saveDir);
                tcpre.ConnectionFinished += (o, e) => ctf.Save(e.Connection);

                int cnt = 0;
                // Read all packets from file until EOF
                using (PacketCommunicator communicator = dev.Open())
                {
                    communicator.SetFilter("tcp");
                    communicator.ReceivePackets(0, p =>
                    {
                        tcpre.AddPacket(p.Ethernet.IpV4);
                        ReportProgress(ProgressSource.TCPReassemble,
                                       (int) ((double)cnt / plist.Count) ,
                                       string.Format("分析中...{0}/{1}", ++cnt, plist.Count));
                    });
                }
            }

            ReportProgress(ProgressSource.TCPReassemble, 100, "完成...打开文件夹...");

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
            HttpRecon(saveDir, null);
        }

        // Long time operation
        public void KeywordHttpReconstruct(string saveDir, IList<string> keywords)
        {
            Collection<ExtractConstrain> col = new Collection<ExtractConstrain>();
            foreach (var key in keywords)
            {
                col.Add(new HttpKeywordConstrain()
                {
                    Keyword = key
                });
            }
            HttpRecon(saveDir, col);
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

        #region Inner Implement
        private void HttpRecon(string saveDir, Collection<ExtractConstrain> coll)
        {
            if (!fileLoaded)
            {
                throw new InvalidOperationException("No file has been loaded");
            }

            ReportProgress(ProgressSource.HttpReconstruct, 0, "分析中...");
            using (TcpReassemble tcpre = new TcpReassemble())
            {
                // Reconstruct http files
                HttpReconstructor httpRecon = new HttpReconstructor();
                // Save result to files
                HttpConstrainExtract htf = new HttpConstrainExtract(saveDir);
                if (coll != null)
                {
                    foreach (var cons in coll)
                    {
                        htf.ConstrainCollection.Add(cons);
                    }
                }
                
                tcpre.ConnectionFinished += (o, e) =>
                {
                    httpRecon.OnConnectionFinished(e.Connection);
                    foreach (var rpy in httpRecon.ResponseList)
                    {
                        htf.OutputContent(rpy);
                    }
                };

                int cnt = 0;
                // Read all packets from file until EOF
                using (PacketCommunicator communicator = dev.Open())
                {
                    communicator.SetFilter("tcp");
                    communicator.ReceivePackets(0, p =>
                    {
                        tcpre.AddPacket(p.Ethernet.IpV4);
                        ReportProgress(ProgressSource.HttpReconstruct,
                                       (int)((double)cnt / plist.Count),
                                       string.Format("分析中...{0}/{1}", ++cnt, plist.Count));
                    });
                }

                ReportProgress(ProgressSource.HttpReconstruct, 100, "完成...打开文件夹...");
            }

            // Open folder
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = saveDir,
                Verb = "open"
            });
        }

        private void OnPacketArrival(Packet packet)
        {
            plist.Add(new MetaPacket(packet));

            loadedbytes += packet.Length;
            ReportProgress(ProgressSource.Load,
                            (int) (Prog_ReadFileStart + (double) loadedbytes / totalbytes * Prog_ReadFileLen),
                            "读取文件...");

            if (packet.Timestamp < Earliest)
                Earliest = packet.Timestamp;

            if (packet.Timestamp > Latest)
                Latest = packet.Timestamp;
        }
        #endregion

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
            ReportProgress(ProgressSource.Load, (int)(tot + each), "分析中...");

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
                ReportProgress(ProgressSource.Load, (int)(tot + (i + 1) * each), "分析中...");
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
        internal void ReportProgress(ProgressSource source, int progress, object userstate)
        {
            if (ProgressChanged != null && lastreport != progress)
            {
                ProgressChanged(this,
                                new ProgressChangedEventArgs(progress,
                                                new Tuple<ProgressSource, object>(source, userstate)));
                lastreport = progress;
            }
        }

        static readonly int Prog_ReadFileStart = 0;
        static readonly int Prog_ReadFileLen = 40;
        static readonly int Prog_SortPacketsStart = Prog_ReadFileStart + Prog_ReadFileLen;
        static readonly int Prog_SortPacketsLen = 10;
        static readonly int Prog_AnalyzePacketsStart = Prog_SortPacketsStart + Prog_SortPacketsLen;
        static readonly int Prog_AnalyzePacketsLen = 50;

        public enum ProgressSource
        {
            Load, TCPReassemble, HttpReconstruct
        }
        #endregion
    }


}
