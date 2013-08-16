﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay.Charts;
using System.Diagnostics;
using System;

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
                dic[key] = default(long);
            }
            dic[key]++;
        }
    }

    public static class DictionaryExtensions
    {
        /// <summary>
        /// Merge two dictionary.
        /// Entry's value will be add if it exists in both dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Dictionary<TKey, long> Merge<TKey>(this Dictionary<TKey, long> lhs, Dictionary<TKey, long> rhs)
        {
            Dictionary<TKey, long> res = new Dictionary<TKey, long>(lhs);

            foreach (var key in rhs.Keys)
            {
                if (!res.ContainsKey(key))
                {
                    res[key] = default(long);
                }

                res[key] += rhs[key];
            }
            return res;
        }

        

        /// <summary>
        /// Return a new dictionary.
        /// An entry will be passed to result as is if it only exists in lhs,
        /// A key present both in lhs and rhs will have an entry in result, its value
        /// is lhs[key] - rhs[key].
        /// Entries only exists in rhs won't effect the result.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Dictionary<TKey, long> Difference<TKey>(this Dictionary<TKey, long> lhs, Dictionary<TKey, long> rhs)
        {
            Dictionary<TKey, long> res = new Dictionary<TKey, long>();

            foreach (var key in lhs.Keys)
            {
                if (!rhs.ContainsKey(key))
                {
                    res[key] = lhs[key];
                }
                else
                {
                    res[key] = lhs[key] - rhs[key];
                }

                if (res[key] == 0)
                {
                    res.Remove(key);
                }
            }

            return res;
        }
    }

    public static class MathHelper
    {
        public static long Clamp(long value, long min, long max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        /// <summary>Clamps specified value to [0,1]</summary>
        /// <param name="d">Value to clamp</param>
        /// <returns>Value in range [0,1]</returns>
        public static double Clamp(double value)
        {
            return Math.Max(0, Math.Min(value, 1));
        }

        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static Rect CreateRectByPoints(double xMin, double yMin, double xMax, double yMax)
        {
            return new Rect(new Point(xMin, yMin), new Point(xMax, yMax));
        }

        public static double Interpolate(double start, double end, double ratio)
        {
            return start * (1 - ratio) + end * ratio;
        }

        public static double RadiansToDegrees(this double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double DegreesToRadians(this double degrees)
        {
            return degrees / 180 * Math.PI;
        }

        /// <summary>
        /// Converts vector into angle.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <returns>Angle in degrees.</returns>
        public static double ToAngle(this Vector vector)
        {
            return Math.Atan2(-vector.Y, vector.X).RadiansToDegrees();
        }

        public static Point ToPoint(this Vector v)
        {
            return new Point(v.X, v.Y);
        }

        public static bool IsNaN(this double d)
        {
            return Double.IsNaN(d);
        }

        public static bool IsNotNaN(this double d)
        {
            return !Double.IsNaN(d);
        }

        public static bool IsFinite(this double d)
        {
            return !Double.IsNaN(d) && !Double.IsInfinity(d);
        }

        public static bool IsInfinite(this double d)
        {
            return Double.IsInfinity(d);
        }

        public static bool AreClose(double d1, double d2, double diffRatio)
        {
            return Math.Abs(d1 / d2 - 1) < diffRatio;
        }
    }

    public static class DoubleCollectionExtension
    {
        public static DoubleCollection Create(params double[] collection)
        {
            return new DoubleCollection(collection);
        }
    }

    public static class PlacementExtensions
    {
        public static bool IsBottomOrTop(this AxisPlacement placement)
        {
            return placement == AxisPlacement.Bottom || placement == AxisPlacement.Top;
        }
    }

    public static class RoundingHelper
    {
        public static int GetDifferenceLog(double min, double max)
        {
            return (int)Math.Round(Math.Log10(Math.Abs(max - min)));
        }

        public static double Round(double number, int rem)
        {
            if (rem <= 0)
            {
                rem = MathHelper.Clamp(-rem, 0, 15);
                return Math.Round(number, rem);
            }
            else
            {
                double pow = Math.Pow(10, rem - 1);
                double val = pow * Math.Round(number / Math.Pow(10, rem - 1));
                return val;
            }
        }

        public static double Round(double value, Range<double> range)
        {
            int log = GetDifferenceLog(range.Min, range.Max);

            return Round(value, log);
        }

        public static RoundingInfo CreateRoundedRange(double min, double max)
        {
            double delta = max - min;

            if (delta == 0)
                return new RoundingInfo { Min = min, Max = max, Log = 0 };

            int log = (int)Math.Round(Math.Log10(Math.Abs(delta))) + 1;

            double newMin = Round(min, log);
            double newMax = Round(max, log);
            if (newMin == newMax)
            {
                log--;
                newMin = Round(min, log);
                newMax = Round(max, log);
            }

            return new RoundingInfo { Min = newMin, Max = newMax, Log = log };
        }
    }

    [DebuggerDisplay("{Min} - {Max}, Log = {Log}")]
    public sealed class RoundingInfo
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public int Log { get; set; }
    }
}