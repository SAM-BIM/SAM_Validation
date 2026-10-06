// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkEngine
    {
        [JsonPropertyName("kind")]
        [JsonPropertyOrder(0)]
        public EngineKind Kind { get; set; }

        [JsonPropertyName("name")]
        [JsonPropertyOrder(1)]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        [JsonPropertyOrder(2)]
        public string? Version { get; set; }

        [JsonPropertyName("sdkVersion")]
        [JsonPropertyOrder(3)]
        public string? SdkVersion { get; set; }
    }
}
