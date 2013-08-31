using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PcapDotNet.Packets;

namespace TrafficAnalysis.DeviceDataSource
{
    public interface ICaptureDescreption
    {
        /// <summary>
        /// The capture task object
        /// </summary>
        Task CaptureTask { get; }

        /// <summary>
        /// The CancellationTokenSource to cancel the task
        /// </summary>
        CancellationTokenSource Cancellation { get; }

        /// <summary>
        /// Get the command to cancel the capture task
        /// </summary>
        RoutedCommand CancelTaskCommand { get; }

        /// <summary>
        /// The options about the task.
        /// This should be set before the task start,
        /// otherwise it wouldn't have effect
        /// </summary>
        DumpOptions Options { get; }

        /// <summary>
        /// The device to capture.
        /// This should be set before the task start,
        /// otherwise it wouldn't have effect
        /// </summary>
        DeviceDes Device { get; }

        /// <summary>
        /// Create a new task using Options and start it
        /// </summary>
        void StartCapture();
    }

    public class DumpOptions
    {
        public string Path { get; set; }
        public int Count { get; set; }
        public TimeSpan Durance { get; set; }
        public string Filter { get; set; }
        public PacketArrivalEventHandler Callback { get; set; }
    }

    #region PacketArrivalEvent
    public class PacketArrivalEventArgs : EventArgs
    {
        public IList<Packet> Packets { get; private set; }

        public PacketArrivalEventArgs(Packet packet)
        {
            Packets = new List<Packet>();
            Packets.Add(packet);
        }
    }
    public delegate void PacketArrivalEventHandler(object sender, PacketArrivalEventArgs args);
    #endregion

    #region CaptureEvent
    public class CaptureEventArgs : EventArgs
    {
        public ICaptureDescreption ControlBlock { get; private set; }
        public CaptureEventArgs(ICaptureDescreption ccb)
        {
            ControlBlock = ccb;
        }
    }
    public delegate void CaptureEventHandler(object sendor, CaptureEventArgs e);
    #endregion    
}
