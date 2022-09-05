#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
	/// <summary>Represents the Mx3Hash implementation. This class cannot be derived.</summary>
	public sealed class Mx3Hash : Hash
    {
		#region Constants
		private const UInt64 C = 0XBEA225F9EB34556Dul;
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
        public Mx3Hash(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public Mx3Hash() : this(0ul) { }
		#endregion

		#region Methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static UInt64 Mix(UInt64 x)
		{
			x ^= x >> 32;
			x *= C;
			x ^= x >> 29;
			x *= C;
			x ^= x >> 32;
			x *= C;
			x ^= x >> 29;

			return x;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static UInt64 MixStream(UInt64 hash, UInt64 x)
		{
			x *= C;
			x ^= x >> 39;

			hash += x * C;
			hash *= C;

			return hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static UInt64 MixStream(UInt64 hash, UInt64 a, UInt64 b, UInt64 c, UInt64 d)
		{
			a *= C;
			b *= C;
			c *= C;
			d *= C;

			a ^= a >> 39;
			b ^= b >> 39;
			c ^= c >> 39;
			d ^= d >> 39;

			hash += a * C;
			hash *= C;
			hash += b * C;
			hash *= C;
			hash += c * C;
			hash *= C;
			hash += d * C;
			hash *= C;

			return hash;
		}

		/// <inheritdoc/>
		protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
			Int32 offset = 0;
            Int32 count = buffer.Length;

			UInt64 hash = MixStream(m_Seed, (UInt64)count + 1);

			while (count >= 64)
			{
				UInt64 a, b, c, d;

				a = BinaryOperations.Read64(buffer, offset);
				b = BinaryOperations.Read64(buffer, offset + 8);
				c = BinaryOperations.Read64(buffer, offset + 16);
				d = BinaryOperations.Read64(buffer, offset + 24);
				hash = MixStream(hash, a, b, c, d);

				a = BinaryOperations.Read64(buffer, offset + 32);
				b = BinaryOperations.Read64(buffer, offset + 40);
				c = BinaryOperations.Read64(buffer, offset + 48);
				d = BinaryOperations.Read64(buffer, offset + 56);
				hash = MixStream(hash, a, b, c, d);

				offset += 64;
				count -= 64;
			}

			while (count >= 8)
			{
				UInt64 x = BinaryOperations.Read64(buffer, offset);
				hash = MixStream(hash, x);

				offset += 8;
				count -= 8;
			}

			switch (count)
			{
				case 1:
					UInt64 v1 = buffer[offset];
					hash = Mix(MixStream(hash, v1));
					break;

				case 2:
					UInt64 v2 = BinaryOperations.Read16(buffer, offset);
					hash = Mix(MixStream(hash, v2));
					break;

				case 3:
					UInt64 v3 = BinaryOperations.Read16(buffer, offset) | ((UInt64)buffer[offset + 2] << 16);
					hash = Mix(MixStream(hash, v3));
					break;

				case 4:
					UInt64 v4 = BinaryOperations.Read32(buffer, offset);
					hash = Mix(MixStream(hash, v4));
					break;

				case 5:
					UInt64 v5 = BinaryOperations.Read32(buffer, offset) | ((UInt64)buffer[offset + 4] << 32);
					hash = Mix(MixStream(hash, v5));
					break;

				case 6:
					UInt64 v6 = BinaryOperations.Read32(buffer, offset) | ((UInt64)BinaryOperations.Read16(buffer, offset + 4) << 32);
					hash = Mix(MixStream(hash, v6));
					break;

				case 7:
					UInt64 v7 = BinaryOperations.Read32(buffer, offset) | ((UInt64)BinaryOperations.Read16(buffer, offset + 4) << 32) | ((UInt64)buffer[offset + 6] << 48);
					hash = Mix(MixStream(hash, v7));
					break;

				default:
					hash = Mix(hash);
					break;
			}

			Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }
}