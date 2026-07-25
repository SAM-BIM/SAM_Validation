// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

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
                // TOLERANCES.md: percentage bands do not apply to hour-of-year, and until the absolute hour
                // thresholds are reviewed peak-hour differences are INFORMATIONAL and cannot produce a Fail.
                // The band is therefore capped at Warn (match within the warn hours, otherwise warn) and is
                // additionally excluded from the numerical gate (see NumericalStatus). The fail-hours
                // threshold is still carried on the profile and reported, ready for a future promotion.
                absolute = CircularHourDiff(tasValue, openStudioValue);
                signed = null; // A circular difference has no single meaningful sign across the year boundary.
                relative = null;
                band = absolute <= profile.HourWarnAbsolute ? ComparisonBand.Match : ComparisonBand.Warn;
            }
            else
            {
                // TOLERANCES.md difference calculation: the near-zero floor is the lower bound of the SCALE
                // (the denominator), not an additive allowance. scale = max(|tas|, |os|, floor); the band is
                // taken from the relative difference against that scale. This keeps a tiny denominator from
                // exploding into an extreme percentage, WITHOUT masking a genuine near-zero disagreement
                // (e.g. 0 vs floor is a 100% difference, not a match).
                signed = openStudioValue - tasValue;
                absolute = Math.Abs(signed.Value);
                double floor = profile.NearZeroFloor(unit);
                double scale = Math.Max(Math.Max(Math.Abs(tasValue), Math.Abs(openStudioValue)), floor);
                relative = scale > 0 ? absolute / scale : 0d;
                band = Classify(relative.Value, profile);
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
        /// Maps a relative difference to a band using the profile's boundaries: below the warn boundary is
        /// a match, at least warn but below fail warns, at least fail fails (TOLERANCES.md: Pass &lt; 5%,
        /// 5% &lt;= Warn &lt; 15%, Fail &gt;= 15%).
        /// </summary>
        private static ComparisonBand Classify(double relative, ToleranceProfile profile)
        {
            if (relative < profile.WarnRelative)
            {
                return ComparisonBand.Match;
            }

            return relative < profile.FailRelative ? ComparisonBand.Warn : ComparisonBand.Fail;
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

    }
}
