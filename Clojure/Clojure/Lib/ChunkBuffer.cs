/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public void add(object o)
        {
            _buffer[_end++] = o;
        }

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
