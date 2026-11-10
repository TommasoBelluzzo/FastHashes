#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the HalftimeHash implementation. This class cannot be derived.</summary>
    public sealed class HalftimeHash : Hash<UInt64>
    {
        #region Members
        private static readonly UInt64[] s_DefaultEntropy = GenerateEntropy(0ul);

        private readonly HalftimeHashStyle m_Style;
        private readonly Int32 m_Lanes;
        private readonly UInt64[] m_Entropy;
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the internal processing style of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.HalftimeHashStyle"/>.</value>
        [ExcludeFromCodeCoverage]
        public HalftimeHashStyle Style => m_Style;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="style">The <see cref="T:FastHashes.HalftimeHashStyle"/> value specifying the internal processing style of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="style">style</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public HalftimeHash(HalftimeHashStyle style, UInt64 seed)
        {
            if (!Enum.IsDefined(typeof(HalftimeHashStyle), style))
                throw new ArgumentOutOfRangeException(nameof(style), "Invalid style specified.");

            m_Style = style;
            m_Lanes = 1 << (Int32)style;
            m_Entropy = (seed == 0) ? s_DefaultEntropy : GenerateEntropy(seed);
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.HalftimeHashStyle.S128"/> and a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public HalftimeHash() : this(HalftimeHashStyle.S128, 0ul) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="style">The <see cref="T:FastHashes.HalftimeHashStyle"/> value specifying the internal processing style of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="style">style</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public HalftimeHash(HalftimeHashStyle style) : this(style, 0ul) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.HalftimeHashStyle.S128"/> and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public HalftimeHash(UInt64 seed) : this(HalftimeHashStyle.S128, seed) { }
        #endregion

        #region Methods
        private static UInt64[] GenerateEntropy(UInt64 seed)
        {
            UInt64[] entropy = new UInt64[8866];

            UInt64 w = seed;
            UInt64 x = 0xECFC1357D65941AEul;
            UInt64 y = 0xBE1927F97B8C43F1ul;
            UInt64 z = 0xF4D4BEB14AE042BBul;

            for (Int32 i = 0; i < 8866; ++i)
            {
                UInt64 oldW = w;
                UInt64 oldX = x;
                UInt64 oldY = y;
                UInt64 oldZ = z;

                entropy[i] = oldX;

                w = unchecked(0xD3833E804F4C574Bul * oldZ);
                x = unchecked(oldZ + BinaryOperations.RotateLeft(oldW, 52));
                y = unchecked(oldY - oldX);
                z = BinaryOperations.RotateLeft(unchecked(oldY + oldW), 19);
            }

            return entropy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix(UInt64 accumulator, UInt64 value, UInt64 entropy)
        {
            UInt64 sum = unchecked((UInt32)value + (UInt32)entropy) | ((UInt64)unchecked((UInt32)(value >> 32) + (UInt32)(entropy >> 32)) << 32);
            UInt64 v = unchecked(accumulator + ((UInt64)(UInt32)sum * (sum >> 32)));

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt64 Tabulate(UInt64 value, Int32 offset)
        {
            UInt64 result = 0ul;

            for (Int32 i = 0; i < 8; ++i)
                result ^= m_Entropy[offset + (i * 256) + unchecked((Byte)(value >> (i * 8)))];

            return result;
        }

        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;

            Int32 blockBytes = m_Lanes * sizeof(UInt64);
            Int32 groupBytes = 18 * blockBytes;
            Int32 groups = count / groupBytes;

            Span<UInt64> stack = groups > 0
                ? stackalloc UInt64[144 * m_Lanes]
                : Span<UInt64>.Empty;

            Span<Int32> stackLengths = stackalloc Int32[9];
            stackLengths.Clear();

            Span<UInt64> accumulator = stackalloc UInt64[2 * m_Lanes];
            accumulator.Clear();

            Span<UInt64> scratch = stackalloc UInt64[21 * m_Lanes];
            Span<UInt64> hashes = stackalloc UInt64[7 * m_Lanes];

            for (Int32 group = 0; group < groups; ++group)
            {
                Int32 level = 0;

                while (stackLengths[level] == 8)
                    ++level;

                for (Int32 j = level - 1; j >= 0; --j)
                {
                    Int32 destination = 2 * (((j + 1) * 8) + stackLengths[j + 1]) * m_Lanes;
                    Int32 source = 16 * j * m_Lanes;
                    Int32 entropyOffset = 533 + (14 * j);

                    for (Int32 channel = 0; channel < 2; ++channel)
                    {
                        for (Int32 lane = 0; lane < m_Lanes; ++lane)
                        {
                            UInt64 value = stack[source + (channel * m_Lanes) + lane];

                            for (Int32 child = 1; child < 8; ++child)
                            {
                                UInt64 input = stack[source + (2 * child * m_Lanes) + (channel * m_Lanes) + lane];
                                UInt64 entropy = m_Entropy[entropyOffset + (channel * 7) + child - 1];
                                value = Mix(value, input, entropy);
                            }

                            stack[destination + (channel * m_Lanes) + lane] = value;
                        }
                    }

                    stackLengths[j] = 0;
                    ++stackLengths[j + 1];
                }

                Int32 groupOffset = group * groupBytes;

                for (Int32 i = 0; i < 18 * m_Lanes; ++i)
                    scratch[i] = BinaryOperations.Read64(buffer, groupOffset + (i * 8));

                for (Int32 i = 0; i < 3 * m_Lanes; ++i)
                {
                    UInt64 value = scratch[i];

                    for (Int32 row = 1; row < 6; ++row)
                        value ^= scratch[(3 * row * m_Lanes) + i];

                    scratch[(18 * m_Lanes) + i] = value;
                }

                for (Int32 row = 0; row < 7; ++row)
                {
                    for (Int32 lane = 0; lane < m_Lanes; ++lane)
                    {
                        UInt64 value = 0ul;

                        for (Int32 column = 0; column < 3; ++column)
                        {
                            UInt64 input = scratch[(((row * 3) + column) * m_Lanes) + lane];
                            value = Mix(value, input, m_Entropy[512 + (row * 3) + column]);
                        }

                        hashes[row * m_Lanes + lane] = value;
                    }
                }

                Int32 slotOffset = 2 * stackLengths[0] * m_Lanes;

                for (Int32 lane = 0; lane < m_Lanes; ++lane)
                {
                    UInt64 a = unchecked(
                        hashes[lane] + hashes[(2 * m_Lanes) + lane] +
                        hashes[(3 * m_Lanes) + lane] + (hashes[(4 * m_Lanes) + lane] << 1) +
                        hashes[(5 * m_Lanes) + lane] + (hashes[(6 * m_Lanes) + lane] << 2)
                    );

                    UInt64 b = unchecked(
                        hashes[m_Lanes + lane] + hashes[(2 * m_Lanes) + lane] +
                        (hashes[(3 * m_Lanes) + lane] << 1) + hashes[(4 * m_Lanes) + lane] +
                        (hashes[(5 * m_Lanes) + lane] << 2) + hashes[(6 * m_Lanes) + lane]
                    );

                    stack[slotOffset + lane] = a;
                    stack[slotOffset + m_Lanes + lane] = b;
                }

                ++stackLengths[0];
            }

            Int32 seedOffset = 659;

            for (Int32 level = 0; level < 9 && stackLengths[level] > 0; ++level)
            {
                for (Int32 child = 0; child < stackLengths[level]; ++child)
                {
                    Int32 source = 2 * ((level * 8) + child) * m_Lanes;

                    for (Int32 channel = 0; channel < 2; ++channel)
                    {
                        for (Int32 lane = 0; lane < m_Lanes; ++lane)
                        {
                            Int32 index = (channel * m_Lanes) + lane;
                            accumulator[index] = Mix(accumulator[index], stack[source + index], m_Entropy[seedOffset + lane]);
                        }

                        seedOffset += m_Lanes;
                    }
                }
            }

            Int32 inputOffset = groups * groupBytes;

            while (inputOffset + blockBytes <= count)
            {
                for (Int32 channel = 0; channel < 2; ++channel)
                {
                    for (Int32 lane = 0; lane < m_Lanes; ++lane)
                    {
                        UInt64 word = BinaryOperations.Read64(buffer, inputOffset + (lane * 8));
                        Int32 index = (channel * m_Lanes) + lane;
                        accumulator[index] = Mix(accumulator[index], word, m_Entropy[seedOffset + index]);
                    }
                }

                seedOffset += m_Lanes;
                inputOffset += blockBytes;
            }

            for (Int32 channel = 0; channel < 2; ++channel)
            {
                for (Int32 lane = 0; lane < m_Lanes; ++lane)
                {
                    Int32 byteOffset = inputOffset + (lane * 8);

                    UInt64 word;

                    if (byteOffset + 8 <= count)
                        word = BinaryOperations.Read64(buffer, byteOffset);
                    else if (byteOffset < count)
                        word = BinaryOperations.ReadTail64(buffer, byteOffset);
                    else
                        word = 0ul;

                    Int32 index = channel * m_Lanes + lane;
                    accumulator[index] = Mix(accumulator[index], word, m_Entropy[seedOffset + index]);
                }
            }

            UInt64 o0 = 0ul;
            UInt64 o1 = 0ul;

            for (Int32 lane = 0; lane < m_Lanes; ++lane)
            {
                o0 = unchecked(o0 + accumulator[lane]);
                o1 = unchecked(o1 + accumulator[m_Lanes + lane]);
            }

            o0 = Tabulate(o0, 2048);
            o1 = Tabulate(o1, 4096);

            UInt64 hash = Tabulate((UInt64)count, 0) ^ o0 ^ o1;

            return hash;
        }



        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Style}";
        }
        #endregion
    }
}