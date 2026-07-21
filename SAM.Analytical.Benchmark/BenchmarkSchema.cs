// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Benchmark
{
    public static class BenchmarkSchema
    {
        public const string CurrentVersion = "1.0.0";

        private static readonly Regex SemanticVersionPattern = new Regex(
            @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$",
            RegexOptions.CultureInvariant);

        public static SchemaCompatibility GetCompatibility(string? version)
        {
            if (!TryParseVersion(version, out int major, out int minor, out int patch))
            {
                return SchemaCompatibility.Malformed;
            }

            TryParseVersion(CurrentVersion, out int currentMajor, out int currentMinor, out int currentPatch);
            if (major != currentMajor)
            {
                return SchemaCompatibility.IncompatibleMajor;
            }

            return minor == currentMinor
                ? SchemaCompatibility.Compatible
                : SchemaCompatibility.CompatibleWithMinorWarning;
        }

        public static bool TryParseVersion(string? version, out int major, out int minor, out int patch)
        {
            major = 0;
            minor = 0;
            patch = 0;

            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            Match match = SemanticVersionPattern.Match(version);
            return match.Success
                && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out major)
                && int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out minor)
                && int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out patch);
        }
    }
}
