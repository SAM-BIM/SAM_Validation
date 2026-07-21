// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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
