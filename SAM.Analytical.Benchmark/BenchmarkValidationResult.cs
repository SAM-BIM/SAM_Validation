using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkValidationResult
    {
        internal BenchmarkValidationResult(IEnumerable<ValidationIssue> issues)
        {
            Issues = issues.ToArray();
            Errors = Issues.Where(x => x.Severity == ValidationSeverity.Error).ToArray();
            Warnings = Issues.Where(x => x.Severity == ValidationSeverity.Warning).ToArray();
        }

        public IReadOnlyList<ValidationIssue> Issues { get; }

        public IReadOnlyList<ValidationIssue> Errors { get; }

        public IReadOnlyList<ValidationIssue> Warnings { get; }

        public bool IsValid => Errors.Count == 0;
    }
}
