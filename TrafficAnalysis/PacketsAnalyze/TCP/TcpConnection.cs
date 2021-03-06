﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    public class TcpConnection
    {
        #region TcpStream
        private TcpStream[] streams = new TcpStream[2];
        /// <summary>
        /// Get TCP stream in given direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public TcpStream Stream(int dir) { return streams[dir]; }
        #endregion

        #region TCB
        private TCB[] tcbs = new TCB[2];
        /// <summary>
        /// Get receiver's TCP Control Block in given direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public TCB ControlBlock(int dir) { return tcbs[dir]; }
        #endregion

        /// <summary>
        /// Tcp AddressPair that identificates this connection
        /// </summary>
        public TcpPair Pair { get; private set; }

        /// <summary>
        /// Get the connection ID.
        /// </summary>
        public UInt64 ConnectionID { get; private set; }

        /// <summary>
        /// Directly append data to stream in given direction.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="data"></param>
        public void WritePacketData(int dir, Byte[] data)
        {
            TcpStream stream = streams[dir];
            stream.IsEmpty = false;

            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// When received a FIN, we can close the stream since no more data will be received.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns>Indicate whether both stream has been closed.</returns>
        public bool CloseStream(int dir)
        {
            streams[dir].OnClose();

            return streams[dir].IsFinished && streams[1 - dir].IsFinished;
        }

        public TcpConnection(TcpPair pair)
        {
            for (int i = 0; i != 2; i++)
            {
                tcbs[i] = new TCB();
                streams[i] = new TcpStream();
            }

            ConnectionID = GetID();
            Pair = pair;
        }

        #region ConnectionID generate
        private static UInt64 nxtid = 0;
        private static UInt64 GetID()
        {
            return nxtid++;
        }

        /// <summary>
        /// Set next id to use. This should be careful.
        /// </summary>
        /// <param name="?"></param>
        internal static void SetNextID(UInt64 id)
        {
            nxtid = id;
        }
        #endregion
    }
}
