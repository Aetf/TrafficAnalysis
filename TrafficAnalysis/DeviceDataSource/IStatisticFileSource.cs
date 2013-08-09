using System;

namespace TrafficAnalysis.DeviceDataSource
{
    /// <summary>
    /// Interface for analyze a pcap file
    /// </summary>
    interface IFileStatisticSource
    {
        void Load(string filepath);

        StatisticsInfo Query(DateTime start, DateTime end);
    }
}
