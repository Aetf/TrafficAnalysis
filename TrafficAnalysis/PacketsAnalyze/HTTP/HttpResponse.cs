using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    class HttpResponse
    {
        public HttpResponse()
        {
            ContentLength = -1;
            Body = null;
            OtherHeaders = new Dictionary<string, string>();
            TransferEncoding = "";
            ContentType = "plain/text";
        }

        #region Headers
        public int ResponseCode { get; set; }
        public int ParseResponseCode(Byte[] data, int index)
        {
            ResponseCode = 0;
            int factor = 100;
            for (int i = index; i != index + 3; i++)
            {
                ResponseCode += factor * (data[i] - 0x30); // data[i] - '0'
                factor /= 10;
            }

            return 3;
        }

        public string TransferEncoding { get; set; }
        public int ParseTransferEncoding(Byte[] data, int index)
        {
            int pos = index;
            while (data[pos] != 0x0D  // data[pos] != '\n'
               && data[pos] != 0x0A) // data[pos] != '\r'
            {
                pos++;
            }
            int count = pos - index;

            TransferEncoding = ASCIIEncoding.ASCII.GetString(data, index, count).TrimEnd();

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

        public string ContentType { get; set; }
        public int ParseContentType(Byte[] data, int index)
        {
            int pos = index;
            while(data[pos] != 0x0D  // data[pos] != '\n'
               && data[pos] != 0x0A) // data[pos] != '\r'
            {
                pos++;
            }
            int count = pos - index;

            ContentType = ASCIIEncoding.ASCII.GetString(data, index, count);
            ContentType.TrimEnd();

            return count;
        }

        public Dictionary<string, string> OtherHeaders { get; private set; }

        public HttpMethod Method { get; set; }
        #endregion

        public Byte[] Body { get; set; }

        public HttpRequest Request { get; set; }

        #region Some information about the connection
        /// <summary>
        /// Who sent it
        /// </summary>
        public IPEndPoint SentSource { get; set; }

        public UInt64 ConnectionID { get; set; }
        #endregion

        #region Static Helper Methods

        #endregion
    }
}
