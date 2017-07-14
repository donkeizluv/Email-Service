using System;
using System.Collections.Generic;

namespace CsvHelper.TypeConversion
{
    /// <summary>
    ///     Creates <see cref="T:CsvHelper.TypeConversion.TypeConverterOptions" />.
    /// </summary>
    public static class TypeConverterOptionsFactory
    {
        private static readonly Dictionary<Type, TypeConverterOptions> typeConverterOptions;

        private static readonly object locker;

        static TypeConverterOptionsFactory()
        {
            typeConverterOptions = new Dictionary<Type, TypeConverterOptions>();
            locker = new object();
        }

        /// <summary>
        ///     Adds the <see cref="T:CsvHelper.TypeConversion.TypeConverterOptions" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <param name="type">The type the options are for.</param>
        /// <param name="options">The options.</param>
        public static void AddOptions(Type type, TypeConverterOptions options)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (options == null)
                throw new ArgumentNullException("options");
            lock (locker)
            {
                typeConverterOptions[type] = options;
            }
        }

        /// <summary>
        ///     Adds the <see cref="T:CsvHelper.TypeConversion.TypeConverterOptions" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type the options are for.</typeparam>
        /// <param name="options">The options.</param>
        public static void AddOptions<T>(TypeConverterOptions options)
        {
            AddOptions(typeof(T), options);
        }

        /// <summary>
        ///     Get the <see cref="T:CsvHelper.TypeConversion.TypeConverterOptions" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <param name="type">The type the options are for.</param>
        /// <returns>The options for the given type.</returns>
        public static TypeConverterOptions GetOptions(Type type)
        {
            TypeConverterOptions typeConverterOption;
            TypeConverterOptions typeConverterOption1;
            if (type == null)
                throw new ArgumentNullException();
            lock (locker)
            {
                if (!typeConverterOptions.TryGetValue(type, out typeConverterOption))
                {
                    typeConverterOption = new TypeConverterOptions();
                    typeConverterOptions.Add(type, typeConverterOption);
                }
                typeConverterOption1 = typeConverterOption;
            }
            return typeConverterOption1;
        }

        /// <summary>
        ///     Get the <see cref="T:CsvHelper.TypeConversion.TypeConverterOptions" /> for the given <see cref="T:System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type the options are for.</typeparam>
        /// <returns>The options for the given type.</returns>
        public static TypeConverterOptions GetOptions<T>()
        {
            return GetOptions(typeof(T));
        }

        /// <summary>
        ///     Removes the <see cref="T:CsvHelper.TypeConversion.TypeConverterOptions" /> for the given type.
        /// </summary>
        /// <param name="type">The type to remove the options for.</param>
        public static void RemoveOptions(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            lock (locker)
            {
                typeConverterOptions.Remove(type);
            }
        }

        /// <summary>
        ///     Removes the <see cref="T:CsvHelper.TypeConversion.TypeConverterOptions" /> for the given type.
        /// </summary>
        /// <typeparam name="T">The type to remove the options for.</typeparam>
        public static void RemoveOptions<T>()
        {
            RemoveOptions(typeof(T));
        }
    }
}