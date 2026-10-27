#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class ConformanceTests : IClassFixture<ConformanceFixture>
    {
        #region Members
        private readonly ConformanceFixture m_ReferenceFixture;
        private readonly ITestOutputHelper m_Output;
        #endregion

        #region Constructors
        public ConformanceTests(ConformanceFixture referenceFixture, ITestOutputHelper output)
        {
            m_ReferenceFixture = referenceFixture;
            m_Output = output;
        }
        #endregion

        #region Methods
        [Fact]
        public void ConformanceTest()
        {
            Dictionary<String,(Int32 Passed,Int32 Failed)> results = new Dictionary<String,(Int32,Int32)>();
            List<String> errors = new List<String>();

            foreach (ConformanceFixture.ReferenceCase testCase in m_ReferenceFixture.Cases)
            {
                String algorithm = testCase.Algorithm;

                if (!results.ContainsKey(algorithm))
                    results[algorithm] = (0, 0);

                try
                {
                    Byte[] buffer = Convert.FromHexString(testCase.Input);
                    Byte[] expected = Convert.FromHexString(testCase.Expected);

                    Hash hash = ConformanceFixture.CreateHash(algorithm, testCase.Seed);
                    Byte[] actual = hash.ComputeHash(buffer);

                    if ((expected.Length != hash.Length / 8) || !actual.AsSpan().SequenceEqual(expected))
                    {
                        results[algorithm] = (results[algorithm].Passed, results[algorithm].Failed + 1);

                        errors.Add($"ALGORITHM: {algorithm}, CASE: {testCase.Name}, SEED: {testCase.Seed}, INPUT: {testCase.Input}, EXPECTED: {testCase.Expected}, ACTUAL: {Convert.ToHexString(actual)}, REFERENCE: {testCase.Provenance}");
                    }
                    else
                        results[algorithm] = (results[algorithm].Passed + 1, results[algorithm].Failed);
                }
                catch (Exception ex)
                {
                    results[algorithm] = (results[algorithm].Passed, results[algorithm].Failed + 1);

                    errors.Add($"ALGORITHM: {algorithm}, CASE: {testCase.Name}, SEED: {testCase.Seed}, ERROR: {ex.GetType().Name} - {ex.Message}");
                }
            }

            Int32 totalPassed = 0;
            Int32 totalFailed = 0;

            m_Output.WriteLine("REFERENCE CONFORMANCE RESULTS");
            m_Output.WriteLine("-----------------------------");

            foreach (KeyValuePair<String,(Int32 Passed,Int32 Failed)> result in results.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                Int32 passed = result.Value.Passed;
                Int32 failed = result.Value.Failed;

                totalPassed += passed;
                totalFailed += failed;

                m_Output.WriteLine($"{result.Key}: {passed} passed, {failed} failed");
            }

            m_Output.WriteLine("-----------------------------");
            m_Output.WriteLine($"TOTAL: {totalPassed} passed, {totalFailed} failed");

            if (errors.Count > 0)
            {
                m_Output.WriteLine("");
                m_Output.WriteLine("CONFORMANCE FAILURES");

                foreach (String error in errors)
                    m_Output.WriteLine(error);
            }

            Assert.True(totalFailed == 0, $"Reference conformance failed: {totalFailed} of {totalPassed + totalFailed} vectors failed.{Environment.NewLine}{String.Join(Environment.NewLine, errors)}");
        }
        #endregion
    }
}