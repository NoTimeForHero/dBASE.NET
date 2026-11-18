using System;
using System.IO;
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
    private FastDbf memoryTable;
    private int memoColumn;
    private int[] memoColumns;
    private string path;
#if NET8_0_OR_GREATER
    private byte[] markerData;
#endif

    [GlobalSetup]
    public void SetUp()
    {
#if NET8_0_OR_GREATER
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        path = Environment.GetEnvironmentVariable(Program.DatasetPathVariable);
        var encoding = Encoding.GetEncoding(866);
        table = FastDbf.ReadFile(path, encoding, readOnly: true);
        memoryTable = new FastDbf(new MemoryStream(File.ReadAllBytes(path), writable: false),
            new MemoryStream(File.ReadAllBytes(Path.ChangeExtension(path, ".dbt")), writable: false),
            encoding, readOnly: true);
        memoColumn = Enumerable.Range(0, table.Fields.Count).Single(index => table.Fields[index].Type == DbfFieldType.Memo);
        memoColumns = new[] { memoColumn };
        var largeMemo = (string)table.GetValue(2, memoColumn);
        if (largeMemo.Length != 1404)
            throw new InvalidOperationException("Expected the large DBT test fixture.");
#if NET8_0_OR_GREATER
        markerData = encoding.GetBytes(largeMemo + "\u001a\u001a");
        if (ScalarMarker() != largeMemo.Length * Reads || SpanMarker() != largeMemo.Length * Reads)
            throw new InvalidOperationException("The DBT marker searches disagree.");
#endif
    }

    [GlobalCleanup]
    public void TearDown()
    {
        table?.Dispose();
        memoryTable?.Dispose();
    }

    [Benchmark]
    public long ReadMemoValues() => ReadMemoValues(table);

    [Benchmark]
    public long ReadMemoInMemory() => ReadMemoValues(memoryTable);

    private long ReadMemoValues(FastDbf source)
    {
        long length = 0;
        for (int read = 0; read < Reads; read++)
            length += (source.GetValue(read % source.RecordCount, memoColumn) as string)?.Length ?? 0;
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

#if NET8_0_OR_GREATER
    [Benchmark]
    public int ScalarMarker()
    {
        int sum = 0;
        for (int read = 0; read < Reads; read++)
        {
            for (int i = 1; i < markerData.Length; i++)
            {
                if (markerData[i] != 0x1a || markerData[i - 1] != 0x1a) continue;
                sum += i - 1;
                break;
            }
        }
        return sum;
    }

    [Benchmark]
    public int SpanMarker()
    {
        int sum = 0;
        for (int read = 0; read < Reads; read++)
            sum += markerData.AsSpan().IndexOf("\u001a\u001a"u8);
        return sum;
    }
#endif

    internal void VerifyReads()
    {
        int index = 0;
        foreach (var row in table.ReadValues(memoColumns))
        {
            if (row.RowIndex != index || row.IsDeleted != table.IsDeleted(index) ||
                !Equals(table.GetValue(index, memoColumn), row.Values[0]) ||
                !Equals(memoryTable.GetValue(index, memoColumn), row.Values[0]))
                throw new InvalidOperationException($"DBT projection differs at row {index}.");
            index++;
        }
        if (index != table.RecordCount)
            throw new InvalidOperationException("Unexpected DBT record count.");
        VerifyMarkerAtBlockBoundary();
        VerifyMemoOverwrite();
    }

    private void VerifyMemoOverwrite()
    {
        using (var dbf = new MemoryStream(File.ReadAllBytes(path)))
        using (var memo = new MemoryStream(File.ReadAllBytes(Path.ChangeExtension(path, ".dbt"))))
        using (var editable = new FastDbf(dbf, memo, table.Encoding))
        {
            var other = editable.GetValue(0, memoColumn);
            var record = editable.GetRecord(2);
            const string replacement = "Проверка DBT";
            record.Data[memoColumn] = replacement;
            editable.WriteRecord(2, record);
            if (!Equals(replacement, editable.GetValue(2, memoColumn)) || !Equals(other, editable.GetValue(0, memoColumn)))
                throw new InvalidOperationException("Overwriting a DBT memo changed the wrong payload.");
        }
    }

    private void VerifyMarkerAtBlockBoundary()
    {
        var dbfBytes = File.ReadAllBytes(path);
        var fields = table.Fields;
        int pointerOffset = BitConverter.ToUInt16(dbfBytes, 8) + 1 + fields.Take(memoColumn).Sum(field => field.Length);
        Encoding.ASCII.GetBytes("1".PadLeft(fields[memoColumn].Length)).CopyTo(dbfBytes, pointerOffset);

        var memoBytes = new byte[1026];
        string payload = new string('A', 200) + (char)0x1a + new string('B', 310);
        Encoding.ASCII.GetBytes(payload).CopyTo(memoBytes, 512);
        memoBytes[1023] = 0x1a;
        memoBytes[1024] = 0x1a;
        memoBytes[1025] = (byte)'X';

        foreach (bool shortReads in new[] { false, true })
        {
            using (var dbf = new MemoryStream(dbfBytes, writable: false))
            using (var memo = shortReads ? new ShortMemoStream(memoBytes) : new MemoryStream(memoBytes, writable: false))
            using (var input = new FastDbf(dbf, memo, readOnly: true))
            {
                if (!Equals(payload, input.GetValue(0, memoColumn)) || memo.Position != 1023)
                    throw new InvalidOperationException("DBT decoding mishandled an isolated or split end marker.");
            }
        }

        memoBytes[1024] = 0;
        using (var dbf = new MemoryStream(dbfBytes, writable: false))
        using (var memo = new MemoryStream(memoBytes, writable: false))
        using (var input = new FastDbf(dbf, memo, readOnly: true))
        {
            try
            {
                input.GetValue(0, memoColumn);
                throw new InvalidOperationException("DBT decoding accepted a missing end marker.");
            }
            catch (InvalidDataException)
            {
                // A memo without a complete end marker is malformed.
            }
        }
    }

    private sealed class ShortMemoStream : MemoryStream
    {
        internal ShortMemoStream(byte[] bytes) : base(bytes, writable: false) { }

        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(13, count));
    }
}
