#region Using Directives
using System;
using System.Linq;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    public static class UnsafeBuffer
    {
        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void BlockCopy(Byte[] source, Int32 sourceOffset, Byte[] destination, Int32 destinationOffset, Int32 length)
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

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "The length parameter must be greater than or equal to 0.");

            if ((sourceOffset + length) > sourceLength)
                throw new InvalidOperationException("The block defined by source offset and length parameters must be within the bounds of the source array.");

            if ((destinationOffset + length) > destinationLength)
                throw new InvalidOperationException("The block defined by destination offset and length parameters must be within the bounds of the destination array.");

            fixed (Byte* pinSource = &source[sourceOffset])
            fixed (Byte* pinDestination = &destination[destinationOffset])
            {
                Byte* pointerSource = pinSource;
                Byte* pointerDestination = pinDestination;
                
                SMALLTABLE:

                switch (length)
                {
                    case 0:
                        return;
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

                while (length >= 64)
                {
                    *(pointerDestinationLong + 0) = *(pointerSourceLong + 0);
                    *(pointerDestinationLong + 1) = *(pointerSourceLong + 1);
                    *(pointerDestinationLong + 2) = *(pointerSourceLong + 2);
                    *(pointerDestinationLong + 3) = *(pointerSourceLong + 3);
                    *(pointerDestinationLong + 4) = *(pointerSourceLong + 4);
                    *(pointerDestinationLong + 5) = *(pointerSourceLong + 5);
                    *(pointerDestinationLong + 6) = *(pointerSourceLong + 6);
                    *(pointerDestinationLong + 7) = *(pointerSourceLong + 7);

                    if (length == 64)
                        return;

                    pointerSourceLong += 8;
                    pointerDestinationLong += 8;

                    length -= 64;
                }

                if (length > 32)
                {
                    *(pointerDestinationLong + 0) = *(pointerSourceLong + 0);
                    *(pointerDestinationLong + 1) = *(pointerSourceLong + 1);
                    *(pointerDestinationLong + 2) = *(pointerSourceLong + 2);
                    *(pointerDestinationLong + 3) = *(pointerSourceLong + 3);

                    pointerSourceLong += 4;
                    pointerDestinationLong += 4;

                    length -= 32;
                }
                
                pointerSource = (Byte*)pointerSourceLong;
                pointerDestination = (Byte*)pointerDestinationLong;

                goto SMALLTABLE;
            }
        }
        #endregion
    }
}