#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace FastHashes.Tests
{
    public sealed class Fixture : IDisposable
    {
        #region Members
        private readonly IEnumerable<String> m_Words;
        #endregion

        #region Properties
        public IEnumerable<String> Words => m_Words;
        #endregion

        #region Constructors
        public Fixture()
        {
#if NETCOREAPP1_0 || NETCOREAPP1_1
            String fileDirectory = AppContext.BaseDirectory;
#else
            String fileDirectory = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
#endif
            String filePath = Path.Combine(fileDirectory, @"Data\Words.txt");

            m_Words = File.Exists(filePath) ? File.ReadAllLines(filePath) : new String[0];
        }
        #endregion

        #region Methods
        public void Dispose() { }
        #endregion
    }
}