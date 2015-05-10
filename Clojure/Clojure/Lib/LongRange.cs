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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Implements the special common case of a finite range based on long start, end, and step.
    /// </summary>
    public class LongRange: ASeq, Counted, IChunkedSeq, IReduce, IEnumerable, IEnumerable<Object>
    {
        #region Data

        const int CHUNK_SIZE = 32;

        // Invariants guarantee this is never an empty or infinite seq
        //   assert(start != end && step != 0)
        readonly long _start;
        readonly long _end;
        readonly long _step;
        readonly ExceedsBoundDel _exceedsBoundDel;
        volatile LongChunk _chunk;  // lazy
        volatile ISeq _chunkNext;        // lazy
        volatile ISeq _next;             // cached

        #endregion

        #region BoundsCheck

        // this is a one method interface in the JVM version
        // I've converted it to a delegate

        private delegate bool ExceedsBoundDel(long val);

        private static ExceedsBoundDel PositiveStep(long end) 
        {
            return ( val => val >= end);
        }

        private static ExceedsBoundDel NegativeStep(long end) 
        {
            return ( val => val <= end);
        }

        #endregion

        #region Ctors and facxtories

        LongRange(long start, long end, long step, ExceedsBoundDel exceedsBoundDel)
        {
            _start = start;
            _end = end;
            _step = step;
            _exceedsBoundDel = exceedsBoundDel;
        }

        private LongRange(long start, long end, long step, ExceedsBoundDel exceedsBoundDel, LongChunk chunk, ISeq chunkNext)
        {
            _start = start;
            _end = end;
            _step = step;
            _exceedsBoundDel = exceedsBoundDel;
            _chunk = chunk;
            _chunkNext = chunkNext;
        }


        private LongRange(IPersistentMap meta, long start, long end, long step, ExceedsBoundDel exceedsBoundDel, LongChunk chunk, ISeq chunkNext)
            : base(meta)
        {
            _start = start;
            _end = end;
            _step = step;
            _exceedsBoundDel = exceedsBoundDel;
            _chunk = chunk;
            _chunkNext = chunkNext;
        }


        public static ISeq create(long end)
        {
            if (end > 0)
                return new LongRange(0L, end, 1L, PositiveStep(end));
            return PersistentList.EMPTY;
        }

        public static ISeq create(long start, long end)
        {
            if (start >= end)
                return PersistentList.EMPTY;
            return new LongRange(start, end, 1L, PositiveStep(end));
        }

        public static ISeq create(long start, long end, long step)
        {
            if (step > 0)
            {
                if (end <= start) return PersistentList.EMPTY;
                return new LongRange(start, end, step, PositiveStep(end));
            }
            else if (step < 0)
            {
                if (end >= start) return PersistentList.EMPTY;
                return new LongRange(start, end, step, NegativeStep(end));
            }
            else
            {
                if (end == start) return PersistentList.EMPTY;
                return Repeat.create(start);
            }
        }

        #endregion

        #region IObj methods

        public override IObj withMeta(IPersistentMap meta)
        {
            if (meta == _meta)
                return this;
            return new LongRange(meta, _start, _end, _step, _exceedsBoundDel, _chunk, _chunkNext);
        }

        #endregion

        #region ISeq methods

        public override object first()
        {
            return _start;
        }

        void ForceChunk()
        {
            if (_chunk != null) return;

            long nextStart = _start + (_step * CHUNK_SIZE);
            if (_exceedsBoundDel(nextStart))
            {
                int count = AbsCount(_start, _end, _step);
                _chunk = new LongChunk(_start, _step, count);
            }
            else
            {
                _chunk = new LongChunk(_start, _step, CHUNK_SIZE);
                _chunkNext = new LongRange(nextStart, _end, _step, _exceedsBoundDel);
            }
        }

        public override ISeq next()
        {
            if (_next != null)
                return _next;

            ForceChunk();
            if (_chunk.count() > 1)
            {
                LongChunk smallerChunk = (LongChunk)_chunk.dropFirst();
                _next = new LongRange(smallerChunk.first(), _end, _step, _exceedsBoundDel, smallerChunk, _chunkNext);
                return _next;
            }
            return chunkedNext();
        }

        public new IPersistentCollection cons(object o)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Counted methods

        static int AbsCount(long start, long end, long step)
        {
            double c = (double)(end - start) / step;
            int ic = (int)c;
            if (c > ic)
                return ic + 1;
            else
                return ic;
        }

        public override int count()
        {
            return AbsCount(_start, _end, _step);
        }
  
        #endregion

        #region IChunkedSeq methods

        public IChunk chunkedFirst()
        {
            ForceChunk();
            return _chunk;
        }

        public ISeq chunkedNext()
        {
            return chunkedMore().seq();
        }

        public ISeq chunkedMore()
        {
            ForceChunk();
            if (_chunkNext == null)
                return PersistentList.EMPTY;
            return _chunkNext;
        }

        #endregion

        #region IReduce methods

        public object reduce(IFn f)
        {
            Object acc = _start;
            long i = _start + _step;
            while (!_exceedsBoundDel(i))
            {
                acc = f.invoke(acc, i);
                Reduced accRed = acc as Reduced;
                if (accRed != null)
                    return accRed.deref();
                i += _step;
            }
            return acc;
        }

        public object reduce(IFn f, object val)
        {
            Object acc = val;
            long i = _start;
            do
            {
                acc = f.invoke(acc, i);
                if (RT.isReduced(acc)) return ((Reduced)acc).deref();
                i += _step;
            } while (!_exceedsBoundDel(i));
            return acc;
        }

        #endregion

        #region IEnumerable

        public new IEnumerator GetEnumerator()
        {
            long next = _start;
            while ( ! _exceedsBoundDel(next))
            {
                yield return next;
                next = next + _step;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region LongChunk

        class LongChunk: IChunk
        {
            #region Data

            readonly long _start;
            readonly long _step;
            readonly int _count;

            #endregion

            #region Ctors and factories

            public LongChunk(long start, long step, int count)
            {
                _start = start;
                _step = step;
                _count = count;
            }

            #endregion

            #region Misc

            public long first()
            {
                return _start;
            }

            #endregion

            #region IChunk implementation

            public IChunk dropFirst()
            {
                if (_count <= 1)
                    throw new InvalidOperationException("dropFirst of empty chunk");
                return new LongChunk(_start + _step, _step, _count - 1);
            }

            public object reduce(IFn f, object init)
            {
                long x = _start;
                Object ret = init;
                for (int i = 0; i < _count; i++)
                {
                    ret = f.invoke(ret, x);
                    if (RT.isReduced(ret))
                        return ret;
                    x += _step;
                }
                return ret;
            }

            public object nth(int i)
            {
                return _start + (i * _step);
            }

            public object nth(int i, object notFound)
            {
                if (i >= 0 && i < _count)
                    return _start + (i * _step);
                return notFound;
            }

            public int count()
            {
                return _count;
            }

            #endregion
        }

        #endregion
    }
}


