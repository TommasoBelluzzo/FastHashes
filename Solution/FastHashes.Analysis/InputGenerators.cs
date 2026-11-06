#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Analysis
{
   /// <summary>Small deterministic input-pair families for targeted structural stress analysis.</summary>
    internal static class StructuralInputGenerator
    {
        #region Methods
        public static IEnumerable<StructuralPair> Sparse(Int32 keyLength)
        {
            Int32 bits = checked(keyLength * 8);
            for (Int32 bit = 0; bit < bits; ++bit)
            {
                Byte[] a = new Byte[keyLength];
                Byte[] b = new Byte[keyLength];
                b[bit / 8] = (Byte)(1 << (bit % 8));
                yield return new StructuralPair("sparse", $"single-bit:{bit}", a, b);
            }

            // Two distant set bits; bounded to a linear number of probes.
            for (Int32 bit = 0; bit < bits; ++bit)
            {
                Int32 other = (bit + Math.Max(1, bits / 2)) % bits;
                if (other <= bit)
                    continue;
                Byte[] a = new Byte[keyLength];
                Byte[] b = new Byte[keyLength];
                b[bit / 8] |= (Byte)(1 << (bit % 8));
                b[other / 8] |= (Byte)(1 << (other % 8));
                yield return new StructuralPair("sparse", $"two-bits:{bit}:{other}", a, b);
            }
        }

        public static IEnumerable<StructuralPair> TwoBytes(Int32 keyLength)
        {
            if (keyLength < 2)
                yield break;

            SortedSet<Int32> positions = [0, 1, keyLength / 2, keyLength - 2, keyLength - 1];
            Byte[] values = [1, 0x80, 0xFF];
            foreach (Int32 p in positions)
            {
                foreach (Int32 q in positions)
                {
                    if (q <= p)
                        continue;

                    foreach (Byte value in values)
                    {
                        Byte[] a = new Byte[keyLength];
                        a[p] = value;
                        a[q] = value;

                        Byte[] first = (Byte[])a.Clone();
                        first[p] ^= 0xFF;
                        yield return new StructuralPair("twobytes", $"positions:{p}:{q}:value:{value:X2}:first", a, first);

                        Byte[] second = (Byte[])a.Clone();
                        second[q] ^= 0xFF;
                        yield return new StructuralPair("twobytes", $"positions:{p}:{q}:value:{value:X2}:second", a, second);

                        Byte[] both = (Byte[])a.Clone();
                        both[p] ^= 0xFF;
                        both[q] ^= 0xFF;
                        yield return new StructuralPair("twobytes", $"positions:{p}:{q}:value:{value:X2}:both", a, both);
                    }
                }
            }
        }

        public static IEnumerable<StructuralPair> Cyclic(Int32 keyLength)
        {
            Byte[][] patterns = [[0x00, 0xFF], [0xAA, 0x55], [0x01, 0x02, 0x03], [0x00, 0x01, 0x00, 0xFF], [0x10, 0x20, 0x30, 0x40, 0x50]];
            foreach (Byte[] pattern in patterns)
            {
                if (pattern.Length > keyLength)
                    continue;

                Byte[] a = Repeat(pattern, keyLength, 0);
                for (Int32 phase = 1; phase < Math.Min(pattern.Length, 3); ++phase)
                {
                    Byte[] shifted = Repeat(pattern, keyLength, phase);
                    yield return new StructuralPair("cyclic", $"period:{pattern.Length}:phase:{phase}", a, shifted);
                }

                Byte[] shorter = new Byte[pattern.Length - 1];
                Array.Copy(pattern, shorter, shorter.Length);
                Byte[] changedPeriod = Repeat(shorter, keyLength, 0);
                yield return new StructuralPair("cyclic", $"period:{pattern.Length}:shortened:{shorter.Length}", a, changedPeriod);
            }
        }

        public static IEnumerable<StructuralPair> Permutation(Int32 keyLength)
        {
            if (keyLength < 2)
                yield break;
            Int32 width = Math.Min(8, keyLength);
            Byte[] baseline = new Byte[keyLength];
            for (Int32 i = 0; i < width; ++i)
                baseline[i] = (Byte)(i + 1);
            for (Int32 i = 0; i < width; ++i)
            {
                for (Int32 j = i + 1; j < width; ++j)
                {
                    Byte[] altered = (Byte[])baseline.Clone();
                    (altered[i], altered[j]) = (altered[j], altered[i]);
                    yield return new StructuralPair("permutation", $"swap:{i}:{j}", baseline, altered);
                }
            }
        }

        public static IEnumerable<StructuralPair> Window(Int32 keyLength)
        {
            if (keyLength < 2)
                yield break;
            for (Int32 offset = 0; offset <= keyLength - 2; ++offset)
            {
                foreach (UInt16 value in new UInt16[] { 1, 0x0100, 0x8001, 0xFFFF })
                {
                    Byte[] a = new Byte[keyLength];
                    Byte[] b = new Byte[keyLength];
                    b[offset] = (Byte)(value & 0xFF);
                    b[offset + 1] = (Byte)(value >> 8);
                    yield return new StructuralPair("window", $"offset:{offset}:value:{value:X4}", a, b);
                }
            }
        }

        public static IEnumerable<StructuralPair> Zeroes(Int32 maximumLength)
        {
            foreach (Int32 length in BoundaryLengths(maximumLength))
            {
                if (length == 0)
                    continue;
                Byte[] a = new Byte[length - 1];
                Byte[] b = new Byte[length];
                yield return new StructuralPair("zeroes", $"zero-lengths:{length - 1}:{length}", a, b);
            }
        }

        public static IEnumerable<StructuralPair> Boundaries(Int32 maximumLength)
        {
            foreach (Int32 length in BoundaryLengths(maximumLength))
            {
                if (length == 0)
                    continue;
                Byte[] a = new Byte[length - 1];
                Byte[] b = new Byte[length];
                // Length is the only difference; prefix remains identical.
                Array.Fill(a, (Byte)0xA5);
                Array.Fill(b, (Byte)0xA5);
                yield return new StructuralPair("boundaries", $"lengths:{length - 1}:{length}:A5", a, b);
            }
        }

        public static IEnumerable<StructuralPair> SeedProbes(Int32 keyLength)
        {
            Int32 length = Math.Max(1, keyLength);
            Byte[] zero = new Byte[length];
            Byte[] tail = new Byte[length];
            tail[^1] = 1;
            yield return new StructuralPair("seed", "zero-vs-last-bit", zero, tail);
            Byte[] uniform = new Byte[length];
            Byte[] changed = new Byte[length];
            Array.Fill(uniform, (Byte)0xA5);
            Array.Fill(changed, (Byte)0xA5);
            changed[0] ^= 1;
            yield return new StructuralPair("seed", "pattern-vs-first-bit", uniform, changed);
        }

        private static IEnumerable<Int32> BoundaryLengths(Int32 maximumLength)
        {
            Int32[] thresholds = [0, 1, 2, 3, 4, 8, 16, 32, 64, 128, 256, 512];
            SortedSet<Int32> lengths = [];
            foreach (Int32 threshold in thresholds)
            {
                for (Int32 delta = -1; delta <= 1; ++delta)
                {
                    Int32 length = threshold + delta;
                    if ((length >= 0) && (length <= maximumLength))
                        lengths.Add(length);
                }
            }
            foreach (Int32 length in lengths)
                yield return length;
        }

        private static Byte[] Repeat(Byte[] pattern, Int32 length, Int32 phase)
        {
            Byte[] result = new Byte[length];
            for (Int32 i = 0; i < length; ++i)
                result[i] = pattern[(i + phase) % pattern.Length];
            return result;
        }

        #endregion
    }

    internal sealed record StructuralPair(String Family, String Case, Byte[] InputA, Byte[] InputB);
}
