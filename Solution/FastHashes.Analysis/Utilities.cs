#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
#endregion

namespace FastHashes.Analysis
{
    internal static class Utilities
    {
        #region Constants
        private static readonly JsonSerializerOptions s_JsonOptions = new()
        {
            WriteIndented = true,
            IndentSize = 2,
            AllowTrailingCommas = false
        };
        #endregion

        #region Methods
        public static Dictionary<String, Object> CreateMetadata(String analysisName, Config config)
        {
            return new Dictionary<String, Object>
            {
                ["Analysis"] = analysisName,
                ["Hash"] = $"{config.HashName} ({(config.HashSeed.HasValue ? config.HashSeed.Value.ToString() : "DEFAULT")})",
                ["UtcTimestamp"] = DateTimeOffset.UtcNow,
                ["OperatingSystem"] = RuntimeInformation.OSDescription,
                ["OperatingSystemArchitecture"] = RuntimeInformation.OSArchitecture.ToString(),
                ["ProcessArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
                ["Runtime"] = RuntimeInformation.FrameworkDescription,
                ["LibraryVersion"] = config.LibraryVersion,
                ["GeneratorSeed"] = config.GeneratorSeed,
                ["Samples"] = config.Samples,
                ["KeysLength"] = config.KeysLength,
                ["CorpusSize"] = config.CorpusSize,
                ["Significance"] = config.Significance
            };
        }

        public static String CreateRunDirectory(String hashName)
        {
            StringBuilder hashReferenceBuilder = new(hashName.Length);

            foreach (Char character in hashName)
                hashReferenceBuilder.Append((Char.IsLetterOrDigit(character) || (character == '-') || (character == '_')) ? character : '_');

            String hashReference = (hashReferenceBuilder.Length == 0) ? "Unknown" : hashReferenceBuilder.ToString();

            String root = Path.Combine(Environment.CurrentDirectory, "Analysis", hashReference);
            String runId = DateTimeOffset.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            String directory = Path.Combine(root, runId);
            String reservation = directory + ".lock";

            Directory.CreateDirectory(root);

            using FileStream reservationStream = new(reservation, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            
            try
            {
                if (Directory.Exists(directory))
                    throw new IOException($"Analysis directory already exists: {directory}.");

                Directory.CreateDirectory(directory);
                
                return directory;
            }
            finally
            {
                reservationStream.Dispose();
                File.Delete(reservation);
            }
        }

        public static String FormatCsvDouble(Double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        public static String FormatCsvInteger(Int32 value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static String FormatCsvInteger(Int64 value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static String SerializeJson(Object value)
        {
            return JsonSerializer.Serialize(value, s_JsonOptions);
        }

        public static String WriteArtifacts(String hashName, Action<String> writeFunction, String existingDirectory = null)
        {
            Boolean created = existingDirectory == null;
            String directory = created ? CreateRunDirectory(hashName) : existingDirectory;
            HashSet<String> existingFiles = created ? [] : new(Directory.GetFiles(directory), StringComparer.OrdinalIgnoreCase);

            try
            {
                writeFunction(directory);
                return directory;
            }
            catch
            {
                if (created)
                {
                    try { Directory.Delete(directory, true); }
                    catch { }
                }
                else
                {
                    foreach (String file in Directory.GetFiles(directory))
                    {
                        if (existingFiles.Contains(file))
                            continue;

                        try { File.Delete(file); }
                        catch { }
                    }
                }

                throw;
            }
        }
        #endregion
    }
}