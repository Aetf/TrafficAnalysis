using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.DynamicDataDisplay.Converters;
using System.Windows;
using System.Globalization;
using Microsoft.Research.DynamicDataDisplay;

namespace TrafficAnalysis.ChartEx
{
    public class InjectedPlotterHorizontalSyncConverter : GenericValueConverter<DataRect>
    {
        private readonly InjectedPlotter injectedPlotter;
        public InjectedPlotterHorizontalSyncConverter(InjectedPlotter plotter)
		{
			if (plotter == null)
				throw new ArgumentNullException("plotter");

			this.injectedPlotter = plotter;
		}

		public override object ConvertCore(DataRect value, Type targetType, object parameter, CultureInfo culture)
		{
			if (injectedPlotter.Plotter == null)
				return DependencyProperty.UnsetValue;

			var outerVisible = value;
			var innerVisible = injectedPlotter.Visible;
            return new DataRect(outerVisible.XMin, innerVisible.YMin, outerVisible.Width, innerVisible.Height);
		}

		public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DataRect)
			{
				DataRect innerVisible = (DataRect)value;
				var outerVisible = injectedPlotter.Plotter.Visible;
				return outerVisible;
			}
			else
			{
				return DependencyProperty.UnsetValue;
			}
		}
    }
}
