using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SAM.Analytical.Benchmark
{
    public static class BenchmarkCliHost
    {
        public static int Run(
            string[] args,
            string usage,
            IEnumerable<string> requiredOptions,
            Func<BenchmarkArguments, CancellationToken, int> body,
            TextWriter? standardOutput = null,
            TextWriter? standardError = null,
            CancellationToken cancellationToken = default)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            standardOutput = standardOutput ?? Console.Out;
            standardError = standardError ?? Console.Error;
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

                BenchmarkArgumentParseResult parsed = BenchmarkArgumentParser.Parse(args, requiredOptions);
                if (!parsed.IsSuccess)
                {
                    standardError.WriteLine("error: " + parsed.Error);
                    standardError.WriteLine(usage);
                    return (int)BenchmarkExitCode.InvalidUsage;
                }

                if (parsed.Arguments!.HelpRequested)
                {
                    standardOutput.WriteLine(usage);
                    return (int)BenchmarkExitCode.Success;
                }

                cancellationToken.ThrowIfCancellationRequested();
                return body(parsed.Arguments, cancellationToken);
            }
            catch (Exception exception)
            {
                standardError.WriteLine("error: " + exception.Message);
                return (int)MapException(exception);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        public static BenchmarkExitCode MapException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception is BenchmarkValidationException)
            {
                return BenchmarkExitCode.ValidationFailure;
            }

            if (exception is IOException || exception is UnauthorizedAccessException
                || exception is JsonException || exception is DecoderFallbackException)
            {
                return BenchmarkExitCode.InputOutputOrSerializationFailure;
            }

            if (exception is ArgumentException || exception is FormatException)
            {
                return BenchmarkExitCode.InvalidUsage;
            }

            return BenchmarkExitCode.ProducerFailure;
        }
    }
}
