// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SAM.Analytical.Benchmark;
using SAM.Analytical.Benchmark.Compare;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    [TestClass]
    public sealed class ReportTests
    {
        private static readonly ToleranceProfile Profile = ToleranceProfile.Default;

        private static ComparisonResult Compare()
        {
            return Query.Compare(ScenarioDocuments.Tas(), ScenarioDocuments.OpenStudio(), Profile);
        }

        [TestMethod]
        public void MarkdownIsByteStableAcrossRepeatedRenders()
        {
            AssertRepeatable(Modify.WriteMarkdown(Compare()), Modify.WriteMarkdown(Compare()));
        }

        [TestMethod]
        public void CsvIsByteStableAcrossRepeatedRenders()
        {
            AssertRepeatable(Modify.WriteCsv(Compare()), Modify.WriteCsv(Compare()));
        }

        [TestMethod]
        public void SummaryIsByteStableAcrossRepeatedRenders()
        {
            AssertRepeatable(Modify.WriteSummaryJson(Compare()), Modify.WriteSummaryJson(Compare()));
        }

        [TestMethod]
        public void ReportsUseLfNewlinesAndNoBom()
        {
            byte[] markdown = Modify.ToUtf8(Modify.WriteMarkdown(Compare()));
            byte[] csv = Modify.ToUtf8(Modify.WriteCsv(Compare()));
            byte[] summary = Modify.ToUtf8(Modify.WriteSummaryJson(Compare()));

            foreach (byte[] bytes in new[] { markdown, csv, summary })
            {
                Assert.IsFalse(bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf, "unexpected BOM");
                Assert.IsFalse(Encoding.UTF8.GetString(bytes).Contains('\r'), "unexpected CR");
            }
        }

        [TestMethod]
        public void SummarySchemaVersionIsStamped()
        {
            StringAssert.Contains(Modify.WriteSummaryJson(Compare()), "\"summarySchemaVersion\": \"1.0.0\"");
        }

        [TestMethod]
        public void ReportsRecordCoverageDiagnostics()
        {
            // TOLERANCES.md: unavailable required metrics must be visible in the reports, not just implied by
            // the coverage status. The shared scenario has an unavailable model metric and one-sided spaces.
            ComparisonResult result = Compare();
            Assert.IsTrue(result.UnavailableRequiredMetricCount > 0);
            Assert.AreEqual(GateStatus.Warn, result.CoverageStatus);

            string summary = Modify.WriteSummaryJson(result);
            StringAssert.Contains(summary, "\"requiredMetricCount\": " + result.RequiredMetricCount);
            StringAssert.Contains(summary, "\"comparableMetricCount\": " + result.ComparableMetricCount);
            StringAssert.Contains(summary, "\"unavailableRequiredMetricCount\": " + result.UnavailableRequiredMetricCount);

            string markdown = Modify.WriteMarkdown(result);
            StringAssert.Contains(markdown, "| Required metrics unavailable | " + result.UnavailableRequiredMetricCount + " |");
            StringAssert.Contains(markdown, "**Coverage is INCOMPLETE**");
        }

        [TestMethod]
        public void GoldenReportsMatchCommittedFixturesByteForByte()
        {
            // Regenerate goldens (and the two input fixtures) when explicitly requested; otherwise assert.
            if (string.Equals(Environment.GetEnvironmentVariable("SAM_BENCHMARK_UPDATE_GOLDEN"), "1", StringComparison.Ordinal))
            {
                RegenerateGoldens();
            }

            BenchmarkDocument tas = BenchmarkSerializer.Read(RuntimeFixture("benchmark-TAS.json"));
            BenchmarkDocument openStudio = BenchmarkSerializer.Read(RuntimeFixture("benchmark-OpenStudio.json"));
            ComparisonResult result = Query.Compare(tas, openStudio, Profile);

            AssertMatchesFixture("comparison.md", Modify.WriteMarkdown(result));
            AssertMatchesFixture("comparison.csv", Modify.WriteCsv(result));
            AssertMatchesFixture("comparison-summary.json", Modify.WriteSummaryJson(result));
        }

        private static void RegenerateGoldens()
        {
            BenchmarkDocument tas = ScenarioDocuments.Tas();
            BenchmarkDocument openStudio = ScenarioDocuments.OpenStudio();

            // Serialize the neutral inputs exactly as a producer would (LF, UTF-8 no BOM, trailing newline).
            byte[] tasBytes = Utf8(BenchmarkSerializer.Serialize(tas) + "\n");
            byte[] openStudioBytes = Utf8(BenchmarkSerializer.Serialize(openStudio) + "\n");

            ComparisonResult result = Query.Compare(
                BenchmarkSerializer.Deserialize(BenchmarkSerializer.Serialize(tas)),
                BenchmarkSerializer.Deserialize(BenchmarkSerializer.Serialize(openStudio)),
                Profile);

            byte[] markdown = Modify.ToUtf8(Modify.WriteMarkdown(result));
            byte[] csv = Modify.ToUtf8(Modify.WriteCsv(result));
            byte[] summary = Modify.ToUtf8(Modify.WriteSummaryJson(result));

            // Write to the committed source fixtures AND the runtime copy, so the assertion below passes in
            // the same run without a rebuild.
            foreach (string dir in new[] { SourceFixturesDirectory(), Path.Combine(AppContext.BaseDirectory, "Fixtures") })
            {
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, "benchmark-TAS.json"), tasBytes);
                File.WriteAllBytes(Path.Combine(dir, "benchmark-OpenStudio.json"), openStudioBytes);
                File.WriteAllBytes(Path.Combine(dir, "comparison.md"), markdown);
                File.WriteAllBytes(Path.Combine(dir, "comparison.csv"), csv);
                File.WriteAllBytes(Path.Combine(dir, "comparison-summary.json"), summary);
            }
        }

        private static void AssertMatchesFixture(string name, string actual)
        {
            byte[] expected = File.ReadAllBytes(RuntimeFixture(name));
            byte[] actualBytes = Modify.ToUtf8(actual);
            CollectionAssert.AreEqual(
                expected,
                actualBytes,
                $"Golden report '{name}' does not match. Re-run with SAM_BENCHMARK_UPDATE_GOLDEN=1 if the change is intended.");
        }

        private static void AssertRepeatable(string first, string second)
        {
            CollectionAssert.AreEqual(Modify.ToUtf8(first), Modify.ToUtf8(second));
        }

        private static string RuntimeFixture(string name)
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        }

        private static string SourceFixturesDirectory([CallerFilePath] string? sourceFilePath = null)
        {
            return Path.Combine(Path.GetDirectoryName(sourceFilePath!)!, "Fixtures");
        }

        private static byte[] Utf8(string text)
        {
            return new UTF8Encoding(false).GetBytes(text);
        }
    }
}
