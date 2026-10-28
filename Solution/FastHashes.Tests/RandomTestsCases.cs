#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class RandomTestsCases
    {
        #region Test Cases
        private static readonly List<(UInt32 Seed, Int32 BufferLength, Int32 Offset, Int32 Count, Byte[] Expected)> s_TestCasesBuffer =
        [
            (9u, 0, 0, 0, []),
            (266u, 10, 0, 3, [193, 74, 65, 0, 0, 0, 0, 0, 0, 0]),
            (148077u, 10, 0, 10, [221, 40, 74, 23, 172, 98, 184, 208, 157, 36]),
            (8018u, 10, 1, 5, [0, 71, 102, 179, 5, 105, 0, 0, 0, 0]),
            (5946u, 10, 3, 2, [0, 0, 0, 103, 109, 0, 0, 0, 0, 0]),
            (139u, 10, 7, 1, [0, 0, 0, 0, 0, 0, 0, 73, 0, 0])
        ];

        private static readonly List<(UInt32 Seed, UInt32 Expected)> s_TestCasesValue =
        [
            (1707731u, 3589663199u),
            (26016u, 107436895u),
            (37u, 88619671u),
            (75u, 88820105u),
            (8810u, 73101954u),
            (93455u, 237727616u),
            (0u, 88675226u),
            (740u, 90056284u),
            (128u, 88938266u),
            (8944u, 73377480u)
        ];

        public static IEnumerable<Object[]> DataBuffer()
        {
            foreach (var (Seed, BufferLength, Offset, Count, Expected) in s_TestCasesBuffer)
                yield return new Object[] { Seed, BufferLength, Offset, Count, Expected };
        }

        public static IEnumerable<Object[]> DataValue()
        {
            foreach (var (Seed, Expected) in s_TestCasesValue)
                yield return new Object[] { Seed, Expected };
        }
        #endregion
    }
}