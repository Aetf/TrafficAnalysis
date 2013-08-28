using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.PacketsAnalyze.HTTP
{
    public static class Parses
    {
        public static int ParseHex(Byte[] data, int index, out int value)
        {
            value = 0;
            int pos = index;

            if (!IsHexDigit(data[pos]))
            {
                // Don't start with a digit, return
                return 0;
            }

            // Find the end of the number
            while (IsHexDigit(data[pos])) // data[pos]  match  [0-9a-fA-F]
                pos++;

            // record the width
            int width = pos - index;

            // back to the last byte belongs to the number
            pos--;

            int factor = 1;
            while (pos >= index)
            {
                int digit = 0;
                if (data[pos] >= 0x61)
                    digit = data[pos] - 0x61 + 10; // data[i] - 'a'
                else if (data[pos] >= 0x41)
                    digit = data[pos] - 0x41 + 10; // data[i] - 'A'
                else
                    digit = data[pos] - 0x30; // data[i] - '0'

                value += factor * digit;
                factor *= 16;
                pos--;
            }

            return width;
        }


        private static bool IsHexDigit(Byte ch)
        {
            return (ch >= 0x30 && ch <= 0x39)  // '0' <= ch <= '9'
                || (ch >= 0x41 && ch <= 0x46)  // 'A' <= ch <= 'F'
                || (ch >= 0x61 && ch <= 0x66); // 'a' <= ch <= 'f'
        }
    }
}
