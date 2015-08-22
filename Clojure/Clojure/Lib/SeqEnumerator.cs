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

    public class TypedSeqEnumerator<T> : IEnumerator, IEnumerator<T>
    {
        #region Data
       
        private bool _isRealized;
        private object _orig;
        private object _curr;
        private object _next;

        static readonly object _start = new object();


        #endregion

        #region C-tors


        /// <summary>
        /// Construct one from a given sequence.  (preserve original ctor for compatibility)
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public TypedSeqEnumerator(ISeq o)
        {
            _isRealized = false;
            _curr = _start;
            _orig = o;
            _next = o;
        }

        /// <summary>
        /// Construct one from a given sequence.
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public TypedSeqEnumerator(object o)
        {
            _isRealized = false;
            _curr = _start;
            _orig = o;
            _next = o;
        }

        #endregion

        #region IEnumerator and IEnumerator<T> Members

        /// <summary>
        /// The current item.
        /// </summary>
        public object Current
        {
            get
            {
                if (_next == null  )
                    throw new InvalidOperationException("No current value.");

                if (_curr == _start)
                    _curr = RT.first(_next);

                return _curr;
            }
        }

        /// <summary>
        /// Move to the next item.
        /// </summary>
        /// <returns><value>true</value> if there is a next item; 
        /// <value>false</value> if the sequence is already at the end.</returns>
        public bool MoveNext()
        {
            if (_next == null )
                return false;

            if (! _isRealized)
            {
                _curr = _start;
                _isRealized = true;
                _next = RT.seq(_next);
            }
            else 
            {
                _curr = _start;
                _next = RT.next(_next);
            }
            
            return _next != null;
        }

        public void Reset()
        {
            // TODO: Fix this -- we already realized this.
            _isRealized = false;
            _curr = _start;
            _next = _orig;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            _orig = null;
            _curr = null;
            _next = null;
        }

        T IEnumerator<T>.Current
        {
            get { return (T)this.Current; }
        }

        #endregion
     }

    /// <summary>
    /// Implements standard IEnumerator behavior over an <see cref="ISeq">ISeq</see>.
    /// </summary>
    /// <remarks>Equivalent to Java verion: SeqIterator</remarks>
    public sealed class SeqEnumerator : TypedSeqEnumerator<Object>
    {
        #region C-tors


        /// <summary>
        /// Construct one from a given sequence. (preserve original ctor for compatibility)
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public SeqEnumerator(ISeq seq)
            : base(seq)
        {
        }

        /// <summary>
        /// Construct one from a given sequence.
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public SeqEnumerator(object seq)
            :base(seq)
        {
        }

        #endregion
    }

    public sealed class IMapEntrySeqEnumerator : TypedSeqEnumerator<IMapEntry>
    {
        #region C-tors


        /// <summary>
        /// Construct one from a given sequence. (preserve original ctor for compatibility)
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public IMapEntrySeqEnumerator(ISeq seq)
            : base(seq)
        {
        }

        /// <summary>
        /// Construct one from a given sequence.
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public IMapEntrySeqEnumerator(object seq)
            :base(seq)
        {
        }

        #endregion
    }

}



