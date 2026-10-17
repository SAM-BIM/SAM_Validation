// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    /// <summary>
    /// SAM#211: the comparator moved from the benchmark-compare executable into a reusable library, with the CLI
    /// kept as a thin wrapper. Fixtures/Regression/&lt;case&gt; holds what the PRE-extraction CLI produced for each
    /// case - the two input documents, comparison.md/.csv/comparison-summary.json, and, per --fail-on-gate mode,
    /// the exit code and console output (cli-&lt;mode&gt;.txt, output directory written as &lt;out&gt;). Both routes
    /// after the extraction must reproduce them byte for byte: the wrapper CLI, and a direct library call
    /// (BenchmarkSerializer.Read + Query.Compare + Modify.WriteReports), which is how SAM_UI consumes it.
    /// The Markdown golden was refreshed for the presentation-only two-decimal report; comparison.csv and
    /// comparison-summary.json remain byte-identical to the pre-extraction output.
    /// </summary>
    [TestClass]
    public class ExtractionRegressionTests
    {
        private static readonly string[] ReportFiles = { Modify.MarkdownFileName, Modify.CsvFileName, Modify.SummaryFileName };

        private static readonly string[] FailOnGateModes = { "never", "warn", "fail" };

        public static IEnumerable<object[]> Cases()
        {
            yield return new object[] { "fixtures" };
            yield return new object[] { "scenario" };
            yield return new object[] { "weather-mismatch" };
        }

        [TestMethod]
        [DynamicData(nameof(Cases), DynamicDataSourceType.Method)]
        public void LibraryReproducesThePreExtractionReportsByteForByte(string name)
        {
            string caseDirectory = CaseDirectory(name);
            string outputDirectory = NewDirectory();
            try
            {
                BenchmarkDocument tas = BenchmarkSerializer.Read(Path.Combine(caseDirectory, "benchmark-TAS.json"));
                BenchmarkDocument openStudio = BenchmarkSerializer.Read(Path.Combine(caseDirectory, "benchmark-OpenStudio.json"));

                ComparisonResult result = Query.Compare(tas, openStudio, ToleranceProfile.Resolve(null));
                Modify.WriteReports(result, outputDirectory);

                AssertReportsMatch(caseDirectory, outputDirectory);
            }
            finally
            {
                Directory.Delete(outputDirectory, true);
            }
        }

        [TestMethod]
        [DynamicData(nameof(Cases), DynamicDataSourceType.Method)]
        public void CliReproducesThePreExtractionExitCodesConsoleOutputAndReports(string name)
        {
            string caseDirectory = CaseDirectory(name);
            foreach (string mode in FailOnGateModes)
            {
                string outputDirectory = NewDirectory();
                try
                {
                    var standardOutput = new StringWriter();
                    int exitCode = Program.Run(
                        new[] { "--tas", Path.Combine(caseDirectory, "benchmark-TAS.json"), "--openstudio", Path.Combine(caseDirectory, "benchmark-OpenStudio.json"), "--out", outputDirectory, "--fail-on-gate", mode },
                        standardOutput,
                        new StringWriter());

                    // The console output names the output directory; the golden holds it as <out>, with '/' separators.
                    string actual = "exit=" + exitCode + "\n" + standardOutput.ToString().Replace(outputDirectory, "<out>").Replace("\r\n", "\n").Replace('\\', '/');
                    string expected = File.ReadAllText(Path.Combine(caseDirectory, "cli-" + mode + ".txt"));
                    Assert.AreEqual(expected, actual, name + " --fail-on-gate " + mode);

                    AssertReportsMatch(caseDirectory, outputDirectory);
                }
                finally
                {
                    Directory.Delete(outputDirectory, true);
                }
            }
        }

        private static void AssertReportsMatch(string expectedDirectory, string actualDirectory)
        {
            foreach (string file in ReportFiles)
            {
                CollectionAssert.AreEqual(
                    File.ReadAllBytes(Path.Combine(expectedDirectory, file)),
                    File.ReadAllBytes(Path.Combine(actualDirectory, file)),
                    expectedDirectory + ": " + file + " differs from the pre-extraction output");
            }
        }

        private static string NewDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "sam-compare-regression-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static string CaseDirectory(string name, [CallerFilePath] string? sourceFilePath = null)
        {
            return Path.Combine(Path.GetDirectoryName(sourceFilePath!)!, "Fixtures", "Regression", name);
        }
    }
}
