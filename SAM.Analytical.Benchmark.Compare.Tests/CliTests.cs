// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using SAM.Analytical.Benchmark;
using SAM.Analytical.Benchmark.Compare;

namespace SAM.Analytical.Benchmark.Compare.Tests
{
    [TestClass]
    public sealed class CliTests
    {
        private const string GuidA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        [TestMethod]
        public void WritesThreeReportsAndReturnsSuccessForAMatchingPair()
        {
            using var workspace = new Workspace();
            string tas = workspace.WriteDocument("benchmark-TAS.json", Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }));
            string openStudio = workspace.WriteDocument("benchmark-OpenStudio.json", Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }));
            string outDir = Path.Combine(workspace.Root, "report");

            int exit = Program.Run(
                new[] { "--tas", tas, "--openstudio", openStudio, "--out", outDir, "--tolerance-profile", "default" },
                new StringWriter(),
                new StringWriter());

            Assert.AreEqual((int)BenchmarkExitCode.Success, exit);
            Assert.IsTrue(File.Exists(Path.Combine(outDir, "comparison.md")));
            Assert.IsTrue(File.Exists(Path.Combine(outDir, "comparison.csv")));
            Assert.IsTrue(File.Exists(Path.Combine(outDir, "comparison-summary.json")));
        }

        [TestMethod]
        public void GateFailureWithoutFlagStillSucceedsAndWritesReports()
        {
            using var workspace = new Workspace();
            string tas = workspace.WriteDocument("benchmark-TAS.json", ScenarioDocuments.Tas());
            string openStudio = workspace.WriteDocument("benchmark-OpenStudio.json", ScenarioDocuments.OpenStudio());
            string outDir = Path.Combine(workspace.Root, "report");

            int exit = Program.Run(new[] { "--tas", tas, "--openstudio", openStudio, "--out", outDir }, new StringWriter(), new StringWriter());

            Assert.AreEqual((int)BenchmarkExitCode.Success, exit);
            Assert.IsTrue(File.Exists(Path.Combine(outDir, "comparison-summary.json")));
        }

        [TestMethod]
        public void FailOnGateReturnsGateExitCodeButStillWritesReports()
        {
            using var workspace = new Workspace();
            string tas = workspace.WriteDocument("benchmark-TAS.json", ScenarioDocuments.Tas());
            string openStudio = workspace.WriteDocument("benchmark-OpenStudio.json", ScenarioDocuments.OpenStudio());
            string outDir = Path.Combine(workspace.Root, "report");

            int exit = Program.Run(new[] { "--tas", tas, "--openstudio", openStudio, "--out", outDir, "--fail-on-gate", "fail" }, new StringWriter(), new StringWriter());

            Assert.AreEqual(Program.GateFailureExitCode, exit);
            Assert.IsTrue(File.Exists(Path.Combine(outDir, "comparison-summary.json")));
        }

        [TestMethod]
        public void MissingInputFileReturnsInputOutputExitCode()
        {
            using var workspace = new Workspace();
            string openStudio = workspace.WriteDocument("benchmark-OpenStudio.json", Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }));
            string outDir = Path.Combine(workspace.Root, "report");

            int exit = Program.Run(
                new[] { "--tas", Path.Combine(workspace.Root, "does-not-exist.json"), "--openstudio", openStudio, "--out", outDir },
                new StringWriter(),
                new StringWriter());

            Assert.AreEqual((int)BenchmarkExitCode.InputOutputOrSerializationFailure, exit);
        }

        [TestMethod]
        public void MisspelledOptionalFlagReturnsUsageExitCodeInsteadOfSilentlyIgnoring()
        {
            using var workspace = new Workspace();
            string tas = workspace.WriteDocument("benchmark-TAS.json", ScenarioDocuments.Tas());
            string openStudio = workspace.WriteDocument("benchmark-OpenStudio.json", ScenarioDocuments.OpenStudio());
            string outDir = Path.Combine(workspace.Root, "report");

            // A typo in an optional flag must be rejected, not silently ignored (which would disable the
            // caller's intended gate enforcement).
            int exit = Program.Run(
                new[] { "--tas", tas, "--openstudio", openStudio, "--out", outDir, "--fail-on-gtae", "fail" },
                new StringWriter(),
                new StringWriter());

            Assert.AreEqual((int)BenchmarkExitCode.InvalidUsage, exit);
        }

        [TestMethod]
        public void MissingRequiredOptionReturnsUsageExitCode()
        {
            int exit = Program.Run(new[] { "--tas", "a.json" }, new StringWriter(), new StringWriter());

            Assert.AreEqual((int)BenchmarkExitCode.InvalidUsage, exit);
        }

        [TestMethod]
        public void UnknownToleranceProfileReturnsUsageExitCode()
        {
            using var workspace = new Workspace();
            string tas = workspace.WriteDocument("benchmark-TAS.json", Builders.Document(EngineKind.Tas, BenchmarkRoute.NativeTas, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }));
            string openStudio = workspace.WriteDocument("benchmark-OpenStudio.json", Builders.Document(EngineKind.OpenStudio, BenchmarkRoute.NativeOpenStudio, Builders.Model(), new[] { Builders.Space(GuidA, "Office", 200, 600) }));
            string outDir = Path.Combine(workspace.Root, "report");

            int exit = Program.Run(
                new[] { "--tas", tas, "--openstudio", openStudio, "--out", outDir, "--tolerance-profile", "strict" },
                new StringWriter(),
                new StringWriter());

            Assert.AreEqual((int)BenchmarkExitCode.InvalidUsage, exit);
        }

        private sealed class Workspace : IDisposable
        {
            public Workspace()
            {
                Root = Path.Combine(Path.GetTempPath(), "benchmark-compare-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
            }

            public string Root { get; }

            public string WriteDocument(string name, BenchmarkDocument document)
            {
                string path = Path.Combine(Root, name);
                BenchmarkSerializer.Write(path, document);
                return path;
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(Root, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
