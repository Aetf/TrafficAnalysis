using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net.Mime;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    class HttpExtractFiles
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

        public HttpExtractFiles(string saveDir)
        {
            WorkFolder = saveDir;
        }

        private void FormatBody(HttpResponse rpy)
        {
            ContentType type = new ContentType(rpy.ContentType);
            // First we must deal chunked message
            if (rpy.TransferEncoding.Contains("chunked"))
            {
                using (MemoryStream ss = new MemoryStream())
                {
                    int pos = 0;
                    while (pos < rpy.Body.Length)
                    {
                        int t;
                        int w;
                        if ((w = Parses.ParseHex(rpy.Body, pos, out t)) > 0)
                        {
                            if (t == 0)
                            {
                                pos += 2;
                                break;
                            }
                            // skip length
                            pos += w + 2;
                            // save content
                            ss.Write(rpy.Body, pos, t);
                            // skip to next chunk
                            pos += t + 2;
                        }
                        else
                        {
                            pos++;
                        }
                    }

                    rpy.Body = ss.ToArray();
                    rpy.ContentLength = rpy.Body.Length;
                }
            }

            // TODO: deal with multipart content type

            // Decompress if needed
            if (rpy.OtherHeaders.ContainsKey("Content-Encoding")
             && rpy.OtherHeaders["Content-Encoding"].Contains("gzip"))
            {
                using (MemoryStream mstream = new MemoryStream(rpy.Body))
                {
                    using (MemoryStream tstream = new MemoryStream())
                    {
                        using (GZipStream gstream = new GZipStream(mstream, CompressionMode.Decompress))
                        {
                            using (BufferedStream buf = new BufferedStream(gstream))
                            {
                                buf.CopyTo(tstream);
                                rpy.Body = tstream.ToArray();
                            }
                        }
                    }
                }
            }
            else
            {

            }
        }

        public void OutputContent(HttpResponse rpy)
        {
            if (rpy.Body == null || rpy.Body.Length == 0)
            {
                return;
            }

            FormatBody(rpy);

            DirectoryInfo gpwd = Directory.CreateDirectory(workFolder);
            DirectoryInfo pwd = gpwd.CreateSubdirectory(SelectFolder(rpy));

            // Decide the file name.
            string name = SelectName(rpy);

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

        private string SelectName(HttpResponse rpy)
        {
            ContentType type = new ContentType(rpy.ContentType);

            // Try from mime first.
            string name = type.Name;

            // Try from get string
            if (string.IsNullOrWhiteSpace(name))
            {
                if (rpy.Request != null)
                {
                    string str = rpy.Request.RequestURI;
                    int qpos = str.IndexOf('?');
                    if (qpos == -1)
                    {
                        int s = str.LastIndexOf('/');
                        name = str.Substring(s + 1);
                    }
                    else
                    {
                        int s = str.LastIndexOf('/', qpos);
                        name = str.Substring(s + 1, qpos - s - 1);
                    }
                }
            }

            // Generate a name if all previous atempts failed
            if (string.IsNullOrWhiteSpace(name))
            {
                name = rpy.ConnectionID.ToString() + MimeTypes.GetExt(type.MediaType);
            }

            // Try to recognize more file type using mime
            string ext = MimeTypes.GetExt(type.MediaType);
            if (!name.EndsWith(ext))
            {
                name += ext;
            }

            return name;
        }

        /// <summary>
        /// Create a folder for all files between these two ip
        /// </summary>
        /// <param name="rpy"></param>
        /// <returns></returns>
        private string SelectFolder(HttpResponse rpy)
        {
            string aip = rpy.Destination.Address.ToString().Replace(':', ' ');
            string bip = rpy.Source.Address.ToString().Replace(':', ' ');
            return string.Format("From[{0}] To[{1}]", aip, bip);
        }
    }
}
