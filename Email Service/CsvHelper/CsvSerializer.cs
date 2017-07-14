using System;
using System.IO;
using CsvHelper.Configuration;

namespace CsvHelper
{
    /// <summary>
    ///     Defines methods used to serialize data into a CSV file.
    /// </summary>
    public class CsvSerializer : ICsvSerializer, IDisposable
    {
        private bool disposed;

        private TextWriter writer;

        /// <summary>
        ///     Creates a new serializer using the given <see cref="T:System.IO.TextWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.IO.TextWriter" /> to write the CSV file data to.</param>
        public CsvSerializer(TextWriter writer) : this(writer, new CsvConfiguration())
        {
        }

        /// <summary>
        ///     Creates a new serializer using the given <see cref="T:System.IO.TextWriter" />
        ///     and <see cref="T:CsvHelper.Configuration.CsvConfiguration" />.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.IO.TextWriter" /> to write the CSV file data to.</param>
        /// <param name="configuration">The configuration.</param>
        public CsvSerializer(TextWriter writer, CsvConfiguration configuration)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            this.writer = writer;
            Configuration = configuration;
        }

        /// <summary>
        ///     Gets the configuration.
        /// </summary>
        public CsvConfiguration Configuration { get; }

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
        ///     Writes a record to the CSV file.
        /// </summary>
        /// <param name="record">The record to write.</param>
        public void Write(string[] record)
        {
            CheckDisposed();
            string str = string.Join(Configuration.Delimiter, record);
            writer.WriteLine(str);
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
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if the instance needs to be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing && writer != null)
                writer.Dispose();
            disposed = true;
            writer = null;
        }
    }
}