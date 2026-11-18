using System;
using System.Collections.Generic;
using System.IO;

namespace dBASE.NET
{
    internal static class DbfRecordReader
    {
        private const int BlockSize = 64 * 1024;
        private const byte EndMarker = 0x1a;

        // Each range is valid only until this enumerator advances to the next block.
        internal static IEnumerable<ArraySegment<byte>> Read(Stream stream, DbfHeader header, int? recordCount = null)
        {
            if (recordCount <= 0)
                yield break;

            int recordLength = header.RecordLength;
            if (recordLength == 0)
                throw new InvalidDataException("The DBF record length is zero.");

            int recordsPerBlock = BlockSize / recordLength;
            long fileOffset = header.HeaderLength;
            if (!recordCount.HasValue && (stream is FileStream || stream is MemoryStream))
            {
                long available = stream.Length - fileOffset;
                if (available <= 0)
                    yield break;
                if (available < recordsPerBlock * recordLength)
                    recordsPerBlock = (int)((available + recordLength - 1) / recordLength);
            }

            int remaining = recordCount ?? int.MaxValue;
            var buffer = new byte[Math.Min(recordsPerBlock, remaining) * recordLength];

            while (remaining > 0)
            {
                int recordsToRead = Math.Min(recordsPerBlock, remaining);
                int bytesToRead = recordsToRead * recordLength;
                // An indexed read may have moved the stream since the previous block.
                stream.Seek(fileOffset, SeekOrigin.Begin);
                int bytesRead = FillBlock(stream, buffer, bytesToRead);
                if (recordCount.HasValue && bytesRead < bytesToRead)
                    throw new EndOfStreamException("The DBF stream ended before all physical records were read.");

                for (int offset = 0; offset < bytesRead; offset += recordLength)
                {
                    if (buffer[offset] == EndMarker)
                        yield break;

                    int length = Math.Min(recordLength, bytesRead - offset);
                    yield return new ArraySegment<byte>(buffer, offset, length);
                }

                if (bytesRead < bytesToRead)
                    yield break;

                fileOffset += bytesRead;
                remaining -= recordsToRead;
            }
        }

        private static int FillBlock(Stream stream, byte[] buffer, int length)
        {
#if NET8_0_OR_GREATER
            return stream.ReadAtLeast(buffer.AsSpan(0, length), length, throwOnEndOfStream: false);
#else
            int bytesRead = 0;
            while (bytesRead < length)
            {
                int read = stream.Read(buffer, bytesRead, length - bytesRead);
                if (read == 0)
                    break;
                bytesRead += read;
            }

            return bytesRead;
#endif
        }
    }
}
