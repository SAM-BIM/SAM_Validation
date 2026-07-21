// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkWeather
    {
        [JsonPropertyName("identity")]
        [JsonPropertyOrder(0)]
        public string? Identity { get; set; }

        [JsonPropertyName("hash")]
        [JsonPropertyOrder(1)]
        public string? Hash { get; set; }
    }
}
