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
    /// Implements the special common case of a finite range based on long start, end, and step,
    /// with no more than Int32.MaxValue items.
    /// </summary>
    [Serializable]
    public class LongRange: ASeq, Counted, IChunkedSeq, IReduce, IDrop, IEnumerable, IEnumerable<Object>
    {
        #region Data

        // Invariants guarantee this is never an empty or infinite seq
        //   assert(start != end && step != 0)
        readonly long _start;
        readonly long _end;
        readonly long _step;
        readonly int _count;

        #endregion

        #region Ctors and facxtories

        LongRange(long start, long end, long step, int count)
        {
            _start = start;
            _end = end;
            _step = step;
            _count= count;
        }

        private LongRange(IPersistentMap meta, long start, long end, long step, int count)
            : base(meta)
        {
            _start = start;
            _end = end;
            _step = step;
            _count = count;
        }

        // Captures 
        private static int ToIntExact(long value)
        {
            checked
            {
                return (int)value;
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static ISeq create(long end)
        {
            if (end > 0)
                try
                {
                    return new LongRange(0L, end, 1L, ToIntExact(RangeCount(0L, end, 1L)));
                }
                catch (ArithmeticException)
                {
                    return Range.create(end);  // count > Int32.MaxValue
                }
            return PersistentList.EMPTY;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static ISeq create(long start, long end)
        {
            if (start >= end)
                return PersistentList.EMPTY;
            else
            {
                try
                {
                    return new LongRange(start, end, 1L, ToIntExact(RangeCount(start, end, 1L)));
                }
                catch (ArithmeticException)
                {
                    return Range.create(start,end);  // count > Int32.MaxValue
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static ISeq create(long start, long end, long step)
        {
            if (step > 0)
            {
                if (end <= start) return PersistentList.EMPTY;
                try
                {
                    return new LongRange(start, end, step, ToIntExact(RangeCount(start, end, step)));
                }
                catch (ArithmeticException)
                {
                    return Range.create(start, end, step);
                }
            }
            else if (step < 0)
            {
                if (end >= start) return PersistentList.EMPTY;
                try
                {
                    return new LongRange(start, end, step, ToIntExact(RangeCount(start, end, step)));
                }
                catch (ArithmeticException)
                {
                    return Range.create(start, end, step);
                }
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
            return new LongRange(meta, _start, _end, _step, _count);
        }

        #endregion

        #region ISeq methods

        public override object first()
        {
            return _start;
        }

        public override ISeq next()
        {
            if (_count > 1)
                return new LongRange(_start + _step, _end, _step, _count - 1);
            else
                return null;
        }

        #endregion

        #region Counted methods
        
        // returns exact size of remaining items OR throws ArithmeticException for overflow case
        static long RangeCount(long start, long end, long step)
        {
            // (1) count = ceiling ( (end - start) / step )
            // (2) ceiling(a/b) = (a+b+o)/b where o=-1 for positive stepping and +1 for negative stepping
            // thus: count = end - start + step + o / step
            return Numbers.add(Numbers.add(Numbers.minus(end, start), step), step > 0 ? -1 : 1) / step;
        }

        public override int count()
        {
            return _count;
        }
  
        #endregion

        #region IChunkedSeq methods

        public IChunk chunkedFirst()
        {
            return new LongChunk(_start, _step, _count);
        }

        public ISeq chunkedNext()
        {
            return null;
        }

        public ISeq chunkedMore()
        {
            return PersistentList.EMPTY;
        }

        #endregion

        #region IDrop methods

        public Sequential drop(int n)
        {
            if (n <= 0)
            {
                return this;
            }
            else if (n < _count)
            {
                return new LongRange(_start + (_step * n), _end, _step, _count - n);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region IReduce methods

        public object reduce(IFn f)
        {
            Object acc = _start;
            long i = _start + _step;
            int n = _count;

            while (n > 1)
            {
                acc = f.invoke(acc, i);
                if (acc is Reduced accRed)
                    return accRed.deref();
                i += _step;
                n--;
            }
            return acc;
        }

        public object reduce(IFn f, object val)
        {
            Object acc = val;
            long i = _start;
            int n = _count;
            do
            {
                acc = f.invoke(acc, i);
                if (RT.isReduced(acc)) return ((Reduced)acc).deref();
                i += _step;
                n--;
            } while (n > 0);
            return acc;
        }

        #endregion

        #region IEnumerable

        public new IEnumerator GetEnumerator()
        {
            long next = _start;
            int remaining = _count;
            while (remaining > 0)
            {
                yield return next;
                next += _step;
                remaining--;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region LongChunk

        [Serializable]
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

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
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


