#region Using Directives
using System;
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
		private static void Hash0To15(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, ref UInt64 s1, ref UInt64 s5, ref UInt64 r2l, ref UInt64 r2h)
        {
			r2l = s1;
			r2h = s5;

			if (count >= 8)
			{
				Int32 delta = count - 8;
				Int32 end = offset + count;

				UInt64 fb = (UInt64)(1 << (buffer[count - 1] >> 7));
				Int32 ml8 = delta << 3;

				UInt64 v;

				if (delta <= 3)
				{
					UInt64 m = (UInt64)buffer[end - 3] | ((UInt64)buffer[end - 2] << 8) | ((UInt64)buffer[end - 1] << 16);

					v = (fb << ml8) | (m >> (24 - ml8));
				}
				else
				{
					UInt64 mh = BinaryOperations.Read32(buffer, end - 4);
					UInt64 ml = BinaryOperations.Read32(buffer, offset + 8);

					v = (fb << ml8) | ml | ((mh >> (64 - ml8)) << 32);
				}

				r2h ^= v;
				r2l ^= BinaryOperations.Read64(buffer, offset);
			}
			else if (count > 0)
            {
				UInt64 fb = (UInt64)(1 << (buffer[count - 1] >> 7));

				UInt64 v;

				if (count <= 3)
				{
					fb <<= (count << 3);

					UInt64 m = buffer[offset];

					if (count > 1)
					{
						m |= (UInt64)buffer[offset + 1] << 8;

						if (count > 2)
							m |= (UInt64)buffer[offset + 2] << 16;
					}

					v = fb | m;
				}
				else
				{
					Int32 ml8 = count << 3;
					UInt64 mh = BinaryOperations.Read32(buffer, offset + count - 4);
					UInt64 ml = BinaryOperations.Read32(buffer, offset);

					v = (fb << ml8) | ml | ((mh >> (64 - ml8)) << 32);
				}

				r2l ^= v;
			}
		}

		private static void Hash17To31(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, ref UInt64 s1, ref UInt64 s5, ref UInt64 r1l, ref UInt64 r1h, ref UInt64 r2l, ref UInt64 r2h)
		{
			UInt64 k1 = BinaryOperations.Read64(buffer, offset);
			UInt64 k2 = BinaryOperations.Read64(buffer, offset + 8);
			Kh16(k1, k2, ref s1, ref s5, ref r1l, ref r1h);

			if (count >= 24)
			{
				r2h = s5 ^ KhLpu(buffer, offset, count, 24);
				r2l = s1 ^ BinaryOperations.Read64(buffer, offset + 16);
			}
			else
			{
				r2l = s1 ^ KhLpu(buffer, offset, count, 16);
				r2h = s5;
			}
		}

		private static void Hash32ToEnd(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, ref UInt64 s1, ref UInt64 s5, ref UInt64 r1l, ref UInt64 r1h, ref UInt64 r2l, ref UInt64 r2h)
		{
			if (count >= 64)
			{
				UInt64 s2 = P2 ^ s1;
				UInt64 s3 = P3 ^ s1;
				UInt64 s4 = P4 ^ s1;
				UInt64 s6 = P6 ^ s5;
				UInt64 s7 = P7 ^ s5;
				UInt64 s8 = P8 ^ s5;

				UInt64 r3l = 0ul;
				UInt64 r3h = 0ul;
				UInt64 r4l = 0ul;
				UInt64 r4h = 0ul;

				do
				{
					UInt64[] k = BinaryOperations.ReadArray64(buffer, offset, 8);
					Kh128(s1 ^ k[0], s5 ^ k[1], ref r1l, ref r1h);
					Kh128(s2 ^ k[2], s6 ^ k[3], ref r2l, ref r2h);
					Kh128(s3 ^ k[4], s7 ^ k[5], ref r3l, ref r3h);
					Kh128(s4 ^ k[6], s8 ^ k[7], ref r4l, ref r4h);

					s5 += r1h;
					s6 += r2h;
					s7 += r3h;
					s8 += r4h;

					s2 = s5 ^ r2l;
					s3 = s6 ^ r3l;
					s4 = s7 ^ r4l;
					s1 = s8 ^ r1l;

					count -= 64;
					offset += 64;
				}
				while (count >= 64);

				s5 ^= s6 ^ s7 ^ s8;
				s1 ^= s2 ^ s3 ^ s4;
			}

			if (count >= 32)
			{
				UInt64[] k = BinaryOperations.ReadArray64(buffer, offset, 4);
				Kh16(k[0], k[1], ref s1, ref s5, ref r1l, ref r1h);
				Kh16(k[2], k[3], ref s1, ref s5, ref r1l, ref r1h);

				count -= 32;
				offset += 32;
			}

			if (count >= 16)
			{
				UInt64[] k = BinaryOperations.ReadArray64(buffer, offset, 2);
				Kh16(k[0], k[1], ref s1, ref s5, ref r1l, ref r1h);

				count -= 16;
				offset += 16;
			}

			if (count >= 8)
			{
				r2h = s5 ^ KhLpu(buffer, offset, count, 8);
				r2l = s1 ^ BinaryOperations.Read64(buffer, offset);
			}
			else
			{
				r2l = s1 ^ KhLpu(buffer, offset, count, 0);
				r2h = s5;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Kh16(UInt64 k1, UInt64 k2, ref UInt64 s1, ref UInt64 s5, ref UInt64 rl, ref UInt64 rh)
		{
			Kh128(s1 ^ k1, s5 ^ k2, ref rl, ref rh);

			s5 += rh;
			s1 = s5 ^ rl;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Kh128(UInt64 ab, UInt64 cd, ref UInt64 rl, ref UInt64 rh)
		{
			UInt32 x, y;

			x = (UInt32)(ab >> 32);
			y = (UInt32)cd;
			UInt64 ad = x * (UInt64)y;

			x = (UInt32)ab;
			y = (UInt32)cd;
			UInt64 bd = x * (UInt64)y;

			x = (UInt32)ab;
			y = (UInt32)(cd >> 32);
			UInt64 adbc = ad + (x * (UInt64)y);
			UInt64 carry = !!(adbc < ad) ? 1ul : 0ul;

			x = (UInt32)(ab >> 32);
			y = (UInt32)(cd >> 32);
			UInt64 lo = bd + (adbc << 32);
			UInt64 hi = (x * (UInt64)y) + (adbc >> 32) + (carry << 32) + (!!(lo < bd) ? 1ul : 0ul);

			rl = lo;
            rh = hi;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static UInt64 KhLpu(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, Int32 delta)
		{
			Int32 fbRef = (count > 0) ? count : buffer.Length;
			UInt64 fb = (UInt64)(1 << (buffer[fbRef - 1] >> 7));

			offset += delta;
			count -= delta;

			Int32 ml8 = count << 3;

			UInt64 v;

			if (count <= 4)
				v = (fb << ml8) | ((UInt64)BinaryOperations.Read32(buffer, offset + count - 4) >> (32 - ml8));
			else
				v = (fb << ml8) | (BinaryOperations.Read64(buffer, offset + count - 8) >> (64 - ml8));

			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Mix(UInt64 ab, UInt64 cd, ref UInt64 s1, ref UInt64 s5, ref UInt64 rl, ref UInt64 rh)
		{
			Kh128(ab, cd, ref rl, ref rh);

			s5 += rh;
			s1 = s5 ^ rl;
		}

		/// <inheritdoc/>
		protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
			Int32 offset = 0;
            Int32 count = buffer.Length;

			UInt64 s1 = P1 ^ (m_Seed & 0x5555555555555555ul);
			UInt64 s5 = P5 ^ (m_Seed & 0xAAAAAAAAAAAAAAAAul);

			UInt64 r1l = 0ul;
			UInt64 r1h = 0ul;
			UInt64 r2l = 0ul;
			UInt64 r2h = 0ul;

			Mix(s1, s5, ref s1, ref s5, ref r2l, ref r2h);

			if (count < 16)
				Hash0To15(buffer, offset, count, ref s1, ref s5, ref r2l, ref r2h);
			else if (count < 32)
				Hash17To31(buffer, offset, count, ref s1, ref s5, ref r1l, ref r1h, ref r2l, ref r2h);
			else
				Hash32ToEnd(buffer, offset, count, ref s1, ref s5, ref r1l, ref r1h, ref r2l, ref r2h);

			Mix(r2l, r2h, ref s1, ref s5, ref r1l, ref r1h);
			Mix(s1, s5, ref s1, ref s5, ref r2l, ref r2h);

			UInt64 hash = s1;
            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }
}