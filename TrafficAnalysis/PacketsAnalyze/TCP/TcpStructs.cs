using System;
using System.Net;
using PcapDotNet.Packets.Transport;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    public enum TcpState
    {
        CLOSED, SYN_RECEIVED, SYN_RECEIVED2, SYN_SENT,
        SYN_ACK_SENT, SYN_ACK_RECEIVED, ESTABLISHED,
        FIN_WAIT1, FIN_WAIT2, FIN_WAIT3, CLOSING1,
        CLOSING2, FIN_RECEIVED, CLOSE_WAIT, LAST_ACK, ERROR
    }

    /// <summary>
    /// Endpoint identification
    /// </summary>
    public class TcpPair : IEquatable<TcpPair>
    {
        private IPEndPoint a;
        private IPEndPoint b;

        public IPEndPoint AEP { get { return a; } }
        public IPEndPoint BEP { get { return b; } }

        #region IPEndPoint Wrapper
        public IPAddress AIP
        {
            get { return a.Address; }
        }

        public IPAddress BIP
        {
            get { return b.Address; }
        }

        public Int32 APort
        {
            get { return a.Port; }
        }

        public Int32 BPort
        {
            get { return b.Port; }
        }
        #endregion

        public TcpPair(IPEndPoint epa, IPEndPoint epb)
        {
            a = epa;
            b = epb;
        }

        public IPEndPoint EndPoint(int direction)
        {
            return direction == 0 ? AEP : BEP;
        }

        public bool Equals(IPEndPoint epa, IPEndPoint epb)
        {
            return (a.Equals(epa) && b.Equals(epb))
                    || (a.Equals(epb) && b.Equals(epa));
        }

        public bool Equals(TcpPair other)
        {
            return (other.a.Equals(a) && other.b.Equals(b))
                || (other.a.Equals(b) && other.b.Equals(a));
        }

        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode();
        }
    }
}
