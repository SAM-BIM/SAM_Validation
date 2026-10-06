// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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
