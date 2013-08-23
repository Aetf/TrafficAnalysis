using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TrafficAnalysis.DeviceDataSource
{
    /// <summary>
    /// Interface for analyze a pcap file
    /// </summary>
    interface IFileStatisticSource
    {
        /// <summary>
        /// ProgressChanged.
        /// </summary>
        event ProgressChangedEventHandler ProgressChanged;

        /// <summary>
        /// Load a capture file.
        /// This is a long time operation, and
        /// won't return untile the file is fully analyzed.
        /// This should typically run as a background thread.
        /// </summary>
        /// <param name="filepath"></param>
        void Load(string filepath);

        /// <summary>
        /// Absolute time
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        RangeStatisticsInfo Query(DateTime start, DateTime end);

        /// <summary>
        /// Relative time
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        RangeStatisticsInfo Query(TimeSpan start, TimeSpan end);

        /// <summary>
        /// Reassemble tcp streams in the file and save stream files in given directory.
        /// </summary>
        /// <param name="saveDir"></param>
        void TcpStreamReassemble(string saveDir);

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
