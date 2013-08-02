using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TrafficAnalysis
{
    static public class ControlEx
    {
        public static readonly DependencyProperty IconProperty =
          DependencyProperty.RegisterAttached("Icon", typeof(ImageSource), typeof(ControlEx), new PropertyMetadata(default(ImageSource)));

        public static void SetIcon(UIElement element, ImageSource value)
        {
            element.SetValue(IconProperty, value);
        }

        public static ImageSource GetIcon(UIElement element)
        {
            return (ImageSource)element.GetValue(IconProperty);
        }
    }

    class VsTab
    {
    }
}
