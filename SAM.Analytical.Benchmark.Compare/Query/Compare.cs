// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Query
    {
        /// <summary>
        /// Compares a TAS benchmark document against an OpenStudio benchmark document, producing a fully
        /// deterministic <see cref="ComparisonResult"/>. The comparator is engine-independent: it reads
        /// only the neutral schema types and never touches an engine runtime.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The method validates BOTH documents itself (it does not rely on the caller having read them
        /// through <see cref="BenchmarkSerializer"/>): a document that violates the schema — including a
        /// wrong metric unit, a broken availability invariant, an out-of-range hour, or an incompatible
        /// schema major version — is a contract error and throws <see cref="BenchmarkValidationException"/>,
        /// which the CLI maps to the shared validation exit code.
        /// </para>
        /// <para>
        /// Beyond per-document validity, the comparator checks that the two runs are actually comparable —
        /// same canonical source model, weather and design-day basis, expected engine/route on each side,
        /// and both successful (see <see cref="ProvenanceCompatibility"/>). A provenance failure prevents an
        /// overall <see cref="GateStatus.Pass"/>. A minor schema drift (relative to this comparator or
        /// between the two documents) is recorded as a non-fatal note; a patch-only difference is not.
        /// </para>
        /// </remarks>
        public static ComparisonResult Compare(BenchmarkDocument tas, BenchmarkDocument openStudio, ToleranceProfile profile)
        {
            if (tas == null)
            {
                throw new ArgumentNullException(nameof(tas));
            }

            if (openStudio == null)
            {
                throw new ArgumentNullException(nameof(openStudio));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            // Contract errors (unit mismatch, availability-invariant break, out-of-range hours, malformed or
            // major-incompatible schema version) are rejected here rather than silently normalised to N/A.
            BenchmarkValidator.ThrowIfInvalid(tas);
            BenchmarkValidator.ThrowIfInvalid(openStudio);

            string? schemaDriftNote = CheckSchemaDrift(tas.SchemaVersion, openStudio.SchemaVersion);
            ProvenanceCompatibility provenanceCompatibility = CheckProvenanceCompatibility(tas.Provenance, openStudio.Provenance);

            IReadOnlyList<MetricComparison> modelMetrics = CompareModel(tas.Model, openStudio.Model, profile);
            SpaceAlignment alignment = AlignSpaces(
                (IReadOnlyList<BenchmarkSpaceResult>?)tas.Spaces,
                (IReadOnlyList<BenchmarkSpaceResult>?)openStudio.Spaces);
            IReadOnlyList<SpaceComparison> spaces = CompareSpaces(alignment, profile);

            // Reconciliation sums only uniquely-matched spaces (TOLERANCES.md); one-sided/ambiguous spaces
            // are excluded and make the reconciliation incomplete.
            var tasMatched = new List<BenchmarkSpaceResult>();
            var openStudioMatched = new List<BenchmarkSpaceResult>();
            foreach (SpacePair pair in alignment.Pairs)
            {
                tasMatched.Add(pair.Tas);
                openStudioMatched.Add(pair.OpenStudio);
            }

            var reconciliations = new List<ReconciliationResult>();
            reconciliations.AddRange(Reconcile("TAS", tas.Model, tasMatched, alignment.OnlyInTas.Count, profile));
            reconciliations.AddRange(Reconcile("OpenStudio", openStudio.Model, openStudioMatched, alignment.OnlyInOpenStudio.Count, profile));

            return new ComparisonResult(
                profile,
                tas.SchemaVersion,
                openStudio.SchemaVersion,
                schemaDriftNote,
                provenanceCompatibility,
                tas.Provenance,
                openStudio.Provenance,
                modelMetrics,
                spaces,
                alignment.Diagnostics,
                reconciliations);
        }

        /// <summary>
        /// Builds the schema-drift note. Both documents have already been validated (equal, compatible
        /// major). A MINOR drift — either document's minor differs from this comparator's, or the two
        /// documents' minors differ from each other — warns, because unknown additive fields may be
        /// ignored. A patch-only difference does not warn.
        /// </summary>
        private static string? CheckSchemaDrift(string? tasVersion, string? openStudioVersion)
        {
            var notes = new List<string>();
            if (BenchmarkSchema.GetCompatibility(tasVersion) == SchemaCompatibility.CompatibleWithMinorWarning)
            {
                notes.Add($"The TAS document schema {tasVersion} has a newer minor than this comparator ({BenchmarkSchema.CurrentVersion}); unknown additive fields are ignored.");
            }

            if (BenchmarkSchema.GetCompatibility(openStudioVersion) == SchemaCompatibility.CompatibleWithMinorWarning)
            {
                notes.Add($"The OpenStudio document schema {openStudioVersion} has a newer minor than this comparator ({BenchmarkSchema.CurrentVersion}); unknown additive fields are ignored.");
            }

            if (BenchmarkSchema.TryParseVersion(tasVersion, out _, out int tasMinor, out _)
                && BenchmarkSchema.TryParseVersion(openStudioVersion, out _, out int openStudioMinor, out _)
                && tasMinor != openStudioMinor)
            {
                notes.Add($"The documents declare different schema minor versions (TAS {tasVersion}, OpenStudio {openStudioVersion}); comparing shared fields.");
            }

            return notes.Count == 0 ? null : string.Join(" ", notes);
        }

        private static IReadOnlyList<MetricComparison> CompareModel(BenchmarkModelResult? tas, BenchmarkModelResult? openStudio, ToleranceProfile profile)
        {
            return new List<MetricComparison>
            {
                Band("model", "consumptionHeating", tas?.ConsumptionHeating, openStudio?.ConsumptionHeating, profile),
                Band("model", "consumptionCooling", tas?.ConsumptionCooling, openStudio?.ConsumptionCooling, profile),
                Band("model", "peakHeatingLoad", tas?.PeakHeatingLoad, openStudio?.PeakHeatingLoad, profile),
                Band("model", "peakHeatingHour", tas?.PeakHeatingHour, openStudio?.PeakHeatingHour, profile),
                Band("model", "peakCoolingLoad", tas?.PeakCoolingLoad, openStudio?.PeakCoolingLoad, profile),
                Band("model", "peakCoolingHour", tas?.PeakCoolingHour, openStudio?.PeakCoolingHour, profile),
                Band("model", "floorArea", tas?.FloorArea, openStudio?.FloorArea, profile),
                Band("model", "volume", tas?.Volume, openStudio?.Volume, profile)
            };
        }

        private static IReadOnlyList<SpaceComparison> CompareSpaces(SpaceAlignment alignment, ToleranceProfile profile)
        {
            var comparisons = new List<SpaceComparison>();

            foreach (SpacePair pair in alignment.Pairs)
            {
                string scope = pair.Guid ?? pair.Tas.Name ?? pair.OpenStudio.Name ?? "(unnamed)";
                comparisons.Add(new SpaceComparison(
                    pair.Guid,
                    pair.Tas.Name,
                    pair.OpenStudio.Name,
                    pair.MatchKind,
                    CompareSpaceMetrics(scope, pair.Tas, pair.OpenStudio, profile)));
            }

            foreach (BenchmarkSpaceResult space in alignment.OnlyInTas)
            {
                string scope = space.Guid ?? space.Name ?? "(unnamed)";
                comparisons.Add(new SpaceComparison(
                    space.Guid,
                    space.Name,
                    null,
                    SpaceMatchKind.TasOnly,
                    CompareSpaceMetrics(scope, space, null, profile)));
            }

            foreach (BenchmarkSpaceResult space in alignment.OnlyInOpenStudio)
            {
                string scope = space.Guid ?? space.Name ?? "(unnamed)";
                comparisons.Add(new SpaceComparison(
                    space.Guid,
                    null,
                    space.Name,
                    SpaceMatchKind.OpenStudioOnly,
                    CompareSpaceMetrics(scope, null, space, profile)));
            }

            return comparisons;
        }

        private static IReadOnlyList<MetricComparison> CompareSpaceMetrics(string scope, BenchmarkSpaceResult? tas, BenchmarkSpaceResult? openStudio, ToleranceProfile profile)
        {
            var metrics = new List<MetricComparison>
            {
                Band(scope, "area", tas?.Area, openStudio?.Area, profile),
                Band(scope, "volume", tas?.Volume, openStudio?.Volume, profile)
            };

            AddConditionMetrics(metrics, scope, "heating", tas?.Heating, openStudio?.Heating, profile);
            AddConditionMetrics(metrics, scope, "cooling", tas?.Cooling, openStudio?.Cooling, profile);
            return metrics;
        }

        private static void AddConditionMetrics(
            ICollection<MetricComparison> metrics,
            string scope,
            string condition,
            BenchmarkConditionResult? tas,
            BenchmarkConditionResult? openStudio,
            ToleranceProfile profile)
        {
            metrics.Add(Band(scope, condition + ".designLoad", tas?.DesignLoad, openStudio?.DesignLoad, profile));
            metrics.Add(Band(scope, condition + ".peakLoad", tas?.PeakLoad, openStudio?.PeakLoad, profile));
            metrics.Add(Band(scope, condition + ".peakHour", tas?.PeakHour, openStudio?.PeakHour, profile));
            metrics.Add(Band(scope, condition + ".unmetHours", tas?.UnmetHours, openStudio?.UnmetHours, profile));
        }
    }
}
