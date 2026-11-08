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
        private const Double SIGNIFICANCE = 0.05d;
        private const Double SIGNIFICANCE_MINIMUM = 0.01d;
        private const Double SIGNIFICANCE_MAXIMUM = 0.10d;
        private const Int32 KEYS_LENGTH = 64;
        private const Int32 KEYS_LENGTH_MINIMUM = 16;
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
                DefaultValueFactory = _ => ANALYSIS,
                Validators =
                {
                    result =>
                    {
                        String analysisName = result.GetValueOrDefault<String>();

                        if (String.IsNullOrWhiteSpace(analysisName))
                            result.AddError("Parameter --analysis requires a non-empty value.");
                        else if ((analysisName != "all") && !s_Analyses.Contains(analysisName))
                            result.AddError($"Unknown --analysis parameter value: {analysisName}.");
                    }
                }
            };

            Option<String> hashOption = new("--hash")
            {
                Description = "The registered hash to analyze.",
                Validators =
                {
                    result =>
                    {
                        String hashName = result.GetValueOrDefault<String>();

                        if (String.IsNullOrWhiteSpace(hashName))
                            result.AddError("Parameter --hash requires a non-empty value.");
                        else if (!Hash.GetAlgorithms().Contains(hashName))
                            result.AddError($"Unknown --hash parameter value: {hashName}; hash names are case-sensitive and must match the registry.");
                    }
                }
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
                DefaultValueFactory = _ => SAMPLES,
                Validators =
                {
                    result =>
                    {
                        Int32 value = result.GetValueOrDefault<Int32>();

                        if ((value < SAMPLES_MINIMUM) || (value > SAMPLES_MAXIMUM))
                            result.AddError($"Value of parameter --samples must be between {SAMPLES_MINIMUM} and {SAMPLES_MAXIMUM}.");
                    }
                }
            };

            Option<Int32> keysLengthOption = new("--keys-length")
            {
                Description = $"Input keys length in bytes. [valid range: {KEYS_LENGTH_MINIMUM}-{KEYS_LENGTH_MAXIMUM}]",
                DefaultValueFactory = _ => KEYS_LENGTH,
                Validators =
                {
                    result =>
                    {
                        Int32 value = result.GetValueOrDefault<Int32>();

                        if ((value < KEYS_LENGTH_MINIMUM) || (value > KEYS_LENGTH_MAXIMUM))
                            result.AddError($"Value of parameter --keys-length must be between {KEYS_LENGTH_MINIMUM} and {KEYS_LENGTH_MAXIMUM}.");
                    }
                }
            };

            Option<Double> significanceOption = new("--significance")
            {
                Description = $"Statistical significance level used for hypothesis testing. [valid range: {SIGNIFICANCE_MINIMUM:0.##}-{SIGNIFICANCE_MAXIMUM:0.##}]",
                DefaultValueFactory = _ => SIGNIFICANCE,
                Validators =
                {
                    result =>
                    {
                        Double value = result.GetValueOrDefault<Double>();

                        if (!Double.IsFinite(value) || (value < SIGNIFICANCE_MINIMUM) || (value > SIGNIFICANCE_MAXIMUM))
                            result.AddError($"Value of parameter --significance must be finite, and between {SIGNIFICANCE_MINIMUM:0.##} and {SIGNIFICANCE_MAXIMUM:0.##}.");
                    }
                }
            };

            Option<Int32> corpusSizeOption = new("--corpus-size")
            {
                Description = $"Number of sequential-number and distinct random-byte keys for collision analysis. [valid range: {CORPUS_SIZE_MINIMUM}-{CORPUS_SIZE_MAXIMUM}]",
                DefaultValueFactory = _ => CORPUS_SIZE,
                Validators =
                {
                    result =>
                    {
                        Int32 value = result.GetValueOrDefault<Int32>();

                        if ((value < CORPUS_SIZE_MINIMUM) || (value > CORPUS_SIZE_MAXIMUM))
                            result.AddError($"Value of parameter --corpus-size must be between {CORPUS_SIZE_MINIMUM} and {CORPUS_SIZE_MAXIMUM}.");
                    }
                }
            };

            command.Options.Add(listOption);
            command.Options.Add(analysisOption);
            command.Options.Add(hashOption);
            command.Options.Add(hashSeedOption);
            command.Options.Add(generatorSeedOption);
            command.Options.Add(samplesOption);
            command.Options.Add(keysLengthOption);
            command.Options.Add(significanceOption);
            command.Options.Add(corpusSizeOption);

            command.Validators.Add(result =>
            {
                if (!result.GetValue(listOption))
                {
                    if (result.GetResult(hashOption)?.Implicit != false)
                        result.AddError("Parameter --hash is required.");

                    return;
                }

                Option[] analysisOptions = [analysisOption, hashOption, hashSeedOption, generatorSeedOption, samplesOption, keysLengthOption, significanceOption, corpusSizeOption];

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

                    Config config = new
                    (
                        String.Empty,
                        parseResult.GetValue(analysisOption),
                        null,
                        parseResult.GetValue(hashOption),
                        parseResult.GetValue(hashSeedOption),
                        parseResult.GetValue(generatorSeedOption),
                        parseResult.GetValue(samplesOption),
                        parseResult.GetValue(keysLengthOption),
                        parseResult.GetValue(significanceOption),
                        parseResult.GetValue(corpusSizeOption)
                    );

                    return RunAnalysis(config);
                }
            );

            return command.Parse(args).Invoke();
        }

        private static Int32 RunAnalysis(Config config)
        {
            Assembly assembly = typeof(Hash).Assembly;
            String libraryVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "Unknown";

            Hash hash = config.HashSeed.HasValue
                ? Hash.Create(config.HashName, config.HashSeed.Value)
                : Hash.Create(config.HashName);

            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;

            config = config with { LibraryVersion = libraryVersion, Hash = hash };

            Console.WriteLine("[SYSTEM CONFIGURATION]");
            Console.WriteLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            Console.WriteLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"Library Version: {libraryVersion}");
            Console.WriteLine();

            Console.WriteLine("[ANALYSIS CONFIGURATION]");
            Console.WriteLine($"Analysis: {config.AnalysisName}");
            Console.WriteLine($"Hash: {config.HashName} ({(config.HashSeed.HasValue ? config.HashSeed.Value.ToString() : "DEFAULT")})");
            Console.WriteLine($"Generator Seed: {config.GeneratorSeed}");
            Console.WriteLine($"Samples: {config.Samples}");
            Console.WriteLine($"Keys Length: {config.KeysLength} Bytes");
            Console.WriteLine($"Significance: {config.Significance:0.##}");
            Console.WriteLine($"Corpus Size: {config.CorpusSize}");
            Console.WriteLine();
            
            String directory = null;
            List<Result> results = [];

            foreach (String analysisNameCurrent in s_Analyses)
            {
                if ((config.AnalysisName != "all") && (config.AnalysisName != analysisNameCurrent))
                    continue;

                String label = ti.ToTitleCase(analysisNameCurrent);

                try
                {
                    String result = analysisNameCurrent switch
                    {
                        "avalanche" => AvalancheAnalysis.Run(config, directory),
                        "collision" => CollisionAnalysis.Run(config, directory),
                        "distribution" => DistributionAnalysis.Run(config, directory),
                        "independence" => IndependenceAnalysis.Run(config, directory),
                        "structural" => StructuralAnalysis.Run(config, directory),
                        _ => throw new NotSupportedException($"Unsupported analysis: {analysisNameCurrent}.")
                    };

                    directory ??= result;
                    results.Add(new (label, true, null));
                }
                catch (Exception exception)
                {
                    results.Add(new (label, false, exception.Message));
                }
            }

            foreach (Result result in results)
                Console.WriteLine($"{result.Label} Analysis: {(result.Completed ? "Completed" : $"Failed ({result.Error})")}");

            Console.WriteLine($"Output: {directory ?? "Not Created"}");

            return ((results.Count > 0) && results.TrueForAll(status => status.Completed)) ? 0 : 1;
        }
        #endregion
    }
}