// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkConditionResult
    {
        [JsonPropertyName("designLoad")]
        [JsonPropertyOrder(0)]
        public MetricValue? DesignLoad { get; set; }

        [JsonPropertyName("peakLoad")]
        [JsonPropertyOrder(1)]
        public MetricValue? PeakLoad { get; set; }

        [JsonPropertyName("peakHour")]
        [JsonPropertyOrder(2)]
        public MetricValue? PeakHour { get; set; }

        [JsonPropertyName("unmetHours")]
        [JsonPropertyOrder(3)]
        public MetricValue? UnmetHours { get; set; }
    }
}
