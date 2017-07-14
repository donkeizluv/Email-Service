using System;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts objects to and from strings.
    /// </summary>
    public interface ITypeConverter
    {
        /// <summary>
        ///     Determines whether this instance [can convert from] the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can convert from] the specified type; otherwise, <c>false</c>.
        /// </returns>
        bool CanConvertFrom(Type type);

        /// <summary>
        ///     Determines whether this instance [can convert to] the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can convert to] the specified type; otherwise, <c>false</c>.
        /// </returns>
        bool CanConvertTo(Type type);

        /// <summary>
        ///     Converts the string to an object.
        /// </summary>
        /// <param name="options">The options to use when converting.</param>
        /// <param name="text">The string to convert to an object.</param>
        /// <returns>The object created from the string.</returns>
        object ConvertFromString(TypeConverterOptions options, string text);

        /// <summary>
        ///     Converts the object to a string.
        /// </summary>
        /// <param name="options">The options to use when converting.</param>
        /// <param name="value">The object to convert to a string.</param>
        /// <returns>The string representation of the object.</returns>
        string ConvertToString(TypeConverterOptions options, object value);
    }
}