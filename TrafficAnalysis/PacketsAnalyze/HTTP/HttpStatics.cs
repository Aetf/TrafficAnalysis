using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    public static class HttpStatics
    {
        public const int Httpd_Port = 80;

        
    }

    public enum HttpMethod
    {
        Head, Get, Post, Options, Put, Delete, Trace
    }

    public enum HttpVersion
    {
        Http_1_1, Http_1_0, Http_Unknown
    }
}
