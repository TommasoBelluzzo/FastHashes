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
        #endregion
    }
}