using System;
using CsvHelper.Configuration;

namespace CsvHelper
{
    /// <summary>
    ///     Defines methods used to read parsed data
    ///     from a CSV file.
    /// </summary>
    public interface ICsvReader : ICsvReaderRow, IDisposable
    {
        /// <summary>
        ///     Gets or sets the configuration.
        /// </summary>
        CsvConfiguration Configuration { get; }

        /// <summary>
        ///     Gets the field headers.
        /// </summary>
        string[] FieldHeaders { get; }

        /// <summary>
        ///     Gets the parser.
        /// </summary>
        ICsvParser Parser { get; }

        /// <summary>
        ///     Advances the reader to the next record. If the header hasn't been read
        ///     yet, it'll automatically be read along with the first record.
        /// </summary>
        /// <returns>True if there are more records, otherwise false.</returns>
        bool Read();

        /// <summary>
        ///     Reads the header field without reading the first row.
        /// </summary>
        /// <returns>True if there are more records, otherwise false.</returns>
        bool ReadHeader();
    }
}