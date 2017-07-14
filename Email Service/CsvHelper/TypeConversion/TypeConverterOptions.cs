using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Options used when doing type conversion.
    /// </summary>
    public class TypeConverterOptions
    {
        /// <summary>
        ///     Gets the list of values that can be
        ///     used to represent a boolean of false.
        /// </summary>
        public List<string> BooleanFalseValues { get; } = new List<string>
        {
            "no",
            "n"
        };

        /// <summary>
        ///     Gets the list of values that can be
        ///     used to represent a boolean of true.
        /// </summary>
        public List<string> BooleanTrueValues { get; } = new List<string>
        {
            "yes",
            "y"
        };

        /// <summary>
        ///     Gets or sets the culture info.
        /// </summary>
        public CultureInfo CultureInfo { get; set; }

        /// <summary>
        ///     Gets or sets the date time style.
        /// </summary>
        public DateTimeStyles? DateTimeStyle { get; set; }

        /// <summary>
        ///     Gets or sets the string format.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        ///     Gets or sets the number style.
        /// </summary>
        public NumberStyles? NumberStyle { get; set; }

        /// <summary>
        ///     Gets or sets the time span style.
        /// </summary>
        public TimeSpanStyles? TimeSpanStyle { get; set; }

        /// <summary>
        ///     Merges TypeConverterOptions by applying the values of sources in order to a
        ///     new TypeConverterOptions instance.
        /// </summary>
        /// <param name="sources">The sources that will be applied.</param>
        /// <returns>A new instance of TypeConverterOptions with the source applied to it.</returns>
        public static TypeConverterOptions Merge(params TypeConverterOptions[] sources)
        {
            var typeConverterOption = new TypeConverterOptions();
            var typeConverterOptionsArray = sources;
            for (int i = 0; i < typeConverterOptionsArray.Length; i++)
            {
                var typeConverterOption1 = typeConverterOptionsArray[i];
                if (typeConverterOption1 != null)
                {
                    if (typeConverterOption1.CultureInfo != null)
                        typeConverterOption.CultureInfo = typeConverterOption1.CultureInfo;
                    if (typeConverterOption1.DateTimeStyle.HasValue)
                        typeConverterOption.DateTimeStyle = typeConverterOption1.DateTimeStyle;
                    if (typeConverterOption1.TimeSpanStyle.HasValue)
                        typeConverterOption.TimeSpanStyle = typeConverterOption1.TimeSpanStyle;
                    if (typeConverterOption1.NumberStyle.HasValue)
                        typeConverterOption.NumberStyle = typeConverterOption1.NumberStyle;
                    if (typeConverterOption1.Format != null)
                        typeConverterOption.Format = typeConverterOption1.Format;
                    if (!typeConverterOption.BooleanTrueValues.SequenceEqual(typeConverterOption1.BooleanTrueValues))
                    {
                        typeConverterOption.BooleanTrueValues.Clear();
                        typeConverterOption.BooleanTrueValues.AddRange(typeConverterOption1.BooleanTrueValues);
                    }
                    if (!typeConverterOption.BooleanFalseValues.SequenceEqual(typeConverterOption1.BooleanFalseValues))
                    {
                        typeConverterOption.BooleanFalseValues.Clear();
                        typeConverterOption.BooleanFalseValues.AddRange(typeConverterOption1.BooleanFalseValues);
                    }
                }
            }
            return typeConverterOption;
        }
    }
}