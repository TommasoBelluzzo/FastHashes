#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashRegistryTests
    {
        #region Constants
        private const UInt64 TEST_SEED = 0xFFA10655ul;
        #endregion

        #region Tests: Generic Registry
        [Fact]
        public void RegistryInvalidTest()
        {
            Assert.Throws<ArgumentNullException>(() => Hash.Create(null));
            Assert.Throws<ArgumentNullException>(() => Hash.Create(null, TEST_SEED));
            Assert.Throws<ArgumentNullException>(() => Hash.TryCreate(null, out _));
            Assert.Throws<ArgumentNullException>(() => Hash.TryCreate(null, TEST_SEED, out _));
            Assert.Throws<ArgumentNullException>(() => Hash.GetInitializerDefault(null));
            Assert.Throws<ArgumentNullException>(() => Hash.GetInitializerSeeded(null));

            Assert.Throws<ArgumentException>(() => Hash.Create(String.Empty));
            Assert.Throws<ArgumentException>(() => Hash.Create(String.Empty, TEST_SEED));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerDefault(String.Empty));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerSeeded(String.Empty));

            Assert.False(Hash.TryCreate(String.Empty, out Hash hash1));
            Assert.Null(hash1);

            Assert.False(Hash.TryCreate(String.Empty, TEST_SEED, out Hash hash2));
            Assert.Null(hash2);

            Assert.Throws<ArgumentException>(() => Hash.Create("UnknownAlgorithm"));
            Assert.Throws<ArgumentException>(() => Hash.Create("UnknownAlgorithm", TEST_SEED));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerDefault("UnknownAlgorithm"));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerSeeded("UnknownAlgorithm"));

            Assert.False(Hash.TryCreate("UnknownAlgorithm", out Hash hash3));
            Assert.Null(hash3);

            Assert.False(Hash.TryCreate("UnknownAlgorithm", TEST_SEED, out Hash hash4));
            Assert.Null(hash4);

            Assert.Throws<OverflowException>(() => Hash.Create("XxHash32", UInt64.MaxValue));
            Assert.Throws<OverflowException>(() => Hash.TryCreate("XxHash32", UInt64.MaxValue, out _));
            Assert.Throws<OverflowException>(() => Hash.GetInitializerSeeded("XxHash32")(UInt64.MaxValue));
        }

        [Fact]
        public void RegistryCompletenessTest()
        {
            IReadOnlyList<String> algorithms = Hash.GetAlgorithms();

            Assert.NotEmpty(algorithms);
            Assert.Equal(algorithms.Count, algorithms.Distinct(StringComparer.Ordinal).Count());

            Byte[] buffer = [];

            foreach (String name in algorithms)
            {
                Hash hashDefault = Hash.Create(name);
                Hash hashSeeded = Hash.Create(name, TEST_SEED);

                Hash hashInitializedDefault = Hash.GetInitializerDefault(name)();
                Hash hashInitializedSeeded = Hash.GetInitializerSeeded(name)(TEST_SEED);

                Assert.Equal(hashDefault.GetType(), hashInitializedDefault.GetType());
                Assert.Equal(hashSeeded.GetType(), hashInitializedSeeded.GetType());

                Assert.Equal(hashDefault.ComputeHashBytes(buffer), hashInitializedDefault.ComputeHashBytes(buffer));
                Assert.Equal(hashSeeded.ComputeHashBytes(buffer), hashInitializedSeeded.ComputeHashBytes(buffer));

                Assert.True(Hash.TryCreate(name, out Hash hash1));
                Assert.True(Hash.TryCreate(name, TEST_SEED, out Hash hash2));

                Assert.Equal(hashDefault.ComputeHashBytes(buffer), hash1.ComputeHashBytes(buffer));
                Assert.Equal(hashSeeded.ComputeHashBytes(buffer), hash2.ComputeHashBytes(buffer));
            }
        }

        [Fact]
        public void RegistryCoverageTest()
        {
            IReadOnlyList<String> registered = Hash.GetAlgorithms();
            String[] validated = [.. HashTestsCases.DataHashesValidation().Select(x => (String)x[0])];

            Assert.Equal(validated.Length, validated.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(registered.OrderBy(x => x, StringComparer.Ordinal), validated.OrderBy(x => x, StringComparer.Ordinal));
        }
        #endregion

        #region Tests: Typed Registry
        [Fact]
        public void RegistryTypedInvalidTest()
        {
            Assert.Throws<ArgumentNullException>(() => Hash<UInt64>.Create(null));
            Assert.Throws<ArgumentNullException>(() => Hash<UInt64>.Create(null, TEST_SEED));
            Assert.Throws<ArgumentNullException>(() => Hash<UInt64>.TryCreate(null, out Hash<UInt64> _));
            Assert.Throws<ArgumentNullException>(() => Hash<UInt64>.TryCreate(null, TEST_SEED, out Hash<UInt64> _));

            Assert.Throws<ArgumentException>(() => Hash<UInt64>.Create("UnknownAlgorithm"));
            Assert.Throws<ArgumentException>(() => Hash<UInt64>.Create("UnknownAlgorithm", TEST_SEED));
            Assert.False(Hash<UInt64>.TryCreate("UnknownAlgorithm", out Hash<UInt64> unknown));
            Assert.Null(unknown);
            Assert.False(Hash<UInt64>.TryCreate("UnknownAlgorithm", TEST_SEED, out Hash<UInt64> unknownSeeded));
            Assert.Null(unknownSeeded);

            Assert.Throws<ArgumentException>(() => Hash<UInt64>.Create("XxHash32"));
            Assert.Throws<ArgumentException>(() => Hash<UInt64>.Create("XxHash32", TEST_SEED));
            Assert.False(Hash<UInt64>.TryCreate("XxHash32", out Hash<UInt64> incompatible));
            Assert.Null(incompatible);
            Assert.False(Hash<UInt64>.TryCreate("XxHash32", TEST_SEED, out Hash<UInt64> incompatibleSeeded));
            Assert.Null(incompatibleSeeded);
        }

        [Fact]
        public void RegistryTypedOverflowTest()
        {
            Assert.Throws<OverflowException>(() => Hash<UInt32>.Create("XxHash32", UInt64.MaxValue));
            Assert.Throws<OverflowException>(() => Hash<UInt32>.TryCreate("XxHash32", UInt64.MaxValue, out Hash<UInt32> _));
        }

        [Fact]
        public void RegistryTypedDefaultTest()
        {
            Byte[] buffer = [0, 1, 2, 3, 4, 5, 6, 7];

            Hash<UInt32> hash32 = Hash<UInt32>.Create("XxHash32");
            Assert.IsType<XxHash32>(hash32);

            Hash<UInt64> hash64 = Hash<UInt64>.Create("KomiHash");
            Assert.IsType<KomiHash>(hash64);

            Assert.Equal(Hash.Create("XxHash32").ComputeHashBytes(buffer), hash32.ComputeHashBytes(buffer));
            Assert.Equal(Hash.Create("KomiHash").ComputeHashBytes(buffer), hash64.ComputeHashBytes(buffer));

            Assert.True(Hash<UInt32>.TryCreate("XxHash32", out Hash<UInt32> created32));
            Assert.True(Hash<UInt64>.TryCreate("KomiHash", out Hash<UInt64> created64));

            Assert.NotNull(created32);
            Assert.NotNull(created64);
            Assert.Equal(hash32.ComputeHashValue(buffer), created32.ComputeHashValue(buffer));
            Assert.Equal(hash64.ComputeHashValue(buffer), created64.ComputeHashValue(buffer));
        }

        [Fact]
        public void RegistryTypedSeedTest()
        {
            Byte[] buffer = [0, 1, 2, 3, 4, 5, 6, 7];

            Hash<UInt32> hash32 = Hash<UInt32>.Create("XxHash32", TEST_SEED);
            Hash<UInt64> hash64 = Hash<UInt64>.Create("KomiHash", TEST_SEED);

            Assert.Equal(Hash.Create("XxHash32", TEST_SEED).ComputeHashBytes(buffer), hash32.ComputeHashBytes(buffer));
            Assert.Equal(Hash.Create("KomiHash", TEST_SEED).ComputeHashBytes(buffer), hash64.ComputeHashBytes(buffer));

            Assert.True(Hash<UInt32>.TryCreate("XxHash32", TEST_SEED, out Hash<UInt32> created32));
            Assert.True(Hash<UInt64>.TryCreate("KomiHash", TEST_SEED, out Hash<UInt64> created64));

            Assert.Equal(hash32.ComputeHashValue(buffer), created32.ComputeHashValue(buffer));
            Assert.Equal(hash64.ComputeHashValue(buffer), created64.ComputeHashValue(buffer));
        }
        #endregion
    }
}