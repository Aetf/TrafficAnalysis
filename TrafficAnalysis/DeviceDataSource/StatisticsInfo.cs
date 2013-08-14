using System.Collections.Generic;
using TrafficAnalysis.Util;
using System.Text;

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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("BPS: ").Append(Bps.ToString()).Append('\n');
            sb.Append("PPS: ").Append(Pps.ToString()).Append('\n');
            sb.Append("NetworkLayer:").Append('\n');
            foreach (var key in NetworkLayer.Keys)
            {
                sb.Append('\t').Append(key).Append("=").Append(NetworkLayer[key]).Append('\n');
            }
            sb.Append("TransportLayer:").Append('\n');
            foreach (var key in TransportLayer.Keys)
            {
                sb.Append('\t').Append(key).Append("=").Append(TransportLayer[key]).Append('\n');
            }
            sb.Append("ApplicationLayer:").Append('\n');
            foreach (var key in ApplicationLayer.Keys)
            {
                sb.Append('\t').Append(key).Append("=").Append(ApplicationLayer[key]).Append('\n');
            }
            return sb.ToString();
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

            res.NetworkLayer = lhs.NetworkLayer.Merge(rhs.NetworkLayer);
            res.TransportLayer = lhs.TransportLayer.Merge(rhs.TransportLayer);
            res.ApplicationLayer = lhs.ApplicationLayer.Merge(rhs.ApplicationLayer);

            return res;
        }

        public static CategorizeInfo operator -(CategorizeInfo lhs, CategorizeInfo rhs)
        {
            CategorizeInfo res = new CategorizeInfo();

            res.ApplicationLayer = lhs.ApplicationLayer.Difference(rhs.ApplicationLayer);
            res.TransportLayer = lhs.TransportLayer.Difference(rhs.TransportLayer);
            res.NetworkLayer = lhs.NetworkLayer.Difference(rhs.NetworkLayer);

            return res;
        }
    }
}
