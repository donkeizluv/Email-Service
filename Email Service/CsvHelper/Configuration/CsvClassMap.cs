using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CsvHelper.TypeConversion;

namespace CsvHelper.Configuration
{
    /// <summary>
    ///     Maps class properties to CSV fields.
    /// </summary>
    public abstract class CsvClassMap
    {
        /// <summary>
        ///     Allow only internal creation of CsvClassMap.
        /// </summary>
        internal CsvClassMap()
        {
        }

        /// <summary>
        ///     Gets the constructor expression.
        /// </summary>
        public virtual NewExpression Constructor { get; protected set; }

        /// <summary>
        ///     The class property mappings.
        /// </summary>
        public virtual CsvPropertyMapCollection PropertyMaps { get; } = new CsvPropertyMapCollection();

        /// <summary>
        ///     The class property reference mappings.
        /// </summary>
        public virtual List<CsvPropertyReferenceMap> ReferenceMaps { get; } = new List<CsvPropertyReferenceMap>();

        /// <summary>
        ///     Auto maps all properties for the given type. If a property
        ///     is mapped again it will override the existing map.
        /// </summary>
        /// <param name="ignoreReferences">
        ///     A value indicating if references should be ignored when auto mapping.
        ///     True to ignore references, otherwise false.
        /// </param>
        /// <param name="prefixReferenceHeaders">
        ///     A value indicating if headers of reference properties should
        ///     get prefixed by the parent property name.
        ///     True to prefix, otherwise false.
        /// </param>
        public virtual void AutoMap(bool ignoreReferences = false, bool prefixReferenceHeaders = false)
        {
            AutoMapInternal(this, ignoreReferences, prefixReferenceHeaders, new LinkedList<Type>(), 0);
        }

        /// <summary>
        ///     Auto maps the given map and checks for circular references as it goes.
        /// </summary>
        /// <param name="map">The map to auto map.</param>
        /// <param name="ignoreReferences">
        ///     A value indicating if references should be ignored when auto mapping.
        ///     True to ignore references, otherwise false.
        /// </param>
        /// <param name="prefixReferenceHeaders">
        ///     A value indicating if headers of reference properties should
        ///     get prefixed by the parent property name.
        ///     True to prefix, otherwise false.
        /// </param>
        /// <param name="mapParents">The list of parents for the map.</param>
        internal static void AutoMapInternal(CsvClassMap map, bool ignoreReferences, bool prefixReferenceHeaders,
            LinkedList<Type> mapParents, int indexStart = 0)
        {
            var genericArguments = map.GetType().GetTypeInfo().BaseType.GetGenericArguments()[0];
            if (typeof(IEnumerable).IsAssignableFrom(genericArguments))
                throw new CsvConfigurationException(
                    "Types that inherit IEnumerable cannot be auto mapped. Did you accidentally call GetRecord or WriteRecord which acts on a single record instead of calling GetRecords or WriteRecords which acts on a list of records?");
            var properties = genericArguments.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < properties.Length; i++)
            {
                var propertyInfo = properties[i];
                var type = TypeConverterFactory.GetConverter(propertyInfo.PropertyType).GetType();
                if (type != typeof(EnumerableConverter))
                {
                    bool flag = type == typeof(DefaultTypeConverter);
                    if (!(flag & (propertyInfo.PropertyType.GetConstructor(new Type[0]) != null)))
                    {
                        var csvPropertyMap = new CsvPropertyMap(propertyInfo);
                        csvPropertyMap.Data.Index = map.GetMaxIndex() + 1;
                        if (csvPropertyMap.Data.TypeConverter.CanConvertFrom(typeof(string)) ||
                            csvPropertyMap.Data.TypeConverter.CanConvertTo(typeof(string)) && !flag)
                            map.PropertyMaps.Add(csvPropertyMap);
                    }
                    else if (!ignoreReferences && !CheckForCircularReference(propertyInfo.PropertyType, mapParents))
                    {
                        mapParents.AddLast(genericArguments);
                        var csvClassMap =
                            (CsvClassMap)
                            ReflectionHelper.CreateInstance(
                                typeof(DefaultCsvClassMap<>).MakeGenericType(propertyInfo.PropertyType));
                        AutoMapInternal(csvClassMap, false, prefixReferenceHeaders, mapParents, map.GetMaxIndex() + 1);
                        if (csvClassMap.PropertyMaps.Count > 0 || csvClassMap.ReferenceMaps.Count > 0)
                        {
                            var csvPropertyReferenceMap = new CsvPropertyReferenceMap(propertyInfo, csvClassMap);
                            if (prefixReferenceHeaders)
                                csvPropertyReferenceMap.Prefix(null);
                            map.ReferenceMaps.Add(csvPropertyReferenceMap);
                        }
                    }
                }
            }
            map.ReIndex(indexStart);
        }

