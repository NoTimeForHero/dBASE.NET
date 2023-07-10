using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET
{
    /// <summary>
    /// This abstract classs for helping read base information
    /// </summary>
    public abstract class BaseDbf
    {
        /// <summary>
        /// Header of the file
        /// </summary>
        protected DbfHeader header;

        /// <summary>
        /// The <see cref="System.Text.Encoding" /> class that corresponds to the specified code page.
        /// Default value is <see cref="System.Text.Encoding.ASCII" />
        /// </summary>
        public Encoding Encoding { get; protected set; } = Encoding.ASCII;

        /// <summary>
        /// The collection of <see cref="DbfField" /> that represent table header.
        /// </summary>
        public IReadOnlyList<DbfField> Fields => _fields.AsReadOnly();

        /// <summary>
        /// Real collection of <see cref="DbfField" /> that represent table header.
        /// </summary>
        protected List<DbfField> _fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dbf" />.
        /// </summary>
        protected BaseDbf(Encoding encoding = null, IEnumerable<DbfField> fields = null)
        {
            header = DbfHeader.CreateHeader(DbfVersion.FoxBaseDBase3NoMemo);
            Encoding = encoding ?? Encoding;
            _fields = fields?.ToList() ?? new();
        }
    }
}
