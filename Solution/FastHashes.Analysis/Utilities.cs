#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class Utilities
    {
        #region Methods
        public static String CreateRunDirectory(String hashName)
        {
            StringBuilder hashReferenceBuilder = new(hashName.Length);

            foreach (Char character in hashName)
                hashReferenceBuilder.Append((Char.IsLetterOrDigit(character) || (character == '-') || (character == '_')) ? character : '_');

            String hashReference = (hashReferenceBuilder.Length == 0) ? "Unknown" : hashReferenceBuilder.ToString();

            String root = Path.Combine(Environment.CurrentDirectory, "Analysis", hashReference);
            String runId = DateTimeOffset.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            String directory = Path.Combine(root, runId);
            String reservation = directory + ".lock";

            Directory.CreateDirectory(root);

            using FileStream reservationStream = new(reservation, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            
            try
            {
                if (Directory.Exists(directory))
                    throw new IOException($"Analysis directory already exists: {directory}.");

                Directory.CreateDirectory(directory);
                
                return directory;
            }
            finally
            {
                reservationStream.Dispose();
                File.Delete(reservation);
            }
        }

        public static String WriteArtifacts(String hashName, Action<String> write, String existingDirectory = null)
        {
            Boolean created = existingDirectory == null;
            String directory = created ? CreateRunDirectory(hashName) : existingDirectory;
            HashSet<String> existingFiles = created ? [] : new(Directory.GetFiles(directory), StringComparer.OrdinalIgnoreCase);

            try
            {
                write(directory);
                return directory;
            }
            catch
            {
                if (created)
                {
                    try { Directory.Delete(directory, true); }
                    catch { }
                }
                else
                {
                    foreach (String file in Directory.GetFiles(directory))
                    {
                        if (existingFiles.Contains(file))
                            continue;

                        try { File.Delete(file); }
                        catch { }
                    }
                }

                throw;
            }
        }
        #endregion
    }
}