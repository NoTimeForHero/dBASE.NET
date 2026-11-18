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
    private int amountColumn;
    private int nameColumn;
    private int houseColumn;
    private int dateColumn;
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
        amountColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "PAY_AMOUNT");
        nameColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "FULL_NAME");
        houseColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "HOUSE_NO");
        dateColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "PAY_DATE");
        printedColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "IS_PRINTED");
        memoColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "NOTE");
        if (SequentialGetRecord() != BlockReadRecords() || SequentialGetValue() != BlockReadValues())
            throw new InvalidOperationException("Bulk reads differ from indexed reads.");
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
    public long BlockReadRecords()
    {
        long sum = 0;
        foreach (var record in table.ReadRecords())
            sum += (long)(double)record[idColumn];
        return sum;
    }

    [Benchmark]
    public long BlockReadValues()
    {
        long sum = 0;
        foreach (var row in table.ReadValues(idColumn, printedColumn))
        {
            sum += (long)(double)row.Values[0];
            if ((bool)row.Values[1]) sum++;
        }
        return sum;
    }

    [Benchmark]
    public double BlockReadAmount()
    {
        double sum = 0;
        foreach (var row in table.ReadValues(amountColumn))
            sum += (double)row.Values[0];
        return sum;
    }

    [Benchmark]
    public long BlockReadText()
    {
        long length = 0;
        foreach (var row in table.ReadValues(nameColumn, houseColumn))
            length += ((string)row.Values[0]).Length + ((string)row.Values[1]).Length;
        return length;
    }

    [Benchmark]
    public long BlockReadDateAndLogical()
    {
        long sum = 0;
        foreach (var row in table.ReadValues(dateColumn, printedColumn))
        {
            sum += ((DateTime)row.Values[0]).DayOfYear;
            if ((bool)row.Values[1]) sum++;
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

    [Benchmark]
    public long BlockReadMemo()
    {
        long length = 0;
        foreach (var row in table.ReadValues(memoColumn))
            length += (row.Values[0] as string)?.Length ?? 0;
        return length;
    }

    private FastDbf OpenTable() => FastDbf.ReadFile(path, encoding, readOnly: true);
}
