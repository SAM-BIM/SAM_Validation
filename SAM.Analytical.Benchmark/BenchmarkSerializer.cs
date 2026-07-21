using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public static class BenchmarkSerializer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false, true);
        private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

        public static string Serialize(BenchmarkDocument document)
        {
            BenchmarkValidator.ThrowIfInvalid(document);
            BenchmarkDocument canonicalDocument = Canonicalize(document);
            string json = JsonSerializer.Serialize(canonicalDocument, SerializerOptions);
            return json.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        public static byte[] SerializeToUtf8(BenchmarkDocument document)
        {
            return Utf8WithoutBom.GetBytes(Serialize(document));
        }

        public static BenchmarkDocument Deserialize(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            BenchmarkDocument? document = JsonSerializer.Deserialize<BenchmarkDocument>(json, SerializerOptions);
            BenchmarkValidator.ThrowIfInvalid(document);
            return document!;
        }

        public static BenchmarkDocument Deserialize(ReadOnlySpan<byte> utf8)
        {
            BenchmarkDocument? document = JsonSerializer.Deserialize<BenchmarkDocument>(utf8, SerializerOptions);
            BenchmarkValidator.ThrowIfInvalid(document);
            return document!;
        }

        public static void Write(string path, BenchmarkDocument document)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("An output path is required.", nameof(path));
            }

            File.WriteAllBytes(path, SerializeToUtf8(document));
        }

        public static BenchmarkDocument Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("An input path is required.", nameof(path));
            }

            return Deserialize(File.ReadAllBytes(path));
        }

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNameCaseInsensitive = false,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = true
            };
            options.Converters.Add(new BenchmarkRouteConverter());
            options.Converters.Add(new EngineKindConverter());
            options.Converters.Add(new MetricUnitConverter());
            options.Converters.Add(new RunStateConverter());
            options.Converters.Add(new DesignDaySourceConverter());
            options.Converters.Add(new UtcDateTimeOffsetConverter());
            return options;
        }

        private static BenchmarkDocument Canonicalize(BenchmarkDocument source)
        {
            return new BenchmarkDocument
            {
                SchemaVersion = source.SchemaVersion,
                Provenance = Clone(source.Provenance!),
                Model = Clone(source.Model!),
                Spaces = source.Spaces!
                    .OrderBy(x => string.IsNullOrEmpty(x.Guid) ? "\uffff" : x.Guid, StringComparer.Ordinal)
                    .ThenBy(x => NormalizeName(x.Name), StringComparer.Ordinal)
                    .ThenBy(x => x.Name, StringComparer.Ordinal)
                    .Select(Clone)
                    .ToList()
            };
        }

        private static BenchmarkProvenance Clone(BenchmarkProvenance source)
        {
            return new BenchmarkProvenance
            {
                SourceModelName = source.SourceModelName,
                SourceModelGuid = source.SourceModelGuid,
                SourceFileHash = source.SourceFileHash,
                CanonicalModelHash = source.CanonicalModelHash,
                CanonicalizationVersion = source.CanonicalizationVersion,
                SamCommit = source.SamCommit,
                RunnerCommit = source.RunnerCommit,
                Engine = Clone(source.Engine!),
                Route = source.Route,
                Weather = Clone(source.Weather!),
                DesignDaySource = source.DesignDaySource,
                RunTimestampUtc = source.RunTimestampUtc,
                DurationSeconds = source.DurationSeconds,
                State = source.State,
                ResultSources = Sort(source.ResultSources!),
                Warnings = Sort(source.Warnings!),
                Notes = Sort(source.Notes!)
            };
        }

        private static BenchmarkEngine Clone(BenchmarkEngine source)
        {
            return new BenchmarkEngine
            {
                Kind = source.Kind,
                Name = source.Name,
                Version = source.Version,
                SdkVersion = source.SdkVersion
            };
        }

        private static BenchmarkWeather Clone(BenchmarkWeather source)
        {
            return new BenchmarkWeather { Identity = source.Identity, Hash = source.Hash };
        }

        private static BenchmarkModelResult Clone(BenchmarkModelResult source)
        {
            return new BenchmarkModelResult
            {
                ConsumptionHeating = Clone(source.ConsumptionHeating!),
                ConsumptionCooling = Clone(source.ConsumptionCooling!),
                PeakHeatingLoad = Clone(source.PeakHeatingLoad!),
                PeakHeatingHour = Clone(source.PeakHeatingHour!),
                PeakCoolingLoad = Clone(source.PeakCoolingLoad!),
                PeakCoolingHour = Clone(source.PeakCoolingHour!),
                FloorArea = Clone(source.FloorArea!),
                Volume = Clone(source.Volume!)
            };
        }

        private static BenchmarkSpaceResult Clone(BenchmarkSpaceResult source)
        {
            return new BenchmarkSpaceResult
            {
                Guid = source.Guid,
                Name = source.Name,
                Area = Clone(source.Area!),
                Volume = Clone(source.Volume!),
                Heating = Clone(source.Heating!),
                Cooling = Clone(source.Cooling!)
            };
        }

        private static BenchmarkConditionResult Clone(BenchmarkConditionResult source)
        {
            return new BenchmarkConditionResult
            {
                DesignLoad = Clone(source.DesignLoad!),
                PeakLoad = Clone(source.PeakLoad!),
                PeakHour = Clone(source.PeakHour!),
                UnmetHours = Clone(source.UnmetHours!)
            };
        }

        private static MetricValue Clone(MetricValue source)
        {
            return new MetricValue { Value = source.Value, Unit = source.Unit, Available = source.Available };
        }

        private static List<string> Sort(IEnumerable<string> values)
        {
            return values.OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        private static string NormalizeName(string? value)
        {
            return value?.Trim().Normalize().ToUpperInvariant() ?? string.Empty;
        }
    }
}
