#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#endregion

namespace FastHashes.Tests
{
    public static class Utilities
    {
        #region Methods
        private static Boolean EqualSequences(Byte[] array1, Byte[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;

            if ((array1 == null) || (array2 == null) || (array1.Length != array2.Length))
                return false;

            Int32 n = array1.Length;

            unsafe
            {
                fixed (Byte* pin1 = array1, pin2 = array2)
                {
                    Byte* pointer1 = pin1;
                    Byte* pointer2 = pin2;

                    while (n-- > 0)
                    {
                        if (*pointer1 != *pointer2)
                            return false;

                        ++pointer1;
                        ++pointer2;
                    }

                    return true;
                }  
            }
        }

        private static Int32 CompareSequences(Byte[] array1, Byte[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return 0;

            if (array1 == null)
                return 1;

            if (array2 == null)
                return -1;

            if (array1.Length != array2.Length)
                return -Math.Min(Math.Max(array1.Length - array2.Length, -1), 1);

            Int32 n = array1.Length;

            unsafe
            {
                fixed (Byte* pin1 = array1, pin2 = array2)
                {
                    Byte* pointer1 = pin1;
                    Byte* pointer2 = pin2;

                    Int32 diff = 0;

                    while (n-- > 0)
                    {
                        if (*pointer1 != *pointer2)
                        {
                            diff = *pointer1 - *pointer2;
                            break;
                        }

                        ++pointer1;
                        ++pointer2;
                    }

                    return -Math.Min(Math.Max(diff, -1), 1);
                }  
            }
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
                if (EqualSequences(hashes[i], hashes[i - 1]))
                    ++actualCollisions;
            }

            if ((hashBits <= 32) && ((actualCollisions / expectedCollisions) > 2.0d) && (Math.Abs(actualCollisions - expectedCollisions) > 1.0d))
                return true;

            if ((hashBits > 32) && (actualCollisions > 0.0d))
                return true;

            return false;
        }

        public static String GetStaticFilePath(String fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Invalid file name specified.", nameof(fileName));

            Uri codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            String codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            String dataPath = Path.Combine(Path.GetDirectoryName(codeBasePath), "Data");

            String filePath = Path.Combine(dataPath, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file could not be found.", filePath);

            return filePath;
        }

        public static void FillBuffer(Byte[] buffer, Byte filler)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Length == 0)
                return;

            unsafe
            {
                fixed (Byte* pin = buffer)
                {
                    Byte* pointer = pin;

                    for (Int32 i = 0; i < buffer.Length; ++i)
                        *(pointer + i) = filler;
                }
            }
        }
        #endregion
    }
}