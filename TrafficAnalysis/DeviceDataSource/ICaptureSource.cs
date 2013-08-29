using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Packets;

namespace TrafficAnalysis.DeviceDataSource
{
    interface ICaptureSource
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
    public struct DumpOptions
    {
        public string Path;
        public int Count;
        public TimeSpan Durance;
        public string Filter;
        public PacketArrivalEventHandler Callback;
    }
}
