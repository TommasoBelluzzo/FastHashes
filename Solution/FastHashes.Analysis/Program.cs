#region Using Directives
using System;
#endregion

namespace FastHashes.Analysis
{
    public static class Program
    {
        #region Entry Point
        public static Int32 Main(String[] args)
        {
            try
            {
                AnalysisOptions options = AnalysisOptions.Parse(args);

                if (options.ShowHelp)
                {
                    PrintHelp();
                    return 0;
                }

                if (options.ListAlgorithms)
                {
                    foreach (String algorithm in Hash.GetAlgorithms())
                        Console.WriteLine(algorithm);

                    return 0;
                }

                String directory = AnalysisRunner.Run(options);
                Console.WriteLine($"Infrastructure smoke run completed: {directory}");
                Console.WriteLine("No statistical quality assessment has been performed.");
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

        #region Methods
        private static void PrintHelp()
        {
            Console.WriteLine("FastHashes.Analysis — AN-000 infrastructure");
            Console.WriteLine("Usage: dotnet run --project FastHashes.Analysis -- --hash NAME [options]");
            Console.WriteLine();
            Console.WriteLine("  --hash NAME            Registered algorithm name (required for a run)");
            Console.WriteLine("  --analysis NAME        smoke (default); quality analyses not implemented yet");
            Console.WriteLine("  --hash-seed UINT64      Optional algorithm seed (default: algorithm configuration)");
            Console.WriteLine("  --generator-seed UINT64 Deterministic input generator seed (default: 1)");
            Console.WriteLine("  --samples INT          Smoke samples, 1..10000 (default: 32)");
            Console.WriteLine("  --key-length INT       Key size in bytes, 0..4096 (default: 64)");
            Console.WriteLine("  --output DIRECTORY     Output root (default: Results)");
            Console.WriteLine("  --list                 List registered algorithms");
            Console.WriteLine("  --help                 Display usage");
        }
        #endregion
    }
}
