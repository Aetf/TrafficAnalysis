using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TrafficAnalysis.DeviceDataSource
{
    /// <summary>
    /// Interface for an abstract warpper around WinPcap
    /// </summary>
    public interface IStatisticsSource
    {
        Dictionary<string, StatisticsInfo> Statistics { get; }
        ReadOnlyCollection<DeviceDes> DeviceList { get; }
        ReadOnlyObservableDeviceList MonitoringList { get; }

        void StartCapture(DeviceDes des, string dumppath = null);
        void StopCapture(DeviceDes des);
    }
}
