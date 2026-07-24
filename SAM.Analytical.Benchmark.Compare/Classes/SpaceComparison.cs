// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// The comparison of a single aligned space (or a space present on only one side) together with the
    /// per-metric comparisons that belong to it.
    /// </summary>
    public sealed class SpaceComparison
    {
        public SpaceComparison(
            string? guid,
            string? tasName,
            string? openStudioName,
            SpaceMatchKind matchKind,
            IReadOnlyList<MetricComparison> metrics)
        {
            Guid = guid;
            TasName = tasName;
            OpenStudioName = openStudioName;
            MatchKind = matchKind;
            Metrics = metrics;
        }

        /// <summary>The shared GUID when the space was matched by GUID, otherwise the available side's GUID.</summary>
        public string? Guid { get; }

        /// <summary>The name carried by the TAS document (null if the space is OpenStudio-only).</summary>
        public string? TasName { get; }

        /// <summary>The name carried by the OpenStudio document (null if the space is TAS-only).</summary>
        public string? OpenStudioName { get; }

        public SpaceMatchKind MatchKind { get; }

        public IReadOnlyList<MetricComparison> Metrics { get; }

        /// <summary>A stable scope label for reports: the GUID when present, else the best available name.</summary>
        public string Scope => Guid ?? TasName ?? OpenStudioName ?? "(unnamed)";
    }
}
