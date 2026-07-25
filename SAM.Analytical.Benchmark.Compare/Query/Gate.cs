// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using GateStatusValue = SAM.Analytical.Benchmark.Compare.GateStatus;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Query
    {
        /// <summary>
        /// The numerical status: the worst comparable metric band (Fail over Warn over Pass). N/A metrics do
        /// not affect the ordering, and hour-of-year metrics are excluded — TOLERANCES.md keeps peak-hour
        /// differences informational so they cannot produce a numerical Fail. Their bands are still reported.
        /// </summary>
        public static GateStatusValue NumericalStatus(IEnumerable<MetricComparison> metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            bool anyWarn = false;
            foreach (MetricComparison metric in metrics)
            {
                if (metric.Unit == MetricUnit.HourOfYear)
                {
                    continue;
                }

                if (metric.Band == ComparisonBand.Fail)
                {
                    return GateStatusValue.Fail;
                }

                if (metric.Band == ComparisonBand.Warn)
                {
                    anyWarn = true;
                }
            }

            return anyWarn ? GateStatusValue.Warn : GateStatusValue.Pass;
        }

        /// <summary>
        /// The coverage status: whether the comparison actually compared the runs. Coverage is incomplete
        /// (Warn) when any space is unmatched, duplicated or ambiguous ("split"), when ANY required metric
        /// in a comparable scope was unavailable, or when nothing was comparable at all. TOLERANCES.md
        /// requires unavailable required metrics to be represented in coverage, because N/A metrics do not
        /// affect the numerical status either — without this, a run missing most of its results could
        /// present as a clean overall pass.
        /// </summary>
        /// <param name="diagnostics">The space-alignment diagnostics.</param>
        /// <param name="comparableMetricCount">Required metrics that were actually compared.</param>
        /// <param name="unavailableRequiredMetricCount">
        /// Required metrics that could not be compared, counted over the whole-model metrics and the metrics
        /// of uniquely matched spaces only. One-sided and ambiguous spaces are deliberately excluded: every
        /// one of their metrics is N/A by construction, and that gap is already represented by the unmatched
        /// space diagnostics, so counting them here would double-count the same missing data.
        /// </param>
        public static GateStatusValue CoverageStatus(SpaceMatchDiagnostics diagnostics, int comparableMetricCount, int unavailableRequiredMetricCount)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            bool incompleteSpaces = diagnostics.OnlyInTas.Count > 0
                || diagnostics.OnlyInOpenStudio.Count > 0
                || diagnostics.DuplicateTasGuids.Count > 0
                || diagnostics.DuplicateOpenStudioGuids.Count > 0
                || diagnostics.DuplicateTasNames.Count > 0
                || diagnostics.DuplicateOpenStudioNames.Count > 0
                || diagnostics.AmbiguousNameMatches.Count > 0;

            bool incomplete = incompleteSpaces
                || unavailableRequiredMetricCount > 0
                || comparableMetricCount == 0;

            return incomplete ? GateStatusValue.Warn : GateStatusValue.Pass;
        }

        /// <summary>The provenance status: Pass when the two runs are compatible, otherwise Fail.</summary>
        public static GateStatusValue ProvenanceStatus(ProvenanceCompatibility compatibility)
        {
            if (compatibility == null)
            {
                throw new ArgumentNullException(nameof(compatibility));
            }

            return compatibility.IsCompatible ? GateStatusValue.Pass : GateStatusValue.Fail;
        }

        /// <summary>
        /// The reconciliation status. A within-document total-vs-sum mismatch is a data-quality signal, not a
        /// cross-engine disagreement, so it is capped at Warn: Warn when any reconciliation is incomplete,
        /// not applicable, or lands outside the match band; Pass otherwise.
        /// </summary>
        public static GateStatusValue ReconciliationStatus(IEnumerable<ReconciliationResult> reconciliations)
        {
            if (reconciliations == null)
            {
                throw new ArgumentNullException(nameof(reconciliations));
            }

            foreach (ReconciliationResult reconciliation in reconciliations)
            {
                if (!reconciliation.Complete || reconciliation.Band != ComparisonBand.Match)
                {
                    return GateStatusValue.Warn;
                }
            }

            return GateStatusValue.Pass;
        }

        /// <summary>The overall gate: the worst of the numerical, coverage, provenance and reconciliation statuses.</summary>
        public static GateStatusValue OverallGate(GateStatusValue numerical, GateStatusValue coverage, GateStatusValue provenance, GateStatusValue reconciliation)
        {
            GateStatusValue worst = numerical;
            worst = Worse(worst, coverage);
            worst = Worse(worst, provenance);
            worst = Worse(worst, reconciliation);
            return worst;
        }

        private static GateStatusValue Worse(GateStatusValue left, GateStatusValue right)
        {
            return (int)right > (int)left ? right : left;
        }
    }
}
