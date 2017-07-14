using System.Linq.Expressions;
using System.Reflection;
using CsvHelper.TypeConversion;

namespace CsvHelper.Configuration
{
    /// <summary>
    ///     The configured data for the property map.
    /// </summary>
    public class CsvPropertyMapData
    {
        private object defaultValue;

        private PropertyInfo JustDecompileGenerated_Property_k__BackingField;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:CsvHelper.Configuration.CsvPropertyMapData" /> class.
        /// </summary>
        /// <param name="property">The property.</param>
        public CsvPropertyMapData(PropertyInfo property)
        {
            Property = property;
        }

        /// <summary>
        ///     Gets or sets the expression used to convert data in the
        ///     row to the property.
        /// </summary>
        public virtual Expression ConvertExpression { get; set; }

        /// <summary>
        ///     Gets or sets the default value used when a CSV field is empty.
        /// </summary>
        public virtual object Default
        {
            get { return defaultValue; }
            set
            {
                defaultValue = value;
                IsDefaultSet = true;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the field should be ignored.
        /// </summary>
        public virtual bool Ignore { get; set; }

        /// <summary>
        ///     Gets or sets the column index.
        /// </summary>
        public virtual int Index { get; set; } = -1;

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is default value set.
        ///     the default value was explicitly set. True if it was
        ///     explicitly set, otherwise false.
        /// </summary>
        public virtual bool IsDefaultSet { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating if the index was
        ///     explicitly set. True if it was explicitly set,
        ///     otherwise false.
        /// </summary>
        public virtual bool IsIndexSet { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating if the name was
        ///     explicitly set. True if it was explicity set,
        ///     otherwise false.
        /// </summary>
        public virtual bool IsNameSet { get; set; }

        /// <summary>
        ///     Gets or sets the index of the name.
        ///     This is used if there are multiple
        ///     columns with the same names.
        /// </summary>
        public virtual int NameIndex { get; set; }

        /// <summary>
        ///     Gets the list of column names.
        /// </summary>
        public virtual CsvPropertyNameCollection Names { get; } = new CsvPropertyNameCollection();

        /// <summary>
        ///     Gets the <see cref="T:System.Reflection.PropertyInfo" /> that the data
        ///     is associated with.
        /// </summary>
        public PropertyInfo Property
        {
            get { return JustDecompileGenerated_get_Property(); }
            set { JustDecompileGenerated_set_Property(value); }
        }

        /// <summary>
        ///     Gets or sets the type converter.
        /// </summary>
        public virtual ITypeConverter TypeConverter { get; set; }

        /// <summary>
        ///     Gets or sets the type converter options.
        /// </summary>
        public virtual TypeConverterOptions TypeConverterOptions { get; } = new TypeConverterOptions();

        public virtual PropertyInfo JustDecompileGenerated_get_Property()
        {
            return JustDecompileGenerated_Property_k__BackingField;
        }

        private void JustDecompileGenerated_set_Property(PropertyInfo value)
        {
            JustDecompileGenerated_Property_k__BackingField = value;
        }
    }
}