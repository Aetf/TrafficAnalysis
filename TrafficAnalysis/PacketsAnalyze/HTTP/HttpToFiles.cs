using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    class HttpToFiles
    {
        #region public string WorkFolder
        private string workFolder;
        public string WorkFolder
        {
            get { return workFolder; }
            set
            {
                FileInfo info = new FileInfo(value);
                if (!info.Exists)
                {
                    Directory.CreateDirectory(value);
                    workFolder = value;
                }
                else
                {
                    if ((info.Attributes & FileAttributes.Directory) == 0)
                    {
                        workFolder = info.DirectoryName;
                    }
                    else
                    {
                        workFolder = info.FullName;
                    }
                }
            }
        }
        #endregion

        public HttpToFiles(string saveDir)
        {
            WorkFolder = saveDir;
        }

        private void FormatBody(HttpResponse rpy)
        {
            ContentType type = new ContentType(rpy.ContentType);
            // TODO: deal with multipart content type
        }

        public void OutputContent(HttpResponse rpy)
        {
            if (rpy.Body == null || rpy.Body.Length == 0)
            {
                return;
            }

            FormatBody(rpy);

            DirectoryInfo gpwd = Directory.CreateDirectory(workFolder);

            // Create a folder for all files between these two ip
            string aip = rpy.Request.SentSource.Address.ToString().Replace(':', ' ');
            string bip = rpy.SentSource.Address.ToString().Replace(':', ' ');
            DirectoryInfo pwd = gpwd.CreateSubdirectory(string.Format("From[{0}] To[{1}]", aip, bip));

            // Decide the file name.
            ContentType type = new ContentType(rpy.ContentType);

            // Try from mime first.
            string name = type.Name;

            // Try from get string
            if (string.IsNullOrWhiteSpace(name))
            {
                if (rpy.Request != null)
                {
                    name = rpy.Request.RequestURI.Substring(rpy.Request.RequestURI.LastIndexOf("/") + 1);
                }
            }

            // Generate a name
            if (string.IsNullOrWhiteSpace(name))
            {
                string ext = MimeTypes.GetExt(type.MediaType);
                name = rpy.ConnectionID.ToString() + ext;
            }

            // If file exists, append a number
            string destfile = Path.Combine(pwd.FullName, name);
            int num = 1;
            while (File.Exists(destfile))
            {
                string n = Path.GetFileNameWithoutExtension(name);
                string e = Path.GetExtension(name);
                destfile = Path.Combine(pwd.FullName, string.Format("{0}_{1}{2}", n, num, e));
                num++;
            }

            using (FileStream fs = File.Create(destfile))
            {
                fs.Write(rpy.Body, 0, rpy.Body.Length);
            }
        }
    }
}
