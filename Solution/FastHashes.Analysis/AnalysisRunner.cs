#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
#endregion

namespace FastHashes.Analysis
{
    internal static class AnalysisRunner
    {
        #region Methods
        public static String Run(AnalysisOptions options)
        {
            if (options.Analysis != "smoke")
                throw new NotSupportedException($"Analysis '{options.Analysis}' is not implemented in AN-000. Available: smoke.");

            Hash hash = options.HashSeed.HasValue
                ? Hash.Create(options.HashName, options.HashSeed.Value)
                : Hash.Create(options.HashName);

            String root = Path.GetFullPath(options.OutputDirectory);
            String safeHashName = SanitizeDirectoryName(options.HashName);
            String runId = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8];
            String directory = Path.Combine(root, safeHashName, runId);
            Directory.CreateDirectory(directory);

            InputGenerator generator = new(options.GeneratorSeed);
            Byte[] key = new Byte[options.KeyLength];
            StringBuilder csv = new("Index,InputHex,HashHex\n");

            for (Int32 i = 0; i < options.Samples; ++i)
            {
                generator.NextBytes(key);
                Byte[] digest = hash.ComputeHashBytes(key);
                csv.Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
                   .Append(Convert.ToHexString(key)).Append(',')
                   .Append(Convert.ToHexString(digest)).Append('\n');
            }

            File.WriteAllText(Path.Combine(directory, "smoke.csv"), csv.ToString(), new UTF8Encoding(false));

            var metadata = new
            {
                SchemaVersion = 1,
                Analysis = "smoke",
                Status = "Completed",
                QualityAssessment = "NotPerformed",
                HashName = options.HashName,
                HashLengthBits = hash.Length,
                HashSeed = options.HashSeed,
                GeneratorAlgorithm = "SplitMix64",
                GeneratorSeed = options.GeneratorSeed,
                Samples = options.Samples,
                KeyLengthBytes = options.KeyLength,
                UtcTimestamp = DateTimeOffset.UtcNow,
                LibraryVersion = typeof(Hash).Assembly.GetName().Version?.ToString(),
                Runtime = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                OutputFiles = new[] { "smoke.csv" }
            };

            JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
            File.WriteAllText(Path.Combine(directory, "summary.json"), JsonSerializer.Serialize(metadata, jsonOptions) + "\n", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "summary.txt"),
                $"Hash: {options.HashName}\nAnalysis: smoke\nStatus: Completed\nQuality assessment: Not performed (AN-000)\nSamples: {options.Samples}\nKey length: {options.KeyLength} bytes\nGenerator seed: {options.GeneratorSeed}\nHash seed: {(options.HashSeed.HasValue ? options.HashSeed.Value.ToString(CultureInfo.InvariantCulture) : "default configuration")}\n",
                new UTF8Encoding(false));

            return directory;
        }

        private static String SanitizeDirectoryName(String name)
        {
            StringBuilder result = new(name.Length);

            foreach (Char character in name)
                result.Append(Char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '_');

            return result.Length == 0 ? "Unknown" : result.ToString();
        }
        #endregion
    }
}
