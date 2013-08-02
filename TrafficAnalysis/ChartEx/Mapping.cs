using System;
using System.Windows;

namespace TrafficAnalysis.ChartEx
{
    class Mapping<TSource>
    {
        internal DependencyProperty Property { get; set; }
        internal Func<TSource, object> F { get; set; }
    }
}
