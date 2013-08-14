using System.Collections.Generic;
using System;
using PacketDotNet;
using TrafficAnalysis.Util;
using System.Text;

namespace TrafficAnalysis.DeviceDataSource
{
    class PacketAnalyze
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

        #region PcapDotNet
        //public static CategorizeInfo SortPacket(Packet pk)
        //{
        //    CategorizeInfo info = new CategorizeInfo
        //    {
        //        NetworkLayer = new Dictionary<string,long>(),
        //        TransportLayer = new Dictionary<string,long>(),
        //        ApplicationLayer = new Dictionary<string,long>()
        //    };
        //    if (pk.DataLink.Kind == DataLinkKind.Ethernet)
        //    {
        //        EthernetDatagram ed = pk.Ethernet;
        //        // Network layer
        //        switch (ed.EtherType)
        //        {
        //        case EthernetType.Arp:
        //            info.NetworkLayer.Increment("ARP");
        //            break;
        //        case EthernetType.PointToPointProtocol:
        //            info.NetworkLayer.Increment("PPP");
        //            break;
        //        case EthernetType.IpV4:
        //            info.NetworkLayer.Increment("IPv4");
        //            break;
        //        case EthernetType.IpV6:
        //            info.NetworkLayer.Increment("IPv6");
        //            break;
        //        default:
        //            info.NetworkLayer.Increment("Others");
        //            break;
        //        }

        //        // Transport Layer, only IPv4 is supported
        //        if (ed.EtherType == EthernetType.IpV4)
        //        {
        //            IpV4Datagram ip4d = ed.IpV4;
        //            if (ip4d.Udp != null && ip4d.Udp.IsValid)
        //            {
        //                info.TransportLayer.Increment("UDP");
        //            }
        //            else if (ip4d.Tcp != null && ip4d.Tcp.IsValid)
        //            {
        //                info.TransportLayer.Increment("TCP");
        //            }
        //            else
        //            {
        //                info.TransportLayer.Increment("Others");
        //            }

        //            // Application Layer
        //            HttpDatagram httpdata = null;
        //            if (ip4d.Tcp != null && (httpdata = ip4d.Tcp.Http) != null)
        //            {
        //                info.ApplicationLayer.Increment("HTTP");
        //            }
        //            else
        //            {
        //                info.ApplicationLayer.Increment("Others");
        //            }
        //        }
        //    }

        //    return info;
        //}
        #endregion

        public static void SortPacket(StatisticsInfo ourStat, PcapDotNet.Packets.Packet pk)
        {
            Dictionary<string, long> res = new Dictionary<string, long>();
            ourStat.CInfo = ourStat.CInfo + SortPacket(pk);
        }
    }
}
