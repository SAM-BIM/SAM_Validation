// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    /// <summary>
    /// A single fixed, feature-rich pair of documents shared by the byte-stability and golden report
    /// tests: GUID-matched spaces spanning match/warn/fail bands, a peak-hour that wraps the year boundary,
    /// an unavailable model metric, and spaces present on only one side. Both documents use native routes
    /// so they are fully schema-valid and round-trip through the serializer (native routes require GUIDs,
    /// so name-fallback matching is exercised separately in AlignSpacesTests).
    /// </summary>
    internal static class ScenarioDocuments
    {
        private const string GuidOfficeA = "0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a";
        private const string GuidOfficeB = "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b";
        private const string GuidPlant = "0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c";
        private const string GuidAtrium = "0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d";

        internal static BenchmarkDocument Tas()
        {
            BenchmarkModelResult model = Builders.Model(
                consumptionHeating: Builders.Value(1000, MetricUnit.KilowattHour),
                consumptionCooling: Builders.Value(500, MetricUnit.KilowattHour),
                peakHeatingLoad: Builders.Value(10, MetricUnit.Kilowatt),
                peakHeatingHour: Builders.Value(200, MetricUnit.HourOfYear),
                peakCoolingLoad: Builders.Value(8, MetricUnit.Kilowatt),
                peakCoolingHour: Builders.Value(8759, MetricUnit.HourOfYear),
                floorArea: Builders.Value(240, MetricUnit.SquareMetre),
                volume: Builders.Value(720, MetricUnit.CubicMetre));

            return Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, model, new[]
            {
                Builders.Space(GuidOfficeA, "Office A", 200, 600, Builders.Value(2000, MetricUnit.Watt), Builders.Value(205, MetricUnit.HourOfYear)),
                Builders.Space(GuidOfficeB, "Office B", 40, 120, Builders.Value(2100, MetricUnit.Watt), Builders.Value(205, MetricUnit.HourOfYear)),
                Builders.Space(GuidPlant, "Plant, level 1", 12, 36)
            });
        }

        internal static BenchmarkDocument OpenStudio()
        {
            BenchmarkModelResult model = Builders.Model(
                consumptionHeating: Builders.Value(1000, MetricUnit.KilowattHour),
                consumptionCooling: MetricValue.Unavailable(MetricUnit.KilowattHour),
                peakHeatingLoad: Builders.Value(30, MetricUnit.Kilowatt),
                peakHeatingHour: Builders.Value(205, MetricUnit.HourOfYear),
                peakCoolingLoad: Builders.Value(8.8, MetricUnit.Kilowatt),
                peakCoolingHour: Builders.Value(0, MetricUnit.HourOfYear),
                floorArea: Builders.Value(240, MetricUnit.SquareMetre),
                volume: Builders.Value(720, MetricUnit.CubicMetre));

            return Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, model, new[]
            {
                Builders.Space(GuidOfficeA, "Office A", 200, 600, Builders.Value(2000, MetricUnit.Watt), Builders.Value(205, MetricUnit.HourOfYear)),
                Builders.Space(GuidOfficeB, "Office B (OS)", 44, 132, Builders.Value(2600, MetricUnit.Watt), Builders.Value(240, MetricUnit.HourOfYear)),
                Builders.Space(GuidAtrium, "Atrium", 18, 54)
            });
        }
    }
}
