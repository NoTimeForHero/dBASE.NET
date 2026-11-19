using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using dBASE.NET;

internal static class KeystoneScanData
{
    internal static string RequestsPath => Environment.GetEnvironmentVariable(Program.DatasetPathVariable);
    internal static Encoding Cp866 { get; } = GetEncoding();

    private static Encoding GetEncoding()
    {
#if NET8_0_OR_GREATER
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        return Encoding.GetEncoding(866);
    }

    internal static int[] Select(FastDbf table, params string[] names)
    {
        var fields = table.Fields;
        return names.Select(name => Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == name)).ToArray();
    }
}

[MemoryDiagnoser, MedianColumn, JsonExporterAttribute.Full]
public class KeystoneRequestBenchmarks
{
    private const int City = 78;
    private const string Status = "A";
    private FastDbf table;
    private int[] columns;

    [GlobalSetup]
    public void SetUp()
    {
        table = FastDbf.ReadFile(KeystoneScanData.RequestsPath, KeystoneScanData.Cp866, readOnly: true);
        columns = KeystoneScanData.Select(table, "CITY", "STATUS", "TICKET");
    }

    [GlobalCleanup]
    public void TearDown() => table?.Dispose();

    [Benchmark(Baseline = true)]
    public List<(int RecNo, int Ticket)> ReadValues()
    {
        var matches = new List<(int RecNo, int Ticket)>();
        foreach (var row in table.ReadValues(columns))
            if (!row.IsDeleted && (double)row.Values[0] == City && (string)row.Values[1] == Status)
                matches.Add((row.RowIndex + 1, checked((int)(double)row.Values[2])));
        return matches;
    }

    [Benchmark]
    public List<(int RecNo, int Ticket)> VisitRows()
    {
        var matches = new List<(int RecNo, int Ticket)>();
        table.VisitRows(row =>
        {
            if (!row.IsDeleted && row.GetNumber(0) == City && row.GetText(1) == Status)
                matches.Add((row.RowIndex + 1, checked((int)row.GetNumber(2).Value)));
        }, columns);
        return matches;
    }

    [Benchmark]
    public int VisitMetadata()
    {
        int visible = 0;
        table.VisitRows(row => { if (!row.IsDeleted) visible++; });
        return visible;
    }

    internal static void Verify()
    {
        var benchmark = new KeystoneRequestBenchmarks();
        try
        {
            benchmark.SetUp();
            var expected = benchmark.ReadValues();
            if ((benchmark.table.RecordCount == 65656 && expected.Count != 32) ||
                !expected.SequenceEqual(benchmark.VisitRows()) || !expected.SequenceEqual(benchmark.VisitRows()) ||
                benchmark.VisitMetadata() != benchmark.table.ReadValues().Count(row => !row.IsDeleted))
                throw new InvalidOperationException("Request results differ from ReadValues.");
            if (benchmark.table.RecordCount == 65656 && benchmark.table.ReadValues(benchmark.columns)
                .Count(row => row.IsDeleted && (double)row.Values[0] == City && (string)row.Values[1] == Status) != 2)
                throw new InvalidOperationException("Expected two deleted matches in the full fixture.");
            benchmark.VerifyInterleavedReads();
            if (!expected.SequenceEqual(benchmark.VisitRows()))
                throw new InvalidOperationException("Filter results changed after an interrupted scan.");
            Console.WriteLine($"Verified {expected.Count} matches in {benchmark.table.RecordCount} physical rows.");
        }
        finally { benchmark.TearDown(); }
    }

    private void VerifyInterleavedReads()
    {
        if (table.RecordCount < 2)
            return;

        int checkpoint = Math.Min(81, table.RecordCount - 2);
        int last = table.RecordCount - 1;
        int ticket = columns[2];
        double checkpointTicket = (double)table.GetValue(checkpoint, ticket);
        double lastTicket = (double)table.GetValue(last, ticket);
        int rowsVisited = 0;
        var stop = new InvalidOperationException("Stop the test scan.");
        try
        {
            table.VisitRows(row =>
            {
                if (row.RowIndex != rowsVisited++)
                    throw new InvalidOperationException("Record order changed during traversal.");
                if (row.RowIndex == checkpoint)
                {
                    if ((double)table.GetValue(last, ticket) != lastTicket)
                        throw new InvalidOperationException("Interleaved point read returned the wrong ticket.");
                    int nestedRows = 0;
                    table.VisitRows(_ => nestedRows++);
                    if (nestedRows != table.RecordCount || row.GetNumber(0) != checkpointTicket)
                        throw new InvalidOperationException("The nested scan changed the outer scan.");
                }
                if (row.RowIndex == last)
                    throw stop;
            }, ticket);
            throw new InvalidOperationException("The visitor did not reach the last record.");
        }
        catch (InvalidOperationException ex) when (ReferenceEquals(ex, stop)) { }
    }
}

[MemoryDiagnoser, MedianColumn, JsonExporterAttribute.Full]
public class KeystoneStreetBenchmarks
{
    private static readonly char[] Padding = { ' ' };

    private static FastDbf OpenStreets() => FastDbf.ReadFile(
        Path.Combine(Path.GetDirectoryName(KeystoneScanData.RequestsPath), "streets.dbf"), KeystoneScanData.Cp866, readOnly: true);

    [Benchmark(Baseline = true)]
    public Dictionary<int, (int CityId, string Name)> ReadValues()
    {
        using (var table = OpenStreets())
        {
            int[] columns = KeystoneScanData.Select(table, "ID", "CITY", "NAME");
            var streets = new Dictionary<int, (int CityId, string Name)>(table.RecordCount);
            foreach (var row in table.ReadValues(columns))
                if (!row.IsDeleted)
                    streets.Add(checked((int)(double)row.Values[0]),
                        (checked((int)(double)row.Values[1]), ((string)row.Values[2]).TrimEnd(Padding)));
            return streets;
        }
    }

    [Benchmark]
    public Dictionary<int, (int CityId, string Name)> VisitRows()
    {
        using (var table = OpenStreets())
        {
            int[] columns = KeystoneScanData.Select(table, "ID", "CITY", "NAME");
            var streets = new Dictionary<int, (int CityId, string Name)>(table.RecordCount);
            table.VisitRows(row =>
            {
                if (!row.IsDeleted)
                    streets.Add(checked((int)row.GetNumber(0).Value),
                        (checked((int)row.GetNumber(1).Value), row.GetText(2).TrimEnd(Padding)));
            }, columns);
            return streets;
        }
    }

    internal void Verify()
    {
        var expected = ReadValues();
        var actual = VisitRows();
        if (expected.Count != 50000 || actual.Count != expected.Count ||
            expected.Any(entry => !actual.TryGetValue(entry.Key, out var value) || value != entry.Value))
            throw new InvalidOperationException("Street dictionary differs from ReadValues.");
        Console.WriteLine($"Verified {expected.Count} streets.");
    }
}
