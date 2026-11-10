
#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
#endregion

namespace FastHashes.Tests
{
    public readonly record struct ConformanceCase
    (
        String Algorithm,
        String Name,
        String Seed,
        String Input,
        String Expected,
        String Provenance
    );

    public sealed class ConformanceFixture
    {
        #region Members
        private readonly Dictionary<String, List<ConformanceCase>> m_CasesByAlgorithm = new(StringComparer.Ordinal);
        #endregion

        #region Constructors
        public ConformanceFixture()
        {
            String directory = Path.Combine(AppContext.BaseDirectory, "Data");

            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Data directory could not be found: {directory}.");

            HashSet<String> registered = new(Hash.GetAlgorithms(), StringComparer.Ordinal);

            String[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.Ordinal);

            foreach (String file in files)
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));

                JsonElement root = document.RootElement;

                String algorithm = RequiredString(file, root, "algorithm");

                if (!root.TryGetProperty("reference", out JsonElement reference) || (reference.ValueKind != JsonValueKind.Object))
                    throw new InvalidDataException($"Missing or invalid property 'reference': {file}.");

                String repository = RequiredString(file, reference, "repository");
                String revision = RequiredString(file, reference, "revision");
                String entryPoint = RequiredString(file, reference, "entryPoint");

                String provenance = $"{repository} @ {revision} ({entryPoint})";

                if (!String.Equals(Path.GetFileNameWithoutExtension(file), algorithm, StringComparison.Ordinal))
                    throw new InvalidDataException($"Algorithm name does not match filename: {file}.");

                if (!registered.Remove(algorithm))
                    throw new InvalidDataException($"Unregistered algorithm: {file}.");

                if (!root.TryGetProperty("cases", out JsonElement testCases) || (testCases.ValueKind != JsonValueKind.Array))
                    throw new InvalidDataException($"Missing or invalid property 'cases': {file}.");

                if (testCases.GetArrayLength() == 0)
                    throw new InvalidDataException($"Empty property 'cases': {file}.");

                Int32 hashLength = Hash.Create(algorithm).Length / 8;

                List<ConformanceCase> algorithmCases = [];
                HashSet<String> names = new(StringComparer.Ordinal);

                foreach (JsonElement testCase in testCases.EnumerateArray())
                {
                    String name = RequiredString(file, testCase, "name");
                    String seed = RequiredString(file, testCase, "seed");
                    String input = RequiredString(file, testCase, "input", true);
                    String expected = RequiredString(file, testCase, "expected");

                    if (!names.Add(name))
                        throw new InvalidDataException($"Duplicate reference case '{name}': {file}.");

                    String seedValue = seed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? seed[2..] : seed;

                    if (!UInt64.TryParse(seedValue, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _))
                        throw new InvalidDataException($"Invalid seed in case '{name}': {file}.");

                    Byte[] expectedBytes;

                    try
                    {
                        Convert.FromHexString(input);
                        expectedBytes = Convert.FromHexString(expected);
                    }
                    catch (FormatException exception)
                    {
                        throw new InvalidDataException($"Invalid hexadecimal data in case '{name}': {file}.", exception);
                    }

                    if (expectedBytes.Length != hashLength)
                        throw new InvalidDataException($"Invalid expected hash length in case '{name}': {file}.");

                    algorithmCases.Add(new(algorithm, name, seed, input, expected, provenance));
                }

                m_CasesByAlgorithm.Add(algorithm, algorithmCases);
            }

            if (registered.Count > 0)
                throw new InvalidDataException($"Missing conformance files for registered algorithms: {String.Join(", ", registered.OrderBy(x => x, StringComparer.Ordinal))}.");
        }
        #endregion

        #region Methods
        private static String RequiredString(String file, JsonElement element, String propertyName, Boolean allowEmpty = false)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) || (property.ValueKind != JsonValueKind.String))
                throw new InvalidDataException($"Missing or invalid property '{propertyName}': {file}.");

            String value = property.GetString();

            if ((value == null) || (!allowEmpty && String.IsNullOrWhiteSpace(value)))
                throw new InvalidDataException($"Missing or empty property '{propertyName}': {file}.");

            return value;
        }

        public IEnumerable<ConformanceCase> GetCases()
        {
            return m_CasesByAlgorithm.Values.SelectMany(cases => cases);
        }

        public IEnumerable<ConformanceCase> GetCases(String algorithm)
        {
            if (m_CasesByAlgorithm.TryGetValue(algorithm, out List<ConformanceCase> cases))
                return cases;

            return [];
        }
        #endregion
    }
}