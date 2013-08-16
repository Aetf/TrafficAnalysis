using System.Collections.Generic;
using System;
using PacketDotNet;
using TrafficAnalysis.Util;
using System.Text;
using TrafficAnalysis.DeviceDataSource;

namespace TrafficAnalysis.PacketsAnalyze
{
    class SingleAnalyzer
    {
        public static CategorizeInfo SortPacket(PcapDotNet.Packets.Packet raw)
        {
            // Get a packet from raw data.
            // Be a little lazy and assume that only Ethernet DLL protcol is used.
            EthernetPacket packet = Packet.ParsePacket(LinkLayers.Ethernet, raw.Buffer) as EthernetPacket;

            CategorizeInfo info = new CategorizeInfo
            {
                NetworkLayer = new Dictionary<string, long>(),
                TransportLayer = new Dictionary<string, long>(),
                ApplicationLayer = new Dictionary<string, long>()
            };

            #region Network Layer
            switch (packet.Type)
            {
            case EthernetPacketType.Arp:
                info.NetworkLayer.Increment("ARP");
                break;
            case EthernetPacketType.IpV4:
                info.NetworkLayer.Increment("IPv4");
                break;
            case EthernetPacketType.IpV6:
                info.NetworkLayer.Increment("IPv6");
                break;
            case EthernetPacketType.Loop:
                info.NetworkLayer.Increment("Loop");
                break;
            default:
                info.NetworkLayer.Increment("Others");
                break;
            }
            #endregion

            #region Transport Layer
            IPv4Packet ipv4pk = packet.Extract(typeof(IPv4Packet)) as IPv4Packet;
            if (ipv4pk != null)
            {
                switch (ipv4pk.Protocol)
                {
                case IPProtocolType.TCP:
                    info.TransportLayer.Increment("TCP");
                    break;
                case IPProtocolType.UDP:
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
                if (ipv4pk.Protocol == IPProtocolType.TCP)
                {
                    TcpPacket tcpPk= ipv4pk.PayloadPacket as TcpPacket;
                    string s = new ASCIIEncoding().GetString(tcpPk.PayloadData);
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
