using System.Globalization;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Tests
{
    [TestClass]
    public sealed class CultureAndPrecisionTests
    {
        [TestMethod]
        public void SerializationIsIndependentOfCommaDecimalCulture()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            CultureInfo original = CultureInfo.CurrentCulture;
            string invariant;
            string german;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                invariant = BenchmarkSerializer.Serialize(document);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                german = BenchmarkSerializer.Serialize(document);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }

            Assert.AreEqual(invariant, german);
            StringAssert.Contains(german, "12.3");
            Assert.IsFalse(german.Contains("12,3", StringComparison.Ordinal));
        }

        [TestMethod]
        public void FiniteDoublePrecisionIsPreservedWithoutRounding()
        {
            const double expected = 0.12345678901234566;
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Model!.ConsumptionHeating!.Value = expected;

            BenchmarkDocument roundTrip = BenchmarkSerializer.Deserialize(BenchmarkSerializer.Serialize(document));

            Assert.AreEqual(expected, roundTrip.Model!.ConsumptionHeating!.Value);
        }

        [TestMethod]
        public void SerializerDoesNotIntroduceAbsolutePaths()
        {
            string json = BenchmarkSerializer.Serialize(TestDocuments.CreateValid());

            Assert.IsFalse(json.Contains(@"C:\", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Utf8SpanDeserializationRoundTrips()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            byte[] utf8 = BenchmarkSerializer.SerializeToUtf8(document);

            BenchmarkDocument roundTrip = BenchmarkSerializer.Deserialize(utf8.AsSpan());

            CollectionAssert.AreEqual(utf8, BenchmarkSerializer.SerializeToUtf8(roundTrip));
        }
    }
}
