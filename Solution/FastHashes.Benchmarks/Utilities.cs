#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Benchmarks
{
    public static class Utilities
    {
        #region Members
        private static readonly String[] s_SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        #endregion

        #region Methods
        public static Double Mean(IList<Double> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            Int32 length = values.Count;

            if (length == 0)
                return Double.NaN;

            Double mean = 0.0d;
  
            for (Int32 i = 0; i < length; ++i)
                mean += values[i];
  
            mean /= length;

            return mean;
        }

        public static Double StandardDeviation(IList<Double> values, Double mean)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            Int32 length = values.Count;

            if (length == 0)
                return Double.NaN;

            Double sd = 0.0d;
  
            for (Int32 i = 0; i < length; ++i)
                sd += Math.Pow(values[i] - mean, 2.0d);
  
            sd = Math.Sqrt(sd / length);

            return sd;
        }
        
        public static String FormatSpeed(Double speed)
        {
            Int32 magnitude = (Int32)Math.Log(speed, 1024);
            Double adjustedSpeed = speed / (1L << (magnitude * 10));

            if (Math.Round(adjustedSpeed, 2) >= 1000.0d)
            {
                magnitude += 1;
                adjustedSpeed /= 1024.0d;
            }

            return $"{adjustedSpeed:N2} {s_SizeSuffixes[magnitude]}/s";
        }
        #endregion
    }
}
