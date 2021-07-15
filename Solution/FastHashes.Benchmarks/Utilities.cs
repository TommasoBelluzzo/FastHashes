#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public static class Utilities
    {
        #region Members
        private static readonly String[] s_SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        #endregion

        #region Methods
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
