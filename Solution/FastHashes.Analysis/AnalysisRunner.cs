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
        public static String Run(String hashName, String analysis, UInt64? hashSeed, UInt64 generatorSeed, Int32 samples, Int32 keyLength, String outputDirectory)
        {
            if (analysis != "smoke")
                throw new NotSupportedException($"Analysis '{analysis}' is not implemented in AN-000. Available: smoke.");

            Hash hash = hashSeed.HasValue
                ? Hash.Create(hashName, hashSeed.Value)
                : Hash.Create(hashName);

            String root = Path.GetFullPath(outputDirectory);
            String safeHashName = SanitizeDirectoryName(hashName);
            String runId = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8];
            String directory = Path.Combine(root, safeHashName, runId);
            Directory.CreateDirectory(directory);

            InputGenerator generator = new(generatorSeed);
            Byte[] key = new Byte[keyLength];
            StringBuilder csv = new("Index,InputHex,HashHex\n");

            for (Int32 i = 0; i < samples; ++i)
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
                HashName = hashName,
                HashLengthBits = hash.Length,
                HashSeed = hashSeed,
                GeneratorAlgorithm = "SplitMix64",
                GeneratorSeed = generatorSeed,
                Samples = samples,
                KeyLengthBytes = keyLength,
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
                $"Hash: {hashName}\nAnalysis: smoke\nStatus: Completed\nQuality assessment: Not performed (AN-000)\nSamples: {samples}\nKey length: {keyLength} bytes\nGenerator seed: {generatorSeed}\nHash seed: {(hashSeed.HasValue ? hashSeed.Value.ToString(CultureInfo.InvariantCulture) : "default configuration")}\n",
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
