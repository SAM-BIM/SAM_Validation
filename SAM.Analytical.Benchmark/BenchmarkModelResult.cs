using System.Text.Json.Serialization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkModelResult
    {
        [JsonPropertyName("consumptionHeating")]
        [JsonPropertyOrder(0)]
        public MetricValue? ConsumptionHeating { get; set; }

        [JsonPropertyName("consumptionCooling")]
        [JsonPropertyOrder(1)]
        public MetricValue? ConsumptionCooling { get; set; }

        [JsonPropertyName("peakHeatingLoad")]
        [JsonPropertyOrder(2)]
        public MetricValue? PeakHeatingLoad { get; set; }

        [JsonPropertyName("peakHeatingHour")]
        [JsonPropertyOrder(3)]
        public MetricValue? PeakHeatingHour { get; set; }

        [JsonPropertyName("peakCoolingLoad")]
        [JsonPropertyOrder(4)]
        public MetricValue? PeakCoolingLoad { get; set; }

        [JsonPropertyName("peakCoolingHour")]
        [JsonPropertyOrder(5)]
        public MetricValue? PeakCoolingHour { get; set; }

        [JsonPropertyName("floorArea")]
        [JsonPropertyOrder(6)]
        public MetricValue? FloorArea { get; set; }

        [JsonPropertyName("volume")]
        [JsonPropertyOrder(7)]
        public MetricValue? Volume { get; set; }
    }
}
