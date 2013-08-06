using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public static void UpdateOrAdd(this Collection<NotifyKeyValuePairSL> coll, string key, long value)
        {
            try
            {
                coll.First(pair => pair.Key.Equals(key)).Value = value;
            }
            catch (System.InvalidOperationException)
            {
                coll.Add(new NotifyKeyValuePairSL(key, value));
            }
        }

        public static void Increment<TKey>(this Dictionary<TKey, long> dic, TKey key)
        {
            if (!dic.ContainsKey(key))
            {
                dic[key] = 0;
            }
            dic[key]++;
        }
    }
}
