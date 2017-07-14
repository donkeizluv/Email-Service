using System;
using System.Globalization;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts a DateTimeOffset to and from a string.
    /// </summary>
    public class DateTimeOffsetConverter : DefaultTypeConverter
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
            if (text == null)
                return base.ConvertFromString(options, null);
            if (text.Trim().Length == 0)
                return DateTimeOffset.MinValue;
            object format = (IFormatProvider) options.CultureInfo.GetFormat(typeof(DateTimeFormatInfo));
            if (format == null)
                format = options.CultureInfo;
            var formatProvider = (IFormatProvider) format;
            var dateTimeStyle = options.DateTimeStyle;
            var dateTimeStyle1 = dateTimeStyle.HasValue ? dateTimeStyle.GetValueOrDefault() : DateTimeStyles.None;
            return string.IsNullOrEmpty(options.Format)
                ? DateTimeOffset.Parse(text, formatProvider, dateTimeStyle1)
                : DateTimeOffset.ParseExact(text, options.Format, formatProvider, dateTimeStyle1);
        }
    }
}