namespace SAM.Analytical.Benchmark
{
    public sealed class BenchmarkArgumentParseResult
    {
        internal BenchmarkArgumentParseResult(BenchmarkArguments? arguments, string? error)
        {
            Arguments = arguments;
            Error = error;
        }

        public BenchmarkArguments? Arguments { get; }

        public string? Error { get; }

        public bool IsSuccess => Error == null;
    }
}
