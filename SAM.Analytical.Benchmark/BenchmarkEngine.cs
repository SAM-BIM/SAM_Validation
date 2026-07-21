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
