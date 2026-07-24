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
        /// of the same quantity over its UNIQUELY-MATCHED spaces, banding the difference with the supplied
        /// profile. Per TOLERANCES.md, only additive quantities are reconciled and only available,
        /// uniquely-matched space values contribute; one-sided/ambiguous spaces are excluded and make the
        /// reconciliation incomplete (a within-document diagnostic, kept separate from the numerical gate).
        /// </summary>
        /// <param name="matchedSpaces">This engine's spaces that were uniquely matched to the other document.</param>
        /// <param name="excludedSpaceCount">How many of this engine's spaces were NOT uniquely matched.</param>
        public static IReadOnlyList<ReconciliationResult> Reconcile(
            string engine,
            BenchmarkModelResult? model,
            IReadOnlyList<BenchmarkSpaceResult> matchedSpaces,
            int excludedSpaceCount,
            ToleranceProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            IReadOnlyList<BenchmarkSpaceResult> spaces = matchedSpaces ?? Array.Empty<BenchmarkSpaceResult>();
            return new List<ReconciliationResult>
            {
                Reconcile(engine, "floorArea", MetricUnit.SquareMetre, model?.FloorArea, spaces, excludedSpaceCount, space => space?.Area, profile),
                Reconcile(engine, "volume", MetricUnit.CubicMetre, model?.Volume, spaces, excludedSpaceCount, space => space?.Volume, profile)
            };
        }

        private static ReconciliationResult Reconcile(
            string engine,
            string key,
            MetricUnit unit,
            MetricValue? modelTotal,
            IReadOnlyList<BenchmarkSpaceResult> matchedSpaces,
            int excludedSpaceCount,
            Func<BenchmarkSpaceResult, MetricValue?> selector,
            ToleranceProfile profile)
        {
            double sum = 0;
            int contributing = 0;
            int missing = 0;
            foreach (BenchmarkSpaceResult space in matchedSpaces)
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

            // A reconciliation is only "complete" when nothing was left out: the model total is present,
            // every one of this engine's spaces was uniquely matched, and every matched space had a value.
            bool complete = modelAvailable && excludedSpaceCount == 0 && missing == 0;

            if (!modelAvailable)
            {
                return new ReconciliationResult(engine, key, unit, null, false, sum, contributing, missing, excludedSpaceCount, false, null, null, ComparisonBand.NotApplicable, NotApplicableReason.Unavailable);
            }

            double total = modelTotal!.Value!.Value;
            double absolute = Math.Abs(total - sum);
            double scale = Math.Max(Math.Max(Math.Abs(total), Math.Abs(sum)), profile.NearZeroFloor(unit));
            double relative = scale > 0 ? absolute / scale : 0d;
            ComparisonBand band = Classify(relative, profile);

            return new ReconciliationResult(engine, key, unit, total, true, sum, contributing, missing, excludedSpaceCount, complete, absolute, relative, band, NotApplicableReason.None);
        }
    }
}
