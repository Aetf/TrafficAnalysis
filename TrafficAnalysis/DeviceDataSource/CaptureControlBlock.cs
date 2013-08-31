using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PcapDotNet.Core;
using PcapDotNet.Packets;

namespace TrafficAnalysis.DeviceDataSource
{
    class CaptureControlBlock : ICaptureDescreption
    {
        /// <summary>
        /// The capture task object
        /// </summary>
        public Task CaptureTask { get; private set; }

        /// <summary>
        /// The CancellationTokenSource to cancel the task
        /// </summary>
        public CancellationTokenSource Cancellation { get; private set; }

        /// <summary>
        /// The options about the task.
        /// This should be set before the task start,
        /// otherwise it wouldn't have effect
        /// </summary>
        public DumpOptions Options { get; set; }

        /// <summary>
        /// The device to capture.
        /// This should be set before the task start,
        /// otherwise it wouldn't have effect
        /// </summary>
        public DeviceDes Device { get; set; }

        #region Commands
        public RoutedCommand CancelTaskCommand { get; private set; }
        #endregion

        public CaptureControlBlock()
        {
            Cancellation = new CancellationTokenSource();
            CancelTaskCommand = new RoutedCommand();

            Options = new DumpOptions()
            {
                Count = int.MaxValue,
                Durance = TimeSpan.MaxValue
            };

            
        }

        public void StartCapture()
        {
            var dev = LivePacketDevice.AllLocalMachine.FirstOrDefault(d => d.Name.Equals(Device.Name));
            if (dev == null)
                return;

            var token = Cancellation.Token;

            this.CaptureTask = new Task((state) =>
            {
                DumpOptions option = (DumpOptions)state;

                // Open device
                using (PacketCommunicator communicator = dev.Open(
                    65535, PacketDeviceOpenAttributes.Promiscuous,
                    250
                    ))
                {
                    if (option.Filter != null)
                    {
                        communicator.SetFilter(option.Filter);
                    }

                    using (PacketDumpFile dumpFile = communicator.OpenDump(option.Path))
                    {
                        int count = 0;
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();

                        while (count < option.Count
                               && stopwatch.ElapsedMilliseconds < option.Durance.TotalMilliseconds)
                        {
                            token.ThrowIfCancellationRequested();

                            Packet packet;
                            var result = communicator.ReceivePacket(out packet);

                            if (result == PacketCommunicatorReceiveResult.Ok)
                            {
                                dumpFile.Dump(packet);
                                count++;

                                if (option.Callback != null)
                                {
                                    option.Callback(null, new PacketArrivalEventArgs(packet));
                                }
                            }
                        }
                    }
                }
            }, Options, token, TaskCreationOptions.LongRunning);

            CaptureTask.Start();
        }
    }
}
