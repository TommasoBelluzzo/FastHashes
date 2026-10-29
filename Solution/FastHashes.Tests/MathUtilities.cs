#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class MathUtilities
    {
        #region Methods
        private static Boolean SequencesEqual(Byte[] array1, Byte[] array2)
        {
            if (array1 is null)
                return array2 is null;

            if (ReferenceEquals(array1, array2))
                return true;

            Int32 length1 = array1.Length;
            Int32 length2 = array2.Length;

            if (length1 != length2)
                return false;

            Int32 n = Math.Max(length1, length2);

            for (Int32 i = 0; i < n; ++i)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }

        private static Double ActualCollisions(List<Byte[]> hashes)
        {
            hashes.Sort(CompareSequences);

            Double actualCollisions = 0.0d;

            for (Int32 i = 1; i < hashes.Count; ++i)
            {
                if (SequencesEqual(hashes[i], hashes[i - 1]))
                    ++actualCollisions;
            }

            return actualCollisions;
        }

        private static Double ExpectedCollisions(Double hashesCount, Double hashBits)
        {
            Double expectedCollisions;

            if ((hashBits - (2.0d * Math.Log(hashesCount, 2.0d))) >= 7)
                expectedCollisions = hashesCount * (hashesCount - 1.0d) * Math.Pow(2.0d, -hashBits - 1.0d);
            else
            {
                Double e2b = Math.Pow(2.0d, -hashBits);
                Double lpe = hashesCount * Log1p(-e2b);

                expectedCollisions = Math.Pow(2.0d, hashBits) * ((e2b * hashesCount) + (Math.Exp(lpe) - 1.0d));
            }

            expectedCollisions = Math.Round(expectedCollisions, 4);

            return expectedCollisions;
        }

        private static Double Log1p(Double x)
        {
            if (Math.Abs(x) > 1e-4)
                return Math.Log(1.0d + x);

            return ((-0.5d * x) + 1.0d) * x;
        }

        private static Int32 CompareSequences(Byte[] array1, Byte[] array2)
        {
            if (array1 is null)
                return array2 is null ? 0 : 1;

            if (array2 is null)
                return array1 is null ? 0 : -1;

            if (ReferenceEquals(array1, array2))
                return 0;

            Int32 length1 = array1.Length;
            Int32 length2 = array2.Length;

            if (length1 != length2)
                return -Math.Min(Math.Max(length1 - length2, -1), 1);

            Int32 n = Math.Min(length1, length2);
            Int32 diff = 0;

            for (Int32 i = 0; i < n; ++i)
            {
                Byte value1 = array1[i];
                Byte value2 = array2[i];

                if (value1 != value2)
                {
                    diff = value1 - value2;
                    break;
                }
            }

            Int32 result = -Math.Min(Math.Max(diff, -1), 1);

            return result;
        }

        public static Boolean CollisionsThresholdExceeded(List<Byte[]> hashes, Int32 hashBytes)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));

            Int32 hashBits = hashBytes * 8;
            Double expectedCollisions = ExpectedCollisions(hashes.Count, hashBits);
            Double actualCollisions = ActualCollisions(hashes);

            if (hashBits <= 32)
            {
                Double ratio = actualCollisions / expectedCollisions;

                if ((expectedCollisions >= 0.1d) && (expectedCollisions <= 10.0d))
                {
                    ratio = Math.Ceiling(ratio);

                    if (ratio > 2.0d)
                        return true;

                    return false;
                }

                if ((expectedCollisions < 0.001d) && (actualCollisions >= 1.0d))
                    return true;

                return false;
            }

            if ((expectedCollisions < 1.0d) && (actualCollisions > 0.0d))
                return true;

            return false;
        }
        #endregion
    }
}
