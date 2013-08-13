using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using PcapDotNet.Core;
using PcapDotNet.Packets;

namespace TrafficAnalysis.DeviceDataSource
{
    class MonitorPcap : IStatisticsSource
    {
        #region public Dictionary<string, StatisticsInfo> Statistics
        private Dictionary<string, StatisticsInfo> _Statistics = new Dictionary<string, StatisticsInfo>();
        public Dictionary<string, StatisticsInfo> Statistics
        {
            get
            {
                return _Statistics;
            }
        }
        #endregion

        #region public ReadOnlyCollection<DeviceDes> DeviceList
        private ReadOnlyCollection<DeviceDes> _DeviceList;
        public ReadOnlyCollection<DeviceDes> DeviceList
        {
            get
            {
                return _DeviceList;
            }
        }
        #endregion

        #region Public Methods

        public MonitorPcap()
        {
            InitCapture();
        }

        public void StartCapture(DeviceDes des, string dumpPath = null)
        {
            StartCapture(LivePacketDevice.AllLocalMachine.FirstOrDefault(dev => dev.Name.Equals(des.Name)), dumpPath);
        }

        public void StopCapture(DeviceDes des)
        {
            StopCapture(LivePacketDevice.AllLocalMachine.FirstOrDefault(dev => dev.Name.Equals(des.Name)));
        }

        #endregion

        #region Private and Protected Methods

        protected void InitCapture()
        {
            List<DeviceDes> list = new List<DeviceDes>();

            foreach (SharpPcap.WinPcap.WinPcapDevice dev in SharpPcap.WinPcap.WinPcapDeviceList.Instance)
            {
                DeviceDes des = new DeviceDes
                {
                    Name = dev.Name,
                    FriendlyName = dev.Interface.FriendlyName,
                    Description = dev.Description
                };

                list.Add(des);
                InitDevice(des);
            }
            _DeviceList = new ReadOnlyCollection<DeviceDes>(list);
        }

        private void InitDevice(DeviceDes des)
        {
            // Create a statistics info object for the device.
            Statistics[des.Name] = new StatisticsInfo(0, 0);

            // Create extra info object for the device.
            ExtraInfos[des.Name] = new DeviceControlBlock();
        }

        protected void StartCapture(LivePacketDevice dev, string dumppath = null)
        {
            if (dev == null)
                return;

            ExtraInfos[dev.Name].BackgroundThreadStop = false;
            ExtraInfos[dev.Name].BackgroundThread = new Thread(BackgroundThread);
            ExtraInfos[dev.Name].BackgroundThread.Start(dev);
            ExtraInfos[dev.Name].BackgroundThread.IsBackground = true;
            ExtraInfos[dev.Name].CaptureCancellation = new CancellationTokenSource();

            if (dumppath == null)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(state =>
                {
                    CancellationToken token = (CancellationToken)state;
                    // Open device
                    using (PacketCommunicator communicator = dev.Open(
                        65535, PacketDeviceOpenAttributes.Promiscuous,
                        250
                        ))
                    {
                        while (!token.IsCancellationRequested)
                        {
                            communicator.ReceivePackets(200, packet =>
                            {
                                OnPacketArrivaled(packet, dev);
                            });
                        }
                    }
                }), ExtraInfos[dev.Name].CaptureCancellation.Token);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(state =>
                {
                    CancellationToken token = (CancellationToken)state;
                    // Open device
                    using (PacketCommunicator communicator = dev.Open(
                        65535, PacketDeviceOpenAttributes.Promiscuous,
                        250
                        ))
                    {
                        using (PacketDumpFile dumpFile = communicator.OpenDump(dumppath))
                        {
                            while (!token.IsCancellationRequested)
                            {
                                communicator.ReceivePackets(200, packet =>
                                {
                                    dumpFile.Dump(packet);
                                    OnPacketArrivaled(packet, dev);
                                });
                            }
                        }
                    }
                }), ExtraInfos[dev.Name].CaptureCancellation.Token);
            }
            
        }

        protected void StopCapture(LivePacketDevice dev)
        {
            if (dev == null)
                return;

            if (ExtraInfos[dev.Name].BackgroundThread != null)
            {
                ExtraInfos[dev.Name].BackgroundThreadStop = true;
                ExtraInfos[dev.Name].BackgroundThread.Join();
                ExtraInfos[dev.Name].BackgroundThread = null;
            }

            if (ExtraInfos[dev.Name].CaptureCancellation != null)
            {
                ExtraInfos[dev.Name].CaptureCancellation.Cancel();
                ExtraInfos[dev.Name].CaptureCancellation = null;
            }
        }

        protected void OnPacketArrivaled(Packet packet, LivePacketDevice dev)
        {
            lock (ExtraInfos[dev.Name].QueueLock)
            {
                ExtraInfos[dev.Name].PacketQueue.Add(packet);
            }
        }

        protected void BackgroundThread(Object o)
        {
            LivePacketDevice dev = o as LivePacketDevice;
            DeviceControlBlock info = ExtraInfos[dev.Name];

            while (!info.BackgroundThreadStop)
            {
                bool shouldSleep = true;
                lock (info.QueueLock)
                {
                    if (info.PacketQueue.Count != 0)
                    {
                        shouldSleep = false;
                    }
                }

                if (shouldSleep)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else // should process the queue
                {
                    List<Packet> ourQueue;
                    lock (info.QueueLock)
                    {
                        // swap queues, giving the capture callback a new one
                        ourQueue = info.PacketQueue;
                        info.PacketQueue = new List<Packet>();
                    }

                    StatisticsInfo ourStat = new StatisticsInfo(0, 0);
                    long totbit = 0;
                    DateTime earlist = DateTime.MaxValue;
                    DateTime latest = DateTime.MinValue;
                    foreach (var pk in ourQueue)
                    {
                        // Timestamp and length.
                        if (pk.Timestamp < earlist)
                        {
                            earlist = pk.Timestamp;
                        }
                        if (pk.Timestamp > latest)
                        {
                            latest = pk.Timestamp;
                        }

                        totbit += pk.Length * 8;

                        PacketAnalyze.SortPacket(ourStat, pk);
                    }

                    double delay = (latest - earlist).TotalMilliseconds;
                    ourStat.Bps = (ulong)(totbit * 1000 / delay);
                    ourStat.Pps = (ulong)(ourQueue.Count * 1000 / delay);

                    _Statistics[dev.Name] = ourStat;
                }
            }
        }

        #endregion

        #region Private Field

        /// <summary>
        /// Extra information used when capturing
        /// </summary>
        private Dictionary<string, DeviceControlBlock> ExtraInfos = new Dictionary<string, DeviceControlBlock>();

        class DeviceControlBlock
        {
            /// <summary>
            /// When true the background thread will terminate
            /// </summary>
            public bool BackgroundThreadStop = false;

            public Thread BackgroundThread = null;

            public CancellationTokenSource CaptureCancellation = null;

            /// <summary>
            /// Object that is used to prevent two threads from accessing
            /// PacketQueue at the same time
            /// </summary>
            public object QueueLock = new object();

            /// <summary>
            /// Queues that the callback thread puts packets in. Accessed by
            /// the background thread when QueueLock is held
            /// </summary>
            public List<Packet> PacketQueue = new List<Packet>();

            /// <summary>
            /// Timestamp for the last received sample.
            /// </summary>
            public DateTime LastTimestamp = DateTime.MinValue;
        }

        #endregion
    }
}
