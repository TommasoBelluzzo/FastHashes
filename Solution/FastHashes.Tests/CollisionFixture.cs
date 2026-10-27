#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace FastHashes.Tests
{
    public sealed class CollisionFixture
    {
        #region Members
        private readonly IEnumerable<String> m_Words;
        #endregion

        #region Properties
        public IEnumerable<String> Words => m_Words;
        #endregion

        #region Constructors
        public CollisionFixture()
        {
            String fileDirectory = AppContext.BaseDirectory;

            if (String.IsNullOrWhiteSpace(fileDirectory))
            {
                m_Words = Array.Empty<String>();
                return;
            }

            String filePath = Path.Combine(fileDirectory, "Data", "Words.txt");

            if (!File.Exists(filePath))
            {
                m_Words = Array.Empty<String>();
                return;
            }

            m_Words = File.ReadAllLines(filePath);
        }
        #endregion
    }
}