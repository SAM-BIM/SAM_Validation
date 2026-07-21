// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Benchmark
{
    public static class BenchmarkArgumentParser
    {
        private static readonly Regex OptionNamePattern = new Regex("^[A-Za-z][A-Za-z0-9-]*$", RegexOptions.CultureInvariant);

        public static BenchmarkArgumentParseResult Parse(string[]? args, IEnumerable<string>? requiredOptions = null)
        {
            if (args == null)
            {
                return Failure("Arguments are required.");
            }

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool helpRequested = false;
            for (int index = 0; index < args.Length; index++)
            {
                string argument = args[index];
                if (argument == "--help" || argument == "-h")
                {
                    helpRequested = true;
                    continue;
                }

                if (!argument.StartsWith("--", StringComparison.Ordinal) || argument.Length == 2)
                {
                    return Failure($"Unexpected argument '{argument}'. Options must use --name value or --name=value.");
                }

                string option;
                string value;
                int separator = argument.IndexOf('=');
                if (separator >= 0)
                {
                    option = argument.Substring(2, separator - 2);
                    value = argument.Substring(separator + 1);
                }
                else
                {
                    option = argument.Substring(2);
                    if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        return Failure($"Option '--{option}' requires a value.");
                    }

                    value = args[++index];
                }

                if (!OptionNamePattern.IsMatch(option))
                {
                    return Failure($"Invalid option name '--{option}'.");
                }

                if (string.IsNullOrEmpty(value))
                {
                    return Failure($"Option '--{option}' requires a non-empty value.");
                }

                if (options.ContainsKey(option))
                {
                    return Failure($"Option '--{option}' was supplied more than once.");
                }

                options.Add(option, value);
            }

            var parsedArguments = new BenchmarkArguments(options, helpRequested);
            if (helpRequested)
            {
                return new BenchmarkArgumentParseResult(parsedArguments, null);
            }

            foreach (string required in requiredOptions ?? Enumerable.Empty<string>())
            {
                string normalizedName = BenchmarkArguments.NormalizeName(required);
                if (!options.ContainsKey(normalizedName))
                {
                    return Failure($"Missing required option '--{normalizedName}'.");
                }
            }

            return new BenchmarkArgumentParseResult(parsedArguments, null);
        }

        private static BenchmarkArgumentParseResult Failure(string message)
        {
            return new BenchmarkArgumentParseResult(null, message);
        }
    }
}
