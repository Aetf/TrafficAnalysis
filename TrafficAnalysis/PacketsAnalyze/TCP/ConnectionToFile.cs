using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TrafficAnalysis.PacketsAnalyze.TCP
{
    class ConnectionToFile
    {
        private string savepath;
        public string SavePath
        {
            get { return savepath; }
            set
            {
                FileInfo info = new FileInfo(value);
                if (!info.Exists)
                {
                    Directory.CreateDirectory(value);
                    savepath = value;
                }
                else
                {
                    if ((info.Attributes & FileAttributes.Directory) == 0)
                    {
                        savepath = info.DirectoryName;
                    }
                    else
                    {
                        savepath = info.FullName;
                    }
                }
            }
        }

        public ConnectionToFile(string saveDir)
        {
            SavePath = saveDir;
        }

        public void Save(TcpConnection conn)
        {
            TcpPair pair = conn.Pair;
            string aip = pair.AIP.ToString().Replace(':', ' ');
            string bip = pair.BIP.ToString().Replace(':', ' ');
            string[] name = new string[]
            {
                string.Format("S[{0}][{1}]D[{2}][{3}]_{4}", aip, pair.APort, bip, pair.BPort, conn.ConnectionID),
                string.Format("S[{0}][{1}]D[{2}][{3}]_{4}", bip, pair.BPort, aip, pair.APort, conn.ConnectionID)
            };

            for (int i = 0; i != 2; i++)
            {
                FileInfo info = new FileInfo(Path.Combine(new string[] { SavePath, name[i] }));
                using (var fs = info.Create())
                {
                    conn.Stream(i).Data.Seek(0, SeekOrigin.Begin);
                    conn.Stream(i).Data.CopyTo(fs);
                }
            }
        }
    }
}
