using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using PcapDotNet.Packets;
using PcapDotNet.Core;

namespace TrafficAnalysis.DeviceDataSource
{
    class CaptureMod : ICaptureSource
    {
        public CaptureMod()
        {
            Cancellation = new CancellationTokenSource();

            Options = new DumpOptions()
            {
                Count = int.MaxValue,
                Durance = TimeSpan.MaxValue
            };
        }

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
