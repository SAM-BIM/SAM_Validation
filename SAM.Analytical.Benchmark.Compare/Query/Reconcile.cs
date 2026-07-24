// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Query
    {
        /// <summary>
        /// Reconciles a single engine's additive whole-model totals (floor area, volume) against the sum
        /// of the same quantity over its spaces, banding the difference with the supplied profile. Only
        /// additive quantities are reconciled; non-additive peaks are excluded by design.
        /// </summary>
        public static IReadOnlyList<ReconciliationResult> Reconcile(string engine, BenchmarkDocument? document, ToleranceProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var results = new List<ReconciliationResult>();
            if (document == null)
            {
                return results;
            }

            IReadOnlyList<BenchmarkSpaceResult> spaces = (IReadOnlyList<BenchmarkSpaceResult>?)document.Spaces ?? Array.Empty<BenchmarkSpaceResult>();

            results.Add(Reconcile(engine, "floorArea", MetricUnit.SquareMetre, document.Model?.FloorArea, spaces, space => space?.Area, profile));
            results.Add(Reconcile(engine, "volume", MetricUnit.CubicMetre, document.Model?.Volume, spaces, space => space?.Volume, profile));
            return results;
        }

        private static ReconciliationResult Reconcile(
            string engine,
            string key,
            MetricUnit unit,
            MetricValue? modelTotal,
            IReadOnlyList<BenchmarkSpaceResult> spaces,
            Func<BenchmarkSpaceResult, MetricValue?> selector,
            ToleranceProfile profile)
        {
            double sum = 0;
            int contributing = 0;
            int missing = 0;
            foreach (BenchmarkSpaceResult space in spaces)
            {
                MetricValue? metric = selector(space);
                if (metric != null && metric.Available && metric.Value.HasValue)
                {
                    sum += metric.Value.Value;
                    contributing++;
                }
                else
                {
                    missing++;
                }
            }

            bool modelAvailable = modelTotal != null && modelTotal.Available && modelTotal.Value.HasValue;
            if (!modelAvailable)
            {
                return new ReconciliationResult(engine, key, unit, null, false, sum, contributing, missing, null, null, ComparisonBand.NotApplicable, NotApplicableReason.Unavailable);
            }

            double total = modelTotal!.Value!.Value;
            double absolute = Math.Abs(total - sum);
            double magnitude = Math.Max(Math.Abs(total), Math.Abs(sum));
            double? relative = magnitude > 0 ? absolute / magnitude : (double?)(absolute == 0 ? 0 : null);

            double floor = profile.NearZeroFloor(unit);
            double warnLimit = floor + (profile.WarnRelative * magnitude);
            double failLimit = floor + (profile.FailRelative * magnitude);
            ComparisonBand band = absolute <= warnLimit ? ComparisonBand.Match
                : absolute <= failLimit ? ComparisonBand.Warn
                : ComparisonBand.Fail;

            return new ReconciliationResult(engine, key, unit, total, true, sum, contributing, missing, absolute, relative, band, NotApplicableReason.None);
        }
    }
}
