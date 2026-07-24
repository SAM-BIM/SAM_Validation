// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace SAM.Analytical.Benchmark.Compare
{
    public static partial class Modify
    {
        private static readonly string[] CsvHeader =
        {
            "scope",
            "metric",
            "unit",
            "availability",
            "tasValue",
            "openStudioValue",
            "absoluteDifference",
            "relativeDifference",
            "relativePercent",
            "signedDifference",
            "band",
            "note"
        };

        /// <summary>
        /// Renders the per-metric comparison table (model metrics followed by every space's metrics, in
        /// the deterministic order produced by <see cref="Query.Compare"/>) as RFC-4180 CSV with LF line
        /// endings. Numeric fields are full-precision and unrounded.
        /// </summary>
        public static string WriteCsv(ComparisonResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var builder = new StringBuilder();
            AppendRow(builder, CsvHeader);

            foreach (MetricComparison metric in result.ModelMetrics)
            {
                AppendMetricRow(builder, metric);
            }

            foreach (SpaceComparison space in result.Spaces)
            {
                foreach (MetricComparison metric in space.Metrics)
                {
                    AppendMetricRow(builder, metric);
                }
            }

            return builder.ToString();
        }

        private static void AppendMetricRow(StringBuilder builder, MetricComparison metric)
        {
            AppendRow(builder, new[]
            {
                metric.Scope,
                metric.Key,
                Format.Unit(metric.Unit),
                Format.Availability(metric.Availability),
                Format.Number(metric.TasValue),
                Format.Number(metric.OpenStudioValue),
                Format.Number(metric.AbsoluteDifference),
                Format.Number(metric.RelativeDifference),
                Format.Percent(metric.RelativeDifference),
                Format.Number(metric.SignedDifference),
                Format.Band(metric.Band),
                Format.NotApplicableNote(metric)
            });
        }

        private static void AppendRow(StringBuilder builder, IReadOnlyList<string> fields)
        {
            for (int index = 0; index < fields.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(EscapeCsv(fields[index]));
            }

            builder.Append('\n');
        }

        private static string EscapeCsv(string? field)
        {
            string value = field ?? string.Empty;
            bool mustQuote = value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0;
            if (!mustQuote)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
