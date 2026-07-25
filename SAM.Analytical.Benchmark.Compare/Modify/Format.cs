// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// Deterministic, invariant-culture formatting shared by every report writer. Numeric values are
    /// emitted at full binary64 precision (the shortest round-trippable form) with NO rounding, so the
    /// reports are byte-stable and exactly reproducible.
    /// </summary>
    internal static class Format
    {
        internal static string Number(double? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        internal static string Percent(double? relative)
        {
            // The relative difference is a fraction; a percentage is a convenience view of the same
            // underlying value and is likewise emitted without rounding.
            return relative.HasValue ? (relative.Value * 100d).ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        internal static string Unit(MetricUnit unit)
        {
            switch (unit)
            {
                case MetricUnit.KilowattHour: return "kWh";
                case MetricUnit.WattHour: return "Wh";
                case MetricUnit.Kilowatt: return "kW";
                case MetricUnit.Watt: return "W";
                case MetricUnit.SquareMetre: return "m2";
                case MetricUnit.CubicMetre: return "m3";
                case MetricUnit.HourOfYear: return "hourOfYear";
                case MetricUnit.Hour: return "h";
                default: return "unknown";
            }
        }

        internal static string Band(ComparisonBand band)
        {
            switch (band)
            {
                case ComparisonBand.Match: return "Match";
                case ComparisonBand.Warn: return "Warn";
                case ComparisonBand.Fail: return "Fail";
                default: return "N/A";
            }
        }

        internal static string Gate(GateStatus gate)
        {
            switch (gate)
            {
                case GateStatus.Pass: return "Pass";
                case GateStatus.Warn: return "Warn";
                default: return "Fail";
            }
        }

        internal static string Availability(ComparisonAvailability availability)
        {
            switch (availability)
            {
                case ComparisonAvailability.Both: return "Both";
                case ComparisonAvailability.TasOnly: return "TasOnly";
                case ComparisonAvailability.OpenStudioOnly: return "OpenStudioOnly";
                default: return "Neither";
            }
        }

        internal static string MatchKind(SpaceMatchKind kind)
        {
            switch (kind)
            {
                case SpaceMatchKind.Guid: return "Guid";
                case SpaceMatchKind.Name: return "Name";
                case SpaceMatchKind.TasOnly: return "TasOnly";
                default: return "OpenStudioOnly";
            }
        }

        internal static string NotApplicableReason(NotApplicableReason reason)
        {
            switch (reason)
            {
                case Compare.NotApplicableReason.Unavailable: return "unavailable";
                case Compare.NotApplicableReason.UnitMismatch: return "unit-mismatch";
                default: return string.Empty;
            }
        }

        /// <summary>The marker appended to a metric that carries a band which cannot move the numerical status.</summary>
        internal const string InformationalNote = "informational (excluded from the numerical status)";

        internal static string NotApplicableNote(MetricComparison metric)
        {
            switch (metric.NotApplicableReason)
            {
                case Compare.NotApplicableReason.Unavailable:
                    return "unavailable (" + Availability(metric.Availability) + ")";
                case Compare.NotApplicableReason.UnitMismatch:
                    return "unit-mismatch (tas=" + Unit(metric.TasUnit) + ", openStudio=" + Unit(metric.OpenStudioUnit) + ")";
                default:
                    return metric.Circular ? "circular hour difference" : string.Empty;
            }
        }

        /// <summary>
        /// The per-row note shown in the Markdown and CSV tables: the N/A explanation plus, for a metric
        /// that DOES carry a real band but is designated informational, an explicit marker. Without it a
        /// reader would see a Fail band on a whole-model peak load or a peak hour and reasonably assume it
        /// contributed to the gate.
        /// </summary>
        internal static string MetricNote(MetricComparison metric)
        {
            string note = NotApplicableNote(metric);
            if (metric.Band == ComparisonBand.NotApplicable || !Query.IsInformationalForNumericalGate(metric))
            {
                return note;
            }

            return note.Length == 0 ? InformationalNote : note + "; " + InformationalNote;
        }
    }
}
