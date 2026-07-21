using System.Text;
using System.Text.Json;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Tests
{
    [TestClass]
    public sealed class SerializerTests
    {
        [TestMethod]
        public void ValidDocumentRoundTrips()
        {
            string json = BenchmarkSerializer.Serialize(TestDocuments.CreateValid());

            BenchmarkDocument roundTrip = BenchmarkSerializer.Deserialize(json);

            Assert.AreEqual(json, BenchmarkSerializer.Serialize(roundTrip));
        }

        [TestMethod]
        public void RepeatedSerializationIsByteIdentical()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();

            CollectionAssert.AreEqual(
                BenchmarkSerializer.SerializeToUtf8(document),
                BenchmarkSerializer.SerializeToUtf8(document));
        }

        [TestMethod]
        public void SpacesWarningsAndNotesAreCanonicalizedWithoutMutatingInput()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();

            string json = BenchmarkSerializer.Serialize(document);

            Assert.IsTrue(json.IndexOf("Office A", StringComparison.Ordinal) < json.IndexOf("Office B", StringComparison.Ordinal));
            Assert.IsTrue(json.IndexOf("Alpha warning", StringComparison.Ordinal) < json.IndexOf("Zulu warning", StringComparison.Ordinal));
            Assert.IsTrue(json.IndexOf("first note", StringComparison.Ordinal) < json.IndexOf("second note", StringComparison.Ordinal));
            Assert.AreEqual("Office B", document.Spaces![0].Name);
        }

        [TestMethod]
        public void EnumsUseCanonicalStringTokens()
        {
            string json = BenchmarkSerializer.Serialize(TestDocuments.CreateValid());

            StringAssert.Contains(json, "\"route\": \"Native-OpenStudio\"");
            StringAssert.Contains(json, "\"kind\": \"OpenStudio\"");
            StringAssert.Contains(json, "\"unit\": \"kWh\"");
            StringAssert.Contains(json, "\"state\": \"Success\"");
            Assert.IsFalse(json.Contains("\"route\": 1", StringComparison.Ordinal));
        }

        [TestMethod]
        public void ZeroRemainsAvailableAndUnavailableRemainsNull()
        {
            string json = BenchmarkSerializer.Serialize(TestDocuments.CreateValid());
            using JsonDocument parsed = JsonDocument.Parse(json);

            JsonElement cooling = parsed.RootElement.GetProperty("model").GetProperty("consumptionCooling");
            Assert.AreEqual(0d, cooling.GetProperty("value").GetDouble());
            Assert.IsTrue(cooling.GetProperty("available").GetBoolean());

            JsonElement designLoad = parsed.RootElement.GetProperty("spaces")[0].GetProperty("heating").GetProperty("designLoad");
            Assert.AreEqual(JsonValueKind.Null, designLoad.GetProperty("value").ValueKind);
            Assert.IsFalse(designLoad.GetProperty("available").GetBoolean());
        }

        [TestMethod]
        public void InvalidDocumentIsRefusedBeforeSerialization()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Model!.PeakHeatingLoad!.Value = double.NaN;

            Assert.ThrowsException<BenchmarkValidationException>(() => BenchmarkSerializer.Serialize(document));
        }

        [TestMethod]
        public void Utf8SerializationHasNoBomAndUsesLf()
        {
            byte[] bytes = BenchmarkSerializer.SerializeToUtf8(TestDocuments.CreateValid());
            string json = Encoding.UTF8.GetString(bytes);

            Assert.IsFalse(bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf);
            Assert.IsFalse(json.Contains('\r'));
        }

        [TestMethod]
        public void ReadWriteRoundTripsThroughFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"benchmark-{Guid.NewGuid():N}.json");
            try
            {
                BenchmarkDocument document = TestDocuments.CreateValid();
                BenchmarkSerializer.Write(path, document);

                Assert.AreEqual(BenchmarkSerializer.Serialize(document), BenchmarkSerializer.Serialize(BenchmarkSerializer.Read(path)));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [TestMethod]
        public void GoldenInputSerializesToExpectedFixture()
        {
            string input = File.ReadAllText(FixturePath("golden-benchmark-input.json"));
            string expected = File.ReadAllText(FixturePath("golden-benchmark.json")).Replace("\r\n", "\n").TrimEnd('\n');

            string actual = BenchmarkSerializer.Serialize(BenchmarkSerializer.Deserialize(input));

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Sha256HelpersUseCanonicalPrefixAndLowercaseHex()
        {
            Assert.AreEqual(
                "sha256:ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                BenchmarkHash.ComputeSha256("abc"));
        }

        private static string FixturePath(string name)
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        }
    }
}
