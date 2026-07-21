// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Benchmark
{
    public sealed class ValidationIssue
    {
        public ValidationIssue(string code, string path, string message, ValidationSeverity severity)
        {
            Code = code;
            Path = path;
            Message = message;
            Severity = severity;
        }

        public string Code { get; }

        public string Path { get; }

        public string Message { get; }

        public ValidationSeverity Severity { get; }

        public override string ToString()
        {
            return $"{Severity}: {Code} at {Path}: {Message}";
        }
    }
}
