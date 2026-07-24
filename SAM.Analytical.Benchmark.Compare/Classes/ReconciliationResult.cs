// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// A within-engine reconciliation of an additive whole-model total against the sum of the same
    /// quantity across that engine's spaces (floor area and volume). Non-additive quantities such as peak
    /// loads are deliberately NOT reconciled, because a coincident model peak is not the sum of the
    /// per-space peaks.
    /// </summary>
    public sealed class ReconciliationResult
    {
        public ReconciliationResult(
            string engine,
            string key,
            MetricUnit unit,
            double? modelTotal,
            bool modelTotalAvailable,
            double sumOfSpaces,
            int contributingSpaces,
            int spacesMissingValue,
            double? absoluteDifference,
            double? relativeDifference,
            ComparisonBand band,
            NotApplicableReason notApplicableReason)
        {
            Engine = engine;
            Key = key;
            Unit = unit;
            ModelTotal = modelTotal;
            ModelTotalAvailable = modelTotalAvailable;
            SumOfSpaces = sumOfSpaces;
            ContributingSpaces = contributingSpaces;
            SpacesMissingValue = spacesMissingValue;
            AbsoluteDifference = absoluteDifference;
            RelativeDifference = relativeDifference;
            Band = band;
            NotApplicableReason = notApplicableReason;
        }

        /// <summary>The engine whose internal totals are being reconciled (<c>TAS</c> or <c>OpenStudio</c>).</summary>
        public string Engine { get; }

        /// <summary>The reconciled quantity key (<c>floorArea</c> or <c>volume</c>).</summary>
        public string Key { get; }

        public MetricUnit Unit { get; }

        public double? ModelTotal { get; }

        public bool ModelTotalAvailable { get; }

        public double SumOfSpaces { get; }

        /// <summary>How many spaces contributed an available value to the sum.</summary>
        public int ContributingSpaces { get; }

        /// <summary>How many spaces lacked an available value and were skipped.</summary>
        public int SpacesMissingValue { get; }

        public double? AbsoluteDifference { get; }

        public double? RelativeDifference { get; }

        public ComparisonBand Band { get; }

        public NotApplicableReason NotApplicableReason { get; }
    }
}
