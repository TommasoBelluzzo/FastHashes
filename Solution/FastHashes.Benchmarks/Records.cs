#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Benchmarks
{
    internal readonly record struct BulkSpeedOutput
    (
        Double[] BulkAlignSpeeds,
        Double[] BulkVariability,
        Double BulkMedianSpeed,
        String BulkMedianSpeedFormatted
    );

    internal readonly record struct ChunkSpeedEntry
    (
        Int32 Start,
        Int32 End,
        Double Speed,
        Double Variability
    );

    internal readonly record struct ChunkSpeedOutput
    (
        List<ChunkSpeedEntry> ChunkSpeeds,
        Double ChunkAggregateSpeed,
        String ChunkAggregateSpeedFormatted,
        Double ChunkNormalizedSpeed,
        String ChunkNormalizedSpeedFormatted
    );

    internal readonly record struct Result
    (
        String HashName,
        String BulkMedianSpeedFormatted,
        String ChunkNormalizedSpeedFormatted
    );

    internal readonly record struct SpeedOutput
    (
        Int64 Bytes,
        Int64 Ticks,
        List<Double> BatchSpeeds
    );
}