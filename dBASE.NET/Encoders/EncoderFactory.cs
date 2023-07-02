using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
	internal class EncoderFactory
	{
		public static IEncoder GetEncoder(DbfFieldType type, bool strict = true)
		{
			switch (type)
			{
				case DbfFieldType.Character:
					return CharacterEncoder.Instance;
				case DbfFieldType.Currency:
					return CurrencyEncoder.Instance;
				case DbfFieldType.Date:
					return DateEncoder.Instance;
				case DbfFieldType.DateTime:
					return DateTimeEncoder.Instance;
				case DbfFieldType.Float:
					return FloatEncoder.Instance;
				case DbfFieldType.Integer:
					return IntegerEncoder.Instance;
				case DbfFieldType.Logical:
					return LogicalEncoder.Instance;
				case DbfFieldType.Memo:
					return MemoEncoder.Instance;
				case DbfFieldType.NullFlags:
					return NullFlagsEncoder.Instance;
				case DbfFieldType.Numeric:
					return NumericEncoder.Instance;
				case DbfFieldType.Long:
                    return LongEncoder.Instance;
				default:
					if (strict) throw new ArgumentException("No encoder found for dBASE field type " + type);
                    return UnknownEncoder.Instance;
			}
		}

	}
}
