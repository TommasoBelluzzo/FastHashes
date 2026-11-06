#region Using Directives
using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#endregion

namespace FastHashes.Analysis
{
    public static class Program
    {
        #region Setup
        private const UInt64 GENERATOR_SEED = 1ul;
        private const Int32 SAMPLES = 1000;
        private const Int32 SAMPLES_MINIMUM = 1;
        private const Int32 SAMPLES_MAXIMUM = 10000;
        private const Int32 KEY_LENGTH = 64;
        private const Int32 CORPUS_SIZE = 65536;
        private const Int32 KEY_LENGTH_MINIMUM = 0;
        private const Int32 KEY_LENGTH_MAXIMUM = 4096;
        #endregion

        #region Entry Point
        public static Int32 Main(String[] args)
        {
            RootCommand command = new("Analyzes hash algorithms using statistical quality and distribution tests.");

            Option<Boolean> listOption = new("--list")
            {
                Description = "Lists all registered algorithms without running an analysis."
            };

            Option<String> hashOption = new("--hash")
            {
                Description = "The registered hash to analyze."
            };

            Option<String> analysisOption = new("--analysis")
            {
                Description = "The analysis to perform: avalanche, independence, collision, distribution, structural or smoke."
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

            Option<Int32> keyLengthOption = new("--key-length")
            {
                Description = $"Input key length in bytes. [valid range: {KEY_LENGTH_MINIMUM}-{KEY_LENGTH_MAXIMUM}]",
                DefaultValueFactory = _ => KEY_LENGTH
            };

            Option<Int32> corpusSizeOption = new("--corpus-size")
            {
                Description = "Number of sequential-number and distinct random-byte keys for collision analysis. [valid range: 1-1000000]",
                DefaultValueFactory = _ => CORPUS_SIZE
            };

            hashOption.Validators.Add(result =>
            {
                String hashName = result.GetValueOrDefault<String>();

                if (String.IsNullOrWhiteSpace(hashName))
                    result.AddError("Parameter --hash requires a non-empty value.");
                else if (!Hash.GetAlgorithms().Contains(hashName))
                    result.AddError($"Unknown hash: {hashName}; hash names are case-sensitive and must match the registry.");
            });

            analysisOption.Validators.Add(result =>
            {
                String analysisName = result.GetValueOrDefault<String>();

                if (String.IsNullOrWhiteSpace(analysisName))
                    result.AddError("Parameter --analysis requires a non-empty value.");
                else if ((analysisName != "avalanche") && (analysisName != "independence") && (analysisName != "collision") && (analysisName != "distribution") && (analysisName != "structural") && (analysisName != "smoke"))
                    result.AddError($"Unknown --analysis parameter value: {analysisName}.");
            });

            samplesOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < SAMPLES_MINIMUM) || (value > SAMPLES_MAXIMUM))
                    result.AddError($"--samples must be between {SAMPLES_MINIMUM} and {SAMPLES_MAXIMUM}.");
            });

            keyLengthOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < KEY_LENGTH_MINIMUM) || (value > KEY_LENGTH_MAXIMUM))
                    result.AddError($"--key-length must be between {KEY_LENGTH_MINIMUM} and {KEY_LENGTH_MAXIMUM}.");
            });

            corpusSizeOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();
                if ((value < 1) || (value > 1000000))
                    result.AddError("--corpus-size must be between 1 and 1000000.");
            });

            command.Options.Add(listOption);
            command.Options.Add(hashOption);
            command.Options.Add(analysisOption);
            command.Options.Add(hashSeedOption);
            command.Options.Add(generatorSeedOption);
            command.Options.Add(samplesOption);
            command.Options.Add(keyLengthOption);
            command.Options.Add(corpusSizeOption);

            command.Validators.Add(result =>
            {
                if (!result.GetValue(listOption))
                {
                    if (result.GetResult(hashOption)?.Implicit != false)
                        result.AddError("Parameter --hash is required.");

                    if (result.GetResult(analysisOption)?.Implicit != false)
                        result.AddError("Parameter --analysis is required.");

                    if (result.GetResult(corpusSizeOption)?.Implicit == false && result.GetValue(analysisOption) != "collision")
                        result.AddError("Parameter --corpus-size is only valid for collision analysis.");

                    if (result.GetValue(analysisOption) == "structural")
                    {
                        if (result.GetResult(samplesOption)?.Implicit == false || result.GetResult(generatorSeedOption)?.Implicit == false)
                            result.AddError("Parameters --samples and --generator-seed are not applicable to structural analysis.");
                    }

                    return;
                }

                Option[] analysisOptions = [hashOption, analysisOption, hashSeedOption, generatorSeedOption, samplesOption, keyLengthOption, corpusSizeOption];

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
                        parseResult.GetValue(hashOption),
                        parseResult.GetValue(analysisOption),
                        parseResult.GetValue(hashSeedOption),
                        parseResult.GetValue(generatorSeedOption),
                        parseResult.GetValue(samplesOption),
                        parseResult.GetValue(keyLengthOption),
                        parseResult.GetValue(corpusSizeOption)
                    );
                }
            );

            return command.Parse(args).Invoke();
        }
        #endregion

        #region Methods
        private static Int32 RunAnalysis(String selectedHash, String analysis, UInt64? hashSeed, UInt64 generatorSeed, Int32 samples, Int32 keyLength, Int32 corpusSize)
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
            Console.WriteLine($"Hash: {selectedHash}");
            Console.WriteLine($"Analysis: {analysis}");
            Console.WriteLine($"Hash Seed: {(hashSeed.HasValue ? hashSeed.Value.ToString() : "Default Configuration")}");
            Console.WriteLine(analysis == "structural" ? "Generator: Deterministic structural probes" : "Generator: SplitMix64");
            if (analysis != "structural")
            {
                Console.WriteLine($"Generator Seed: {generatorSeed}");
                Console.WriteLine($"Samples: {samples}");
            }
            Console.WriteLine($"Key Length: {keyLength} Bytes");
            if (analysis == "collision")
                Console.WriteLine($"Corpus Size (numbers/random): {corpusSize}");
            Console.WriteLine($"Output Root: {System.IO.Path.Combine(Environment.CurrentDirectory, "Analysis")}");
            Console.WriteLine();

            String title = $"# HASH: {selectedHash} #";
            String frame = new('#', title.Length);

            Console.WriteLine(frame);
            Console.WriteLine(title);
            Console.WriteLine(frame);
            Console.WriteLine();

            try
            {
                String directory = AnalysisRunner.Run(selectedHash, analysis, hashSeed, generatorSeed, samples, keyLength, corpusSize);

                Console.WriteLine(analysis == "avalanche" ? "[AVALANCHE ANALYSIS]" : analysis == "independence" ? "[BIT INDEPENDENCE ANALYSIS]" : analysis == "collision" ? "[COLLISION ANALYSIS]" : analysis == "distribution" ? "[OUTPUT DISTRIBUTION ANALYSIS]" : analysis == "structural" ? "[STRUCTURAL ROBUSTNESS ANALYSIS]" : "[INFRASTRUCTURE SMOKE TEST]");
                Console.WriteLine($"Output: {directory}");
                if (analysis == "smoke")
                    Console.WriteLine("Quality Assessment: Not Performed (AN-000)");
                else if (analysis == "avalanche")
                    Console.WriteLine("Results: avalanche.csv, avalanche-hamming.csv, avalanche-heatmap.svg, avalanche-hamming.svg, avalanche.json, avalanche.txt");
                else if (analysis == "independence")
                    Console.WriteLine("Results: independence.csv, independence-pairs.csv, independence-heatmap.svg, independence.json, independence.txt");
                else if (analysis == "collision")
                    Console.WriteLine("Results: collision-summary.csv, collision-buckets.csv, collision-examples.csv, six occupancy maps, collision.json, collision.txt");
                else if (analysis == "structural")
                    Console.WriteLine("Results: structural-summary.csv, eight structural family CSVs, structural.json, structural.txt");
                else
                    Console.WriteLine("Results: distribution-bits.csv, distribution-bytes.csv, distribution-positions.csv, three distribution SVG plots, distribution.json, distribution.txt");

                return 0;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is OverflowException)
            {
                Console.Error.WriteLine($"Error: {exception.Message}");
                return 2;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Error: {exception}");
                return 1;
            }
        }
        #endregion
    }
}