using System;
using System.Reflection;

namespace CsvHelper.Configuration
{
    /// <summary>
    ///     Mapping info for a reference property mapping to a class.
    /// </summary>
    public class CsvPropertyReferenceMap
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:CsvHelper.Configuration.CsvPropertyReferenceMap" /> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="mapping">The <see cref="T:CsvHelper.Configuration.CsvClassMap" /> to use for the reference map.</param>
        public CsvPropertyReferenceMap(PropertyInfo property, CsvClassMap mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException("mapping");
            Data = new CsvPropertyReferenceMapData(property, mapping);
        }

        /// <summary>
        ///     Gets the property reference map data.
        /// </summary>
        public CsvPropertyReferenceMapData Data { get; }

        /// <summary>
        ///     Gets the mapping.
        /// </summary>
        [Obsolete("This property is deprecated and will be removed in the next major release. Use Data.Mapping instead.",
            false)]
        public CsvClassMap Mapping
        {
            get { return Data.Mapping; }
        }

        /// <summary>
        ///     Gets the property.
        /// </summary>
        [Obsolete(
            "This property is deprecated and will be removed in the next major release. Use Data.Property instead.",
            false)]
        public PropertyInfo Property
        {
            get { return Data.Property; }
        }

        /// <summary>
        ///     Get the largest index for the
        ///     properties and references.
        /// </summary>
        /// <returns>The max index.</returns>
        internal int GetMaxIndex()
        {
            return Data.Mapping.GetMaxIndex();
        }

        /// <summary>
        ///     Appends a prefix to the header of each field of the reference property
        /// </summary>
        /// <param name="prefix">The prefix to be prepended to headers of each reference property</param>
        /// <returns>The current <see cref="T:CsvHelper.Configuration.CsvPropertyReferenceMap" /></returns>
        public CsvPropertyReferenceMap Prefix(string prefix = null)
        {
            if (string.IsNullOrEmpty(prefix))
                prefix = string.Concat(Data.Property.Name, ".");
            Data.Prefix = prefix;
            return this;
        }
    }
}