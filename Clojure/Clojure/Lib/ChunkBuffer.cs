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


namespace clojure.lang
{
    public sealed class ChunkBuffer : Counted
    {
        #region Data

        object[] _buffer;
        int _end;

        #endregion

        #region C-tors

        public ChunkBuffer(int capacity)
        {
            _buffer = new object[capacity];
            _end = 0;
        }

        #endregion

        #region Other

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public void add(object o)
        {
            _buffer[_end++] = o;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IChunk chunk()
        {
            ArrayChunk ret = new ArrayChunk(_buffer, 0, _end);
            _buffer = null;
            return ret;
        }

        #endregion

        #region Counted Members

        public int count()
        {
            return _end;
        }

        #endregion
    }
}
