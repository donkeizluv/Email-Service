using System.Reflection;

namespace CsvHelper.Configuration
{
    /// <summary>
    ///     The configuration data for the reference map.
    /// </summary>
    public class CsvPropertyReferenceMapData
    {
        private PropertyInfo JustDecompileGenerated_Property_k__BackingField;
        private string prefix;

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:CsvHelper.Configuration.CsvPropertyReferenceMapData" /> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="mapping">The mapping this is a reference for.</param>
        public CsvPropertyReferenceMapData(PropertyInfo property, CsvClassMap mapping)
        {
            Property = property;
            Mapping = mapping;
        }

        /// <summary>
        ///     Gets the mapping this is a reference for.
        /// </summary>
        public CsvClassMap Mapping { get; }

        /// <summary>
        ///     Gets or sets the header prefix to use.
        /// </summary>
        public virtual string Prefix
        {
            get { return prefix; }
            set
            {
                prefix = value;
                foreach (var propertyMap in Mapping.PropertyMaps)
                    propertyMap.Data.Names.Prefix = value;
            }
        }

        /// <summary>
        ///     Gets the <see cref="T:System.Reflection.PropertyInfo" /> that the data
        ///     is associated with.
        /// </summary>
        public PropertyInfo Property
        {
            get { return JustDecompileGenerated_get_Property(); }
            set { JustDecompileGenerated_set_Property(value); }
        }

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