// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Query
    {
        /// <summary>
        /// The whole-model metrics of one engine's result, in the order and under the keys the comparator uses.
        /// A metric the engine did not report is returned with a <c>null</c> value; it is never substituted.
        /// Lets a consumer show one engine's results (for example when the other engine did not complete) without
        /// duplicating the metric key list.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, MetricValue?>> ModelMetricValues(BenchmarkModelResult? model)
        {
            return new List<KeyValuePair<string, MetricValue?>>
            {
                new KeyValuePair<string, MetricValue?>("consumptionHeating", model?.ConsumptionHeating),
                new KeyValuePair<string, MetricValue?>("consumptionCooling", model?.ConsumptionCooling),
                new KeyValuePair<string, MetricValue?>("peakHeatingLoad", model?.PeakHeatingLoad),
                new KeyValuePair<string, MetricValue?>("peakHeatingHour", model?.PeakHeatingHour),
                new KeyValuePair<string, MetricValue?>("peakCoolingLoad", model?.PeakCoolingLoad),
                new KeyValuePair<string, MetricValue?>("peakCoolingHour", model?.PeakCoolingHour),
                new KeyValuePair<string, MetricValue?>("floorArea", model?.FloorArea),
                new KeyValuePair<string, MetricValue?>("volume", model?.Volume),
            };
        }

        /// <summary>
        /// The per-space metrics of one engine's space result, in the order and under the keys the comparator uses
        /// (<c>area</c>, <c>volume</c>, then <c>heating.*</c> and <c>cooling.*</c>). A metric the engine did not
        /// report is returned with a <c>null</c> value; it is never substituted.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, MetricValue?>> SpaceMetricValues(BenchmarkSpaceResult? space)
        {
            var values = new List<KeyValuePair<string, MetricValue?>>
            {
                new KeyValuePair<string, MetricValue?>("area", space?.Area),
                new KeyValuePair<string, MetricValue?>("volume", space?.Volume),
            };

            AddConditionMetricValues(values, "heating", space?.Heating);
            AddConditionMetricValues(values, "cooling", space?.Cooling);
            return values;
        }

        private static void AddConditionMetricValues(ICollection<KeyValuePair<string, MetricValue?>> values, string condition, BenchmarkConditionResult? result)
        {
            values.Add(new KeyValuePair<string, MetricValue?>(condition + ".designLoad", result?.DesignLoad));
            values.Add(new KeyValuePair<string, MetricValue?>(condition + ".peakLoad", result?.PeakLoad));
            values.Add(new KeyValuePair<string, MetricValue?>(condition + ".peakHour", result?.PeakHour));
            values.Add(new KeyValuePair<string, MetricValue?>(condition + ".unmetHours", result?.UnmetHours));
        }
    }
}
