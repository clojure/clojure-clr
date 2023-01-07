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

namespace clojure.lang
{
    [Serializable]
    public sealed class ArrayChunk : IChunk
    {
        #region Data

        readonly object[] _array;
        readonly int _off;
        readonly int _end;

        #endregion

        #region C-tors

        public ArrayChunk(object[] array, int off)
            : this(array,off,array.Length)
        {
        }

        public ArrayChunk(object[] array, int off, int end)
        {
            _array = array;
            _off = off;
            _end = end;
        }

        #endregion

        #region Indexed Members

        public object nth(int i)
        {
            return _array[_off + i];
        }

        public object nth(int i, object notFound)
        {
            if (i >= 0 && i < count())
                return nth(i);
            return notFound;
        }

        #endregion

        #region Counted Members

        public int count()
        {
            return _end - _off;
        }

        #endregion

        #region IChunk Members

        public IChunk dropFirst()
        {
            if (_off == _end)
                throw new InvalidOperationException("dropFirst of empty chunk");
            return new ArrayChunk(_array, _off + 1, _end);
        }

        public object reduce(IFn f, object start)
        {
            object ret = f.invoke(start, _array[_off]);
            if (RT.isReduced(ret))
                return ret;
            for (int x = _off + 1; x < _end; x++)
            {
                ret = f.invoke(ret, _array[x]);
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref();
            }
            return ret;
        }

        #endregion
    }
}
