using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace TrafficAnalysis.DeviceDataSource
{
    /// <summary>
    /// Interface for an abstract warpper around WinPcap
    /// </summary>
    public interface IDeviceSource
    {
        Dictionary<string, StatisticsInfo> Statistics { get; }
        ReadOnlyCollection<DeviceDes> DeviceList { get; }
        ReadOnlyObservableDeviceList MonitoringList { get; }

        void StartStatistic(DeviceDes des);
        void StopStatistic(DeviceDes des);

        Tuple<Task, CancellationTokenSource> CreateCaptureTask(DeviceDes des, DumpOptions options);
    }

    public struct DumpOptions
    {
        public string Path;
        public int Count;
        public TimeSpan Durance;
        public string filter;
    }
}
