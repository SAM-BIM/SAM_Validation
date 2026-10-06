// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Benchmark;
using SAM.Analytical.Benchmark.Compare;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    [TestClass]
    public sealed class BandTests
    {
        private static readonly ToleranceProfile Profile = ToleranceProfile.Default;

        private static MetricComparison Band(MetricValue? tas, MetricValue? openStudio)
        {
            return Query.Band("model", "metric", tas, openStudio, Profile);
        }

        [TestMethod]
        public void ExactMatchIsMatchWithZeroDifference()
        {
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.KilowattHour), Builders.Value(100, MetricUnit.KilowattHour));

            Assert.AreEqual(ComparisonBand.Match, metric.Band);
            Assert.AreEqual(0d, metric.AbsoluteDifference);
            Assert.AreEqual(0d, metric.RelativeDifference);
            Assert.AreEqual(ComparisonAvailability.Both, metric.Availability);
        }

        [TestMethod]
        public void WithinWarnBandIsMatch()
        {
            // 100 -> 104: scale = max(100, 104, floor 1) = 104, so rel = 4/104 = 3.85% < 5% warn => match.
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.KilowattHour), Builders.Value(104, MetricUnit.KilowattHour));

            Assert.AreEqual(ComparisonBand.Match, metric.Band);
            Assert.AreEqual(4d, metric.AbsoluteDifference);
            Assert.AreEqual(4d, metric.SignedDifference);
        }

        [TestMethod]
        public void BetweenWarnAndFailIsWarn()
        {
            // 100 -> 112: scale = 112, so rel = 12/112 = 10.7% => between the 5% warn and 15% fail bands.
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.KilowattHour), Builders.Value(112, MetricUnit.KilowattHour));

            Assert.AreEqual(ComparisonBand.Warn, metric.Band);
        }

        [TestMethod]
        public void BeyondFailBandIsFail()
        {
            // 100 -> 140: scale = 140, so rel = 40/140 = 28.6% >= the 15% fail band.
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.KilowattHour), Builders.Value(140, MetricUnit.KilowattHour));

            Assert.AreEqual(ComparisonBand.Fail, metric.Band);
        }

        [TestMethod]
        public void NearZeroFloorBoundsTheDenominatorWithoutMaskingDisagreement()
        {
            // The floor is the SCALE lower bound, not an absolute allowance. With the 10 W default floor:
            // 0.1 vs 0.3 W is a negligible 0.2 W difference -> 0.2/10 = 2% -> Match (the floor tames what
            // would otherwise be a 200% relative difference)...
            MetricComparison negligible = Band(Builders.Value(0.1, MetricUnit.Watt), Builders.Value(0.3, MetricUnit.Watt));
            Assert.AreEqual(ComparisonBand.Match, negligible.Band);

            // ...but 0 W vs 8 W is 8/10 = 80% and must NOT be masked into a match by the floor.
            MetricComparison disagreement = Band(Builders.Value(0, MetricUnit.Watt), Builders.Value(8, MetricUnit.Watt));
            Assert.AreEqual(ComparisonBand.Fail, disagreement.Band);
        }

        [DataTestMethod]
        [DataRow(0d, 0d, ComparisonBand.Match)]
        [DataRow(0d, 4d, ComparisonBand.Match)]
        [DataRow(0d, 10d, ComparisonBand.Warn)]
        [DataRow(0d, 20d, ComparisonBand.Fail)]
        public void NearZeroRuleMatchesToleranceContractExamples(double tas, double openStudio, ComparisonBand expected)
        {
            // Reproduces the TOLERANCES.md near-zero table for a hypothetical 100 W floor: the effective
            // relative difference is measured against the floor, so 4/10/20 W land in Pass/Warn/Fail.
            var floors = new Dictionary<MetricUnit, double> { [MetricUnit.Watt] = 100 };
            var profile = new ToleranceProfile("contract-example", 0.05, 0.15, 1, 24, floors, 0);

            MetricComparison metric = Query.Band("model", "metric", Builders.Value(tas, MetricUnit.Watt), Builders.Value(openStudio, MetricUnit.Watt), profile);

            Assert.AreEqual(expected, metric.Band);
        }

        [TestMethod]
        public void UnavailableOnOneSideIsNotApplicableNeverFail()
        {
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.KilowattHour), MetricValue.Unavailable(MetricUnit.KilowattHour));

            Assert.AreEqual(ComparisonBand.NotApplicable, metric.Band);
            Assert.AreEqual(NotApplicableReason.Unavailable, metric.NotApplicableReason);
            Assert.AreEqual(ComparisonAvailability.TasOnly, metric.Availability);
            Assert.IsNull(metric.AbsoluteDifference);
            Assert.IsNull(metric.RelativeDifference);
        }

        [TestMethod]
        public void UnavailableOnBothSidesIsNotApplicable()
        {
            MetricComparison metric = Band(MetricValue.Unavailable(MetricUnit.Watt), MetricValue.Unavailable(MetricUnit.Watt));

            Assert.AreEqual(ComparisonBand.NotApplicable, metric.Band);
            Assert.AreEqual(NotApplicableReason.Unavailable, metric.NotApplicableReason);
            Assert.AreEqual(ComparisonAvailability.Neither, metric.Availability);
        }

        [TestMethod]
        public void UnitMismatchIsNotApplicableEvenWhenBothAvailable()
        {
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.KilowattHour), Builders.Value(100, MetricUnit.WattHour));

            Assert.AreEqual(ComparisonBand.NotApplicable, metric.Band);
            Assert.AreEqual(NotApplicableReason.UnitMismatch, metric.NotApplicableReason);
            Assert.AreEqual(MetricUnit.KilowattHour, metric.TasUnit);
            Assert.AreEqual(MetricUnit.WattHour, metric.OpenStudioUnit);
        }

        [TestMethod]
        public void CircularPeakHourAcrossYearBoundaryMatches()
        {
            // Hour 8759 (last hour) vs hour 0 (first hour) are one hour apart, not 8759.
            MetricComparison metric = Band(Builders.Value(8759, MetricUnit.HourOfYear), Builders.Value(0, MetricUnit.HourOfYear));

            Assert.IsTrue(metric.Circular);
            Assert.AreEqual(1d, metric.AbsoluteDifference);
            Assert.AreEqual(ComparisonBand.Match, metric.Band);
            Assert.IsNull(metric.RelativeDifference);
            Assert.IsNull(metric.SignedDifference);
        }

        [TestMethod]
        public void PeakHourWithinFailWindowWarns()
        {
            // 10h apart: beyond the profile's 1h warn threshold, within its 24h fail threshold.
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.HourOfYear), Builders.Value(110, MetricUnit.HourOfYear));

            Assert.AreEqual(10d, metric.AbsoluteDifference);
            Assert.AreEqual(ComparisonBand.Warn, metric.Band);
        }

        [TestMethod]
        public void PeakHourBeyondFailWindowReportsTheFailBand()
        {
            // TOLERANCES.md requires absolute warn/fail hour thresholds, so a 100h gap must NOT be reported
            // identically to a 10h gap. Peak-hour differences stay informational because hour-of-year metrics
            // are excluded from the numerical gate (Query.NumericalStatus), not by suppressing this band.
            MetricComparison metric = Band(Builders.Value(100, MetricUnit.HourOfYear), Builders.Value(200, MetricUnit.HourOfYear));

            Assert.AreEqual(100d, metric.AbsoluteDifference);
            Assert.AreEqual(ComparisonBand.Fail, metric.Band);
            Assert.AreEqual(GateStatus.Pass, Query.NumericalStatus(new[] { metric }));
        }

        [DataTestMethod]
        // Informational: every hour-of-year metric, plus the two whole-model peak loads.
        [DataRow("model", "peakHeatingLoad", MetricUnit.Kilowatt, true)]
        [DataRow("model", "peakCoolingLoad", MetricUnit.Kilowatt, true)]
        [DataRow("model", "peakHeatingHour", MetricUnit.HourOfYear, true)]
        [DataRow("model", "peakCoolingHour", MetricUnit.HourOfYear, true)]
        [DataRow("a-space-guid", "heating.peakHour", MetricUnit.HourOfYear, true)]
        // Gating: annual energy, per-space peak/design loads, unmet hours, geometry.
        [DataRow("model", "consumptionHeating", MetricUnit.KilowattHour, false)]
        [DataRow("model", "consumptionCooling", MetricUnit.KilowattHour, false)]
        [DataRow("model", "floorArea", MetricUnit.SquareMetre, false)]
        [DataRow("model", "volume", MetricUnit.CubicMetre, false)]
        [DataRow("a-space-guid", "heating.peakLoad", MetricUnit.Watt, false)]
        [DataRow("a-space-guid", "cooling.peakLoad", MetricUnit.Watt, false)]
        [DataRow("a-space-guid", "heating.designLoad", MetricUnit.Watt, false)]
        [DataRow("a-space-guid", "cooling.unmetHours", MetricUnit.Hour, false)]
        // A space that happens to be NAMED "model" is not captured: its keys are the dotted per-space ones.
        [DataRow("model", "heating.peakLoad", MetricUnit.Watt, false)]
        public void OnlyDesignatedMetricsAreInformationalForTheNumericalGate(string scope, string key, MetricUnit unit, bool expected)
        {
            MetricComparison metric = Query.Band(scope, key, Builders.Value(10, unit), Builders.Value(11, unit), Profile);

            Assert.AreEqual(expected, Query.IsInformationalForNumericalGate(metric));
        }

        [DataTestMethod]
        [DataRow(8759d, 0d, 1d)]
        [DataRow(0d, 8759d, 1d)]
        [DataRow(10d, 20d, 10d)]
        [DataRow(100d, 8750d, 110d)]
        [DataRow(0d, 4380d, 4380d)]
        public void CircularHourDiffWrapsAtYearBoundary(double a, double b, double expected)
        {
            Assert.AreEqual(expected, Query.CircularHourDiff(a, b));
        }
    }
}
