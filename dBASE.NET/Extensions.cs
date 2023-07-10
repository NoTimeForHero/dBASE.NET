using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dBASE.NET.Memo;

namespace dBASE.NET
{
    public static class Extensions
    {
        /// <summary>
        /// Opens a DBF file, reads the contents that initialize the current instance, and then closes the file.
        /// </summary>
        /// <param name="dbf">Target DBF instance</param>
        /// <param name="path">The file to read.</param>
        public static void Read(this Dbf dbf, string path)
        {
            // Open stream for reading.
            using FileStream baseStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            string memoPath = GetMemoPath(path, out var type);
            if (memoPath == null)
            {
                dbf.Read(baseStream, null);
                return;
            }
            using var memoStream = File.Open(memoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var memoryMemoStream = new MemoryStream();
            memoStream.CopyTo(memoryMemoStream);
            dbf.Read(baseStream, memoryMemoStream, type);
        }

        /// <summary>
        /// Creates a new file, writes the current instance to the file, and then closes the file. If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="dbf">Target DBF instance</param>
        /// <param name="path">The output path.</param>
        /// <param name="version">The version <see cref="DbfVersion" />. If unknown specified, use current header version.</param>
        /// <param name="packRecords">Remove all records that were marked as deleted</param>
        public static void Write(this Dbf dbf, string path, DbfVersion version = DbfVersion.Unknown, bool packRecords = false)
        {
            using var stream = File.Open(path, FileMode.Create, FileAccess.Write);

            if (dbf.MemoType == MemoFileType.None)
            {
                dbf.Write(stream, version, packRecords);
                return;
            }

            var extension = dbf.MemoType.ToString().ToLower();
            var memoPath = Path.ChangeExtension(path, extension);
            using var memoStream = File.Open(memoPath, FileMode.Create, FileAccess.Write);
            dbf.Write(stream, version, packRecords, memoStream: memoStream);
        }

        private static string GetMemoPath(string basePath, out MemoFileType type)
        {
            string path;

            path = Path.ChangeExtension(basePath, "fpt");
            if (File.Exists(path))
            {
                type = MemoFileType.FPT;
                return path;
            }

            path = Path.ChangeExtension(basePath, "dbt");
            if (File.Exists(path))
            {
                type = MemoFileType.DBT;
                return path;
            }

            type = MemoFileType.None;
            return null;
        }

    }
}
