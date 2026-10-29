
#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class BinaryOperationsTestsCases
    {
        #region Test Cases
        private static readonly Byte[] s_Buffer = [174, 9, 151, 129, 39, 0, 0, 254, 147, 105, 81, 36, 59, 76, 58, 227];

        public static IEnumerable<Object[]> DataRead()
        {
            yield return [(() => BinaryOperations.Read16(new ReadOnlySpan<Byte>(s_Buffer), 5)), (UInt16)0];
            yield return [(() => BinaryOperations.Read16(new ReadOnlySpan<Byte>(s_Buffer), 8)), (UInt16)27027];
            yield return [(() => BinaryOperations.Read32(new ReadOnlySpan<Byte>(s_Buffer), 1)), 662804233u];
            yield return [(() => BinaryOperations.Read32(new ReadOnlySpan<Byte>(s_Buffer), 10)), 1278944337u];
            yield return [(() => BinaryOperations.Read64(new ReadOnlySpan<Byte>(s_Buffer), 0)), 18302629055311579566ul];
            yield return [(() => BinaryOperations.Read64(new ReadOnlySpan<Byte>(s_Buffer), 5)), 4261620661295644672ul];
        }

        public static IEnumerable<Object[]> DataReadArray()
        {
            yield return [(() => BinaryOperations.ReadArray16(new ReadOnlySpan<Byte>(s_Buffer), 0, 3)), new UInt16[] { 2478, 33175, 39 }];
            yield return [(() => BinaryOperations.ReadArray16(new ReadOnlySpan<Byte>(s_Buffer), 5, 3)), new UInt16[] { 0, 37886, 20841 }];
            yield return [(() => BinaryOperations.ReadArray32(new ReadOnlySpan<Byte>(s_Buffer), 1, 2)), new UInt32[] { 662804233, 2482896896 }];
            yield return [(() => BinaryOperations.ReadArray32(new ReadOnlySpan<Byte>(s_Buffer), 2, 3)), new UInt32[] { 2589079, 1771306496, 1278944337 }];
            yield return [(() => BinaryOperations.ReadArray64(new ReadOnlySpan<Byte>(s_Buffer), 0, 2)), new UInt64[] { 18302629055311579566, 16373483212154956179 }];
            yield return [(() => BinaryOperations.ReadArray64(new ReadOnlySpan<Byte>(s_Buffer), 3, 1)), new UInt64[] { 5866382708757768065 }];
        }

        public static IEnumerable<Object[]> DataReadTail()
        {
            yield return [(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 0)), 0u];
            yield return [(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 12)), 3812248635u];
            yield return [(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 13)), 14891596u];
            yield return [(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 14)), 58170u];
            yield return [(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 15)), 227u];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 0)), 0ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 8)), 16373483212154956179ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 9)), 63958918797480297ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 10)), 249839526552657ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 11)), 975935650596ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 12)), 3812248635ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 13)), 14891596ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 14)), 58170ul];
            yield return [(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 15)), 227ul];
        }

        public static IEnumerable<Object[]> DataRotation()
        {
            yield return [(() => BinaryOperations.RotateLeft(1027, 0)), (UInt16)1027];
            yield return [(() => BinaryOperations.RotateRight(1027, 5)), (UInt16)6176];
            yield return [(() => BinaryOperations.RotateLeft(45561, 3)), (UInt16)36813];
            yield return [(() => BinaryOperations.RotateRight(45561, 9)), (UInt16)64728];
            yield return [(() => BinaryOperations.RotateLeft(10523123u, 0)), 10523123u];
            yield return [(() => BinaryOperations.RotateRight(10523123u, 17)), 1224310864u];
            yield return [(() => BinaryOperations.RotateLeft(944209717u, 8)), 1199519032u];
            yield return [(() => BinaryOperations.RotateRight(944209717u, 31)), 1888419434u];
            yield return [(() => BinaryOperations.RotateLeft(197029066322453301ul, 13)), 9195376900806451287ul];
            yield return [(() => BinaryOperations.RotateRight(197029066322453301ul, 49)), 18334763529516253533ul];
            yield return [(() => BinaryOperations.RotateLeft(6955140290117ul, 13)), 56976509256638464ul];
            yield return [(() => BinaryOperations.RotateRight(6955140290117ul, 62)), 27820561160468ul];
        }

        public static IEnumerable<Object[]> DataSwap()
        {
            yield return [(() => { UInt16 a = 7, b = 4093; BinaryOperations.Swap(ref a, ref b); return a; }), (UInt16)4093];
            yield return [(() => { UInt32 a = 9488124u, b = 4123321u; BinaryOperations.Swap(ref a, ref b); return a; }), 4123321u];
            yield return [(() => { UInt64 a = 234620953293ul, b = 6430983184821ul; BinaryOperations.Swap(ref a, ref b); return a; }), 6430983184821ul];
        }

        public static IEnumerable<Object[]> DataToArray()
        {
            yield return [(() => BinaryOperations.ToArray32([])), ""];
            yield return [(() => BinaryOperations.ToArray32(0x00000000u)), "00000000"];
            yield return [(() => BinaryOperations.ToArray32(0x12345678u)), "78563412"];
            yield return [(() => BinaryOperations.ToArray32(0xFFFFFFFFu)), "FFFFFFFF"];
            yield return [(() => BinaryOperations.ToArray32(0x0123456789ABCDEFul)), "EFCDAB89"];
            yield return [(() => BinaryOperations.ToArray32(0x12345678u, 0x9ABCDEF0u)), "78563412F0DEBC9A"];
            yield return [(() => BinaryOperations.ToArray32(0x00000000u, 0x00000001u, 0x0C000000u, UInt32.MaxValue)), "00000000010000000000000CFFFFFFFF"];
            yield return [(() => BinaryOperations.ToArray32([0x00000022u, 0xA0000000u])), "22000000000000A0"];

            yield return [(() => BinaryOperations.ToArray64([])), ""];
            yield return [(() => BinaryOperations.ToArray64(0x0000000000000000ul)), "0000000000000000"];
            yield return [(() => BinaryOperations.ToArray64(0x0123456789ABCDEFul)), "EFCDAB8967452301"];
            yield return [(() => BinaryOperations.ToArray64(0xFFFFFFFFFFFFFFFFul)), "FFFFFFFFFFFFFFFF"];
            yield return [(() => BinaryOperations.ToArray64(0x12345678u)), "7856341200000000"];
            yield return [(() => BinaryOperations.ToArray64(0x0123456789ABCDEFul, 0x0102030405060708ul)), "EFCDAB89674523010807060504030201"];
            yield return [(() => BinaryOperations.ToArray64(0x0000000000000000ul, 0x0000000000000001ul, 0x0C00000000000000ul, UInt64.MaxValue)), "00000000000000000100000000000000000000000000000CFFFFFFFFFFFFFFFF"];
            yield return [(() => BinaryOperations.ToArray64([0x0000000000000022ul, 0xA000000000000000ul])), "220000000000000000000000000000A0"];

        }
        #endregion
    }
}