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
        /// <c>AddResults.LookupSpace</c>, but never silently combining conflicting or ambiguous identities:
        /// <list type="bullet">
        /// <item>a GUID present exactly once on BOTH sides is matched by GUID;</item>
        /// <item>a GUID duplicated on either side excludes EVERY space carrying it from automatic matching
        /// (it is not uniquely identifiable);</item>
        /// <item>name fallback runs only for spaces still unmatched AND only when at least one side of the
        /// pair lacks a usable GUID — two spaces with different valid GUIDs are explicit distinct identities
        /// and are never merged by name;</item>
        /// <item>a name that maps to several candidates (a "split"), or to two spaces that both carry
        /// different GUIDs, is reported as ambiguous rather than matched.</item>
        /// </list>
        /// The result is deterministic: GUID matches (ordered by GUID) then name matches (ordered by
        /// normalized name), and the one-sided lists are explicitly sorted.
        /// </summary>
        public static SpaceAlignment AlignSpaces(
            IReadOnlyList<BenchmarkSpaceResult>? tasSpaces,
            IReadOnlyList<BenchmarkSpaceResult>? openStudioSpaces)
        {
            List<BenchmarkSpaceResult> tas = (tasSpaces ?? Array.Empty<BenchmarkSpaceResult>()).Where(space => space != null).ToList();
            List<BenchmarkSpaceResult> openStudio = (openStudioSpaces ?? Array.Empty<BenchmarkSpaceResult>()).Where(space => space != null).ToList();

            Dictionary<string, List<BenchmarkSpaceResult>> tasByGuid = GroupByGuid(tas);
            Dictionary<string, List<BenchmarkSpaceResult>> openStudioByGuid = GroupByGuid(openStudio);

            List<string> duplicateTasGuids = tasByGuid.Where(x => x.Value.Count > 1).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
            List<string> duplicateOpenStudioGuids = openStudioByGuid.Where(x => x.Value.Count > 1).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();

            // A duplicated GUID cannot uniquely identify a space, so every member of that GUID group is
            // excluded from matching entirely (neither GUID nor name fallback may claim it).
            var poisoned = new HashSet<BenchmarkSpaceResult>();
            foreach (List<BenchmarkSpaceResult> group in tasByGuid.Values.Where(g => g.Count > 1).Concat(openStudioByGuid.Values.Where(g => g.Count > 1)))
            {
                foreach (BenchmarkSpaceResult space in group)
                {
                    poisoned.Add(space);
                }
            }

            var matchedTas = new HashSet<BenchmarkSpaceResult>();
            var matchedOpenStudio = new HashSet<BenchmarkSpaceResult>();
            var pairs = new List<SpacePair>();

            // Pass 1: GUIDs present exactly once on both sides, ordered deterministically by GUID.
            foreach (string guid in tasByGuid.Where(x => x.Value.Count == 1).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal))
            {
                if (openStudioByGuid.TryGetValue(guid, out List<BenchmarkSpaceResult>? openStudioGroup) && openStudioGroup.Count == 1)
                {
                    BenchmarkSpaceResult tasSpace = tasByGuid[guid][0];
                    BenchmarkSpaceResult openStudioSpace = openStudioGroup[0];
                    pairs.Add(new SpacePair(guid, tasSpace, openStudioSpace, SpaceMatchKind.Guid));
                    matchedTas.Add(tasSpace);
                    matchedOpenStudio.Add(openStudioSpace);
                }
            }

            // Pass 2: name fallback over spaces still unmatched and not poisoned.
            List<BenchmarkSpaceResult> tasNameCandidates = tas.Where(space => !matchedTas.Contains(space) && !poisoned.Contains(space)).ToList();
            List<BenchmarkSpaceResult> openStudioNameCandidates = openStudio.Where(space => !matchedOpenStudio.Contains(space) && !poisoned.Contains(space)).ToList();

            Dictionary<string, List<BenchmarkSpaceResult>> tasByName = GroupByName(tasNameCandidates);
            Dictionary<string, List<BenchmarkSpaceResult>> openStudioByName = GroupByName(openStudioNameCandidates);

            List<string> duplicateTasNames = tasByName.Where(x => x.Value.Count > 1).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
            List<string> duplicateOpenStudioNames = openStudioByName.Where(x => x.Value.Count > 1).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();

            var ambiguousNameMatches = new List<string>();
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
                if (HasUsableGuid(tasSpace) && HasUsableGuid(openStudioSpace))
                {
                    // Both sides carry different valid GUIDs: these are explicit, conflicting identities and
                    // must never be merged by a shared name.
                    ambiguousNameMatches.Add(name);
                    continue;
                }

                string? guid = HasUsableGuid(tasSpace) ? tasSpace.Guid : openStudioSpace.Guid;
                pairs.Add(new SpacePair(guid, tasSpace, openStudioSpace, SpaceMatchKind.Name));
                matchedTas.Add(tasSpace);
                matchedOpenStudio.Add(openStudioSpace);
            }

            List<BenchmarkSpaceResult> onlyTas = tas.Where(space => !matchedTas.Contains(space)).OrderBy(x => x, SpaceOrder).ToList();
            List<BenchmarkSpaceResult> onlyOpenStudio = openStudio.Where(space => !matchedOpenStudio.Contains(space)).OrderBy(x => x, SpaceOrder).ToList();

            var diagnostics = new SpaceMatchDiagnostics(
                tasCount: tas.Count,
                openStudioCount: openStudio.Count,
                matchedByGuid: pairs.Count(pair => pair.MatchKind == SpaceMatchKind.Guid),
                matchedByName: pairs.Count(pair => pair.MatchKind == SpaceMatchKind.Name),
                onlyInTas: onlyTas.Select(Identity).ToList(),
                onlyInOpenStudio: onlyOpenStudio.Select(Identity).ToList(),
                duplicateTasGuids: duplicateTasGuids,
                duplicateOpenStudioGuids: duplicateOpenStudioGuids,
                duplicateTasNames: duplicateTasNames,
                duplicateOpenStudioNames: duplicateOpenStudioNames,
                ambiguousNameMatches: ambiguousNameMatches.OrderBy(x => x, StringComparer.Ordinal).ToList());

            return new SpaceAlignment(pairs, onlyTas, onlyOpenStudio, diagnostics);
        }

        private static readonly IComparer<BenchmarkSpaceResult> SpaceOrder = Comparer<BenchmarkSpaceResult>.Create((left, right) =>
        {
            int byGuid = string.CompareOrdinal(GuidKey(left), GuidKey(right));
            if (byGuid != 0)
            {
                return byGuid;
            }

            int byNormalized = string.CompareOrdinal(NormalizeName(left.Name ?? string.Empty), NormalizeName(right.Name ?? string.Empty));
            return byNormalized != 0 ? byNormalized : string.CompareOrdinal(left.Name ?? string.Empty, right.Name ?? string.Empty);
        });

        private static string GuidKey(BenchmarkSpaceResult space)
        {
            // GUID-less spaces sort after all GUID'd spaces (U+FFFF is above any hex GUID character).
            return HasUsableGuid(space) ? space.Guid! : "￿";
        }

        private static bool HasUsableGuid(BenchmarkSpaceResult space)
        {
            return !string.IsNullOrEmpty(space.Guid);
        }

        private static Dictionary<string, List<BenchmarkSpaceResult>> GroupByGuid(IEnumerable<BenchmarkSpaceResult> spaces)
        {
            var groups = new Dictionary<string, List<BenchmarkSpaceResult>>(StringComparer.Ordinal);
            foreach (BenchmarkSpaceResult space in spaces)
            {
                if (!HasUsableGuid(space))
                {
                    continue;
                }

                if (!groups.TryGetValue(space.Guid!, out List<BenchmarkSpaceResult>? bucket))
                {
                    bucket = new List<BenchmarkSpaceResult>();
                    groups[space.Guid!] = bucket;
                }

                bucket.Add(space);
            }

            return groups;
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
            if (HasUsableGuid(space))
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
