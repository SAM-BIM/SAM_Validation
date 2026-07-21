using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Tests
{
    [TestClass]
    public sealed class ValidationTests
    {
        [TestMethod]
        public void ValidDocumentAndSha256HashesAreAccepted()
        {
            BenchmarkValidationResult result = BenchmarkValidator.Validate(TestDocuments.CreateValid());

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        [TestMethod]
        public void MissingAndMalformedSchemaVersionsAreRejected()
        {
            BenchmarkDocument missing = TestDocuments.CreateValid();
            missing.SchemaVersion = null;
            BenchmarkDocument malformed = TestDocuments.CreateValid();
            malformed.SchemaVersion = "1.0";

            AssertInvalid(missing, "schema.version.malformed");
            AssertInvalid(malformed, "schema.version.malformed");
        }

        [TestMethod]
        public void MajorSchemaMismatchIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.SchemaVersion = "2.0.0";

            AssertInvalid(document, "schema.version.major");
        }

        [TestMethod]
        public void MinorSchemaMismatchProducesCompatibilityWarning()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.SchemaVersion = "1.1.0";

            BenchmarkValidationResult result = BenchmarkValidator.Validate(document);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Any(x => x.Code == "schema.version.minor"));
        }

        [TestMethod]
        public void PatchSchemaMismatchDoesNotWarn()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.SchemaVersion = "1.0.1";

            BenchmarkValidationResult result = BenchmarkValidator.Validate(document);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        [TestMethod]
        public void MissingProvenanceIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Provenance = null;

            AssertInvalid(document, "provenance.missing");
        }

        [TestMethod]
        public void UnsupportedRouteAndEngineAreRejected()
        {
            BenchmarkDocument route = TestDocuments.CreateValid();
            route.Provenance!.Route = (BenchmarkRoute)999;
            BenchmarkDocument engine = TestDocuments.CreateValid();
            engine.Provenance!.Engine!.Kind = (EngineKind)999;

            AssertInvalid(route, "provenance.route");
            AssertInvalid(engine, "provenance.engine.kind");
        }

        [TestMethod]
        public void RouteMustMatchEngineKind()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Provenance!.Route = BenchmarkRoute.NativeTas;

            AssertInvalid(document, "provenance.routeEngine.mismatch");
        }

        [TestMethod]
        public void MalformedHashIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Provenance!.SourceFileHash = "SHA256:ABC";

            AssertInvalid(document, "provenance.hash.malformed");
        }

        [TestMethod]
        public void SuccessfulRunRequiresAllHashes()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Provenance!.CanonicalModelHash = null;

            AssertInvalid(document, "provenance.hash.required");
        }

        [TestMethod]
        public void NonUtcTimestampIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Provenance!.RunTimestampUtc = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.FromHours(2));

            AssertInvalid(document, "provenance.timestamp.utc");
        }

        [TestMethod]
        public void NonStringTimestampIsRejectedAsInvalidJson()
        {
            string json = BenchmarkSerializer.Serialize(TestDocuments.CreateValid())
                .Replace("\"runTimestampUtc\": \"2026-07-21T12:00:00Z\"", "\"runTimestampUtc\": 123");

            Assert.ThrowsException<System.Text.Json.JsonException>(() => BenchmarkSerializer.Deserialize(json));
        }

        [TestMethod]
        public void NegativeAndNonFiniteDurationsAreRejected()
        {
            BenchmarkDocument negative = TestDocuments.CreateValid();
            negative.Provenance!.DurationSeconds = -0.1;
            BenchmarkDocument nonFinite = TestDocuments.CreateValid();
            nonFinite.Provenance!.DurationSeconds = double.PositiveInfinity;

            AssertInvalid(negative, "provenance.duration.invalid");
            AssertInvalid(nonFinite, "provenance.duration.invalid");
        }

        [TestMethod]
        public void DuplicateSpaceGuidIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Spaces![1].Guid = document.Spaces[0].Guid;

            AssertInvalid(document, "space.guid.duplicate");
        }

        [TestMethod]
        public void EmptySpaceGuidAndIdentityAreRejected()
        {
            BenchmarkDocument emptyGuid = TestDocuments.CreateValid();
            emptyGuid.Spaces![0].Guid = string.Empty;
            BenchmarkDocument emptyIdentity = TestDocuments.CreateValid();
            emptyIdentity.Spaces![0].Guid = null;
            emptyIdentity.Spaces[0].Name = " ";

            AssertInvalid(emptyGuid, "space.guid.empty");
            AssertInvalid(emptyIdentity, "space.identity.missing");
        }

        [TestMethod]
        public void SharedGbXmlRouteAllowsGuidlessSpaceWithUniqueName()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Provenance!.Route = BenchmarkRoute.SharedGbXmlOpenStudio;
            document.Spaces![0].Guid = null;

            Assert.IsTrue(BenchmarkValidator.Validate(document).IsValid);
        }

        [TestMethod]
        public void UnavailableMetricWithZeroIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Model!.ConsumptionCooling!.Available = false;

            AssertInvalid(document, "metric.availability.valueForbidden");
        }

        [TestMethod]
        public void AvailableMetricWithNullIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Model!.ConsumptionCooling!.Value = null;

            AssertInvalid(document, "metric.availability.valueRequired");
        }

        [TestMethod]
        public void NaNAndInfinitiesAreRejected()
        {
            foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                BenchmarkDocument document = TestDocuments.CreateValid();
                document.Model!.PeakHeatingLoad!.Value = value;
                AssertInvalid(document, "metric.value.nonFinite");
            }
        }

        [TestMethod]
        public void HourOfYearBoundaryValuesAreAccepted()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Model!.PeakHeatingHour!.Value = 0;
            document.Model.PeakCoolingHour!.Value = 8759;

            Assert.IsTrue(BenchmarkValidator.Validate(document).IsValid);
        }

        [TestMethod]
        public void InvalidHourOfYearValuesAreRejected()
        {
            foreach (double value in new[] { -1d, 8760d, 1.5d })
            {
                BenchmarkDocument document = TestDocuments.CreateValid();
                document.Model!.PeakHeatingHour!.Value = value;
                AssertInvalid(document, "metric.value.hourOfYear");
            }
        }

        [TestMethod]
        public void IncompatibleAndUnsupportedUnitsAreRejected()
        {
            BenchmarkDocument incompatible = TestDocuments.CreateValid();
            incompatible.Model!.ConsumptionHeating!.Unit = MetricUnit.WattHour;
            BenchmarkDocument unsupported = TestDocuments.CreateValid();
            unsupported.Model!.ConsumptionHeating!.Unit = (MetricUnit)999;

            AssertInvalid(incompatible, "metric.unit.incompatible");
            AssertInvalid(unsupported, "metric.unit.unsupported");
        }

        [TestMethod]
        public void NegativeAreaVolumeLoadConsumptionAndUnmetHoursAreRejected()
        {
            var cases = new Action<BenchmarkDocument>[]
            {
                x => x.Model!.FloorArea!.Value = -1,
                x => x.Model!.Volume!.Value = -1,
                x => x.Model!.PeakHeatingLoad!.Value = -1,
                x => x.Model!.ConsumptionHeating!.Value = -1,
                x => x.Spaces![0].Heating!.UnmetHours!.Value = -1
            };

            foreach (Action<BenchmarkDocument> mutate in cases)
            {
                BenchmarkDocument document = TestDocuments.CreateValid();
                mutate(document);
                AssertInvalid(document, "metric.value.negative");
            }
        }

        [TestMethod]
        public void FailedRunRequiresExplanationButMayOmitUnavailableHashes()
        {
            BenchmarkDocument invalid = TestDocuments.CreateValid();
            invalid.Provenance!.State = RunState.Failure;
            invalid.Provenance.Warnings!.Clear();
            invalid.Provenance.Notes!.Clear();

            BenchmarkDocument valid = TestDocuments.CreateValid();
            valid.Provenance!.State = RunState.Failure;
            valid.Provenance.SourceFileHash = null;
            valid.Provenance.CanonicalModelHash = null;
            valid.Provenance.Weather!.Hash = null;

            AssertInvalid(invalid, "provenance.failure.explanation");
            Assert.IsTrue(BenchmarkValidator.Validate(valid).IsValid);
        }

        [TestMethod]
        public void AbsoluteProvenanceIdentityIsRejected()
        {
            BenchmarkDocument document = TestDocuments.CreateValid();
            document.Provenance!.SourceModelName = @"C:\models\SingleBox.json";

            AssertInvalid(document, "provenance.identity.path");
        }

        [TestMethod]
        public void MalformedCommitAndCanonicalizationVersionAreRejected()
        {
            BenchmarkDocument commit = TestDocuments.CreateValid();
            commit.Provenance!.SamCommit = "not-a-commit";
            BenchmarkDocument version = TestDocuments.CreateValid();
            version.Provenance!.CanonicalizationVersion = "v1";

            AssertInvalid(commit, "provenance.commit.malformed");
            AssertInvalid(version, "provenance.canonicalizationVersion");
        }

        private static void AssertInvalid(BenchmarkDocument document, string expectedCode)
        {
            BenchmarkValidationResult result = BenchmarkValidator.Validate(document);
            Assert.IsFalse(result.IsValid, "Expected validation to fail.");
            Assert.IsTrue(result.Errors.Any(x => x.Code == expectedCode),
                $"Expected validation code '{expectedCode}'. Actual: {string.Join(", ", result.Errors.Select(x => x.Code))}");
        }
    }
}
