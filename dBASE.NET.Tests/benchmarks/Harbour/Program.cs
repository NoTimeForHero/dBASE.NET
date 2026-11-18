using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;

internal static class Program
{
    internal const string DatasetPathVariable = "DBASE_HARBOUR_BENCHMARK_PATH";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || !File.Exists(args[0]))
        {
            Console.Error.WriteLine("Usage: HarbourBenchmarks <test_main.dbf> [--verify | BenchmarkDotNet options] or <memo/dbt/large.dbf> --dbt [BenchmarkDotNet options]");
            return 2;
        }

        // The generated benchmark process starts in a different working directory.
        Environment.SetEnvironmentVariable(DatasetPathVariable, Path.GetFullPath(args[0]));
        if (args.Length == 2 && args[1] == "--verify")
        {
            var checks = new HarbourReadBenchmarks();
            try
            {
                checks.SetUp();
                checks.VerifyReads();
            }
            finally
            {
                checks.TearDown();
            }
            Console.WriteLine($"Harbour read checks passed on {RuntimeInformation.FrameworkDescription}.");
            return 0;
        }
        bool dbt = args.Length > 1 && args[1] == "--dbt";
        int optionStart = dbt ? 2 : 1;
        string[] options = args.Length == optionStart ? new[] { "--filter", "*" } : args.Skip(optionStart).ToArray();
        var scenarios = dbt
            ? new[] { typeof(DbtMemoBenchmarks) }
            : new[] { typeof(HarbourReadBenchmarks), typeof(BinaryFieldBenchmarks), typeof(RandomReadBenchmarks) };

        var summaries = BenchmarkSwitcher.FromTypes(scenarios).Run(options);
        return summaries.Any(summary => summary.HasCriticalValidationErrors ||
            summary.Reports.Any(report => !report.Success || report.ResultStatistics == null)) ? 1 : 0;
    }
}
