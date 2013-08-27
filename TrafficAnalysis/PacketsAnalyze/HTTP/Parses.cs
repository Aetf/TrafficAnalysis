using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    public static class Parses
    {
        public static int ParseInt(Byte[] data, int index, out int value)
        {
            value = 0;
            int pos = index;

            if (data[pos] < 0x30 || data[pos] > 0x39) // !('0' <= data[pos] <= '9')
            {
                // Don't start with a digit, return
                return 0;
            }

            // Find the end of the number
            while (data[pos] >= 0x30 && data[pos] <= 0x39) // '0' <= data[pos] <= '9'
                pos++;

            // record the width
            int width = pos - index;

            // back to the last byte belongs to the number
            pos--;

            int factor = 1;
            while (pos >= index)
            {
                value += factor * (data[pos] - 0x30); // data[i] - '0'
                factor *= 10;
                pos--;
            }

            return width;
        }
    }
}
