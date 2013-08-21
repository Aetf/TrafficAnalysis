using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.DeviceDataSource
{
    public class ReadOnlyObservableDeviceList : ReadOnlyObservableCollection<DeviceDes>
    {
        public virtual event NotifyCollectionChangedEventHandler DeviceListChanged;

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DeviceListChanged != null)
                DeviceListChanged(sender, e);
        }

        public ReadOnlyObservableDeviceList(ObservableCollection<DeviceDes> col)
            :base(col)
        {
            base.CollectionChanged += OnCollectionChanged;
        }
    }
}
