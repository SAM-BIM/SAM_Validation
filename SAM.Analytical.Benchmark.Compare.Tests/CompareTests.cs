// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Linq;
using SAM.Analytical.Benchmark;
using SAM.Analytical.Benchmark.Compare;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    [TestClass]
    public sealed class CompareTests
    {
        private const string GuidA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string GuidB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        private static readonly ToleranceProfile Profile = ToleranceProfile.Default;

        private static BenchmarkDocument Tas(BenchmarkModelResult model, params BenchmarkSpaceResult[] spaces)
        {
            return Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, model, spaces);
        }

        private static BenchmarkDocument OpenStudio(BenchmarkModelResult model, params BenchmarkSpaceResult[] spaces)
        {
            return Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, model, spaces);
        }

        [TestMethod]
        public void IdenticalDocumentsPassTheGate()
        {
            // Complete spaces: an overall Pass requires complete metric coverage as well as matching numbers.
            BenchmarkModelResult model = Builders.Model();
            ComparisonResult result = Query.Compare(
                Tas(model, Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                Profile);

            Assert.AreEqual(GateStatus.Pass, result.Gate);
            Assert.AreEqual(GateStatus.Pass, result.NumericalStatus);
            Assert.AreEqual(GateStatus.Pass, result.CoverageStatus);
            Assert.AreEqual(GateStatus.Pass, result.ProvenanceStatus);
            Assert.AreEqual(GateStatus.Pass, result.ReconciliationStatus);
            Assert.AreEqual(0, result.FailCount);
            Assert.AreEqual(0, result.WarnCount);
            Assert.IsNull(result.SchemaDriftNote);
        }

        [TestMethod]
        public void EveryMetricAvailableGivesCompleteCoverage()
        {
            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                Profile);

            // 8 model metrics + 10 metrics on the single matched space, all comparable.
            Assert.AreEqual(18, result.RequiredMetricCount);
            Assert.AreEqual(18, result.ComparableMetricCount);
            Assert.AreEqual(0, result.UnavailableRequiredMetricCount);
            Assert.AreEqual(GateStatus.Pass, result.CoverageStatus);
        }

        [TestMethod]
        public void OneFailingMetricFailsTheGate()
        {
            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(peakHeatingLoad: Builders.Value(10, MetricUnit.Kilowatt)), Builders.Space(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(peakHeatingLoad: Builders.Value(30, MetricUnit.Kilowatt)), Builders.Space(GuidA, "Office", 200, 600)),
                Profile);

            Assert.AreEqual(GateStatus.Fail, result.Gate);
            Assert.IsTrue(result.FailCount >= 1);
        }

        [TestMethod]
        public void OnlyWarnMetricsGiveWarnGate()
        {
            // 10 -> 11.2 kW: 12% difference => between 5% warn and 15% fail bands. Coverage is complete, so
            // the Warn can only come from the numerical status.
            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(peakHeatingLoad: Builders.Value(10, MetricUnit.Kilowatt)), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(peakHeatingLoad: Builders.Value(11.2, MetricUnit.Kilowatt)), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                Profile);

            Assert.AreEqual(GateStatus.Warn, result.Gate);
            Assert.AreEqual(GateStatus.Warn, result.NumericalStatus);
            Assert.AreEqual(GateStatus.Pass, result.CoverageStatus);
            Assert.AreEqual(0, result.FailCount);
        }

        [TestMethod]
        public void UnavailableModelMetricIsNotApplicableButLeavesCoverageIncomplete()
        {
            BenchmarkModelResult osModel = Builders.Model(consumptionHeating: MetricValue.Unavailable(MetricUnit.KilowattHour));

            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                OpenStudio(osModel, Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                Profile);

            // A legitimately-absent metric is N/A and never a numerical failure...
            MetricComparison unavailable = result.ModelMetrics.Single(metric => metric.Key == "consumptionHeating");
            Assert.AreEqual(NotApplicableReason.Unavailable, unavailable.NotApplicableReason);
            Assert.AreEqual(GateStatus.Pass, result.NumericalStatus);
            // ...but TOLERANCES.md requires it to count against coverage, so an incomplete result cannot
            // present as a complete Pass.
            Assert.AreEqual(1, result.UnavailableRequiredMetricCount);
            Assert.AreEqual(17, result.ComparableMetricCount);
            Assert.AreEqual(GateStatus.Warn, result.CoverageStatus);
            Assert.AreEqual(GateStatus.Warn, result.Gate);
        }

        [TestMethod]
        public void UnavailableMetricInAMatchedSpaceLeavesCoverageIncomplete()
        {
            // The space aligns by GUID and every whole-model metric is comparable; only one per-space metric
            // is missing on the OpenStudio side (the documented design-load gap on the native route).
            BenchmarkSpaceResult openStudioSpace = Builders.CompleteSpace(GuidA, "Office", 200, 600);
            openStudioSpace.Heating!.DesignLoad = MetricValue.Unavailable(MetricUnit.Watt);

            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(), openStudioSpace),
                Profile);

            Assert.AreEqual(GateStatus.Pass, result.NumericalStatus);
            Assert.AreEqual(1, result.UnavailableRequiredMetricCount);
            Assert.AreEqual(GateStatus.Warn, result.CoverageStatus);
            Assert.AreEqual(GateStatus.Warn, result.Gate);
        }

        [TestMethod]
        public void NoComparableMetricsLeavesCoverageIncomplete()
        {
            // Both runs produced identities but no measurements at all: nothing is comparable, so the
            // numerical status has nothing to fail on and coverage is the only thing preventing a Pass.
            ComparisonResult result = Query.Compare(
                Tas(Builders.EmptyModel(), Builders.EmptySpace(GuidA, "Office")),
                OpenStudio(Builders.EmptyModel(), Builders.EmptySpace(GuidA, "Office")),
                Profile);

            Assert.AreEqual(0, result.ComparableMetricCount);
            Assert.AreEqual(18, result.UnavailableRequiredMetricCount);
            Assert.AreEqual(GateStatus.Pass, result.NumericalStatus);
            Assert.AreEqual(GateStatus.Warn, result.CoverageStatus);
            Assert.AreNotEqual(GateStatus.Pass, result.Gate);
        }

        [TestMethod]
        public void OneSidedSpaceMetricsAreNotCountedAsUnavailableRequiredMetrics()
        {
            // The TAS-only space contributes 10 always-N/A metrics. They must NOT be counted as unavailable
            // required metrics — the unmatched space is already reported by the alignment diagnostics, so
            // counting its metrics as well would double-count the same gap. Only the genuinely missing
            // matched-scope metric (the OpenStudio model's consumptionCooling) is counted.
            BenchmarkModelResult osModel = Builders.Model(consumptionCooling: MetricValue.Unavailable(MetricUnit.KilowattHour));

            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(), Builders.CompleteSpace(GuidA, "Shared", 200, 600), Builders.CompleteSpace(GuidB, "TasOnly", 40, 120)),
                OpenStudio(osModel, Builders.CompleteSpace(GuidA, "Shared", 200, 600)),
                Profile);

            Assert.AreEqual(18, result.RequiredMetricCount); // 8 model + 10 matched space; the TAS-only space is excluded.
            Assert.AreEqual(1, result.UnavailableRequiredMetricCount);
            Assert.AreEqual(17, result.ComparableMetricCount);
            // The one-sided space's metrics are still reported (and still counted as N/A overall).
            Assert.AreEqual(11, result.NotApplicableCount);
            Assert.AreEqual(GateStatus.Warn, result.CoverageStatus);
        }

        [TestMethod]
        public void UnitMismatchIsAContractErrorRejectedByCompare()
        {
            // A metric carrying the wrong unit is a schema-invalid document; Compare validates both inputs
            // itself and rejects it as a contract error rather than normalising it to a passing N/A.
            BenchmarkModelResult osModel = Builders.Model(consumptionCooling: Builders.Value(500, MetricUnit.WattHour));

            Assert.ThrowsException<BenchmarkValidationException>(() => Query.Compare(
                Tas(Builders.Model(), Builders.Space(GuidA, "Office", 200, 600)),
                OpenStudio(osModel, Builders.Space(GuidA, "Office", 200, 600)),
                Profile));
        }

        [TestMethod]
        public void PeakHourDisagreementIsInformationalAndDoesNotFailTheGate()
        {
            // Peak hours differ by 100h but everything else matches. The profile's absolute hour thresholds
            // (1h warn / 24h fail) put that in the Fail band, and TOLERANCES.md keeps peak-hour differences
            // informational by excluding hour-of-year metrics from the numerical gate — not by hiding the band.
            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(peakHeatingHour: Builders.Value(100, MetricUnit.HourOfYear)), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(peakHeatingHour: Builders.Value(200, MetricUnit.HourOfYear)), Builders.CompleteSpace(GuidA, "Office", 200, 600)),
                Profile);

            MetricComparison peakHour = result.ModelMetrics.Single(metric => metric.Key == "peakHeatingHour");
            Assert.AreEqual(ComparisonBand.Fail, peakHour.Band);       // reported honestly...
            Assert.AreEqual(GateStatus.Pass, result.NumericalStatus);  // ...but excluded from the numerical gate...
            Assert.AreEqual(GateStatus.Pass, result.Gate);             // ...so the overall gate stays Pass.
        }

        [TestMethod]
        public void ReconciliationExcludesOneSidedSpacesAndIsMarkedIncomplete()
        {
            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(floorArea: Builders.Value(240, MetricUnit.SquareMetre)), Builders.Space(GuidA, "A", 200, 600), Builders.Space(GuidB, "TasOnly", 40, 120)),
                OpenStudio(Builders.Model(floorArea: Builders.Value(240, MetricUnit.SquareMetre)), Builders.Space(GuidA, "A", 200, 600)),
                Profile);

            ReconciliationResult tasFloor = result.Reconciliations.Single(r => r.Engine == "TAS" && r.Key == "floorArea");
            // Only the uniquely-matched space A (200) is summed; the TAS-only space B is excluded.
            Assert.AreEqual(200d, tasFloor.SumOfSpaces);
            Assert.AreEqual(1, tasFloor.ContributingSpaces);
            Assert.AreEqual(1, tasFloor.ExcludedSpaces);
            Assert.IsFalse(tasFloor.Complete);
        }

        [TestMethod]
        public void MissingSpaceProducesTasOnlyComparison()
        {
            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(), Builders.Space(GuidA, "Shared", 200, 600), Builders.Space(GuidB, "TasOnly", 40, 120)),
                OpenStudio(Builders.Model(), Builders.Space(GuidA, "Shared", 200, 600)),
                Profile);

            SpaceComparison tasOnly = result.Spaces.Single(space => space.MatchKind == SpaceMatchKind.TasOnly);
            Assert.AreEqual(GuidB, tasOnly.Guid);
            // Every metric of a one-sided space is N/A, so the numerical status stays Pass...
            Assert.IsTrue(tasOnly.Metrics.All(metric => metric.Band == ComparisonBand.NotApplicable));
            Assert.AreEqual(GateStatus.Pass, result.NumericalStatus);
            // ...but the unmatched space leaves coverage incomplete, so the overall gate is not a clean Pass.
            Assert.AreEqual(GateStatus.Warn, result.CoverageStatus);
            Assert.AreEqual(GateStatus.Warn, result.Gate);
        }

        [TestMethod]
        public void ModelTotalMatchingSumOfSpacesReconciles()
        {
            BenchmarkModelResult model = Builders.Model(floorArea: Builders.Value(240, MetricUnit.SquareMetre));
            ComparisonResult result = Query.Compare(
                Tas(model, Builders.Space(GuidA, "A", 200, 600), Builders.Space(GuidB, "B", 40, 120)),
                OpenStudio(Builders.Model(floorArea: Builders.Value(240, MetricUnit.SquareMetre)), Builders.Space(GuidA, "A", 200, 600), Builders.Space(GuidB, "B", 40, 120)),
                Profile);

            ReconciliationResult tasFloor = result.Reconciliations.Single(r => r.Engine == "TAS" && r.Key == "floorArea");
            Assert.AreEqual(240d, tasFloor.SumOfSpaces);
            Assert.AreEqual(ComparisonBand.Match, tasFloor.Band);
            Assert.AreEqual(2, tasFloor.ContributingSpaces);
        }

        [TestMethod]
        public void ModelTotalDivergingFromSumOfSpacesIsFlagged()
        {
            BenchmarkModelResult model = Builders.Model(floorArea: Builders.Value(1000, MetricUnit.SquareMetre));
            ComparisonResult result = Query.Compare(
                Tas(model, Builders.Space(GuidA, "A", 200, 600), Builders.Space(GuidB, "B", 40, 120)),
                OpenStudio(Builders.Model(), Builders.Space(GuidA, "A", 200, 600), Builders.Space(GuidB, "B", 40, 120)),
                Profile);

            ReconciliationResult tasFloor = result.Reconciliations.Single(r => r.Engine == "TAS" && r.Key == "floorArea");
            Assert.AreEqual(1000d, tasFloor.ModelTotal);
            Assert.AreEqual(240d, tasFloor.SumOfSpaces);
            Assert.AreEqual(ComparisonBand.Fail, tasFloor.Band);
        }

        [TestMethod]
        public void MinorSchemaDriftIsRecordedNotFatal()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "1.0.0");
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "1.1.0");

            ComparisonResult result = Query.Compare(tas, openStudio, Profile);

            Assert.IsNotNull(result.SchemaDriftNote);
        }

        [TestMethod]
        public void EqualButNewerMinorSchemaStillWarns()
        {
            // Both documents are 1.1.0 (newer than the comparator's 1.0.0); even though they agree with each
            // other, the newer minor must still be surfaced because unknown additive fields may be ignored.
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "1.1.0");
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "1.1.0");

            ComparisonResult result = Query.Compare(tas, openStudio, Profile);

            Assert.IsNotNull(result.SchemaDriftNote);
        }

        [TestMethod]
        public void PatchOnlySchemaDifferenceDoesNotWarn()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "1.0.0");
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "1.0.1");

            ComparisonResult result = Query.Compare(tas, openStudio, Profile);

            Assert.IsNull(result.SchemaDriftNote);
        }

        [TestMethod]
        public void MajorSchemaIncompatibilityIsAContractError()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "2.0.0");
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });

            Assert.ThrowsException<BenchmarkValidationException>(() => Query.Compare(tas, openStudio, Profile));
        }

        [TestMethod]
        public void MalformedSchemaVersionIsAContractError()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "banana");
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });

            Assert.ThrowsException<BenchmarkValidationException>(() => Query.Compare(tas, openStudio, Profile));
        }

        [TestMethod]
        public void MismatchedWeatherMakesProvenanceIncompatibleAndPreventsPass()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });
            // Same model, but the two runs used a different weather file (the D10 Boston-vs-Gatwick case).
            openStudio.Provenance!.Weather!.Hash = "sha256:9999999999999999999999999999999999999999999999999999999999999999";
            openStudio.Provenance.Weather.Identity = "Boston.epw";

            ComparisonResult result = Query.Compare(tas, openStudio, Profile);

            Assert.IsFalse(result.ProvenanceCompatibility.IsCompatible);
            Assert.AreEqual(GateStatus.Fail, result.ProvenanceStatus);
            Assert.AreEqual(GateStatus.Fail, result.Gate);
            Assert.IsTrue(result.ProvenanceCompatibility.Mismatches.Any(m => m.Field == "weatherHash"));
        }

        [TestMethod]
        public void DifferentCanonicalizationVersionsAreReportedWithoutComparingTheirHashes()
        {
            // SCHEMA.md: hashes carrying different canonicalization versions must not be compared, and a
            // version mismatch does not prove different models. The version mismatch is the finding; the
            // (incomparable) canonical hashes must not add a second, misleading "different model" mismatch.
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.CompleteSpace(GuidA, "Office", 200, 600) });
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.CompleteSpace(GuidA, "Office", 200, 600) });
            openStudio.Provenance!.CanonicalizationVersion = "1.1.0";
            openStudio.Provenance.CanonicalModelHash = "sha256:4444444444444444444444444444444444444444444444444444444444444444";

            ComparisonResult result = Query.Compare(tas, openStudio, Profile);

            Assert.IsTrue(result.ProvenanceCompatibility.Mismatches.Any(m => m.Field == "canonicalizationVersion"));
            Assert.IsFalse(result.ProvenanceCompatibility.Mismatches.Any(m => m.Field == "canonicalModelHash"));
            Assert.AreEqual(GateStatus.Fail, result.ProvenanceStatus);
        }

        [TestMethod]
        public void SameCanonicalizationVersionStillComparesTheCanonicalHashes()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.CompleteSpace(GuidA, "Office", 200, 600) });
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.CompleteSpace(GuidA, "Office", 200, 600) });
            openStudio.Provenance!.CanonicalModelHash = "sha256:4444444444444444444444444444444444444444444444444444444444444444";

            ComparisonResult result = Query.Compare(tas, openStudio, Profile);

            Assert.IsTrue(result.ProvenanceCompatibility.Mismatches.Any(m => m.Field == "canonicalModelHash"));
            Assert.AreEqual(GateStatus.Fail, result.ProvenanceStatus);
        }

        [TestMethod]
        public void TwoDocumentsFromTheSameEngineAreIncompatible()
        {
            // Swapped inputs: the "openStudio" argument is actually a second TAS document.
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });
            BenchmarkDocument alsoTas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });

            ComparisonResult result = Query.Compare(tas, alsoTas, Profile);

            Assert.IsFalse(result.ProvenanceCompatibility.IsCompatible);
            Assert.AreNotEqual(GateStatus.Pass, result.Gate);
        }
    }
}
