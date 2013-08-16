using System;
using System.Windows.Media;

namespace TrafficAnalysis.Util
{
    class ColorGen
    {
        static Random r = new Random();
        public static Color GetColor()
        {
            //  为了在白色背景上显示，尽量生成深色
            int int_Red = r.Next(256);
            int int_Green = r.Next(256);
            int int_Blue = (int_Red + int_Green > 400) ? 0 : 400 - int_Red - int_Green;
            int_Blue = (int_Blue > 255) ? 255 : int_Blue;

            return Color.FromRgb((byte)int_Red, (byte)int_Green, (byte)int_Blue);
        }
    }
}
