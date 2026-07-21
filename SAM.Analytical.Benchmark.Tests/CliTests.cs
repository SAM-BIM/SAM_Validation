using System.Globalization;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Tests
{
    [TestClass]
    public sealed class CliTests
    {
        [TestMethod]
        public void ParserSupportsSeparatedAndEqualsOptions()
        {
            BenchmarkArgumentParseResult result = BenchmarkArgumentParser.Parse(
                new[] { "--model", "model.json", "--out=result.json", "--timeout-seconds", "2.5" },
                new[] { "model", "out" });

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("model.json", result.Arguments!.RequireOption("--model"));
            Assert.AreEqual("result.json", result.Arguments.GetOption("out"));
            Assert.AreEqual(TimeSpan.FromSeconds(2.5), result.Arguments.GetTimeout());
        }

        [TestMethod]
        public void ParserReportsMissingRequiredOption()
        {
            BenchmarkArgumentParseResult result = BenchmarkArgumentParser.Parse(
                new[] { "--model", "model.json" },
                new[] { "model", "out" });

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Missing required option '--out'.", result.Error);
        }

        [TestMethod]
        public void ParserRejectsDuplicateAndMissingValues()
        {
            Assert.IsFalse(BenchmarkArgumentParser.Parse(new[] { "--out", "a", "--out", "b" }).IsSuccess);
            Assert.IsFalse(BenchmarkArgumentParser.Parse(new[] { "--out" }).IsSuccess);
        }

        [TestMethod]
        public void TimeoutOutsideTimeSpanRangeIsAUsageError()
        {
            BenchmarkArgumentParseResult parsed = BenchmarkArgumentParser.Parse(new[] { "--timeout-seconds", "100000000000000000000000000000" });

            Assert.ThrowsException<ArgumentException>(() => parsed.Arguments!.GetTimeout());
        }

        [TestMethod]
        public void HostPrintsHelpWithoutInvokingBody()
        {
            var output = new StringWriter();
            bool invoked = false;

            int exitCode = BenchmarkCliHost.Run(
                new[] { "--help" },
                "usage: benchmark --model <path>",
                new[] { "model" },
                (_, _) => { invoked = true; return 0; },
                output,
                new StringWriter());

            Assert.AreEqual((int)BenchmarkExitCode.Success, exitCode);
            Assert.IsFalse(invoked);
            StringAssert.Contains(output.ToString(), "usage: benchmark");
        }

        [TestMethod]
        public void HostReturnsUsageExitCodeAndConsistentError()
        {
            var error = new StringWriter();

            int exitCode = BenchmarkCliHost.Run(
                Array.Empty<string>(),
                "usage: benchmark --model <path>",
                new[] { "model" },
                (_, _) => 0,
                new StringWriter(),
                error);

            Assert.AreEqual((int)BenchmarkExitCode.InvalidUsage, exitCode);
            StringAssert.StartsWith(error.ToString(), "error: Missing required option '--model'.");
            StringAssert.Contains(error.ToString(), "usage: benchmark");
        }

        [TestMethod]
        public void HostUsesInvariantCultureAndRestoresCallerCulture()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            var commaCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = commaCulture;
            try
            {
                int exitCode = BenchmarkCliHost.Run(
                    Array.Empty<string>(),
                    "usage",
                    Array.Empty<string>(),
                    (_, _) =>
                    {
                        Assert.AreEqual(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
                        Assert.AreEqual("1.5", 1.5.ToString(CultureInfo.CurrentCulture));
                        return 0;
                    },
                    new StringWriter(),
                    new StringWriter());

                Assert.AreEqual(0, exitCode);
                Assert.AreEqual(commaCulture, CultureInfo.CurrentCulture);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [TestMethod]
        public void ExitCodeMappingIsStable()
        {
            BenchmarkValidationResult invalid = BenchmarkValidator.Validate(new BenchmarkDocument());

            Assert.AreEqual(BenchmarkExitCode.ValidationFailure, BenchmarkCliHost.MapException(new BenchmarkValidationException(invalid)));
            Assert.AreEqual(BenchmarkExitCode.InputOutputOrSerializationFailure, BenchmarkCliHost.MapException(new IOException("io")));
            Assert.AreEqual(BenchmarkExitCode.InvalidUsage, BenchmarkCliHost.MapException(new ArgumentException("usage")));
            Assert.AreEqual(BenchmarkExitCode.ProducerFailure, BenchmarkCliHost.MapException(new InvalidOperationException("producer")));
        }

        [TestMethod]
        public void PathHelpersValidateExistingInputAndOutputDirectory()
        {
            string input = Path.GetTempFileName();
            string output = Path.Combine(Path.GetTempPath(), $"benchmark-{Guid.NewGuid():N}.json");
            try
            {
                Assert.AreEqual(Path.GetFullPath(input), BenchmarkCliPaths.ValidateInputFile(input));
                Assert.AreEqual(Path.GetFullPath(output), BenchmarkCliPaths.ValidateOutputFile(output));
                Assert.ThrowsException<FileNotFoundException>(() => BenchmarkCliPaths.ValidateInputFile(output));
            }
            finally
            {
                File.Delete(input);
            }
        }
    }
}
