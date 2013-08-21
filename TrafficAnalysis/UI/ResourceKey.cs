using System;
using System.Windows.Markup;

namespace TrafficAnalysis.UI
{
    public class ResourceKey : MarkupExtension
    {
        public object Value { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Value;
        }
    }
}
