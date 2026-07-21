using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkArguments
    {
        private readonly IReadOnlyDictionary<string, string> options;

        internal BenchmarkArguments(IDictionary<string, string> options, bool helpRequested)
        {
            this.options = new ReadOnlyDictionary<string, string>(options);
            HelpRequested = helpRequested;
        }

        public bool HelpRequested { get; }

        public IReadOnlyDictionary<string, string> Options => options;

        public bool HasOption(string name)
        {
            return options.ContainsKey(NormalizeName(name));
        }

        public string? GetOption(string name)
        {
            return options.TryGetValue(NormalizeName(name), out string? value) ? value : null;
        }

        public string RequireOption(string name)
        {
            string normalizedName = NormalizeName(name);
            if (!options.TryGetValue(normalizedName, out string? value))
            {
                throw new ArgumentException($"Missing required option '--{normalizedName}'.", nameof(name));
            }

            return value;
        }

        public TimeSpan? GetTimeout(string name = "timeout-seconds")
        {
            string? text = GetOption(name);
            if (text == null)
            {
                return null;
            }

            if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double seconds)
                || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0)
            {
                throw new ArgumentException($"Option '--{NormalizeName(name)}' must be a positive number of seconds.", nameof(name));
            }

            try
            {
                return TimeSpan.FromSeconds(seconds);
            }
            catch (OverflowException exception)
            {
                throw new ArgumentException($"Option '--{NormalizeName(name)}' is outside the supported timeout range.", nameof(name), exception);
            }
        }

        internal static string NormalizeName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return name.StartsWith("--", StringComparison.Ordinal) ? name.Substring(2) : name;
        }
    }
}
