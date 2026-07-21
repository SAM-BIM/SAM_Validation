// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Benchmark
{
    public static class BenchmarkValidator
    {
        private static readonly Regex GuidPattern = new Regex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant);
        private static readonly Regex HashPattern = new Regex("^sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant);
        private static readonly Regex CommitPattern = new Regex("^[0-9a-f]{7,64}$", RegexOptions.CultureInvariant);

        public static BenchmarkValidationResult Validate(BenchmarkDocument? document)
        {
            var issues = new List<ValidationIssue>();
            if (document == null)
            {
                AddError(issues, "document.missing", "$", "The benchmark document is required.");
                return new BenchmarkValidationResult(issues);
            }

            ValidateSchemaVersion(document.SchemaVersion, issues);
            ValidateProvenance(document.Provenance, issues);
            ValidateModel(document.Model, issues);
            ValidateSpaces(document.Spaces, document.Provenance?.Route ?? BenchmarkRoute.Unknown, issues);
            return new BenchmarkValidationResult(issues);
        }

        public static void ThrowIfInvalid(BenchmarkDocument? document)
        {
            BenchmarkValidationResult result = Validate(document);
            if (!result.IsValid)
            {
                throw new BenchmarkValidationException(result);
            }
        }

        private static void ValidateSchemaVersion(string? version, ICollection<ValidationIssue> issues)
        {
            switch (BenchmarkSchema.GetCompatibility(version))
            {
                case SchemaCompatibility.Malformed:
                    AddError(issues, "schema.version.malformed", "$.schemaVersion", "A semantic version in major.minor.patch form is required.");
                    break;
                case SchemaCompatibility.IncompatibleMajor:
                    AddError(issues, "schema.version.major", "$.schemaVersion", $"Schema major version must match {BenchmarkSchema.CurrentVersion}.");
                    break;
                case SchemaCompatibility.CompatibleWithMinorWarning:
                    AddWarning(issues, "schema.version.minor", "$.schemaVersion", $"Schema minor version differs from {BenchmarkSchema.CurrentVersion}; known v1 fields will be validated.");
                    break;
            }
        }

        private static void ValidateProvenance(BenchmarkProvenance? provenance, ICollection<ValidationIssue> issues)
        {
            if (provenance == null)
            {
                AddError(issues, "provenance.missing", "$.provenance", "Provenance is required.");
                return;
            }

            RequirePortableIdentity(provenance.SourceModelName, "$.provenance.sourceModelName", issues);
            ValidateOptionalGuid(provenance.SourceModelGuid, "$.provenance.sourceModelGuid", issues);
            ValidateOptionalHash(provenance.SourceFileHash, "$.provenance.sourceFileHash", issues);
            ValidateOptionalHash(provenance.CanonicalModelHash, "$.provenance.canonicalModelHash", issues);

            if (!BenchmarkSchema.TryParseVersion(provenance.CanonicalizationVersion, out _, out _, out _))
            {
                AddError(issues, "provenance.canonicalizationVersion", "$.provenance.canonicalizationVersion", "A semantic canonicalization version is required.");
            }

            RequireCommit(provenance.SamCommit, "$.provenance.samCommit", issues);
            RequireCommit(provenance.RunnerCommit, "$.provenance.runnerCommit", issues);
            ValidateEngine(provenance.Engine, issues);
            ValidateEnum(provenance.Route, BenchmarkRoute.Unknown, "provenance.route", "$.provenance.route", issues);
            ValidateWeather(provenance.Weather, issues);
            ValidateEnum(provenance.DesignDaySource, DesignDaySource.Unknown, "provenance.designDaySource", "$.provenance.designDaySource", issues);

            if (provenance.RunTimestampUtc == default)
            {
                AddError(issues, "provenance.timestamp.missing", "$.provenance.runTimestampUtc", "A UTC run timestamp is required.");
            }
            else if (provenance.RunTimestampUtc.Offset != TimeSpan.Zero)
            {
                AddError(issues, "provenance.timestamp.utc", "$.provenance.runTimestampUtc", "The run timestamp must be UTC.");
            }

            if (!provenance.DurationSeconds.HasValue)
            {
                AddError(issues, "provenance.duration.missing", "$.provenance.durationSeconds", "Run duration is required.");
            }
            else if (!IsFinite(provenance.DurationSeconds.Value) || provenance.DurationSeconds.Value < 0)
            {
                AddError(issues, "provenance.duration.invalid", "$.provenance.durationSeconds", "Run duration must be a finite, non-negative number.");
            }

            ValidateEnum(provenance.State, RunState.Unknown, "provenance.state", "$.provenance.state", issues);
            ValidateStringList(provenance.ResultSources, "resultSources", "$.provenance.resultSources", issues, requireDistinct: true);
            ValidateStringList(provenance.Warnings, "warnings", "$.provenance.warnings", issues, requireDistinct: false);
            ValidateStringList(provenance.Notes, "notes", "$.provenance.notes", issues, requireDistinct: false);
            ValidateRouteEngine(provenance.Route, provenance.Engine?.Kind ?? EngineKind.Unknown, issues);

            bool hasExplanation = (provenance.Warnings?.Count ?? 0) > 0 || (provenance.Notes?.Count ?? 0) > 0;
            if (provenance.State == RunState.Success)
            {
                RequireGuid(provenance.SourceModelGuid, "$.provenance.sourceModelGuid", issues);
                RequireHash(provenance.SourceFileHash, "$.provenance.sourceFileHash", issues);
                RequireHash(provenance.CanonicalModelHash, "$.provenance.canonicalModelHash", issues);
                RequireHash(provenance.Weather?.Hash, "$.provenance.weather.hash", issues);
            }
            else if (provenance.State == RunState.Failure && !hasExplanation)
            {
                AddError(issues, "provenance.failure.explanation", "$.provenance", "A failed run must include a warning or note explaining the failure.");
            }

            if (provenance.Engine != null && provenance.Engine.Version == null && !hasExplanation)
            {
                AddError(issues, "provenance.engine.versionExplanation", "$.provenance.engine.version", "An unavailable engine version must be explained in warnings or notes.");
            }

        }

        private static void ValidateEngine(BenchmarkEngine? engine, ICollection<ValidationIssue> issues)
        {
            if (engine == null)
            {
                AddError(issues, "provenance.engine.missing", "$.provenance.engine", "Engine information is required.");
                return;
            }

            ValidateEnum(engine.Kind, EngineKind.Unknown, "provenance.engine.kind", "$.provenance.engine.kind", issues);
            RequireText(engine.Name, "provenance.engine.name", "$.provenance.engine.name", issues);
            OptionalText(engine.Version, "provenance.engine.version", "$.provenance.engine.version", issues);
            OptionalText(engine.SdkVersion, "provenance.engine.sdkVersion", "$.provenance.engine.sdkVersion", issues);
        }

        private static void ValidateWeather(BenchmarkWeather? weather, ICollection<ValidationIssue> issues)
        {
            if (weather == null)
            {
                AddError(issues, "provenance.weather.missing", "$.provenance.weather", "Weather information is required.");
                return;
            }

            RequirePortableIdentity(weather.Identity, "$.provenance.weather.identity", issues);
            ValidateOptionalHash(weather.Hash, "$.provenance.weather.hash", issues);
        }

        private static void ValidateModel(BenchmarkModelResult? model, ICollection<ValidationIssue> issues)
        {
            if (model == null)
            {
                AddError(issues, "model.missing", "$.model", "Model results are required.");
                return;
            }

            ValidateMetric(model.ConsumptionHeating, MetricUnit.KilowattHour, "$.model.consumptionHeating", MetricKind.NonNegative, issues);
            ValidateMetric(model.ConsumptionCooling, MetricUnit.KilowattHour, "$.model.consumptionCooling", MetricKind.NonNegative, issues);
            ValidateMetric(model.PeakHeatingLoad, MetricUnit.Kilowatt, "$.model.peakHeatingLoad", MetricKind.NonNegative, issues);
            ValidateMetric(model.PeakHeatingHour, MetricUnit.HourOfYear, "$.model.peakHeatingHour", MetricKind.HourOfYear, issues);
            ValidateMetric(model.PeakCoolingLoad, MetricUnit.Kilowatt, "$.model.peakCoolingLoad", MetricKind.NonNegative, issues);
            ValidateMetric(model.PeakCoolingHour, MetricUnit.HourOfYear, "$.model.peakCoolingHour", MetricKind.HourOfYear, issues);
            ValidateMetric(model.FloorArea, MetricUnit.SquareMetre, "$.model.floorArea", MetricKind.NonNegative, issues);
            ValidateMetric(model.Volume, MetricUnit.CubicMetre, "$.model.volume", MetricKind.NonNegative, issues);
        }

        private static void ValidateSpaces(IList<BenchmarkSpaceResult>? spaces, BenchmarkRoute route, ICollection<ValidationIssue> issues)
        {
            if (spaces == null)
            {
                AddError(issues, "spaces.missing", "$.spaces", "The spaces array is required.");
                return;
            }

            var guids = new HashSet<string>(StringComparer.Ordinal);
            var fallbackNames = new HashSet<string>(StringComparer.Ordinal);
            bool nativeRoute = route == BenchmarkRoute.NativeOpenStudio || route == BenchmarkRoute.NativeTas;

            for (int index = 0; index < spaces.Count; index++)
            {
                BenchmarkSpaceResult? space = spaces[index];
                string path = $"$.spaces[{index}]";
                if (space == null)
                {
                    AddError(issues, "space.missing", path, "A space result cannot be null.");
                    continue;
                }

                if (space.Guid == string.Empty)
                {
                    AddError(issues, "space.guid.empty", path + ".guid", "A space GUID must be null or a 32-character lowercase hexadecimal value.");
                }
                else if (space.Guid != null)
                {
                    if (!GuidPattern.IsMatch(space.Guid))
                    {
                        AddError(issues, "space.guid.malformed", path + ".guid", "A space GUID must contain 32 lowercase hexadecimal characters.");
                    }
                    else if (!guids.Add(space.Guid))
                    {
                        AddError(issues, "space.guid.duplicate", path + ".guid", "Space GUIDs must be unique.");
                    }
                }
                else if (nativeRoute)
                {
                    AddError(issues, "space.guid.required", path + ".guid", "Native routes require a source SAM space GUID.");
                }

                RequireText(space.Name, "space.name.missing", path + ".name", issues);
                if (space.Guid == null && !string.IsNullOrWhiteSpace(space.Name))
                {
                    string normalizedName = NormalizeName(space.Name!);
                    if (!fallbackNames.Add(normalizedName))
                    {
                        AddError(issues, "space.name.duplicateFallback", path + ".name", "GUID-less fallback space names must be unique after normalization.");
                    }
                }

                if (space.Guid == null && string.IsNullOrWhiteSpace(space.Name))
                {
                    AddError(issues, "space.identity.missing", path, "A space requires a GUID or non-empty name identity.");
                }

                ValidateMetric(space.Area, MetricUnit.SquareMetre, path + ".area", MetricKind.NonNegative, issues);
                ValidateMetric(space.Volume, MetricUnit.CubicMetre, path + ".volume", MetricKind.NonNegative, issues);
                ValidateCondition(space.Heating, path + ".heating", issues);
                ValidateCondition(space.Cooling, path + ".cooling", issues);
            }
        }

        private static void ValidateCondition(BenchmarkConditionResult? condition, string path, ICollection<ValidationIssue> issues)
        {
            if (condition == null)
            {
                AddError(issues, "condition.missing", path, "Heating and cooling condition results are required.");
                return;
            }

            ValidateMetric(condition.DesignLoad, MetricUnit.Watt, path + ".designLoad", MetricKind.NonNegative, issues);
            ValidateMetric(condition.PeakLoad, MetricUnit.Watt, path + ".peakLoad", MetricKind.NonNegative, issues);
            ValidateMetric(condition.PeakHour, MetricUnit.HourOfYear, path + ".peakHour", MetricKind.HourOfYear, issues);
            ValidateMetric(condition.UnmetHours, MetricUnit.Hour, path + ".unmetHours", MetricKind.NonNegative, issues);
        }

        private static void ValidateMetric(MetricValue? metric, MetricUnit expectedUnit, string path, MetricKind kind, ICollection<ValidationIssue> issues)
        {
            if (metric == null)
            {
                AddError(issues, "metric.missing", path, "The metric wrapper is required, even when the value is unavailable.");
                return;
            }

            if (!Enum.IsDefined(typeof(MetricUnit), metric.Unit) || metric.Unit == MetricUnit.Unknown)
            {
                AddError(issues, "metric.unit.unsupported", path + ".unit", "A supported metric unit is required.");
            }
            else if (metric.Unit != expectedUnit)
            {
                AddError(issues, "metric.unit.incompatible", path + ".unit", $"Expected unit {expectedUnit}.");
            }

            if (metric.Available && !metric.Value.HasValue)
            {
                AddError(issues, "metric.availability.valueRequired", path, "An available metric must have a value.");
            }
            else if (!metric.Available && metric.Value.HasValue)
            {
                AddError(issues, "metric.availability.valueForbidden", path, "An unavailable metric must have a null value.");
            }

            if (!metric.Value.HasValue)
            {
                return;
            }

            double value = metric.Value.Value;
            if (!IsFinite(value))
            {
                AddError(issues, "metric.value.nonFinite", path + ".value", "Metric values must be finite.");
                return;
            }

            if (kind == MetricKind.NonNegative && value < 0)
            {
                AddError(issues, "metric.value.negative", path + ".value", "This metric must be non-negative.");
            }
            else if (kind == MetricKind.HourOfYear && (value < 0 || value > 8759 || value != Math.Truncate(value)))
            {
                AddError(issues, "metric.value.hourOfYear", path + ".value", "Hour-of-year must be an integer from 0 through 8759.");
            }
        }

        private static void ValidateOptionalHash(string? value, string path, ICollection<ValidationIssue> issues)
        {
            if (value != null && !HashPattern.IsMatch(value))
            {
                AddError(issues, "provenance.hash.malformed", path, "Hashes must use sha256: followed by 64 lowercase hexadecimal characters.");
            }
        }

        private static void RequireHash(string? value, string path, ICollection<ValidationIssue> issues)
        {
            if (value == null)
            {
                AddError(issues, "provenance.hash.required", path, "A successful run requires this SHA-256 hash.");
            }
        }

        private static void ValidateOptionalGuid(string? value, string path, ICollection<ValidationIssue> issues)
        {
            if (value != null && !GuidPattern.IsMatch(value))
            {
                AddError(issues, "provenance.guid.malformed", path, "GUIDs must contain 32 lowercase hexadecimal characters.");
            }
        }

        private static void RequireGuid(string? value, string path, ICollection<ValidationIssue> issues)
        {
            if (value == null)
            {
                AddError(issues, "provenance.guid.required", path, "A successful run requires the source model GUID.");
            }
        }

        private static void RequireCommit(string? value, string path, ICollection<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value) || !CommitPattern.IsMatch(value))
            {
                AddError(issues, "provenance.commit.malformed", path, "A source revision of 7 to 64 lowercase hexadecimal characters is required.");
            }
        }

        private static void RequirePortableIdentity(string? value, string path, ICollection<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(issues, "provenance.identity.missing", path, "A portable identity is required.");
                return;
            }

            if (Path.IsPathRooted(value!) || value!.StartsWith("/", StringComparison.Ordinal) || value.StartsWith("\\", StringComparison.Ordinal)
                || Regex.IsMatch(value, "^[A-Za-z]:[\\\\/]", RegexOptions.CultureInvariant))
            {
                AddError(issues, "provenance.identity.path", path, "Portable identities must not be absolute machine paths.");
            }
        }

        private static void RequireText(string? value, string code, string path, ICollection<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(issues, code, path, "A non-empty value is required.");
            }
        }

        private static void OptionalText(string? value, string code, string path, ICollection<ValidationIssue> issues)
        {
            if (value != null && string.IsNullOrWhiteSpace(value))
            {
                AddError(issues, code, path, "Use null rather than an empty value.");
            }
        }

        private static void ValidateStringList(IList<string>? values, string name, string path, ICollection<ValidationIssue> issues, bool requireDistinct)
        {
            if (values == null)
            {
                AddError(issues, $"provenance.{name}.missing", path, $"The {name} array is required.");
                return;
            }

            var distinct = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < values.Count; index++)
            {
                string? value = values[index];
                if (string.IsNullOrWhiteSpace(value))
                {
                    AddError(issues, $"provenance.{name}.empty", $"{path}[{index}]", "List entries must be non-empty.");
                }
                else if (requireDistinct && !distinct.Add(value))
                {
                    AddError(issues, $"provenance.{name}.duplicate", $"{path}[{index}]", "List entries must be distinct.");
                }
            }
        }

        private static void ValidateRouteEngine(BenchmarkRoute route, EngineKind engine, ICollection<ValidationIssue> issues)
        {
            bool openStudioRoute = route == BenchmarkRoute.NativeOpenStudio || route == BenchmarkRoute.SharedGbXmlOpenStudio;
            bool tasRoute = route == BenchmarkRoute.NativeTas || route == BenchmarkRoute.SharedGbXmlTas;
            if ((openStudioRoute && engine != EngineKind.OpenStudio) || (tasRoute && engine != EngineKind.Tas))
            {
                AddError(issues, "provenance.routeEngine.mismatch", "$.provenance.route", "The route must match the declared engine kind.");
            }
        }

        private static void ValidateEnum<T>(T value, T unknown, string code, string path, ICollection<ValidationIssue> issues) where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value) || EqualityComparer<T>.Default.Equals(value, unknown))
            {
                AddError(issues, code, path, "A supported value is required.");
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string NormalizeName(string value)
        {
            return value.Trim().Normalize().ToUpperInvariant();
        }

        private static void AddError(ICollection<ValidationIssue> issues, string code, string path, string message)
        {
            issues.Add(new ValidationIssue(code, path, message, ValidationSeverity.Error));
        }

        private static void AddWarning(ICollection<ValidationIssue> issues, string code, string path, string message)
        {
            issues.Add(new ValidationIssue(code, path, message, ValidationSeverity.Warning));
        }

        private enum MetricKind
        {
            NonNegative,
            HourOfYear
        }
    }
}
