#region Copyright(c) 2006 ZO, All right reserved.
// -----------------------------------------------------------------------------
// Copyright(c) 2006 ZO, All right reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
//   1.  Redistribution of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//   2.  Redistribution in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//   3.  The name of the author may not be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
// EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
// OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
// OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
// ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// -----------------------------------------------------------------------------
#endregion


#region Using directives

using System;
using System.IO;
using System.Text;
using ZO.SmartCore.Core;
using ArgumentNullException=ZO.SmartCore.Core.ArgumentNullException;
using ArgumentOutOfRangeException=ZO.SmartCore.Core.ArgumentOutOfRangeException;

#endregion

namespace ZO.SmartCore.IO
{
    /// <summary>
    /// Provides an implementation of the abstract Reader class that buffers data from 
    /// another stream reader. The internal buffer is initially filled with the contents
    ///  read in from the underlying stream. Characters in this internal buffer can 
    /// then be "pushed back", or replaced with other characters at the position marked. 
    /// </summary>
    public class PushbackReader : TextReader
    {

        #region Constructors


        /// <summary>
        /// Initializes a new instance of the <see cref="PushbackReader"/> class with a 
        /// pushback buffer of the default size.
        /// </summary>
        /// <param name="reader">The reader from which characters will be read.</param>
        /// <exception cref="T:System.ArgumentNullException">stream is null. </exception>
        /// <exception cref="UnreadableStreamException">stream does not support reading. </exception>
        public PushbackReader(TextReader reader)
            : this(reader, DEFAULT_BUFFER_SIZE)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushbackReader"/> class with a 
        /// pushback buffer of the given size.
        /// </summary>
        /// <param name="reader">The reader from which characters will be read.</param>
        /// <param name="size">The size of the pushback buffer.</param>
        /// <exception cref="T:System.ArgumentNullException">stream is null. </exception>
        /// <exception cref="UnreadableStreamException">stream does not support reading. </exception>
        public PushbackReader(TextReader reader, int size)
        {
            if (reader == null) throw new ArgumentNullException();

            if (size <= 0) throw new PositiveNumberRequiredException("size");

            this._Reader = reader;

            this.buf = new char[size];

            position = size;
        }



        #endregion

        #region Destructor

        #endregion

        #region Fields

        /// <summary>
        ///  This is the default buffer size
        /// </summary>
        private const int DEFAULT_BUFFER_SIZE = 1;

        private TextReader _Reader;

        /// <summary>
        /// This is the position in the buffer from which the next char will be
        /// read.  Bytes are stored in reverse order in the buffer, starting from
        /// <code>buf[buf.length - 1]</code> to <code>buf[0]</code>.  Thus when 
        /// <code>pos</code> is 0 the buffer is full and <code>buf.length</code> when 
        /// it is empty
        /// </summary>
        private int position;


        /// <summary>
        /// This is the buffer that is used to store the pushed back data
        /// </summary>
        private char[] buf;


        #endregion

        #region Events

        #endregion

        #region Operators

        #endregion

        #region Properties

