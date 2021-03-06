using System;
using System.Globalization;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts an Int64 to and from a string.
    /// </summary>
    public class Int64Converter : DefaultTypeConverter
    {
        /// <summary>
        ///     Determines whether this instance [can convert from] the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can convert from] the specified type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvertFrom(Type type)
        {
            return type == typeof(string);
        }

        /// <summary>
        ///     Converts the string to an object.
        /// </summary>
        /// <param name="options">The options to use when converting.</param>
        /// <param name="text">The string to convert to an object.</param>
        /// <returns>The object created from the string.</returns>
        public override object ConvertFromString(TypeConverterOptions options, string text)
        {
            long num;
            var numberStyle = options.NumberStyle;
            if (long.TryParse(text, numberStyle.HasValue ? numberStyle.GetValueOrDefault() : NumberStyles.Integer,
                options.CultureInfo, out num))
                return num;
            return base.ConvertFromString(options, text);
        }
    }
}