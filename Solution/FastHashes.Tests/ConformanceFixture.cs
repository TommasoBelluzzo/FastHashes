#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
#endregion

namespace FastHashes.Tests
{
    public sealed class ConformanceFixture
    {
        #region Constants
        private const String VECTORS_DIRECTORY = "ReferenceVectors";
        #endregion

        #region Members
        private readonly IEnumerable<ReferenceCase> m_Cases;
        #endregion

        #region Properties
        public IEnumerable<ReferenceCase> Cases => m_Cases;
        #endregion

        #region Constructors
        public ConformanceFixture()
        {
            String directory = Path.Combine(AppContext.BaseDirectory, "Data", VECTORS_DIRECTORY);

            if (!Directory.Exists(directory))
            {
                m_Cases = Array.Empty<ReferenceCase>();
                return;
            }

            String[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.Ordinal);

            if (files.Length == 0)
            {
                m_Cases = Array.Empty<ReferenceCase>();
                return;
            }

            List<ReferenceCase> casesList = new List<ReferenceCase>();

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

                foreach (JsonElement testCase in testCases.EnumerateArray())
                {
                    String name = RequiredString(testCase, "name");
                    String seed = RequiredString(testCase, "seed");
                    String input = RequiredString(testCase, "input", true);
                    String expected = RequiredString(testCase, "expected");

                    casesList.Add(new ReferenceCase(algorithm, name, seed, input, expected, provenance));
                }
            }

            m_Cases = casesList;
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

        public static Hash CreateHash(String algorithm, String seed)
        {
            UInt64 seedValue = UInt64.Parse(seed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? seed.Substring(2) : seed, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            switch (algorithm)
            {
                case "xxHash32":
                    return new XxHash32(checked((UInt32)seedValue));

                case "xxHash64":
                    return new XxHash64(seedValue);

                default:
                    throw new NotSupportedException($"Unknown reference algorithm: {algorithm}");
            }
        }
        #endregion

        #region Nested Classes
        public sealed class ReferenceCase
        {
            #region Properties
            public String Algorithm { get; }
            public String Name { get; }
            public String Seed { get; }
            public String Input { get; }
            public String Expected { get; }
            public String Provenance { get; }
            #endregion

            #region Constructors
            public ReferenceCase(String algorithm, String name, String seed, String input, String expected, String provenance)
            {
                Algorithm = algorithm;
                Name = name;
                Seed = seed;
                Input = input;
                Expected = expected;
                Provenance = provenance;
            }
            #endregion
        }
        #endregion
    }
}