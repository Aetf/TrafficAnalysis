using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using TrafficAnalysis.PacketsAnalyze.TCP;
using TrafficAnalysis.PacketsAnalyze;
using System.Net.Mime;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    class HttpReconstructor
    {
        public List<HttpRequest> RequestList { get; private set; }

        public List<HttpResponse> ResponseList { get; private set; }

        public HttpReconstructor()
        {
            RequestList = new List<HttpRequest>();
            ResponseList = new List<HttpResponse>();
        }

        public void OnConnectionFinished(TcpConnection conn)
        {
            // We only want http stream
            if (conn.Pair.APort != HttpStatics.Httpd_Port
                && conn.Pair.BPort != HttpStatics.Httpd_Port)
                return;

            RequestList.Clear();
            ResponseList.Clear();

            // Find client direction
            int reqDir = conn.Pair.APort == HttpStatics.Httpd_Port ? 1 : 0;
            int rpyDir = 1 - reqDir;

            // Set position to begin to all streams, in case they are not there.
            for (int i = 0; i != 2; i++)
            {
                conn.Stream(i).Data.Seek(0, SeekOrigin.Begin);
            }

            FindRequests(conn, reqDir);
            FindResponses(conn, rpyDir);
        }

        private void FindRequests(TcpConnection conn, int direction)
        {
            Int64 totalRequestLength = conn.Stream(direction).Data.Length;

            ReqStateType state = ReqStateType.ReqStateStartMethod;
            Byte[] data = conn.Stream(direction).Data.ToArray();
            int cur = 0;
            HttpRequest req = null;
            while (cur < data.Length)
            {
                switch (state)
                {
                case ReqStateType.ReqStateStartMethod:
                    // Try to find a word describing a method.
                    // These are all the methods defined in
                    // draft-ietf-http-v11-spec-rev-06
                    bool found = false;
                    HttpMethod[] allmethods = (HttpMethod[])Enum.GetValues(typeof(HttpMethod));
                    foreach (var method in allmethods)
                    {
                        string str = method.ToString() + ' ';
                        int methodlen = str.Length;
                        if (strncasecmp(data, cur, methodlen, str))
                        {
                            found = true;
                            // Make a new record for this entry
                            req = new HttpRequest();
                            req.Method = method;

                            cur += methodlen;
                            state = ReqStateType.ReqStateFinishMethod;
                            break;
                        }
                    }

                    // Couldn't find a valid method,
                    // so increment and attempt to resynchronize.
		            // This shouldn't happen often.
                    if (!found)
                    {
                        cur++;
                    }
                    break;

                case ReqStateType.ReqStateFinishMethod:
                    // RequestURI
                    cur += req.ParseRequestURI(data, cur);

                    // Http version
                    cur += req.ParseHttpVersion(data, cur);

                    state = ReqStateType.ReqStateFindContentLength;
                    break;

                case ReqStateType.ReqStateFindContentLength:
                    // Locate content-length field, if any
                    if (strncasecmp(data, cur, 17, "\r\nContent-Length:"))
                    {
                        cur += 17;
                        // skip leading spaces
                        while (data[cur] == 0x20) // data[cur] == '<space>'
                            cur++;

                        cur += req.ParseContentLength(data, cur);
                    }
                    else if (strncasecmp(data, cur, 4, "\r\n\r\n"))
                    {
                        // if no content-length header detected, assume zero.
                        // fall through
                        req.ContentLength = 0;
                        state = ReqStateType.ReqStateFinishHeader;
                    }
                    else if(strncasecmp(data, cur, 2, "\r\n"))
                    {
                        cur += 2;
                        // Try to fatch other possible headers
                        int pos = cur;
                        while (data[pos] != 0x0D  // data[pos] != '\n'
                            && data[pos] != 0x0A) // data[pos] != '\r'
                        {
                            pos++;
                        }

                        string str = Encoding.ASCII.GetString(data, cur, pos - cur);
                        int idx = str.IndexOf(":");
                        if (idx != -1)
                        {
                            string field = str.Substring(0, idx).Trim();
                            string value = str.Substring(idx + 1).Trim();
                            req.OtherHeaders[field] = value;
                        }
                        else
                        {
                            // This should not happen during a specific compatate connection.
                            // But if it happened, only we can do is to ignore it.
                        }

                        cur = pos;
                    }
                    else
                    {
                        cur++;
                    }
                    break;

                case ReqStateType.ReqStateFinishHeader:
                    if (strncasecmp(data, cur, 4, "\r\n\r\n"))
                    {
                        // Found end of header
                        cur += 4;

                        if (req.ContentLength > 0)
                        {
                            req.Body = new Byte[req.ContentLength];
                            Array.Copy(data, cur, req.Body, 0, req.ContentLength);
                        }
                        else
                        {
                            // QUESTION: What if a POST with no content-length?
                            req.Body = null;
                        }

                        state = ReqStateType.ReqStateStartMethod;

                        // Fill in other infos.
                        req.SentSource = conn.Pair.EndPoint(direction);
                        req.ConnectionID = conn.ConnectionID;
                        RequestList.Add(req);
                    }
                    else
                    {
                        cur++;
                    }
                    break;
                }
            }
        }

        private void FindResponses(TcpConnection conn, int direction)
        {
            Int64 totalReplyLength = conn.Stream(direction).Data.Length;

            RpyStateType state = RpyStateType.RpyStateStartHttp;
            Byte[] data = conn.Stream(direction).Data.ToArray();
            int cur = 0;
            int reqIdx = 0;
            HttpResponse rpy = null;
            while (cur < data.Length)
            {
                switch (state)
                {
                // Start state: Find "HTTP/" that begins a response
                case RpyStateType.RpyStateStartHttp:
                    if (strncasecmp(data, cur, 5, "HTTP/"))
                    {
                        // Found start of a response
                        state = RpyStateType.RpyStateFinishHttp;

                        cur += 5;
                        rpy = new HttpResponse();
                    }
                    else
                    {
                        cur++;
                    }
                    break;

                // Finish off HTTP string (version number) by looking for whitespace
                case RpyStateType.RpyStateFinishHttp:
                    if (data[cur] == 0x20) // data[cur] == <space>
                    {
                        state = RpyStateType.RpyStateFindResponse;
                    }
                    else
                    {
                        cur++;
                    }
                    break;

                // Look for response code by finding non-whitespace.
                case RpyStateType.RpyStateFindResponse:
                    if (data[cur] != 0x20) // data[cur] == <space>
                    {
                        cur += rpy.ParseResponseCode(data, cur);
                        state = RpyStateType.RpyStateFindContentLength;
                    }
                    else
                    {
                        cur++;
                    }
                    break;

                // this state is now misnamed since we pull out other
                // headers than just content-length now.
                case RpyStateType.RpyStateFindContentLength:
                    if (strncasecmp(data, cur, 17, "\r\nContent-Length:"))
                    {
                        cur += 17;
                        // skip leading spaces
                        while (data[cur] == 0x20) // data[cur] == '<space>'
                            cur++;

                        cur += rpy.ParseContentLength(data, cur);
                    }
                    else if (strncasecmp(data, cur, 15, "\r\nContent-Type:"))
                    {
                        cur += 15;
                        // skip leading spaces
                        while (data[cur] == 0x20) // data[cur] == '<space>'
                            cur++;

                        cur += rpy.ParseContentType(data, cur);
                    }
                    else if(strncasecmp(data, cur, 20, "\r\nTransfer-Encoding:"))
                    {
                        cur += 20;
                        // skip leading spaces
                        while (data[cur] == 0x20) // data[cur] == '<space>'
                            cur++;

                        cur += rpy.ParseTransferEncoding(data, cur);
                    }
                    else if (strncasecmp(data, cur, 4, "\r\n\r\n"))
                    {
                        // No increment for cur here, effectively fall through
                        state = RpyStateType.RpyStateFinishHeader;
                    }
                    else if(strncasecmp(data, cur, 2, "\r\n"))
                    {
                        cur += 2;
                        // Try to fatch other possible headers
                        int pos = cur;
                        while (data[pos] != 0x0D  // data[pos] != '\n'
                            && data[pos] != 0x0A) // data[pos] != '\r'
                        {
                            pos++;
                        }

                        string str = Encoding.ASCII.GetString(data, cur, pos - cur);
                        int idx = str.IndexOf(":");
                        if (idx != -1)
                        {
                            string field = str.Substring(0, idx).Trim();
                            string value = str.Substring(idx + 1).Trim();
                            rpy.OtherHeaders[field] = value;
                        }
                        else
                        {
                            // This should not happen during a specific compatate connection.
                            // But if it happened, only we can do is to ignore it.
                        }

                        cur = pos;
                    }
                    else
                    {
                        cur++;
                    }
                    break;

                // Skip over the rest of the header
                case RpyStateType.RpyStateFinishHeader:
                    if (strncasecmp(data, cur, 4, "\r\n\r\n"))
                    {
                        // Found end of header
                        cur += 4;

                        // At this point, we need to find the end of the
                        // response body.  There's a variety of ways to
                        // do this, but in any case, we need to make sure
                        // that ryp.ContentLength, cur are all set appropriately.

                        // See if we can ignore the body.
                        // We can do this
                        // for the reply to HEAD
                        // for a 1xx
                        // for a 204 (no content), 205 (reset content), or 304 (not modified).
                        if (RequestList[reqIdx].Method == HttpMethod.Head
                            || rpy.ResponseCode < 200
                            || rpy.ResponseCode == 204
                            || rpy.ResponseCode == 205
                            || rpy.ResponseCode == 304)
                        {
                            rpy.ContentLength = 0;
                            rpy.Body = null;
                        }
                        else if (rpy.TransferEncoding != "")
                        {
                            // According to RFC 2616, when both TransferEncoding and ContentLength
                            // present, latter is ignored.
                            if (rpy.TransferEncoding.Contains("chunked"))
                            {
                                int pos = cur;
                                while (pos < data.Length)
                                {
                                    int t;
                                    int w;
                                    if ((w = Parses.ParseInt(data, pos, out t)) > 0)
                                    {
                                        if (t == 0)
                                        {
                                            pos += 2;
                                            break;
                                        }
                                        // skip to next chunk
                                        pos += w + 2 + t + 2;
                                    }
                                    else
                                    {
                                        pos++;
                                    }
                                }

                                rpy.ContentLength = pos - cur + 1;
                                rpy.Body = new Byte[rpy.ContentLength];
                                Array.Copy(data, cur, rpy.Body, 0, rpy.ContentLength);
                                cur += rpy.ContentLength;

                                // TODO: There may also tailors to deal with
                            }
                            else
                            {
                                // Use content-length header if one was present.
                                rpy.Body = new Byte[rpy.ContentLength];
                                Array.Copy(data, cur, rpy.Body, 0, rpy.ContentLength);
                                cur += rpy.ContentLength;
                            }
                        }
                        else if (rpy.ContentLength != -1)
                        {
                            // Use content-length header if one was present.
                            rpy.Body = new Byte[rpy.ContentLength];
                            Array.Copy(data, cur, rpy.Body, 0, rpy.ContentLength);
                            cur += rpy.ContentLength;
                        }
                        else
                        {
                            // No content-length header found,
                            // so delimit response by end of stream.
                            // But make sure we do not have a "\r\n\r\n" string
                            // in the response, which may indicate the begining
                            // of a following response.
                            int start = cur;
                            while (cur < data.Length)
                            {
                                if (strncasecmp(data, cur, 4, "\r\n\r\n"))
                                {
                                    cur += 4;
                                    state = RpyStateType.RpyStateStartHttp;
                                    break;
                                }
                                else
                                {
                                    cur++;
                                }
                            }

                            if (state == RpyStateType.RpyStateStartHttp)
                            {
                                rpy.ContentLength = cur - start;
                                
                            }
                            else
                            {
                                rpy.ContentLength = data.Length - start;
                                cur = data.Length;
                            }

                            rpy.Body = new Byte[rpy.ContentLength];
                            Array.Copy(data, start, rpy.Body, 0, rpy.ContentLength);
                        }

                        // Set next state
                        state = RpyStateType.RpyStateStartHttp;

                        // Fill in other infos
                        rpy.SentSource = conn.Pair.EndPoint(direction);
                        rpy.ConnectionID = conn.ConnectionID;
                        // Add reply to list
                        ResponseList.Add(rpy);
                        // Should always match in a complete connection
                        if (reqIdx < RequestList.Count)
                        {
                            rpy.Request = RequestList[reqIdx];
                            RequestList[reqIdx++].Response = rpy;
                        }
                    }
                    else
                    {
                        cur++;
                    }
                    break;
                }
            }
        }

        #region Helpers
        private bool strncasecmp(Byte[] strData, int byteIndex, int byteCount, string str)
        {
            if (byteIndex + byteCount > strData.Length)
                return false;

            Decoder de = Encoding.ASCII.GetDecoder();
            Char[] chars = new Char[byteCount];
            int chlen = de.GetChars(strData, byteIndex, byteCount, chars, 0, true);
            string s = new string(chars, 0, chlen);

            return s.Equals(str, StringComparison.OrdinalIgnoreCase);
        }


        #endregion

        #region Static
        private enum RpyStateType
        {
            RpyStateStartHttp, RpyStateFinishHttp,
            RpyStateFindResponse, RpyStateFindContentLength,
            RpyStateFinishHeader
        }

        private enum ReqStateType
        {
            ReqStateStartMethod, ReqStateFinishMethod,
            ReqStateFindContentLength, ReqStateFinishHeader
        }
        #endregion
    }
}
