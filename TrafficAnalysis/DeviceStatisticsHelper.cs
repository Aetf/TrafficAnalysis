using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using TrafficAnalysis.ChartEx;
using TrafficAnalysis.DeviceDataSource;
using TrafficAnalysis.Util;

namespace TrafficAnalysis
{
    public class NotifyKeyValuePair<TKey, TValue> : INotifyPropertyChanged
    {
        public NotifyKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        private TKey _key;
        public TKey Key
        {
            get { return _key; }
            set
            {
                var old = _key;
                _key = value;
                if (old == null || !old.Equals(_key))
                    OnPropertyChanged("Key");
            }
        }

        private TValue _value;
        public TValue Value
        {
            get { return _value; }
            set
            {
                var old = _value;
                _value = value;
                if (old == null || !old.Equals(_value))
                    OnPropertyChanged("Value");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is NotifyKeyValuePair<TKey, TValue>)
            {
                return this._key.Equals(((NotifyKeyValuePair<TKey, TValue>)obj)._key);
            }
            else if (obj is string)
            {
                return this._key.Equals((string)obj);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _key.GetHashCode();
        }

        #region PropertyChanged Event
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

    public class NotifyKeyValuePairSL : NotifyKeyValuePair<string, long>
    {
        public NotifyKeyValuePairSL(string key, long value)
            : base(key, value)
        {

        }
    }

    class DeviceStatisticsHelper
    {
        public DeviceDes Device { get; set; }

        public ChartPlotter BpsPlotter { get; set; }

        public ChartPlotter PpsPlotter { get; set; }

        #region public bool Shown
        private bool shown;
        public bool Shown
        {
            get
            {
                return shown;
            }
            
            set
            {
                if (value)
                {
                    startTime = DateTime.Now;
                }
                shown = value;
            }
        }
        #endregion

        public bool CaptureThis { get; set; }

        public RemovableDataSource<Tuple<TimeSpan, double>> Bps { get; set; }

        public RemovableDataSource<Tuple<TimeSpan, double>> Pps { get; set; }

        public ObservableCollection<NotifyKeyValuePairSL> NetworkLayer { get; set; }

        public ObservableCollection<NotifyKeyValuePairSL> TransportLayer { get; set; }

        public ObservableCollection<NotifyKeyValuePairSL> ApplicationLayer { get; set; }

        private DateTime startTime;

        public DeviceStatisticsHelper(ChartPlotter bps, ChartPlotter pps, DeviceDes des)
        {
            Device = des;
            Shown = false;
            CaptureThis = false;
            Pps = new RemovableDataSource<Tuple<TimeSpan, double>>();
            Bps = new RemovableDataSource<Tuple<TimeSpan, double>>();
            NetworkLayer = new ObservableCollection<NotifyKeyValuePairSL>();
            TransportLayer = new ObservableCollection<NotifyKeyValuePairSL>();
            ApplicationLayer = new ObservableCollection<NotifyKeyValuePairSL>();
            BpsPlotter = bps;
            PpsPlotter = pps;

            var axis = BpsPlotter.MainHorizontalAxis as HorizontalTimeSpanAxis;
            Bps.SetXYMapping(tp => new Point(axis.ConvertToDouble(tp.Item1), tp.Item2));
            axis = PpsPlotter.MainHorizontalAxis as HorizontalTimeSpanAxis;
            Pps.SetXYMapping(tp => new Point(axis.ConvertToDouble(tp.Item1), tp.Item2));
        }

        public void ChangeTo(StatisticsInfo info)
        {
            Bps.SuspendUpdate();
            Pps.SuspendUpdate();
            DateTime cur = DateTime.Now;
            Bps.Add(Tuple.Create(cur - startTime, info.Bps));
            Pps.Add(Tuple.Create(cur - startTime, info.Pps));

            //TODO: Check whether chart has enough points. if true, remove the first one.
            if (false)
            {
                Bps.RemoveAt(0);
                Pps.RemoveAt(0);
            }

            Bps.ResumeUpdate();
            Pps.ResumeUpdate();

            foreach (var key in info.TransportLayer.Keys)
            {
                TransportLayer.UpdateOrAdd(key, info.TransportLayer[key]);
            }
            foreach (var key in info.ApplicationLayer.Keys)
            {
                ApplicationLayer.UpdateOrAdd(key, info.ApplicationLayer[key]);
            }
            foreach (var key in info.NetworkLayer.Keys)
            {
                NetworkLayer.UpdateOrAdd(key, info.NetworkLayer[key]);
            }
        }
    }
}
