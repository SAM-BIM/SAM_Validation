// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Benchmark;
using System;
using System.Globalization;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// Presentation-only formatting for the comparison view and the Markdown report.
    /// <para>
    /// The raw <see cref="Format.Number"/> and <see cref="Format.Percent"/> remain the full-precision,
    /// invariant contract used by comparison.csv and comparison-summary.json. This class never changes a
    /// value, a unit, a band or a calculation: it only renders an already-computed value for people.
    /// </para>
    /// </summary>
    public static class PresentationFormat
    {
        /// <summary>The marker for a value an engine did not report. It is never rendered as zero.</summary>
        public const string Unavailable = "n/a";

        /// <summary>The marker for a quantity that has no meaningful percentage (a circular hour).</summary>
        public const string NotApplicable = "—";

        /// <summary>The exact label used for a band that is reported but excluded from the numerical status.</summary>
        public const string InformationalNote = "Informational — excluded from numerical status";

        /// <summary>
        /// Two-decimal presentation of an engineering quantity; hour-of-year values stay integers.
        /// Null, NaN and infinity render as <paramref name="unavailable"/> (empty for the UI) and are
        /// never substituted with zero.
        /// </summary>
        public static string Number(double? value, MetricUnit unit, IFormatProvider? provider = null, string unavailable = Unavailable)
        {
            if (!IsFinite(value))
            {
                return unavailable;
            }

            provider ??= CultureInfo.InvariantCulture;
            if (unit == MetricUnit.HourOfYear)
            {
                return value!.Value.ToString("0", provider);
            }

            return value!.Value.ToString("0.00", provider);
        }

        /// <summary>
        /// Two-decimal presentation of a fraction as a percentage. Null, NaN and infinity render as
        /// <paramref name="unavailable"/>. The underlying fraction is not changed.
        /// </summary>
        public static string Percent(double? relative, IFormatProvider? provider = null, string unavailable = Unavailable)
        {
            if (!IsFinite(relative))
            {
                return unavailable;
            }

            provider ??= CultureInfo.InvariantCulture;
            return (relative!.Value * 100d).ToString("0.00", provider);
        }

        /// <summary>Two-decimal percentage with a percent sign, for a displayed tolerance threshold.</summary>
        public static string Threshold(double fraction, IFormatProvider? provider = null)
        {
            return Percent(fraction, provider) + "%";
        }

        /// <summary>
        /// Human-readable note for a metric in the view and Markdown report. Informational rows keep their
        /// band and carry the comparator's authoritative label; a unit mismatch is kept distinct from an
        /// informational metric and from an unavailable value.
        /// </summary>
        public static string Note(MetricComparison metric)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            string note;
            switch (metric.NotApplicableReason)
            {
                case NotApplicableReason.Unavailable:
                    note = "Unavailable (" + AvailabilityText(metric.Availability) + ")";
                    break;
                case NotApplicableReason.UnitMismatch:
                    note = "Unit mismatch (TAS " + Format.Unit(metric.TasUnit) + " vs OpenStudio " + Format.Unit(metric.OpenStudioUnit) + ")";
                    break;
                default:
                    note = metric.Circular ? "Circular hour difference" : string.Empty;
                    break;
            }

            if (metric.Band != ComparisonBand.NotApplicable && Query.IsInformationalForNumericalGate(metric))
            {
                note = note.Length == 0 ? InformationalNote : note + "; " + InformationalNote;
            }

            return note;
        }

        /// <summary>The space name for display, without changing the alignment that produced it.</summary>
        public static string SpaceName(SpaceComparison space)
        {
            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }

            bool hasTas = !string.IsNullOrWhiteSpace(space.TasName);
            bool hasOpenStudio = !string.IsNullOrWhiteSpace(space.OpenStudioName);
            if (hasTas && hasOpenStudio)
            {
                return string.Equals(space.TasName, space.OpenStudioName, StringComparison.Ordinal)
                    ? space.TasName!
                    : space.TasName + " / " + space.OpenStudioName;
            }

            if (hasTas)
            {
                return space.TasName!;
            }

            if (hasOpenStudio)
            {
                return space.OpenStudioName!;
            }

            return "(unnamed)";
        }

        /// <summary>The complete identifier shown beside a space, or an explicit marker when there is none.</summary>
        public static string SpaceGuid(SpaceComparison space)
        {
            if (space == null)
            {
                throw new ArgumentNullException(nameof(space));
            }

            return string.IsNullOrWhiteSpace(space.Guid) ? "(no GUID)" : space.Guid!;
        }

        /// <summary>A compact identifier for a grid cell; the complete value stays available to inspect/copy.</summary>
        public static string CompactGuid(string? guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return string.Empty;
            }

            string value = guid!.Trim();
            return value.Length <= 12 ? value : value.Substring(0, 8) + "…";
        }

        /// <summary>The explicit limitation that metadata equality is not evidence of converted-input equality.</summary>
        public const string InputManifestLimitation =
            "Matching canonical model, weather and design-day metadata does not prove that the two engines received equivalent converted inputs. " +
            "Per-engine input manifests are unavailable (SAM_OpenStudio#30, SAM_UI#230).";

        private static bool IsFinite(double? value)
        {
            return value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);
        }

        private static string AvailabilityText(ComparisonAvailability availability)
        {
            switch (availability)
            {
                case ComparisonAvailability.Both:
                    return "both engines";
                case ComparisonAvailability.TasOnly:
                    return "Tas only";
                case ComparisonAvailability.OpenStudioOnly:
                    return "OpenStudio only";
                default:
                    return "neither engine";
            }
        }
    }
}
