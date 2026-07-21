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
