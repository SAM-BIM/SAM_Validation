// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkProvenance
    {
        [JsonPropertyName("sourceModelName")]
        [JsonPropertyOrder(0)]
        public string? SourceModelName { get; set; }

        [JsonPropertyName("sourceModelGuid")]
        [JsonPropertyOrder(1)]
        public string? SourceModelGuid { get; set; }

        [JsonPropertyName("sourceFileHash")]
        [JsonPropertyOrder(2)]
        public string? SourceFileHash { get; set; }

        [JsonPropertyName("canonicalModelHash")]
        [JsonPropertyOrder(3)]
        public string? CanonicalModelHash { get; set; }

        [JsonPropertyName("canonicalizationVersion")]
        [JsonPropertyOrder(4)]
        public string? CanonicalizationVersion { get; set; }

        [JsonPropertyName("samCommit")]
        [JsonPropertyOrder(5)]
        public string? SamCommit { get; set; }

        [JsonPropertyName("runnerCommit")]
        [JsonPropertyOrder(6)]
        public string? RunnerCommit { get; set; }

        [JsonPropertyName("engine")]
        [JsonPropertyOrder(7)]
        public BenchmarkEngine? Engine { get; set; }

        [JsonPropertyName("route")]
        [JsonPropertyOrder(8)]
        public BenchmarkRoute Route { get; set; }

        [JsonPropertyName("weather")]
        [JsonPropertyOrder(9)]
        public BenchmarkWeather? Weather { get; set; }

        [JsonPropertyName("designDaySource")]
        [JsonPropertyOrder(10)]
        public DesignDaySource DesignDaySource { get; set; }

        [JsonPropertyName("runTimestampUtc")]
        [JsonPropertyOrder(11)]
        public DateTimeOffset RunTimestampUtc { get; set; }

        [JsonPropertyName("durationSeconds")]
        [JsonPropertyOrder(12)]
        public double? DurationSeconds { get; set; }

        [JsonPropertyName("state")]
        [JsonPropertyOrder(13)]
        public RunState State { get; set; }

        [JsonPropertyName("resultSources")]
        [JsonPropertyOrder(14)]
        public List<string>? ResultSources { get; set; }

        [JsonPropertyName("warnings")]
        [JsonPropertyOrder(15)]
        public List<string>? Warnings { get; set; }

        [JsonPropertyName("notes")]
        [JsonPropertyOrder(16)]
        public List<string>? Notes { get; set; }
    }
}
