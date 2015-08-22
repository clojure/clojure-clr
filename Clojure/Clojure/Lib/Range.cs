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

namespace clojure.lang
{
    /// <summary>
    /// Implements generic numeric (potentially infinite) range.    
    /// </summary>
    [Serializable]
    public class Range : ASeq, IChunkedSeq, IReduce, IEnumerable, IEnumerable<object>
    {
        #region Data

        const int CHUNK_SIZE = 32;

        // Invariants guarantee this is never an empty or infinite seq
        //   assert(start != end && step != 0)
        readonly object _start;
        readonly object _end;
        readonly object _step;
        readonly IBoundsCheck _boundsCheck;
        volatile IChunk _chunk;         // lazy
        volatile ISeq _chunkNext;       // lazy
        volatile ISeq _next;            // cached

        #endregion

        #region BoundsCheck

        private interface IBoundsCheck
        {
            bool ExceededBounds(object val);
        }

        [Serializable]
        private class PositiveStepCheck : IBoundsCheck
        {
            object _end;

            public PositiveStepCheck(object end)
            {
                _end = end;
            }

            public bool ExceededBounds(object val)
            {
                return Numbers.gte(val, _end);
            }
        }

        [Serializable]
        private class NegativeStepCheck : IBoundsCheck
        {
            object _end;

            public NegativeStepCheck(object end)
            {
                _end = end;
            }

            public bool ExceededBounds(object val)
            {
                return Numbers.lte(val, _end);
            }
        }

        private static IBoundsCheck PositiveStep(object end)
        {
            return new PositiveStepCheck(end);
        }

        private static IBoundsCheck NegativeStep(object end)
        {
            return new NegativeStepCheck(end);
        }

        #endregion

        #region C-tors and factory methods

        private Range(Object start, Object end, Object step, IBoundsCheck boundsCheck)
        {
            _end = end;
            _start = start;
            _step = step;
            _boundsCheck = boundsCheck;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private Range(Object start, Object end, Object step, IBoundsCheck boundsCheck, IChunk chunk, ISeq chunkNext)
        {
            _end = end;
            _start = start;
            _step = step;
            _boundsCheck = boundsCheck;
            _chunk = chunk;
            _chunkNext = chunkNext;
        }

        private Range(IPersistentMap meta, Object start, Object end, Object step, IBoundsCheck boundsCheck, IChunk chunk, ISeq chunkNext)
            : base(meta)
        {
            _end = end;
            _start = start;
            _step = step;
            _boundsCheck = boundsCheck;
            _chunk = chunk;
            _chunkNext = chunkNext;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(Object end)
        {
            if (Numbers.isPos(end))
                return new Range(0L, end, 1L, PositiveStep(end));
            return PersistentList.EMPTY;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(Object start, Object end)
        {
            return create(start, end, 1L);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(Object start, Object end, Object step)
        {
            if ((Numbers.isPos(step) && Numbers.gt(start, end)) ||
               (Numbers.isNeg(step) && Numbers.gt(end, start)) ||
               Numbers.equiv(start, end))
                return PersistentList.EMPTY;
            if (Numbers.isZero(step))
                return Repeat.create(start);
            return new Range(start, end, step, Numbers.isPos(step) ? PositiveStep(end) : NegativeStep(end));
        }
    
        #endregion

        #region IPersistentCollection members

        ///// <summary>
        ///// Gets the number of items in the collection.
        ///// </summary>
        ///// <returns>The number of items in the collection.</returns>
        //public override int count()
        //{
        //    return _n < _end ? _end - _n : 0;
        //}

        #endregion

        #region ISeq members

        /// <summary>
        /// Gets the first item.
        /// </summary>
        /// <returns>The first item.</returns>
        public override object first()
        {
            return _start;
        }

        void ForceChunk()
        {
            if (_chunk != null) return;

            Object[] arr = new Object[CHUNK_SIZE];
            int n = 0;
            Object val = _start;
            while (n < CHUNK_SIZE)
            {
                arr[n++] = val;
                val = Numbers.addP(val, _step);
                if (_boundsCheck.ExceededBounds(val))
                {
                    //partial last chunk
                    _chunk = new ArrayChunk(arr, 0, n);
                    return;
                }
            }

            // full last chunk
            if (_boundsCheck.ExceededBounds(val))
            {
                _chunk = new ArrayChunk(arr, 0, CHUNK_SIZE);
                return;
            }

            // full intermediate chunk
            _chunk = new ArrayChunk(arr, 0, CHUNK_SIZE);
            _chunkNext = new Range(val, _end, _step, _boundsCheck);
        }

        /// <summary>
        /// Return a seq of the items after the first.  Calls <c>seq</c> on its argument.  If there are no more items, returns nil."
        /// </summary>
        /// <returns>A seq of the items after the first, or <c>nil</c> if there are no more items.</returns>
        public override ISeq next()
        {
            if (_next != null)
                return _next;

            ForceChunk();
            if (_chunk.count() > 1)
            {
                IChunk smallerChunk = _chunk.dropFirst();
                _next = new Range(_meta, smallerChunk.nth(0), _end, _step, _boundsCheck, smallerChunk, _chunkNext);
                return _next;
            }
            return chunkedNext();
        }

        #endregion

        #region IObj members

        /// <summary>
        /// Create a copy with new metadata.
        /// </summary>
        /// <param name="meta">The new metadata.</param>
        /// <returns>A copy of the object with new metadata attached.</returns>
        public override IObj withMeta(IPersistentMap meta)
        {
            return meta == this.meta()
                 ? this
                 : new Range(meta, _start, _end, _step, _boundsCheck, _chunk, _chunkNext);
        }

        #endregion
        
        #region IReduce Members

        /// <summary>
        /// Reduce the collection using a function.
        /// </summary>
        /// <param name="f">The function to apply.</param>
        /// <returns>The reduced value</returns>
        /// <remarks>Computes f(...f(f(f(i0,i1),i2),i3),...).</remarks>
        public object reduce(IFn f)
        {
            Object acc = _start;
            object i = Numbers.addP(_start, _step);
            while (!_boundsCheck.ExceededBounds(i))
            {
                acc = f.invoke(acc, i);
                if (RT.isReduced(acc)) return ((Reduced)acc).deref();
                i = Numbers.addP(i, _step);
            }
            return acc;
        }

        /// <summary>
        /// Reduce the collection using a function.
        /// </summary>
        /// <param name="f">The function to apply.</param>
        /// <param name="start">An initial value to get started.</param>
        /// <returns>The reduced value</returns>
        /// <remarks>Computes f(...f(f(f(start,i0),i1),i2),...).</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#")]
        public object reduce(IFn f, object val)
        {
            Object acc = val;
            Object i = _start;
            while (!_boundsCheck.ExceededBounds(i))
            {
                acc = f.invoke(acc, i);
                if (RT.isReduced(acc)) return ((Reduced)acc).deref();
                i = Numbers.addP(i, _step);
            }
            return acc;
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


        //public new IPersistentCollection cons(object o)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion

        #region IEnumerable

        public new IEnumerator GetEnumerator()
        {
            object next = _start;
            while (!_boundsCheck.ExceededBounds(next))
            {
                yield return next;
                next = Numbers.addP(next, _step);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
