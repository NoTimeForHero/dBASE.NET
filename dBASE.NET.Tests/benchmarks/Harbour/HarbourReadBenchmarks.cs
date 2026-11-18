using System;
using System.IO;
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
    private int[] allColumns;
    private int idColumn;
    private int amountColumn;
    private int nameColumn;
    private int houseColumn;
    private int dateColumn;
    private int printedColumn;
    private int memoColumn;
    private int[] randomRows;

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
        allColumns = Enumerable.Range(0, fields.Count).ToArray();
        idColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "ID");
        amountColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "PAY_AMOUNT");
        nameColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "FULL_NAME");
        houseColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "HOUSE_NO");
        dateColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "PAY_DATE");
        printedColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "IS_PRINTED");
        memoColumn = Enumerable.Range(0, fields.Count).Single(index => fields[index].Name == "NOTE");
        randomRows = Enumerable.Range(0, table.RecordCount).ToArray();
        var random = new Random(866);
        for (int i = randomRows.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (randomRows[i], randomRows[j]) = (randomRows[j], randomRows[i]);
        }
        long total = SequentialGetRecord();
        long selected = SequentialGetValue();
        if (total != BlockReadRecords() || total != BlockReadAllValues() || total != RandomGetRecord() ||
            selected != BlockReadValues() || selected != RandomGetValue())
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
    public long RandomGetRecord()
    {
        long sum = 0;
        foreach (int row in randomRows)
            sum += (long)(double)table.GetRecord(row)[idColumn];
        return sum;
    }

    [Benchmark]
    public long RandomGetValue()
    {
        long sum = 0;
        foreach (int row in randomRows)
        {
            sum += (long)(double)table.GetValue(row, idColumn);
            if ((bool)table.GetValue(row, printedColumn)) sum++;
        }
        return sum;
    }

    internal void VerifyReads()
    {
        using (var dbfStream = File.OpenRead(path))
        using (var memoStream = File.OpenRead(Path.ChangeExtension(path, ".fpt")))
        using (var expected = new FastDbf(dbfStream, memoStream, encoding, readOnly: true))
        {
            int index = 0;
            foreach (var projected in table.ReadValues(allColumns))
            {
                var reference = expected.GetRecord(index);
                var indexed = table.GetRecord(index);
                table.GetRecord(randomRows[index]);
                if (projected.RowIndex != index || projected.IsDeleted != reference.IsDeleted ||
                    indexed.IsDeleted != reference.IsDeleted || table.IsDeleted(index) != reference.IsDeleted)
                    throw new InvalidOperationException($"Record metadata differs at row {index}.");

                foreach (int column in allColumns)
                {
                    var actual = table.GetValue(index, column);
                    table.GetValue(randomRows[index], column);
                    if (!Equals(reference[column], actual) || !Equals(reference[column], indexed[column]) ||
                        !Equals(reference[column], projected.Values[column]))
                        throw new InvalidOperationException($"Value differs at row {index}, column {column}.");
                }
                index++;
            }
            if (index != expected.RecordCount)
                throw new InvalidOperationException("Unexpected physical record count.");
        }
        var idField = table.Fields[idColumn];
        var previousType = idField.Type;
        try
        {
            idField.Type = DbfFieldType.Character;
            if (!Equals("1", table.GetRecord(0)[idColumn]))
                throw new InvalidOperationException("Indexed record decoding ignored a schema change.");
        }
        finally
        {
            idField.Type = previousType;
        }
        VerifyShortField();
    }

    private void VerifyShortField()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string dbfPath = Path.Combine(directory, "short.dbf");
        string memoPath = Path.ChangeExtension(dbfPath, ".fpt");
        try
        {
            File.Copy(path, dbfPath);
            File.Copy(Path.ChangeExtension(path, ".fpt"), memoPath);
            using (var file = new FileStream(dbfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var header = new BinaryReader(file))
            using (var indexed = FastDbf.ReadFile(dbfPath, encoding, readOnly: true))
            {
                file.Position = 8;
                int headerLength = header.ReadUInt16();
                int recordLength = header.ReadUInt16();
                int lastRow = indexed.RecordCount - 1;
                int nameOffset = 1 + indexed.Fields.Take(nameColumn).Sum(field => (int)field.Length);
                indexed.GetValue(0, nameColumn); // Leave bytes in the field buffer before a short read.
                file.Position = headerLength + (long)lastRow * recordLength + nameOffset;
                file.WriteByte(encoding.GetBytes("Я")[0]);
                file.SetLength(file.Position);
                file.Flush();
                if (!Equals("Я", indexed.GetValue(lastRow, nameColumn)))
                    throw new InvalidOperationException("A short field read included bytes outside the available range.");
                try
                {
                    indexed.GetRecord(lastRow);
                    throw new InvalidOperationException("A short record read included bytes outside the available range.");
                }
                catch (ArgumentException)
                {
                    // The field crosses the actual end of the record.
                }
            }
        }
        finally
        {
            File.Delete(dbfPath);
            File.Delete(memoPath);
            Directory.Delete(directory);
        }
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
    public long BlockReadAllValues()
    {
        long sum = 0;
        foreach (var row in table.ReadValues(allColumns))
            sum += (long)(double)row.Values[idColumn];
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
