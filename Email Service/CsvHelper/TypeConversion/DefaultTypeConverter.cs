using System;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts an object to and from a string.
    /// </summary>
    public class DefaultTypeConverter : ITypeConverter
    {
        /// <summary>
        ///     Determines whether this instance [can convert from] the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can convert from] the specified type; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanConvertFrom(Type type)
        {
            return false;
        }

        /// <summary>
        ///     Determines whether this instance [can convert to] the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can convert to] the specified type; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanConvertTo(Type type)
        {
            return type == typeof(string);
        }

        /// <summary>
        ///     Converts the string to an object.
        /// </summary>
        /// <param name="options">The options to use when converting.</param>
        /// <param name="text">The string to convert to an object.</param>
        /// <returns>The object created from the string.</returns>
        public virtual object ConvertFromString(TypeConverterOptions options, string text)
        {
            throw new CsvTypeConverterException("The conversion cannot be performed.");
        }

        /// <summary>
        ///     Converts the object to a string.
        /// </summary>
        /// <param name="options">The options to use when converting.</param>
        /// <param name="value">The object to convert to a string.</param>
        /// <returns>The string representation of the object.</returns>
        public virtual string ConvertToString(TypeConverterOptions options, object value)
        {
            if (value == null)
                return string.Empty;
            var formattable = value as IFormattable;
            if (formattable == null)
                return value.ToString();
            return formattable.ToString(options.Format, options.CultureInfo);
        }
    }
}