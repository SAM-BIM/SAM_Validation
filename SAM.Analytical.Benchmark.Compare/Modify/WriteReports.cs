// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Modify
    {
        /// <summary>The Markdown report file name written into the output directory.</summary>
        public const string MarkdownFileName = "comparison.md";

        /// <summary>The CSV report file name written into the output directory.</summary>
        public const string CsvFileName = "comparison.csv";

        /// <summary>The machine-readable summary file name written into the output directory.</summary>
        public const string SummaryFileName = "comparison-summary.json";

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false, true);

        /// <summary>The report text encoded as UTF-8 without a byte order mark (LF newlines preserved).</summary>
        public static byte[] ToUtf8(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return Utf8WithoutBom.GetBytes(text);
        }

        /// <summary>
        /// Writes all three reports (<c>comparison.md</c>, <c>comparison.csv</c>,
        /// <c>comparison-summary.json</c>) into <paramref name="outputDirectory"/>, creating it if needed,
        /// as UTF-8 without BOM and LF newlines. Returns the written file paths in write order.
        /// </summary>
        public static IReadOnlyList<string> WriteReports(ComparisonResult result, string outputDirectory)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
            }

            string fullDirectory = Path.GetFullPath(outputDirectory);
            if (File.Exists(fullDirectory))
            {
                throw new IOException("The output path identifies a file, not a directory: " + fullDirectory);
            }

            Directory.CreateDirectory(fullDirectory);

            string markdownPath = Path.Combine(fullDirectory, MarkdownFileName);
            string csvPath = Path.Combine(fullDirectory, CsvFileName);
            string summaryPath = Path.Combine(fullDirectory, SummaryFileName);

            File.WriteAllBytes(markdownPath, ToUtf8(WriteMarkdown(result)));
            File.WriteAllBytes(csvPath, ToUtf8(WriteCsv(result)));
            File.WriteAllBytes(summaryPath, ToUtf8(WriteSummaryJson(result)));

            return new[] { markdownPath, csvPath, summaryPath };
        }
    }
}
