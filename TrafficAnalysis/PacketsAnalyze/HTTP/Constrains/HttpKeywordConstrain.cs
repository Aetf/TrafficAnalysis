using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mime;
using System.Threading.Tasks;
using TrafficAnalysis.Util;
using System.Text.RegularExpressions;

namespace TrafficAnalysis.PacketsAnalyze.HTTP.Constrains
{
    class HttpKeywordConstrain : ExtractConstrain
    {
        public string Keyword { get; set; }

        public override bool Apply(HttpResponse rpy)
        {
            if (!rpy.ContentType.Contains("text/html"))
                return false;

            string charset = DetectCharSet(rpy);
            Encoding encoding = Encoding.GetEncoding(charset);

            Byte[] bytes = encoding.GetBytes(Keyword);

            //if (rpy.Body.Locate(bytes).Length == 0)
            //    return false;            
            if (rpy.Body.BMIndexOf(bytes) == -1)
                return false;

            return true;
        }

        string DetectCharSet(HttpResponse rpy)
        {
            string charset = null;
            
            // first from content type
            ContentType type = new ContentType(rpy.ContentType);
            charset = type.CharSet;

            // then from html meta
            if (string.IsNullOrEmpty(charset))
            {
                Byte[] headend = Encoding.ASCII.GetBytes("</head>");
                Byte[] headendU = Encoding.ASCII.GetBytes("</HEAD>");

                int pos = rpy.Body.BMIndexOf(headend);
                if (pos == -1)
                    pos = rpy.Body.BMIndexOf(headendU);

                if (pos != -1)
                {
                    string header = Encoding.ASCII.GetString(rpy.Body, 0, pos + 1);
                    Regex r = new Regex(@"<meta\s[^>]*?charset=['""]?([^>]*?)['""][^>]*?/?>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    charset = r.Match(header).Groups[1].Value;
                }
            }

            // fallback
            if (string.IsNullOrEmpty(charset))
            {
                charset = "utf-8";
            }
            return charset;
        }
    }
}
