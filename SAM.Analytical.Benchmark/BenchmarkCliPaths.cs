// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;

namespace SAM.Analytical.Benchmark
{
    public static class BenchmarkCliPaths
    {
        public static string ValidateInputFile(string path)
        {
            string fullPath = GetFullPath(path, nameof(path));
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The input file does not exist.", fullPath);
            }

            return fullPath;
        }

        public static string ValidateOutputFile(string path)
        {
            string fullPath = GetFullPath(path, nameof(path));
            if (Directory.Exists(fullPath))
            {
                throw new IOException("The output path identifies a directory, not a file.");
            }

            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException("The output directory does not exist.");
            }

            return fullPath;
        }

        private static string GetFullPath(string path, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A file path is required.", parameterName);
            }

            return Path.GetFullPath(path);
        }
    }
}
