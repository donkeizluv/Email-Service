using System;
using System.Collections;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CsvHelper
{
    /// <summary>
    ///     Defines methods used to write to a CSV file.
    /// </summary>
    public interface ICsvWriter : IDisposable
    {
        /// <summary>
        ///     Gets or sets the configuration.
        /// </summary>
        CsvConfiguration Configuration { get; }

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
        void ClearRecordCache<T>();

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
        void ClearRecordCache(Type type);

        /// <summary>
        ///     Clears the record cache for all types. After <see cref="M:CsvHelper.ICsvWriter.WriteRecord``1(``0)" /> is called
        ///     the
        ///     first time, code is dynamically generated based on the
        ///     <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />,
        ///     compiled, and stored for the given type T. If the <see cref="T:CsvHelper.Configuration.CsvPropertyMapCollection" />
        ///     changes, <see cref="M:CsvHelper.ICsvWriter.ClearRecordCache" /> needs to be called to update the
        ///     record cache.
        /// </summary>
        void ClearRecordCache();

        /// <summary>
        ///     Ends writing of the current record
        ///     and starts a new record. This is used
        ///     when manually writing records with <see cref="M:CsvHelper.ICsvWriter.WriteField``1(``0)" />
        /// </summary>
        void NextRecord();

        /// <summary>
        ///     Write the Excel seperator record.
        /// </summary>
        void WriteExcelSeparator();

        /// <summary>
        ///     Writes the field to the CSV file. The field
        ///     may get quotes added to it.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <param name="field">The field to write.</param>
        void WriteField(string field);

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
        void WriteField(string field, bool shouldQuote);

        /// <summary>
        ///     Writes the field to the CSV file.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to write.</param>
        void WriteField<T>(T field);

        /// <summary>
        ///     Writes the field to the CSV file.
        ///     When all fields are written for a record,
        ///     <see cref="M:CsvHelper.ICsvWriter.NextRecord" /> must be called
        ///     to complete writing of the current record.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">The field to write.</param>
        /// <param name="converter">The converter used to convert the field into a string.</param>
        void WriteField<T>(T field, ITypeConverter converter);

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
        void WriteField<T, TConverter>(T field);

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
        void WriteField(Type type, object field);

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
        void WriteField(Type type, object field, ITypeConverter converter);

        /// <summary>
        ///     Writes the header record from the given properties.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        void WriteHeader<T>();

        /// <summary>
        ///     Writes the header record from the given properties.
        /// </summary>
        /// <param name="type">The type of the record.</param>
        void WriteHeader(Type type);

        /// <summary>
        ///     Writes the record to the CSV file.
        /// </summary>
        /// <typeparam name="T">The type of the record.</typeparam>
        /// <param name="record">The record to write.</param>
        void WriteRecord<T>(T record);

        /// <summary>
        ///     Writes the record to the CSV file.
        /// </summary>
        /// <param name="type">The type of the record.</param>
        /// <param name="record">The record to write.</param>
        [Obsolete(
            "This method is deprecated and will be removed in the next major release. Use WriteRecord<T>( T record ) instead.",
            false)]
        void WriteRecord(Type type, object record);

        /// <summary>
        ///     Writes the list of records to the CSV file.
        /// </summary>
        /// <param name="records">The list of records to write.</param>
        void WriteRecords(IEnumerable records);
    }
}