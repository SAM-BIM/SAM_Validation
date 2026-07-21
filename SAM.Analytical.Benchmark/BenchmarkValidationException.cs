using System;
using System.Linq;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkValidationException : Exception
    {
        public BenchmarkValidationException(BenchmarkValidationResult validationResult)
            : base(CreateMessage(validationResult))
        {
            ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
        }

        public BenchmarkValidationResult ValidationResult { get; }

        private static string CreateMessage(BenchmarkValidationResult validationResult)
        {
            if (validationResult == null)
            {
                return "Benchmark validation failed.";
            }

            return "Benchmark validation failed: " + string.Join("; ", validationResult.Errors.Select(x => x.ToString()));
        }
    }
}
