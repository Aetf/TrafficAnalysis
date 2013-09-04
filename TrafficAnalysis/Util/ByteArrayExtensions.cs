using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficAnalysis.Util
{
    public static class ByteArrayExtensions
    {
        #region Boyer Moore Search

        const int ALPHABET_SIZE = 256;

        /// <summary>
        /// Returns the index within this byte array of the first occurrence of the
        /// specified pattern. If not found, return -1.
        /// </summary>
        /// <param name="self">The byte array to be scanned</param>
        /// <param name="pattern">The target pattern to search</param>
        /// <returns>The start index of the sub array</returns>
        public static int BMIndexOf(this Byte[] self, Byte[] pattern)
        {
            if (pattern.Length == 0)
                return 0;

            int[] charTable = MakeCharTable(pattern);
            int[] offsetTable = MakeOffsetTable(pattern);
            for (int i = pattern.Length - 1, j; i < self.Length; )
            {
                for (j = pattern.Length - 1; pattern[j] == self[i]; --i, --j)
                {
                    if (j == 0)
                    {
                        return i;
                    }
                }
                // i += needle.Length - j; // For naive method
                i += Math.Max(offsetTable[pattern.Length - 1 - j], charTable[self[i]]);
            }
            return -1;
        }
        
        /// <summary>
        /// Makes the jump table based on the mismatched character information.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static int[] MakeCharTable(Byte[] pattern)
        {
            int[] table = new int[ALPHABET_SIZE];
            for (int i = 0; i < table.Length; ++i)
            {
                table[i] = pattern.Length;
            }
            for (int i = 0; i < pattern.Length - 1; ++i)
            {
                table[pattern[i]] = pattern.Length - 1 - i;
            }
            return table;
        }

        /// <summary>
        /// Makes the jump table based on the scan offset which mismatch occurs.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static int[] MakeOffsetTable(Byte[] pattern)
        {
            int[] table = new int[pattern.Length];
            int lastPrefixPosition = pattern.Length;
            for (int i = pattern.Length - 1; i >= 0; --i)
            {
                if (IsPrefix(pattern, i + 1))
                {
                    lastPrefixPosition = i + 1;
                }
                table[pattern.Length - 1 - i] = lastPrefixPosition - i + pattern.Length - 1;
            }
            for (int i = 0; i < pattern.Length - 1; ++i)
            {
                int slen = SuffixLength(pattern, i);
                table[slen] = pattern.Length - 1 - i + slen;
            }
            return table;
        }

        /// <summary>
        /// Is pattern[p:end] a prefix of pattern?
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static bool IsPrefix(Byte[] pattern, int p)
        {
            for (int i = p, j = 0; i < pattern.Length; ++i, ++j)
            {
                if (pattern[i] != pattern[j])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the maximum length of the substring ends at p and is a suffix.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static int SuffixLength(Byte[] pattern, int p)
        {
            int len = 0;
            for (int i = p, j = pattern.Length - 1;
                 i >= 0 && pattern[i] == pattern[j]; --i, --j)
            {
                len += 1;
            }
            return len;
        }

        /// <summary>
        /// Simplified version of the Boyer-Moore algorithm.
        /// Only uses the second jump table of the full algorithm
        /// </summary>
        /// <param name="self"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static int SimpleBoyerMooreSearch(this byte[] self, byte[] pattern)
        {
            int[] lookup = new int[256];
            for (int i = 0; i < lookup.Length; i++) { lookup[i] = pattern.Length; }

            for (int i = 0; i < pattern.Length; i++)
            {
                lookup[pattern[i]] = pattern.Length - i - 1;
            }

            int index = pattern.Length - 1;
            var lastByte = pattern.Last();
            while (index < self.Length)
            {
                var checkByte = self[index];
                if (self[index] == lastByte)
                {
                    bool found = true;
                    for (int j = pattern.Length - 2; j >= 0; j--)
                    {
                        if (self[index - pattern.Length + j + 1] != pattern[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                        return index - pattern.Length + 1;
                    else
                        index++;
                }
                else
                {
                    index += lookup[checkByte];
                }
            }
            return -1;
        }
        #endregion

        #region Byte by Byte search
        static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }
        #endregion
    }
}
