using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using dBASE.NET;

[MemoryDiagnoser]
[JsonExporterAttribute.Brief]
public class HarbourReadBenchmarks
{
    private Encoding encoding;
    private string path;
    private FastDbf table;
    private int idColumn;
    private int printedColumn;
    private int memoColumn;

    [GlobalSetup]
    public void SetUp()
    {
#if NET8_0_OR_GREATER
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        encoding = Encoding.GetEncoding(866);
        path = Environment.GetEnvironmentVariable(Program.DatasetPathVariable);
        table = OpenTable();
        var fields = table.Fields;
        idColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "ID");
        printedColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "IS_PRINTED");
        memoColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "NOTE");
    }

    [GlobalCleanup]
    public void TearDown()
    {
        table?.Dispose();
    }

    [Benchmark]
    public int OpenAndClose()
    {
        using (var opened = OpenTable())
            return opened.RecordCount;
    }

    [Benchmark]
    public long SequentialGetRecord()
    {
        long sum = 0;
        for (int row = 0; row < table.RecordCount; row++)
            sum += (long)(double)table.GetRecord(row)[idColumn];
        return sum;
    }

    [Benchmark]
    public long SequentialGetValue()
    {
        long sum = 0;
        for (int row = 0; row < table.RecordCount; row++)
        {
            sum += (long)(double)table.GetValue(row, idColumn);
            if ((bool)table.GetValue(row, printedColumn)) sum++;
        }
        return sum;
    }

    [Benchmark]
    public long SequentialGetMemo()
    {
        long length = 0;
        for (int row = 0; row < table.RecordCount; row++)
            length += (table.GetValue(row, memoColumn) as string)?.Length ?? 0;
        return length;
    }

    private FastDbf OpenTable() => FastDbf.ReadFile(path, encoding, readOnly: true);
}
