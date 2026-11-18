using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using dBASE.NET;

[MemoryDiagnoser]
[JsonExporterAttribute.Brief]
public class DbtMemoBenchmarks
{
    private const int Reads = 10000;
    private FastDbf table;
    private int memoColumn;
    private int[] memoColumns;

    [GlobalSetup]
    public void SetUp()
    {
#if NET8_0_OR_GREATER
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        var path = Environment.GetEnvironmentVariable(Program.DatasetPathVariable);
        table = FastDbf.ReadFile(path, Encoding.GetEncoding(866), readOnly: true);
        memoColumn = Enumerable.Range(0, table.Fields.Count).Single(index => table.Fields[index].Type == DbfFieldType.Memo);
        memoColumns = new[] { memoColumn };
        if (((string)table.GetValue(2, memoColumn)).Length != 1404)
            throw new InvalidOperationException("Expected the large DBT test fixture.");
    }

    [GlobalCleanup]
    public void TearDown() => table?.Dispose();

    [Benchmark]
    public long ReadMemoValues()
    {
        long length = 0;
        for (int read = 0; read < Reads; read++)
            length += (table.GetValue(read % table.RecordCount, memoColumn) as string)?.Length ?? 0;
        return length;
    }

    [Benchmark]
    public long BlockReadMemoValues()
    {
        long length = 0;
        int read = 0;
        while (read < Reads)
        {
            foreach (var row in table.ReadValues(memoColumns))
            {
                length += (row.Values[0] as string)?.Length ?? 0;
                if (++read == Reads)
                    return length;
            }
        }
        return length;
    }

    internal void VerifyReads()
    {
        int index = 0;
        foreach (var row in table.ReadValues(memoColumns))
        {
            if (row.RowIndex != index || row.IsDeleted != table.IsDeleted(index) ||
                !Equals(table.GetValue(index, memoColumn), row.Values[0]))
                throw new InvalidOperationException($"DBT projection differs at row {index}.");
            index++;
        }
        if (index != table.RecordCount)
            throw new InvalidOperationException("Unexpected DBT record count.");
    }
}
