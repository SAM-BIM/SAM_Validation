// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Benchmark.Compare
{
    /// <summary>
    /// Thrown by <see cref="Query.Compare"/> when a document's schema version is malformed or its major
    /// version is incompatible with the comparator's supported schema major. The CLI maps this to the
    /// shared validation exit code (4), consistent with how the schema library reports an incompatible
    /// document when it is read.
    /// </summary>
    public sealed class SchemaIncompatibleException : Exception
    {
        public SchemaIncompatibleException(string message)
            : base(message)
        {
        }
    }
}
