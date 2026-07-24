// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// Diagnostics describing how the two documents' space lists lined up: counts by match kind plus the
    /// specific identities that failed to align cleanly (present on one side only, duplicated, or an
    /// ambiguous "split" where one name matched several candidates).
    /// </summary>
    public sealed class SpaceMatchDiagnostics
    {
        public SpaceMatchDiagnostics(
            int tasCount,
            int openStudioCount,
            int matchedByGuid,
            int matchedByName,
            IReadOnlyList<string> onlyInTas,
            IReadOnlyList<string> onlyInOpenStudio,
            IReadOnlyList<string> duplicateTasGuids,
            IReadOnlyList<string> duplicateOpenStudioGuids,
            IReadOnlyList<string> duplicateTasNames,
            IReadOnlyList<string> duplicateOpenStudioNames,
            IReadOnlyList<string> ambiguousNameMatches)
        {
            TasCount = tasCount;
            OpenStudioCount = openStudioCount;
            MatchedByGuid = matchedByGuid;
            MatchedByName = matchedByName;
            OnlyInTas = onlyInTas;
            OnlyInOpenStudio = onlyInOpenStudio;
            DuplicateTasGuids = duplicateTasGuids;
            DuplicateOpenStudioGuids = duplicateOpenStudioGuids;
            DuplicateTasNames = duplicateTasNames;
            DuplicateOpenStudioNames = duplicateOpenStudioNames;
            AmbiguousNameMatches = ambiguousNameMatches;
        }

        public int TasCount { get; }

        public int OpenStudioCount { get; }

        public int MatchedByGuid { get; }

        public int MatchedByName { get; }

        /// <summary>Identities (GUID or name) present only in the TAS document.</summary>
        public IReadOnlyList<string> OnlyInTas { get; }

        /// <summary>Identities (GUID or name) present only in the OpenStudio document.</summary>
        public IReadOnlyList<string> OnlyInOpenStudio { get; }

        /// <summary>GUIDs that appeared on more than one TAS space (only the first is aligned).</summary>
        public IReadOnlyList<string> DuplicateTasGuids { get; }

        /// <summary>GUIDs that appeared on more than one OpenStudio space (only the first is aligned).</summary>
        public IReadOnlyList<string> DuplicateOpenStudioGuids { get; }

        /// <summary>Normalized names duplicated among GUID-less TAS spaces (ambiguous for name fallback).</summary>
        public IReadOnlyList<string> DuplicateTasNames { get; }

        /// <summary>Normalized names duplicated among GUID-less OpenStudio spaces (ambiguous for name fallback).</summary>
        public IReadOnlyList<string> DuplicateOpenStudioNames { get; }

        /// <summary>
        /// Names that could not be matched by the name fallback because the same normalized name would
        /// match several candidates on the other side (a "split" space).
        /// </summary>
        public IReadOnlyList<string> AmbiguousNameMatches { get; }
    }
}
