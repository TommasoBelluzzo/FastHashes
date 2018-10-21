#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
#endregion

namespace FastHashes.Tests
{
    public static class CommandLineUtilities
    {
        #region Members
        private static readonly Regex s_CommandRegex = new Regex(@"^-([A-Za-z0-9]+)$", RegexOptions.Compiled);
        #endregion

        #region Methods
        public static Dictionary<String,String[]> ParseArguments(String[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            Dictionary<String,String[]> arguments = new Dictionary<String,String[]>();

            String currentName = String.Empty;
            List<String> values = new List<String>();

            for (Int32 i = 0; i < args.Length; ++i)
            {
                String arg = args[i];
                Match match = s_CommandRegex.Match(arg);

                if (match.Success)
                {
                    if (!String.IsNullOrEmpty(currentName))
                        arguments[currentName] = values.ToArray();

                    values.Clear();

                    currentName = match.Groups[1].Value;
                }
                else if (String.IsNullOrEmpty(currentName))
                    arguments[arg] = new String[0];
                else
                    values.Add(arg);
            }

            if (!String.IsNullOrEmpty(currentName))
                arguments[currentName] = values.ToArray();

            return arguments;
        }

        public static String TryGetHashes(Dictionary<String,String[]> arguments, HashInfo[] hashInfos, out String[] hashes)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            if (hashInfos == null)
                throw new ArgumentNullException(nameof(hashInfos));

            hashes = new String[0];

            String[] argumentHashes = arguments["hashes"];

            if (argumentHashes.Length == 0)
                return "ERROR: no hashes have been specified.";

            List<String> hashesList;

            if ((argumentHashes.Length == 1) && String.Equals(argumentHashes[0], "ALL", StringComparison.Ordinal))
            {
                hashesList = new List<String>(hashInfos.Length);

                for (Int32 i = 0; i < hashInfos.Length; ++i)
                    hashesList.Add(hashInfos[i].Name);
            }
            else
            {
                hashesList = new List<String>(argumentHashes.Length);

                for (Int32 i = 0; i < argumentHashes.Length; ++i)
                {
                    String hashName = argumentHashes[i];
                    Boolean hashFound = false;

                    for (Int32 j = 0; j < hashInfos.Length; ++j)
                    {
                        String hashInfoName = hashInfos[j].Name;

                        if (String.Equals(hashName, hashInfoName, StringComparison.Ordinal))
                        {
                            hashesList.Add(hashInfoName);
                            hashFound = true;

                            break;
                        }
                    }

                    if (!hashFound)
                        return $"ERROR: unrecognized hash \"{hashName}\"."; 
                }
            }

            hashes = hashesList.ToArray();

            return String.Empty;
        }

        public static String TryGetTests(Dictionary<String,String[]> arguments, out Boolean qualityTests, out Boolean speedTests, out Boolean validationTests)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            qualityTests = false;
            speedTests = false;
            validationTests = false;

            String[] argumentTests = arguments["tests"];

            if (argumentTests.Length == 0)
                return "ERROR: no tests have been specified.";

            if ((argumentTests.Length == 1) && String.Equals(argumentTests[0], "ALL", StringComparison.Ordinal))
            {
                qualityTests = true;
                speedTests = true;
                validationTests = true;
            }
            else
            {
                for (Int32 i = 0; i < argumentTests.Length; ++i)
                {
                    String argumentTest = argumentTests[i];

                    switch (argumentTest)
                    {
                        case "Q":
                            qualityTests = true;
                            break;

                        case "S":
                            speedTests = true;
                            break;

                        case "V":
                            validationTests = true;
                            break;

                        default:
                            return $"ERROR: unrecognized test \"{argumentTest}\".";
                    }
                }
            }

            return String.Empty;
        }

        public static void PrintHelp(HashInfo[] hashInfos)
        {
            if (hashInfos == null)
                throw new ArgumentNullException(nameof(hashInfos));

            Assembly assembly = Assembly.GetExecutingAssembly();
            String assemblyName = Path.GetFileNameWithoutExtension(assembly.Location);

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Version assemblyVersion = null;

            for (Int32 i = 0; i < assemblies.Length; ++i)
            {
                AssemblyName currentAssemblyName = assemblies[i].GetName();

                if (currentAssemblyName.Name != "FastHashes")
                    continue;

                assemblyVersion = currentAssemblyName.Version;

                break;
            }

            if (assemblyVersion == null)
                assemblyVersion = assembly.GetName().Version;

            String title = $"# FASTHASHES v{assemblyVersion.Major}.{assemblyVersion.Minor} #";
            String frame = new String('#', title.Length);

            Console.WriteLine(frame);
            Console.WriteLine(title);
            Console.WriteLine(frame);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine(" - Display the current help section:");
            Console.WriteLine($"   {assemblyName} -help");
            Console.WriteLine(" - Run the specified tests on the specified hashes:");
            Console.WriteLine($"   {assemblyName} -tests [ALL | T1 ... Tn] -hashes [ALL | H1 ... Hn]");
            Console.WriteLine();
            Console.WriteLine("Available Tests:");
            Console.WriteLine(" - Q: Quality Tests");
            Console.WriteLine(" - S: Speed Tests");
            Console.WriteLine(" - V: Validation Tests");
            Console.WriteLine();
            Console.WriteLine("Available Hashes:");

            for (Int32 i = 0; i < hashInfos.Length; ++i)
                Console.WriteLine($" - {hashInfos[i].Name}");
        }
        #endregion
    }
}
