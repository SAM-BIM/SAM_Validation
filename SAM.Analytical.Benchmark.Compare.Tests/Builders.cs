// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    /// <summary>
    /// Fabricates in-memory, schema-valid benchmark documents for portable comparator tests. No real
    /// files, engines or simulation are involved.
    /// </summary>
    internal static class Builders
    {
        internal const string HashModel = "sha256:1111111111111111111111111111111111111111111111111111111111111111";
        internal const string HashCanonical = "sha256:2222222222222222222222222222222222222222222222222222222222222222";
        internal const string HashWeather = "sha256:3333333333333333333333333333333333333333333333333333333333333333";
        internal const string SamCommit = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        internal const string RunnerCommit = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        internal static MetricValue Value(double value, MetricUnit unit) => MetricValue.AvailableValue(value, unit);

        internal static MetricValue Missing(MetricUnit unit) => MetricValue.Unavailable(unit);

        internal static BenchmarkDocument Document(
            EngineKind engine,
            BenchmarkRoute route,
            BenchmarkModelResult model,
            IEnumerable<BenchmarkSpaceResult> spaces,
            string schemaVersion = BenchmarkSchema.CurrentVersion)
        {
            return new BenchmarkDocument
            {
                SchemaVersion = schemaVersion,
                Provenance = Provenance(engine, route),
                Model = model,
                Spaces = new List<BenchmarkSpaceResult>(spaces)
            };
        }

        internal static BenchmarkProvenance Provenance(EngineKind engine, BenchmarkRoute route)
        {
            return new BenchmarkProvenance
            {
                SourceModelName = "SingleBox",
                SourceModelGuid = "0123456789abcdef0123456789abcdef",
                SourceFileHash = HashModel,
                CanonicalModelHash = HashCanonical,
                CanonicalizationVersion = BenchmarkCanonicalization.CurrentVersion,
                SamCommit = SamCommit,
                RunnerCommit = RunnerCommit,
                Engine = new BenchmarkEngine
                {
                    Kind = engine,
                    Name = engine == EngineKind.OpenStudio ? "EnergyPlus" : "TAS",
                    Version = engine == EngineKind.OpenStudio ? "24.1.0" : "9.5.3",
                    SdkVersion = engine == EngineKind.OpenStudio ? "3.10.0" : null
                },
                Route = route,
                Weather = new BenchmarkWeather { Identity = "London-Gatwick.epw", Hash = HashWeather },
                DesignDaySource = DesignDaySource.Ddy,
                RunTimestampUtc = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero),
                DurationSeconds = 12.5,
                State = RunState.Success,
                ResultSources = new List<string> { "AnalyticalModelSimulationResult", "SpaceSimulationResult" },
                Warnings = new List<string>(),
                Notes = new List<string>()
            };
        }

        internal static BenchmarkModelResult Model(
            MetricValue? consumptionHeating = null,
            MetricValue? consumptionCooling = null,
            MetricValue? peakHeatingLoad = null,
            MetricValue? peakHeatingHour = null,
            MetricValue? peakCoolingLoad = null,
            MetricValue? peakCoolingHour = null,
            MetricValue? floorArea = null,
            MetricValue? volume = null)
        {
            return new BenchmarkModelResult
            {
                ConsumptionHeating = consumptionHeating ?? Value(1000, MetricUnit.KilowattHour),
                ConsumptionCooling = consumptionCooling ?? Value(500, MetricUnit.KilowattHour),
                PeakHeatingLoad = peakHeatingLoad ?? Value(10, MetricUnit.Kilowatt),
                PeakHeatingHour = peakHeatingHour ?? Value(200, MetricUnit.HourOfYear),
                PeakCoolingLoad = peakCoolingLoad ?? Value(8, MetricUnit.Kilowatt),
                PeakCoolingHour = peakCoolingHour ?? Value(4600, MetricUnit.HourOfYear),
                FloorArea = floorArea ?? Value(240, MetricUnit.SquareMetre),
                Volume = volume ?? Value(720, MetricUnit.CubicMetre)
            };
        }

        internal static BenchmarkSpaceResult Space(
            string? guid,
            string name,
            double area,
            double volume,
            MetricValue? heatingPeakLoad = null,
            MetricValue? heatingPeakHour = null)
        {
            return new BenchmarkSpaceResult
            {
                Guid = guid,
                Name = name,
                Area = Value(area, MetricUnit.SquareMetre),
                Volume = Value(volume, MetricUnit.CubicMetre),
                Heating = new BenchmarkConditionResult
                {
                    DesignLoad = Missing(MetricUnit.Watt),
                    PeakLoad = heatingPeakLoad ?? Value(2000, MetricUnit.Watt),
                    PeakHour = heatingPeakHour ?? Value(205, MetricUnit.HourOfYear),
                    UnmetHours = Value(0, MetricUnit.Hour)
                },
                Cooling = new BenchmarkConditionResult
                {
                    DesignLoad = Missing(MetricUnit.Watt),
                    PeakLoad = Missing(MetricUnit.Watt),
                    PeakHour = Missing(MetricUnit.HourOfYear),
                    UnmetHours = Missing(MetricUnit.Hour)
                }
            };
        }
    }
}
