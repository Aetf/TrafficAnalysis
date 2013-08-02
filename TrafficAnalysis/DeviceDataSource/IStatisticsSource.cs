using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.DeviceDataSource
{
    /// <summary>
    /// Represent statistics information for an interface.
    /// </summary>
    public struct StatisticsInfo
    {
        /// <summary>
        /// Bit per second
        /// </summary>
        public double Bps;

        /// <summary>
        /// Packet per second
        /// </summary>
        public double Pps;

        /// <summary>
        /// Packer per second classified by network layer
        /// </summary>
        public Dictionary<string, long> NetworkLayer;

        /// <summary>
        /// Packet per second classified by transport layer packet type
        /// </summary>
        public Dictionary<string, long> TransportLayer;

        /// <summary>
        /// Packet per second classified by application layer packet type
        /// </summary>
        public Dictionary<string, long> ApplicationLayer;

        public StatisticsInfo(ulong bps, ulong pps)
        {
            Bps = bps;
            Pps = pps;
            NetworkLayer = new Dictionary<string, long>();
            TransportLayer = new Dictionary<string, long>();
            ApplicationLayer = new Dictionary<string, long>();
        }
    }

    /// <summary>
    /// Interface for an abstract warpper around WinPcap
    /// </summary>
    interface IStatisticsSource
    {
        Dictionary<string, StatisticsInfo> Statistics { get; }
        ReadOnlyCollection<DeviceDes> DeviceList { get; }

        void StartCapture(DeviceDes des);
        void StopCapture(DeviceDes des);
    }
}
