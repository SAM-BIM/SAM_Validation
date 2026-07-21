namespace SAM.Analytical.Benchmark
{
    public enum BenchmarkExitCode
    {
        Success = 0,
        InvalidUsage = 2,
        InputOutputOrSerializationFailure = 3,
        ValidationFailure = 4,
        ProducerFailure = 5
    }
}
