// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// The full, engine-independent outcome of comparing a TAS benchmark document against an OpenStudio
    /// benchmark document: per-metric bands, per-space comparisons, alignment diagnostics, within-engine
    /// reconciliation, a schema-drift note and the overall gate status. Everything the report writers
    /// need is here; the writers add no wall-clock or machine-specific data, so reports are reproducible.
    /// </summary>
    public sealed class ComparisonResult
    {
        public ComparisonResult(
            string toleranceProfileName,
            string? tasSchemaVersion,
            string? openStudioSchemaVersion,
            string? schemaDriftNote,
            BenchmarkProvenance? tasProvenance,
            BenchmarkProvenance? openStudioProvenance,
            IReadOnlyList<MetricComparison> modelMetrics,
            IReadOnlyList<SpaceComparison> spaces,
            SpaceMatchDiagnostics spaceDiagnostics,
            IReadOnlyList<ReconciliationResult> reconciliations)
        {
            ToleranceProfileName = toleranceProfileName;
            TasSchemaVersion = tasSchemaVersion;
            OpenStudioSchemaVersion = openStudioSchemaVersion;
            SchemaDriftNote = schemaDriftNote;
            TasProvenance = tasProvenance;
            OpenStudioProvenance = openStudioProvenance;
            ModelMetrics = modelMetrics;
            Spaces = spaces;
            SpaceDiagnostics = spaceDiagnostics;
            Reconciliations = reconciliations;
        }

        public string ToleranceProfileName { get; }

        public string? TasSchemaVersion { get; }

        public string? OpenStudioSchemaVersion { get; }

        /// <summary>A human-readable note when the two documents declare different schema versions, else null.</summary>
        public string? SchemaDriftNote { get; }

        public BenchmarkProvenance? TasProvenance { get; }

        public BenchmarkProvenance? OpenStudioProvenance { get; }

        public IReadOnlyList<MetricComparison> ModelMetrics { get; }

        public IReadOnlyList<SpaceComparison> Spaces { get; }

        public SpaceMatchDiagnostics SpaceDiagnostics { get; }

        public IReadOnlyList<ReconciliationResult> Reconciliations { get; }

        /// <summary>Every metric comparison (model then spaces) in report order.</summary>
        public IEnumerable<MetricComparison> AllMetrics => ModelMetrics.Concat(Spaces.SelectMany(space => space.Metrics));

        public int MatchCount => AllMetrics.Count(metric => metric.Band == ComparisonBand.Match);

        public int WarnCount => AllMetrics.Count(metric => metric.Band == ComparisonBand.Warn);

        public int FailCount => AllMetrics.Count(metric => metric.Band == ComparisonBand.Fail);

        public int NotApplicableCount => AllMetrics.Count(metric => metric.Band == ComparisonBand.NotApplicable);

        /// <summary>The overall gate: the worst applicable band across every model and space metric.</summary>
        public GateStatus Gate => Query.GateStatus(AllMetrics);
    }
}
