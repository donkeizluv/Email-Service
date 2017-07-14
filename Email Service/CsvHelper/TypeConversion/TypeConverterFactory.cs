using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Creates <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />s.
    /// </summary>
    public static class TypeConverterFactory
    {
        private static readonly Dictionary<Type, ITypeConverter> typeConverters;

        private static readonly object locker;

        /// <summary>
        ///     Initializes the <see cref="T:CsvHelper.TypeConversion.TypeConverterFactory" /> class.
        /// </summary>
        static TypeConverterFactory()
        {
            typeConverters = new Dictionary<Type, ITypeConverter>();
            locker = new object();
            CreateDefaultConverters();
        }

        /// <summary>
        ///     Adds the <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <param name="type">The type the converter converts.</param>
        /// <param name="typeConverter">The type converter that converts the type.</param>
        public static void AddConverter(Type type, ITypeConverter typeConverter)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");
            lock (locker)
            {
                typeConverters[type] = typeConverter;
            }
        }

        /// <summary>
        ///     Adds the <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type the converter converts.</typeparam>
        /// <param name="typeConverter">The type converter that converts the type.</param>
        public static void AddConverter<T>(ITypeConverter typeConverter)
        {
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");
            lock (locker)
            {
                typeConverters[typeof(T)] = typeConverter;
            }
        }

        private static void CreateDefaultConverters()
        {
            AddConverter(typeof(bool), new BooleanConverter());
            AddConverter(typeof(byte), new ByteConverter());
            AddConverter(typeof(char), new CharConverter());
            AddConverter(typeof(DateTime), new DateTimeConverter());
            AddConverter(typeof(DateTimeOffset), new DateTimeOffsetConverter());
            AddConverter(typeof(decimal), new DecimalConverter());
            AddConverter(typeof(double), new DoubleConverter());
            AddConverter(typeof(float), new SingleConverter());
            AddConverter(typeof(Guid), new GuidConverter());
            AddConverter(typeof(short), new Int16Converter());
            AddConverter(typeof(int), new Int32Converter());
            AddConverter(typeof(long), new Int64Converter());
            AddConverter(typeof(sbyte), new SByteConverter());
            AddConverter(typeof(string), new StringConverter());
            AddConverter(typeof(TimeSpan), new TimeSpanConverter());
            AddConverter(typeof(ushort), new UInt16Converter());
            AddConverter(typeof(uint), new UInt32Converter());
            AddConverter(typeof(ulong), new UInt64Converter());
            AddConverter(typeof(IEnumerable), new EnumerableConverter());
        }

        /// <summary>
        ///     Gets the converter for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <param name="type">The type to get the converter for.</param>
        /// <returns>The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> for the given <see cref="T:System.Type" />.</returns>
        public static ITypeConverter GetConverter(Type type)
        {
            ITypeConverter typeConverter;
            ITypeConverter typeConverter1;
            if (type == null)
                throw new ArgumentNullException("type");
            lock (locker)
            {
                if (!typeConverters.TryGetValue(type, out typeConverter))
                {
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                        return GetConverter(typeof(IEnumerable));
                    if (typeof(Enum).IsAssignableFrom(type))
                    {
                        AddConverter(type, new EnumConverter(type));
                        return GetConverter(type);
                    }
                    if (!type.GetTypeInfo().IsGenericType || !(type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        return new DefaultTypeConverter();
                    AddConverter(type, new NullableConverter(type));
                    return GetConverter(type);
                }
                typeConverter1 = typeConverter;
            }
            return typeConverter1;
        }

        /// <summary>
        ///     Gets the converter for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type to get the converter for.</typeparam>
        /// <returns>The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> for the given <see cref="T:System.Type" />.</returns>
        public static ITypeConverter GetConverter<T>()
        {
            return GetConverter(typeof(T));
        }

        /// <summary>
        ///     Removes the <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <param name="type">The type to remove the converter for.</param>
        public static void RemoveConverter(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            lock (locker)
            {
                typeConverters.Remove(type);
            }
        }

        /// <summary>
        ///     Removes the <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type to remove the converter for.</typeparam>
        public static void RemoveConverter<T>()
        {
            RemoveConverter(typeof(T));
        }
    }
}