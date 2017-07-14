using System;
using System.Reflection;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts an Enum to and from a string.
    /// </summary>
    public class EnumConverter : DefaultTypeConverter
    {
        private readonly Type type;

        /// <summary>
        ///     Creates a new <see cref="T:CsvHelper.TypeConversion.EnumConverter" /> for the given <see cref="T:System.Enum" />
        ///     <see cref="T:System.Type" />.
        /// </summary>
        /// <param name="type">The type of the Enum.</param>
        public EnumConverter(Type type)
        {
            typeof(Enum).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
            if (!typeof(Enum).IsAssignableFrom(type))
                throw new ArgumentException(string.Format("'{0}' is not an Enum.", type.FullName));
            this.type = type;
        }

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
            object obj;
            try
            {
                obj = Enum.Parse(type, text, true);
            }
            catch
            {
                obj = base.ConvertFromString(options, text);
            }
            return obj;
        }
    }
}