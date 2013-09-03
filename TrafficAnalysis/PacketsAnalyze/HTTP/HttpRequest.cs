using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    public class HttpRequest
    {

        #region Headers
        public HttpMethod Method { get; set; }

        public string RequestURI { get; set; }
        public int ParseRequestURI(Byte[] data, int index)
        {
            int pos = index;
            while (data[pos] != 0x20)  // data[pos] == '<space>'
            {
                pos++;
            }
            int count = pos - index;

            RequestURI = FormatRequestURI(ASCIIEncoding.ASCII.GetString(data, index, count));

            return count;
        }

        public HttpVersion Version { get; set; }
        public int ParseHttpVersion(Byte[] data, int index)
        {
            int pos = index;
            while (data[pos] != 0x0D  // data[pos] != '\n'
               && data[pos] != 0x0A) // data[pos] != '\r'
            {
                pos++;
            }
            int count = pos - index;

            string str = ASCIIEncoding.ASCII.GetString(data, index, count);
            if (str.IndexOf("1.1") != -1)
            {
                Version = HttpVersion.Http_1_1;
            }
            else if (str.IndexOf("1.0") != -1)
            {
                Version = HttpVersion.Http_1_0;
            }
            else
            {
                Version = HttpVersion.Http_Unknown;
            }

            return count;
        }

        public int ContentLength { get; set; }
        public int ParseContentLength(Byte[] data, int index)
        {
            ContentLength = 0;
            int pos = index;

            // Find the end of the number
            while (data[pos] >= 0x30 && data[pos] <= 0x39) // '0' <= data[pos] <= '9'
                pos++;

            // record the width
            int width = pos - index;

            // back to the last byte belongs to the number
            pos--;

            int factor = 1;
            while (pos >= index)
            {
                ContentLength += factor * (data[pos] - 0x30); // data[i] - '0'
                factor *= 10;
                pos--;
            }

            return width;
        }

        public Dictionary<string, string> OtherHeaders { get; private set; }
        #endregion

        public Byte[] Body { get; set; }

        /// <summary>
        /// Matching response, this could be null
        /// </summary>
        public HttpResponse Response { get; set; }

        #region Some information about the connection
        /// <summary>
        /// Who sent it
        /// </summary>
        public IPEndPoint Source { get; set; }

        /// <summary>
        /// Who it was sent to
        /// </summary>
        public IPEndPoint Destination { get; set; }

        public UInt64 ConnectionID { get; set; }
        #endregion

        public HttpRequest()
        {
            OtherHeaders = new Dictionary<string, string>();
        }

        #region Static Helper Methods
        // TODO: Finish this method
        public static string FormatRequestURI(string str)
        {
            StringBuilder sb = new StringBuilder();
            //Decoder de = Encoding.ASCII.GetDecoder();
            //for (int i = 0; i != str.Length; i++)
            //{
            //    if (str[i] == '%')
            //    {
            //        Byte buf;

            //    }
            //    else
            //    {
            //        sb.Append(str[i]);
            //    }
            //}
            sb.Append(str);

            return sb.ToString();
        }
        #endregion
    }
}
