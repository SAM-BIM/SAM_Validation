using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkSpaceResult
    {
        [JsonPropertyName("guid")]
        [JsonPropertyOrder(0)]
        public string? Guid { get; set; }

        [JsonPropertyName("name")]
        [JsonPropertyOrder(1)]
        public string? Name { get; set; }

        [JsonPropertyName("area")]
        [JsonPropertyOrder(2)]
        public MetricValue? Area { get; set; }

        [JsonPropertyName("volume")]
        [JsonPropertyOrder(3)]
        public MetricValue? Volume { get; set; }

        [JsonPropertyName("heating")]
        [JsonPropertyOrder(4)]
        public BenchmarkConditionResult? Heating { get; set; }

        [JsonPropertyName("cooling")]
        [JsonPropertyOrder(5)]
        public BenchmarkConditionResult? Cooling { get; set; }
    }
}
