// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Query
    {
        /// <summary>
        /// Checks whether the two documents are actually comparable: the first argument must be a
        /// successful TAS run and the second a successful OpenStudio run, produced from the same canonical
        /// source model, weather and design-day basis, via a compatible route family. Every disagreement is
        /// reported as a <see cref="ProvenanceMismatch"/>; any mismatch makes the pair incompatible and
        /// prevents an overall pass. This guards against swapped CLI inputs, two documents from the same
        /// engine, different models or weather, and failure documents whose measurements are mostly
        /// unavailable.
        /// </summary>
        public static ProvenanceCompatibility CheckProvenanceCompatibility(BenchmarkProvenance? tas, BenchmarkProvenance? openStudio)
        {
            var mismatches = new List<ProvenanceMismatch>();

            if (tas == null || openStudio == null)
            {
                mismatches.Add(new ProvenanceMismatch("provenance", tas == null ? "missing" : "present", openStudio == null ? "missing" : "present"));
                return new ProvenanceCompatibility(mismatches);
            }

            // Each argument must carry the engine and route family the CLI position promises.
            RequireEngineKind(mismatches, "tasEngineKind", tas.Engine?.Kind, EngineKind.Tas);
            RequireEngineKind(mismatches, "openStudioEngineKind", openStudio.Engine?.Kind, EngineKind.OpenStudio);
            RequireRouteFamily(mismatches, "tasRoute", tas.Route, isTas: true);
            RequireRouteFamily(mismatches, "openStudioRoute", openStudio.Route, isTas: false);

            // Route pairing: native must pair with native, shared-gbXML with shared-gbXML.
            if (RouteFamily(tas.Route) != RouteFamily(openStudio.Route))
            {
                mismatches.Add(new ProvenanceMismatch("routePairing", RouteToken(tas.Route), RouteToken(openStudio.Route)));
            }

            // Both runs must have succeeded; a failure document's measurements are largely unavailable.
            if (tas.State != RunState.Success || openStudio.State != RunState.Success)
            {
                mismatches.Add(new ProvenanceMismatch("runState", RunStateToken(tas.State), RunStateToken(openStudio.State)));
            }

            // Same canonicalization rules, otherwise the canonical hashes are not comparable.
            if (!StringsEqual(tas.CanonicalizationVersion, openStudio.CanonicalizationVersion))
            {
                mismatches.Add(new ProvenanceMismatch("canonicalizationVersion", tas.CanonicalizationVersion, openStudio.CanonicalizationVersion));
            }

            // Same canonical source model, source-model identity, weather and design-day basis.
            RequireEqual(mismatches, "canonicalModelHash", tas.CanonicalModelHash, openStudio.CanonicalModelHash);
            RequireEqual(mismatches, "sourceModelGuid", tas.SourceModelGuid, openStudio.SourceModelGuid);
            RequireEqual(mismatches, "weatherHash", tas.Weather?.Hash, openStudio.Weather?.Hash);
            RequireEqual(mismatches, "weatherIdentity", tas.Weather?.Identity, openStudio.Weather?.Identity);
            if (tas.DesignDaySource != openStudio.DesignDaySource)
            {
                mismatches.Add(new ProvenanceMismatch("designDaySource", DesignDayToken(tas.DesignDaySource), DesignDayToken(openStudio.DesignDaySource)));
            }

            return new ProvenanceCompatibility(mismatches);
        }

        private static void RequireEngineKind(ICollection<ProvenanceMismatch> mismatches, string field, EngineKind? actual, EngineKind expected)
        {
            if (actual != expected)
            {
                mismatches.Add(new ProvenanceMismatch(field, EngineKindToken(actual), EngineKindToken(expected)));
            }
        }

        private static void RequireRouteFamily(ICollection<ProvenanceMismatch> mismatches, string field, BenchmarkRoute route, bool isTas)
        {
            bool ok = isTas
                ? route == BenchmarkRoute.NativeTas || route == BenchmarkRoute.SharedGbXmlTas
                : route == BenchmarkRoute.NativeOpenStudio || route == BenchmarkRoute.SharedGbXmlOpenStudio;
            if (!ok)
            {
                mismatches.Add(new ProvenanceMismatch(field, RouteToken(route), isTas ? "a TAS route" : "an OpenStudio route"));
            }
        }

        private static void RequireEqual(ICollection<ProvenanceMismatch> mismatches, string field, string? tas, string? openStudio)
        {
            if (!StringsEqual(tas, openStudio))
            {
                mismatches.Add(new ProvenanceMismatch(field, tas, openStudio));
            }
        }

        private static bool StringsEqual(string? left, string? right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }

        private static string RouteFamily(BenchmarkRoute route)
        {
            switch (route)
            {
                case BenchmarkRoute.NativeTas:
                case BenchmarkRoute.NativeOpenStudio:
                    return "Native";
                case BenchmarkRoute.SharedGbXmlTas:
                case BenchmarkRoute.SharedGbXmlOpenStudio:
                    return "SharedGbXML";
                default:
                    return "Unknown";
            }
        }

        private static string EngineKindToken(EngineKind? kind)
        {
            if (!kind.HasValue)
            {
                return "unknown";
            }

            switch (kind.Value)
            {
                case EngineKind.OpenStudio: return "OpenStudio";
                case EngineKind.Tas: return "TAS";
                default: return "unknown";
            }
        }

        private static string RouteToken(BenchmarkRoute route)
        {
            switch (route)
            {
                case BenchmarkRoute.NativeOpenStudio: return "Native-OpenStudio";
                case BenchmarkRoute.NativeTas: return "Native-TAS";
                case BenchmarkRoute.SharedGbXmlOpenStudio: return "SharedGbXML-OpenStudio";
                case BenchmarkRoute.SharedGbXmlTas: return "SharedGbXML-TAS";
                default: return "Unknown";
            }
        }

        private static string DesignDayToken(DesignDaySource source)
        {
            switch (source)
            {
                case DesignDaySource.Ddy: return "DDY";
                case DesignDaySource.EmbeddedModel: return "EmbeddedModel";
                case DesignDaySource.None: return "None";
                default: return "Unknown";
            }
        }

        private static string RunStateToken(RunState state)
        {
            switch (state)
            {
                case RunState.Success: return "Success";
                case RunState.Failure: return "Failure";
                default: return "Unknown";
            }
        }
    }
}
