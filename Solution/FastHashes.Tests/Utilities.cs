#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class Utilities
    {
        #region Methods
        private static Boolean SequencesEqual(Byte[] array1, Byte[] array2)
        {
            if (ReferenceEquals(array1, null))
                return ReferenceEquals(array2, null);

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

        private static Int32 CompareSequences(Byte[] array1, Byte[] array2)
        {
            if (ReferenceEquals(array1, null))
                return ReferenceEquals(array2, null) ? 0 : 1;

            if (ReferenceEquals(array2, null))
                return ReferenceEquals(array1, null) ? 0 : -1;

            if (ReferenceEquals(array1, array2))
                return 0;

            Int32 length1 = array1.Length;
            Int32 length2 = array2.Length;

            if (length1 != length2)
                return -Math.Min(Math.Max(length1 - length2, -1), 1);

            Int32 n = Math.Max(length1, length2);
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

            return -Math.Min(Math.Max(diff, -1), 1);
        }

        public static Boolean CollisionsThresholdExceeded(List<Byte[]> hashes, Int32 hashBytes)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));

            Int32 hashesCount = hashes.Count;
            Double hashesCountFloat = hashesCount;
            hashes.Sort(CompareSequences);

            Int32 hashBits = hashBytes * 8;

            Double expectedCollisions = Math.Round((hashesCountFloat * (hashesCountFloat - 1.0d)) / Math.Pow(2.0d, hashBits + 1));
            Double actualCollisions = 0.0d;

            for (Int32 i = 1; i < hashesCount; ++i)
            {
                if (SequencesEqual(hashes[i], hashes[i - 1]))
                    ++actualCollisions;
            }

            if ((hashBits <= 32) && ((actualCollisions / expectedCollisions) > 2.0d) && (Math.Abs(actualCollisions - expectedCollisions) > 1.0d))
                return true;

            if ((hashBits > 32) && (actualCollisions > 0.0d))
                return true;

            return false;
        }
        #endregion
    }
}