#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using dBASE.NET;

[MemoryDiagnoser]
[JsonExporterAttribute.Brief]
public class TextTrimBenchmarks
{
    private const int Rows = 10000;
    private const byte Space = (byte)' ';
    private byte[] data;

    [Params(12, 64)]
    public int Width { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        data = new byte[Rows * Width];
        Array.Fill(data, Space);
        for (int row = 0; row < Rows; row++)
        {
            if (row % 4 == 0) continue;
            int padding = row % 3;
            int length = row % 4 == 1 ? Width - padding : (row % 7) + 1;
            data.AsSpan(row * Width + padding, length).Fill(0x92); // CP866 'Т'.
        }

        int expected = ScalarTrim();
        if (SpanTrim() != expected || SpanSearch() != expected)
            throw new InvalidOperationException("The trimming implementations returned different lengths.");
    }

    [Benchmark(Baseline = true)]
    public int ScalarTrim()
    {
        int total = 0;
        for (int offset = 0; offset < data.Length; offset += Width)
        {
            int start = offset;
            int end = start + Width;
            while (start < end && data[start] == Space) start++;
            while (end > start && data[end - 1] == Space) end--;
            total += end - start;
        }
        return total;
    }

    [Benchmark]
    public int SpanTrim()
    {
        int total = 0;
        for (int offset = 0; offset < data.Length; offset += Width)
            total += data.AsSpan(offset, Width).Trim(Space).Length;
        return total;
    }

    [Benchmark]
    public int SpanSearch()
    {
        int total = 0;
        for (int offset = 0; offset < data.Length; offset += Width)
        {
            var field = data.AsSpan(offset, Width);
            int first = field.IndexOfAnyExcept(Space);
            if (first >= 0)
                total += field.Slice(first).LastIndexOfAnyExcept(Space) + 1;
        }
        return total;
    }

    internal static void VerifyDbfText()
    {
        var cp866 = Encoding.GetEncoding(866);
        using var source = new Dbf(cp866);
        source.Create(new[]
        {
            new DbfField("SHORT", DbfFieldType.Character, 12),
            new DbfField("WIDE", DbfFieldType.Character, 64),
        });
        var row = source.CreateRecord();
        row.Data[0] = " X ";
        row.Data[1] = "  Тест ";
        source.CreateRecord();

        using var serialized = new MemoryStream();
        source.Write(serialized, leaveOpen: true);
        byte[] data = serialized.ToArray();
        using var standard = new FastDbf(new MemoryStream(data), encoding: cp866, readOnly: true);
        using var custom = new FastDbf(new MemoryStream(data), encoding: new PaddingAwareEncoding(), readOnly: true);
        if (!Equals("X", standard.GetValue(0, 0)) || !Equals("Тест", standard.GetValue(0, 1)) ||
            standard.GetValue(1, 1) != null || !Equals("64", custom.GetValue(1, 1)))
            throw new InvalidOperationException("Text padding or the custom decoder result differs.");

        var strict = new UTF8Encoding(false, true);
        using var dbf = new FastDbf(new MemoryStream(data), encoding: strict, readOnly: true);
        int offset = BitConverter.ToUInt16(data, 8) + 1 + 12;
        byte[] field = data.AsSpan(offset, 64).ToArray();
        var expected = CaptureFallback(() => strict.GetString(field, 0, field.Length));
        var actual = CaptureFallback(() => dbf.GetValue(0, 1));
        if (expected.Index != actual.Index || !expected.BytesUnknown.AsSpan().SequenceEqual(actual.BytesUnknown))
            throw new InvalidOperationException("Strict UTF-8 decoding reported a different byte range.");
    }

    private static DecoderFallbackException CaptureFallback(Action read)
    {
        try
        {
            read();
            throw new InvalidOperationException("Expected strict UTF-8 to reject the CP866 bytes.");
        }
        catch (DecoderFallbackException error)
        {
            return error;
        }
    }

    private sealed class PaddingAwareEncoding : UTF8Encoding
    {
        public override string GetString(byte[] bytes, int index, int count) => count.ToString();
    }
}
#endif
