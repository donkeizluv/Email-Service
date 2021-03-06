using System.IO;
using CsvHelper.Configuration;

namespace CsvHelper
{
    /// <summary>
    ///     Creates CsvHelper classes.
    /// </summary>
    public class CsvFactory : ICsvFactory
    {
        /// <summary>
        ///     Creates an <see cref="T:CsvHelper.ICsvParser" />.
        /// </summary>
        /// <param name="reader">The text reader to use for the csv parser.</param>
        /// <param name="configuration">The configuration to use for the csv parser.</param>
        /// <returns>The created parser.</returns>
        public virtual ICsvParser CreateParser(TextReader reader, CsvConfiguration configuration)
        {
            return new CsvParser(reader, configuration);
        }

        /// <summary>
        ///     Creates an <see cref="T:CsvHelper.ICsvParser" />.
        /// </summary>
        /// <param name="reader">The text reader to use for the csv parser.</param>
        /// <returns>The created parser.</returns>
        public virtual ICsvParser CreateParser(TextReader reader)
        {
            return new CsvParser(reader);
        }

        /// <summary>
        ///     Creates an <see cref="T:CsvHelper.ICsvReader" />.
        /// </summary>
        /// <param name="reader">The text reader to use for the csv reader.</param>
        /// <param name="configuration">The configuration to use for the reader.</param>
        /// <returns>The created reader.</returns>
        public virtual ICsvReader CreateReader(TextReader reader, CsvConfiguration configuration)
        {
            return new CsvReader(reader, configuration);
        }

        /// <summary>
        ///     Creates an <see cref="T:CsvHelper.ICsvReader" />.
        /// </summary>
        /// <param name="reader">The text reader to use for the csv reader.</param>
        /// <returns>The created reader.</returns>
        public virtual ICsvReader CreateReader(TextReader reader)
        {
            return new CsvReader(reader);
        }

        /// <summary>
        ///     Creates an <see cref="T:CsvHelper.ICsvReader" />.
        /// </summary>
        /// <param name="parser">The parser used to create the reader.</param>
        /// <returns>The created reader.</returns>
        public virtual ICsvReader CreateReader(ICsvParser parser)
        {
            return new CsvReader(parser);
        }

        /// <summary>
        ///     Creates an <see cref="T:CsvHelper.ICsvWriter" />.
        /// </summary>
        /// <param name="writer">The text writer to use for the csv writer.</param>
        /// <param name="configuration">The configuration to use for the writer.</param>
        /// <returns>The created writer.</returns>
        public virtual ICsvWriter CreateWriter(TextWriter writer, CsvConfiguration configuration)
        {
            return new CsvWriter(writer, configuration);
        }

        /// <summary>
        ///     Creates an <see cref="T:CsvHelper.ICsvWriter" />.
        /// </summary>
        /// <param name="writer">The text writer to use for the csv writer.</param>
        /// <returns>The created writer.</returns>
        public virtual ICsvWriter CreateWriter(TextWriter writer)
        {
            return new CsvWriter(writer);
        }
    }
}