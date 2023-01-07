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
    public class PushbackTextReader : TextReader, IDisposable
    {
        #region Data

        protected TextReader _baseReader;
        protected TextReader BaseReader
        {
            get { return _baseReader; }
        }

        protected int _unreadChar;
        protected bool _hasUnread = false;

        bool _disposed = false;

        #endregion

        #region C-tors

        public PushbackTextReader(TextReader reader)
        {
            _baseReader = reader;
        }

        #endregion

        #region Lookahead

        public override int Peek()
        {
            return _baseReader.Peek();
        }

        #endregion

        #region Unreading

        public virtual void Unread(int ch)
        {
            if (_hasUnread)
                throw new IOException("Can't unread a second character.");

            _unreadChar = ch;
            _hasUnread = true;
 
        }


        #endregion

        #region Basic reading

        public override int Read()
        {
            int ret;
            if (_hasUnread)
            {
                ret = _unreadChar;
                _hasUnread = false;
            }
            else
                ret = _baseReader.Read();

            return ret;
        }

       #endregion

        #region Lifetime methods

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_baseReader != null)
                        _baseReader.Dispose();
                }
                _baseReader = null;
                _disposed = true;
                base.Dispose(disposing);
            }
        }

        #endregion

    }
}
