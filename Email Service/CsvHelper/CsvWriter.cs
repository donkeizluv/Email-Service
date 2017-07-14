using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CsvHelper
{
    /// <summary>
    ///     Used to write CSV files.
    /// </summary>
    public class CsvWriter : ICsvWriter, IDisposable
    {
        private readonly CsvConfiguration configuration;

        private readonly List<string> currentRecord = new List<string>();

        private readonly Dictionary<Type, Delegate> typeActions = new Dictionary<Type, Delegate>();
        private bool disposed;

        private bool hasExcelSeperatorBeenRead;

        private bool hasHeaderBeenWritten;

        private bool hasRecordBeenWritten;

        private ICsvSerializer serializer;

        /// <summary>
        ///     Creates a new CSV writer using the given <see cref="T:System.IO.TextWriter" />,
        ///     a default <see cref="T:CsvHelper.Configuration.CsvConfiguration" /> and <see cref="T:CsvHelper.CsvSerializer" />
        ///     as the default serializer.
        /// </summary>
        /// <param name="writer">The writer used to write the CSV file.</param>
        public CsvWriter(TextWriter writer) : this(writer, new CsvConfiguration())
        {
        }

        /// <summary>
        ///     Creates a new CSV writer using the given <see cref="T:System.IO.TextWriter" />
        ///     and <see cref="T:CsvHelper.Configuration.CsvConfiguration" /> and <see cref="T:CsvHelper.CsvSerializer" />
        ///     as the default serializer.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.IO.StreamWriter" /> use to write the CSV file.</param>
        /// <param name="configuration">The configuration.</param>
        public CsvWriter(TextWriter writer, CsvConfiguration configuration)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            this.configuration = configuration;
            serializer = new CsvSerializer(writer, configuration);
        }

        /// <summary>
        ///     Creates a new CSV writer using the given <see cref="T:CsvHelper.ICsvSerializer" />.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        public CsvWriter(ICsvSerializer serializer)
        {
            if (serializer == null)
                throw new ArgumentNullException("serializer");
            if (serializer.Configuration == null)
                throw new CsvConfigurationException("The given serializer has no configuration.");
            this.serializer = serializer;
            configuration = serializer.Configuration;
        }

        /// <summary>
        ///     Gets the configuration.
        /// </summary>
        public virtual CsvConfiguration Configuration
        {
            get { return configuration; }
        }

        /// <summary>
        ///     Clears the record cache for the given type. After <see cref="M:CsvHelper.ICsvWriter.WriteRecord``1(``0)" /> is
        ///     called the
        ///     first time, code is dynamically generated based on the
        ///     <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />,
        ///     compiled, and stored for the given type T. If the <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />
        ///     changes, <see cref="M:CsvHelper.ICsvWriter.ClearRecordCache``1" /> needs to be called to update the
        ///     record cache.
        /// </summary>
        /// <typeparam name="T">The record type.</typeparam>
        public virtual void ClearRecordCache<T>()
        {
            CheckDisposed();
            ClearRecordCache(typeof(T));
        }

        /// <summary>
        ///     Clears the record cache for the given type. After <see cref="M:CsvHelper.ICsvWriter.WriteRecord``1(``0)" /> is
        ///     called the
        ///     first time, code is dynamically generated based on the
        ///     <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />,
        ///     compiled, and stored for the given type T. If the <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />
        ///     changes, <see cref="M:CsvHelper.ICsvWriter.ClearRecordCache(System.Type)" /> needs to be called to update the
        ///     record cache.
        /// </summary>
        /// <param name="type">The record type.</param>
        public virtual void ClearRecordCache(Type type)
        {
            CheckDisposed();
            typeActions.Remove(type);
        }

        /// <summary>
        ///     Clears the record cache for all types. After <see cref="M:CsvHelper.ICsvWriter.WriteRecord``1(``0)" /> is called
        ///     the
        ///     first time, code is dynamically generated based on the
        ///     <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />,
        ///     compiled, and stored for the given type T. If the <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />
        ///     changes, <see cref="M:CsvHelper.ICsvWriter.ClearRecordCache" /> needs to be called to update the
        ///     record cache.
        /// </summary>
        public virtual void ClearRecordCache()
        {
            CheckDisposed();
            typeActions.Clear();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Ends writing of the current record
        ///     and starts a new record. This is used
        ///     when manually writing records with WriteField.
        /// </summary>
        public virtual void NextRecord()
        {
            CheckDisposed();
            serializer.Write(currentRecord.ToArray());
            currentRecord.Clear();
        }

        /// <summary>
        ///     Write the Excel seperator record.
        /// </summary>
        public virtual void WriteExcelSeparator()
        {
            CheckDisposed();
            if (hasHeaderBeenWritten)
                throw new CsvWriterException("The Excel seperator record must be the first record written in the file.");
            if (hasRecordBeenWritten)
                throw new CsvWriterException("The Excel seperator record must be the first record written in the file.");
            WriteField(string.Concat("sep=", configuration.Delimiter), false);
            NextRecord();
        }

        /// <summary>
        ///     Writes the field to the CSV file. The field
        ///     may get quotes added to it.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <param name="field">The field to write.</param>
        public virtual void WriteField(string field)
        {
            CheckDisposed();
            bool quoteAllFields = configuration.QuoteAllFields;
            if (configuration.TrimFields)
                field = field.Trim();
            if (!configuration.QuoteNoFields && !string.IsNullOrEmpty(field) &&
                (quoteAllFields | field.Contains(configuration.QuoteString) || field[0] == ' ' ||
                 field[field.Length - 1] == ' ' || field.IndexOfAny(configuration.QuoteRequiredChars) > -1 ||
                 configuration.Delimiter.Length > 1 && field.Contains(configuration.Delimiter)))
                quoteAllFields = true;
            WriteField(field, quoteAllFields);
        }

        /// <summary>
        ///     Writes the field to the CSV file. This will
        ///     ignore any need to quote and ignore the
        ///     <see cref="P:CsvHelper.Configuration.CsvConfiguration.QuoteAllFields" />
        ///     and just quote based on the shouldQuote
        ///     parameter.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <param name="field">The field to write.</param>
        /// <param name="shouldQuote">True to quote the field, otherwise false.</param>
        public virtual void WriteField(string field, bool shouldQuote)
        {
            char quote;
            CheckDisposed();
            if (shouldQuote && !string.IsNullOrEmpty(field))
                field = field.Replace(configuration.QuoteString, configuration.DoubleQuoteString);
            if (configuration.UseExcelLeadingZerosFormatForNumerics && !string.IsNullOrEmpty(field) && field[0] == '0' &&
                field.All(char.IsDigit))
            {
                string str = configuration.Quote.ToString();
                quote = configuration.Quote;
                field = string.Concat("=", str, field, quote.ToString());
            }
            else if (shouldQuote)
            {
                string str1 = configuration.Quote.ToString();
                quote = configuration.Quote;
                field = string.Concat(str1, field, quote.ToString());
            }
            currentRecord.Add(field);
        }

        /// <summary>
        ///     Writes the field to the CSV file.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to write.</param>
        public virtual void WriteField<T>(T field)
        {
            CheckDisposed();
            var type = field.GetType();
            if (type == typeof(string))
            {
                WriteField((object) field as string);
                return;
            }
            WriteField(field, TypeConverterFactory.GetConverter(type));
        }

        /// <summary>
        ///     Writes the field to the CSV file.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to write.</param>
        /// <param name="converter">The converter used to convert the field into a string.</param>
        public virtual void WriteField<T>(T field, ITypeConverter converter)
        {
            CheckDisposed();
            var options = TypeConverterOptionsFactory.GetOptions(field.GetType());
            if (options.CultureInfo == null)
                options.CultureInfo = configuration.CultureInfo;
            WriteField(converter.ConvertToString(options, field));
        }

        /// <summary>
        ///     Writes the field to the CSV file
        ///     using the given <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <typeparam name="TConverter">The type of the converter.</typeparam>
        /// <param name="field">The field to write.</param>
        public virtual void WriteField<T, TConverter>(T field)
        {
            CheckDisposed();
            WriteField(field, TypeConverterFactory.GetConverter<TConverter>());
        }

        /// <summary>
        ///     Writes the field to the CSV file.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="field">The field to write.</param>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Use WriteField<T>( T field ) instead.",
            false)]
        public virtual void WriteField(Type type, object field)
        {
            CheckDisposed();
            if (type == typeof(string))
            {
                WriteField(field as string);
                return;
            }
            WriteField(type, field, TypeConverterFactory.GetConverter(type));
        }

        /// <summary>
        ///     Writes the field to the CSV file.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="field">The field to write.</param>
        /// <param name="converter">The converter used to convert the field into a string.</param>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Use WriteField<T>( T field, ITypeConverter converter ) instead.",
            false)]
        public virtual void WriteField(Type type, object field, ITypeConverter converter)
        {
            CheckDisposed();
            var options = TypeConverterOptionsFactory.GetOptions(type);
            if (options.CultureInfo == null)
                options.CultureInfo = configuration.CultureInfo;
            WriteField(converter.ConvertToString(options, field));
        }

        /// <summary>
        ///     Writes the header record from the given properties.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        public virtual void WriteHeader<T>()
        {
            CheckDisposed();
            WriteHeader(typeof(T));
        }

        /// <summary>
        ///     Writes the header record from the given properties.
        /// </summary>
        /// <param name="type">The type of the record.</param>
        public virtual void WriteHeader(Type type)
        {
            CheckDisposed();
            if (type == null)
                throw new ArgumentNullException("type");
            if (!configuration.HasHeaderRecord)
                throw new CsvWriterException(
                    "Configuration.HasHeaderRecord is false. This will need to be enabled to write the header.");
            if (hasHeaderBeenWritten)
                throw new CsvWriterException(
                    "The header record has already been written. You can't write it more than once.");
            if (hasRecordBeenWritten)
                throw new CsvWriterException(
                    "Records have already been written. You can't write the header after writing records has started.");
            if (type == typeof(object))
                return;
            if (configuration.Maps[type] == null)
                configuration.Maps.Add(configuration.AutoMap(type));
            var csvPropertyMapCollections = new CsvPropertyMapCollection();
            AddProperties(csvPropertyMapCollections, configuration.Maps[type]);
            foreach (var csvPropertyMapCollection in csvPropertyMapCollections)
            {
                if (!CanWrite(csvPropertyMapCollection))
                    continue;
                WriteField(csvPropertyMapCollection.Data.Names.FirstOrDefault());
            }
            NextRecord();
            hasHeaderBeenWritten = true;
        }

        /// <summary>
        ///     Writes the record to the CSV file.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        /// <param name="record">The record to write.</param>
        public virtual void WriteRecord<T>(T record)
        {
            CheckDisposed();
            try
            {
                GetWriteRecordAction<T>()(record);
                hasRecordBeenWritten = true;
                NextRecord();
            }
            catch (Exception exception)
            {
                int? nullable = null;
                ExceptionHelper.AddExceptionDataMessage(exception, null, record.GetType(), null, nullable, null);
                throw;
            }
        }

        /// <summary>
        ///     Writes the record to the CSV file.
        /// </summary>
        /// <param name="type">The type of the record.</param>
        /// <param name="record">The record to write.</param>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Use WriteRecord<T>( T record ) instead.",
            false)]
        public virtual void WriteRecord(Type type, object record)
        {
            CheckDisposed();
            try
            {
                try
                {
                    GetWriteRecordAction(type).DynamicInvoke(record);
                }
                catch (TargetInvocationException targetInvocationException)
                {
                    throw targetInvocationException.InnerException;
                }
                hasRecordBeenWritten = true;
                NextRecord();
            }
            catch (Exception exception)
            {
                ExceptionHelper.AddExceptionDataMessage(exception, null, type, null, null, null);
                throw;
            }
        }

        /// <summary>
        ///     Writes the list of records to the CSV file.
        /// </summary>
        /// <param name="records">The list of records to write.</param>
        public virtual void WriteRecords(IEnumerable records)
        {
            CheckDisposed();
            Type type = null;
            try
            {
                if (configuration.HasExcelSeparator && !hasExcelSeperatorBeenRead)
                {
                    WriteExcelSeparator();
                    hasExcelSeperatorBeenRead = true;
                }
                var type1 = records.GetType().GetInterfaces().FirstOrDefault(t =>
                {
                    if (!t.GetTypeInfo().IsGenericType)
                        return false;
                    return t.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                });
                if (type1 != null)
                {
                    type = type1.GetGenericArguments().Single();
                    bool isPrimitive = type.GetTypeInfo().IsPrimitive;
                    if (configuration.HasHeaderRecord && !hasHeaderBeenWritten && !isPrimitive)
                        WriteHeader(type);
                }
                foreach (var record in records)
                {
                    type = record.GetType();
                    bool flag = type.GetTypeInfo().IsPrimitive;
                    if (configuration.HasHeaderRecord && !hasHeaderBeenWritten && !flag)
                        WriteHeader(type);
                    try
                    {
                        GetWriteRecordAction(record.GetType()).DynamicInvoke(record);
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        throw targetInvocationException.InnerException;
                    }
                    NextRecord();
                }
            }
            catch (Exception exception)
            {
                ExceptionHelper.AddExceptionDataMessage(exception, null, type, null, null, null);
                throw;
            }
        }

        /// <summary>
        ///     Adds the properties from the mapping. This will recursively
        ///     traverse the mapping tree and add all properties for
        ///     reference maps.
        /// </summary>
        /// <param name="properties">The properties to be added to.</param>
        /// <param name="mapping">The mapping where the properties are added from.</param>
        protected virtual void AddProperties(CsvPropertyMapCollection properties, CsvClassMap mapping)
        {
            properties.AddRange(mapping.PropertyMaps);
            foreach (var referenceMap in mapping.ReferenceMaps)
                AddProperties(properties, referenceMap.Data.Mapping);
        }

        /// <summary>
        ///     Checks if the property can be written.
        /// </summary>
        /// <param name="propertyMap">The property map that we are checking.</param>
        /// <returns>
        ///     A value indicating if the property can be written.
        ///     True if the property can be written, otherwise false.
        /// </returns>
        protected virtual bool CanWrite(CsvPropertyMap propertyMap)
        {
            bool cantWrite =
                // Ignored properties.
                propertyMap.Data.Ignore ||
                // Properties that don't have a public getter
                // and we are honoring the accessor modifier.
                propertyMap.Data.Property.GetGetMethod() == null && !configuration.IgnorePrivateAccessor ||
                // Properties that don't have a getter at all.
                propertyMap.Data.Property.GetGetMethod(true) == null;
            return !cantWrite;
        }

        /// <summary>
        ///     Checks if the instance has been disposed of.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException" />
        protected virtual void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().ToString());
        }

        /// <summary>
        ///     Combines the delegates into a single multicast delegate.
        ///     This is needed because Silverlight doesn't have the
        ///     Delegate.Combine( params Delegate[] ) overload.
        /// </summary>
        /// <param name="delegates">The delegates to combine.</param>
        /// <returns>A multicast delegate combined from the given delegates.</returns>
        protected virtual Delegate CombineDelegates(IEnumerable<Delegate> delegates)
        {
            return delegates.Aggregate(null, new Func<Delegate, Delegate, Delegate>(Delegate.Combine));
        }

        /// <summary>
        ///     Creates the action for an object.
        /// </summary>
        /// <param name="type">The type of object to create the action for.</param>
        protected virtual void CreateActionForObject(Type type)
        {
            var parameterExpression = Expression.Parameter(type, "record");
            var csvPropertyMapCollections = new CsvPropertyMapCollection();
            AddProperties(csvPropertyMapCollections, configuration.Maps[type]);
            if (csvPropertyMapCollections.Count == 0)
                throw new CsvWriterException(string.Format("No properties are mapped for type '{0}'.", type.FullName));
            var delegates = new List<Delegate>();
            foreach (var csvPropertyMapCollection in csvPropertyMapCollections)
            {
                if (!CanWrite(csvPropertyMapCollection) || csvPropertyMapCollection.Data.TypeConverter == null ||
                    !csvPropertyMapCollection.Data.TypeConverter.CanConvertTo(typeof(string)))
                    continue;
                var expression = CreatePropertyExpression(parameterExpression, configuration.Maps[type],
                    csvPropertyMapCollection);
                var constantExpression = Expression.Constant(csvPropertyMapCollection.Data.TypeConverter);
                if (csvPropertyMapCollection.Data.TypeConverterOptions.CultureInfo == null)
                    csvPropertyMapCollection.Data.TypeConverterOptions.CultureInfo = configuration.CultureInfo;
                var constantExpression1 =
                    Expression.Constant(
                        TypeConverterOptions.Merge(
                            TypeConverterOptionsFactory.GetOptions(csvPropertyMapCollection.Data.Property.PropertyType),
                            csvPropertyMapCollection.Data.TypeConverterOptions));
                var method = csvPropertyMapCollection.Data.TypeConverter.GetType().GetMethod("ConvertToString");
                expression = Expression.Convert(expression, typeof(object));
                expression = Expression.Call(constantExpression, method, constantExpression1, expression);
                if (type.GetTypeInfo().IsClass)
                    expression = Expression.Condition(Expression.Equal(parameterExpression, Expression.Constant(null)),
                        Expression.Constant(string.Empty), expression);
                var methodCallExpression = Expression.Call(Expression.Constant(this), "WriteField",
                    new[] {typeof(string)}, expression);
                var type1 = typeof(Action<>).MakeGenericType(type);
                delegates.Add(Expression.Lambda(type1, methodCallExpression, parameterExpression).Compile());
            }
            typeActions[type] = CombineDelegates(delegates);
        }

        /// <summary>
        ///     Creates the action for a primitive.
        /// </summary>
        /// <param name="type">The type of primitive to create the action for.</param>
        protected virtual void CreateActionForPrimitive(Type type)
        {
            var parameterExpression = Expression.Parameter(type, "record");
            Expression expression = Expression.Convert(parameterExpression, typeof(object));
            var converter = TypeConverterFactory.GetConverter(type);
            var constantExpression = Expression.Constant(converter);
            var method = converter.GetType().GetMethod("ConvertToString");
            var options = TypeConverterOptionsFactory.GetOptions(type);
            if (options.CultureInfo == null)
                options.CultureInfo = configuration.CultureInfo;
            expression = Expression.Call(constantExpression, method, Expression.Constant(options), expression);
            expression = Expression.Call(Expression.Constant(this), "WriteField", new[] {typeof(string)}, expression);
            var type1 = typeof(Action<>).MakeGenericType(type);
            typeActions[type] = Expression.Lambda(type1, expression, parameterExpression).Compile();
        }

        /// <summary>
        ///     Creates a property expression for the given property on the record.
        ///     This will recursively traverse the mapping to find the property
        ///     and create a safe property accessor for each level as it goes.
        /// </summary>
        /// <param name="recordExpression">The current property expression.</param>
        /// <param name="mapping">The mapping to look for the property to map on.</param>
        /// <param name="propertyMap">The property map to look for on the mapping.</param>
        /// <returns>An Expression to access the given property.</returns>
        protected virtual Expression CreatePropertyExpression(Expression recordExpression, CsvClassMap mapping,
            CsvPropertyMap propertyMap)
        {
            Type propertyType;
            Expression expression;
            Expression expression1;
            if (mapping.PropertyMaps.Any(pm => pm == propertyMap))
                return Expression.Property(recordExpression, propertyMap.Data.Property);
            var enumerator = mapping.ReferenceMaps.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    var memberExpression = Expression.Property(recordExpression, current.Data.Property);
                    var expression2 = CreatePropertyExpression(memberExpression, current.Data.Mapping, propertyMap);
                    if (expression2 == null)
                        continue;
                    if (!current.Data.Property.PropertyType.GetTypeInfo().IsValueType)
                    {
                        var binaryExpression = Expression.Equal(memberExpression, Expression.Constant(null));
                        bool isValueType = propertyMap.Data.Property.PropertyType.GetTypeInfo().IsValueType;
                        bool flag = !isValueType
                            ? false
                            : propertyMap.Data.Property.PropertyType.GetTypeInfo().IsGenericType;
                        if (!isValueType || flag || configuration.UseNewObjectForNullReferenceProperties)
                        {
                            propertyType = propertyMap.Data.Property.PropertyType;
                        }
                        else
                        {
                            propertyType = typeof(Nullable<>).MakeGenericType(propertyMap.Data.Property.PropertyType);
                            expression2 = Expression.Convert(expression2, propertyType);
                        }
                        if (!isValueType || flag)
                            expression1 = Expression.Constant(null, propertyType);
                        else
                            expression1 = Expression.New(propertyType);
                        expression = Expression.Condition(binaryExpression, expression1, expression2);
                        return expression;
                    }
                    else
                    {
                        expression = expression2;
                        return expression;
                    }
                }
                return null;
            }
            finally
            {
                ((IDisposable) enumerator).Dispose();
            }
            return expression;
        }

        /// <summary>
        ///     Creates the write record action for the given type if it
        ///     doesn't already exist.
        /// </summary>
        /// <param name="type">The type of the custom class being written.</param>
        protected virtual void CreateWriteRecordAction(Type type)
        {
            if (typeActions.ContainsKey(type))
                return;
            if (configuration.Maps[type] == null)
                configuration.Maps.Add(configuration.AutoMap(type));
            if (type.GetTypeInfo().IsPrimitive)
            {
                CreateActionForPrimitive(type);
                return;
            }
            CreateActionForObject(type);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if the instance needs to be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing && serializer != null)
                serializer.Dispose();
            disposed = true;
            serializer = null;
        }

        /// <summary>
        ///     Gets the action delegate used to write the custom
        ///     class object to the writer.
        /// </summary>
        /// <typeparam name="T">The type of the custom class being written.</typeparam>
        /// <returns>The action delegate.</returns>
        protected virtual Action<T> GetWriteRecordAction<T>()
        {
            var type = typeof(T);
            CreateWriteRecordAction(type);
            return (Action<T>) typeActions[type];
        }

        /// <summary>
        ///     Gets the action delegate used to write the custom
        ///     class object to the writer.
        /// </summary>
        /// <param name="type">The type of the custom class being written.</param>
        /// <returns>The action delegate.</returns>
        protected virtual Delegate GetWriteRecordAction(Type type)
        {
            CreateWriteRecordAction(type);
            return typeActions[type];
        }
    }
}