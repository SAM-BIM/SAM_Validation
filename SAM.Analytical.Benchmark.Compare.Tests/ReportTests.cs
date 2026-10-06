// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Linq;
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
        public void CsvCarriesTheToleranceProfileAndItsValuesOnEveryRow()
        {
            // TOLERANCES.md: a report must name the profile AND carry its actual values. The CSV is often
            // archived on its own, so every row must be interpretable and reproducible without the siblings.
            ComparisonResult result = Compare();
            string csv = Modify.WriteCsv(result);
            string[] lines = csv.TrimEnd('\n').Split('\n');

            StringAssert.EndsWith(lines[0], "toleranceProfile,warnRelative,failRelative,hourWarnAbsolute,hourFailAbsolute,nearZeroFloor,informational");

            // A kWh row: the default profile's 5%/15% bands, 1h/24h hour limits and the 1 kWh floor.
            string consumption = lines.Single(line => line.StartsWith("model,consumptionHeating,", StringComparison.Ordinal));
            StringAssert.EndsWith(consumption, "default,0.05,0.15,1,24,1,false");

            // A W row carries the same profile but ITS unit's floor (10 W), and no row may omit the profile.
            string spacePeak = lines.First(line => line.Contains(",heating.peakLoad,W,", StringComparison.Ordinal));
            StringAssert.EndsWith(spacePeak, "default,0.05,0.15,1,24,10,false");
            foreach (string line in lines.Skip(1))
            {
                StringAssert.Contains(line, ",default,");
            }
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
        public void ReportsMarkInformationalMetricsThatCarryABand()
        {
            // The whole-model peak loads and the peak hours are excluded from the numerical status, so every
            // report must say so on the row itself — otherwise a Fail band reads as a gate failure.
            ComparisonResult result = Compare();
            MetricComparison peakLoad = result.ModelMetrics.Single(metric => metric.Key == "peakHeatingLoad");
            Assert.AreNotEqual(ComparisonBand.NotApplicable, peakLoad.Band);
            Assert.IsTrue(Query.IsInformationalForNumericalGate(peakLoad));

            string markdown = Modify.WriteMarkdown(result);
            StringAssert.Contains(markdown, "| peakHeatingLoad | kW | 10 | 30 | 20 | 66.66666666666666 | Fail | informational (excluded from the numerical status) |");
            StringAssert.Contains(markdown, "**excluded from the numerical status**");

            StringAssert.Contains(Modify.WriteCsv(result), "model,peakHeatingLoad,kW,Both,10,30,20,");
            StringAssert.Contains(Modify.WriteCsv(result), "Fail,informational (excluded from the numerical status),default,");

            // Annual energy is NOT informational and must not be marked.
            MetricComparison consumption = result.ModelMetrics.Single(metric => metric.Key == "consumptionHeating");
            Assert.IsFalse(Query.IsInformationalForNumericalGate(consumption));
            StringAssert.Contains(Modify.WriteSummaryJson(result), "\"key\": \"consumptionHeating\",");
            StringAssert.Contains(Modify.WriteSummaryJson(result), "\"informational\": true");
            StringAssert.Contains(Modify.WriteSummaryJson(result), "\"informational\": false");
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
