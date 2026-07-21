// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class MetricValue
    {
        [JsonPropertyName("value")]
        [JsonPropertyOrder(0)]
        public double? Value { get; set; }

        [JsonPropertyName("unit")]
        [JsonPropertyOrder(1)]
        public MetricUnit Unit { get; set; }

        [JsonPropertyName("available")]
        [JsonPropertyOrder(2)]
        public bool Available { get; set; }

        public static MetricValue AvailableValue(double value, MetricUnit unit)
        {
            return new MetricValue { Value = value, Unit = unit, Available = true };
        }

        public static MetricValue Unavailable(MetricUnit unit)
        {
            return new MetricValue { Value = null, Unit = unit, Available = false };
        }
    }
}
