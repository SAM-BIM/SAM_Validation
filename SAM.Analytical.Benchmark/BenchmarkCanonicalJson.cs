// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SAM.Analytical.Benchmark
{
    /// <summary>
    /// Canonical JSON v1: the engine-neutral representation hashed into
    /// <c>provenance.canonicalModelHash</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The helper canonicalizes JSON supplied by a producer. It has no knowledge of SAM, OpenStudio,
    /// Tas, Rhino, or Grasshopper: obtaining the neutral SAM model JSON is each producer's
    /// responsibility, and the canonical hash must be taken from the loaded <c>AnalyticalModel</c>
    /// before any engine-specific translation or mutation.
    /// </para>
    /// <para>
    /// The v1 rules are:
    /// input must be a single valid JSON value encoded as UTF-8 without a byte order mark;
    /// object properties are written in ascending ordinal (UTF-16 code unit) property-name order;
    /// array order is preserved exactly;
    /// property names and string values are escaped by <see cref="Utf8JsonWriter"/> using
    /// <see cref="JavaScriptEncoder.Default"/>;
    /// booleans and nulls keep their normal JSON representation;
    /// numbers that fit <see cref="long"/> or <see cref="ulong"/> are emitted exactly and all other
    /// numbers are emitted through the shortest round-trippable binary64 form, with negative zero
    /// normalised to zero;
    /// no indentation or insignificant whitespace is emitted, and no trailing newline;
    /// duplicate object-property names, comments, trailing commas, non-finite numbers, text that is not
    /// well-formed UTF-8, and nesting deeper than 256 levels are rejected;
    /// no property is removed, ignored, or otherwise normalised in version 1.
    /// </para>
    /// <para>
    /// This is a versioned SAM benchmark canonicalization contract, not a claim of full semantic JSON
    /// equivalence. Numeric precision beyond binary64 is not preserved, and two documents that a JSON
    /// reader would treat as equal may still canonicalize differently. Changing any rule above changes
    /// every hash and requires a new <see cref="BenchmarkCanonicalization.CurrentVersion"/>.
    /// </para>
    /// </remarks>
    public static class BenchmarkCanonicalJson
    {
        private const int MaxDepth = 256;

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false, true);

        private static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = MaxDepth
        };

        private static readonly JsonWriterOptions WriterOptions = new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.Default,
            Indented = false,
            SkipValidation = false
        };

        /// <summary>
        /// Canonicalizes a UTF-8 JSON value into canonical JSON v1 bytes.
        /// </summary>
        /// <param name="utf8Json">A single valid JSON value encoded as UTF-8 without a byte order mark.</param>
        /// <returns>Canonical UTF-8 bytes with no byte order mark and no trailing newline.</returns>
        /// <exception cref="JsonException">The input violates the canonical JSON v1 rules.</exception>
        public static byte[] Canonicalize(ReadOnlySpan<byte> utf8Json)
        {
            // JsonDocument.Parse references the buffer it is given, so canonicalization owns a copy.
            using (JsonDocument document = JsonDocument.Parse(utf8Json.ToArray(), DocumentOptions))
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, WriterOptions))
                {
                    CanonicalJsonWriter.Write(document.RootElement, writer);
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Canonicalizes a JSON value into canonical JSON v1 text.
        /// </summary>
        /// <param name="json">A single valid JSON value.</param>
        /// <returns>Canonical JSON text; the canonical form is always ASCII.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">The input violates the canonical JSON v1 rules.</exception>
        public static string Canonicalize(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return Utf8WithoutBom.GetString(Canonicalize(Utf8WithoutBom.GetBytes(json).AsSpan()));
        }

        /// <summary>
        /// Computes the SHA-256 of the canonical JSON v1 bytes of a UTF-8 JSON value.
        /// </summary>
        /// <param name="utf8Json">A single valid JSON value encoded as UTF-8 without a byte order mark.</param>
        /// <returns><c>sha256:</c> followed by 64 lowercase hexadecimal characters.</returns>
        /// <exception cref="JsonException">The input violates the canonical JSON v1 rules.</exception>
        public static string ComputeSha256(ReadOnlySpan<byte> utf8Json)
        {
            return BenchmarkHash.ComputeSha256(Canonicalize(utf8Json));
        }

        /// <summary>
        /// Computes the SHA-256 of the canonical JSON v1 bytes of a JSON value.
        /// </summary>
        /// <param name="json">A single valid JSON value.</param>
        /// <returns><c>sha256:</c> followed by 64 lowercase hexadecimal characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">The input violates the canonical JSON v1 rules.</exception>
        public static string ComputeSha256(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return ComputeSha256(Utf8WithoutBom.GetBytes(json).AsSpan());
        }
    }
}
