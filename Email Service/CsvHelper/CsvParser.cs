using System;
using System.IO;
using System.Linq;
using CsvHelper.Configuration;

namespace CsvHelper
{
    /// <summary>
    ///     Parses a CSV file.
    /// </summary>
    public class CsvParser : ICsvParser, IDisposable
    {
        private readonly CsvConfiguration configuration;

        private readonly char[] readerBuffer;

        private char c;

        private int charsRead;

        private char? cPrev;

        private int currentRawRow;

        private int currentRow;
        private bool disposed;

        private bool hasExcelSeparatorBeenRead;

        private string JustDecompileGenerated_RawRecord_k__BackingField;

        private bool read;

        private TextReader reader;

        private int readerBufferPosition;

        private string[] record;

        /// <summary>
        ///     Creates a new parser using the given <see cref="T:System.IO.TextReader" />.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.IO.TextReader" /> with the CSV file data.</param>
        public CsvParser(TextReader reader) : this(reader, new CsvConfiguration())
        {
        }

        /// <summary>
        ///     Creates a new parser using the given <see cref="T:System.IO.TextReader" />
        ///     and <see cref="T:CsvHelper.Configuration.CsvConfiguration" />.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.IO.TextReader" /> with the CSV file data.</param>
        /// <param name="configuration">The configuration.</param>
        public CsvParser(TextReader reader, CsvConfiguration configuration)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            this.reader = reader;
            this.configuration = configuration;
            readerBuffer = new char[configuration.BufferSize];
        }

        /// <summary>
        ///     Gets the row of the CSV file that the parser is currently on.
        ///     This is the actual file row.
        /// </summary>
        public virtual int RawRow
        {
            get { return currentRawRow; }
        }

        /// <summary>
        ///     Gets the byte position that the parser is currently on.
        /// </summary>
        public virtual long BytePosition { get; protected set; }

        /// <summary>
        ///     Gets the character position that the parser is currently on.
        /// </summary>
        public virtual long CharPosition { get; protected set; }

        /// <summary>
        ///     Gets the configuration.
        /// </summary>
        public virtual CsvConfiguration Configuration
        {
            get { return configuration; }
        }

        /// <summary>
        ///     Gets the field count.
        /// </summary>
        public virtual int FieldCount { get; protected set; }

        /// <summary>
        ///     Gets the raw row for the current record that was parsed.
        /// </summary>
        public string RawRecord
        {
            get { return JustDecompileGenerated_get_RawRecord(); }
            set { JustDecompileGenerated_set_RawRecord(value); }
        }

