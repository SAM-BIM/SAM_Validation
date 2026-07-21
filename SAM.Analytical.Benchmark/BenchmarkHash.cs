// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Analytical.Benchmark
{
    public static class BenchmarkHash
    {
        public static string ComputeSha256(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                return "sha256:" + ToLowerHex(sha256.ComputeHash(bytes));
            }
        }

        public static string ComputeSha256(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return ComputeSha256(Encoding.UTF8.GetBytes(text));
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
