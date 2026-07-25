// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Modify
    {
        /// <summary>
        /// Renders a human-readable Markdown report. The content is derived entirely from the comparison
        /// (no wall-clock or machine-specific data is added), so the output is deterministic and
        /// byte-stable. Provisional tolerance bands are labelled as such.
        /// </summary>
        public static string WriteMarkdown(ComparisonResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var builder = new StringBuilder();
            Line(builder, "# Benchmark comparison: TAS vs OpenStudio");
            Line(builder, string.Empty);
            Line(builder, "Independent comparison of two engine-neutral benchmark documents. Tolerance bands are");
            Line(builder, "**provisional reporting buckets**, not validated thresholds. Peak-hour bands are");
            Line(builder, "informational and are excluded from the gate. Reconciliation is a within-document diagnostic,");
            Line(builder, "kept separate from the gate.");
            Line(builder, string.Empty);

            Line(builder, "## Summary");
            Line(builder, string.Empty);
            Line(builder, "| Field | Value |");
            Line(builder, "| --- | --- |");
            Line(builder, Row("Tolerance profile", result.ToleranceProfileName));
            Line(builder, Row("Gate status (overall)", Format.Gate(result.Gate)));
            Line(builder, Row("Numerical status", Format.Gate(result.NumericalStatus)));
            Line(builder, Row("Coverage status", Format.Gate(result.CoverageStatus)));
            Line(builder, Row("Provenance status", Format.Gate(result.ProvenanceStatus)));
            Line(builder, Row("Reconciliation status", Format.Gate(result.ReconciliationStatus)));
            Line(builder, Row("Metrics matched", result.MatchCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, Row("Metrics warned", result.WarnCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, Row("Metrics failed", result.FailCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, Row("Metrics not applicable", result.NotApplicableCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, Row("TAS schema version", result.TasSchemaVersion));
            Line(builder, Row("OpenStudio schema version", result.OpenStudioSchemaVersion));
            Line(builder, string.Empty);

            if (result.SchemaDriftNote != null)
            {
                Line(builder, "> **Schema drift:** " + EscapeInline(result.SchemaDriftNote));
                Line(builder, string.Empty);
            }

            AppendProvenanceCompatibility(builder, result);
            AppendToleranceProfile(builder, result);
            AppendProvenance(builder, result);
            AppendModelMetrics(builder, result);
            AppendReconciliation(builder, result);
            AppendSpaceDiagnostics(builder, result);
            AppendSpaceMetrics(builder, result);

            return builder.ToString();
        }

        private static void AppendProvenanceCompatibility(StringBuilder builder, ComparisonResult result)
        {
            ProvenanceCompatibility compatibility = result.ProvenanceCompatibility;
            Line(builder, "## Provenance compatibility");
            Line(builder, string.Empty);
            if (compatibility.IsCompatible)
            {
                Line(builder, "Compatible: both runs share the same canonical model, weather and design-day basis, with the expected engine and route on each side.");
                Line(builder, string.Empty);
                return;
            }

            Line(builder, "**INCOMPATIBLE** — the two documents are not a valid comparison pair, so a numerical pass is not meaningful.");
            Line(builder, string.Empty);
            Line(builder, "| Field | TAS | OpenStudio |");
            Line(builder, "| --- | --- | --- |");
            foreach (ProvenanceMismatch mismatch in compatibility.Mismatches)
            {
                Line(builder, Row3(mismatch.Field, mismatch.Tas, mismatch.OpenStudio));
            }

            Line(builder, string.Empty);
        }

        private static void AppendToleranceProfile(StringBuilder builder, ComparisonResult result)
        {
            ToleranceProfile profile = result.ToleranceProfile;
            Line(builder, "## Tolerance profile");
            Line(builder, string.Empty);
            Line(builder, "Provisional reporting bands (not validated thresholds). Recorded so a changed profile is never hidden behind an unchanged name.");
            Line(builder, string.Empty);
            Line(builder, "| Setting | Value |");
            Line(builder, "| --- | --- |");
            Line(builder, Row("Name", profile.Name));
            Line(builder, Row("Warn (relative)", Format.Number(profile.WarnRelative)));
            Line(builder, Row("Fail (relative)", Format.Number(profile.FailRelative)));
            Line(builder, Row("Peak-hour warn (h)", Format.Number(profile.HourWarnAbsolute)));
            Line(builder, Row("Peak-hour fail (h)", Format.Number(profile.HourFailAbsolute)));
            Line(builder, Row("Default near-zero floor", Format.Number(profile.DefaultNearZeroFloor)));
            foreach (System.Collections.Generic.KeyValuePair<MetricUnit, double> floor in profile.ConfiguredNearZeroFloors())
            {
                Line(builder, Row("Near-zero floor (" + Format.Unit(floor.Key) + ")", Format.Number(floor.Value)));
            }

            Line(builder, string.Empty);
        }

        private static void AppendProvenance(StringBuilder builder, ComparisonResult result)
        {
            Line(builder, "## Provenance");
            Line(builder, string.Empty);
            Line(builder, "| Field | TAS | OpenStudio |");
            Line(builder, "| --- | --- | --- |");
            BenchmarkProvenance? tas = result.TasProvenance;
            BenchmarkProvenance? openStudio = result.OpenStudioProvenance;
            Line(builder, Row3("Engine kind", EngineKindToken(tas?.Engine?.Kind), EngineKindToken(openStudio?.Engine?.Kind)));
            Line(builder, Row3("Engine name", tas?.Engine?.Name, openStudio?.Engine?.Name));
            Line(builder, Row3("Engine version", tas?.Engine?.Version, openStudio?.Engine?.Version));
            Line(builder, Row3("SDK version", tas?.Engine?.SdkVersion, openStudio?.Engine?.SdkVersion));
            Line(builder, Row3("Route", RouteToken(tas?.Route), RouteToken(openStudio?.Route)));
            Line(builder, Row3("Weather", tas?.Weather?.Identity, openStudio?.Weather?.Identity));
            Line(builder, Row3("Design-day source", DesignDayToken(tas?.DesignDaySource), DesignDayToken(openStudio?.DesignDaySource)));
            Line(builder, Row3("Run state", RunStateToken(tas?.State), RunStateToken(openStudio?.State)));
            Line(builder, Row3("Source model", tas?.SourceModelName, openStudio?.SourceModelName));
            Line(builder, Row3("Source model GUID", tas?.SourceModelGuid, openStudio?.SourceModelGuid));
            Line(builder, Row3("SAM commit", tas?.SamCommit, openStudio?.SamCommit));
            Line(builder, string.Empty);
        }

        private static void AppendModelMetrics(StringBuilder builder, ComparisonResult result)
        {
            Line(builder, "## Model metrics");
            Line(builder, string.Empty);
            AppendMetricTableHeader(builder);
            foreach (MetricComparison metric in result.ModelMetrics)
            {
                AppendMetricRow(builder, metric.Key, metric);
            }

            Line(builder, string.Empty);
        }

        private static void AppendReconciliation(StringBuilder builder, ComparisonResult result)
        {
            Line(builder, "## Model-total vs sum-of-spaces reconciliation");
            Line(builder, string.Empty);
            Line(builder, "Additive quantities only; non-additive peak loads are not reconciled. Only uniquely-matched spaces are summed; unmatched or ambiguous spaces make a reconciliation incomplete.");
            Line(builder, string.Empty);
            Line(builder, "| Engine | Quantity | Unit | Model total | Sum of spaces | Abs diff | Band | Used/Missing/Excluded | Complete |");
            Line(builder, "| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (ReconciliationResult reconciliation in result.Reconciliations)
            {
                Line(builder, "| " + string.Join(" | ", new[]
                {
                    EscapeInline(reconciliation.Engine),
                    EscapeInline(reconciliation.Key),
                    Format.Unit(reconciliation.Unit),
                    EscapeInline(Format.Number(reconciliation.ModelTotal)),
                    EscapeInline(Format.Number(reconciliation.SumOfSpaces)),
                    EscapeInline(Format.Number(reconciliation.AbsoluteDifference)),
                    Format.Band(reconciliation.Band),
                    reconciliation.ContributingSpaces.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + "/" + reconciliation.SpacesMissingValue.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + "/" + reconciliation.ExcludedSpaces.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    reconciliation.Complete ? "yes" : "no"
                }) + " |");
            }

            Line(builder, string.Empty);
        }

        private static void AppendSpaceDiagnostics(StringBuilder builder, ComparisonResult result)
        {
            SpaceMatchDiagnostics diagnostics = result.SpaceDiagnostics;
            Line(builder, "## Space alignment");
            Line(builder, string.Empty);
            Line(builder, "| Field | Value |");
            Line(builder, "| --- | --- |");
            Line(builder, Row("TAS spaces", diagnostics.TasCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, Row("OpenStudio spaces", diagnostics.OpenStudioCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, Row("Matched by GUID", diagnostics.MatchedByGuid.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, Row("Matched by name", diagnostics.MatchedByName.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Line(builder, string.Empty);

            AppendList(builder, "Only in TAS", diagnostics.OnlyInTas);
            AppendList(builder, "Only in OpenStudio", diagnostics.OnlyInOpenStudio);
            AppendList(builder, "Duplicate TAS GUIDs", diagnostics.DuplicateTasGuids);
            AppendList(builder, "Duplicate OpenStudio GUIDs", diagnostics.DuplicateOpenStudioGuids);
            AppendList(builder, "Duplicate TAS names", diagnostics.DuplicateTasNames);
            AppendList(builder, "Duplicate OpenStudio names", diagnostics.DuplicateOpenStudioNames);
            AppendList(builder, "Ambiguous (split) name matches", diagnostics.AmbiguousNameMatches);
        }

        private static void AppendSpaceMetrics(StringBuilder builder, ComparisonResult result)
        {
            Line(builder, "## Space metrics");
            Line(builder, string.Empty);
            if (result.Spaces.Count == 0)
            {
                Line(builder, "_No spaces._");
                Line(builder, string.Empty);
                return;
            }

            Line(builder, "| Space | Match | Metric | Unit | TAS | OpenStudio | Abs diff | Rel % | Band | Note |");
            Line(builder, "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (SpaceComparison space in result.Spaces)
            {
                foreach (MetricComparison metric in space.Metrics)
                {
                    Line(builder, "| " + string.Join(" | ", new[]
                    {
                        EscapeInline(space.Scope),
                        Format.MatchKind(space.MatchKind),
                        EscapeInline(metric.Key),
                        Format.Unit(metric.Unit),
                        EscapeInline(Format.Number(metric.TasValue)),
                        EscapeInline(Format.Number(metric.OpenStudioValue)),
                        EscapeInline(Format.Number(metric.AbsoluteDifference)),
                        EscapeInline(Format.Percent(metric.RelativeDifference)),
                        Format.Band(metric.Band),
                        EscapeInline(Format.NotApplicableNote(metric))
                    }) + " |");
                }
            }

            Line(builder, string.Empty);
        }

        private static void AppendMetricTableHeader(StringBuilder builder)
        {
            Line(builder, "| Metric | Unit | TAS | OpenStudio | Abs diff | Rel % | Band | Note |");
            Line(builder, "| --- | --- | --- | --- | --- | --- | --- | --- |");
        }

        private static void AppendMetricRow(StringBuilder builder, string label, MetricComparison metric)
        {
            Line(builder, "| " + string.Join(" | ", new[]
            {
                EscapeInline(label),
                Format.Unit(metric.Unit),
                EscapeInline(Format.Number(metric.TasValue)),
                EscapeInline(Format.Number(metric.OpenStudioValue)),
                EscapeInline(Format.Number(metric.AbsoluteDifference)),
                EscapeInline(Format.Percent(metric.RelativeDifference)),
                Format.Band(metric.Band),
                EscapeInline(Format.NotApplicableNote(metric))
            }) + " |");
        }

        private static void AppendList(StringBuilder builder, string title, IReadOnlyList<string> values)
        {
            Line(builder, "**" + title + ":** " + (values.Count == 0 ? "none" : values.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            foreach (string value in values)
            {
                Line(builder, "- " + EscapeInline(value));
            }

            Line(builder, string.Empty);
        }

        private static string Row(string label, string? value)
        {
            return "| " + EscapeInline(label) + " | " + EscapeInline(value ?? string.Empty) + " |";
        }

        private static string Row3(string label, string? tas, string? openStudio)
        {
            return "| " + EscapeInline(label) + " | " + EscapeInline(tas ?? string.Empty) + " | " + EscapeInline(openStudio ?? string.Empty) + " |";
        }

        private static void Line(StringBuilder builder, string text)
        {
            builder.Append(text);
            builder.Append('\n');
        }

        private static string EscapeInline(string value)
        {
            // Keep table cells single-line and pipe-safe.
            return value.Replace("\\", "\\\\").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private static string EngineKindToken(EngineKind? kind)
        {
            if (!kind.HasValue)
            {
                return string.Empty;
            }

            switch (kind.Value)
            {
                case EngineKind.OpenStudio: return "OpenStudio";
                case EngineKind.Tas: return "TAS";
                default: return "Unknown";
            }
        }

        private static string RouteToken(BenchmarkRoute? route)
        {
            if (!route.HasValue)
            {
                return string.Empty;
            }

            switch (route.Value)
            {
                case BenchmarkRoute.NativeOpenStudio: return "Native-OpenStudio";
                case BenchmarkRoute.NativeTas: return "Native-TAS";
                case BenchmarkRoute.SharedGbXmlOpenStudio: return "SharedGbXML-OpenStudio";
                case BenchmarkRoute.SharedGbXmlTas: return "SharedGbXML-TAS";
                default: return "Unknown";
            }
        }

        private static string DesignDayToken(DesignDaySource? source)
        {
            if (!source.HasValue)
            {
                return string.Empty;
            }

            switch (source.Value)
            {
                case DesignDaySource.Ddy: return "DDY";
                case DesignDaySource.EmbeddedModel: return "EmbeddedModel";
                case DesignDaySource.None: return "None";
                default: return "Unknown";
            }
        }

        private static string RunStateToken(RunState? state)
        {
            if (!state.HasValue)
            {
                return string.Empty;
            }

            switch (state.Value)
            {
                case RunState.Success: return "Success";
                case RunState.Failure: return "Failure";
                default: return "Unknown";
            }
        }
    }
}
