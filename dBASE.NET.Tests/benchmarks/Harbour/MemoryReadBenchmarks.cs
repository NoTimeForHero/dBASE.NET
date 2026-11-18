using System;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using dBASE.NET;

[MemoryDiagnoser]
[JsonExporterAttribute.Brief]
public class MemoryReadBenchmarks
{
    private FastDbf harbour;
    private FastDbf wide;
    private int[] narrowColumns;
    private int[] wideColumns;

    [GlobalSetup]
    public void SetUp()
    {
#if NET8_0_OR_GREATER
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        var encoding = Encoding.GetEncoding(866);
        var path = Environment.GetEnvironmentVariable(Program.DatasetPathVariable);
        harbour = new FastDbf(new MemoryStream(File.ReadAllBytes(path), writable: false),
            new MemoryStream(File.ReadAllBytes(Path.ChangeExtension(path, ".fpt")), writable: false),
            encoding, readOnly: true);
        var names = harbour.Fields.Select(field => field.Name).ToList();
        narrowColumns = new[] { names.IndexOf("ID"), names.IndexOf("IS_PRINTED") };
        wide = CreateWideTable(Math.Min(harbour.RecordCount, 10000), encoding);
        wideColumns = Enumerable.Range(0, wide.Fields.Count).ToArray();
        Verify(harbour, narrowColumns);
        Verify(wide, wideColumns);
    }

    [GlobalCleanup]
    public void TearDown()
    {
        harbour?.Dispose();
        wide?.Dispose();
    }

    [Benchmark]
    public long ScanHarbour() => Scan(harbour, narrowColumns);

    [Benchmark]
    public long ScanWide() => Scan(wide, wideColumns);

    private static long Scan(FastDbf table, int[] columns)
    {
        long sum = 0;
        foreach (var row in table.ReadValues(columns))
            sum += Checksum(row.IsDeleted, row.Values);
        return sum;
    }

    private static void Verify(FastDbf table, int[] columns)
    {
        long expected = 0;
        for (int index = 0; index < table.RecordCount; index++)
        {
            var record = table.GetRecord(index);
            expected += Checksum(record.IsDeleted, columns.Select(column => record[column]).ToArray());
        }
        if (Scan(table, columns) != expected)
            throw new InvalidOperationException("The in-memory indexed and projected reads disagree.");
    }

    private static long Checksum(bool deleted, object[] values)
    {
        long sum = deleted ? 1 : 0;
        foreach (object value in values)
        {
            if (value is double number) sum += (long)number;
            else if (value is bool flag && flag) sum++;
            else if (value is string text) sum += text.Length;
            else if (value is DateTime date) sum += date.DayOfYear;
        }
        return sum;
    }

    private static FastDbf CreateWideTable(int count, Encoding encoding)
    {
        using (var dbf = new Dbf(encoding))
        {
            var fields = Enumerable.Range(0, 8).SelectMany(column => new[]
            {
                new DbfField("N" + column, DbfFieldType.Numeric, 9),
                new DbfField("L" + column, DbfFieldType.Logical, 1),
                new DbfField("C" + column, DbfFieldType.Character, 64),
                new DbfField("D" + column, DbfFieldType.Date, 8),
            });
            dbf.Create(fields);
            for (int index = 0; index < count; index++)
            {
                var row = dbf.CreateRecord();
                row.IsDeleted = index % 17 == 0;
                for (int column = 0; column < 32; column += 4)
                {
                    row.Data[column] = index + column;
                    row.Data[column + 1] = index % 2 == 0;
                    row.Data[column + 2] = "Текст DBF";
                    row.Data[column + 3] = new DateTime(2024, 1, 1).AddDays(index % 366);
                }
            }
            var data = new MemoryStream();
            dbf.Write(data, leaveOpen: true);
            data.Position = 0;
            return new FastDbf(data, encoding: encoding, readOnly: true);
        }
    }
}
