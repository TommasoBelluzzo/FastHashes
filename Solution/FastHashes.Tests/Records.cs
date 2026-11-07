#region Using Directives
using System;
#endregion

namespace FastHashes.Tests
{
    internal readonly record struct CollisionCase
    (
        String Algorithm,
        String Name,
        String Seed,
        String Input,
        String Expected,
        String Provenance
    );
}