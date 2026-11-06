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
        public static String Run(String hashName, String analysis, UInt64? hashSeed, UInt64 generatorSeed, Int32 samples, Int32 keyLength, Int32 corpusSize)
        {
            if ((analysis != "smoke") && (analysis != "avalanche") && (analysis != "independence") && (analysis != "collision"))
                throw new NotSupportedException($"Analysis '{analysis}' is not implemented.");

            Hash hash = hashSeed.HasValue
                ? Hash.Create(hashName, hashSeed.Value)
                : Hash.Create(hashName);

            // Validate the workload before creating any output files.
            if (analysis == "avalanche")
                AvalancheAnalysis.Validate(hash.Length, samples, keyLength);
            else if (analysis == "independence")
                IndependenceAnalysis.Validate(hash.Length, samples, keyLength);

            if (analysis == "collision")
                return CollisionAnalysis.Run(hash, hashName, hashSeed, generatorSeed, corpusSize, keyLength);

            if (analysis == "avalanche")
                return AvalancheAnalysis.Run(hash, hashName, hashSeed, generatorSeed, samples, keyLength);

            if (analysis == "independence")
                return IndependenceAnalysis.Run(hash, hashName, hashSeed, generatorSeed, samples, keyLength);

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
                OutputFiles = new[] { "smoke.csv", "smoke.json", "smoke.txt" }
            };

            return WriteArtifacts(hashName, directory =>
            {
                File.WriteAllText(Path.Combine(directory, "smoke.csv"), csv.ToString(), new UTF8Encoding(false));
                JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
                File.WriteAllText(Path.Combine(directory, "smoke.json"), JsonSerializer.Serialize(metadata, jsonOptions) + "\n", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(directory, "smoke.txt"),
                    $"Hash: {hash.GetType().Name}\nAnalysis: smoke\nStatus: Completed\nQuality assessment: Not performed (AN-000)\nSamples: {samples}\nKey length: {keyLength} bytes\nGenerator seed: {generatorSeed}\n",
                    new UTF8Encoding(false));
            });
        }

        // Artifact directories are created only after computations succeed.
        // On write failure, only this invocation's newly created directory is removed.
        internal static String WriteArtifacts(String hashName, Action<String> write)
        {
            String root = Path.Combine(Environment.CurrentDirectory, "Analysis");
            String safeHashName = SanitizeDirectoryName(hashName);
            String runId = DateTimeOffset.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            String directory = Path.Combine(root, safeHashName, runId);

            if (Directory.Exists(directory))
                throw new IOException($"Analysis directory already exists: {directory}. Re-run after the next second to avoid overwriting results.");

            Directory.CreateDirectory(directory);

            try
            {
                write(directory);
                return directory;
            }
            catch
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException)
                {
                    // Preserve the original reporting failure.
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve the original reporting failure.
                }

                throw;
            }
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
