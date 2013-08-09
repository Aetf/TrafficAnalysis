using System.Collections.Generic;
using TrafficAnalysis.Util;

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
        public Dictionary<string, long> NetworkLayer
        {
            get
            {
                return cinfo.NetworkLayer;
            }
        }

        /// <summary>
        /// Packet per second classified by transport layer packet type
        /// </summary>
        public Dictionary<string, long> TransportLayer
        {
            get
            {
                return cinfo.TransportLayer;
            }
        }

        /// <summary>
        /// Packet per second classified by application layer packet type
        /// </summary>
        public Dictionary<string, long> ApplicationLayer
        {
            get
            {
                return cinfo.ApplicationLayer;
            }
        }

        #region public CategorizeInfo CInfo
        private CategorizeInfo cinfo;
        public CategorizeInfo CInfo
        {
            get { return cinfo; }
            set { cinfo = value; }
        }
        #endregion

        public StatisticsInfo(ulong bps, ulong pps)
        {
            Bps = bps;
            Pps = pps;
            cinfo = new CategorizeInfo
            {
                NetworkLayer = new Dictionary<string, long>(),
                TransportLayer = new Dictionary<string,long>(),
                ApplicationLayer = new Dictionary<string, long>()
            };
        }
    }

    /// <summary>
    /// Represent categorize information for an interface or a packet.
    /// </summary>
    public struct CategorizeInfo
    {
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

        public static CategorizeInfo operator +(CategorizeInfo lhs, CategorizeInfo rhs)
        {
            CategorizeInfo res = new CategorizeInfo();

            res.NetworkLayer = Auxiliary.Merge(lhs.NetworkLayer, rhs.NetworkLayer);
            res.TransportLayer = Auxiliary.Merge(lhs.TransportLayer, rhs.TransportLayer);
            res.ApplicationLayer = Auxiliary.Merge(lhs.ApplicationLayer, rhs.ApplicationLayer);

            return res;
        }

        public static CategorizeInfo operator -(CategorizeInfo lhs, CategorizeInfo rhs)
        {
            CategorizeInfo res = new CategorizeInfo();

            res.ApplicationLayer = Auxiliary.Difference(lhs.ApplicationLayer, rhs.ApplicationLayer);
            res.TransportLayer = Auxiliary.Difference(lhs.TransportLayer, rhs.TransportLayer);
            res.NetworkLayer = Auxiliary.Difference(lhs.NetworkLayer, rhs.NetworkLayer);

            return res;
        }
    }
}
