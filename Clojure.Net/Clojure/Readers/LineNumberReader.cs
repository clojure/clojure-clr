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

#endregion

namespace ZO.SmartCore.IO
{
    /// <summary>
    /// Implements a reader that counts the number of lines and Position in text data. 
    /// </summary>
    public class LineNumberReader : TextReader
    {
        #region Constructors

        /// <summary>Initializes a new instance of the <see cref="LineNumberReader"></see> class for the specified stream.</summary>
        /// <param name="reader">The reader from which characters will be read.</param>
        /// <exception cref="T:System.ArgumentNullException">stream is null. </exception>
        /// <exception cref="T:System.ArgumentException">stream does not support reading. </exception>
        public LineNumberReader(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException();

            this._Reader = reader;
        }




        #endregion

        #region Destructor

        #endregion

        #region Fields

        private TextReader _Reader;

        /// <summary>
        /// The current line number.
        /// </summary>
        private int _LineNumber;


        private int _Position;


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


        /// <summary>
        /// Gets the current line number.
        /// </summary>
        /// <value>The current line number.</value>
        public int LineNumber
        {
            get { return this._LineNumber; }
            private set
            {
                this._LineNumber = value;
            }
        }

        /// <summary>
        /// Gets the current position in current line.
        /// </summary>
        /// <value>The current position in line.</value>
        public int Position
        {
            get { return this._Position; }
            private set
            {
                this._Position = value;
            }
        }

        #endregion

        #region Methods


        /// <summary>
        /// Reads the next character from the input stream and advances the character position by one character.
        /// </summary>
        /// <returns>
        /// The next character from the input stream represented as an <see cref="T:System.Int32"></see> object, or -1 if no more characters are available.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        public override int Read()
        {
            int retval = this.Reader.Read();

            if ((char)retval == '\r')
            {
                if (Peek() == '\n') this.Reader.Read();
                retval = '\n';
            }

            if ((char)retval == '\n')
            {
                LineNumber++;
                Position = 0;
            }
            else { ++Position; }
            return retval;
        }


        /// <summary>
        /// Reads a maximum of count characters from the current stream into buffer, beginning at index.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between index and (index + count - 1) replaced by the characters read from the current source.</param>
        /// <param name="index">The index of buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read.</param>
        /// <returns>
        /// The number of characters that have been read, or 0 if at the end of the stream and no data was read. The number will be less than or equal to the count parameter, depending on whether the data is available within the stream.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">index or count is negative. </exception>
        /// <exception cref="T:System.ArgumentException">The buffer length minus index is less than count. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs, such as the stream is closed. </exception>
        public override int Read(char[] buffer, int index, int count)
        {

            int bytesRead = this.Reader.Read(buffer, index, count);

            CountLinesInBuffer(buffer, index, bytesRead);

            return bytesRead;

        }


        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>
        /// The next line from the input stream, or null if the end of the input stream is reached.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.OutOfMemoryException">There is insufficient memory to allocate a buffer 
        /// for the returned string. </exception>
        public override string ReadLine()
        {
            string retval = this.Reader.ReadLine();

            if (retval != null)
            {
                LineNumber++;

                Position = 0;
            }

            return retval;

        }

        /// <summary>
        /// Counts the lines in buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private void CountLinesInBuffer(char[] buffer, int index, int count)
        {

            int i = index;

            int lastIndex = index + count;


            do
            {

                char ch = buffer[i];

                if (ch == '\r')
                {
                    if ((i + 1 < lastIndex) && (buffer[i + 1] == '\n')) i++;

                    ch = '\n';
                }

                if (ch == '\n')
                {
                    LineNumber++;
                    Position = 0;
                }
                else
                {
                    ++Position;
                }
            } while (++i < lastIndex);

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
        /// Closes the <see cref="T:System.IO.TextReader"></see> and releases any system resources 
        /// associated with the TextReader.
        /// </summary>
        public override void Close()
        {
            this._Reader.Close();
            base.Close();
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

            string retval = this.Reader.ReadToEnd();

            if (!String.IsNullOrEmpty(retval)) LineNumber++;

            Position = 0;

            return retval;
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
            int bytesRead = this.Reader.ReadBlock(buffer, index, count);

            CountLinesInBuffer(buffer, index, bytesRead);

            return bytesRead;
        }

        #endregion



    }
}