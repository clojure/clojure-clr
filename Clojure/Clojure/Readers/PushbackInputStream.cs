/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/

using System;
using System.IO;

namespace clojure.lang
{
    public class PushbackInputStream : Stream, IDisposable
    {
        #region Data

        protected Stream BaseStream { get; set; }

        protected byte _unreadByte;
        protected bool _hasUnread;

        bool _disposed;

        #endregion

        #region C-tors

        public PushbackInputStream(Stream stream)
        {
            BaseStream = stream;
        }

        #endregion

        #region Unreading

        public virtual void Unread(byte b)
        {
            if (_hasUnread)
                throw new IOException("Can't unread a second byte.");

            _unreadByte = b;
            _hasUnread = true;
        }

        #endregion

        #region Lifetime methods

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (BaseStream != null)
                        BaseStream.Dispose();
                }
                BaseStream = null;
                _disposed = true;
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Stream methods

        public override bool CanRead
        {
            get { return BaseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }


        public override int Read(byte[] buffer, int offset, int count)
        {

            if (!BaseStream.CanRead)
            {
                throw new NotSupportedException();
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count + offset > buffer.Length)
            {
                throw new ArgumentException("The sum of offset and count is larger than the buffer length");
            }

            if (_disposed)
            {
                throw new ObjectDisposedException("PushbackInputStream", "Cannot access a closed Stream.");
            }

            if (count == 0)
            {
                return 0;
            }

            if (_hasUnread)
            {
                buffer[offset] = _unreadByte;
                _hasUnread = false;

                return 1 + BaseStream.Read(buffer, offset + 1, count - 1);
            }

            return BaseStream.Read(buffer, offset, count);
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
