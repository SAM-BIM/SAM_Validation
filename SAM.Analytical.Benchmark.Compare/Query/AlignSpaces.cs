// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Query
    {
        /// <summary>
        /// Aligns the two documents' spaces GUID-first then Name-fallback, mirroring
        /// <c>AddResults.LookupSpace</c>: a shared 32-hex source GUID is the strongest identity, and only
        /// GUID-less (or GUID-unmatched) spaces fall back to a normalized-name match. Duplicates and
        /// ambiguous "split" names are surfaced through <see cref="SpaceMatchDiagnostics"/> rather than
        /// silently dropped. The result is deterministic: matched spaces come first (GUID matches ordered
        /// by GUID, name matches by normalized name), then TAS-only, then OpenStudio-only.
        /// </summary>
        public static SpaceAlignment AlignSpaces(
            IReadOnlyList<BenchmarkSpaceResult>? tasSpaces,
            IReadOnlyList<BenchmarkSpaceResult>? openStudioSpaces)
        {
            List<BenchmarkSpaceResult> tas = (tasSpaces ?? Array.Empty<BenchmarkSpaceResult>()).Where(space => space != null).ToList();
            List<BenchmarkSpaceResult> openStudio = (openStudioSpaces ?? Array.Empty<BenchmarkSpaceResult>()).Where(space => space != null).ToList();

            var duplicateTasGuids = new List<string>();
            var duplicateOpenStudioGuids = new List<string>();
            var duplicateTasNames = new List<string>();
            var duplicateOpenStudioNames = new List<string>();

            Dictionary<string, BenchmarkSpaceResult> tasByGuid = IndexByGuid(tas, duplicateTasGuids);
            Dictionary<string, BenchmarkSpaceResult> openStudioByGuid = IndexByGuid(openStudio, duplicateOpenStudioGuids);

            var matchedTas = new HashSet<BenchmarkSpaceResult>();
            var matchedOpenStudio = new HashSet<BenchmarkSpaceResult>();
            var pairs = new List<SpacePair>();

            // Pass 1: GUID matches, ordered deterministically by GUID.
            foreach (string guid in tasByGuid.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (openStudioByGuid.TryGetValue(guid, out BenchmarkSpaceResult? openStudioSpace))
                {
                    BenchmarkSpaceResult tasSpace = tasByGuid[guid];
                    pairs.Add(new SpacePair(guid, tasSpace, openStudioSpace, SpaceMatchKind.Guid));
                    matchedTas.Add(tasSpace);
                    matchedOpenStudio.Add(openStudioSpace);
                }
            }

            // Pass 2: name fallback for spaces still unmatched. Only spaces WITHOUT a usable GUID key are
            // eligible, and a name that is ambiguous (maps to several candidates on either side) is skipped
            // and diagnosed as a split rather than matched arbitrarily.
            List<BenchmarkSpaceResult> tasNameCandidates = tas.Where(space => !matchedTas.Contains(space)).ToList();
            List<BenchmarkSpaceResult> openStudioNameCandidates = openStudio.Where(space => !matchedOpenStudio.Contains(space)).ToList();

            Dictionary<string, List<BenchmarkSpaceResult>> tasByName = GroupByName(tasNameCandidates);
            Dictionary<string, List<BenchmarkSpaceResult>> openStudioByName = GroupByName(openStudioNameCandidates);

            foreach (KeyValuePair<string, List<BenchmarkSpaceResult>> entry in tasByName.Where(x => x.Value.Count > 1))
            {
                duplicateTasNames.Add(entry.Key);
            }

            foreach (KeyValuePair<string, List<BenchmarkSpaceResult>> entry in openStudioByName.Where(x => x.Value.Count > 1))
            {
                duplicateOpenStudioNames.Add(entry.Key);
            }

            var ambiguousNameMatches = new List<string>();
            var nameMatches = new List<SpacePair>();
            foreach (string name in tasByName.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!openStudioByName.TryGetValue(name, out List<BenchmarkSpaceResult>? openStudioGroup))
                {
                    continue;
                }

                List<BenchmarkSpaceResult> tasGroup = tasByName[name];
                if (tasGroup.Count != 1 || openStudioGroup.Count != 1)
                {
                    // A one-to-many or many-to-many name relationship (a split space) cannot be aligned safely.
                    ambiguousNameMatches.Add(name);
                    continue;
                }

                BenchmarkSpaceResult tasSpace = tasGroup[0];
                BenchmarkSpaceResult openStudioSpace = openStudioGroup[0];
                string? guid = tasSpace.Guid ?? openStudioSpace.Guid;
                nameMatches.Add(new SpacePair(guid, tasSpace, openStudioSpace, SpaceMatchKind.Name));
                matchedTas.Add(tasSpace);
                matchedOpenStudio.Add(openStudioSpace);
            }

            pairs.AddRange(nameMatches);

            List<BenchmarkSpaceResult> onlyTas = tas.Where(space => !matchedTas.Contains(space)).ToList();
            List<BenchmarkSpaceResult> onlyOpenStudio = openStudio.Where(space => !matchedOpenStudio.Contains(space)).ToList();

            var diagnostics = new SpaceMatchDiagnostics(
                tasCount: tas.Count,
                openStudioCount: openStudio.Count,
                matchedByGuid: pairs.Count(pair => pair.MatchKind == SpaceMatchKind.Guid),
                matchedByName: pairs.Count(pair => pair.MatchKind == SpaceMatchKind.Name),
                onlyInTas: onlyTas.Select(Identity).OrderBy(x => x, StringComparer.Ordinal).ToList(),
                onlyInOpenStudio: onlyOpenStudio.Select(Identity).OrderBy(x => x, StringComparer.Ordinal).ToList(),
                duplicateTasGuids: duplicateTasGuids.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                duplicateOpenStudioGuids: duplicateOpenStudioGuids.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                duplicateTasNames: duplicateTasNames.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                duplicateOpenStudioNames: duplicateOpenStudioNames.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                ambiguousNameMatches: ambiguousNameMatches.OrderBy(x => x, StringComparer.Ordinal).ToList());

            return new SpaceAlignment(pairs, onlyTas, onlyOpenStudio, diagnostics);
        }

        private static Dictionary<string, BenchmarkSpaceResult> IndexByGuid(IEnumerable<BenchmarkSpaceResult> spaces, List<string> duplicates)
        {
            var index = new Dictionary<string, BenchmarkSpaceResult>(StringComparer.Ordinal);
            foreach (BenchmarkSpaceResult space in spaces)
            {
                if (string.IsNullOrEmpty(space.Guid))
                {
                    continue;
                }

                if (!index.ContainsKey(space.Guid!))
                {
                    index[space.Guid!] = space;
                }
                else if (!duplicates.Contains(space.Guid!))
                {
                    duplicates.Add(space.Guid!);
                }
            }

            return index;
        }

        private static Dictionary<string, List<BenchmarkSpaceResult>> GroupByName(IEnumerable<BenchmarkSpaceResult> spaces)
        {
            var groups = new Dictionary<string, List<BenchmarkSpaceResult>>(StringComparer.Ordinal);
            foreach (BenchmarkSpaceResult space in spaces)
            {
                if (string.IsNullOrWhiteSpace(space.Name))
                {
                    continue;
                }

                string key = NormalizeName(space.Name!);
                if (!groups.TryGetValue(key, out List<BenchmarkSpaceResult>? bucket))
                {
                    bucket = new List<BenchmarkSpaceResult>();
                    groups[key] = bucket;
                }

                bucket.Add(space);
            }

            return groups;
        }

        private static string Identity(BenchmarkSpaceResult space)
        {
            if (!string.IsNullOrEmpty(space.Guid))
            {
                return space.Guid!;
            }

            return string.IsNullOrWhiteSpace(space.Name) ? "(unnamed)" : space.Name!;
        }

        /// <summary>
        /// The space-name normalization used for the name fallback, identical to the schema validator's
        /// (<c>Trim().Normalize().ToUpperInvariant()</c>) so the comparator and producers agree.
        /// </summary>
        internal static string NormalizeName(string value)
        {
            return value.Trim().Normalize().ToUpperInvariant();
        }
    }

    /// <summary>A GUID- or name-matched pair of spaces from the two documents.</summary>
    public sealed class SpacePair
    {
        public SpacePair(string? guid, BenchmarkSpaceResult tas, BenchmarkSpaceResult openStudio, SpaceMatchKind matchKind)
        {
            Guid = guid;
            Tas = tas;
            OpenStudio = openStudio;
            MatchKind = matchKind;
        }

        public string? Guid { get; }

        public BenchmarkSpaceResult Tas { get; }

        public BenchmarkSpaceResult OpenStudio { get; }

        public SpaceMatchKind MatchKind { get; }
    }

    /// <summary>The outcome of <see cref="Query.AlignSpaces"/>: matched pairs, one-sided spaces and diagnostics.</summary>
    public sealed class SpaceAlignment
    {
        public SpaceAlignment(
            IReadOnlyList<SpacePair> pairs,
            IReadOnlyList<BenchmarkSpaceResult> onlyInTas,
            IReadOnlyList<BenchmarkSpaceResult> onlyInOpenStudio,
            SpaceMatchDiagnostics diagnostics)
        {
            Pairs = pairs;
            OnlyInTas = onlyInTas;
            OnlyInOpenStudio = onlyInOpenStudio;
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<SpacePair> Pairs { get; }

        public IReadOnlyList<BenchmarkSpaceResult> OnlyInTas { get; }

        public IReadOnlyList<BenchmarkSpaceResult> OnlyInOpenStudio { get; }

        public SpaceMatchDiagnostics Diagnostics { get; }
    }
}
