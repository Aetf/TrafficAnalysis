using System.Windows;

namespace TrafficAnalysis.Util
{
    public static class Auxiliary
    {
        public static double Height(this Thickness thickness)
        {
            return thickness.Top + thickness.Bottom;
        }

        public static double Width(this Thickness thickness)
        {
            return thickness.Left + thickness.Right;
        }
    }
}
