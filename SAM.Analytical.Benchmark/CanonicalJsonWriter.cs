// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SAM.Analytical.Benchmark
{
    /// <summary>
    /// Recursive traversal that emits canonical JSON v1 for a parsed <see cref="JsonElement"/>.
    /// </summary>
    /// <remarks>
    /// Internal on purpose: the canonicalization contract is exposed through
    /// <see cref="BenchmarkCanonicalJson"/> only, so the traversal and writer settings can be corrected
    /// under a new canonicalization version without changing the public API.
    /// </remarks>
    internal static class CanonicalJsonWriter
    {
        internal static void Write(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    WriteObject(element, writer);
                    break;
                case JsonValueKind.Array:
                    WriteArray(element, writer);
                    break;
                case JsonValueKind.String:
                    writer.WriteStringValue(ReadString(element));
                    break;
                case JsonValueKind.Number:
                    WriteNumber(element, writer);
                    break;
                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;
                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;
                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;
                default:
                    throw new JsonException($"Canonical JSON does not support the '{element.ValueKind}' value kind.");
            }
        }

        private static void WriteObject(JsonElement element, Utf8JsonWriter writer)
        {
            // Property names are materialised first so duplicates are rejected before anything is
            // written, and so ordering is decided on the decoded names rather than their escaped form.
            var properties = new List<KeyValuePair<string, JsonElement>>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string name = ReadName(property);
                if (!names.Add(name))
                {
                    throw new JsonException($"Canonical JSON rejects the duplicate object property name '{name}'.");
                }

                properties.Add(new KeyValuePair<string, JsonElement>(name, property.Value));
            }

            writer.WriteStartObject();
            foreach (KeyValuePair<string, JsonElement> property in properties.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Key);
                Write(property.Value, writer);
            }

            writer.WriteEndObject();
        }

        private static void WriteArray(JsonElement element, Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (JsonElement item in element.EnumerateArray())
            {
                Write(item, writer);
            }

            writer.WriteEndArray();
        }

        private static void WriteNumber(JsonElement element, Utf8JsonWriter writer)
        {
            // Integral tokens are emitted exactly. Everything else is emitted through the shortest
            // round-trippable binary64 form, so 1, 1.0, 1.00 and 1e0 all canonicalize to 1.
            if (element.TryGetInt64(out long signed))
            {
                writer.WriteNumberValue(signed);
                return;
            }

            if (element.TryGetUInt64(out ulong unsigned))
            {
                writer.WriteNumberValue(unsigned);
                return;
            }

            if (!element.TryGetDouble(out double value) || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new JsonException($"Canonical JSON rejects the non-finite or unrepresentable number '{element.GetRawText()}'.");
            }

            // Negative zero compares equal to zero, so it must not produce a different canonical hash.
            writer.WriteNumberValue(value == 0d ? 0d : value);
        }

        private static string ReadName(JsonProperty property)
        {
            try
            {
                return property.Name;
            }
            catch (InvalidOperationException exception)
            {
                throw new JsonException("Canonical JSON rejects an object property name that is not well-formed UTF-8 text.", exception);
            }
        }

        private static string ReadString(JsonElement element)
        {
            try
            {
                return element.GetString()!;
            }
            catch (InvalidOperationException exception)
            {
                throw new JsonException("Canonical JSON rejects a string value that is not well-formed UTF-8 text.", exception);
            }
        }
    }
}