        /// <summary>
        ///     Gets the row of the CSV file that the parser is currently on.
        ///     This is the logical CSV row.
        /// </summary>
        public virtual int Row
        {
            get { return currentRow; }
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
        ///     Reads a record from the CSV file.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.List`1" /> of fields for the record read.
        ///     If there are no more records, null is returned.
        /// </returns>
        public virtual string[] Read()
        {
            string[] strArrays;
            CheckDisposed();
            try
            {
                if (configuration.HasExcelSeparator && !hasExcelSeparatorBeenRead)
                    ReadExcelSeparator();
                var strArrays1 = ReadLine();
                if (configuration.DetectColumnCountChanges && strArrays1 != null && FieldCount > 0)
                {
                    if (FieldCount == strArrays1.Length)
                        if (!strArrays1.Any(field => field == null))
                            goto Label0;
                    throw new CsvBadDataException("An inconsistent number of columns has been detected.");
                }
                Label0:
                strArrays = strArrays1;
            }
            catch (Exception exception)
            {
                ExceptionHelper.AddExceptionDataMessage(exception, this, null, null, null, null);
                throw;
            }
            return strArrays;
        }

        public virtual string JustDecompileGenerated_get_RawRecord()
        {
            return JustDecompileGenerated_RawRecord_k__BackingField;
        }

        private void JustDecompileGenerated_set_RawRecord(string value)
        {
            JustDecompileGenerated_RawRecord_k__BackingField = value;
        }

        /// <summary>
        ///     Adds the field to the current record.
        /// </summary>
        /// <param name="recordPosition">The record position to add the field to.</param>
        /// <param name="field">The field to add.</param>
        protected virtual void AddFieldToRecord(ref int recordPosition, string field, ref bool fieldIsBad)
        {
            if (record.Length < recordPosition + 1)
            {
                Array.Resize(ref record, recordPosition + 1);
                if (currentRow == 1)
                    FieldCount = record.Length;
            }
            if (fieldIsBad && configuration.ThrowOnBadData)
                throw new CsvBadDataException(string.Format("Field: '{0}'", field));
            if (fieldIsBad && configuration.BadDataCallback != null)
                configuration.BadDataCallback(field);
            fieldIsBad = false;
            record[recordPosition] = field;
            recordPosition = recordPosition + 1;
        }

        /// <summary>
        ///     Appends the current buffer data to the field.
        /// </summary>
        /// <param name="field">The field to append the current buffer to.</param>
        /// <param name="fieldStartPosition">The start position in the buffer that the .</param>
        /// <param name="length">The length.</param>
        protected virtual void AppendField(ref string field, int fieldStartPosition, int length)
        {
            field = string.Concat(field, new string(readerBuffer, fieldStartPosition, length));
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
            if (disposing && reader != null)
                reader.Dispose();
            disposed = true;
            reader = null;
        }

        /// <summary>
        ///     Gets the current character from the buffer while
        ///     advancing the buffer if it ran out.
        /// </summary>
        /// <param name="ch">The char that gets the read char set to.</param>
        /// <param name="fieldStartPosition">The start position of the current field.</param>
        /// <param name="rawFieldStartPosition">The start position of the raw field.</param>
        /// <param name="field">The field.</param>
        /// <param name="prevCharWasDelimiter">A value indicating if the previous char read was a delimiter.</param>
        /// <param name="recordPosition">The position in the record we are currently at.</param>
        /// <param name="fieldLength">The length of the field in the buffer.</param>
        /// <param name="inComment">A value indicating if the row is current a comment row.</param>
        /// <param name="isPeek">
        ///     A value indicating if this call is a peek. If true and the end of the record was found
        ///     no record handling will be done.
        /// </param>
        /// <returns>A value indicating if read a char was read. True if a char was read, otherwise false.</returns>
        protected bool GetChar(out char ch, ref int fieldStartPosition, ref int rawFieldStartPosition, ref string field,
            ref bool fieldIsBad, bool prevCharWasDelimiter, ref int recordPosition, ref int fieldLength, bool inComment,
            bool inDelimiter, bool inQuotes, bool isPeek)
        {
            if (readerBufferPosition == charsRead)
            {
                if (!inDelimiter && !inComment)
                    AppendField(ref field, fieldStartPosition, fieldLength);
                UpdateBytePosition(fieldStartPosition, readerBufferPosition - fieldStartPosition);
                fieldLength = 0;
                RawRecord = string.Concat(RawRecord,
                    new string(readerBuffer, rawFieldStartPosition, readerBufferPosition - rawFieldStartPosition));
                charsRead = reader.Read(readerBuffer, 0, readerBuffer.Length);
                readerBufferPosition = 0;
                fieldStartPosition = 0;
                rawFieldStartPosition = 0;
                if (charsRead == 0)
                {
                    if (isPeek)
                    {
                        ch = '\0';
                        return false;
                    }
                    if (!((c == '\r' || c == '\n' || c == 0 ? false : !inComment) | inQuotes))
                    {
                        RawRecord = null;
                        record = null;
                    }
                    else
                    {
                        if (prevCharWasDelimiter)
                            field = "";
                        AddFieldToRecord(ref recordPosition, field, ref fieldIsBad);
                    }
                    ch = '\0';
                    return false;
                }
            }
            ch = readerBuffer[readerBufferPosition];
            return true;
        }

        /// <summary>
        ///     Reads the Excel seperator and sets it to the delimiter.
        /// </summary>
        protected virtual void ReadExcelSeparator()
        {
            string str = reader.ReadLine();
            if (str != null)
                configuration.Delimiter = str.Substring(4);
            hasExcelSeparatorBeenRead = true;
        }

        /// <summary>
        ///     Reads the next line.
        /// </summary>
        /// <returns>The line separated into fields.</returns>
        protected virtual string[] ReadLine()
        {
            int? nullable;
            int quote;
            char? nullable1;
            int? nullable2;
            char chr;
            char chr1;
            char chr2;
            int? nullable3;
            int? nullable4;
            bool flag;
            int? nullable5;
            int? nullable6;
            int? nullable7;
            int? nullable8;
            int? nullable9;
            int? nullable10;
            int? nullable11;
            int? nullable12;
            int? nullable13;
            int? nullable14;
            int? nullable15;
            string str = null;
            int num = readerBufferPosition;
            int num1 = readerBufferPosition;
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            bool flag5 = false;
            bool flag6 = false;
            int num2 = 0;
            bool flag7 = false;
            int num3 = 0;
            record = new string[FieldCount];
            RawRecord = string.Empty;
            currentRow = currentRow + 1;
            currentRawRow = currentRawRow + 1;
            while (true)
            {
                if (read)
                    cPrev = c;
                int num4 = readerBufferPosition - num;
                read = GetChar(out c, ref num, ref num1, ref str, ref flag3, flag7, ref num3, ref num4, flag4, flag5,
                    flag1, false);
                if (!read)
                    break;
                readerBufferPosition = readerBufferPosition + 1;
                CharPosition = CharPosition + 1;
                if (configuration.UseExcelLeadingZerosFormatForNumerics)
                {
                    if (c == '=' && !flag6)
                    {
                        if (!flag7)
                        {
                            nullable1 = cPrev;
                            if (nullable1.HasValue)
                            {
                                nullable14 = nullable1.GetValueOrDefault();
                            }
                            else
                            {
                                nullable2 = null;
                                nullable14 = nullable2;
                            }
                            nullable = nullable14;
                            if (nullable.GetValueOrDefault() == 13 ? !nullable.HasValue : true)
                            {
                                nullable1 = cPrev;
                                if (nullable1.HasValue)
                                {
                                    nullable15 = nullable1.GetValueOrDefault();
                                }
                                else
                                {
                                    nullable2 = null;
                                    nullable15 = nullable2;
                                }
                                nullable = nullable15;
                                if ((nullable.GetValueOrDefault() == 10 ? !nullable.HasValue : true) && cPrev.HasValue)
                                    goto Label1;
                            }
                        }
                        num4 = readerBufferPosition - num;
                        GetChar(out chr, ref num, ref num1, ref str, ref flag3, flag7, ref num3, ref num4, flag4, flag5,
                            flag1, true);
                        if (chr != '\"')
                            goto Label0;
                        flag6 = true;
                        continue;
                    }
                    Label1:
                    if (!flag6)
                        goto Label0;
                    if (c == '\"')
                    {
                        nullable1 = cPrev;
                        if (nullable1.HasValue)
                        {
                            nullable13 = nullable1.GetValueOrDefault();
                        }
                        else
                        {
                            nullable2 = null;
                            nullable13 = nullable2;
                        }
                        nullable = nullable13;
                        if (nullable.GetValueOrDefault() == 61 ? nullable.HasValue : false)
                            continue;
                    }
                    if (!char.IsDigit(c))
                        if (c != '\"')
                        {
                            flag6 = false;
                            continue;
                        }
                        else
                        {
                            bool chr3 = GetChar(out chr1, ref num, ref num1, ref str, ref flag3, flag7, ref num3,
                                ref num4, flag4, flag5, flag1, true);
                            if (chr1 == configuration.Delimiter[0] || chr1 == '\r' || chr1 == '\n' || chr1 == 0)
                            {
                                AppendField(ref str, num, readerBufferPosition - num);
                                UpdateBytePosition(num, readerBufferPosition - num);
                                str = str.Trim('=', '\"');
                                num = readerBufferPosition;
                                if (!chr3)
                                    AddFieldToRecord(ref num3, str, ref flag3);
                                flag6 = false;
                                continue;
                            }
                        }
                }
                Label0:
                if (c != configuration.Quote || configuration.IgnoreQuotes)
                {
                    flag7 = false;
                    if (!(flag2 & flag1))
                    {
                        nullable1 = cPrev;
                        if (nullable1.HasValue)
                        {
                            nullable3 = nullable1.GetValueOrDefault();
                        }
                        else
                        {
                            nullable2 = null;
                            nullable3 = nullable2;
                        }
                        nullable = nullable3;
                        quote = configuration.Quote;
                        if ((nullable.GetValueOrDefault() == quote ? nullable.HasValue : false) &&
                            !configuration.IgnoreQuotes)
                        {
                            if (c != configuration.Delimiter[0] && c != '\r' && c != '\n')
                                flag3 = true;
                            flag2 = false;
                        }
                        if (!flag4 || c == '\r' || c == '\n')
                            if ((c == configuration.Delimiter[0]) | flag5)
                            {
                                if (!flag5)
                                {
                                    num2 = 0;
                                    AppendField(ref str, num, readerBufferPosition - num - 1);
                                    UpdateBytePosition(num, readerBufferPosition - num);
                                    AddFieldToRecord(ref num3, str, ref flag3);
                                    num = readerBufferPosition;
                                    str = null;
                                    flag5 = true;
                                }
                                if (num2 == configuration.Delimiter.Length - 1)
                                {
                                    UpdateBytePosition(num, readerBufferPosition - num);
                                    flag5 = false;
                                    flag7 = true;
                                    num = readerBufferPosition;
                                }
                                else if (configuration.Delimiter[num2] == c)
                                {
                                    num2++;
                                }
                                else
                                {
                                    num3--;
                                    num = num - (num2 + 1);
                                    flag5 = false;
                                }
                            }
                            else if (c == '\r' || c == '\n')
                            {
                                num4 = readerBufferPosition - num - 1;
                                if (c == '\r')
                                {
                                    GetChar(out chr2, ref num, ref num1, ref str, ref flag3, flag7, ref num3, ref num4,
                                        flag4, flag5, flag1, true);
                                    if (chr2 == '\n')
                                    {
                                        readerBufferPosition = readerBufferPosition + 1;
                                        CharPosition = CharPosition + 1;
                                    }
                                }
                                nullable1 = cPrev;
                                if (nullable1.HasValue)
                                {
                                    nullable4 = nullable1.GetValueOrDefault();
                                }
                                else
                                {
                                    nullable2 = null;
                                    nullable4 = nullable2;
                                }
                                nullable = nullable4;
                                if (nullable.GetValueOrDefault() == 13 ? nullable.HasValue : false)
                                {
                                    flag = true;
                                }
                                else
                                {
                                    nullable1 = cPrev;
                                    if (nullable1.HasValue)
                                    {
                                        nullable5 = nullable1.GetValueOrDefault();
                                    }
                                    else
                                    {
                                        nullable2 = null;
                                        nullable5 = nullable2;
                                    }
                                    nullable = nullable5;
                                    flag = nullable.GetValueOrDefault() == 10 ? nullable.HasValue : false;
                                }
                                if (flag | flag4 || !cPrev.HasValue)
                                {
                                    UpdateBytePosition(num, readerBufferPosition - num);
                                    num = readerBufferPosition;
                                    flag4 = false;
                                    if (!configuration.IgnoreBlankLines)
                                        break;
                                    currentRow = currentRow + 1;
                                }
                                else
                                {
                                    AppendField(ref str, num, num4);
                                    UpdateBytePosition(num, readerBufferPosition - num);
                                    AddFieldToRecord(ref num3, str, ref flag3);
                                    break;
                                }
                            }
                            else if (configuration.AllowComments && c == configuration.Comment)
                            {
                                nullable1 = cPrev;
                                if (nullable1.HasValue)
                                {
                                    nullable6 = nullable1.GetValueOrDefault();
                                }
                                else
                                {
                                    nullable2 = null;
                                    nullable6 = nullable2;
                                }
                                nullable = nullable6;
                                if (nullable.GetValueOrDefault() == 13 ? !nullable.HasValue : true)
                                {
                                    nullable1 = cPrev;
                                    if (nullable1.HasValue)
                                    {
                                        nullable7 = nullable1.GetValueOrDefault();
                                    }
                                    else
                                    {
                                        nullable2 = null;
                                        nullable7 = nullable2;
                                    }
                                    nullable = nullable7;
                                    if ((nullable.GetValueOrDefault() == 10 ? !nullable.HasValue : true) &&
                                        cPrev.HasValue)
                                        continue;
                                }
                                flag4 = true;
                            }
                    }
                    else
                    {
                        if (c != '\r')
                            if (c == '\n')
                            {
                                nullable1 = cPrev;
                                if (nullable1.HasValue)
                                {
                                    nullable8 = nullable1.GetValueOrDefault();
                                }
                                else
                                {
                                    nullable2 = null;
                                    nullable8 = nullable2;
                                }
                                nullable = nullable8;
                                if (nullable.GetValueOrDefault() == 13 ? nullable.HasValue : false)
                                    continue;
                            }
                        currentRawRow = currentRawRow + 1;
                    }
                }
                else
                {
                    if (!flag2)
                    {
                        if (!flag7)
                        {
                            nullable1 = cPrev;
                            if (nullable1.HasValue)
                            {
                                nullable11 = nullable1.GetValueOrDefault();
                            }
                            else
                            {
                                nullable2 = null;
                                nullable11 = nullable2;
                            }
                            nullable = nullable11;
                            if (nullable.GetValueOrDefault() == 13 ? !nullable.HasValue : true)
                            {
                                nullable1 = cPrev;
                                if (nullable1.HasValue)
                                {
                                    nullable12 = nullable1.GetValueOrDefault();
                                }
                                else
                                {
                                    nullable2 = null;
                                    nullable12 = nullable2;
                                }
                                nullable = nullable12;
                                if ((nullable.GetValueOrDefault() == 10 ? !nullable.HasValue : true) && cPrev.HasValue)
                                    goto Label2;
                            }
                        }
                        flag2 = true;
                    }
                    Label2:
                    if (flag2)
                    {
                        flag1 = !flag1;
                        if (num != readerBufferPosition - 1)
                        {
                            AppendField(ref str, num, readerBufferPosition - num - 1);
                            UpdateBytePosition(num, readerBufferPosition - num);
                        }
                        nullable1 = cPrev;
                        if (nullable1.HasValue)
                        {
                            nullable9 = nullable1.GetValueOrDefault();
                        }
                        else
                        {
                            nullable2 = null;
                            nullable9 = nullable2;
                        }
                        nullable = nullable9;
                        quote = configuration.Quote;
                        if ((nullable.GetValueOrDefault() == quote ? !nullable.HasValue : true) || !flag1)
                        {
                            if (!flag1)
                            {
                                nullable1 = cPrev;
                                if (nullable1.HasValue)
                                {
                                    nullable10 = nullable1.GetValueOrDefault();
                                }
                                else
                                {
                                    nullable2 = null;
                                    nullable10 = nullable2;
                                }
                                nullable = nullable10;
                                quote = configuration.Quote;
                                if ((nullable.GetValueOrDefault() == quote ? !nullable.HasValue : true) &&
                                    readerBufferPosition != 1)
                                    goto Label3;
                            }
                            UpdateBytePosition(num, readerBufferPosition - num);
                            Label3:
                            num = readerBufferPosition;
                        }
                        flag7 = false;
                    }
                    else
                    {
                        flag3 = true;
                    }
                }
            }
            if (record != null)
                RawRecord = string.Concat(RawRecord, new string(readerBuffer, num1, readerBufferPosition - num1));
            return record;
        }

        /// <summary>
        ///     Updates the byte position using the data from the reader buffer.
        /// </summary>
        /// <param name="fieldStartPosition">The field start position.</param>
        /// <param name="length">The length.</param>
        protected virtual void UpdateBytePosition(int fieldStartPosition, int length)
        {
            if (configuration.CountBytes)
                BytePosition = BytePosition +
                               configuration.Encoding.GetByteCount(readerBuffer, fieldStartPosition, length);
        }
    }
}