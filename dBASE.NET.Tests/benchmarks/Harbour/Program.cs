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
            Console.Error.WriteLine("Usage: HarbourBenchmarks <test_main.dbf> [--verify | BenchmarkDotNet options], <memo/dbt/large.dbf> --dbt [...], or <requests.dbf> --keystone [...]");
            return 2;
        }

        // The generated benchmark process starts in a different working directory.
        Environment.SetEnvironmentVariable(DatasetPathVariable, Path.GetFullPath(args[0]));
        if (args.Length > 1 && args[1] == "--keystone")
        {
            if (args.Length == 3 && args[2] == "--verify")
            {
                KeystoneRequestBenchmarks.Verify();
                new KeystoneStreetBenchmarks().Verify();
                Console.WriteLine($"Keystone scan checks passed on {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.ProcessArchitecture}.");
                return 0;
            }

            return Run(new[] { typeof(KeystoneRequestBenchmarks), typeof(KeystoneStreetBenchmarks) }, args.Skip(2).ToArray());
        }
        if (args.Length == 2 && args[1] == "--verify")
        {
            var checks = new HarbourReadBenchmarks();
            try
            {
                checks.SetUp();
                checks.VerifyReads();
#if NET8_0_OR_GREATER
                TextTrimBenchmarks.VerifyDbfText();
#endif
            }
            finally
            {
                checks.TearDown();
            }
            Console.WriteLine($"Harbour read checks passed on {RuntimeInformation.FrameworkDescription}.");
            return 0;
        }
        bool dbt = args.Length > 1 && args[1] == "--dbt";
        if (dbt && args.Length == 3 && args[2] == "--verify")
        {
            var checks = new DbtMemoBenchmarks();
            try
            {
                checks.SetUp();
                checks.VerifyReads();
            }
            finally
            {
                checks.TearDown();
            }
            Console.WriteLine($"DBT read checks passed on {RuntimeInformation.FrameworkDescription}.");
            return 0;
        }
        int optionStart = dbt ? 2 : 1;
        var scenarios = dbt
            ? new[] { typeof(DbtMemoBenchmarks) }
            : new[]
            {
                typeof(HarbourReadBenchmarks), typeof(BinaryFieldBenchmarks), typeof(RandomReadBenchmarks), typeof(MemoryReadBenchmarks),
#if NET8_0_OR_GREATER
                typeof(TextTrimBenchmarks),
#endif
            };

        return Run(scenarios, args.Skip(optionStart).ToArray());
    }

    private static int Run(Type[] scenarios, string[] options)
    {
        if (options.Length == 0) options = new[] { "--filter", "*" };
        var summaries = BenchmarkSwitcher.FromTypes(scenarios).Run(options);
        return summaries.Any(summary => summary.HasCriticalValidationErrors ||
            summary.Reports.Any(report => !report.Success || report.ResultStatistics == null)) ? 1 : 0;
    }
}
