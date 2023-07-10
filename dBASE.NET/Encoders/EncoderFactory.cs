using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dBASE.NET.Encoders
{
	internal class EncoderFactory
    {
        private static readonly Dictionary<DbfFieldType, IEncoder> encoders = new()
        {
            {DbfFieldType.Character, Resolve<CharacterEncoder>()},
            {DbfFieldType.Currency, Resolve<CurrencyEncoder>()},
            {DbfFieldType.Date, Resolve<DateEncoder>()},
            {DbfFieldType.DateTime, Resolve<DateTimeEncoder>()},
            {DbfFieldType.Float, Resolve<FloatEncoder>()},
            {DbfFieldType.Integer, Resolve<IntegerEncoder>()},
            {DbfFieldType.Logical, Resolve<LogicalEncoder>()},
            {DbfFieldType.Memo, Resolve<MemoEncoder>()},
            {DbfFieldType.NullFlags, Resolve<NullFlagsEncoder>()},
            {DbfFieldType.Numeric, Resolve<NumericEncoder>()},
            {DbfFieldType.Long, Resolve<LongEncoder>()},
        };

        private static readonly IEncoder UnknownEncoder = Resolve<UnknownEncoder>();


		public static IEncoder GetEncoder(DbfFieldType type, bool strict = true)
		{
			var hasValue = encoders.TryGetValue(type, out IEncoder encoder);
			if (hasValue) return encoder;
            if (strict) throw new ArgumentException("No encoder found for dBASE field type " + type);
            return UnknownEncoder;
        }

        public static T Resolve<T>() where T : IEncoder
        {
            return Activator.CreateInstance<T>();
        }

	}
}
