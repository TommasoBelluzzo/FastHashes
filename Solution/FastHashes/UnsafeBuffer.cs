#region Using Directives
using System;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents a utility for manipulating byte arrays.</summary>
    public static class UnsafeBuffer
    {
        #region Methods
        /// <summary>Copies the specified region of a source array into the specified region of a destination array.</summary>
        /// <param name="source">The source <see cref="T:System.Byte"/>[].</param>
        /// <param name="sourceOffset">The zero-based offset into <paramref name="source">source</paramref>.</param>
        /// <param name="destination">The destination <see cref="T:System.Byte"/>[].</param>
        /// <param name="destinationOffset">The zero-based offset into <paramref name="destination">destination</paramref>.</param>
        /// <param name="count">The number of bytes to copy.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="source">source</paramref> is less than <paramref name="sourceOffset">sourceOffset</paramref> plus <paramref name="count">count</paramref> or when the number of bytes in <paramref name="destination">destination</paramref> is less than <paramref name="destinationOffset">destinationOffset</paramref> plus <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="source">source</paramref> and <paramref name="destination">destination</paramref> are <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="sourceOffset">sourceOffset</paramref> is not within the bounds of <paramref name="source">source</paramref>, when <paramref name="destinationOffset">destinationOffset</paramref> is not within the bounds of <paramref name="destination">destination</paramref> or when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void BlockCopy(Byte[] source, Int32 sourceOffset, Byte[] destination, Int32 destinationOffset, Int32 count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            Int32 sourceLength = source.Length;
            Int32 destinationLength = destination.Length;

            if ((sourceOffset < 0) || (sourceOffset >= sourceLength))
                throw new ArgumentOutOfRangeException(nameof(sourceOffset), "The source offset parameter must be within the bounds of the source array.");

            if ((destinationOffset < 0) || (destinationOffset >= destinationLength))
                throw new ArgumentOutOfRangeException(nameof(destinationOffset), "The destination offset parameter must be within the bounds of the destination array.");

            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), "The count parameter must be greater than or equal to 1.");

            if ((sourceOffset + count) > sourceLength)
                throw new ArgumentException("The block defined by source offset and count parameters must be within the bounds of the source array.");

            if ((destinationOffset + count) > destinationLength)
                throw new ArgumentException("The block defined by destination offset and count parameters must be within the bounds of the destination array.");

            fixed (Byte* pinSource = &source[sourceOffset])
            fixed (Byte* pinDestination = &destination[destinationOffset])
            {
                Byte* pointerSource = pinSource;
                Byte* pointerDestination = pinDestination;
                
LengthSwitch:

                switch (count)
                {
                    case 1:
                        *pointerDestination = *pointerSource;
                        return;
                    case 2:
                        *(Int16*)pointerDestination = *(Int16*)pointerSource;
                        return;
                    case 3:
                        *(Int16*)(pointerDestination + 0) = *(Int16*)(pointerSource + 0);
                        *(pointerDestination + 2) = *(pointerSource + 2);
                        return;
                    case 4:
                        *(Int32*)pointerDestination = *(Int32*)pointerSource;
                        return;
                    case 5:
                        *(Int32*)(pointerDestination + 0) = *(Int32*)(pointerSource + 0);
                        *(pointerDestination + 4) = *(pointerSource + 4);
                        return;
                    case 6:
                        *(Int32*)(pointerDestination + 0) = *(Int32*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 4) = *(Int16*)(pointerSource + 4);
                        return;
                    case 7:
                        *(Int32*)(pointerDestination + 0) = *(Int32*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 4) = *(Int16*)(pointerSource + 4);
                        *(pointerDestination + 6) = *(pointerSource + 6);
                        return;
                    case 8:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        return;
                    case 9:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(pointerDestination + 8) = *(pointerSource + 8);
                        return;
                    case 10:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 8) = *(Int16*)(pointerSource + 8);
                        return;
                    case 11:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 8) = *(Int16*)(pointerSource + 8);
                        *(pointerDestination + 10) = *(pointerSource + 10);
                        return;
                    case 12:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        return;
                    case 13:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        *(pointerDestination + 12) = *(pointerSource + 12);
                        return;
                    case 14:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 12) = *(Int16*)(pointerSource + 12);
                        return;
                    case 15:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 12) = *(Int16*)(pointerSource + 12);
                        *(pointerDestination + 14) = *(pointerSource + 14);
                        return;
                    case 16:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        return;
                    case 17:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(pointerDestination + 16) = *(pointerSource + 16);
                        return;
                    case 18:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 16) = *(Int16*)(pointerSource + 16);
                        return;
                    case 19:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 16) = *(Int16*)(pointerSource + 16);
                        *(pointerDestination + 18) = *(pointerSource + 18);
                        return;
                    case 20:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        return;
                    case 21:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        *(pointerDestination + 20) = *(pointerSource + 20);
                        return;
                    case 22:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 20) = *(Int16*)(pointerSource + 20);
                        return;
                    case 23:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 20) = *(Int16*)(pointerSource + 20);
                        *(pointerDestination + 22) = *(pointerSource + 22);
                        return;
                    case 24:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        return;
                    case 25:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(pointerDestination + 24) = *(pointerSource + 24);
                        return;
                    case 26:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 24) = *(Int16*)(pointerSource + 24);
                        return;
                    case 27:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 24) = *(Int16*)(pointerSource + 24);
                        *(pointerDestination + 26) = *(pointerSource + 26);
                        return;
                    case 28:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        return;
                    case 29:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        *(pointerDestination + 28) = *(pointerSource + 28);
                        return;
                    case 30:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        *(Int16*)(pointerDestination + 28) = *(Int16*)(pointerSource + 28);
                        return;
                    case 31:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        *(Int16*)(pointerDestination + 28) = *(Int16*)(pointerSource + 28);
                        *(pointerDestination + 30) = *(pointerSource + 30);
                        return;
                    case 32:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int64*)(pointerDestination + 24) = *(Int64*)(pointerSource + 24);
                        return;
                }
    
                Int64* pointerSourceLong = (Int64*)pointerSource;
                Int64* pointerDestinationLong = (Int64*)pointerDestination;

                while (count >= 64)
                {
                    *(pointerDestinationLong + 0) = *(pointerSourceLong + 0);
                    *(pointerDestinationLong + 1) = *(pointerSourceLong + 1);
                    *(pointerDestinationLong + 2) = *(pointerSourceLong + 2);
                    *(pointerDestinationLong + 3) = *(pointerSourceLong + 3);
                    *(pointerDestinationLong + 4) = *(pointerSourceLong + 4);
                    *(pointerDestinationLong + 5) = *(pointerSourceLong + 5);
                    *(pointerDestinationLong + 6) = *(pointerSourceLong + 6);
                    *(pointerDestinationLong + 7) = *(pointerSourceLong + 7);

                    if (count == 64)
                        return;

                    pointerSourceLong += 8;
                    pointerDestinationLong += 8;

                    count -= 64;
                }

                if (count > 32)
                {
                    *(pointerDestinationLong + 0) = *(pointerSourceLong + 0);
                    *(pointerDestinationLong + 1) = *(pointerSourceLong + 1);
                    *(pointerDestinationLong + 2) = *(pointerSourceLong + 2);
                    *(pointerDestinationLong + 3) = *(pointerSourceLong + 3);

                    pointerSourceLong += 4;
                    pointerDestinationLong += 4;

                    count -= 32;
                }
                
                pointerSource = (Byte*)pointerSourceLong;
                pointerDestination = (Byte*)pointerDestinationLong;

                goto LengthSwitch;
            }
        }
        #endregion
    }
}