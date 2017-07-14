using System;
using System.Globalization;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts a TimeSpan to and from a string.
    /// </summary>
    public class TimeSpanConverter : DefaultTypeConverter
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
            TimeSpan timeSpan;
            IFormatProvider cultureInfo = options.CultureInfo;
            var timeSpanStyle = options.TimeSpanStyle;
            var timeSpanStyle1 = timeSpanStyle.HasValue ? timeSpanStyle.GetValueOrDefault() : TimeSpanStyles.None;
            if (!string.IsNullOrEmpty(options.Format) &&
                TimeSpan.TryParseExact(text, options.Format, cultureInfo, timeSpanStyle1, out timeSpan))
                return timeSpan;
            if (string.IsNullOrEmpty(options.Format) && TimeSpan.TryParse(text, cultureInfo, out timeSpan))
                return timeSpan;
            return base.ConvertFromString(options, text);
        }
    }
}