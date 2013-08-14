using System;
using System.Collections.Generic;

namespace TrafficAnalysis.DeviceDataSource
{
    /// <summary>
    /// Interface for analyze a pcap file
    /// </summary>
    interface IFileStatisticSource
    {
        /// <summary>
        /// Load a capture file.
        /// Won't return untile the file is fully analyzed.
        /// </summary>
        /// <param name="filepath"></param>
        void Load(string filepath);

        /// <summary>
        /// Absolute time
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        StatisticsInfo Query(DateTime start, DateTime end);

        /// <summary>
        /// Relative time
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        StatisticsInfo Query(TimeSpan start, TimeSpan end);

        /// <summary>
        /// Reset to load a new file.
        /// </summary>
        void Reset();

        /// <summary>
        /// Bps list
        /// </summary>
        IList<KeyValuePair<TimeSpan, double>> BpsList { get; }

        /// <summary>
        /// Pps list
        /// </summary>
        IList<KeyValuePair<TimeSpan, double>> PpsList { get; }

        /// <summary>
        /// Indicate whether the capture has been loaded
        /// </summary>
        bool FileLoaded { get; }

        /// <summary>
        /// The earliest timestamp in the file
        /// </summary>
        DateTime Earliest { get; }

        /// <summary>
        /// The latest timestamp in the file
        /// </summary>
        DateTime Latest { get; }
    }
}
