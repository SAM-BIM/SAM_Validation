// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Runtime.CompilerServices;
using SAM.Analytical.Benchmark;
using SAM.Analytical.Benchmark.Compare;

namespace SAM.Analytical.Benchmark.Compare.ApiTests
{
    /// <summary>
    /// The supported public contract of the comparator library (SAM#211), exercised the way SAM_UI uses it: read two
    /// B1a documents, compare them, read every row, status and diagnostic, render the cells with the reports' own
    /// formatting, and write the reports. This project has no access to internals, so a member made internal again
    /// breaks the build here.
    /// </summary>
    [TestClass]
    public class PublicApiTests
    {
        [TestMethod]
        public void ConsumerCanCompareReadEveryRowAndWriteTheReports()
        {
            string caseDirectory = Path.Combine(SourceDirectory(), "..", "SAM.Analytical.Benchmark.Compare.Tests", "Fixtures", "Regression", "weather-mismatch");

            BenchmarkDocument tas = BenchmarkSerializer.Read(Path.Combine(caseDirectory, "benchmark-TAS.json"));
            BenchmarkDocument openStudio = BenchmarkSerializer.Read(Path.Combine(caseDirectory, "benchmark-OpenStudio.json"));

            ComparisonResult result = Query.Compare(tas, openStudio, ToleranceProfile.Default);

            // Statuses and counts.
            Assert.AreEqual(ToleranceProfile.DefaultName, result.ToleranceProfileName);
            GateStatus[] statuses = { result.Gate, result.NumericalStatus, result.CoverageStatus, result.ProvenanceStatus, result.ReconciliationStatus };
            Assert.AreEqual(5, statuses.Length);
            Assert.AreEqual(result.MatchCount + result.WarnCount + result.FailCount + result.NotApplicableCount, result.AllMetrics.Count());
            Assert.IsTrue(result.RequiredMetricCount >= result.ComparableMetricCount);

            // Provenance: the weather mismatch of this case is reported field by field.
            Assert.IsFalse(result.ProvenanceCompatibility.IsCompatible);
            ProvenanceMismatch mismatch = result.ProvenanceCompatibility.Mismatches.First(x => x.Field == "weatherHash");
            Assert.AreNotEqual(mismatch.Tas, mismatch.OpenStudio);
            Assert.IsNotNull(result.TasProvenance);
            Assert.IsNotNull(result.OpenStudioProvenance);

            // Every metric row, rendered with the reports' own formatting.
            foreach (MetricComparison metric in result.AllMetrics)
            {
                string[] cells =
                {
                    metric.Scope, metric.Key, Format.Unit(metric.Unit), Format.Availability(metric.Availability),
                    Format.Number(metric.TasValue), Format.Number(metric.OpenStudioValue), Format.Number(metric.AbsoluteDifference),
                    Format.Percent(metric.RelativeDifference), Format.Number(metric.SignedDifference), Format.Band(metric.Band), Format.MetricNote(metric),
                };
                Assert.AreEqual(11, cells.Length);

                bool informational = Query.IsInformationalForNumericalGate(metric);
                _ = informational;
                _ = (metric.TasAvailable, metric.OpenStudioAvailable, metric.Circular, metric.NotApplicableReason, metric.TasUnit, metric.OpenStudioUnit);

                if (metric.Availability != ComparisonAvailability.Both)
                {
                    Assert.AreEqual(ComparisonBand.NotApplicable, metric.Band);
                    Assert.IsNull(metric.AbsoluteDifference);
                }
            }

            // Spaces, alignment and reconciliation.
            foreach (SpaceComparison space in result.Spaces)
            {
                _ = (space.Guid, space.TasName, space.OpenStudioName, space.MatchKind, space.Scope, space.Metrics.Count, Format.MatchKind(space.MatchKind));
            }

            SpaceMatchDiagnostics diagnostics = result.SpaceDiagnostics;
            _ = (diagnostics.TasCount, diagnostics.OpenStudioCount, diagnostics.MatchedByGuid, diagnostics.MatchedByName, diagnostics.OnlyInTas, diagnostics.OnlyInOpenStudio, diagnostics.AmbiguousNameMatches);
            foreach (ReconciliationResult reconciliation in result.Reconciliations)
            {
                _ = (reconciliation.Engine, reconciliation.Key, reconciliation.ModelTotal, reconciliation.SumOfSpaces, reconciliation.Complete, reconciliation.Band);
            }

            Assert.AreEqual("Fail", Format.Gate(GateStatus.Fail));

            // Reports.
            string outputDirectory = Path.Combine(Path.GetTempPath(), "sam-compare-api-" + Guid.NewGuid().ToString("N"));
            try
            {
                IReadOnlyList<string> written = Modify.WriteReports(result, outputDirectory);
                CollectionAssert.AreEquivalent(
                    new[] { Modify.MarkdownFileName, Modify.CsvFileName, Modify.SummaryFileName },
                    written.Select(Path.GetFileName).ToArray());
                Assert.AreEqual(File.ReadAllText(Path.Combine(caseDirectory, Modify.CsvFileName)), Modify.WriteCsv(result));
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
                }
            }
        }

        [TestMethod]
        public void ConsumerCanListOneEnginesMetricsWithTheComparatorsKeys()
        {
            string caseDirectory = Path.Combine(SourceDirectory(), "..", "SAM.Analytical.Benchmark.Compare.Tests", "Fixtures", "Regression", "weather-mismatch");
            BenchmarkDocument tas = BenchmarkSerializer.Read(Path.Combine(caseDirectory, "benchmark-TAS.json"));
            BenchmarkDocument openStudio = BenchmarkSerializer.Read(Path.Combine(caseDirectory, "benchmark-OpenStudio.json"));
            ComparisonResult result = Query.Compare(tas, openStudio, ToleranceProfile.Default);

            // Same keys, same order as the comparator's model rows; a missing result lists every key with no value.
            IReadOnlyList<KeyValuePair<string, MetricValue?>> model = Query.ModelMetricValues(tas.Model);
            CollectionAssert.AreEqual(result.ModelMetrics.Select(x => x.Key).ToArray(), model.Select(x => x.Key).ToArray());
            Assert.IsTrue(Query.ModelMetricValues(null).All(x => x.Value == null));

            SpaceComparison space = result.Spaces.First();
            BenchmarkSpaceResult tasSpace = tas.Spaces!.First(x => x.Name == space.TasName);
            IReadOnlyList<KeyValuePair<string, MetricValue?>> spaceValues = Query.SpaceMetricValues(tasSpace);
            CollectionAssert.AreEqual(space.Metrics.Select(x => x.Key).ToArray(), spaceValues.Select(x => x.Key).ToArray());
            Assert.IsTrue(Query.SpaceMetricValues(null).All(x => x.Value == null));
        }

        private static string SourceDirectory([CallerFilePath] string? sourceFilePath = null)
        {
            return Path.GetDirectoryName(sourceFilePath!)!;
        }
    }
}
