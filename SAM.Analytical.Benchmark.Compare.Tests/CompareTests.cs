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
            BenchmarkModelResult model = Builders.Model();
            ComparisonResult result = Query.Compare(
                Tas(model, Builders.Space(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(), Builders.Space(GuidA, "Office", 200, 600)),
                Profile);

            Assert.AreEqual(GateStatus.Pass, result.Gate);
            Assert.AreEqual(0, result.FailCount);
            Assert.AreEqual(0, result.WarnCount);
            Assert.IsNull(result.SchemaDriftNote);
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
            // 10 -> 11.2 kW: 12% difference => between 5% warn and 15% fail bands.
            ComparisonResult result = Query.Compare(
                Tas(Builders.Model(peakHeatingLoad: Builders.Value(10, MetricUnit.Kilowatt)), Builders.Space(GuidA, "Office", 200, 600)),
                OpenStudio(Builders.Model(peakHeatingLoad: Builders.Value(11.2, MetricUnit.Kilowatt)), Builders.Space(GuidA, "Office", 200, 600)),
                Profile);

            Assert.AreEqual(GateStatus.Warn, result.Gate);
            Assert.AreEqual(0, result.FailCount);
        }

        [TestMethod]
        public void UnavailableAndUnitMismatchMetricsNeverFailTheGate()
        {
            BenchmarkModelResult tasModel = Builders.Model();
            BenchmarkModelResult osModel = Builders.Model(
                consumptionHeating: MetricValue.Unavailable(MetricUnit.KilowattHour),
                consumptionCooling: Builders.Value(500, MetricUnit.WattHour));

            ComparisonResult result = Query.Compare(
                Tas(tasModel, Builders.Space(GuidA, "Office", 200, 600)),
                OpenStudio(osModel, Builders.Space(GuidA, "Office", 200, 600)),
                Profile);

            Assert.AreEqual(GateStatus.Pass, result.Gate);
            Assert.IsTrue(result.NotApplicableCount >= 2);

            MetricComparison unitMismatch = result.ModelMetrics.Single(metric => metric.Key == "consumptionCooling");
            Assert.AreEqual(NotApplicableReason.UnitMismatch, unitMismatch.NotApplicableReason);
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
            // Every metric of a one-sided space is N/A and cannot fail the gate.
            Assert.IsTrue(tasOnly.Metrics.All(metric => metric.Band == ComparisonBand.NotApplicable));
            Assert.AreEqual(GateStatus.Pass, result.Gate);
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
        public void MajorSchemaIncompatibilityThrows()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "2.0.0");
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });

            Assert.ThrowsException<SchemaIncompatibleException>(() => Query.Compare(tas, openStudio, Profile));
        }

        [TestMethod]
        public void MalformedSchemaVersionThrows()
        {
            BenchmarkDocument tas = Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }, schemaVersion: "banana");
            BenchmarkDocument openStudio = Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) });

            Assert.ThrowsException<SchemaIncompatibleException>(() => Query.Compare(tas, openStudio, Profile));
        }
    }
}
