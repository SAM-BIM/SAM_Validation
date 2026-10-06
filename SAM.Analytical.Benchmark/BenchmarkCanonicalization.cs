// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Benchmark
{
    /// <summary>
    /// The versioned SAM benchmark canonicalization contract recorded in
    /// <c>provenance.canonicalizationVersion</c>.
    /// </summary>
    /// <remarks>
    /// This is the single authoritative source of the canonicalization version. DTOs, validation,
    /// documentation examples, and tests must reference <see cref="CurrentVersion"/> rather than
    /// repeating the literal. Any change to the canonical JSON rules implemented by
    /// <see cref="BenchmarkCanonicalJson"/> changes the resulting hashes and therefore requires a new
    /// canonicalization version; a changed algorithm must never masquerade as a changed model.
    /// </remarks>
    public static class BenchmarkCanonicalization
    {
        /// <summary>
        /// The canonicalization rules implemented by this library.
        /// </summary>
        public const string CurrentVersion = "1.0.0";

        /// <summary>
        /// Classifies a document's canonicalization version against <see cref="CurrentVersion"/>.
        /// </summary>
        /// <remarks>
        /// Version parsing and the major/minor comparison rules are shared with
        /// <see cref="BenchmarkSchema"/>, so <see cref="SchemaCompatibility"/> is reused as the result
        /// type. An equal major version means the canonical hashes were produced by rules this library
        /// understands; a differing major version means they were not and must not be treated as
        /// comparable evidence.
        /// </remarks>
        public static SchemaCompatibility GetCompatibility(string? version)
        {
            if (!BenchmarkSchema.TryParseVersion(version, out int major, out int minor, out _))
            {
                return SchemaCompatibility.Malformed;
            }

            BenchmarkSchema.TryParseVersion(CurrentVersion, out int currentMajor, out int currentMinor, out _);
            if (major != currentMajor)
            {
                return SchemaCompatibility.IncompatibleMajor;
            }

            return minor == currentMinor
                ? SchemaCompatibility.Compatible
                : SchemaCompatibility.CompatibleWithMinorWarning;
        }
    }
}
