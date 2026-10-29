#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Benchmarks
{
    public static class MathUtilities
    {
        #region Methods
        public static Double Mean(IList<Double> values, Boolean filter = true)
        {
            ArgumentNullException.ThrowIfNull(values, nameof(values));

            Int32 length = values.Count;

            if (length == 0)
                return Double.NaN;

            if (filter && (length >= 5))
            {
                Double mean = Mean(values, false);
                Double sd = StandardDeviation(values);

                if (sd > 0.0d)
                {
                    Double threshold = 2.0d * sd;
                    
                    List<Double> filtered = new(length);

                    for (Int32 i = 0; i < length; ++i)
                    {
                        if (Math.Abs(values[i] - mean) <= threshold)
                            filtered.Add(values[i]);
                    }

                    if (filtered.Count > 0)
                        return Mean(filtered, false);
                }
            }

            Double result = 0.0d;

            for (Int32 i = 0; i < length; ++i)
                result += values[i];

            return result / length;
        }

        public static Double Median(IList<Double> values, Boolean filter = true)
        {
            ArgumentNullException.ThrowIfNull(values, nameof(values));

            Int32 length = values.Count;

            if (length == 0)
                return Double.NaN;

            if (filter && (length >= 5))
            {
                Double median = Median(values, false);
                Double mad = MedianAbsoluteDeviation(values);

                if (mad > 0.0d)
                {
                    List<Double> filtered = new(length);

                    for (Int32 i = 0; i < length; ++i)
                    {
                        Double score = (0.6745d * Math.Abs(values[i] - median)) / mad;

                        if (score <= 3.5d)
                            filtered.Add(values[i]);
                    }

                    if (filtered.Count > 0)
                        return Median(filtered, false);
                }
            }

            Double[] sorted = new Double[length];

            for (Int32 i = 0; i < length; ++i)
                sorted[i] = values[i];

            Array.Sort(sorted);

            Int32 middle = length / 2;

            if ((length & 1) == 0)
                return (sorted[middle - 1] / 2.0d) + (sorted[middle] / 2.0d);

            return sorted[middle];
        }

        public static Double MedianAbsoluteDeviation(IList<Double> values)
        {
            ArgumentNullException.ThrowIfNull(values, nameof(values));

            Int32 length = values.Count;

            if (length == 0)
                return Double.NaN;

            Double median = Median(values, false);
            Double[] deviations = new Double[length];

            for (Int32 i = 0; i < length; ++i)
                deviations[i] = Math.Abs(values[i] - median);

            return Median(deviations, false);
        }

        public static Double StandardDeviation(IList<Double> values)
        {
            ArgumentNullException.ThrowIfNull(values, nameof(values));

            Int32 length = values.Count;

            if (length == 0)
                return Double.NaN;

            Double mean = Mean(values, false);
            Double sd = 0.0d;

            for (Int32 i = 0; i < length; ++i)
                sd += Math.Pow(values[i] - mean, 2.0d);

            return Math.Sqrt(sd / length);
        }
        #endregion
    }
}