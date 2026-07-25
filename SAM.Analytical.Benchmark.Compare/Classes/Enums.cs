// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// The tolerance band a single metric comparison lands in. Bands are provisional reporting
    /// buckets driven by a <see cref="ToleranceProfile"/>, never validated scientific thresholds.
    /// </summary>
    public enum ComparisonBand
    {
        /// <summary>The two values agree within the profile's warn band (the tightest, "green" band).</summary>
        Match = 0,

        /// <summary>The two values disagree beyond the warn band but within the fail band ("amber").</summary>
        Warn,

        /// <summary>The two values disagree beyond the fail band ("red").</summary>
        Fail,

        /// <summary>
        /// The metric cannot be compared (a value is unavailable on one/both sides, or the units differ).
        /// N/A never counts as a failure.
        /// </summary>
        NotApplicable
    }

    /// <summary>Why a comparison is <see cref="ComparisonBand.NotApplicable"/>.</summary>
    public enum NotApplicableReason
    {
        /// <summary>The comparison is applicable (a real band was assigned).</summary>
        None = 0,

        /// <summary>A value is unavailable on one or both sides.</summary>
        Unavailable,

        /// <summary>Both values are available but their declared units differ, so they are not comparable.</summary>
        UnitMismatch
    }

    /// <summary>Which sides carry an available value for a metric.</summary>
    public enum ComparisonAvailability
    {
        /// <summary>Neither side has an available value.</summary>
        Neither = 0,

        /// <summary>Only the TAS side has an available value.</summary>
        TasOnly,

        /// <summary>Only the OpenStudio side has an available value.</summary>
        OpenStudioOnly,

        /// <summary>Both sides have an available value.</summary>
        Both
    }

    /// <summary>How a space was aligned across the two documents.</summary>
    public enum SpaceMatchKind
    {
        /// <summary>Matched on a shared 32-hex source GUID (the strongest identity).</summary>
        Guid = 0,

        /// <summary>Matched on a normalized name after GUID matching left it unmatched.</summary>
        Name,

        /// <summary>Present only in the TAS document.</summary>
        TasOnly,

        /// <summary>Present only in the OpenStudio document.</summary>
        OpenStudioOnly
    }

    /// <summary>The overall gate outcome for a comparison, derived from the worst applicable band.</summary>
    public enum GateStatus
    {
        /// <summary>Every applicable metric landed in the match band.</summary>
        Pass = 0,

        /// <summary>At least one applicable metric warned, but none failed.</summary>
        Warn,

        /// <summary>At least one applicable metric failed.</summary>
        Fail
    }
}
