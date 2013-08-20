using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    public class TCB
    {
        /// <summary>
        /// Next sequence number excepted to receive.
        /// </summary>
        public UInt32 seq;

        #region Finite State Machine
        public TcpState State { get; private set; }

        /// <summary>
        /// This simplified fsm only changes state according to
        /// three flags: ACK, FIN, SYN
        /// So only have flags here is enough,
        /// but we must figure out whether is this stream's corresponding source
        /// host sent or received these flags.
        /// We only use this to detect connection start and end.
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="isReceived">true if flags was received by this tcb's corresponding host</param>
        public void Transit(TCPFlags flags, bool isReceived)
        {
            switch (State)
            {
            case TcpState.CLOSED:
                {
                    //if (isReceived && flags.SYN && !flags.ACK)
                    //{
                    //    state = TcpState.SYN_RECEIVED;
                    //}
                    //else if (!isReceived && flags.SYN && !flags.ACK)
                    //{
                    //    state = TcpState.SYN_SENT;
                    //}
                    if (isReceived)
                    {
                        State = TcpState.ESTABLISHED;
                    }
                }
                break;
            case TcpState.ESTABLISHED:
                {
                    if (isReceived && flags.FIN)
                    {
                        State = TcpState.CLOSED;
                    }
                }
                break;
            }
            
        }
        #endregion

        public TCB()
        {
            State = TcpState.CLOSED;
        }
    }
}
