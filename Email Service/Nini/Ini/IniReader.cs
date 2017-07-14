#region Copyright

//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of 
// which has been included with this distribution in the LICENSE.txt file.
// 

#endregion

using System;
using System.IO;
using System.Text;

namespace AnEmailService.Nini.Ini
{

    #region IniReadState enumeration

    #endregion

    #region IniType enumeration

    #endregion

    /// <include file='IniReader.xml' path='//Class[@name="IniReader"]/docs/*' />
    public class IniReader : IDisposable
    {
        #region Protected methods

        /// <include file='IniReader.xml' path='//Method[@name="DisposeBoolean"]/docs/*' />
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                textReader.Close();
                disposed = true;

                if (disposing) GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region Private variables

        private readonly TextReader textReader;
        private readonly StringBuilder name = new StringBuilder();
        private readonly StringBuilder value = new StringBuilder();
        private readonly StringBuilder comment = new StringBuilder();
        private bool hasComment;
        private bool disposed;
        private char[] commentDelimiters = {';'};
        private char[] assignDelimiters = {'='};

        #endregion

        #region Public properties

        /// <include file='IniReader.xml' path='//Property[@name="Name"]/docs/*' />
        public string Name
        {
            get { return name.ToString(); }
        }

        /// <include file='IniReader.xml' path='//Property[@name="Value"]/docs/*' />
        public string Value
        {
            get { return value.ToString(); }
        }

        /// <include file='IniReader.xml' path='//Property[@name="Type"]/docs/*' />
        public IniType Type { get; private set; } = IniType.Empty;

        /// <include file='IniReader.xml' path='//Property[@name="Comment"]/docs/*' />
        public string Comment
        {
            get { return hasComment ? comment.ToString() : null; }
        }

        /// <include file='IniReader.xml' path='//Property[@name="LineNumber"]/docs/*' />
        public int LineNumber { get; private set; } = 1;

        /// <include file='IniReader.xml' path='//Property[@name="LinePosition"]/docs/*' />
        public int LinePosition { get; private set; } = 1;

        /// <include file='IniReader.xml' path='//Property[@name="IgnoreComments"]/docs/*' />
        public bool IgnoreComments { get; set; } = false;

        /// <include file='IniReader.xml' path='//Property[@name="ReadState"]/docs/*' />
        public IniReadState ReadState { get; private set; } = IniReadState.Initial;

        /// <include file='IniReader.xml' path='//Property[@name="LineContinuation"]/docs/*' />
        public bool LineContinuation { get; set; } = false;

        /// <include file='IniReader.xml' path='//Property[@name="AcceptCommentAfterKey"]/docs/*' />
        public bool AcceptCommentAfterKey { get; set; } = true;

        /// <include file='IniReader.xml' path='//Property[@name="AcceptNoAssignmentOperator"]/docs/*' />
        public bool AcceptNoAssignmentOperator { get; set; } = false;

        /// <include file='IniReader.xml' path='//Property[@name="ConsumeAllKeyText"]/docs/*' />
        public bool ConsumeAllKeyText { get; set; } = false;

        #endregion

        #region Constructors

        /// <include file='IniReader.xml' path='//Constructor[@name="ConstructorPath"]/docs/*' />
        public IniReader(string filePath)
        {
            textReader = new StreamReader(filePath);
        }

        /// <include file='IniReader.xml' path='//Constructor[@name="ConstructorTextReader"]/docs/*' />
        public IniReader(TextReader reader)
        {
            textReader = reader;
        }

        /// <include file='IniReader.xml' path='//Constructor[@name="ConstructorStream"]/docs/*' />
        public IniReader(Stream stream)
            : this(new StreamReader(stream))
        {
        }

        #endregion

        #region Public methods

        /// <include file='IniReader.xml' path='//Method[@name="Read"]/docs/*' />
        public bool Read()
        {
            bool result = false;

            if (ReadState != IniReadState.EndOfFile
                || ReadState != IniReadState.Closed)
            {
                ReadState = IniReadState.Interactive;
                result = ReadNext();
            }

            return result;
        }

        /// <include file='IniReader.xml' path='//Method[@name="MoveToNextSection"]/docs/*' />
        public bool MoveToNextSection()
        {
            bool result = false;

            while (true)
            {
                result = Read();

                if (Type == IniType.Section || !result) break;
            }

            return result;
        }

