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
            ToleranceProfile toleranceProfile,
            string? tasSchemaVersion,
            string? openStudioSchemaVersion,
            string? schemaDriftNote,
            ProvenanceCompatibility provenanceCompatibility,
            BenchmarkProvenance? tasProvenance,
            BenchmarkProvenance? openStudioProvenance,
            IReadOnlyList<MetricComparison> modelMetrics,
            IReadOnlyList<SpaceComparison> spaces,
            SpaceMatchDiagnostics spaceDiagnostics,
            IReadOnlyList<ReconciliationResult> reconciliations)
        {
            ToleranceProfile = toleranceProfile;
            TasSchemaVersion = tasSchemaVersion;
            OpenStudioSchemaVersion = openStudioSchemaVersion;
            SchemaDriftNote = schemaDriftNote;
            ProvenanceCompatibility = provenanceCompatibility;
            TasProvenance = tasProvenance;
            OpenStudioProvenance = openStudioProvenance;
            ModelMetrics = modelMetrics;
            Spaces = spaces;
            SpaceDiagnostics = spaceDiagnostics;
            Reconciliations = reconciliations;
        }

        public ToleranceProfile ToleranceProfile { get; }

        public string ToleranceProfileName => ToleranceProfile.Name;

        public string? TasSchemaVersion { get; }

        public string? OpenStudioSchemaVersion { get; }

        /// <summary>A human-readable note when the two documents declare different schema minor versions, else null.</summary>
        public string? SchemaDriftNote { get; }

        /// <summary>Whether the two runs are actually comparable (same model, weather, design-day, engines/routes).</summary>
        public ProvenanceCompatibility ProvenanceCompatibility { get; }

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

        /// <summary>Metrics that were actually compared (a real band was assigned), used for coverage.</summary>
        public int ComparableMetricCount => AllMetrics.Count(metric => metric.Band != ComparisonBand.NotApplicable);

        /// <summary>The worst comparable metric band (hour-of-year excluded, informational).</summary>
        public GateStatus NumericalStatus => Query.NumericalStatus(AllMetrics);

        /// <summary>Whether every space aligned and at least one metric was comparable (else Warn).</summary>
        public GateStatus CoverageStatus => Query.CoverageStatus(SpaceDiagnostics, ComparableMetricCount);

        /// <summary>Whether the two runs are compatible enough to compare (else Fail).</summary>
        public GateStatus ProvenanceStatus => Query.ProvenanceStatus(ProvenanceCompatibility);

        /// <summary>Whether within-document model totals reconcile against their matched spaces (else Warn).</summary>
        public GateStatus ReconciliationStatus => Query.ReconciliationStatus(Reconciliations);

        /// <summary>
        /// The overall gate: the worst of the numerical, coverage, provenance and reconciliation statuses, so
        /// missing data, mismatched inputs or incomplete coverage can never present as a clean pass.
        /// </summary>
        public GateStatus Gate => Query.OverallGate(NumericalStatus, CoverageStatus, ProvenanceStatus, ReconciliationStatus);
    }
}
