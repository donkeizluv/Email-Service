using System;
using System.Globalization;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Converts a Boolean to and from a string.
    /// </summary>
    public class BooleanConverter : DefaultTypeConverter
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
            bool flag;
            short num;
            object obj;
            if (bool.TryParse(text, out flag))
                return flag;
            if (short.TryParse(text, out num))
            {
                if (num == 0)
                    return false;
                if (num == 1)
                    return true;
            }
            string str = (text ?? string.Empty).Trim();
            foreach (string booleanTrueValue in options.BooleanTrueValues)
            {
                if (options.CultureInfo.CompareInfo.Compare(booleanTrueValue, str, CompareOptions.IgnoreCase) != 0)
                    continue;
                obj = true;
                return obj;
            }
            var enumerator = options.BooleanFalseValues.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    string current = enumerator.Current;
                    if (options.CultureInfo.CompareInfo.Compare(current, str, CompareOptions.IgnoreCase) != 0)
                        continue;
                    obj = false;
                    return obj;
                }
                return base.ConvertFromString(options, text);
            }
            finally
            {
                ((IDisposable) enumerator).Dispose();
            }
            return obj;
        }
    }
}