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
        /// The two documents must share a compatible schema major version (a malformed or major-mismatched
        /// version throws <see cref="BenchmarkValidationException"/>, which the CLI maps to the shared
        /// validation exit code). A minor-version difference between the two is recorded as a non-fatal
        /// drift note. Callers that read documents through <see cref="BenchmarkSerializer"/> have already
        /// had each document validated individually; this method re-checks compatibility so it is safe to
        /// call on hand-built documents too.
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

            string? schemaDriftNote = CheckSchemaCompatibility(tas.SchemaVersion, openStudio.SchemaVersion);

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
                tas.Provenance,
                openStudio.Provenance,
                modelMetrics,
                spaces,
                alignment.Diagnostics,
                reconciliations);
        }

        private static string? CheckSchemaCompatibility(string? tasVersion, string? openStudioVersion)
        {
            RequireCompatibleMajor(tasVersion, "TAS");
            RequireCompatibleMajor(openStudioVersion, "OpenStudio");

            if (!string.Equals(tasVersion, openStudioVersion, StringComparison.Ordinal))
            {
                return $"The documents declare different schema versions (TAS {tasVersion}, OpenStudio {openStudioVersion}); comparing shared v1 fields.";
            }

            return null;
        }

        private static void RequireCompatibleMajor(string? version, string engine)
        {
            SchemaCompatibility compatibility = BenchmarkSchema.GetCompatibility(version);
            if (compatibility == SchemaCompatibility.Malformed || compatibility == SchemaCompatibility.IncompatibleMajor)
            {
                throw new SchemaIncompatibleException(
                    $"The {engine} document schema version '{version}' is not compatible with comparator schema {BenchmarkSchema.CurrentVersion}.");
            }
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
