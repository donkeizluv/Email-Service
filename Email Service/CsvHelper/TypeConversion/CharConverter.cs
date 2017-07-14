using System;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts a Char to and from a string.
    /// </summary>
    public class CharConverter : DefaultTypeConverter
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
            char chr;
            if (text != null && text.Length > 1)
                text = text.Trim();
            if (char.TryParse(text, out chr))
                return chr;
            return base.ConvertFromString(options, text);
        }
    }
}