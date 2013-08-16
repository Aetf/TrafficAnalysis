using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    public class TcpConnection
    {
        private TcpStream[] streams = new TcpStream[2];
        /// <summary>
        /// Get TCP stream in given direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public TcpStream Stream(int dir) { return streams[dir]; }

        public TcpPair Pair { get; private set; }

        private TCB[] tcbs = new TCB[2];
        /// <summary>
        /// Get TCP Control Block in given direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public TCB ControlBlock(int dir) { return tcbs[dir]; }

        private bool[] first = new bool[2];
        /// <summary>
        /// If is the first packet in given direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public bool IsFirst(int dir) { return first[dir]; }

        public TcpConnection(TcpPair pair)
        {
            tcbs[0] = new TCB();
            tcbs[1] = new TCB();
            first[0] = first[1] = true;
            Pair = pair;
        }
    }
}
