// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Benchmark.Tests
{
    /// <summary>
    /// Canonical JSON v1 rules. The source is deliberately ASCII-only: JSON escape sequences appear in
    /// raw string literals and literal code points in ordinary literals, so no assertion depends on how
    /// this file happens to be encoded on disk.
    /// </summary>
    [TestClass]
    public sealed class CanonicalJsonTests
    {
        private static readonly Regex HashPattern = new Regex("^sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant);

        [TestMethod]
        public void ObjectPropertyOrderIsNotSignificant()
        {
            const string first = """{"b":1,"a":2,"c":3}""";
            const string second = """{"c":3,"b":1,"a":2}""";

            Assert.AreEqual("""{"a":2,"b":1,"c":3}""", BenchmarkCanonicalJson.Canonicalize(first));
            CollectionAssert.AreEqual(Canonicalize(first), Canonicalize(second));
            Assert.AreEqual(BenchmarkCanonicalJson.ComputeSha256(first), BenchmarkCanonicalJson.ComputeSha256(second));
        }

        [TestMethod]
        public void InsignificantWhitespaceIsNotSignificant()
        {
            const string compact = """{"a":[1,2],"b":{"c":null}}""";
            const string spaced = "  {\n\t\"a\" : [ 1 , 2 ] ,\r\n  \"b\" : { \"c\" : null }\n}  \n";

            Assert.AreEqual(compact, BenchmarkCanonicalJson.Canonicalize(spaced));
            CollectionAssert.AreEqual(Canonicalize(compact), Canonicalize(spaced));
            Assert.AreEqual(BenchmarkCanonicalJson.ComputeSha256(compact), BenchmarkCanonicalJson.ComputeSha256(spaced));
        }

        [TestMethod]
        public void ArrayOrderRemainsSignificant()
        {
            const string first = """{"a":[1,2,3]}""";
            const string second = """{"a":[3,2,1]}""";

            Assert.AreEqual(first, BenchmarkCanonicalJson.Canonicalize(first));
            Assert.AreEqual(second, BenchmarkCanonicalJson.Canonicalize(second));
            Assert.AreNotEqual(BenchmarkCanonicalJson.ComputeSha256(first), BenchmarkCanonicalJson.ComputeSha256(second));
        }

        [TestMethod]
        public void ArrayElementObjectsAreSortedWithoutReorderingTheArray()
        {
            Assert.AreEqual(
                """[{"a":2,"b":1},{"a":1,"b":2}]""",
                BenchmarkCanonicalJson.Canonicalize("""[{"b":1,"a":2},{"b":2,"a":1}]"""));
        }

        [TestMethod]
        public void NestedObjectPropertiesAreSortedRecursively()
        {
            Assert.AreEqual(
                """{"a":[{"c":2,"d":1}],"z":{"y":{"a":2,"b":1},"z":{"b":3,"c":4}}}""",
                BenchmarkCanonicalJson.Canonicalize("""{"z":{"z":{"c":4,"b":3},"y":{"b":1,"a":2}},"a":[{"d":1,"c":2}]}"""));
        }

        [TestMethod]
        public void PropertyNamesAreSortedByOrdinalUtf16CodeUnit()
        {
            // Ordinal order is I (U+0049), i (U+0069), U+0130, U+0131 - never a linguistic collation.
            Assert.AreEqual(
                """{"I":2,"i":1,"\u0130":3,"\u0131":4}""",
                BenchmarkCanonicalJson.Canonicalize("""{"i":1,"I":2,"\u0130":3,"\u0131":4}"""));

            // A supplementary code point sorts by its leading surrogate, so U+1F600 precedes U+E000.
            Assert.AreEqual(
                """{"\uD83D\uDE00":2,"\uE000":1,"\uFFFF":3}""",
                BenchmarkCanonicalJson.Canonicalize("""{"\uE000":1,"\uD83D\uDE00":2,"\uFFFF":3}"""));
        }

        [TestMethod]
        public void StringEscapingIsDeterministic()
        {
            // Whichever legal spelling the input used, one escaped form comes out.
            Assert.AreEqual("""["Ab"]""", BenchmarkCanonicalJson.Canonicalize("""["\u0041\u0062"]"""));
            Assert.AreEqual("""["a/b\\c"]""", BenchmarkCanonicalJson.Canonicalize("""["a\/b\\c"]"""));
            Assert.AreEqual(
                """["\t\n\r\b\f\u0000\u001F"]""",
                BenchmarkCanonicalJson.Canonicalize("""["\t\n\r\b\f\u0000\u001f"]"""));
            Assert.AreEqual(
                """["\u003C\u0026\u003E\u0027\u002B=\u0060"]""",
                BenchmarkCanonicalJson.Canonicalize("""["<&>'+=`"]"""));
            Assert.AreEqual(
                """["\u007F\u00A0"]""",
                BenchmarkCanonicalJson.Canonicalize("""["\u007f\u00a0"]"""));
        }

        [TestMethod]
        public void UnicodeIsDeterministicAndCanonicalOutputIsAscii()
        {
            // U+00E9, U+2014, U+4E2D U+6587, U+1F600 as literal code points and as JSON escapes.
            const string literal = "[\"caf\u00e9 \u2014 \u4e2d\u6587 \ud83d\ude00\"]";
            const string escaped = """["caf\u00e9 \u2014 \u4e2d\u6587 \ud83d\ude00"]""";

            Assert.AreEqual(
                """["caf\u00E9 \u2014 \u4E2D\u6587 \uD83D\uDE00"]""",
                BenchmarkCanonicalJson.Canonicalize(literal));
            CollectionAssert.AreEqual(Canonicalize(literal), Canonicalize(escaped));
            Assert.AreEqual(BenchmarkCanonicalJson.ComputeSha256(literal), BenchmarkCanonicalJson.ComputeSha256(escaped));
            Assert.IsTrue(Canonicalize(literal).All(x => x < 0x80), "Canonical JSON v1 escapes every non-ASCII character.");
        }

        [TestMethod]
        public void IntegerAndFloatingPointValuesSerializeDeterministically()
        {
            Assert.AreEqual(
                """[1,1,1,1,1,0.1,-12.5,100,9223372036854775807,-9223372036854775808,9223372036854775808]""",
                BenchmarkCanonicalJson.Canonicalize("""[1,1.0,1.00,1e0,1E0,0.1,-12.5,1e2,9223372036854775807,-9223372036854775808,9223372036854775808]"""));

            // Finite binary64 precision survives canonicalization unrounded.
            Assert.AreEqual(
                """[0.12345678901234566,3.141592653589793]""",
                BenchmarkCanonicalJson.Canonicalize("""[0.12345678901234566,3.141592653589793]"""));
        }

        [TestMethod]
        public void GenuineNumericZeroRemainsZero()
        {
            // Zero is a measured value; it must never be dropped, nulled, or split by its spelling.
            Assert.AreEqual(
                """{"a":0,"b":0,"c":0,"d":0,"e":0,"f":0}""",
                BenchmarkCanonicalJson.Canonicalize("""{"a":0,"b":0.0,"c":-0.0,"d":0e0,"e":-0,"f":-0.0e5}"""));
        }

        [TestMethod]
        public void BooleansAndNullsArePreserved()
        {
            Assert.AreEqual(
                """{"f":false,"n":null,"t":true}""",
                BenchmarkCanonicalJson.Canonicalize("""{"t":true,"f":false,"n":null}"""));

            Assert.AreEqual("""{"a":{},"b":[]}""", BenchmarkCanonicalJson.Canonicalize("""{"a":{ },"b":[ ]}"""));
        }

        [TestMethod]
        public void DuplicatePropertyNamesAreRejected()
        {
            AssertRejected("""{"a":1,"a":2}""");
            AssertRejected("""{"a":1,"a":1}""");
            AssertRejected("""{"x":{"a":1,"a":2}}""");

            // Names that differ only by escape spelling are the same name.
            AssertRejected("""{"a":1,"\u0061":2}""");
        }

        [TestMethod]
        public void CommentsAreRejected()
        {
            AssertRejected("""{"a":1 /* block */}""");
            AssertRejected("""{"a":1} // line""");
        }

        [TestMethod]
        public void TrailingCommasAreRejected()
        {
            AssertRejected("""{"a":1,}""");
            AssertRejected("""[1,2,]""");
        }

        [TestMethod]
        public void InvalidJsonIsRejected()
        {
            AssertRejected(string.Empty);
            AssertRejected("{");
            AssertRejected("""{"a"}""");
            AssertRejected("""{'a':1}""");
            AssertRejected("[01]");
            AssertRejected("[NaN]");
            AssertRejected("""{"a":Infinity}""");
            AssertRejected("""{"a":1} {"b":2}""");
            AssertRejected("\ufeff{}");

            // Overflowing binary64 is a non-finite value, not a very large number.
            AssertRejected("[1e400]");
            AssertRejected("[-1e400]");
        }

        [TestMethod]
        public void NestingBeyondTheSupportedDepthIsRejected()
        {
            string supported = new string('[', 255) + "1" + new string(']', 255);

            Assert.AreEqual(supported, BenchmarkCanonicalJson.Canonicalize(supported));
            AssertRejected(new string('[', 300) + "1" + new string(']', 300));
        }

        [TestMethod]
        public void NullInputIsRejected()
        {
            Assert.ThrowsException<ArgumentNullException>(() => BenchmarkCanonicalJson.Canonicalize((string)null!));
            Assert.ThrowsException<ArgumentNullException>(() => BenchmarkCanonicalJson.ComputeSha256((string)null!));
        }

        [TestMethod]
        public void RepeatedCallsAreByteIdentical()
        {
            const string json = """{"b":[1,2,{"z":null,"a":"\u00e9"}],"a":0.1}""";

            byte[] first = Canonicalize(json);
            byte[] second = Canonicalize(json);
            byte[] reapplied = Canonicalize(BenchmarkCanonicalJson.Canonicalize(json));

            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AreEqual(first, reapplied);
            Assert.AreEqual(BenchmarkCanonicalJson.ComputeSha256(json), BenchmarkCanonicalJson.ComputeSha256(json));
        }

        [TestMethod]
        public void CurrentOperatingSystemCultureDoesNotChangeTheResult()
        {
            const string json = """{"i":1,"I":2,"value":12.5,"small":1e-7,"text":"Istanbul"}""";

            CultureInfo culture = CultureInfo.CurrentCulture;
            CultureInfo uiCulture = CultureInfo.CurrentUICulture;
            byte[] invariant;
            byte[] german;
            byte[] turkish;
            try
            {
                // de-DE uses a comma decimal separator; tr-TR collates dotted and dotless i differently.
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
                invariant = Canonicalize(json);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
                german = Canonicalize(json);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                turkish = Canonicalize(json);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }

            CollectionAssert.AreEqual(invariant, german);
            CollectionAssert.AreEqual(invariant, turkish);
            StringAssert.Contains(Encoding.UTF8.GetString(invariant), "12.5");
        }

        [TestMethod]
        public void Utf8AndStringInputProduceIdenticalResults()
        {
            const string json = """{"b":"\u00e9\u4e2d\ud83d\ude00","a":[0,-0.0,1e2]}""";

            byte[] utf8 = Encoding.UTF8.GetBytes(json);

            CollectionAssert.AreEqual(BenchmarkCanonicalJson.Canonicalize(utf8.AsSpan()), Canonicalize(json));
            Assert.AreEqual(
                Encoding.UTF8.GetString(BenchmarkCanonicalJson.Canonicalize(utf8.AsSpan())),
                BenchmarkCanonicalJson.Canonicalize(json));
            Assert.AreEqual(BenchmarkCanonicalJson.ComputeSha256(utf8.AsSpan()), BenchmarkCanonicalJson.ComputeSha256(json));
        }

        [TestMethod]
        public void HashUsesTheBenchmarkSha256FormatOverCanonicalBytes()
        {
            const string json = """{ "b" : 1 , "a" : 2 }""";

            string hash = BenchmarkCanonicalJson.ComputeSha256(json);

            Assert.IsTrue(HashPattern.IsMatch(hash), $"Expected sha256:<64 lowercase hex>, got '{hash}'.");
            Assert.AreEqual(hash, hash.ToLowerInvariant());
            Assert.AreEqual(BenchmarkHash.ComputeSha256(Canonicalize(json)), hash);
            Assert.AreEqual(BenchmarkHash.ComputeSha256("""{"a":2,"b":1}"""), hash);
        }

        [TestMethod]
        public void CanonicalOutputHasNoBomAndNoInsignificantWhitespace()
        {
            byte[] canonical = Canonicalize("""{ "a" : 1 , "b" : "x" }""");

            Assert.IsFalse(canonical.Length >= 3 && canonical[0] == 0xef && canonical[1] == 0xbb && canonical[2] == 0xbf);
            Assert.IsFalse(canonical.Contains((byte)13), "Canonical JSON v1 never emits a carriage return.");
            Assert.IsFalse(canonical.Contains((byte)10), "Canonical JSON v1 emits no trailing newline.");
            Assert.IsFalse(canonical.Contains((byte)32), "Canonical JSON v1 emits no insignificant whitespace.");
        }

        [TestMethod]
        public void EquivalentFixturesCanonicalizeToTheSameRecordedBytesAndHash()
        {
            byte[] expected = File.ReadAllBytes(FixturePath("canonicalization-model.canonical.json"));
            byte[] first = File.ReadAllBytes(FixturePath("canonicalization-model-a.json"));
            byte[] second = File.ReadAllBytes(FixturePath("canonicalization-model-b.json"));

            CollectionAssert.AreEqual(expected, BenchmarkCanonicalJson.Canonicalize(first.AsSpan()));
            CollectionAssert.AreEqual(expected, BenchmarkCanonicalJson.Canonicalize(second.AsSpan()));
            Assert.AreEqual(
                BenchmarkCanonicalJson.ComputeSha256(first.AsSpan()),
                BenchmarkCanonicalJson.ComputeSha256(second.AsSpan()));
            Assert.AreEqual(BenchmarkHash.ComputeSha256(expected), BenchmarkCanonicalJson.ComputeSha256(first.AsSpan()));
        }

        private static byte[] Canonicalize(string json)
        {
            return BenchmarkCanonicalJson.Canonicalize(Encoding.UTF8.GetBytes(json).AsSpan());
        }

        private static void AssertRejected(string json)
        {
            Exception? thrown = null;
            try
            {
                BenchmarkCanonicalJson.Canonicalize(json);
            }
            catch (Exception exception)
            {
                thrown = exception;
            }

            Assert.IsNotNull(thrown, $"Expected canonical JSON v1 to reject '{json}'.");
            Assert.IsInstanceOfType<JsonException>(thrown, $"Expected a JsonException for '{json}', got {thrown!.GetType().Name}.");
        }

        private static string FixturePath(string name)
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        }
    }
}
