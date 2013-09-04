using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mime;
using System.Threading.Tasks;
using TrafficAnalysis.Util;

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

        public string DetectCharSet(HttpResponse rpy)
        {
            string charset = null;
            
            // first from content type
            ContentType type = new ContentType(rpy.ContentType);
            charset = type.CharSet;

            // then from html meta
            if (string.IsNullOrEmpty(charset))
            {
                charset = "utf-8";
            }
            return charset;
        }
    }
}
