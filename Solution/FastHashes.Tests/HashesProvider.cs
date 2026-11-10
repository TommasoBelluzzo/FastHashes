#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    internal static class HashesProvider
    {
        public static IEnumerable<Object[]> GetHashes()
        {
            foreach (String algorithm in Hash.GetAlgorithms())
                yield return new Object[] { algorithm };
        }
    }
}