#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Benchmarks
{
    internal readonly record struct BulkThroughputOutput
    (
        Double[] AlignmentThroughputs,
        Double[] BatchMads,
        Double MedianThroughput,
        String MedianThroughputFormatted
    );

    internal readonly record struct ChunkRangeThroughput
    (
        Int32 Start,
        Int32 End,
        Double Throughput,
        Double MedianBatchMad
    );

    internal readonly record struct ChunkThroughputOutput
    (
        List<ChunkRangeThroughput> RangeThroughputs,
        Double AggregateThroughput,
        String AggregateThroughputFormatted,
        Double AverageThroughput,
        String AverageThroughputFormatted
    );

    internal readonly record struct ThroughputMeasurement
    (
        Int64 Bytes,
        Int64 Ticks,
        List<Double> BatchThroughputs
    );

    internal readonly record struct Result
    (
        String HashName,
        Double BulkMedianThroughput,
        String BulkMedianThroughputFormatted,
        Double ChunkAverageThroughput,
        String ChunkAverageThroughputFormatted
    );
}