// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Modify
    {
        /// <summary>The version of the comparator-owned comparison-summary document.</summary>
        public const string SummarySchemaVersion = "1.0.0";

        private static readonly JsonWriterOptions SummaryWriterOptions = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.Default,
            SkipValidation = false
        };

        /// <summary>
        /// Renders <c>comparison-summary.json</c>: the comparator-owned, versioned machine-readable
        /// summary. Property order is fixed in code and numbers are written at full binary64 precision, so
        /// the output is deterministic and byte-stable (LF newlines, UTF-8 without BOM when written).
        /// </summary>
        public static string WriteSummaryJson(ComparisonResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, SummaryWriterOptions))
            {
                writer.WriteStartObject();
                writer.WriteString("summarySchemaVersion", SummarySchemaVersion);
                writer.WriteString("toleranceProfile", result.ToleranceProfileName);
                WriteToleranceProfile(writer, result.ToleranceProfile);
                writer.WriteString("gate", Format.Gate(result.Gate));

                writer.WriteStartObject("statuses");
                writer.WriteString("overall", Format.Gate(result.Gate));
                writer.WriteString("numerical", Format.Gate(result.NumericalStatus));
                writer.WriteString("coverage", Format.Gate(result.CoverageStatus));
                writer.WriteString("provenance", Format.Gate(result.ProvenanceStatus));
                writer.WriteString("reconciliation", Format.Gate(result.ReconciliationStatus));
                writer.WriteEndObject();

                WriteProvenanceCompatibility(writer, result.ProvenanceCompatibility);

                writer.WriteStartObject("counts");
                writer.WriteNumber("match", result.MatchCount);
                writer.WriteNumber("warn", result.WarnCount);
                writer.WriteNumber("fail", result.FailCount);
                writer.WriteNumber("notApplicable", result.NotApplicableCount);
                writer.WriteEndObject();

                writer.WriteStartObject("schema");
                WriteStringOrNull(writer, "tas", result.TasSchemaVersion);
                WriteStringOrNull(writer, "openStudio", result.OpenStudioSchemaVersion);
                WriteStringOrNull(writer, "driftNote", result.SchemaDriftNote);
                writer.WriteEndObject();

                writer.WriteStartObject("provenance");
                WriteProvenance(writer, "tas", result.TasProvenance);
                WriteProvenance(writer, "openStudio", result.OpenStudioProvenance);
                writer.WriteEndObject();

                writer.WriteStartArray("modelMetrics");
                foreach (MetricComparison metric in result.ModelMetrics)
                {
                    WriteMetric(writer, metric);
                }

                writer.WriteEndArray();

                writer.WriteStartArray("spaces");
                foreach (SpaceComparison space in result.Spaces)
                {
                    WriteSpace(writer, space);
                }

                writer.WriteEndArray();

                WriteDiagnostics(writer, result.SpaceDiagnostics);

                writer.WriteStartArray("reconciliation");
                foreach (ReconciliationResult reconciliation in result.Reconciliations)
                {
                    WriteReconciliation(writer, reconciliation);
                }

                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            string json = Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n").Replace('\r', '\n');
            return json + "\n";
        }

        private static void WriteSpace(Utf8JsonWriter writer, SpaceComparison space)
        {
            writer.WriteStartObject();
            writer.WriteString("scope", space.Scope);
            WriteStringOrNull(writer, "guid", space.Guid);
            WriteStringOrNull(writer, "tasName", space.TasName);
            WriteStringOrNull(writer, "openStudioName", space.OpenStudioName);
            writer.WriteString("matchKind", Format.MatchKind(space.MatchKind));
            writer.WriteStartArray("metrics");
            foreach (MetricComparison metric in space.Metrics)
            {
                WriteMetric(writer, metric);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static void WriteMetric(Utf8JsonWriter writer, MetricComparison metric)
        {
            writer.WriteStartObject();
            writer.WriteString("scope", metric.Scope);
            writer.WriteString("key", metric.Key);
            writer.WriteString("unit", Format.Unit(metric.Unit));
            writer.WriteString("availability", Format.Availability(metric.Availability));
            writer.WriteString("band", Format.Band(metric.Band));
            WriteStringOrNull(writer, "notApplicableReason", NullIfEmpty(Format.NotApplicableReason(metric.NotApplicableReason)));
            writer.WriteBoolean("circular", metric.Circular);

            WriteSide(writer, "tas", metric.TasValue, metric.TasAvailable, metric.TasUnit);
            WriteSide(writer, "openStudio", metric.OpenStudioValue, metric.OpenStudioAvailable, metric.OpenStudioUnit);

            WriteNumberOrNull(writer, "absoluteDifference", metric.AbsoluteDifference);
            WriteNumberOrNull(writer, "relativeDifference", metric.RelativeDifference);
            WriteNumberOrNull(writer, "signedDifference", metric.SignedDifference);
            writer.WriteEndObject();
        }

        private static void WriteSide(Utf8JsonWriter writer, string name, double? value, bool available, MetricUnit unit)
        {
            writer.WriteStartObject(name);
            WriteNumberOrNull(writer, "value", value);
            writer.WriteBoolean("available", available);
            writer.WriteString("unit", Format.Unit(unit));
            writer.WriteEndObject();
        }

        private static void WriteReconciliation(Utf8JsonWriter writer, ReconciliationResult reconciliation)
        {
            writer.WriteStartObject();
            writer.WriteString("engine", reconciliation.Engine);
            writer.WriteString("key", reconciliation.Key);
            writer.WriteString("unit", Format.Unit(reconciliation.Unit));
            WriteNumberOrNull(writer, "modelTotal", reconciliation.ModelTotal);
            writer.WriteBoolean("modelTotalAvailable", reconciliation.ModelTotalAvailable);
            writer.WriteNumber("sumOfSpaces", reconciliation.SumOfSpaces);
            writer.WriteNumber("contributingSpaces", reconciliation.ContributingSpaces);
            writer.WriteNumber("spacesMissingValue", reconciliation.SpacesMissingValue);
            writer.WriteNumber("excludedSpaces", reconciliation.ExcludedSpaces);
            writer.WriteBoolean("complete", reconciliation.Complete);
            WriteNumberOrNull(writer, "absoluteDifference", reconciliation.AbsoluteDifference);
            WriteNumberOrNull(writer, "relativeDifference", reconciliation.RelativeDifference);
            writer.WriteString("band", Format.Band(reconciliation.Band));
            WriteStringOrNull(writer, "notApplicableReason", NullIfEmpty(Format.NotApplicableReason(reconciliation.NotApplicableReason)));
            writer.WriteEndObject();
        }

        private static void WriteProvenanceCompatibility(Utf8JsonWriter writer, ProvenanceCompatibility compatibility)
        {
            writer.WriteStartObject("provenanceCompatibility");
            writer.WriteBoolean("compatible", compatibility.IsCompatible);
            writer.WriteStartArray("mismatches");
            foreach (ProvenanceMismatch mismatch in compatibility.Mismatches)
            {
                writer.WriteStartObject();
                writer.WriteString("field", mismatch.Field);
                WriteStringOrNull(writer, "tas", mismatch.Tas);
                WriteStringOrNull(writer, "openStudio", mismatch.OpenStudio);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static void WriteToleranceProfile(Utf8JsonWriter writer, ToleranceProfile profile)
        {
            // TOLERANCES.md: reports must include the actual profile values, so a changed profile cannot be
            // hidden behind an unchanged name.
            writer.WriteStartObject("toleranceProfileDetail");
            writer.WriteString("name", profile.Name);
            writer.WriteNumber("warnRelative", profile.WarnRelative);
            writer.WriteNumber("failRelative", profile.FailRelative);
            writer.WriteNumber("hourWarnAbsolute", profile.HourWarnAbsolute);
            writer.WriteNumber("hourFailAbsolute", profile.HourFailAbsolute);
            writer.WriteNumber("defaultNearZeroFloor", profile.DefaultNearZeroFloor);
            writer.WriteStartObject("nearZeroFloors");
            foreach (System.Collections.Generic.KeyValuePair<MetricUnit, double> floor in profile.ConfiguredNearZeroFloors())
            {
                writer.WriteNumber(Format.Unit(floor.Key), floor.Value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static void WriteDiagnostics(Utf8JsonWriter writer, SpaceMatchDiagnostics diagnostics)
        {
            writer.WriteStartObject("spaceDiagnostics");
            writer.WriteNumber("tasCount", diagnostics.TasCount);
            writer.WriteNumber("openStudioCount", diagnostics.OpenStudioCount);
            writer.WriteNumber("matchedByGuid", diagnostics.MatchedByGuid);
            writer.WriteNumber("matchedByName", diagnostics.MatchedByName);
            WriteStringArray(writer, "onlyInTas", diagnostics.OnlyInTas);
            WriteStringArray(writer, "onlyInOpenStudio", diagnostics.OnlyInOpenStudio);
            WriteStringArray(writer, "duplicateTasGuids", diagnostics.DuplicateTasGuids);
            WriteStringArray(writer, "duplicateOpenStudioGuids", diagnostics.DuplicateOpenStudioGuids);
            WriteStringArray(writer, "duplicateTasNames", diagnostics.DuplicateTasNames);
            WriteStringArray(writer, "duplicateOpenStudioNames", diagnostics.DuplicateOpenStudioNames);
            WriteStringArray(writer, "ambiguousNameMatches", diagnostics.AmbiguousNameMatches);
            writer.WriteEndObject();
        }

        private static void WriteProvenance(Utf8JsonWriter writer, string name, BenchmarkProvenance? provenance)
        {
            if (provenance == null)
            {
                writer.WriteNull(name);
                return;
            }

            writer.WriteStartObject(name);
            WriteStringOrNull(writer, "sourceModelName", provenance.SourceModelName);
            WriteStringOrNull(writer, "sourceModelGuid", provenance.SourceModelGuid);
            WriteStringOrNull(writer, "sourceFileHash", provenance.SourceFileHash);
            WriteStringOrNull(writer, "canonicalModelHash", provenance.CanonicalModelHash);
            WriteStringOrNull(writer, "canonicalizationVersion", provenance.CanonicalizationVersion);
            WriteStringOrNull(writer, "samCommit", provenance.SamCommit);
            WriteStringOrNull(writer, "runnerCommit", provenance.RunnerCommit);

            writer.WriteStartObject("engine");
            WriteStringOrNull(writer, "kind", NullIfEmpty(EngineKindToken(provenance.Engine?.Kind)));
            WriteStringOrNull(writer, "name", provenance.Engine?.Name);
            WriteStringOrNull(writer, "version", provenance.Engine?.Version);
            WriteStringOrNull(writer, "sdkVersion", provenance.Engine?.SdkVersion);
            writer.WriteEndObject();

            writer.WriteString("route", RouteToken(provenance.Route));

            writer.WriteStartObject("weather");
            WriteStringOrNull(writer, "identity", provenance.Weather?.Identity);
            WriteStringOrNull(writer, "hash", provenance.Weather?.Hash);
            writer.WriteEndObject();

            writer.WriteString("designDaySource", DesignDayToken(provenance.DesignDaySource));
            writer.WriteString("runTimestampUtc", provenance.RunTimestampUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
            WriteNumberOrNull(writer, "durationSeconds", provenance.DurationSeconds);
            writer.WriteString("state", RunStateToken(provenance.State));
            WriteStringArray(writer, "resultSources", provenance.ResultSources);
            WriteStringArray(writer, "warnings", provenance.Warnings);
            WriteStringArray(writer, "notes", provenance.Notes);
            writer.WriteEndObject();
        }

        private static void WriteStringArray(Utf8JsonWriter writer, string name, IReadOnlyList<string>? values)
        {
            writer.WriteStartArray(name);
            if (values != null)
            {
                foreach (string value in values)
                {
                    writer.WriteStringValue(value);
                }
            }

            writer.WriteEndArray();
        }

        private static void WriteStringOrNull(Utf8JsonWriter writer, string name, string? value)
        {
            if (value == null)
            {
                writer.WriteNull(name);
            }
            else
            {
                writer.WriteString(name, value);
            }
        }

        private static void WriteNumberOrNull(Utf8JsonWriter writer, string name, double? value)
        {
            if (value.HasValue)
            {
                writer.WriteNumber(name, value.Value);
            }
            else
            {
                writer.WriteNull(name);
            }
        }

        private static string? NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