        /// <summary>
        /// Gets the reader associated with this pushback reader.
        /// </summary>
        /// <value>The reader.</value>
        public virtual TextReader Reader
        {
            get
            {
                return this._Reader;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Closes the <see cref="T:System.IO.TextReader"></see> and releases any system resources 
        /// associated with the TextReader.
        /// </summary>
        public override void Close()
        {
            lock (this)
            {
                this.buf = null;
            }

            this._Reader.Close();
            base.Close();
        }

        /// <summary>
        /// Reads the next character without changing the state of the reader or the character source. Returns the next available character without actually reading it from the input stream.
        /// </summary>
        /// <returns>
        /// The next character to be read, or -1 if no more characters are available or the stream does not support seeking.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextReader"></see> is closed. </exception>
        public override int Peek()
        {
            return this._Reader.Peek();
        }

        /// <summary>
        /// Reads the next character from the input stream and advances the character position by one character.
        /// </summary>
        /// <returns>
        /// The next character from the input stream, or -1 if no more characters are available. The default implementation returns -1.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextReader"></see> is closed. </exception>
        public override int Read()
        {
            if (this.buf == null) throw new StreamDisposedException();

            if (position == this.buf.Length) return this.Reader.Read();

            ++position;

            return this.buf[position - 1]; //& 0xFFFF);
        }

        /// <summary>
        /// Reads a maximum of count characters from the current stream and writes the data to buffer, beginning at index.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between index and (index + count - 1) replaced by the characters read from the current source.</param>
        /// <param name="index">The place in buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read. If the end of the stream is reached before count of characters is read into buffer, the current method returns.</param>
        /// <returns>
        /// The number of characters that have been read. The number will be less than or equal to count, depending on whether the data is available within the stream. This method returns zero if called when no more characters are left to read.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index or count is negative. </exception>
        /// <exception cref="T:System.ArgumentException">The buffer length minus index is less than count. </exception>
        /// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextReader"></see> is closed. </exception>
        public override int Read(char[] buffer, int index, int count)
        {
            if (buf == null) throw new StreamDisposedException();

            if (index < 0 || count < 0 || index + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            int numBytes = Math.Min(buf.Length - position, count);

            if (numBytes > 0)
            {
                Array.Copy(buf, position, buffer, index, numBytes);
                position += numBytes;
            }

            int num = count - numBytes;
            if (num > 0)
            {
                num = this.Reader.Read(buffer, numBytes, num);
                numBytes += num;
            }

            return numBytes;

        }

        /// <summary>
        /// Reads a maximum of count characters from the current stream and writes the data to buffer, beginning at index.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between index and (index + count - 1) replaced by the characters read from the current source.</param>
        /// <returns>
        /// The number of characters that have been read. The number will be less than or equal to count, depending on whether the data is available within the stream. This method returns zero if called when no more characters are left to read.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index or count is negative. </exception>
        /// <exception cref="T:System.ArgumentException">The buffer length minus index is less than count. </exception>
        /// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextReader"></see> is closed. </exception>
        public int Read(char[] buffer)
        {
            return this.Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>
        /// The next line from the input stream, or null if all characters have been read.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The number of characters in the next line is larger than <see cref="F:System.Int32.MaxValue"></see></exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.OutOfMemoryException">There is insufficient memory to allocate a buffer for the returned string. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextReader"></see> is closed. </exception>
        public override string ReadLine()
        {
            int ch;

            StringBuilder sb = new StringBuilder();

            ch = this.Read();

            while (true)
            {
                if (ch == '\r')
                {
                    if (Peek() == '\n') this.Reader.Read();
                    break;
                }
                sb.Append(ch);
                ch = this.Read();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Reads a maximum of count characters from the current stream and writes the data to buffer, beginning at index.
        /// </summary>
        /// <param name="buffer">When this method returns, this parameter contains the specified character array with the values between index and (index + count -1) replaced by the characters read from the current source.</param>
        /// <param name="index">The place in buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read.</param>
        /// <returns>
        /// The number of characters that have been read. The number will be less than or equal to count, depending on whether all input characters have been read.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index or count is negative. </exception>
        /// <exception cref="T:System.ArgumentException">The buffer length minus index is less than count. </exception>
        /// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextReader"></see> is closed. </exception>
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            if (buf == null) throw new StreamDisposedException();

            if (index < 0 || count < 0 || index + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            int numBytes = Math.Min(buf.Length - position, count);

            if (numBytes > 0)
            {
                Array.Copy(buf, position, buffer, index, numBytes);
                position += numBytes;
            }

            int num = count - numBytes;
            if (num > 0)
            {
                num = this.Reader.ReadBlock(buffer, numBytes, num);
                numBytes += num;
            }

            return numBytes;
        }


        /// <summary>
        /// Reads all characters from the current position to the end of the TextReader and returns them as one string.
        /// </summary>
        /// <returns>
        /// A string containing all characters from the current position to the end of the TextReader.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The number of characters in the next line is larger than <see cref="F:System.Int32.MaxValue"></see></exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.OutOfMemoryException">There is insufficient memory to allocate a buffer for the returned string. </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextReader"></see> is closed. </exception>
        public override string ReadToEnd()
        {

            StringBuilder sb = new StringBuilder();

            char[] ch1 = new char[256];


            while (true)
            {
                int num = Reader.Read(ch1, 0, 256);

                if (num == -1) break;
                sb.Append(buf, 0, num);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Push back a single character. 
        /// </summary>
        /// <param name="c">The character to push back.</param>
        public void Unread(int c)
        {
            if (buf == null) throw new StreamDisposedException();

            if (position == 0) throw new IOException("Pushback buffer is full");

            --position;

            buf[position] = (char)c; // & 0xFFFF);

        }


        /// <summary>
        /// Push back an array of characters by copying it to the front of the pushback buffer.
        ///  After this method returns, the next character to be read will have the value cbuf[0], 
        /// the character after that will have the value cbuf[1], and so forth. 
        /// </summary>
        /// <param name="buffer">Character array to push back </param>
        public void Unread(char[] buffer)
        {
            Unread(buffer, 0, buffer.Length);
        }


        /// <summary>
        /// Push back a portion of an array of characters by copying it to the front of the pushback buffer.
        ///  After this method returns, the next character to be read will have the value cbuf[off], 
        /// the character after that will have the value cbuf[off+1], and so forth. 
        /// </summary>
        /// <param name="buffer"> Character array</param>
        /// <param name="offset">Offset of first character to push back</param>
        /// <param name="length">Number of characters to push back </param>
        public void Unread(char[] buffer, int offset, int length)
        {
            if (buf == null) throw new StreamDisposedException();

            if (position == 0) throw new IOException("Pushback buffer is full");

            if (length > buf.Length) throw new System.ArgumentOutOfRangeException("length");

            // Note the order that these chars are being added is the opposite
            // of what would be done if they were added to the buffer one at a time.
            // See the Java Class Libraries book p. 1397.
            Array.Copy(buffer, offset, buf, position - length, length);

            // Don't put this into the arraycopy above, an exception might be thrown
            // and in that case we don't want to modify pos.
            position -= length;


        }

        #endregion
    }
}
