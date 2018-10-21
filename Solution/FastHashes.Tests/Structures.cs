#region Using Directives
using System;
#endregion

namespace FastHashes.Tests
{
    public struct AnalysisResult
    {
        #region Properties
        public Boolean Outcome { get; set; }
        public Int32 HashesCount { get; set; }
        public Double ExpectedCollisions { get; set; }
        public Double ObservedCollisions { get; set; }
        public Double WorstBias { get; set; }
        public Int32 WorstBit { get; set; }
        public Int32 WorstWindow { get; set; }
        #endregion
    }

    public struct HashInfo
    {
        #region Properties
        public Func<UInt32,Hash> Initializer { get; }
        public Int32 Length { get; }
        public String Name { get; }
        public UInt32 Verification { get; }
        #endregion

        #region Constructors
        public HashInfo(Func<UInt32,Hash> initializer, UInt32 verification)
        {
            if (initializer == null)
                throw new ArgumentNullException(nameof(initializer));

            Hash hash = initializer(0u);
            Length = hash.Length;
            Name = hash.ToString();

            Initializer = initializer;
            Verification = verification;
        }
        #endregion
    }
}