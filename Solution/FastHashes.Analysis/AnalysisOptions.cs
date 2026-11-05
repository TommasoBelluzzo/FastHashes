#region Using Directives
using System;
using System.Globalization;
#endregion

namespace FastHashes.Analysis
{
    internal sealed class AnalysisOptions
    {
        #region Properties
        public String HashName { get; private set; }
        public UInt64? HashSeed { get; private set; }
        public UInt64 GeneratorSeed { get; private set; } = 1ul;
        public String Analysis { get; private set; } = "smoke";
        public Int32 Samples { get; private set; } = 32;
        public Int32 KeyLength { get; private set; } = 64;
        public String OutputDirectory { get; private set; } = "Results";
        public Boolean ShowHelp { get; private set; }
        public Boolean ListAlgorithms { get; private set; }
        #endregion

        #region Methods
        public static AnalysisOptions Parse(String[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            AnalysisOptions options = new();

            for (Int32 i = 0; i < args.Length; ++i)
            {
                String argument = args[i];

                if (argument == "--help" || argument == "-h")
                {
                    options.ShowHelp = true;
                    continue;
                }

                if (argument == "--list")
                {
                    options.ListAlgorithms = true;
                    continue;
                }

                if (i == args.Length - 1)
                    throw new ArgumentException($"Missing value for '{argument}'.");

                String value = args[++i];

                switch (argument)
                {
                    case "--hash":
                        options.HashName = value;
                        break;
                    case "--hash-seed":
                        options.HashSeed = ParseUInt64(value, argument);
                        break;
                    case "--generator-seed":
                        options.GeneratorSeed = ParseUInt64(value, argument);
                        break;
                    case "--analysis":
                        options.Analysis = value.ToLowerInvariant();
                        break;
                    case "--samples":
                        options.Samples = ParseInt32(value, argument, 1, 10000);
                        break;
                    case "--key-length":
                        options.KeyLength = ParseInt32(value, argument, 0, 4096);
                        break;
                    case "--output":
                        options.OutputDirectory = value;
                        break;
                    default:
                        throw new ArgumentException($"Unknown option '{argument}'.");
                }
            }

            if (!options.ShowHelp && !options.ListAlgorithms && String.IsNullOrWhiteSpace(options.HashName))
                throw new ArgumentException("The --hash option is required.");

            if (String.IsNullOrWhiteSpace(options.OutputDirectory))
                throw new ArgumentException("The output directory cannot be empty.");

            return options;
        }

        private static UInt64 ParseUInt64(String value, String option)
        {
            if (!UInt64.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out UInt64 result))
                throw new ArgumentException($"'{option}' requires an unsigned 64-bit decimal integer.");

            return result;
        }

        private static Int32 ParseInt32(String value, String option, Int32 minimum, Int32 maximum)
        {
            if (!Int32.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out Int32 result) || result < minimum || result > maximum)
                throw new ArgumentException($"'{option}' requires an integer between {minimum} and {maximum}.");

            return result;
        }
        #endregion
    }
}
