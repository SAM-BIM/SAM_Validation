// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Benchmark;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Modify
    {
        /// <summary>
        /// Renders a human-readable Markdown report. The content is derived entirely from the comparison
        /// (no wall-clock or machine-specific data is added), so the output is deterministic and
        /// byte-stable. Provisional tolerance bands are labelled as such.
        /// <para>
        /// Presentation only: engineering quantities, absolute differences and normalized percentages use
        /// two decimals; hour-of-year values and hour differences stay integers. comparison.csv and
        /// comparison-summary.json continue to use the raw, full-precision <see cref="Format"/> contract.
        /// </para>
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
            Line(builder, "**provisional reporting buckets**, not validated thresholds. The overall gate is the worst of");
            Line(builder, "the numerical, coverage, provenance and reconciliation statuses. Peak-hour bands and the two");
            Line(builder, "whole-model peak-load bands are informational and are excluded from the numerical status — they are");
            Line(builder, "still calculated and shown, and rows carrying such a band are marked in the Note column;");
            Line(builder, "reconciliation is a separate within-document diagnostic status that also contributes to the");
            Line(builder, "overall gate.");
            Line(builder, string.Empty);
            Line(builder, "Presentation: engineering quantities, absolute differences and normalized percentages are shown to");
            Line(builder, "two decimals. `comparison.csv` and `comparison-summary.json` keep full precision. `n/a` = not");
            Line(builder, "reported by at least one engine; `—` = not applicable (for example, no percentage for a circular");
            Line(builder, "hour). Neither marker is ever zero. Hour-of-year values and hour differences are integers.");
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
            Line(builder, Row("Metrics matched", Count(result.MatchCount)));
            Line(builder, Row("Metrics warned", Count(result.WarnCount)));
            Line(builder, Row("Metrics failed (all bands)", Count(result.FailCount)));
            Line(builder, Row("Fail rows contributing to numerical status", Count(result.ContributingFailCount)));
            Line(builder, Row("Fail rows informational (excluded)", Count(result.InformationalFailCount)));
            Line(builder, Row("Metrics not applicable", Count(result.NotApplicableCount)));
            Line(builder, Row("Required metrics compared", Count(result.ComparableMetricCount) + " of " + Count(result.RequiredMetricCount)));
            Line(builder, Row("Required metrics unavailable", Count(result.UnavailableRequiredMetricCount)));
            Line(builder, Row("TAS schema version", result.TasSchemaVersion));
            Line(builder, Row("OpenStudio schema version", result.OpenStudioSchemaVersion));
            Line(builder, string.Empty);
            Line(builder, "Band counts include informational rows: " + Count(result.InformationalFailCount) + " of the "
                + Count(result.FailCount) + " Fail rows are informational and excluded from the numerical status; "
                + Count(result.ContributingFailCount) + " contribute to it.");
            Line(builder, string.Empty);

            if (result.SchemaDriftNote != null)
            {
                Line(builder, "> **Schema drift:** " + EscapeInline(result.SchemaDriftNote));
                Line(builder, string.Empty);
            }

            AppendCoverage(builder, result);
            AppendProvenanceCompatibility(builder, result);
            AppendToleranceProfile(builder, result);
            AppendDifferenceDefinition(builder, result);
            AppendProvenance(builder, result);
            AppendEngineNotes(builder, result);
            AppendModelMetrics(builder, result);
            AppendReconciliation(builder, result);
            AppendSpaceDiagnostics(builder, result);
            AppendSpaceMetrics(builder, result);

            return builder.ToString();
        }

        private static void AppendDifferenceDefinition(StringBuilder builder, ComparisonResult result)
        {
            ToleranceProfile profile = result.ToleranceProfile;
            Line(builder, "## Difference definition");
            Line(builder, string.Empty);
            Line(builder, "Normalized difference (%) = 100 × |OpenStudio − TAS| / max(|TAS|, |OpenStudio|, applicable near-zero floor).");
            Line(builder, string.Empty);
            Line(builder, "It is unsigned and is **not** percentage change against TAS. Peak-hour metrics use the circular hour");
            Line(builder, "difference and have no percentage. Bands use the stored fractions, shown here as Match < "
                + PresentationFormat.Threshold(profile.WarnRelative) + ", Warn "
                + PresentationFormat.Threshold(profile.WarnRelative) + " to < "
                + PresentationFormat.Threshold(profile.FailRelative) + ", Fail >= "
                + PresentationFormat.Threshold(profile.FailRelative) + " (provisional).");
            Line(builder, string.Empty);
        }

        private static void AppendCoverage(StringBuilder builder, ComparisonResult result)
        {
            // TOLERANCES.md: unavailable required metrics and unmatched spaces are counted separately, and a
            // numerically passing report with poor coverage must not be described as complete. Coverage is
            // therefore reported in full, with the reasons it is incomplete spelled out.
            SpaceMatchDiagnostics diagnostics = result.SpaceDiagnostics;
            int unmatchedSpaces = diagnostics.OnlyInTas.Count + diagnostics.OnlyInOpenStudio.Count;
            int duplicateIdentities = diagnostics.DuplicateTasGuids.Count
                + diagnostics.DuplicateOpenStudioGuids.Count
                + diagnostics.DuplicateTasNames.Count
                + diagnostics.DuplicateOpenStudioNames.Count;

            Line(builder, "## Coverage");
            Line(builder, string.Empty);
            Line(builder, "How much of the two runs was actually compared. Required metrics are the whole-model metrics plus");
            Line(builder, "the metrics of uniquely matched spaces; the metrics of one-sided, duplicated or ambiguous spaces are");
            Line(builder, "not counted here because those spaces are reported under space alignment instead. An unavailable");
            Line(builder, "metric is never a numerical failure, so it is recorded here — otherwise missing results could read as");
            Line(builder, "a clean pass.");
            Line(builder, string.Empty);
            Line(builder, "| Field | Value |");
            Line(builder, "| --- | --- |");
            Line(builder, Row("Coverage status", Format.Gate(result.CoverageStatus)));
            Line(builder, Row("Required metrics", Count(result.RequiredMetricCount)));
            Line(builder, Row("Compared", Count(result.ComparableMetricCount)));
            Line(builder, Row("Unavailable (not compared)", Count(result.UnavailableRequiredMetricCount)));
            Line(builder, Row("Unmatched spaces", Count(unmatchedSpaces)));
            Line(builder, Row("Ambiguous (split) space names", Count(diagnostics.AmbiguousNameMatches.Count)));
            Line(builder, Row("Duplicated space identities", Count(duplicateIdentities)));
            Line(builder, string.Empty);

            if (result.CoverageStatus == GateStatus.Pass)
            {
                Line(builder, "Coverage is complete: every space aligned uniquely and every required metric was compared.");
                Line(builder, string.Empty);
                return;
            }

            Line(builder, "**Coverage is INCOMPLETE**, so this comparison must not be described as a complete result:");
            Line(builder, string.Empty);
            if (result.ComparableMetricCount == 0)
            {
                Line(builder, "- no metric could be compared at all.");
            }

            if (result.UnavailableRequiredMetricCount > 0)
            {
                Line(builder, "- " + Count(result.UnavailableRequiredMetricCount) + " of " + Count(result.RequiredMetricCount)
                    + " required metrics were unavailable on at least one side (reported as N/A, never as a numerical failure).");
            }

            if (unmatchedSpaces > 0)
            {
                Line(builder, "- " + Count(unmatchedSpaces) + " space(s) exist on only one side.");
            }

            if (diagnostics.AmbiguousNameMatches.Count > 0)
            {
                Line(builder, "- " + Count(diagnostics.AmbiguousNameMatches.Count) + " space name(s) were ambiguous (split) and could not be aligned.");
            }

            if (duplicateIdentities > 0)
            {
                Line(builder, "- " + Count(duplicateIdentities) + " space identity/identities were duplicated and could not be matched uniquely.");
            }

            Line(builder, string.Empty);
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
            Line(builder, Row("Warn (relative)", PresentationFormat.Threshold(profile.WarnRelative)));
            Line(builder, Row("Fail (relative)", PresentationFormat.Threshold(profile.FailRelative)));
            Line(builder, Row("Peak-hour warn (h)", Count((int)profile.HourWarnAbsolute)));
            Line(builder, Row("Peak-hour fail (h)", Count((int)profile.HourFailAbsolute)));
            Line(builder, Row("Default near-zero floor", PresentationFormat.Number(profile.DefaultNearZeroFloor, MetricUnit.Unknown)));
            foreach (KeyValuePair<MetricUnit, double> floor in profile.ConfiguredNearZeroFloors())
            {
                Line(builder, Row("Near-zero floor (" + Format.Unit(floor.Key) + ")", PresentationFormat.Number(floor.Value, floor.Key)));
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
            Line(builder, "`Design-day source: None` means no design-day source was supplied; sizing-period runs stay disabled.");
            Line(builder, string.Empty);
            Line(builder, "> **Input limitation:** " + PresentationFormat.InputManifestLimitation);
            Line(builder, string.Empty);
        }

        private static void AppendEngineNotes(StringBuilder builder, ComparisonResult result)
        {
            Line(builder, "## Engine warnings and notes");
            Line(builder, string.Empty);
            Line(builder, "These are the producers' own diagnostics; they are not comparison results and do not change a band.");
            Line(builder, string.Empty);
            AppendEngineNotes(builder, "TAS", result.TasProvenance);
            AppendEngineNotes(builder, "OpenStudio", result.OpenStudioProvenance);
        }

        private static void AppendEngineNotes(StringBuilder builder, string engine, BenchmarkProvenance? provenance)
        {
            Line(builder, "### " + engine);
            Line(builder, string.Empty);
            if (provenance == null)
            {
                Line(builder, "_No provenance._");
                Line(builder, string.Empty);
                return;
            }

            IReadOnlyList<string> warnings = provenance.Warnings ?? new List<string>();
            IReadOnlyList<string> notes = provenance.Notes ?? new List<string>();
            if (warnings.Count == 0 && notes.Count == 0)
            {
                Line(builder, "_No warnings or notes._");
                Line(builder, string.Empty);
                return;
            }

            foreach (string warning in warnings)
            {
                Line(builder, "- **Warning:** " + EscapeInline(warning));
            }

            foreach (string note in notes)
            {
                Line(builder, "- **Note:** " + EscapeInline(note));
            }

            Line(builder, string.Empty);
        }

        private static void AppendModelMetrics(StringBuilder builder, ComparisonResult result)
        {
            Line(builder, "## Model metrics");
            Line(builder, string.Empty);
            // TOLERANCES.md / METRICS.md require these methodological limitations to remain visible even when
            // the values fall in Pass, so they are printed with the model metrics regardless of band.
            Line(builder, "> **Caveats (apply even when values pass):** whole-model peak-load semantics differ between");
            Line(builder, "> engines — OpenStudio reports a coincident total, TAS a building-profile maximum — so peak-load");
            Line(builder, "> comparisons are informational, not equivalence claims. `peakHeatingLoad` and `peakCoolingLoad`");
            Line(builder, "> are therefore banded and shown below but **excluded from the numerical status**, exactly like the");
            Line(builder, "> peak-hour metrics; promoting them into the gate needs the B4 corpus and energy-modeller review.");
            Line(builder, "> Every other metric — annual energy, per-space peak loads, unmet hours, geometry — gates normally.");
            Line(builder, "> The primary conditioning pairing (OpenStudio Ideal Loads — TAS thermostats + IZAMs + TBD sizing)");
            Line(builder, "> is a **candidate** pending validation. A tolerance profile cannot remove these limitations.");
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
            Line(builder, "| Engine | Quantity | Unit | Model total | Sum of spaces | Abs diff | Normalized difference (%) | Band | Used/Missing/Excluded | Complete |");
            Line(builder, "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (ReconciliationResult reconciliation in result.Reconciliations)
            {
                Line(builder, "| " + string.Join(" | ", new[]
                {
                    EscapeInline(reconciliation.Engine),
                    EscapeInline(reconciliation.Key),
                    Format.Unit(reconciliation.Unit),
                    EscapeInline(PresentationFormat.Number(reconciliation.ModelTotal, reconciliation.Unit)),
                    EscapeInline(PresentationFormat.Number(reconciliation.SumOfSpaces, reconciliation.Unit)),
                    EscapeInline(PresentationFormat.Number(reconciliation.AbsoluteDifference, reconciliation.Unit)),
                    EscapeInline(reconciliation.RelativeDifference.HasValue
                        ? PresentationFormat.Percent(reconciliation.RelativeDifference)
                        : PresentationFormat.NotApplicable),
                    Format.Band(reconciliation.Band),
                    Count(reconciliation.ContributingSpaces) + "/" + Count(reconciliation.SpacesMissingValue) + "/" + Count(reconciliation.ExcludedSpaces),
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
            Line(builder, Row("TAS spaces", Count(diagnostics.TasCount)));
            Line(builder, Row("OpenStudio spaces", Count(diagnostics.OpenStudioCount)));
            Line(builder, Row("Matched by GUID", Count(diagnostics.MatchedByGuid)));
            Line(builder, Row("Matched by name", Count(diagnostics.MatchedByName)));
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

            Line(builder, "| Space | GUID | Match | Metric | Unit | TAS | OpenStudio | Abs diff | Normalized difference (%) | Band | Note |");
            Line(builder, "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (SpaceComparison space in result.Spaces)
            {
                foreach (MetricComparison metric in space.Metrics)
                {
                    Line(builder, "| " + string.Join(" | ", new[]
                    {
                        EscapeInline(PresentationFormat.SpaceName(space)),
                        EscapeInline(PresentationFormat.SpaceGuid(space)),
                        Format.MatchKind(space.MatchKind),
                        EscapeInline(metric.Key),
                        Format.Unit(metric.Unit),
                        EscapeInline(PresentationFormat.Number(metric.TasValue, metric.Unit)),
                        EscapeInline(PresentationFormat.Number(metric.OpenStudioValue, metric.Unit)),
                        EscapeInline(PresentationDifference(metric)),
                        EscapeInline(PresentationPercent(metric)),
                        Format.Band(metric.Band),
                        EscapeInline(PresentationFormat.Note(metric))
                    }) + " |");
                }
            }

            Line(builder, string.Empty);
        }

        private static void AppendMetricTableHeader(StringBuilder builder)
        {
            Line(builder, "| Metric | Unit | TAS | OpenStudio | Abs diff | Normalized difference (%) | Band | Note |");
            Line(builder, "| --- | --- | --- | --- | --- | --- | --- | --- |");
        }

        private static void AppendMetricRow(StringBuilder builder, string label, MetricComparison metric)
        {
            Line(builder, "| " + string.Join(" | ", new[]
            {
                EscapeInline(label),
                Format.Unit(metric.Unit),
                EscapeInline(PresentationFormat.Number(metric.TasValue, metric.Unit)),
                EscapeInline(PresentationFormat.Number(metric.OpenStudioValue, metric.Unit)),
                EscapeInline(PresentationDifference(metric)),
                EscapeInline(PresentationPercent(metric)),
                Format.Band(metric.Band),
                EscapeInline(PresentationFormat.Note(metric))
            }) + " |");
        }

        private static string PresentationDifference(MetricComparison metric)
        {
            if (!metric.AbsoluteDifference.HasValue)
            {
                return PresentationFormat.Unavailable;
            }

            return PresentationFormat.Number(metric.AbsoluteDifference, metric.Circular ? MetricUnit.HourOfYear : metric.Unit);
        }

        private static string PresentationPercent(MetricComparison metric)
        {
            if (metric.Circular)
            {
                return PresentationFormat.NotApplicable;
            }

            if (!metric.RelativeDifference.HasValue)
            {
                return metric.Band == ComparisonBand.NotApplicable ? PresentationFormat.Unavailable : PresentationFormat.NotApplicable;
            }

            return PresentationFormat.Percent(metric.RelativeDifference);
        }

        private static void AppendList(StringBuilder builder, string title, IReadOnlyList<string> values)
        {
            Line(builder, "**" + title + ":** " + (values.Count == 0 ? "none" : Count(values.Count)));
            foreach (string value in values)
            {
                Line(builder, "- " + EscapeInline(value));
            }

            Line(builder, string.Empty);
        }

        private static string Count(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
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