        /// <include file='IniReader.xml' path='//Method[@name="MoveToNextKey"]/docs/*' />
        public bool MoveToNextKey()
        {
            bool result = false;

            while (true)
            {
                result = Read();

                if (Type == IniType.Section)
                {
                    result = false;
                    break;
                }
                if (Type == IniType.Key || !result) break;
            }

            return result;
        }

        /// <include file='IniReader.xml' path='//Method[@name="Close"]/docs/*' />
        public void Close()
        {
            Reset();
            ReadState = IniReadState.Closed;

            if (textReader != null) textReader.Close();
        }

        /// <include file='IniReader.xml' path='//Method[@name="Dispose"]/docs/*' />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <include file='IniReader.xml' path='//Method[@name="GetCommentDelimiters"]/docs/*' />
        public char[] GetCommentDelimiters()
        {
            var result = new char[commentDelimiters.Length];
            Array.Copy(commentDelimiters, 0, result, 0, commentDelimiters.Length);

            return result;
        }

        /// <include file='IniReader.xml' path='//Method[@name="SetCommentDelimiters"]/docs/*' />
        public void SetCommentDelimiters(char[] delimiters)
        {
            if (delimiters.Length < 1) throw new ArgumentException("Must supply at least one delimiter");

            commentDelimiters = delimiters;
        }

        /// <include file='IniReader.xml' path='//Method[@name="GetAssignDelimiters"]/docs/*' />
        public char[] GetAssignDelimiters()
        {
            var result = new char[assignDelimiters.Length];
            Array.Copy(assignDelimiters, 0, result, 0, assignDelimiters.Length);

            return result;
        }

