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

        /// <summary>
        /// Every metric that this comparison could reasonably have compared: the whole-model metrics plus
        /// the metrics of uniquely matched spaces. Metrics of one-sided, duplicated or ambiguous spaces are
        /// excluded because they are N/A by construction — their gap is already reported by the space
        /// alignment diagnostics, so counting them here would double-count the same missing data.
        /// </summary>
        public IEnumerable<MetricComparison> RequiredMetrics => ModelMetrics.Concat(
            Spaces.Where(space => space.MatchKind == SpaceMatchKind.Guid || space.MatchKind == SpaceMatchKind.Name)
                .SelectMany(space => space.Metrics));

        /// <summary>The number of required metrics (whole-model plus uniquely matched spaces).</summary>
        public int RequiredMetricCount => RequiredMetrics.Count();

        /// <summary>Required metrics that were actually compared (a real band was assigned).</summary>
        public int ComparableMetricCount => RequiredMetrics.Count(metric => metric.Band != ComparisonBand.NotApplicable);

        /// <summary>
        /// Required metrics that could NOT be compared, i.e. the coverage gap. In practice the reason is
        /// always an unavailable value: a unit mismatch is a contract error that <see cref="Query.Compare"/>
        /// rejects before banding, so it cannot reach a comparison result.
        /// </summary>
        public int UnavailableRequiredMetricCount => RequiredMetrics.Count(metric => metric.Band == ComparisonBand.NotApplicable);

        /// <summary>The worst comparable metric band (hour-of-year excluded, informational).</summary>
        public GateStatus NumericalStatus => Query.NumericalStatus(AllMetrics);

        /// <summary>
        /// Whether every space aligned AND every required metric was actually compared (else Warn), so an
        /// incomplete run can never present as complete.
        /// </summary>
        public GateStatus CoverageStatus => Query.CoverageStatus(SpaceDiagnostics, ComparableMetricCount, UnavailableRequiredMetricCount);

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
