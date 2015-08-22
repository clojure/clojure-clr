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
    [Serializable]
    public class LongRange: ASeq, Counted, IChunkedSeq, IReduce, IEnumerable, IEnumerable<Object>
    {
        #region Data

        const int CHUNK_SIZE = 32;

        // Invariants guarantee this is never an empty or infinite seq
        //   assert(start != end && step != 0)
        readonly long _start;
        readonly long _end;
        readonly long _step;
        readonly IBoundsCheck _boundsCheck;
        volatile LongChunk _chunk;  // lazy
        volatile ISeq _chunkNext;        // lazy
        volatile ISeq _next;             // cached

        #endregion

        #region BoundsCheck

        // this is a one method interface in the JVM version
        // Originally, I converted it to a delegate.
        // Subsequently, they decided to make this class serializable, and serializability and lambdas/delegates do not mix.
        // So I'm moving to the JVM solution.  Sigh.

        private interface IBoundsCheck
        {
            bool ExceededBounds(long val);
        }

        [Serializable]
        private class PositiveStepCheck: IBoundsCheck
        {
            long _end;

            public PositiveStepCheck(long end)
            {
                _end = end;
            }
            public bool ExceededBounds(long val)
            {
                return val >= _end;
            }
        }

        [Serializable]
        private class NegativeStepCheck : IBoundsCheck
        {
            long _end;

            public NegativeStepCheck(long end)
            {
                _end = end;
            }
            public bool ExceededBounds(long val)
            {
                return val <= _end;
            }
        }

        private static IBoundsCheck PositiveStep(long end) 
        {
            return new PositiveStepCheck(end);
        }

        private static IBoundsCheck NegativeStep(long end) 
        {
            return new NegativeStepCheck(end);
        }

        #endregion

        #region Ctors and facxtories

        LongRange(long start, long end, long step, IBoundsCheck boundsCheck)
        {
            _start = start;
            _end = end;
            _step = step;
            _boundsCheck = boundsCheck;
        }

        private LongRange(long start, long end, long step, IBoundsCheck boundsCheck, LongChunk chunk, ISeq chunkNext)
        {
            _start = start;
            _end = end;
            _step = step;
            _boundsCheck = boundsCheck;
            _chunk = chunk;
            _chunkNext = chunkNext;
        }


        private LongRange(IPersistentMap meta, long start, long end, long step, IBoundsCheck boundsCheck, LongChunk chunk, ISeq chunkNext)
            : base(meta)
        {
            _start = start;
            _end = end;
            _step = step;
            _boundsCheck = boundsCheck;
            _chunk = chunk;
            _chunkNext = chunkNext;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(long end)
        {
            if (end > 0)
                return new LongRange(0L, end, 1L, PositiveStep(end));
            return PersistentList.EMPTY;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(long start, long end)
        {
            if (start >= end)
                return PersistentList.EMPTY;
            return new LongRange(start, end, 1L, PositiveStep(end));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
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
            return new LongRange(meta, _start, _end, _step, _boundsCheck, _chunk, _chunkNext);
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

            long count;
            try
            {
                count = RangeCount(_start, _end, _step);
            }
            catch (ArithmeticException)
            {
                // size of total range is > Long.MAX_VALUE so must step to count
                // this only happens in pathological range cases like:
                // (range -9223372036854775808 9223372036854775807 9223372036854775807)
                count = SteppingCount(_start, _end, _step);
            }

            if (count > CHUNK_SIZE)
            { // not last chunk
                long nextStart = _start + (_step * CHUNK_SIZE);   // cannot overflow, must be < end
                _chunk = new LongChunk(_start, _step, CHUNK_SIZE);
                _chunkNext = new LongRange(nextStart, _end, _step, _boundsCheck);
            }
            else
            {  // last chunk
                _chunk = new LongChunk(_start, _step, (int)count);   // count must 

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
                _next = new LongRange(smallerChunk.first(), _end, _step, _boundsCheck, smallerChunk, _chunkNext);
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

        // fallback count mechanism for pathological cases
        // returns either exact count or CHUNK_SIZE+1
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "end")]
        long SteppingCount(long start, long end, long step)
        {
            long count = 1;
            long s = start;
            while (count <= CHUNK_SIZE)
            {
                try
                {
                    s = Numbers.add(s, step);
                    if (_boundsCheck.ExceededBounds(s))
                        break;
                    else
                        count++;
                }
                catch (ArithmeticException)
                {
                    break;
                }
            }
            return count;
        }
        
        // returns exact size of remaining items OR throws ArithmeticException for overflow case
        long RangeCount(long start, long end, long step)
        {
            // (1) count = ceiling ( (end - start) / step )
            // (2) ceiling(a/b) = (a+b+o)/b where o=-1 for positive stepping and +1 for negative stepping
            // thus: count = end - start + step + o / step
            return Numbers.add(Numbers.add(Numbers.minus(end, start), step), _step > 0 ? -1 : 1) / step;
        }

        public override int count()
        {
            try
            {
                long c = RangeCount(_start, _end, _step);
                if (c > Int32.MaxValue)
                {
                    return Numbers.ThrowIntOverflow();
                }
                else
                {
                    return (int)c;
                }
            }
            catch (ArithmeticException)
            {
                // rare case from large range or step, fall back to iterating and counting
                IEnumerator enumerator = this.GetEnumerator();
                long count = 0;
                while (enumerator.MoveNext())
                {
                    count++;
                }

                if (count > Int32.MaxValue)
                    return Numbers.ThrowIntOverflow();
                else
                    return (int)count;
            }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public object reduce(IFn f)
        {
            Object acc = _start;
            long i = _start + _step;
            while (!_boundsCheck.ExceededBounds(i))
            {
                acc = f.invoke(acc, i);
                Reduced accRed = acc as Reduced;
                if (accRed != null)
                    return accRed.deref();
                i += _step;
            }
            return acc;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#")]
        public object reduce(IFn f, object val)
        {
            Object acc = val;
            long i = _start;
            do
            {
                acc = f.invoke(acc, i);
                if (RT.isReduced(acc)) return ((Reduced)acc).deref();
                i += _step;
            } while (!_boundsCheck.ExceededBounds(i));
            return acc;
        }

        #endregion

        #region IEnumerable

        public new IEnumerator GetEnumerator()
        {
            long next = _start;
            while (!_boundsCheck.ExceededBounds(next))
            {
                yield return next;
                try
                {
                    next = Numbers.add(next, _step);
                }
                catch (ArithmeticException)
                {
                    yield break;
                }
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


