using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using dBASE.NET;

const int openIterations = 100;
const int sampleCount = 3;
if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: HarbourBenchmarks <test_main.dbf>");
    return 2;
}

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var encoding = Encoding.GetEncoding(866);
var path = Path.GetFullPath(args[0]);
using var table = OpenTable();
var idColumn = FindColumn("ID");
var printedColumn = FindColumn("IS_PRINTED");
var memoColumn = FindColumn("NOTE");

Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
Console.WriteLine($"Dataset: {path}");
Console.WriteLine($"Records: {table.RecordCount}, DBF bytes: {new FileInfo(path).Length}, " +
                  $"FPT bytes: {new FileInfo(Path.ChangeExtension(path, "fpt")).Length}, encoding: {encoding.CodePage}");
Console.WriteLine("Operation\tMedian ms/pass\tAllocated bytes/pass\tChecksum");

Measure("Open and close", openIterations, () =>
{
    using var opened = OpenTable();
    return opened.RecordCount;
});
Measure("Sequential GetRecord", 1, () =>
{
    long sum = 0;
    for (int row = 0; row < table.RecordCount; row++)
        sum += (long)(double)table.GetRecord(row)[idColumn];
    return sum;
});
Measure("GetValue (ID, IS_PRINTED)", 1, () =>
{
    long sum = 0;
    for (int row = 0; row < table.RecordCount; row++)
    {
        sum += (long)(double)table.GetValue(row, idColumn);
        if ((bool)table.GetValue(row, printedColumn)) sum++;
    }
    return sum;
});
Measure("GetValue (NOTE)", 1, () =>
{
    long length = 0;
    for (int row = 0; row < table.RecordCount; row++)
        length += (table.GetValue(row, memoColumn) as string)?.Length ?? 0;
    return length;
});
return 0;

FastDbf OpenTable() => FastDbf.ReadFile(path, encoding, readOnly: true);

int FindColumn(string name) => Enumerable.Range(0, table.Fields.Count)
    .Single(index => table.Fields[index].Name == name);

static void Measure(string name, int passes, Func<long> action)
{
    long expected = action(); // Warm up before collecting samples.
    var durations = new double[sampleCount];
    var allocations = new long[sampleCount];
    for (int sample = 0; sample < sampleCount; sample++)
    {
        long checksum = 0;
        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        for (int pass = 0; pass < passes; pass++) checksum += action();
        durations[sample] = Stopwatch.GetElapsedTime(start).TotalMilliseconds / passes;
        allocations[sample] = (GC.GetAllocatedBytesForCurrentThread() - bytesBefore) / passes;
        if (checksum != expected * passes)
            throw new InvalidOperationException($"Results changed while measuring {name}.");
    }

    Array.Sort(durations);
    Array.Sort(allocations);
    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1:F3}\t{2}\t{3}",
        name, durations[sampleCount / 2], allocations[sampleCount / 2], expected));
}
