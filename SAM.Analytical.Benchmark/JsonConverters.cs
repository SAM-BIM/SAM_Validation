// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    internal abstract class StringValueConverter<T> : JsonConverter<T> where T : struct
    {
        private readonly IReadOnlyDictionary<string, T> values;
        private readonly IReadOnlyDictionary<T, string> names;

        protected StringValueConverter(IReadOnlyDictionary<string, T> values)
        {
            this.values = values;
            var reverse = new Dictionary<T, string>();
            foreach (KeyValuePair<string, T> item in values)
            {
                reverse.Add(item.Value, item.Key);
            }

            names = reverse;
        }

        protected abstract T Unknown { get; }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected a string value for {typeof(T).Name}.");
            }

            string? value = reader.GetString();
            return value != null && values.TryGetValue(value, out T result) ? result : Unknown;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (!names.TryGetValue(value, out string? name))
            {
                throw new JsonException($"Unsupported {typeof(T).Name} value '{value}'.");
            }

            writer.WriteStringValue(name);
        }
    }

    internal sealed class BenchmarkRouteConverter : StringValueConverter<BenchmarkRoute>
    {
        public BenchmarkRouteConverter()
            : base(new Dictionary<string, BenchmarkRoute>(StringComparer.Ordinal)
            {
                ["Native-OpenStudio"] = BenchmarkRoute.NativeOpenStudio,
                ["Native-TAS"] = BenchmarkRoute.NativeTas,
                ["SharedGbXML-OpenStudio"] = BenchmarkRoute.SharedGbXmlOpenStudio,
                ["SharedGbXML-TAS"] = BenchmarkRoute.SharedGbXmlTas
            })
        {
        }

        protected override BenchmarkRoute Unknown => BenchmarkRoute.Unknown;
    }

    internal sealed class EngineKindConverter : StringValueConverter<EngineKind>
    {
        public EngineKindConverter()
            : base(new Dictionary<string, EngineKind>(StringComparer.Ordinal)
            {
                ["OpenStudio"] = EngineKind.OpenStudio,
                ["TAS"] = EngineKind.Tas
            })
        {
        }

        protected override EngineKind Unknown => EngineKind.Unknown;
    }

    internal sealed class MetricUnitConverter : StringValueConverter<MetricUnit>
    {
        public MetricUnitConverter()
            : base(new Dictionary<string, MetricUnit>(StringComparer.Ordinal)
            {
                ["kWh"] = MetricUnit.KilowattHour,
                ["Wh"] = MetricUnit.WattHour,
                ["kW"] = MetricUnit.Kilowatt,
                ["W"] = MetricUnit.Watt,
                ["m2"] = MetricUnit.SquareMetre,
                ["m3"] = MetricUnit.CubicMetre,
                ["hourOfYear"] = MetricUnit.HourOfYear,
                ["h"] = MetricUnit.Hour
            })
        {
        }

        protected override MetricUnit Unknown => MetricUnit.Unknown;
    }

    internal sealed class RunStateConverter : StringValueConverter<RunState>
    {
        public RunStateConverter()
            : base(new Dictionary<string, RunState>(StringComparer.Ordinal)
            {
                ["Success"] = RunState.Success,
                ["Failure"] = RunState.Failure
            })
        {
        }

        protected override RunState Unknown => RunState.Unknown;
    }

    internal sealed class DesignDaySourceConverter : StringValueConverter<DesignDaySource>
    {
        public DesignDaySourceConverter()
            : base(new Dictionary<string, DesignDaySource>(StringComparer.Ordinal)
            {
                ["DDY"] = DesignDaySource.Ddy,
                ["EmbeddedModel"] = DesignDaySource.EmbeddedModel,
                ["None"] = DesignDaySource.None
            })
        {
        }

        protected override DesignDaySource Unknown => DesignDaySource.Unknown;
    }

    internal sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected an ISO 8601 timestamp string.");
            }

            string? text = reader.GetString();
            if (text == null || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset value))
            {
                throw new JsonException("Expected an ISO 8601 timestamp.");
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
        }
    }
}
