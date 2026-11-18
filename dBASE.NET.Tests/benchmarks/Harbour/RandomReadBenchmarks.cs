using System;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using dBASE.NET;

[MemoryDiagnoser]
[JsonExporterAttribute.Brief]
public class RandomReadBenchmarks
{
    private string path;
    private long[] offsets;

    [Params(128, 10000)]
    public int Reads { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        path = Environment.GetEnvironmentVariable(Program.DatasetPathVariable);
        using (var table = FastDbf.ReadFile(path, Encoding.ASCII, readOnly: true))
            PrepareOffsets(table);
    }

    private void PrepareOffsets(FastDbf table)
    {
        int dateColumn = Enumerable.Range(0, table.Fields.Count).Single(index => table.Fields[index].Name == "PAY_DATE");
        int dateOffset = 1 + table.Fields.Take(dateColumn).Sum(field => (int)field.Length);
        if (table.Fields[dateColumn].Length != sizeof(long) || table.RecordCount == 0)
            throw new InvalidOperationException("Expected nonempty DBF records with eight-byte dates.");

        int headerLength;
        int recordLength;
        using (var headerReader = new BinaryReader(File.OpenRead(path)))
        {
            headerReader.BaseStream.Position = 8;
            headerLength = headerReader.ReadUInt16();
            recordLength = headerReader.ReadUInt16();
        }

        int[] rows = Enumerable.Range(0, table.RecordCount).ToArray();
        var random = new Random(866);
        for (int i = rows.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (rows[i], rows[j]) = (rows[j], rows[i]);
        }

        offsets = new long[Reads];
        long expected = 0;
        for (int i = 0; i < offsets.Length; i++)
        {
            int row = rows[i % rows.Length];
            offsets[i] = headerLength + (long)recordLength * row + dateOffset;
            var date = (DateTime)table.GetValue(row, dateColumn);
            var bytes = Encoding.ASCII.GetBytes(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            expected = unchecked(expected + BitConverter.ToInt64(bytes, 0));
        }

        if (BufferedFileStream() != expected || PointReadsWithReusedBuffer() != expected || MappedFile() != expected)
            throw new InvalidOperationException("The readers disagree on the same physical DBF fields.");
    }

    [Benchmark(Baseline = true)]
    public long BufferedFileStream() => ReadFile(4096);

    [Benchmark]
    public long PointReadsWithReusedBuffer() => ReadFile(1);

    [Benchmark]
    public long MappedFile()
    {
        using (var file = File.OpenRead(path))
        using (var mapped = MemoryMappedFile.CreateFromFile(file, null, 0, MemoryMappedFileAccess.Read,
            HandleInheritability.None, leaveOpen: true))
        using (var view = mapped.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
        {
            long sum = 0;
            foreach (long offset in offsets)
                sum = unchecked(sum + view.ReadInt64(offset));
            return sum;
        }
    }

    private long ReadFile(int bufferSize)
    {
        // BinaryReader keeps a reusable buffer for each eight-byte point read.
        using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
        using (var reader = new BinaryReader(file))
        {
            long sum = 0;
            foreach (long offset in offsets)
            {
                file.Position = offset;
                sum = unchecked(sum + reader.ReadInt64());
            }
            return sum;
        }
    }
}
