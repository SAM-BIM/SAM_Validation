// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Benchmark
{
    public enum BenchmarkRoute
    {
        Unknown = 0,
        NativeOpenStudio,
        NativeTas,
        SharedGbXmlOpenStudio,
        SharedGbXmlTas
    }

    public enum EngineKind
    {
        Unknown = 0,
        OpenStudio,
        Tas
    }

    public enum MetricUnit
    {
        Unknown = 0,
        KilowattHour,
        WattHour,
        Kilowatt,
        Watt,
        SquareMetre,
        CubicMetre,
        HourOfYear,
        Hour
    }

    public enum RunState
    {
        Unknown = 0,
        Success,
        Failure
    }

    public enum DesignDaySource
    {
        Unknown = 0,
        Ddy,
        EmbeddedModel,
        None
    }

    public enum SchemaCompatibility
    {
        Malformed = 0,
        IncompatibleMajor,
        Compatible,
        CompatibleWithMinorWarning
    }

    public enum ValidationSeverity
    {
        Error = 0,
        Warning
    }
}
