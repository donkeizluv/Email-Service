using System;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts a Nullable to and from a string.
    /// </summary>
    public class NullableConverter : DefaultTypeConverter
    {
        /// <summary>
        ///     Creates a new <see cref="T:CsvHelper.TypeConversion.NullableConverter" /> for the given
        ///     <see cref="T:System.Nullable`1" /> <see cref="T:System.Type" />.
        /// </summary>
        /// <param name="type">The nullable type.</param>
        /// <exception cref="T:System.ArgumentException">type is not a nullable type.</exception>
        public NullableConverter(Type type)
        {
            NullableType = type;
            UnderlyingType = Nullable.GetUnderlyingType(type);
            if (UnderlyingType == null)
                throw new ArgumentException("type is not a nullable type.");
            UnderlyingTypeConverter = TypeConverterFactory.GetConverter(UnderlyingType);
        }

        /// <summary>
        ///     Gets the type of the nullable.
        /// </summary>
        /// <value>
        ///     The type of the nullable.
        /// </value>
        public Type NullableType { get; private set; }

        /// <summary>
        ///     Gets the underlying type of the nullable.
        /// </summary>
        /// <value>
        ///     The underlying type.
        /// </value>
        public Type UnderlyingType { get; }

        /// <summary>
        ///     Gets the type converter for the underlying type.
        /// </summary>
        /// <value>
        ///     The type converter.
        /// </value>
        public ITypeConverter UnderlyingTypeConverter { get; }

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
            if (string.IsNullOrEmpty(text))
                return null;
            return UnderlyingTypeConverter.ConvertFromString(options, text);
        }

        /// <summary>
        ///     Converts the object to a string.
        /// </summary>
        /// <param name="options">The options to use when converting.</param>
        /// <param name="value">The object to convert to a string.</param>
        /// <returns>The string representation of the object.</returns>
        public override string ConvertToString(TypeConverterOptions options, object value)
        {
            return UnderlyingTypeConverter.ConvertToString(options, value);
        }
    }
}