// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// A single provenance field that disagrees between the two documents (e.g. a different canonical
    /// model hash or weather), reported so an incompatible comparison cannot masquerade as a clean pass.
    /// </summary>
    public sealed class ProvenanceMismatch
    {
        public ProvenanceMismatch(string field, string? tas, string? openStudio)
        {
            Field = field;
            Tas = tas;
            OpenStudio = openStudio;
        }

        public string Field { get; }

        public string? Tas { get; }

        public string? OpenStudio { get; }
    }

    /// <summary>
    /// Whether the two documents were produced from compatible inputs — the same canonical source model,
    /// weather and design-day source. TOLERANCES.md treats provenance compatibility as a distinct gate
    /// concern that must be checked before a numerical gate is meaningful: a numerical Pass over
    /// mismatched inputs (different model or weather) is not evidence of engine agreement.
    /// </summary>
    public sealed class ProvenanceCompatibility
    {
        public ProvenanceCompatibility(IReadOnlyList<ProvenanceMismatch> mismatches)
        {
            Mismatches = mismatches;
        }

        public IReadOnlyList<ProvenanceMismatch> Mismatches { get; }

        /// <summary>True when the two documents share compatible source-model, weather and design-day inputs.</summary>
        public bool IsCompatible => Mismatches.Count == 0;
    }
}
