#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class StatsUtilities
    {
        #region Constants
        private const Int32 MAXIMUM_LENGTH = 20;
        #endregion

        #region Methods
        public static AnalysisResult AnalyzeHashes(List<Byte[]> hashes, Int32 hashBytes)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));

            Int32 hashBits = hashBytes * 8;

            Int32 hashesCount = hashes.Count;
            Double hashesCountFloat = hashesCount;

            hashes.Sort(NativeMethods.CompareSequences);

            Double expectedCollisions = Math.Round((hashesCountFloat * (hashesCountFloat - 1.0d)) / Math.Pow(2.0d, hashBits + 1));
            Double observedCollisions = 0.0d;
            Boolean result = true;

            for (Int32 i = 1; i < hashesCount; ++i)
            {
                if (NativeMethods.EqualSequences(hashes[i], hashes[i - 1]))
                    ++observedCollisions;
            }

            if (hashBits <= 32)
            {
                if (((observedCollisions / expectedCollisions) > 2.0d) && (Math.Abs(observedCollisions - expectedCollisions) > 1.0d))
                    result = false;
            }
            else if (observedCollisions > 0.0d)
                result = false;

            Int32 maximumLength = MAXIMUM_LENGTH;

            while((hashesCountFloat / (1 << maximumLength)) < 5.0d)
                --maximumLength;

            Int32[] bins = new Int32[1 << maximumLength];

            Double worstBias = 0.0d;
            Int32 worstBit = -1;
            Int32 worstWindow = -1;

            for (Int32 start = 0; start < hashBits; ++start)
            {
                Int32 length = maximumLength;
                Int32 binsCount = (1 << length);

                for (Int32 i = 0; i < binsCount; ++i)
                    bins[i] = 0;

                for (Int32 i = 0; i < hashesCount; ++i)
                {
                    Byte[] hash = hashes[i];
                    Int32 index = (Int32)BitsUtilities.Window(hash, start, length);

                    ++bins[index];
                }

                while (binsCount >= 256)
                {
                    Double r = 0.0d;

                    for (Int32 i = 0; i < binsCount; ++i)
                        r += Math.Pow(bins[i], 2.0d);

                    r = Math.Sqrt(r / binsCount);

                    Double f = (Math.Pow(hashesCount, 2.0d) - 1.0d) / ((binsCount * Math.Pow(r, 2.0d)) - hashesCount);
                    Double bias = (1.0d - (f / binsCount)) * 100.0d;

                    if (bias > worstBias)
                    {
                        worstBias = bias;
                        worstBit = start;
                        worstWindow = length;
                    }

                    --length;
                    binsCount /= 2;

                    if (length < 8)
                        break;

                    for (Int32 i = 0; i < binsCount; ++i)
                        bins[i] += bins[binsCount + i];
                }
            }

            return new AnalysisResult
            {
                Outcome = result,
                HashesCount = hashesCount,
                ExpectedCollisions = expectedCollisions,
                ObservedCollisions = observedCollisions,
                WorstBias = worstBias,
                WorstBit = worstBit,
                WorstWindow = worstWindow
            };
        }

        public static Double ChooseK(Int32 n, Int32 k)
        {
            if (k > (n - k))
                k = n - k;

            Double c = 1.0d;

            for (Int32 i = 0; i < k; ++i)
                c *= (Double)(n - i) / (i + 1);

            return c;
        }

        public static Double ChooseUpToK(Int32 n, Int32 k)
        {
            Double c = 0.0d;

            for (Int32 i = 1; i <= k; ++i)
                c += ChooseK(n, i);

            return c;
        }

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
        #endregion
    }
}