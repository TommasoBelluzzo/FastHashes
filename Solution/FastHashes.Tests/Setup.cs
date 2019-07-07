#region Using Directives
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class TestsFixture : IDisposable
    {
        #region Members
        private readonly RandomXorShift m_Random;
        private readonly ReadOnlyCollection<String> m_Words;
        private readonly ReadOnlyDictionary<String,Func<UInt32,Hash>> m_HashInitializers;
        #endregion

        #region Properties
        public Int32 WordsCount => m_Words.Count;

        public ReadOnlyCollection<String> Words => m_Words;
        #endregion

        #region Constructors
        public TestsFixture()
        {
            m_Random = new RandomXorShift();
            m_Words = CreateWords();
            m_HashInitializers = CreateHashInitializers();
        }
        #endregion

        #region Methods
        public Hash CreateHash(String hashIdentifier)
        {
            return CreateHash(hashIdentifier, m_Random.NextValue());
        }

        public Hash CreateHash(String hashIdentifier, UInt32 seed)
        {
            if (String.IsNullOrWhiteSpace(hashIdentifier))
                throw new ArgumentException("Invalid hash identifier specified.", nameof(hashIdentifier));

            if (!m_HashInitializers.TryGetValue(hashIdentifier, out Func<UInt32,Hash> initializer))
                throw new ArgumentException("Unsupported hash identifier specified.", nameof(hashIdentifier));

            return initializer(seed);
        }

        public void Dispose() { }
        #endregion

        #region Methods (Static)
        private static ReadOnlyCollection<String> CreateWords()
        {
            String wordsFilePath = Utilities.GetStaticFilePath("Words.txt");
            String[] words = File.ReadAllLines(wordsFilePath);

            return words.ToList().AsReadOnly();
        }

        private static ReadOnlyDictionary<String,Func<UInt32,Hash>> CreateHashInitializers()
        {
            Dictionary<String,Func<UInt32,Hash>> hashInitializers = new Dictionary<String,Func<UInt32,Hash>>
            {
                ["FarmHash32"] = x => new FarmHash32(x),
                ["FarmHash64"] = x => new FarmHash64(x),
                ["FarmHash128"] = x => new FarmHash128(x),
                ["FastHash32"] = x => new FastHash32(x),
                ["FastHash64"] = x => new FastHash64(x),
                ["FastPositiveHash-V0"] = x => new FastPositiveHash(FastPositiveHashVariant.V0, x),
                ["FastPositiveHash-V1"] = x => new FastPositiveHash(FastPositiveHashVariant.V1, x),
                ["FastPositiveHash-V2"] = x => new FastPositiveHash(FastPositiveHashVariant.V2, x),
                ["HalfSipHash"] = x => new HalfSipHash(x),
                ["HighwayHash64"] = x => new HighwayHash64(x),
                ["HighwayHash128"] = x => new HighwayHash128(x),
                ["HighwayHash256"] = x => new HighwayHash256(x),
                ["MetroHash64-V1"] = x => new MetroHash64(MetroHashVariant.V1, x),
                ["MetroHash64-V2"] = x => new MetroHash64(MetroHashVariant.V2, x),
                ["MetroHash128-V1"] = x => new MetroHash128(MetroHashVariant.V1, x),
                ["MetroHash128-V2"] = x => new MetroHash128(MetroHashVariant.V2, x),
                ["MurmurHash32"] = x => new MurmurHash32(x),
                ["MurmurHash64-X86"] = x => new MurmurHash64(MurmurHashEngine.X86, x),
                ["MurmurHash64-X64"] = x => new MurmurHash64(MurmurHashEngine.X64, x),
                ["MurmurHash128-X86"] = x => new MurmurHash128(MurmurHashEngine.X86, x),
                ["MurmurHash128-X64"] = x => new MurmurHash128(MurmurHashEngine.X64, x),
                ["MumHash"] = x => new MumHash(x),
                ["SipHash-13"] = x => new SipHash(SipHashVariant.V13, x),
                ["SipHash-24"] = x => new SipHash(SipHashVariant.V24, x),
                ["SpookyHash32"] = x => new SpookyHash32(x),
                ["SpookyHash64"] = x => new SpookyHash64(x),
                ["SpookyHash128"] = x => new SpookyHash128(x),
                ["xxHash32"] = x => new xxHash32(x),
                ["xxHash64"] = x => new xxHash64(x)
            };

            return (new ReadOnlyDictionary<String,Func<UInt32,Hash>>(hashInitializers));
        }
        #endregion
    }

    [CollectionDefinition("Tests")]
    public sealed class TestsCollection : ICollectionFixture<TestsFixture> { }
}