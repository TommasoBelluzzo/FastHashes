#region Using Directives
using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
	/// <summary>Represents the KomiHash implementation. This class cannot be derived.</summary>
    public sealed class KomiHash : Hash
    {
        #region Constants
        private const UInt64 P1 = 0x243F6A8885A308D3ul;
        private const UInt64 P2 = 0x13198A2E03707344ul;
        private const UInt64 P3 = 0xA4093822299F31D0ul;
        private const UInt64 P4 = 0x082EFA98EC4E6C89ul;
        private const UInt64 P5 = 0x452821E638D01377ul;
        private const UInt64 P6 = 0xBE5466CF34E90C6Cul;
        private const UInt64 P7 = 0xC0AC29B7C97C50DDul;
        private const UInt64 P8 = 0x3F84D5B5B5470917ul;
		#endregion

		#region Members
        private readonly UInt64 m_Seed;
		#endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public KomiHash(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public KomiHash() : this(0ul) { }
		#endregion

		#region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Finalize(UInt64 r1, UInt64 r2, ref UInt64 s1, ref UInt64 s5)
        {
            Mix(r1, r2, ref s1, ref s5);
            s1 ^= s5;

            Round(ref s1, ref s5);

            return s1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Hash(ReadOnlySpan<Byte> buffer, Int32 offset, ref UInt64 s1, ref UInt64 s5)
        {
			UInt64 u = U64(buffer, offset) ^ s1;
			UInt64 w = U64(buffer, offset + 8) ^ s5;

            Mix(u, w, ref s1, ref s5);
            s1 ^= s5;
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static UInt64 Hash0To15(ReadOnlySpan<Byte> buffer, Int32 count, UInt64 s1, UInt64 s5)
		{
			UInt64 r1 = s1;
			UInt64 r2 = s5;

			if (count > 7)
			{
				r1 ^= U64(buffer, 0);

				Int32 ml8 = count * 8;

				if (count < 12)
				{
					ml8 ^= 88;

					UInt64 m = (UInt64)buffer[count - 3]
						| ((UInt64)buffer[count - 1] << 16)
						| (1UL << 24)
						| ((UInt64)buffer[count - 2] << 8);

					r2 ^= m >> ml8;
				}
				else
				{
					Int32 mhs = 128 - ml8;
					UInt64 mh = (U32(buffer, count - 4) | (1UL << 32)) >> mhs;

					r2 ^= (mh << 32) | U32(buffer, 8);
				}
			}
			else if (count != 0)
			{
				Int32 ml8 = count * 8;

				if (count < 4)
				{
					r1 ^= buffer[0];
					r1 ^= 1UL << ml8;

					if (count != 1)
					{
						r1 ^= (UInt64)buffer[1] << 8;

						if (count != 2)
							r1 ^= (UInt64)buffer[2] << 16;
					}
				}
				else
				{
					Int32 mhs = 64 - ml8;
					UInt64 mh = (U32(buffer, count - 4) | (1UL << 32)) >> mhs;

					r1 ^= (mh << 32) | U32(buffer, 0);
				}
			}

			UInt64 hash = Finalize(r1, r2, ref s1, ref s5);

			return hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static UInt64 Hash16To31(ReadOnlySpan<Byte> buffer, Int32 count, UInt64 s1, UInt64 s5)
		{
			Hash(buffer, 0, ref s1, ref s5);

			UInt64 r1;
			UInt64 r2;

			Int32 ml8 = count * 8;

			if (count < 24)
			{
				ml8 ^= 184;

				r1 = (((U64(buffer, count - 8) >> 8) | (1UL << 56)) >> ml8) ^ s1;
				r2 = s5;
			}
			else
			{
				r2 = (U64(buffer, count - 8) >> 8) | (1UL << 56);

				ml8 ^= 248;

				r1 = U64(buffer, 16) ^ s1;
				r2 = (r2 >> ml8) ^ s5;
			}

			UInt64 hash = Finalize(r1, r2, ref s1, ref s5);

			return hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static UInt64 Hash32ToEnd(ReadOnlySpan<Byte> buffer, Int32 count, UInt64 s1, UInt64 s5)
		{
			Int32 offset = 0;

			if (count > 63)
			{
				UInt64 s2 = P2 ^ s1;
				UInt64 s3 = P3 ^ s1;
				UInt64 s4 = P4 ^ s1;
				UInt64 s6 = P6 ^ s5;
				UInt64 s7 = P7 ^ s5;
				UInt64 s8 = P8 ^ s5;

				do
				{
					Mix(U64(buffer, offset) ^ s1, U64(buffer, offset + 32) ^ s5, ref s1, ref s5);
					Mix(U64(buffer, offset + 8) ^ s2, U64(buffer, offset + 40) ^ s6, ref s2, ref s6);
					Mix(U64(buffer, offset + 16) ^ s3, U64(buffer, offset + 48) ^ s7, ref s3, ref s7);
					Mix(U64(buffer, offset + 24) ^ s4, U64(buffer, offset + 56) ^ s8, ref s4, ref s8);

					offset += 64;
					count -= 64;

					s4 ^= s7;
					s1 ^= s8;
					s2 ^= s5;
					s3 ^= s6;
				}
				while (count > 63);

				s5 ^= s6 ^ s7 ^ s8;
				s1 ^= s2 ^ s3 ^ s4;
			}

			if (count > 31)
			{
				Hash(buffer, offset, ref s1, ref s5);
				Hash(buffer, offset + 16, ref s1, ref s5);

				offset += 32;
				count -= 32;
			}

			if (count > 15)
			{
				Hash(buffer, offset, ref s1, ref s5);

				offset += 16;
				count -= 16;
			}

			UInt64 r1;
			UInt64 r2;

			Int32 sh = count * 8;

			if (count < 8)
			{
				sh ^= 56;

				r1 = (U64(buffer, offset + count - 8) >> 8) | (1UL << 56);
				r2 = s5;
				r1 = (r1 >> sh) ^ s1;
			}
			else
			{
				r2 = (U64(buffer, offset + count - 8) >> 8) | (1UL << 56);

				sh ^= 120;

				r1 = U64(buffer, offset) ^ s1;
				r2 = (r2 >> sh) ^ s5;
			}

			UInt64 hash = Finalize(r1, r2, ref s1, ref s5);

			return hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Mix(UInt64 a, UInt64 b, ref UInt64 lo, ref UInt64 hi)
		{
			unchecked
			{
				#if NET5_0_OR_GREATER
				UInt64 high = Math.BigMul(a, b, out lo);
				hi += high;
				#else
				UInt64 a0 = (UInt32)a;
				UInt64 a1 = a >> 32;
				UInt64 b0 = (UInt32)b;
				UInt64 b1 = b >> 32;

				UInt64 p0 = a0 * b0;
				UInt64 p1 = a0 * b1;
				UInt64 p2 = a1 * b0;
				UInt64 p3 = a1 * b1;

				UInt64 middle = (p0 >> 32) + (UInt32)p1 + (UInt32)p2;
				lo = (middle << 32) | (UInt32)p0;

				UInt64 high = p3 + (p1 >> 32) + (p2 >> 32) + (middle >> 32);
				hi += high;
				#endif
			}
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Round(ref UInt64 s1, ref UInt64 s5)
        {
            Mix(s1, s5, ref s1, ref s5);
            s1 ^= s5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 U32(ReadOnlySpan<Byte> buffer, Int32 offset)
		{
			return BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 U64(ReadOnlySpan<Byte> buffer, Int32 offset)
		{
			return BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(offset, 8));
		} 

		/// <inheritdoc/>
		protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
		{
			Int32 count = buffer.Length;

			UInt64 s1 = P1 ^ (m_Seed & 0x5555555555555555UL);
			UInt64 s5 = P5 ^ (m_Seed & 0xAAAAAAAAAAAAAAAAUL);

			Round(ref s1, ref s5);

			UInt64 hash;

			if (count < 16)
				hash = Hash0To15(buffer, count, s1, s5);
			else if (count < 32)
				hash = Hash16To31(buffer, count, s1, s5);
			else
				hash = Hash32ToEnd(buffer, count, s1, s5);

			Byte[] result = BinaryOperations.ToArray64(hash);

			return result;
		}
		#endregion
    }
}