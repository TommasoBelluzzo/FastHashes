
#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ReferenceCase = (System.String Algorithm, System.String Name, System.String Seed, System.String Input, System.String Expected, System.String Provenance);
#endregion

namespace FastHashes.Tests
{
    public sealed class ConformanceFixture
    {
        #region Members
        private readonly Dictionary<String,List<ReferenceCase>> m_CasesByAlgorithm = new(StringComparer.Ordinal);
        #endregion

        #region Constructors
        public ConformanceFixture()
        {
            String directory = Path.Combine(AppContext.BaseDirectory, "Data", "Vectors");

            if (!Directory.Exists(directory))
                return;

            String[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.Ordinal);

            foreach (String file in files)
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
                JsonElement root = document.RootElement;

                String algorithm = RequiredString(root, "algorithm");
                JsonElement reference = root.GetProperty("reference");

                String repository = RequiredString(reference, "repository");
                String revision = RequiredString(reference, "revision");
                String entryPoint = RequiredString(reference, "entryPoint");

                String provenance = $"{repository} @ {revision} ({entryPoint})";

                if (!String.Equals(Path.GetFileNameWithoutExtension(file), algorithm, StringComparison.Ordinal))
                    throw new InvalidDataException($"Algorithm name does not match filename: {file}");

                if (!root.TryGetProperty("cases", out JsonElement testCases) || (testCases.ValueKind != JsonValueKind.Array))
                    throw new InvalidDataException($"Invalid reference cases in {file}");

                List<ReferenceCase> algorithmCases = new List<ReferenceCase>();

                foreach (JsonElement testCase in testCases.EnumerateArray())
                {
                    String name = RequiredString(testCase, "name");
                    String seed = RequiredString(testCase, "seed");
                    String input = RequiredString(testCase, "input", true);
                    String expected = RequiredString(testCase, "expected");

                    algorithmCases.Add((algorithm, name, seed, input, expected, provenance));
                }

                m_CasesByAlgorithm.Add(algorithm, algorithmCases);
            }
        }
        #endregion

        #region Methods
        private static String RequiredString(JsonElement element, String propertyName, Boolean allowEmpty = false)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) || (property.ValueKind != JsonValueKind.String))
                throw new InvalidDataException($"Missing or invalid JSON property: {propertyName}");

            String value = property.GetString();

            if ((value == null) || (!allowEmpty && String.IsNullOrWhiteSpace(value)))
                throw new InvalidDataException($"Missing or empty JSON property: {propertyName}");

            return value;
        }

        public IEnumerable<ReferenceCase> GetCases()
        {
            return m_CasesByAlgorithm.Values.SelectMany(cases => cases);
        }

        public IEnumerable<ReferenceCase> GetCases(String algorithm)
        {
            if (m_CasesByAlgorithm.TryGetValue(algorithm, out List<ReferenceCase> cases))
                return cases;

            return Array.Empty<ReferenceCase>();
        }
        #endregion
    }
}