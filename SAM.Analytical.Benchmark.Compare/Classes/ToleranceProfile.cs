// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// A named, configurable set of provisional tolerance bands. These are reporting buckets only —
    /// they are explicitly NOT validated thresholds and must never be presented as such.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per TOLERANCES.md, a metric's band comes from its RELATIVE difference
    /// <c>|a-b| / max(|a|,|b|, floor(unit))</c> compared against <see cref="WarnRelative"/> and
    /// <see cref="FailRelative"/>. The per-unit <see cref="NearZeroFloor"/> is a lower bound on the SCALE
    /// (the denominator), not an additive allowance: it stops a tiny denominator turning a negligible
    /// absolute difference into an extreme percentage, while still letting a genuine near-zero
    /// disagreement (0 versus the floor is 100%) show up. Hour-of-year metrics ignore the relative term and
    /// use the absolute <see cref="HourWarnAbsolute"/>/<see cref="HourFailAbsolute"/> limits with a
    /// circular (year-boundary-wrapping) difference instead.
    /// </para>
    /// </remarks>
    public sealed class ToleranceProfile
    {
        /// <summary>The default profile name recognised by the CLI.</summary>
        public const string DefaultName = "default";

        private readonly Dictionary<MetricUnit, double> nearZeroFloors;

        public ToleranceProfile(
            string name,
            double warnRelative,
            double failRelative,
            double hourWarnAbsolute,
            double hourFailAbsolute,
            IReadOnlyDictionary<MetricUnit, double> nearZeroFloors,
            double defaultNearZeroFloor)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A tolerance profile name is required.", nameof(name));
            }

            if (!IsFiniteNonNegative(warnRelative) || !IsFiniteNonNegative(failRelative))
            {
                throw new ArgumentException("Relative bands must be finite and non-negative.");
            }

            if (failRelative < warnRelative)
            {
                throw new ArgumentException("The fail band must be at least as wide as the warn band.");
            }

            if (!IsFiniteNonNegative(hourWarnAbsolute) || !IsFiniteNonNegative(hourFailAbsolute) || hourFailAbsolute < hourWarnAbsolute)
            {
                throw new ArgumentException("Hour bands must be finite, non-negative and ordered warn <= fail.");
            }

            if (!IsFiniteNonNegative(defaultNearZeroFloor))
            {
                throw new ArgumentException("The default near-zero floor must be finite and non-negative.", nameof(defaultNearZeroFloor));
            }

            Name = name;
            WarnRelative = warnRelative;
            FailRelative = failRelative;
            HourWarnAbsolute = hourWarnAbsolute;
            HourFailAbsolute = hourFailAbsolute;
            DefaultNearZeroFloor = defaultNearZeroFloor;
            this.nearZeroFloors = new Dictionary<MetricUnit, double>();
            if (nearZeroFloors != null)
            {
                foreach (KeyValuePair<MetricUnit, double> entry in nearZeroFloors)
                {
                    if (!IsFiniteNonNegative(entry.Value))
                    {
                        throw new ArgumentException("Near-zero floors must be finite and non-negative.", nameof(nearZeroFloors));
                    }

                    this.nearZeroFloors[entry.Key] = entry.Value;
                }
            }
        }

        /// <summary>The profile name, echoed into every report for provenance.</summary>
        public string Name { get; }

        /// <summary>The relative half-width of the warn ("green") band, e.g. 0.05 for ±5%.</summary>
        public double WarnRelative { get; }

        /// <summary>The relative half-width of the fail ("amber/red" boundary) band, e.g. 0.15 for ±15%.</summary>
        public double FailRelative { get; }

        /// <summary>The absolute hour-of-year difference within which a peak-hour comparison matches.</summary>
        public double HourWarnAbsolute { get; }

        /// <summary>The absolute hour-of-year difference within which a peak-hour comparison only warns.</summary>
        public double HourFailAbsolute { get; }

        /// <summary>The near-zero absolute floor used when a metric's unit has no explicit floor.</summary>
        public double DefaultNearZeroFloor { get; }

        /// <summary>The provisional default profile: ±5% warn, ±15% fail, ±1h warn / ±24h fail on peak hours.</summary>
        public static ToleranceProfile Default { get; } = CreateDefault();

        /// <summary>The per-unit near-zero absolute floor (falls back to <see cref="DefaultNearZeroFloor"/>).</summary>
        public double NearZeroFloor(MetricUnit unit)
        {
            return nearZeroFloors.TryGetValue(unit, out double floor) ? floor : DefaultNearZeroFloor;
        }

        /// <summary>
        /// The explicitly-configured per-unit near-zero floors, in ascending unit order, so reports can
        /// record the exact profile values (TOLERANCES.md requires the actual values, not just the name).
        /// </summary>
        public IReadOnlyList<KeyValuePair<MetricUnit, double>> ConfiguredNearZeroFloors()
        {
            var floors = new List<KeyValuePair<MetricUnit, double>>(nearZeroFloors);
            floors.Sort((left, right) => ((int)left.Key).CompareTo((int)right.Key));
            return floors;
        }

        /// <summary>Resolves a profile by (case-insensitive) name. Currently only <c>default</c> exists.</summary>
        public static ToleranceProfile Resolve(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase))
            {
                return Default;
            }

            throw new ArgumentException($"Unknown tolerance profile '{name}'. The only supported profile is '{DefaultName}'.", nameof(name));
        }

        private static ToleranceProfile CreateDefault()
        {
            // Provisional per-unit floors: magnitudes at or below these are treated as effectively zero,
            // so a benign difference between two near-zero readings does not explode into a large relative
            // difference. They are NOT validated significance thresholds.
            var floors = new Dictionary<MetricUnit, double>
            {
                [MetricUnit.KilowattHour] = 1.0,   // 1 kWh
                [MetricUnit.WattHour] = 1000.0,    // 1 kWh expressed in Wh
                [MetricUnit.Kilowatt] = 0.01,      // 10 W
                [MetricUnit.Watt] = 10.0,          // 10 W
                [MetricUnit.SquareMetre] = 0.01,   // 0.01 m2
                [MetricUnit.CubicMetre] = 0.01,    // 0.01 m3
                [MetricUnit.Hour] = 0.5            // half an unmet hour
            };

            return new ToleranceProfile(
                DefaultName,
                warnRelative: 0.05,
                failRelative: 0.15,
                hourWarnAbsolute: 1.0,
                hourFailAbsolute: 24.0,
                nearZeroFloors: floors,
                defaultNearZeroFloor: 0.0);
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
        }
    }
}
