using System.Collections.Generic;
using System;
using System.Linq;
using TrafficAnalysis.Util;
using System.Text;
using TrafficAnalysis.DeviceDataSource;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Ethernet;

namespace TrafficAnalysis.PacketsAnalyze
{
    class SingleAnalyzer
    {
        public static CategorizeInfo SortPacket(PcapDotNet.Packets.Packet raw)
        {
            // Get a packet from raw data.
            // Be a little lazy and assume that only Ethernet DLL protcol is used.
            EthernetDatagram packet = raw.Ethernet;

            CategorizeInfo info = new CategorizeInfo
            {
                NetworkLayer = new Dictionary<string, long>(),
                TransportLayer = new Dictionary<string, long>(),
                ApplicationLayer = new Dictionary<string, long>()
            };

            #region Network Layer
            switch (packet.EtherType)
            {
            case EthernetType.Arp:
                info.NetworkLayer.Increment("ARP");
                break;
            case EthernetType.IpV4:
                info.NetworkLayer.Increment("IPv4");
                break;
            case EthernetType.IpV6:
                info.NetworkLayer.Increment("IPv6");
                break;
            default:
                info.NetworkLayer.Increment("Others");
                break;
            }
            #endregion

            #region Transport Layer
            IpV4Datagram ipv4pk = null;
            if (packet.EtherType == EthernetType.IpV4)
            {
                ipv4pk = packet.IpV4;
                switch (ipv4pk.Protocol)
                {
                case IpV4Protocol.Tcp:
                    info.TransportLayer.Increment("TCP");
                    break;
                case IpV4Protocol.Udp:
                    info.TransportLayer.Increment("UDP");
                    break;
                default:
                    info.TransportLayer.Increment("Others");
                    break;
                }
            }
            #endregion

            #region Application Layer
            if (ipv4pk != null)
            {
                if (ipv4pk.Protocol == IpV4Protocol.Tcp)
                {
                    TcpDatagram tcpPk= ipv4pk.Tcp;
                    string s = new ASCIIEncoding().GetString(tcpPk.Payload.ToArray());
                    if (s.IndexOf("HTTP") > 0)
                    {  // Found HTTP head
                        info.ApplicationLayer.Increment("HTTP Header");
                    }
                    else
                    {
                        info.ApplicationLayer.Increment("Others");
                    }
                }
            }
            #endregion

            return info;
        }
    }
}