        /// <include file='IniReader.xml' path='//Method[@name="SetAssignDelimiters"]/docs/*' />
        public void SetAssignDelimiters(char[] delimiters)
        {
            if (delimiters.Length < 1) throw new ArgumentException("Must supply at least one delimiter");

            assignDelimiters = delimiters;
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Destructor.
        /// </summary>
        ~IniReader()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Resets all of the current INI line data.
        /// </summary>
        private void Reset()
        {
            name.Remove(0, name.Length);
            value.Remove(0, value.Length);
            comment.Remove(0, comment.Length);
            Type = IniType.Empty;
            hasComment = false;
        }

        /// <summary>
        ///     Reads the next INI line item.
        /// </summary>
        private bool ReadNext()
        {
            bool result = true;
            int ch = PeekChar();
            Reset();

            if (IsComment(ch))
            {
                Type = IniType.Empty;
                ReadChar(); // consume comment character
                ReadComment();

                return result;
            }

            switch (ch)
            {
                case ' ':
                case '\t':
                case '\r':
                    SkipWhitespace();
                    ReadNext();
                    break;
                case '\n':
                    ReadChar();
                    break;
                case '[':
                    ReadSection();
                    break;
                case -1:
                    ReadState = IniReadState.EndOfFile;
                    result = false;
                    break;
                default:
                    ReadKey();
                    break;
            }

            return result;
        }

        /// <summary>
        ///     Reads a comment. Must start after the comment delimiter.
        /// </summary>
        private void ReadComment()
        {
            int ch = -1;
            SkipWhitespace();
            hasComment = true;

            do
            {
                ch = ReadChar();
                comment.Append((char) ch);
            } while (!EndOfLine(ch));

            RemoveTrailingWhitespace(comment);
        }

        /// <summary>
        ///     Removes trailing whitespace from a StringBuilder.
        /// </summary>
        private void RemoveTrailingWhitespace(StringBuilder builder)
        {
            string temp = builder.ToString();

            builder.Remove(0, builder.Length);
            builder.Append(temp.TrimEnd(null));
        }

        /// <summary>
        ///     Reads a key.
        /// </summary>
        private void ReadKey()
        {
            int ch = -1;
            Type = IniType.Key;

            while (true)
            {
                ch = PeekChar();

                if (IsAssign(ch))
                {
                    ReadChar();
                    break;
                }

                if (EndOfLine(ch))
                {
                    if (AcceptNoAssignmentOperator) break;
                    throw new IniException(this,
                        string.Format("Expected assignment operator ({0})",
                            assignDelimiters[0]));
                }

                name.Append((char) ReadChar());
            }

            ReadKeyValue();
            SearchForComment();
            RemoveTrailingWhitespace(name);
        }

        /// <summary>
        ///     Reads the value of a key.
        /// </summary>
        private void ReadKeyValue()
        {
            int ch = -1;
            bool foundQuote = false;
            int characters = 0;
            SkipWhitespace();

            while (true)
            {
                ch = PeekChar();

                if (!IsWhitespace(ch)) characters++;

                if (!ConsumeAllKeyText && ch == '"')
                {
                    ReadChar();

                    if (!foundQuote && characters == 1)
                    {
                        foundQuote = true;
                        continue;
                    }
                    break;
                }

                if (foundQuote && EndOfLine(ch)) throw new IniException(this, "Expected closing quote (\")");

                // Handle line continuation
                if (LineContinuation && ch == '\\')
                {
                    var buffer = new StringBuilder();
                    buffer.Append((char) ReadChar()); // append '\'

                    while (PeekChar() != '\n' && IsWhitespace(PeekChar()))
                        if (PeekChar() != '\r') buffer.Append((char) ReadChar());
                        else ReadChar(); // consume '\r'

                    if (PeekChar() == '\n')
                    {
                        // continue reading key value on next line
                        ReadChar();
                        continue;
                    }
                    // Replace consumed characters
                    value.Append(buffer);
                }

                if (!ConsumeAllKeyText) if (AcceptCommentAfterKey && IsComment(ch) && !foundQuote) break;

                // Always break at end of line
                if (EndOfLine(ch)) break;

                value.Append((char) ReadChar());
            }

            if (!foundQuote) RemoveTrailingWhitespace(value);
        }

        /// <summary>
        ///     Reads an INI section.
        /// </summary>
        private void ReadSection()
        {
            int ch = -1;
            Type = IniType.Section;
            ch = ReadChar(); // consume "["

            while (true)
            {
                ch = PeekChar();
                if (ch == ']') break;
                if (EndOfLine(ch)) throw new IniException(this, "Expected section end (])");

                name.Append((char) ReadChar());
            }

            ConsumeToEnd(); // all after '[' is garbage            
            RemoveTrailingWhitespace(name);
        }

        /// <summary>
        ///     Looks for a comment.
        /// </summary>
        private void SearchForComment()
        {
            int ch = ReadChar();

            while (!EndOfLine(ch))
            {
                if (IsComment(ch))
                {
                    if (IgnoreComments) ConsumeToEnd();
                    else ReadComment();
                    break;
                }
                ch = ReadChar();
            }
        }

        /// <summary>
        ///     Consumes all data until the end of a line.
        /// </summary>
        private void ConsumeToEnd()
        {
            int ch = -1;

            do
            {
                ch = ReadChar();
            } while (!EndOfLine(ch));
        }

        /// <summary>
        ///     Returns and consumes the next character from the stream.
        /// </summary>
        private int ReadChar()
        {
            int result = textReader.Read();

            if (result == '\n')
            {
                LineNumber++;
                LinePosition = 1;
            }
            else
            {
                LinePosition++;
            }

            return result;
        }

        /// <summary>
        ///     Returns the next upcoming character from the stream.
        /// </summary>
        private int PeekChar()
        {
            return textReader.Peek();
        }

        /// <summary>
        ///     Returns true if a comment character is found.
        /// </summary>
        private bool IsComment(int ch)
        {
            return HasCharacter(commentDelimiters, ch);
        }

        /// <summary>
        ///     Returns true if character is an assign character.
        /// </summary>
        private bool IsAssign(int ch)
        {
            return HasCharacter(assignDelimiters, ch);
        }

        /// <summary>
        ///     Returns true if the character is found in the given array.
        /// </summary>
        private bool HasCharacter(char[] characters, int ch)
        {
            bool result = false;

            for (int i = 0; i < characters.Length; i++)
                if (ch == characters[i])
                {
                    result = true;
                    break;
                }

            return result;
        }

        /// <summary>
        ///     Returns true if a value is whitespace.
        /// </summary>
        private bool IsWhitespace(int ch)
        {
            return ch == 0x20 || ch == 0x9 || ch == 0xD || ch == 0xA;
        }

        /// <summary>
        ///     Skips all whitespace.
        /// </summary>
        private void SkipWhitespace()
        {
            while (IsWhitespace(PeekChar()))
            {
                if (EndOfLine(PeekChar())) break;

                ReadChar();
            }
        }

        /// <summary>
        ///     Returns true if an end of line is found.  End of line
        ///     includes both an end of line or end of file.
        /// </summary>
        private bool EndOfLine(int ch)
        {
            return ch == '\n' || ch == -1;
        }

        #endregion
    }
}