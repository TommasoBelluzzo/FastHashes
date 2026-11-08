#region Using Directives
using System;
#endregion

namespace FastHashes.Analysis
{
    internal static class MathUtilities
    {
        #region Methods
        public static Double[] BinomialTailProbabilities(Int32 samples)
        {
            Double[] logFactorials = new Double[samples + 1];

            for (Int32 i = 2; i <= samples; ++i)
                logFactorials[i] = logFactorials[i - 1] + Math.Log(i);

            Int32 middle = samples / 2;

            Double[] masses = new Double[middle + 1];
            masses[middle] = Math.Exp(logFactorials[samples] - logFactorials[middle] - logFactorials[samples - middle] - samples * Math.Log(2.0d));

            for (Int32 k = middle; k > 0; --k)
                masses[k - 1] = masses[k] * k / (samples - k + 1.0d);

            Double cumulative = 0.0d;
            Double[] tails = new Double[middle + 1];

            for (Int32 k = 0; k <= middle; ++k)
            {
                cumulative += masses[k];
                tails[k] = Math.Min(1.0d, 2.0d * cumulative);
            }

            return tails;
        }

        public static Double ChiSquare(Int32[] counts, Int64 total)
        {
            Double expected = (Double)total / counts.Length;
            Double statistic = 0.0d;

            foreach (Int32 count in counts)
            {
                Double difference = count - expected;
                statistic += difference * difference / expected;
            }

            return statistic;
        }

        public static Double Entropy(Int32[] counts, Int64 total)
        {
            Double entropy = 0.0d;

            foreach (Int32 count in counts)
            {
                if (count == 0)
                    continue;

                Double probability = (Double)count / total;
                entropy -= probability * Math.Log2(probability);
            }

            return entropy;
        }

        public static Double Erfc(Double x)
        {
            const Double TWO_OVER_SQRT_PI = 1.1283791670955126d;
            const Double INV_SQRT_PI = 0.5641895835477563d;

            if (Double.IsNaN(x))
                return Double.NaN;

            if (Double.IsPositiveInfinity(x))
                return 0.0d;

            if (Double.IsNegativeInfinity(x))
                return 2.0d;

            if (x == 0.0d)
                return 1.0d;

            if (x < 0.0d)
                return 2.0d - Erfc(-x);

            if (x < 2.0d)
            {
                Double term = x;
                Double sum = x;

                for (Int32 n = 0; n < 100; ++n)
                {
                    term *= 2.0d * x * x / (2 * n + 3);
                    sum += term;

                    if (term <= sum * 1.0e-16d)
                        break;
                }

                return Math.Max(0.0d, 1.0d - TWO_OVER_SQRT_PI * Math.Exp(-x * x) * sum);
            }

            Int32 terms = x < 3.0d ? 64 : x < 4.0d ? 32 : x < 5.0d ? 24 : 16;
            Double fraction = 0.0d;

            for (Int32 n = terms; n >= 1; --n)
                fraction = n / (((n % 2) == 1 ? 2.0d * x : x) + fraction);

            return Math.Max(0.0d, Math.Min(1.0d, Math.Exp(-x * x) * INV_SQRT_PI / (x + fraction)));
        }

        public static Double ExpectedCollisionPairs(Double count, Int32 bits)
        {
            return count * (count - 1.0d) * Math.Pow(2.0d, -bits - 1.0d);
        }

        public static Double ExpectedCollisionDuplicates(Double count, Int32 bits)
        {
            Double space = Math.Pow(2.0d, bits);

            if (count / space < 1.0e-6d)
                return ExpectedCollisionPairs(count, bits);

            Double probability = 1.0d / space;

            Double log = probability < 1.0e-4d
                ? -probability * (1.0d + probability * (0.5d + probability / 3.0d))
                : Math.Log(1.0d - probability);

            Double value = count * log;

            Double occupiedFraction = Math.Abs(value) < 1.0e-4d
                ? -value * (1.0d + value * (0.5d + value * (1.0d / 6.0d + value / 24.0d)))
                : 1.0d - Math.Exp(value);

            return Math.Max(0.0d, count - (space * occupiedFraction));
        }

        public static Double PhiCoefficient(Int32 n00, Int32 n01, Int32 n10, Int32 n11)
        {
            Double a = (Double)(n10 + n11) * (n00 + n01);
            Double b = (Double)(n01 + n11) * (n00 + n10);

            if ((a == 0.0d) || (b == 0.0d))
                return Double.NaN;

            Double phi = ((Double)n11 * n00 - (Double)n10 * n01) / Math.Sqrt(a * b);

            return phi;
        }
        #endregion
    }
}