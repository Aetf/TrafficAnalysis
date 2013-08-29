using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace TrafficAnalysis.DeviceDataSource
{
    /// <summary>
    /// Interface for an abstract warpper around WinPcap
    /// Used to run statistics on devices.
    /// </summary>
    public interface IDeviceSource
    {
        Dictionary<string, StatisticsInfo> Statistics { get; }
        ReadOnlyCollection<DeviceDes> DeviceList { get; }
        ReadOnlyObservableDeviceList MonitoringList { get; }

        void StartStatistic(DeviceDes des);
        void StopStatistic(DeviceDes des);
    }
}
