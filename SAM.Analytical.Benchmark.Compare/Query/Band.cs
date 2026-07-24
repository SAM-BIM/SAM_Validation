// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using GateStatusValue = SAM.Analytical.Benchmark.Compare.GateStatus;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Query
    {
        /// <summary>The number of hours in a non-leap year, used to wrap circular hour-of-year differences.</summary>
        public const int HoursInYear = 8760;

        /// <summary>
        /// Assigns a tolerance band to a single metric. Availability wins first: if either side is
        /// unavailable the band is <see cref="ComparisonBand.NotApplicable"/> (never a failure). Available
        /// pairs with mismatched units are also N/A. Otherwise the absolute difference is banded against
        /// the profile's combined near-zero-floor + relative limit; hour-of-year metrics use the circular
        /// difference and the profile's absolute hour limits.
        /// </summary>
        public static MetricComparison Band(string scope, string key, MetricValue? tas, MetricValue? openStudio, ToleranceProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            MetricValue tasMetric = tas ?? MetricValue.Unavailable(MetricUnit.Unknown);
            MetricValue openStudioMetric = openStudio ?? MetricValue.Unavailable(MetricUnit.Unknown);

            bool tasAvailable = tasMetric.Available && tasMetric.Value.HasValue;
            bool openStudioAvailable = openStudioMetric.Available && openStudioMetric.Value.HasValue;

            MetricUnit tasUnit = tasMetric.Unit;
            MetricUnit openStudioUnit = openStudioMetric.Unit;

            // Prefer an available side's unit as the canonical reporting unit; fall back to whichever is set.
            MetricUnit unit = tasAvailable ? tasUnit
                : openStudioAvailable ? openStudioUnit
                : tasUnit != MetricUnit.Unknown ? tasUnit : openStudioUnit;

            if (!tasAvailable || !openStudioAvailable)
            {
                return new MetricComparison(
                    scope, key, unit,
                    tasAvailable ? tasMetric.Value : null, tasAvailable,
                    openStudioAvailable ? openStudioMetric.Value : null, openStudioAvailable,
                    circular: false,
                    absoluteDifference: null,
                    relativeDifference: null,
                    signedDifference: null,
                    band: ComparisonBand.NotApplicable,
                    notApplicableReason: NotApplicableReason.Unavailable,
                    tasUnit: tasUnit,
                    openStudioUnit: openStudioUnit);
            }

            if (tasUnit != openStudioUnit)
            {
                // Both sides carry a value but in different units: comparing them would be meaningless.
                return new MetricComparison(
                    scope, key, unit,
                    tasMetric.Value, true,
                    openStudioMetric.Value, true,
                    circular: false,
                    absoluteDifference: null,
                    relativeDifference: null,
                    signedDifference: null,
                    band: ComparisonBand.NotApplicable,
                    notApplicableReason: NotApplicableReason.UnitMismatch,
                    tasUnit: tasUnit,
                    openStudioUnit: openStudioUnit);
            }

            double tasValue = tasMetric.Value!.Value;
            double openStudioValue = openStudioMetric.Value!.Value;
            bool circular = unit == MetricUnit.HourOfYear;

            double absolute;
            double? signed;
            double? relative;
            ComparisonBand band;

            if (circular)
            {
                absolute = CircularHourDiff(tasValue, openStudioValue);
                signed = null; // A circular difference has no single meaningful sign across the year boundary.
                relative = null;
                band = absolute <= profile.HourWarnAbsolute ? ComparisonBand.Match
                    : absolute <= profile.HourFailAbsolute ? ComparisonBand.Warn
                    : ComparisonBand.Fail;
            }
            else
            {
                signed = openStudioValue - tasValue;
                absolute = Math.Abs(signed.Value);
                double magnitude = Math.Max(Math.Abs(tasValue), Math.Abs(openStudioValue));
                relative = magnitude > 0 ? absolute / magnitude : (double?)(absolute == 0 ? 0 : null);

                double floor = profile.NearZeroFloor(unit);
                double warnLimit = floor + (profile.WarnRelative * magnitude);
                double failLimit = floor + (profile.FailRelative * magnitude);
                band = absolute <= warnLimit ? ComparisonBand.Match
                    : absolute <= failLimit ? ComparisonBand.Warn
                    : ComparisonBand.Fail;
            }

            return new MetricComparison(
                scope, key, unit,
                tasValue, true,
                openStudioValue, true,
                circular,
                absolute,
                relative,
                signed,
                band,
                NotApplicableReason.None,
                tasUnit,
                openStudioUnit);
        }

        /// <summary>
        /// The circular (year-boundary-wrapping) distance between two hour-of-year values in [0, 8759]:
        /// <c>min(|a-b|, 8760-|a-b|)</c>, so hour 8759 and hour 0 are one hour apart, not 8759.
        /// </summary>
        public static double CircularHourDiff(double a, double b)
        {
            double direct = Math.Abs(a - b);
            double wrapped = HoursInYear - direct;
            return Math.Min(direct, wrapped);
        }

        /// <summary>The worst applicable band across the metrics maps to the overall gate status.</summary>
        public static GateStatusValue GateStatus(IEnumerable<MetricComparison> metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            bool anyWarn = false;
            foreach (MetricComparison metric in metrics)
            {
                if (metric.Band == ComparisonBand.Fail)
                {
                    return GateStatusValue.Fail;
                }

                if (metric.Band == ComparisonBand.Warn)
                {
                    anyWarn = true;
                }
            }

            return anyWarn ? GateStatusValue.Warn : GateStatusValue.Pass;
        }
    }
}
