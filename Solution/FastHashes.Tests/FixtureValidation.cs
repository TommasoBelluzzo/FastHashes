#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
#endregion

namespace FastHashes.Tests
{
    public sealed class FixtureValidation
    {
        #region Members
        private readonly Dictionary<String, String> m_Cases = new(StringComparer.Ordinal);
        #endregion

        #region Constructors
        public FixtureValidation()
        {
            String filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Validation.json");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Validation file could not be found: {filePath}.", filePath);

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(filePath));
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException($"Malformed validation file content: {filePath}.");

            HashSet<String> registered = new(Hash.GetAlgorithms(), StringComparer.Ordinal);

            foreach (JsonProperty property in root.EnumerateObject())
            {
                String algorithm = property.Name;

                if (!registered.Remove(algorithm))
                    throw new InvalidDataException($"Duplicate or unregistered algorithm '{algorithm}': {filePath}.");

                if (property.Value.ValueKind != JsonValueKind.String)
                    throw new InvalidDataException($"Invalid value for '{algorithm}': {filePath}.");

                String expected = property.Value.GetString();
                Int32 hashLength = Hash.Create(algorithm).Length / 8;

                if ((expected == null) || !expected.StartsWith("0x", StringComparison.Ordinal) || (expected.Length != (2 + (hashLength * 2))))
                    throw new InvalidDataException($"Invalid value for '{algorithm}': {filePath}.");

                try
                {
                    Convert.FromHexString(expected[2..]);
                }
                catch (FormatException exception)
                {
                    throw new InvalidDataException($"Invalid value for '{algorithm}': {filePath}.", exception);
                }

                m_Cases.Add(algorithm, expected);
            }

            if (registered.Count > 0)
                throw new InvalidDataException($"Missing values for registered algorithms: {String.Join(", ", registered.OrderBy(x => x, StringComparer.Ordinal))}.");
        }
        #endregion

        #region Methods
        public String GetExpected(String algorithm)
        {
            return m_Cases[algorithm];
        }
        #endregion
    }
}
