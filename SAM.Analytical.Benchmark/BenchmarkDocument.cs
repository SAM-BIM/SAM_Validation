// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkDocument
    {
        [JsonPropertyName("schemaVersion")]
        [JsonPropertyOrder(0)]
        public string? SchemaVersion { get; set; }

        [JsonPropertyName("provenance")]
        [JsonPropertyOrder(1)]
        public BenchmarkProvenance? Provenance { get; set; }

        [JsonPropertyName("model")]
        [JsonPropertyOrder(2)]
        public BenchmarkModelResult? Model { get; set; }

        [JsonPropertyName("spaces")]
        [JsonPropertyOrder(3)]
        public List<BenchmarkSpaceResult>? Spaces { get; set; }
    }
}
