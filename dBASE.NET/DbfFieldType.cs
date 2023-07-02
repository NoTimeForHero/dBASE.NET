using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CS1591

namespace dBASE.NET
{
	/// <summary>
	/// Not all types are currently implemented.
	/// </summary>
	public enum DbfFieldType
	{
		Character = 'C',
		Currency = 'Y',
		Numeric = 'N',
		Float = 'F',
		Date = 'D',
		DateTime = 'T',
		Double = 'B',
		Integer = 'I',
		Logical = 'L',
		Memo = 'M',
		General = 'G',
		Picture = 'P',
		NullFlags = '0',
        Long=10001, // TODO: Change name for normal?
    }

    class DbfFiledTypeParser
    {
        public static DbfFieldType Parse(byte input)
        {
			// [x]Harbour DBase (like Advantage Database Server?)
            if (input == '+') return DbfFieldType.Integer; // Auto-Increment ID
            if (input == '@') return DbfFieldType.Long; // Unix Timestamp
            if (input == '^') return DbfFieldType.Long; // Record modification count

            return (DbfFieldType)input;
        }
    }

}
