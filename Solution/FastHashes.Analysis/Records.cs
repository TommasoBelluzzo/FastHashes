#region Using Directives
using System;
#endregion

namespace FastHashes.Analysis
{
    internal readonly record struct Config
    (
        String LibraryVersion,
        String AnalysisName,
        Hash Hash,
        String HashName,
        UInt64? HashSeed,
        UInt64 GeneratorSeed,
        Int32 Samples,
        Int32 KeysLength,
        Double Significance,
        Int32 CorpusSize
    );

    internal readonly record struct Result
    (
        String Label,
        Boolean Completed,
        String Error
    );
}