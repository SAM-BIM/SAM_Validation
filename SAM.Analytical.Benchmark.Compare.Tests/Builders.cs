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
                // Defaults line up with a single 200 m2 / 600 m3 space so a one-space document reconciles
                // cleanly (model total == sum of spaces) unless a test overrides them.
                FloorArea = floorArea ?? Value(200, MetricUnit.SquareMetre),
                Volume = volume ?? Value(600, MetricUnit.CubicMetre)
            };
        }

        /// <summary>
        /// A space with EVERY v1 per-space metric available, so a comparison built from it has complete
        /// metric coverage. <see cref="Space"/> deliberately leaves the design loads and the cooling group
        /// unavailable (the realistic OpenStudio-native shape), which now makes coverage incomplete.
        /// </summary>
        internal static BenchmarkSpaceResult CompleteSpace(
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
                    DesignLoad = Value(2400, MetricUnit.Watt),
                    PeakLoad = heatingPeakLoad ?? Value(2000, MetricUnit.Watt),
                    PeakHour = heatingPeakHour ?? Value(205, MetricUnit.HourOfYear),
                    UnmetHours = Value(0, MetricUnit.Hour)
                },
                Cooling = new BenchmarkConditionResult
                {
                    DesignLoad = Value(1800, MetricUnit.Watt),
                    PeakLoad = Value(1500, MetricUnit.Watt),
                    PeakHour = Value(4600, MetricUnit.HourOfYear),
                    UnmetHours = Value(0, MetricUnit.Hour)
                }
            };
        }

        /// <summary>A space whose every metric is unavailable, for the no-comparable-coverage case.</summary>
        internal static BenchmarkSpaceResult EmptySpace(string? guid, string name)
        {
            return new BenchmarkSpaceResult
            {
                Guid = guid,
                Name = name,
                Area = Missing(MetricUnit.SquareMetre),
                Volume = Missing(MetricUnit.CubicMetre),
                Heating = MissingCondition(),
                Cooling = MissingCondition()
            };
        }

        private static BenchmarkConditionResult MissingCondition()
        {
            return new BenchmarkConditionResult
            {
                DesignLoad = Missing(MetricUnit.Watt),
                PeakLoad = Missing(MetricUnit.Watt),
                PeakHour = Missing(MetricUnit.HourOfYear),
                UnmetHours = Missing(MetricUnit.Hour)
            };
        }

        /// <summary>A model result whose every metric is unavailable, for the no-comparable-coverage case.</summary>
        internal static BenchmarkModelResult EmptyModel()
        {
            return new BenchmarkModelResult
            {
                ConsumptionHeating = Missing(MetricUnit.KilowattHour),
                ConsumptionCooling = Missing(MetricUnit.KilowattHour),
                PeakHeatingLoad = Missing(MetricUnit.Kilowatt),
                PeakHeatingHour = Missing(MetricUnit.HourOfYear),
                PeakCoolingLoad = Missing(MetricUnit.Kilowatt),
                PeakCoolingHour = Missing(MetricUnit.HourOfYear),
                FloorArea = Missing(MetricUnit.SquareMetre),
                Volume = Missing(MetricUnit.CubicMetre)
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
