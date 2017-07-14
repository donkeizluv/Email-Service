#region Copyright

//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of
// which has been included with this distribution in the LICENSE.txt file.
//

#endregion Copyright

using System;
using System.IO;
using System.Text;

namespace AnEmailService.Nini.Ini
{
    /// <include file='IniWriter.xml' path='//Class[@name="IniWriter"]/docs/*' />
    public class IniWriter : IDisposable
    {
        #region Protected methods

        /// <include file='IniWriter.xml' path='//Method[@name="DisposeBoolean"]/docs/*' />
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (textWriter != null) textWriter.Close();
                if (BaseStream != null) BaseStream.Close();
                disposed = true;

                if (disposing) GC.SuppressFinalize(this);
            }
        }

        #endregion Protected methods

        #region Private variables

        private int indentation;
        private readonly TextWriter textWriter;
        private readonly string eol = "\r\n";
        private readonly StringBuilder indentationBuffer = new StringBuilder();
        private bool disposed;

        #endregion Private variables

        #region Public properties

        /// <include file='IniWriter.xml' path='//Property[@name="Indentation"]/docs/*' />
        public int Indentation
        {
            get { return indentation; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Negative values are illegal");

                indentation = value;
                indentationBuffer.Remove(0, indentationBuffer.Length);
                for (int i = 0; i < value; i++)
                    indentationBuffer.Append(' ');
            }
        }

        /// <include file='IniWriter.xml' path='//Property[@name="UseValueQuotes"]/docs/*' />
        public bool UseValueQuotes { get; set; } = false;

        /// <include file='IniWriter.xml' path='//Property[@name="WriteState"]/docs/*' />
        public IniWriteState WriteState { get; private set; } = IniWriteState.Start;

        /// <include file='IniWriter.xml' path='//Property[@name="CommentDelimiter"]/docs/*' />
        public char CommentDelimiter { get; set; } = ';';

        /// <include file='IniWriter.xml' path='//Property[@name="AssignDelimiter"]/docs/*' />
        public char AssignDelimiter { get; set; } = '=';

        /// <include file='IniWriter.xml' path='//Property[@name="BaseStream"]/docs/*' />
        public Stream BaseStream { get; }

        #endregion Public properties

        #region Constructors

        /// <include file='IniWriter.xml' path='//Constructor[@name="ConstructorPath"]/docs/*' />
        public IniWriter(string filePath)
            : this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
        }

        /// <include file='IniWriter.xml' path='//Constructor[@name="ConstructorTextWriter"]/docs/*' />
        public IniWriter(TextWriter writer)
        {
            textWriter = writer;
            var streamWriter = writer as StreamWriter;
            if (streamWriter != null) BaseStream = streamWriter.BaseStream;
        }

        /// <include file='IniWriter.xml' path='//Constructor[@name="ConstructorStream"]/docs/*' />
        public IniWriter(Stream stream)
            : this(new StreamWriter(stream))
        {
        }

        #endregion Constructors

        #region Public methods

        /// <include file='IniWriter.xml' path='//Method[@name="Close"]/docs/*' />
        public void Close()
        {
            textWriter.Close();
            WriteState = IniWriteState.Closed;
        }

        /// <include file='IniWriter.xml' path='//Method[@name="Flush"]/docs/*' />
        public void Flush()
        {
            textWriter.Flush();
        }

        /// <include file='IniWriter.xml' path='//Method[@name="ToString"]/docs/*' />
        public override string ToString()
        {
            return textWriter.ToString();
        }

        /// <include file='IniWriter.xml' path='//Method[@name="WriteSection"]/docs/*' />
        public void WriteSection(string section)
        {
            ValidateState();
            WriteState = IniWriteState.Section;
            WriteLine("[" + section + "]");
        }

        /// <include file='IniWriter.xml' path='//Method[@name="WriteSectionComment"]/docs/*' />
        public void WriteSection(string section, string comment)
        {
            ValidateState();
            WriteState = IniWriteState.Section;
            WriteLine("[" + section + "]" + Comment(comment));
        }

        /// <include file='IniWriter.xml' path='//Method[@name="WriteKey"]/docs/*' />
        public void WriteKey(string key, string value)
        {
            ValidateStateKey();
            WriteLine(key + " " + AssignDelimiter + " " + GetKeyValue(value));
        }

        /// <include file='IniWriter.xml' path='//Method[@name="WriteKeyComment"]/docs/*' />
        public void WriteKey(string key, string value, string comment)
        {
            ValidateStateKey();
            WriteLine(key + " " + AssignDelimiter + " " + GetKeyValue(value) + Comment(comment));
        }

        /// <include file='IniWriter.xml' path='//Method[@name="WriteEmpty"]/docs/*' />
        public void WriteEmpty()
        {
            ValidateState();
            if (WriteState == IniWriteState.Start) WriteState = IniWriteState.BeforeFirstSection;
            WriteLine("");
        }

        /// <include file='IniWriter.xml' path='//Method[@name="WriteEmptyComment"]/docs/*' />
        public void WriteEmpty(string comment)
        {
            ValidateState();
            if (WriteState == IniWriteState.Start) WriteState = IniWriteState.BeforeFirstSection;
            if (comment == null) WriteLine("");
            else WriteLine(CommentDelimiter + " " + comment);
        }

        /// <include file='IniWriter.xml' path='//Method[@name="Dispose"]/docs/*' />
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion Public methods

        #region Private methods

        /// <summary>
        ///     Destructor.
        /// </summary>
        ~IniWriter()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Returns the value of a key.
        /// </summary>
        private string GetKeyValue(string text)
        {
            string result;

            if (UseValueQuotes) result = MassageValue('"' + text + '"');
            else result = MassageValue(text);

            return result;
        }

        /// <summary>
        ///     Validates whether a key can be written.
        /// </summary>
        private void ValidateStateKey()
        {
            ValidateState();

            switch (WriteState)
            {
                case IniWriteState.BeforeFirstSection:
                case IniWriteState.Start:
                    throw new InvalidOperationException("The WriteState is not Section");
                case IniWriteState.Closed:
                    throw new InvalidOperationException("The writer is closed");
            }
        }

        /// <summary>
        ///     Validates the state to determine if the item can be written.
        /// </summary>
        private void ValidateState()
        {
            if (WriteState == IniWriteState.Closed) throw new InvalidOperationException("The writer is closed");
        }

        /// <summary>
        ///     Returns a formatted comment.
        /// </summary>
        private string Comment(string text)
        {
            return text == null ? "" : " " + CommentDelimiter + " " + text;
        }

        /// <summary>
        ///     Writes data to the writer.
        /// </summary>
        private void Write(string value)
        {
            textWriter.Write(indentationBuffer + value);
        }

        /// <summary>
        ///     Writes a full line to the writer.
        /// </summary>
        private void WriteLine(string value)
        {
            Write(value + eol);
        }

        /// <summary>
        ///     Fixes the incoming value to prevent illegal characters from
        ///     hurting the integrity of the INI file.
        /// </summary>
        private string MassageValue(string text)
        {
            return text.Replace("\n", "");
        }

        #endregion Private methods
    }
}