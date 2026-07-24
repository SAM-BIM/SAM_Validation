// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// <c>benchmark-compare</c>: the B3 headless comparator. Loads two engine-neutral benchmark
    /// documents (one per engine), checks their schema compatibility, aligns and diffs them against a
    /// configurable <see cref="ToleranceProfile"/>, and writes <c>comparison.md</c>,
    /// <c>comparison.csv</c> and <c>comparison-summary.json</c> into an output directory.
    /// <para>
    /// The comparator is independent of both engines — it references only the neutral schema library.
    /// Argument parsing, invariant culture and exception mapping are delegated to the shared benchmark
    /// CLI host so the exit-code contract matches every other benchmark tool: <c>0</c> success, <c>2</c>
    /// usage, <c>3</c> input/IO/serialization, <c>4</c> validation/schema-incompatible, <c>5</c> internal
    /// failure. A tolerance gate is NOT a failure by default: the reports are always written and the gate
    /// outcome is recorded in the summary. Only <c>--fail-on-gate</c> makes a warn/fail gate return the
    /// dedicated exit code <c>1</c>.
    /// </para>
    /// </summary>
    public static class Program
    {
        /// <summary>The dedicated exit code used when a gate outcome trips <c>--fail-on-gate</c>.</summary>
        public const int GateFailureExitCode = 1;

        private static readonly string[] RequiredOptions = { "tas", "openstudio", "out" };

        public static int Main(string[] args)
        {
            return Run(args, Console.Out, Console.Error);
        }

        /// <summary>
        /// Testable entry point: the shared host drives parsing, culture, help and exception mapping;
        /// <see cref="Execute"/> carries the comparator logic. Writers are injectable so CLI tests can
        /// capture the output without touching the process <see cref="Console"/>.
        /// </summary>
        internal static int Run(string[] args, TextWriter standardOutput, TextWriter standardError)
        {
            return BenchmarkCliHost.Run(
                args,
                Usage,
                RequiredOptions,
                (arguments, _) => Execute(arguments, standardOutput),
                standardOutput,
                standardError);
        }

        private static int Execute(BenchmarkArguments arguments, TextWriter standardOutput)
        {
            string tasPath = BenchmarkCliPaths.ValidateInputFile(arguments.RequireOption("tas"));
            string openStudioPath = BenchmarkCliPaths.ValidateInputFile(arguments.RequireOption("openstudio"));
            string outputDirectory = arguments.RequireOption("out");

            // Unknown profile names surface as an ArgumentException, which the host maps to a usage error (2).
            ToleranceProfile profile = ToleranceProfile.Resolve(arguments.GetOption("tolerance-profile"));
            FailOnGate failOnGate = ParseFailOnGate(arguments.GetOption("fail-on-gate"));

            // BenchmarkSerializer.Read validates each document individually: a missing/unparseable file is a
            // JsonException (exit 3), an individually-invalid document a BenchmarkValidationException (exit 4).
            BenchmarkDocument tas = BenchmarkSerializer.Read(tasPath);
            BenchmarkDocument openStudio = BenchmarkSerializer.Read(openStudioPath);

            ComparisonResult result;
            try
            {
                result = Query.Compare(tas, openStudio, profile);
            }
            catch (SchemaIncompatibleException exception)
            {
                // Mapped to the shared validation exit code, consistent with an individually-read incompatible
                // document, rather than the generic internal-failure code the host would otherwise assign.
                standardOutput.WriteLine("error: " + exception.Message);
                return (int)BenchmarkExitCode.ValidationFailure;
            }

            IReadOnlyList<string> written = Modify.WriteReports(result, outputDirectory);
            foreach (string path in written)
            {
                standardOutput.WriteLine("Wrote " + path);
            }

            standardOutput.WriteLine(
                "Gate=" + Format.Gate(result.Gate)
                + " match=" + result.MatchCount
                + " warn=" + result.WarnCount
                + " fail=" + result.FailCount
                + " na=" + result.NotApplicableCount);

            if (GateTrips(result.Gate, failOnGate))
            {
                standardOutput.WriteLine("Gate failed under --fail-on-gate=" + failOnGate.ToString().ToLowerInvariant() + ".");
                return GateFailureExitCode;
            }

            return (int)BenchmarkExitCode.Success;
        }

        private static bool GateTrips(GateStatus gate, FailOnGate failOnGate)
        {
            switch (failOnGate)
            {
                case FailOnGate.Fail:
                    return gate == GateStatus.Fail;
                case FailOnGate.Warn:
                    return gate == GateStatus.Fail || gate == GateStatus.Warn;
                default:
                    return false;
            }
        }

        private static FailOnGate ParseFailOnGate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "never", StringComparison.OrdinalIgnoreCase))
            {
                return FailOnGate.Never;
            }

            if (string.Equals(value, "warn", StringComparison.OrdinalIgnoreCase))
            {
                return FailOnGate.Warn;
            }

            if (string.Equals(value, "fail", StringComparison.OrdinalIgnoreCase))
            {
                return FailOnGate.Fail;
            }

            throw new ArgumentException("Option '--fail-on-gate' must be one of 'never', 'warn' or 'fail'.");
        }

        private enum FailOnGate
        {
            Never = 0,
            Warn,
            Fail
        }

        private const string Usage =
            "Usage: benchmark-compare --tas <benchmark-TAS.json> --openstudio <benchmark-OpenStudio.json> --out <report-directory>\n" +
            "                         [--tolerance-profile <name>] [--fail-on-gate <never|warn|fail>]\n" +
            "\n" +
            "Writes comparison.md, comparison.csv and comparison-summary.json into the output directory.\n" +
            "\n" +
            "Exit codes: 0 success, 1 gate failed (only with --fail-on-gate), 2 usage error,\n" +
            "            3 input/IO/serialization error, 4 validation/schema-incompatible, 5 internal failure.";
    }
}
