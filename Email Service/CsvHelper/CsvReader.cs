using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CsvHelper
{
    /// <summary>
    ///     Reads data that was parsed from <see cref="T:CsvHelper.ICsvParser" />.
    /// </summary>
    public class CsvReader : ICsvReader, ICsvReaderRow, IDisposable
    {
        private const string DoneReadingExceptionMessage =
            "The reader has already exhausted all records. If you would like to iterate the records more than once, store the records in memory. i.e. Use CsvReader.GetRecords<T>().ToList()";

        private readonly CsvConfiguration configuration;

        private readonly Dictionary<string, List<int>> namedIndexes = new Dictionary<string, List<int>>();

        private readonly Dictionary<Type, Delegate> recordFuncs = new Dictionary<Type, Delegate>();

        private int currentIndex = -1;

        private string[] currentRecord;
        private bool disposed;

        private bool doneReading;

        private bool hasBeenRead;

        private string[] headerRecord;

        private ICsvParser parser;

        /// <summary>
        ///     Creates a new CSV reader using the given <see cref="T:System.IO.TextReader" /> and
        ///     <see cref="T:CsvHelper.CsvParser" /> as the default parser.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public CsvReader(TextReader reader) : this(reader, new CsvConfiguration())
        {
        }

        /// <summary>
        ///     Creates a new CSV reader using the given <see cref="T:System.IO.TextReader" /> and
        ///     <see cref="T:CsvHelper.Configuration.CsvConfiguration" /> and <see cref="T:CsvHelper.CsvParser" /> as the default
        ///     parser.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="configuration">The configuration.</param>
        public CsvReader(TextReader reader, CsvConfiguration configuration)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            parser = new CsvParser(reader, configuration);
            this.configuration = configuration;
        }

        /// <summary>
        ///     Creates a new CSV reader using the given <see cref="T:CsvHelper.ICsvParser" />.
        /// </summary>
        /// <param name="parser">The <see cref="T:CsvHelper.ICsvParser" /> used to parse the CSV file.</param>
        public CsvReader(ICsvParser parser)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");
            if (parser.Configuration == null)
                throw new CsvConfigurationException("The given parser has no configuration.");
            this.parser = parser;
            configuration = parser.Configuration;
        }

        /// <summary>
        ///     Gets the configuration.
        /// </summary>
        public virtual CsvConfiguration Configuration
        {
            get { return configuration; }
        }

        /// <summary>
        ///     Get the current record;
        /// </summary>
        public virtual string[] CurrentRecord
        {
            get
            {
                CheckDisposed();
                CheckHasBeenRead();
                return currentRecord;
            }
        }

        /// <summary>
        ///     Gets the field headers.
        /// </summary>
        public virtual string[] FieldHeaders
        {
            get
            {
                CheckDisposed();
                if (headerRecord == null)
                    throw new CsvReaderException("You must call ReadHeader or Read before accessing the field headers.");
                return headerRecord;
            }
        }

        /// <summary>
        ///     Gets the raw field at position (column) index.
        /// </summary>
        /// <param name="index">The zero based index of the field.</param>
        /// <returns>The raw field.</returns>
        public virtual string this[int index]
        {
            get
            {
                CheckDisposed();
                CheckHasBeenRead();
                return GetField(index);
            }
        }

        /// <summary>
        ///     Gets the raw field at position (column) name.
        /// </summary>
        /// <param name="name">The named index of the field.</param>
        /// <returns>The raw field.</returns>
        public virtual string this[string name]
        {
            get
            {
                CheckDisposed();
                CheckHasBeenRead();
                return GetField(name);
            }
        }

        /// <summary>
        ///     Gets the raw field at position (column) name.
        /// </summary>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the field.</param>
        /// <returns>The raw field.</returns>
        public virtual string this[string name, int index]
        {
            get
            {
                CheckDisposed();
                CheckHasBeenRead();
                return GetField(name, index);
            }
        }

        /// <summary>
        ///     Gets the parser.
        /// </summary>
        public virtual ICsvParser Parser
        {
            get { return parser; }
        }

        /// <summary>
        ///     Gets the current row.
        /// </summary>
        public int Row
        {
            get
            {
                CheckDisposed();
                CheckHasBeenRead();
                return parser.Row;
            }
        }

        /// <summary>
        ///     Clears the record cache for the given type. After <see cref="M:CsvHelper.ICsvReaderRow.GetRecord``1" /> is called
        ///     the
        ///     first time, code is dynamically generated based on the
        ///     <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />,
        ///     compiled, and stored for the given type T. If the <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />
        ///     changes, <see cref="M:CsvHelper.ICsvReaderRow.ClearRecordCache``1" /> needs to be called to update the
        ///     record cache.
        /// </summary>
        public virtual void ClearRecordCache<T>()
        {
            CheckDisposed();
            ClearRecordCache(typeof(T));
        }

        /// <summary>
        ///     Clears the record cache for the given type. After <see cref="M:CsvHelper.ICsvReaderRow.GetRecord``1" /> is called
        ///     the
        ///     first time, code is dynamically generated based on the
        ///     <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />,
        ///     compiled, and stored for the given type T. If the <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />
        ///     changes, <see cref="M:CsvHelper.ICsvReaderRow.ClearRecordCache(System.Type)" /> needs to be called to update the
        ///     record cache.
        /// </summary>
        /// <param name="type">The type to invalidate.</param>
        public virtual void ClearRecordCache(Type type)
        {
            CheckDisposed();
            recordFuncs.Remove(type);
        }

        /// <summary>
        ///     Clears the record cache for all types. After <see cref="M:CsvHelper.ICsvReaderRow.GetRecord``1" /> is called the
        ///     first time, code is dynamically generated based on the
        ///     <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />,
        ///     compiled, and stored for the given type T. If the <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />
        ///     changes, <see cref="M:CsvHelper.ICsvReaderRow.ClearRecordCache" /> needs to be called to update the
        ///     record cache.
        /// </summary>
        public virtual void ClearRecordCache()
        {
            CheckDisposed();
            recordFuncs.Clear();
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
        ///     Gets the raw field at position (column) index.
        /// </summary>
        /// <param name="index">The zero based index of the field.</param>
        /// <returns>The raw field.</returns>
        public virtual string GetField(int index)
        {
            CheckDisposed();
            CheckHasBeenRead();
            currentIndex = index;
            if (index >= currentRecord.Length)
            {
                if (configuration.WillThrowOnMissingField)
                {
                    var csvMissingFieldException =
                        new CsvMissingFieldException(string.Format("Field at index '{0}' does not exist.", index));
                    ExceptionHelper.AddExceptionDataMessage(csvMissingFieldException, Parser, typeof(string),
                        namedIndexes, index, currentRecord);
                    throw csvMissingFieldException;
                }
                return null;
            }
            string str = currentRecord[index];
            if (configuration.TrimFields && str != null)
                str = str.Trim();
            return str;
        }

        /// <summary>
        ///     Gets the raw field at position (column) name.
        /// </summary>
        /// <param name="name">The named index of the field.</param>
        /// <returns>The raw field.</returns>
        public virtual string GetField(string name)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, 0, false);
            if (fieldIndex < 0)
                return null;
            return GetField(fieldIndex);
        }

        /// <summary>
        ///     Gets the raw field at position (column) name and the index
        ///     instance of that field. The index is used when there are
        ///     multiple columns with the same header name.
        /// </summary>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <returns>The raw field.</returns>
        public virtual string GetField(string name, int index)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, index, false);
            if (fieldIndex < 0)
                return null;
            return GetField(fieldIndex);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="index">The index of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        public virtual object GetField(Type type, int index)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return GetField(type, index, TypeConverterFactory.GetConverter(type));
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="name">The named index of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        public virtual object GetField(Type type, string name)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return GetField(type, name, TypeConverterFactory.GetConverter(type));
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        public virtual object GetField(Type type, string name, int index)
        {
            CheckDisposed();
            CheckHasBeenRead();
            var converter = TypeConverterFactory.GetConverter(type);
            return GetField(type, name, index, converter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="index">The index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Object" />.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        public virtual object GetField(Type type, int index, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            var options = TypeConverterOptionsFactory.GetOptions(type);
            if (options.CultureInfo == null)
                options.CultureInfo = configuration.CultureInfo;
            return converter.ConvertFromString(options, GetField(index));
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="name">The named index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Object" />.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        public virtual object GetField(Type type, string name, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, 0, false);
            return GetField(type, fieldIndex, converter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="type">The type of the field.</param>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Object" />.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        public virtual object GetField(Type type, string name, int index, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, index, false);
            return GetField(type, fieldIndex, converter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) index.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="index">The zero based index of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T>(int index)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return GetField<T>(index, TypeConverterFactory.GetConverter<T>());
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T>(string name)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return GetField<T>(name, TypeConverterFactory.GetConverter<T>());
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position
        ///     (column) name and the index instance of that field. The index
        ///     is used when there are multiple columns with the same header name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <returns></returns>
        public virtual T GetField<T>(string name, int index)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return GetField<T>(name, index, TypeConverterFactory.GetConverter<T>());
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) index using
        ///     the given <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="index">The zero based index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T>(int index, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            if (index < currentRecord.Length && index >= 0)
                return (T) GetField(typeof(T), index, converter);
            if (configuration.WillThrowOnMissingField)
            {
                var csvMissingFieldException =
                    new CsvMissingFieldException(string.Format("Field at index '{0}' does not exist.", index));
                ExceptionHelper.AddExceptionDataMessage(csvMissingFieldException, Parser, typeof(T), namedIndexes, index,
                    currentRecord);
                throw csvMissingFieldException;
            }
            return default(T);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name using
        ///     the given <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T>(string name, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, 0, false);
            return GetField<T>(fieldIndex, converter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position
        ///     (column) name and the index instance of that field. The index
        ///     is used when there are multiple columns with the same header name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T>(string name, int index, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, index, false);
            return GetField<T>(fieldIndex, converter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) index using
        ///     the given <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <typeparam name="TConverter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </typeparam>
        /// <param name="index">The zero based index of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T, TConverter>(int index)
            where TConverter : ITypeConverter
        {
            CheckDisposed();
            CheckHasBeenRead();
            var tConverter = ReflectionHelper.CreateInstance<TConverter>();
            return GetField<T>(index, tConverter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name using
        ///     the given <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <typeparam name="TConverter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T, TConverter>(string name)
            where TConverter : ITypeConverter
        {
            CheckDisposed();
            CheckHasBeenRead();
            var tConverter = ReflectionHelper.CreateInstance<TConverter>();
            return GetField<T>(name, tConverter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position
        ///     (column) name and the index instance of that field. The index
        ///     is used when there are multiple columns with the same header name.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <typeparam name="TConverter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <returns>The field converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetField<T, TConverter>(string name, int index)
            where TConverter : ITypeConverter
        {
            CheckDisposed();
            CheckHasBeenRead();
            var tConverter = ReflectionHelper.CreateInstance<TConverter>();
            return GetField<T>(name, index, tConverter);
        }

        /// <summary>
        ///     Gets the record converted into <see cref="T:System.Type" /> T.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the record.</typeparam>
        /// <returns>The record converted to <see cref="T:System.Type" /> T.</returns>
        public virtual T GetRecord<T>()
        {
            T t;
            CheckDisposed();
            CheckHasBeenRead();
            try
            {
                t = CreateRecord<T>();
            }
            catch (Exception exception)
            {
                ExceptionHelper.AddExceptionDataMessage(exception, parser, typeof(T), namedIndexes, currentIndex,
                    currentRecord);
                throw;
            }
            return t;
        }

        /// <summary>
        ///     Gets the record.
        /// </summary>
        /// <param name="type">The <see cref="T:System.Type" /> of the record.</param>
        /// <returns>The record.</returns>
        public virtual object GetRecord(Type type)
        {
            object obj;
            CheckDisposed();
            CheckHasBeenRead();
            try
            {
                obj = CreateRecord(type);
            }
            catch (Exception exception)
            {
                ExceptionHelper.AddExceptionDataMessage(exception, parser, type, namedIndexes, currentIndex,
                    currentRecord);
                throw;
            }
            return obj;
        }

        /// <summary>
        ///     Gets all the records in the CSV file and
        ///     converts each to <see cref="T:System.Type" /> T. The Read method
        ///     should not be used when using this.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the record.</typeparam>
        /// <returns>An <see cref="T:System.Collections.Generic.IList`1" /> of records.</returns>
        public virtual IEnumerable<T> GetRecords<T>()
        {
            T t;
            CheckDisposed();
            while (Read())
            {
                try
                {
                    t = CreateRecord<T>();
                }
                catch (Exception exception1)
                {
                    var exception = exception1;
                    ExceptionHelper.AddExceptionDataMessage(exception, parser, typeof(T), namedIndexes, currentIndex,
                        currentRecord);
                    if (!configuration.IgnoreReadingExceptions)
                        throw;
                    if (configuration.ReadingExceptionCallback != null)
                        configuration.ReadingExceptionCallback(exception, this);
                    continue;
                }
                yield return t;
                t = default(T);
            }
        }

        /// <summary>
        ///     Gets all the records in the CSV file and
        ///     converts each to <see cref="T:System.Type" /> T. The Read method
        ///     should not be used when using this.
        /// </summary>
        /// <param name="type">The <see cref="T:System.Type" /> of the record.</param>
        /// <returns>An <see cref="T:System.Collections.Generic.IList`1" /> of records.</returns>
        public virtual IEnumerable<object> GetRecords(Type type)
        {
            object obj;
            CheckDisposed();
            while (Read())
            {
                try
                {
                    obj = CreateRecord(type);
                }
                catch (Exception exception1)
                {
                    var exception = exception1;
                    ExceptionHelper.AddExceptionDataMessage(exception, parser, type, namedIndexes, currentIndex,
                        currentRecord);
                    if (!configuration.IgnoreReadingExceptions)
                        throw;
                    if (configuration.ReadingExceptionCallback != null)
                        configuration.ReadingExceptionCallback(exception, this);
                    continue;
                }
                yield return obj;
                obj = null;
            }
        }

        /// <summary>
        ///     Determines whether the current record is empty.
        ///     A record is considered empty if all fields are empty.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if [is record empty]; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsRecordEmpty()
        {
            CheckDisposed();
            CheckHasBeenRead();
            return IsRecordEmpty(true);
        }

        /// <summary>
        ///     Advances the reader to the next record.
        ///     If HasHeaderRecord is true (true by default), the first record of
        ///     the CSV file will be automatically read in as the header record
        ///     and the second record will be returned.
        /// </summary>
        /// <returns>True if there are more records, otherwise false.</returns>
        public virtual bool Read()
        {
            CheckDisposed();
            if (doneReading)
                throw new CsvReaderException(
                    "The reader has already exhausted all records. If you would like to iterate the records more than once, store the records in memory. i.e. Use CsvReader.GetRecords<T>().ToList()");
            if (configuration.HasHeaderRecord && headerRecord == null)
                ReadHeader();
            do
            {
                currentRecord = parser.Read();
            } while (ShouldSkipRecord());
            currentIndex = -1;
            hasBeenRead = true;
            if (currentRecord == null)
                doneReading = true;
            return currentRecord != null;
        }

        /// <summary>
        ///     Reads the header field without reading the first row.
        /// </summary>
        /// <returns>True if there are more records, otherwise false.</returns>
        public virtual bool ReadHeader()
        {
            CheckDisposed();
            if (doneReading)
                throw new CsvReaderException(
                    "The reader has already exhausted all records. If you would like to iterate the records more than once, store the records in memory. i.e. Use CsvReader.GetRecords<T>().ToList()");
            if (!configuration.HasHeaderRecord)
                throw new CsvReaderException("Configuration.HasHeaderRecord is false.");
            if (headerRecord != null)
                throw new CsvReaderException("Header record has already been read.");
            do
            {
                currentRecord = parser.Read();
            } while (ShouldSkipRecord());
            headerRecord = currentRecord;
            currentRecord = null;
            ParseNamedIndexes();
            return headerRecord != null;
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) index.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="index">The zero based index of the field.</param>
        /// <param name="field">The field converted to type T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T>(int index, out T field)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return TryGetField(index, TypeConverterFactory.GetConverter<T>(), out field);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T>(string name, out T field)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return TryGetField(name, TypeConverterFactory.GetConverter<T>(), out field);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position
        ///     (column) name and the index instance of that field. The index
        ///     is used when there are multiple columns with the same header name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T>(string name, int index, out T field)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return TryGetField(name, index, TypeConverterFactory.GetConverter<T>(), out field);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) index
        ///     using the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="index">The zero based index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T>(int index, ITypeConverter converter, out T field)
        {
            bool flag;
            CheckDisposed();
            CheckHasBeenRead();
            if (converter is DateTimeConverter && StringHelper.IsNullOrWhiteSpace(currentRecord[index]))
            {
                field = default(T);
                return false;
            }
            try
            {
                field = GetField<T>(index, converter);
                flag = true;
            }
            catch
            {
                field = default(T);
                flag = false;
            }
            return flag;
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name
        ///     using the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T>(string name, ITypeConverter converter, out T field)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, 0, true);
            if (fieldIndex == -1)
            {
                field = default(T);
                return false;
            }
            return TryGetField(fieldIndex, converter, out field);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name
        ///     using the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T>(string name, int index, ITypeConverter converter, out T field)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, index, true);
            if (fieldIndex == -1)
            {
                field = default(T);
                return false;
            }
            return TryGetField(fieldIndex, converter, out field);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) index
        ///     using the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <typeparam name="TConverter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </typeparam>
        /// <param name="index">The zero based index of the field.</param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T, TConverter>(int index, out T field)
            where TConverter : ITypeConverter
        {
            CheckDisposed();
            CheckHasBeenRead();
            var tConverter = ReflectionHelper.CreateInstance<TConverter>();
            return TryGetField(index, tConverter, out field);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name
        ///     using the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <typeparam name="TConverter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T, TConverter>(string name, out T field)
            where TConverter : ITypeConverter
        {
            CheckDisposed();
            CheckHasBeenRead();
            var tConverter = ReflectionHelper.CreateInstance<TConverter>();
            return TryGetField(name, tConverter, out field);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Type" /> T at position (column) name
        ///     using the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <typeparam name="T">The <see cref="T:System.Type" /> of the field.</typeparam>
        /// <typeparam name="TConverter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Type" /> T.
        /// </typeparam>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <param name="field">The field converted to <see cref="T:System.Type" /> T.</param>
        /// <returns>A value indicating if the get was successful.</returns>
        public virtual bool TryGetField<T, TConverter>(string name, int index, out T field)
            where TConverter : ITypeConverter
        {
            CheckDisposed();
            CheckHasBeenRead();
            var tConverter = ReflectionHelper.CreateInstance<TConverter>();
            return TryGetField(name, index, tConverter, out field);
        }

        /// <summary>
        ///     Adds a <see cref="T:System.Linq.Expressions.MemberBinding" /> for each property for it's field.
        /// </summary>
        /// <param name="properties">The properties to add bindings for.</param>
        /// <param name="bindings">The bindings that will be added to from the properties.</param>
        protected virtual void AddPropertyBindings(CsvPropertyMapCollection properties, List<MemberBinding> bindings)
        {
            foreach (var property in properties)
                if (property.Data.ConvertExpression == null)
                {
                    if (!CanRead(property) || property.Data.TypeConverter == null ||
                        !property.Data.TypeConverter.CanConvertFrom(typeof(string)))
                        continue;
                    int fieldIndex = -1;
                    if (property.Data.IsNameSet)
                    {
                        fieldIndex = GetFieldIndex(property.Data.Names.ToArray(), property.Data.NameIndex, false);
                        if (fieldIndex == -1)
                            continue;
                    }
                    else if (property.Data.IsIndexSet)
                    {
                        fieldIndex = property.Data.Index;
                    }
                    else if (configuration.HasHeaderRecord)
                    {
                        fieldIndex = GetFieldIndex(property.Data.Names.ToArray(), property.Data.NameIndex, false);
                        if (fieldIndex == -1)
                            continue;
                    }
                    else if (fieldIndex == -1)
                    {
                        fieldIndex = property.Data.Index;
                    }
                    var getMethod =
                        typeof(ICsvReaderRow).GetProperty("Item", typeof(string), new[] {typeof(int)}).GetGetMethod();
                    Expression expression = Expression.Call(Expression.Constant(this), getMethod,
                        Expression.Constant(fieldIndex, typeof(int)));
                    var constantExpression = Expression.Constant(property.Data.TypeConverter);
                    if (property.Data.TypeConverterOptions.CultureInfo == null)
                        property.Data.TypeConverterOptions.CultureInfo = configuration.CultureInfo;
                    var constantExpression1 =
                        Expression.Constant(
                            TypeConverterOptions.Merge(
                                TypeConverterOptionsFactory.GetOptions(property.Data.Property.PropertyType),
                                property.Data.TypeConverterOptions));
                    Expression expression1 = Expression.Call(constantExpression, "ConvertFromString", null,
                        constantExpression1, expression);
                    expression1 = Expression.Convert(expression1, property.Data.Property.PropertyType);
                    if (!property.Data.IsDefaultSet)
                    {
                        expression = expression1;
                    }
                    else
                    {
                        Expression expression2 = Expression.Convert(Expression.Constant(property.Data.Default),
                            property.Data.Property.PropertyType);
                        expression =
                            Expression.Condition(
                                Expression.Equal(
                                    Expression.Convert(
                                        Expression.Coalesce(expression, Expression.Constant(string.Empty)),
                                        typeof(string)), Expression.Constant(string.Empty, typeof(string))), expression2,
                                expression1);
                    }
                    bindings.Add(Expression.Bind(property.Data.Property, expression));
                }
                else
                {
                    Expression expression3 = Expression.Invoke(property.Data.ConvertExpression,
                        Expression.Constant(this));
                    expression3 = Expression.Convert(expression3, property.Data.Property.PropertyType);
                    bindings.Add(Expression.Bind(property.Data.Property, expression3));
                }
        }

        /// <summary>
        ///     Determines if the property for the <see cref="CsvPropertyMap" />
        ///     can be read.
        /// </summary>
        /// <param name="propertyMap">The property map.</param>
        /// <returns>A value indicating of the property can be read. True if it can, otherwise false.</returns>
        protected virtual bool CanRead(CsvPropertyMap propertyMap)
        {
            bool cantRead =
                // Ignored properties.
                propertyMap.Data.Ignore ||
                // Properties that don't have a public setter
                // and we are honoring the accessor modifier.
                propertyMap.Data.Property.GetSetMethod() == null && !configuration.IgnorePrivateAccessor ||
                // Properties that don't have a setter at all.
                propertyMap.Data.Property.GetSetMethod(true) == null;
            return !cantRead;
        }

        /// <summary>
        ///     Determines if the property for the <see cref="CsvPropertyReferenceMap" />
        ///     can be read.
        /// </summary>
        /// <param name="propertyReferenceMap">The reference map.</param>
        /// <returns>A value indicating of the property can be read. True if it can, otherwise false.</returns>
        protected virtual bool CanRead(CsvPropertyReferenceMap propertyReferenceMap)
        {
            bool cantRead =
                // Properties that don't have a public setter
                // and we are honoring the accessor modifier.
                propertyReferenceMap.Data.Property.GetSetMethod() == null && !configuration.IgnorePrivateAccessor ||
                // Properties that don't have a setter at all.
                propertyReferenceMap.Data.Property.GetSetMethod(true) == null;
            return !cantRead;
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
        ///     Checks if the reader has been read yet.
        /// </summary>
        /// <exception cref="T:CsvHelper.CsvReaderException" />
        protected virtual void CheckHasBeenRead()
        {
            if (!hasBeenRead)
                throw new CsvReaderException("You must call read on the reader before accessing its data.");
        }

        /// <summary>
        ///     Creates a dynamic object from the current record.
        /// </summary>
        /// <returns>The dynamic object.</returns>
        protected virtual dynamic CreateDynamic()
        {
            var expandoObjects = new ExpandoObject();
            IDictionary<string, object> strs = expandoObjects;
            if (headerRecord == null)
                for (int i = 0; i < currentRecord.Length; i++)
                {
                    string str = string.Concat("Field", i + 1);
                    strs.Add(str, currentRecord[i]);
                }
            else
                for (int j = 0; j < headerRecord.Length; j++)
                {
                    string str1 = headerRecord[j];
                    strs.Add(str1, currentRecord[j]);
                }
            return expandoObjects;
        }

        /// <summary>
        ///     Creates the function for an object.
        /// </summary>
        /// <param name="recordType">The type of object to create the function for.</param>
        protected virtual void CreateFuncForObject(Type recordType)
        {
            var memberBindings = new List<MemberBinding>();
            CreatePropertyBindingsForMapping(configuration.Maps[recordType], recordType, memberBindings);
            if (memberBindings.Count == 0)
                throw new CsvReaderException(string.Format("No properties are mapped for type '{0}'.",
                    recordType.FullName));
            var memberInitExpression =
                Expression.MemberInit(configuration.Maps[recordType].Constructor ?? Expression.New(recordType),
                    memberBindings);
            var type = typeof(Func<>).MakeGenericType(recordType);
            recordFuncs[recordType] = Expression.Lambda(type, memberInitExpression).Compile();
        }

        /// <summary>
        ///     Creates the function for a primitive.
        /// </summary>
        /// <param name="recordType">The type of the primitive to create the function for.</param>
        protected virtual void CreateFuncForPrimitive(Type recordType)
        {
            var getMethod =
                typeof(ICsvReaderRow).GetProperty("Item", typeof(string), new[] {typeof(int)}).GetGetMethod();
            Expression expression = Expression.Call(Expression.Constant(this), getMethod,
                Expression.Constant(0, typeof(int)));
            var converter = TypeConverterFactory.GetConverter(recordType);
            var options = TypeConverterOptionsFactory.GetOptions(recordType);
            if (options.CultureInfo == null)
                options.CultureInfo = configuration.CultureInfo;
            expression = Expression.Call(Expression.Constant(converter), "ConvertFromString", null,
                Expression.Constant(options), expression);
            expression = Expression.Convert(expression, recordType);
            var type = typeof(Func<>).MakeGenericType(recordType);
            recordFuncs[recordType] = Expression.Lambda(type, expression).Compile();
        }

        /// <summary>
        ///     Creates the property bindings for the given <see cref="T:CsvHelper.Configuration.CsvClassMap" />.
        /// </summary>
        /// <param name="mapping">The mapping to create the bindings for.</param>
        /// <param name="recordType">The type of record.</param>
        /// <param name="bindings">The bindings that will be added to from the mapping.</param>
        protected virtual void CreatePropertyBindingsForMapping(CsvClassMap mapping, Type recordType,
            List<MemberBinding> bindings)
        {
            AddPropertyBindings(mapping.PropertyMaps, bindings);
            foreach (var referenceMap in mapping.ReferenceMaps)
            {
                if (!CanRead(referenceMap))
                    continue;
                var memberBindings = new List<MemberBinding>();
                CreatePropertyBindingsForMapping(referenceMap.Data.Mapping, referenceMap.Data.Property.PropertyType,
                    memberBindings);
                var memberInitExpression = Expression.MemberInit(
                    Expression.New(referenceMap.Data.Property.PropertyType), memberBindings);
                bindings.Add(Expression.Bind(referenceMap.Data.Property, memberInitExpression));
            }
        }

        /// <summary>
        ///     Creates the read record func for the given type if it
        ///     doesn't already exist.
        /// </summary>
        /// <param name="recordType">Type of the record.</param>
        protected virtual void CreateReadRecordFunc(Type recordType)
        {
            if (recordFuncs.ContainsKey(recordType))
                return;
            if (configuration.Maps[recordType] == null)
                configuration.Maps.Add(configuration.AutoMap(recordType));
            if (recordType.GetTypeInfo().IsPrimitive)
            {
                CreateFuncForPrimitive(recordType);
                return;
            }
            CreateFuncForObject(recordType);
        }

        /// <summary>
        ///     Creates the record for the given type.
        /// </summary>
        /// <typeparam name="T">The type of record to create.</typeparam>
        /// <returns>The created record.</returns>
        protected virtual T CreateRecord<T>()
        {
            return GetReadRecordFunc<T>()();
        }

        /// <summary>
        ///     Creates the record for the given type.
        /// </summary>
        /// <param name="type">The type of record to create.</param>
        /// <returns>The created record.</returns>
        protected virtual object CreateRecord(Type type)
        {
            object obj;
            if (type == typeof(object))
                return CreateDynamic();
            try
            {
                obj = GetReadRecordFunc(type).DynamicInvoke();
            }
            catch (TargetInvocationException targetInvocationException)
            {
                throw targetInvocationException.InnerException;
            }
            return obj;
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if the instance needs to be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing && parser != null)
                parser.Dispose();
            disposed = true;
            parser = null;
        }

        /// <summary>
        ///     Gets a function to test for an empty string.
        ///     Will check <see cref="P:CsvHelper.Configuration.CsvConfiguration.TrimFields" /> when making its decision.
        /// </summary>
        /// <returns>The function to test for an empty string.</returns>
        protected virtual Func<string, bool> GetEmtpyStringMethod()
        {
            if (!Configuration.TrimFields)
                return string.IsNullOrEmpty;
            return string.IsNullOrWhiteSpace;
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="index">The index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Object" />.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Use GetField( Type, int, ITypeConverter ) instead.",
            false)]
        public virtual object GetField(int index, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            return converter.ConvertFromString(new TypeConverterOptions
            {
                CultureInfo = configuration.CultureInfo
            }, GetField(index));
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="name">The named index of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Object" />.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Use GetField( Type, string, ITypeConverter ) instead.",
            false)]
        public virtual object GetField(string name, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, 0, false);
            return GetField(fieldIndex, converter);
        }

        /// <summary>
        ///     Gets the field converted to <see cref="T:System.Object" /> using
        ///     the specified <see cref="T:CsvHelper.TypeConversion.ITypeConverter" />.
        /// </summary>
        /// <param name="name">The named index of the field.</param>
        /// <param name="index">The zero based index of the instance of the field.</param>
        /// <param name="converter">
        ///     The <see cref="T:CsvHelper.TypeConversion.ITypeConverter" /> used to convert the field to
        ///     <see cref="T:System.Object" />.
        /// </param>
        /// <returns>The field converted to <see cref="T:System.Object" />.</returns>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Use GetField( Type, string, int, ITypeConverter ) instead.",
            false)]
        public virtual object GetField(string name, int index, ITypeConverter converter)
        {
            CheckDisposed();
            CheckHasBeenRead();
            int fieldIndex = GetFieldIndex(name, index, false);
            return GetField(fieldIndex, converter);
        }

        /// <summary>
        ///     Gets the index of the field at name if found.
        /// </summary>
        /// <param name="name">The name of the field to get the index for.</param>
        /// <param name="index">The index of the field if there are multiple fields with the same name.</param>
        /// <param name="isTryGet">A value indicating if the call was initiated from a TryGet.</param>
        /// <returns>The index of the field if found, otherwise -1.</returns>
        /// <exception cref="T:CsvHelper.CsvReaderException">Thrown if there is no header record.</exception>
        /// <exception cref="T:CsvHelper.CsvMissingFieldException">Thrown if there isn't a field with name.</exception>
        protected virtual int GetFieldIndex(string name, int index = 0, bool isTryGet = false)
        {
            return GetFieldIndex(new[] {name}, index, isTryGet);
        }

        /// <summary>
        ///     Gets the index of the field at name if found.
        /// </summary>
        /// <param name="names">The possible names of the field to get the index for.</param>
        /// <param name="index">The index of the field if there are multiple fields with the same name.</param>
        /// <param name="isTryGet">A value indicating if the call was initiated from a TryGet.</param>
        /// <returns>The index of the field if found, otherwise -1.</returns>
        /// <exception cref="T:CsvHelper.CsvReaderException">Thrown if there is no header record.</exception>
        /// <exception cref="T:CsvHelper.CsvMissingFieldException">Thrown if there isn't a field with name.</exception>
        protected virtual int GetFieldIndex(string[] names, int index = 0, bool isTryGet = false)
        {
            if (names == null)
                throw new ArgumentNullException("names");
            if (!configuration.HasHeaderRecord)
                throw new CsvReaderException("There is no header record to determine the index by name.");
            var compareOption = !Configuration.IsHeaderCaseSensitive ? CompareOptions.IgnoreCase : CompareOptions.None;
            string key = null;
            foreach (var namedIndex in namedIndexes)
            {
                string str = namedIndex.Key;
                if (configuration.IgnoreHeaderWhiteSpace)
                    str = Regex.Replace(str, "\\s", string.Empty);
                else if (configuration.TrimHeaders && str != null)
                    str = str.Trim();
                var strArrays = names;
                for (int i = 0; i < strArrays.Length; i++)
                {
                    string str1 = strArrays[i];
                    if (Configuration.CultureInfo.CompareInfo.Compare(str, str1, compareOption) == 0)
                        key = namedIndex.Key;
                }
            }
            if (key != null)
                return namedIndexes[key][index];
            if (configuration.WillThrowOnMissingField && !isTryGet)
            {
                string str2 = string.Format("'{0}'", string.Join("', '", names));
                var csvMissingFieldException =
                    new CsvMissingFieldException(string.Format("Fields {0} do not exist in the CSV file.", str2));
                ExceptionHelper.AddExceptionDataMessage(csvMissingFieldException, Parser, null, namedIndexes,
                    currentIndex, currentRecord);
                throw csvMissingFieldException;
            }
            return -1;
        }

        /// <summary>
        ///     Gets the function delegate used to populate
        ///     a custom class object with data from the reader.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="T:System.Type" /> of object that is created
        ///     and populated.
        /// </typeparam>
        /// <returns>The function delegate.</returns>
        protected virtual Func<T> GetReadRecordFunc<T>()
        {
            var type = typeof(T);
            CreateReadRecordFunc(type);
            return (Func<T>) recordFuncs[type];
        }

        /// <summary>
        ///     Gets the function delegate used to populate
        ///     a custom class object with data from the reader.
        /// </summary>
        /// <param name="recordType">
        ///     The <see cref="T:System.Type" /> of object that is created
        ///     and populated.
        /// </param>
        /// <returns>The function delegate.</returns>
        protected virtual Delegate GetReadRecordFunc(Type recordType)
        {
            CreateReadRecordFunc(recordType);
            return recordFuncs[recordType];
        }

        /// <summary>
        ///     Determines whether the current record is empty.
        ///     A record is considered empty if all fields are empty.
        /// </summary>
        /// <param name="checkHasBeenRead">
        ///     True to check if the record
        ///     has been read, otherwise false.
        /// </param>
        /// <returns>
        ///     <c>true</c> if [is record empty]; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsRecordEmpty(bool checkHasBeenRead)
        {
            CheckDisposed();
            if (checkHasBeenRead)
                CheckHasBeenRead();
            if (currentRecord == null)
                return false;
            return currentRecord.All(GetEmtpyStringMethod());
        }

        /// <summary>
        ///     Parses the named indexes from the header record.
        /// </summary>
        protected virtual void ParseNamedIndexes()
        {
            if (headerRecord == null)
                throw new CsvReaderException("No header record was found.");
            for (int i = 0; i < headerRecord.Length; i++)
            {
                string lower = headerRecord[i];
                if (!Configuration.IsHeaderCaseSensitive)
                    lower = lower.ToLower();
                if (!namedIndexes.ContainsKey(lower))
                    namedIndexes[lower] = new List<int>
                    {
                        i
                    };
                else
                    namedIndexes[lower].Add(i);
            }
        }

        /// <summary>
        ///     Checks if the current record should be skipped or not.
        /// </summary>
        /// <returns><c>true</c> if the current record should be skipped, <c>false</c> otherwise.</returns>
        protected virtual bool ShouldSkipRecord()
        {
            CheckDisposed();
            if (currentRecord == null)
                return false;
            if (configuration.ShouldSkipRecord != null)
                return configuration.ShouldSkipRecord(currentRecord);
            if (!configuration.SkipEmptyRecords)
                return false;
            return IsRecordEmpty(false);
        }
    }
}