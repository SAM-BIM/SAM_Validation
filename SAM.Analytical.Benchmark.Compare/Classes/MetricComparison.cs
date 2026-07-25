// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// The comparison of a single metric across the two documents: the raw values, their absolute and
    /// relative differences, the assigned tolerance band, and availability/unit diagnostics.
    /// </summary>
    /// <remarks>
    /// Underlying <see cref="TasValue"/>/<see cref="OpenStudioValue"/> and the differences are stored at
    /// full binary64 precision — reports format them without rounding so the output is deterministic and
    /// exactly reproducible.
    /// </remarks>
    public sealed class MetricComparison
    {
        public MetricComparison(
            string scope,
            string key,
            MetricUnit unit,
            double? tasValue,
            bool tasAvailable,
            double? openStudioValue,
            bool openStudioAvailable,
            bool circular,
            double? absoluteDifference,
            double? relativeDifference,
            double? signedDifference,
            ComparisonBand band,
            NotApplicableReason notApplicableReason,
            MetricUnit tasUnit,
            MetricUnit openStudioUnit)
        {
            Scope = scope;
            Key = key;
            Unit = unit;
            TasValue = tasValue;
            TasAvailable = tasAvailable;
            OpenStudioValue = openStudioValue;
            OpenStudioAvailable = openStudioAvailable;
            Circular = circular;
            AbsoluteDifference = absoluteDifference;
            RelativeDifference = relativeDifference;
            SignedDifference = signedDifference;
            Band = band;
            NotApplicableReason = notApplicableReason;
            TasUnit = tasUnit;
            OpenStudioUnit = openStudioUnit;
        }

        /// <summary>The owning scope: <c>model</c> for whole-model metrics, or a space identity.</summary>
        public string Scope { get; }

        /// <summary>A stable, dotted metric key (e.g. <c>consumptionHeating</c>, <c>heating.peakLoad</c>).</summary>
        public string Key { get; }

        /// <summary>The canonical unit the comparison is expressed in (the shared unit when both agree).</summary>
        public MetricUnit Unit { get; }

        public double? TasValue { get; }

        public bool TasAvailable { get; }

        public double? OpenStudioValue { get; }

        public bool OpenStudioAvailable { get; }

        /// <summary>True when the metric is an hour-of-year and its difference is computed circularly.</summary>
        public bool Circular { get; }

        /// <summary>The non-negative difference used for banding (<c>|os-tas|</c>, or the circular hour diff).</summary>
        public double? AbsoluteDifference { get; }

        /// <summary>
        /// The relative difference <c>|os-tas| / max(|tas|,|os|)</c>, or null when it is not meaningful
        /// (an hour-of-year metric, an unavailable/mismatched metric, or both magnitudes exactly zero).
        /// </summary>
        public double? RelativeDifference { get; }

        /// <summary>The signed difference <c>os-tas</c> (direction), or null for circular hour metrics.</summary>
        public double? SignedDifference { get; }

        public ComparisonBand Band { get; }

        public NotApplicableReason NotApplicableReason { get; }

        /// <summary>The unit declared on the TAS side (used to explain a unit mismatch).</summary>
        public MetricUnit TasUnit { get; }

        /// <summary>The unit declared on the OpenStudio side (used to explain a unit mismatch).</summary>
        public MetricUnit OpenStudioUnit { get; }

        public ComparisonAvailability Availability
        {
            get
            {
                if (TasAvailable && OpenStudioAvailable)
                {
                    return ComparisonAvailability.Both;
                }

                if (TasAvailable)
                {
                    return ComparisonAvailability.TasOnly;
                }

                return OpenStudioAvailable ? ComparisonAvailability.OpenStudioOnly : ComparisonAvailability.Neither;
            }
        }
    }
}
