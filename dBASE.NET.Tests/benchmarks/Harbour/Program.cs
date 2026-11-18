using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Running;

internal static class Program
{
    internal const string DatasetPathVariable = "DBASE_HARBOUR_BENCHMARK_PATH";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || !File.Exists(args[0]))
        {
            Console.Error.WriteLine("Usage: HarbourBenchmarks <test_main.dbf> [BenchmarkDotNet options]");
            return 2;
        }

        // The generated benchmark process starts in a different working directory.
        Environment.SetEnvironmentVariable(DatasetPathVariable, Path.GetFullPath(args[0]));
        string[] options = args.Length == 1 ? new[] { "--filter", "*" } : args.Skip(1).ToArray();

        var summaries = BenchmarkSwitcher.FromTypes(new[] { typeof(HarbourReadBenchmarks) }).Run(options);
        return summaries.Any(summary => summary.HasCriticalValidationErrors ||
            summary.Reports.Any(report => !report.Success || report.ResultStatistics == null)) ? 1 : 0;
    }
}
