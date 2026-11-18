using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
[JsonExporterAttribute.Brief]
public class BinaryFieldBenchmarks
{
    private const int RecordCount = 10000;
    private const int RecordWidth = 16;
    private ArraySegment<byte>[] fields;

    [GlobalSetup]
    public void SetUp()
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("This comparison uses the existing little-endian DBF reader.");

        var records = new byte[RecordCount * RecordWidth];
        fields = new ArraySegment<byte>[RecordCount];
        for (int row = 0; row < fields.Length; row++)
        {
            int offset = row * RecordWidth + 1;
            long value = ((long)(row - RecordCount / 2) << 32) | (uint)(-row);
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, records, offset, sizeof(long));
            fields[row] = new ArraySegment<byte>(records, offset, sizeof(long));

            var span = new ReadOnlySpan<byte>(records, offset, sizeof(long));
            if (BitConverter.ToInt64(records, offset) != value ||
                BinaryPrimitives.ReadInt64LittleEndian(span) != value ||
                Unsafe.ReadUnaligned<long>(ref records[offset]) != value ||
                BitConverter.ToInt32(records, offset) != -row ||
                BinaryPrimitives.ReadInt32LittleEndian(span) != -row ||
                Unsafe.ReadUnaligned<int>(ref records[offset]) != -row)
                throw new InvalidOperationException($"Binary reads disagree at row {row}.");
        }
    }

    [Benchmark(Baseline = true)]
    public long BitConverterByOffset()
    {
        long sum = 0;
        foreach (var field in fields)
        {
            RequireWidth(field);
            sum += BitConverter.ToInt32(field.Array, field.Offset);
            sum += BitConverter.ToInt64(field.Array, field.Offset);
        }
        return sum;
    }

    [Benchmark]
    public long UnsafeUnaligned()
    {
        long sum = 0;
        foreach (var field in fields)
        {
            RequireWidth(field);
            sum += Unsafe.ReadUnaligned<int>(ref field.Array[field.Offset]);
            sum += Unsafe.ReadUnaligned<long>(ref field.Array[field.Offset]);
        }
        return sum;
    }

    [Benchmark]
    public long BinaryPrimitivesLittleEndian()
    {
        long sum = 0;
        foreach (var field in fields)
        {
            RequireWidth(field);
            var bytes = new ReadOnlySpan<byte>(field.Array, field.Offset, field.Count);
            sum += BinaryPrimitives.ReadInt32LittleEndian(bytes);
            sum += BinaryPrimitives.ReadInt64LittleEndian(bytes);
        }
        return sum;
    }

    private static void RequireWidth(ArraySegment<byte> field)
    {
        if (field.Count < sizeof(long))
            throw new ArgumentException("The DBF field requires at least 8 bytes.", nameof(field));
    }
}
