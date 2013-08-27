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

        private void FormatBody(HttpResponse rpy)
        {
            ContentType type = new ContentType(rpy.ContentType);
            // TODO: deal with multipart content type
        }

        public void OutputContent(HttpResponse rpy)
        {
            FormatBody(rpy);

            DirectoryInfo gpwd = Directory.CreateDirectory(workFolder);

            // Create a folder for all files between these two ip
            string aip = rpy.Request.SentSource.Address.ToString().Replace(':', ' ');
            string bip = rpy.SentSource.Address.ToString().Replace(':', ' ');
            DirectoryInfo pwd = gpwd.CreateSubdirectory(string.Format("From[{0}] To[{1}]", aip, bip));

            // Decide the file name.
            ContentType type = new ContentType(rpy.ContentType);
            string ext = MimeTypes.GetExt(type.MediaType);

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
                name = rpy.ConnectionID.ToString();
            }

            // If file exists, append a number
            string destfile = Path.Combine(pwd.FullName, name + ext);
            int num = 1;
            while (File.Exists(destfile))
            {
                destfile = Path.Combine(pwd.FullName, string.Format("{0}_{1}{2}", name, num, ext));
                num++;
            }

            using (FileStream fs = File.Create(destfile))
            {
                fs.Write(rpy.Body, 0, rpy.Body.Length);
            }
        }
    }
}
