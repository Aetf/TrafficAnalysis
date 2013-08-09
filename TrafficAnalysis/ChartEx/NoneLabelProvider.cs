using System.Windows;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.Charts.Axes;

namespace TrafficAnalysis.ChartEx
{
    class NoneLabelProvider<T> : GenericLabelProvider<T>
    {
        public override UIElement[] CreateLabels(ITicksInfo<T> ticksInfo)
        {
            UIElement[] res = base.CreateLabels(ticksInfo);

            foreach (var e in res)
            {
                e.Visibility = Visibility.Collapsed;
            }

            return res;
        }
    }

    class NoneLabelProviderInt : NoneLabelProvider<int>
    {

    }
}
