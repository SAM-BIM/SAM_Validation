using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Tests
{
    internal static class TestDocuments
    {
        internal const string HashA = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        internal const string HashB = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        internal const string HashC = "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

        internal static BenchmarkDocument CreateValid()
        {
            return new BenchmarkDocument
            {
                SchemaVersion = BenchmarkSchema.CurrentVersion,
                Provenance = new BenchmarkProvenance
                {
                    SourceModelName = "SingleBox",
                    SourceModelGuid = "0123456789abcdef0123456789abcdef",
                    SourceFileHash = HashA,
                    CanonicalModelHash = HashB,
                    CanonicalizationVersion = "1.0.0",
                    SamCommit = "1111111111111111111111111111111111111111",
                    RunnerCommit = "2222222222222222222222222222222222222222",
                    Engine = new BenchmarkEngine
                    {
                        Kind = EngineKind.OpenStudio,
                        Name = "EnergyPlus",
                        Version = "24.1.0",
                        SdkVersion = "3.10.0"
                    },
                    Route = BenchmarkRoute.NativeOpenStudio,
                    Weather = new BenchmarkWeather { Identity = "London-Gatwick.epw", Hash = HashC },
                    DesignDaySource = DesignDaySource.Ddy,
                    RunTimestampUtc = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 12.3,
                    State = RunState.Success,
                    ResultSources = new List<string> { "SpaceSimulationResult", "AnalyticalModelSimulationResult" },
                    Warnings = new List<string> { "Zulu warning", "Alpha warning" },
                    Notes = new List<string> { "second note", "first note" }
                },
                Model = new BenchmarkModelResult
                {
                    ConsumptionHeating = MetricValue.AvailableValue(1234.5, MetricUnit.KilowattHour),
                    ConsumptionCooling = MetricValue.AvailableValue(0, MetricUnit.KilowattHour),
                    PeakHeatingLoad = MetricValue.AvailableValue(12.3, MetricUnit.Kilowatt),
                    PeakHeatingHour = MetricValue.AvailableValue(0, MetricUnit.HourOfYear),
                    PeakCoolingLoad = MetricValue.AvailableValue(9.8, MetricUnit.Kilowatt),
                    PeakCoolingHour = MetricValue.AvailableValue(8759, MetricUnit.HourOfYear),
                    FloorArea = MetricValue.AvailableValue(240, MetricUnit.SquareMetre),
                    Volume = MetricValue.AvailableValue(720, MetricUnit.CubicMetre)
                },
                Spaces = new List<BenchmarkSpaceResult>
                {
                    CreateSpace("fedcba9876543210fedcba9876543210", "Office B", 40, 120, 2100),
                    CreateSpace("0123456789abcdef0123456789abcdef", "Office A", 200, 600, 2200)
                }
            };
        }

        private static BenchmarkSpaceResult CreateSpace(string guid, string name, double area, double volume, double peakLoad)
        {
            return new BenchmarkSpaceResult
            {
                Guid = guid,
                Name = name,
                Area = MetricValue.AvailableValue(area, MetricUnit.SquareMetre),
                Volume = MetricValue.AvailableValue(volume, MetricUnit.CubicMetre),
                Heating = new BenchmarkConditionResult
                {
                    DesignLoad = MetricValue.Unavailable(MetricUnit.Watt),
                    PeakLoad = MetricValue.AvailableValue(peakLoad, MetricUnit.Watt),
                    PeakHour = MetricValue.AvailableValue(205, MetricUnit.HourOfYear),
                    UnmetHours = MetricValue.AvailableValue(0, MetricUnit.Hour)
                },
                Cooling = new BenchmarkConditionResult
                {
                    DesignLoad = MetricValue.Unavailable(MetricUnit.Watt),
                    PeakLoad = MetricValue.Unavailable(MetricUnit.Watt),
                    PeakHour = MetricValue.Unavailable(MetricUnit.HourOfYear),
                    UnmetHours = MetricValue.Unavailable(MetricUnit.Hour)
                }
            };
        }
    }
}
