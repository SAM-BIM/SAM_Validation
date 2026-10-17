// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Benchmark;
using SAM.Analytical.Benchmark.Compare;
using System;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    [TestClass]
    public sealed class PresentationFormatTests
    {
        private static readonly ToleranceProfile Profile = ToleranceProfile.Default;

        [TestMethod]
        public void EngineeringQuantitiesUseTwoDecimalsAndHoursStayIntegers()
        {
            MetricComparison heating = Metric(
                "consumptionHeating", MetricUnit.KilowattHour,
                742.7750866578222, 2810.0182949034006,
                2067.2432082455784, 0.7356689499121726,
                ComparisonBand.Fail, NotApplicableReason.None);

            Assert.AreEqual("742.78", PresentationFormat.Number(heating.TasValue, heating.Unit));
            Assert.AreEqual("2810.02", PresentationFormat.Number(heating.OpenStudioValue, heating.Unit));
            Assert.AreEqual("2067.24", PresentationFormat.Number(heating.AbsoluteDifference, heating.Unit));
            Assert.AreEqual("73.57", PresentationFormat.Percent(heating.RelativeDifference));

            MetricComparison peakHour = Metric(
                "peakHeatingHour", MetricUnit.HourOfYear,
                871, 919, 48, null,
                ComparisonBand.Fail, NotApplicableReason.None, circular: true);

            Assert.AreEqual("871", PresentationFormat.Number(peakHour.TasValue, peakHour.Unit));
            Assert.AreEqual("919", PresentationFormat.Number(peakHour.OpenStudioValue, peakHour.Unit));
            Assert.AreEqual("48", PresentationFormat.Number(peakHour.AbsoluteDifference, peakHour.Unit));
            Assert.AreEqual(PresentationFormat.Unavailable, PresentationFormat.Percent(peakHour.RelativeDifference));
        }

        [TestMethod]
        public void MissingAndNonFiniteValuesAreNeverZero()
        {
            Assert.AreEqual(PresentationFormat.Unavailable, PresentationFormat.Number(null, MetricUnit.KilowattHour));
            Assert.AreEqual(PresentationFormat.Unavailable, PresentationFormat.Number(double.NaN, MetricUnit.KilowattHour));
            Assert.AreEqual(PresentationFormat.Unavailable, PresentationFormat.Number(double.PositiveInfinity, MetricUnit.KilowattHour));
            Assert.AreEqual(PresentationFormat.Unavailable, PresentationFormat.Percent(null));
            Assert.AreEqual(string.Empty, PresentationFormat.Number(null, MetricUnit.KilowattHour, CultureInfo.InvariantCulture, string.Empty));
        }

        [TestMethod]
        public void InformationalAndUnitMismatchNotesAreDistinct()
        {
            MetricComparison informational = Metric(
                "peakHeatingHour", MetricUnit.HourOfYear,
                871, 919, 48, null,
                ComparisonBand.Fail, NotApplicableReason.None, circular: true);

            string informationalNote = PresentationFormat.Note(informational);
            StringAssert.Contains(informationalNote, PresentationFormat.InformationalNote);
            Assert.IsFalse(informationalNote.Contains("Unit mismatch", StringComparison.Ordinal));

            MetricComparison unitMismatch = Metric(
                "consumptionHeating", MetricUnit.Kilowatt,
                1, 2, null, null,
                ComparisonBand.NotApplicable, NotApplicableReason.UnitMismatch,
                tasUnit: MetricUnit.Kilowatt, openStudioUnit: MetricUnit.Watt);

            string unitMismatchNote = PresentationFormat.Note(unitMismatch);
            StringAssert.Contains(unitMismatchNote, "Unit mismatch (TAS kW vs OpenStudio W)");
            Assert.IsFalse(unitMismatchNote.Contains(PresentationFormat.InformationalNote, StringComparison.Ordinal));
        }

        [TestMethod]
        public void SpaceNameAndGuidAreDisplayedWithoutChangingAlignment()
        {
            SpaceComparison same = new SpaceComparison("cb68bed195af496bb255a13ffa5c58e9", "Office", "Office", SpaceMatchKind.Guid, Array.Empty<MetricComparison>());
            Assert.AreEqual("Office", PresentationFormat.SpaceName(same));
            Assert.AreEqual("cb68bed195af496bb255a13ffa5c58e9", PresentationFormat.SpaceGuid(same));

            SpaceComparison different = new SpaceComparison("cb68bed195af496bb255a13ffa5c58e9", "Office", "Office 2", SpaceMatchKind.Guid, Array.Empty<MetricComparison>());
            Assert.AreEqual("Office / Office 2", PresentationFormat.SpaceName(different));

            SpaceComparison unnamed = new SpaceComparison(null, null, null, SpaceMatchKind.TasOnly, Array.Empty<MetricComparison>());
            Assert.AreEqual("(unnamed)", PresentationFormat.SpaceName(unnamed));
            Assert.AreEqual("(no GUID)", PresentationFormat.SpaceGuid(unnamed));
        }

        [TestMethod]
        public void FailCountsSplitInformationalRowsFromContributingRows()
        {
            ComparisonResult result = Query.Compare(ScenarioDocuments.Tas(), ScenarioDocuments.OpenStudio(), Profile);

            Assert.AreEqual(result.FailCount, result.ContributingFailCount + result.InformationalFailCount);
            Assert.IsTrue(result.InformationalFailCount > 0);
            Assert.IsTrue(result.ContributingFailCount > 0);
        }

        [TestMethod]
        public void MarkdownExplainsPresentationAndKeepsRawReportsAtFullPrecision()
        {
            ComparisonResult result = Query.Compare(ScenarioDocuments.Tas(), ScenarioDocuments.OpenStudio(), Profile);
            string markdown = Modify.WriteMarkdown(result);
            string csv = Modify.WriteCsv(result);
            string summary = Modify.WriteSummaryJson(result);

            StringAssert.Contains(markdown, "Normalized difference (%) = 100 × |OpenStudio − TAS| / max(|TAS|, |OpenStudio|, applicable near-zero floor).");
            StringAssert.Contains(markdown, "**not** percentage change against TAS");
            StringAssert.Contains(markdown, "Match < 5.00%");
            StringAssert.Contains(markdown, "Fail >= 15.00%");
            StringAssert.Contains(markdown, PresentationFormat.InformationalNote);
            StringAssert.Contains(markdown, "| Space | GUID |");
            StringAssert.Contains(markdown, "`Design-day source: None` means no design-day source was supplied");
            StringAssert.Contains(markdown, "Input limitation:");

            // The CSV and JSON are the raw contract: the fraction and full precision must remain there.
            StringAssert.Contains(csv, "0.6666666666666666");
            StringAssert.Contains(csv, "66.66666666666666");
            StringAssert.Contains(summary, "\"failRelative\": 0.15");
            Assert.IsFalse(csv.Contains("66.67", StringComparison.Ordinal));
        }

        private static MetricComparison Metric(
            string key,
            MetricUnit unit,
            double? tas,
            double? openStudio,
            double? absoluteDifference,
            double? relativeDifference,
            ComparisonBand band,
            NotApplicableReason reason,
            bool circular = false,
            MetricUnit? tasUnit = null,
            MetricUnit? openStudioUnit = null)
        {
            bool both = band != ComparisonBand.NotApplicable;
            return new MetricComparison(
                "model",
                key,
                unit,
                tas,
                both,
                openStudio,
                both,
                circular,
                absoluteDifference,
                relativeDifference,
                null,
                band,
                reason,
                tasUnit ?? unit,
                openStudioUnit ?? unit);
        }
    }
}
