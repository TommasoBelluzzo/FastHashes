#region Using Directives
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#endregion

namespace FastHashes.Analysis
{
    public static class Program
    {
        #region Setup
        private const String ANALYSIS = "all";
        private const UInt64 GENERATOR_SEED = 42ul;
        private const Int32 SAMPLES = 1000;
        private const Int32 SAMPLES_MINIMUM = 1000;
        private const Int32 SAMPLES_MAXIMUM = 50000;
        private const Int32 KEYS_LENGTH = 64;
        private const Int32 KEYS_LENGTH_MINIMUM = 0;
        private const Int32 KEYS_LENGTH_MAXIMUM = 4096;
        private const Int32 CORPUS_SIZE = 70000;
        private const Int32 CORPUS_SIZE_MINIMUM = 10000;
        private const Int32 CORPUS_SIZE_MAXIMUM = 1000000;

        private static readonly String[] s_Analyses = ["avalanche", "collision", "distribution", "independence", "structural"];
        #endregion

        #region Entry Point
        public static Int32 Main(String[] args)
        {
            RootCommand command = new("Analyzes hash algorithms using statistical quality and distribution tests.");

            Option<Boolean> listOption = new("--list")
            {
                Description = "Lists all registered algorithms without running an analysis."
            };

            Option<String> analysisOption = new("--analysis")
            {
                Description = $"The analysis to perform: all, {String.Join(", ", s_Analyses)}",
                DefaultValueFactory = _ => ANALYSIS
            };

            Option<String> hashOption = new("--hash")
            {
                Description = "The registered hash to analyze."
            };

            Option<UInt64?> hashSeedOption = new("--hash-seed")
            {
                Description = "Optional standardized hash seed; if omitted, the algorithm's default configuration is used."
            };

            Option<UInt64> generatorSeedOption = new("--generator-seed")
            {
                Description = "Deterministic input generator seed.",
                DefaultValueFactory = _ => GENERATOR_SEED
            };

            Option<Int32> samplesOption = new("--samples")
            {
                Description = $"Number of independent base inputs. [valid range: {SAMPLES_MINIMUM}-{SAMPLES_MAXIMUM}]",
                DefaultValueFactory = _ => SAMPLES
            };

            Option<Int32> keysLengthOption = new("--keys-length")
            {
                Description = $"Input keys length in bytes. [valid range: {KEYS_LENGTH_MINIMUM}-{KEYS_LENGTH_MAXIMUM}]",
                DefaultValueFactory = _ => KEYS_LENGTH
            };

            Option<Int32> corpusSizeOption = new("--corpus-size")
            {
                Description = $"Number of sequential-number and distinct random-byte keys for collision analysis. [valid range: {CORPUS_SIZE_MINIMUM}-{CORPUS_SIZE_MAXIMUM}]",
                DefaultValueFactory = _ => CORPUS_SIZE
            };

            hashOption.Validators.Add(result =>
            {
                String hashName = result.GetValueOrDefault<String>();

                if (String.IsNullOrWhiteSpace(hashName))
                    result.AddError("Parameter --hash requires a non-empty value.");
                else if (!Hash.GetAlgorithms().Contains(hashName))
                    result.AddError($"Unknown --hash parameter value: {hashName}; hash names are case-sensitive and must match the registry.");
            });

            analysisOption.Validators.Add(result =>
            {
                String analysisName = result.GetValueOrDefault<String>();

                if (String.IsNullOrWhiteSpace(analysisName))
                    result.AddError("Parameter --analysis requires a non-empty value.");
                else if ((analysisName != "all") && !s_Analyses.Contains(analysisName))
                    result.AddError($"Unknown --analysis parameter value: {analysisName}.");
            });

            samplesOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < SAMPLES_MINIMUM) || (value > SAMPLES_MAXIMUM))
                    result.AddError($"Parameter --samples must be between {SAMPLES_MINIMUM} and {SAMPLES_MAXIMUM}.");
            });

            keysLengthOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < KEYS_LENGTH_MINIMUM) || (value > KEYS_LENGTH_MAXIMUM))
                    result.AddError($"Parameter --keys-length must be between {KEYS_LENGTH_MINIMUM} and {KEYS_LENGTH_MAXIMUM}.");
            });

            corpusSizeOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < CORPUS_SIZE_MINIMUM) || (value > CORPUS_SIZE_MAXIMUM))
                    result.AddError($"Parameter --corpus-size must be between {CORPUS_SIZE_MINIMUM} and {CORPUS_SIZE_MAXIMUM}.");
            });

            command.Options.Add(listOption);
            command.Options.Add(analysisOption);
            command.Options.Add(hashOption);
            command.Options.Add(hashSeedOption);
            command.Options.Add(generatorSeedOption);
            command.Options.Add(samplesOption);
            command.Options.Add(keysLengthOption);
            command.Options.Add(corpusSizeOption);

            command.Validators.Add(result =>
            {
                if (!result.GetValue(listOption))
                {
                    if (result.GetResult(hashOption)?.Implicit != false)
                        result.AddError("Parameter --hash is required.");

                    return;
                }

                Option[] analysisOptions = [analysisOption, hashOption, hashSeedOption, generatorSeedOption, samplesOption, keysLengthOption, corpusSizeOption];

                foreach (Option option in analysisOptions)
                {
                    if (result.GetResult(option)?.Implicit == false)
                    {
                        result.AddError("Parameter --list cannot be combined with other options.");
                        return;
                    }
                }
            });

            command.SetAction
            (
                parseResult =>
                {
                    if (parseResult.GetValue(listOption))
                    {
                        foreach (String algorithm in Hash.GetAlgorithms())
                            Console.WriteLine(algorithm);

                        return 0;
                    }

                    return RunAnalysis
                    (
                        parseResult.GetValue(analysisOption),
                        parseResult.GetValue(hashOption),
                        parseResult.GetValue(hashSeedOption),
                        parseResult.GetValue(generatorSeedOption),
                        parseResult.GetValue(samplesOption),
                        parseResult.GetValue(keysLengthOption),
                        parseResult.GetValue(corpusSizeOption)
                    );
                }
            );

            return command.Parse(args).Invoke();
        }

        private static Int32 RunAnalysis(String analysisName, String hashName, UInt64? hashSeed, UInt64 generatorSeed, Int32 samples, Int32 keysLength, Int32 corpusSize)
        {
            Assembly hashAssembly = typeof(Hash).Assembly;
            String hashVersion = hashAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? hashAssembly.GetName().Version?.ToString()
                ?? "Unknown";

            Console.WriteLine("[SYSTEM CONFIGURATION]");
            Console.WriteLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            Console.WriteLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"FastHashes Version: {hashVersion}");
            Console.WriteLine();

            Console.WriteLine("[ANALYSIS CONFIGURATION]");
            Console.WriteLine($"Analysis: {analysisName}");
            Console.WriteLine($"Hash: {hashName} ({(hashSeed.HasValue ? hashSeed.Value.ToString() : "DEFAULT")})");
            Console.WriteLine($"Generator Seed: {generatorSeed}");
            Console.WriteLine($"Samples: {samples}");
            Console.WriteLine($"Keys Length: {keysLength} Bytes");
            Console.WriteLine($"Corpus Size: {corpusSize}");
            Console.WriteLine();

            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;

            String directory = null;
            List<(String Label, Boolean Completed, String Error)> results = [];

            foreach (String analysisNameCurrent in s_Analyses)
            {
                if ((analysisName != "all") && (analysisName != analysisNameCurrent))
                    continue;

                String label = ti.ToTitleCase(analysisNameCurrent);

                try
                {
                    Hash hash = hashSeed.HasValue ? Hash.Create(hashName, hashSeed.Value) : Hash.Create(hashName);

                    String result = analysisNameCurrent switch
                    {
                        "avalanche" => AvalancheAnalysis.Run(hash, hashName, hashSeed, generatorSeed, samples, keysLength, directory),
                        "collision" => CollisionAnalysis.Run(hash, hashName, hashSeed, generatorSeed, corpusSize, keysLength, directory),
                        "distribution" => DistributionAnalysis.Run(hash, hashName, hashSeed, generatorSeed, samples, keysLength, directory),
                        "independence" => IndependenceAnalysis.Run(hash, hashName, hashSeed, generatorSeed, samples, keysLength, directory),
                        "structural" => StructuralAnalysis.Run(hash, hashName, hashSeed, keysLength, directory),
                        _ => throw new NotSupportedException($"Unsupported analysis: {analysisNameCurrent}.")
                    };

                    directory ??= result;
                    results.Add((label, true, null));
                }
                catch (Exception exception)
                {
                    results.Add((label, false, exception.Message));
                }
            }

            foreach ((String label, Boolean completed, String error) in results)
                Console.WriteLine($"{label} Analysis: {(completed ? "Completed" : $"Failed ({error})")}");

            if (directory is not null)
                Console.WriteLine($"Output: {directory}");

            return ((results.Count > 0) && results.TrueForAll(status => status.Completed)) ? 0 : 1;
        }
        #endregion
    }
}