        /// <summary>
        ///     Checks for circular references.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <param name="mapParents">The list of parents to check against.</param>
        /// <returns>
        ///     A value indicating if a circular reference was found.
        ///     True if a circular reference was found, otherwise false.
        /// </returns>
        internal static bool CheckForCircularReference(Type type, LinkedList<Type> mapParents)
        {
            if (mapParents.Count == 0)
                return false;
            var last = mapParents.Last;
            do
            {
                if (last.Value == type)
                    return true;
                last = last.Previous;
            } while (last != null);
            return false;
        }

        /// <summary>
        ///     Called to create the mappings.
        /// </summary>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Specify your mappings in the constructor instead.",
            false)]
        public virtual void CreateMap()
        {
        }

        /// <summary>
        ///     Get the largest index for the
        ///     properties and references.
        /// </summary>
        /// <returns>The max index.</returns>
        internal int GetMaxIndex()
        {
            if (PropertyMaps.Count == 0 && ReferenceMaps.Count == 0)
                return -1;
            var nums = new List<int>();
            if (PropertyMaps.Count > 0)
                nums.Add(PropertyMaps.Max(pm => pm.Data.Index));
            nums.AddRange(
                from referenceMap in ReferenceMaps
                select referenceMap.GetMaxIndex());
            return nums.Max();
        }

        /// <summary>
        ///     Gets the property map for the given property expression.
        /// </summary>
        /// <typeparam name="T">The type of the class the property belongs to.</typeparam>
        /// <param name="expression">The property expression.</param>
        /// <returns>The CsvPropertyMap for the given expression.</returns>
        [Obsolete("This method is deprecated and will be removed in the next major release.", false)]
        public virtual CsvPropertyMap PropertyMap<T>(Expression<Func<T, object>> expression)
        {
            var property = ReflectionHelper.GetProperty(expression);
            var csvPropertyMap = PropertyMaps.SingleOrDefault(m =>
            {
                if (m.Data.Property == property)
                    return true;
                if (m.Data.Property.Name != property.Name)
                    return false;
                if (m.Data.Property.DeclaringType.IsAssignableFrom(property.DeclaringType))
                    return true;
                return property.DeclaringType.IsAssignableFrom(m.Data.Property.DeclaringType);
            });
            if (csvPropertyMap != null)
                return csvPropertyMap;
            var maxIndex = new CsvPropertyMap(property);
            maxIndex.Data.Index = GetMaxIndex() + 1;
            PropertyMaps.Add(maxIndex);
            return maxIndex;
        }

        /// <summary>
        ///     Resets the indexes based on the given start index.
        /// </summary>
        /// <param name="indexStart">The index start.</param>
        /// <returns>The last index + 1.</returns>
        internal int ReIndex(int indexStart = 0)
        {
            foreach (var propertyMap in PropertyMaps)
            {
                if (propertyMap.Data.IsIndexSet)
                    continue;
                propertyMap.Data.Index = indexStart + propertyMap.Data.Index;
            }
            foreach (var referenceMap in ReferenceMaps)
                indexStart = referenceMap.Data.Mapping.ReIndex(indexStart);
            return indexStart;
        }
    }
}