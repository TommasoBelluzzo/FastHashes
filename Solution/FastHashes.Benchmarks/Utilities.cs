#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public static class Utilities
    {
        #region Members
        private static readonly String[] s_SizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
        #endregion

        #region Methods
        public static String FormatSpeed(Double speed)
        {
            if (!Double.IsFinite(speed))
                throw new ArgumentException("Invalid speed specified.", nameof(speed));

            if (speed < 0.0d)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be non-negative.");

            Int32 magnitude = 0;
            Double adjustedSpeed = speed;

            while ((adjustedSpeed >= 1000.0d) && (magnitude < (s_SizeSuffixes.Length - 1)))
            {
                adjustedSpeed /= 1000.0d;
                ++magnitude;
            }

            return $"{adjustedSpeed:N2} {s_SizeSuffixes[magnitude]}/s";
        }
        #endregion
    }
}
