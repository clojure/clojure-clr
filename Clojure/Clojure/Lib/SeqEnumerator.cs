﻿/**
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
       
        /// <summary>
        /// <value>true</value> if we have reached the end of the sequence.
        /// </summary>
        bool _isAtEnd;

        /// <summary>
        /// Current position in the sequence.
        /// </summary>
        private ISeq _seq;

        /// <summary>
        /// The original sequence (for resetting).
        /// </summary>
        private ISeq _origSeq;

        #endregion

        #region C-tors

        /// <summary>
        /// Construct one from a given sequence.
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public TypedSeqEnumerator(ISeq seq)
        {
            _origSeq = seq;
            _isAtEnd = _origSeq == null;
            _seq = null;
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
                if (_isAtEnd || _seq == null)
                    throw new InvalidOperationException("No current value.");

                return _seq.first();
            }
        }

        /// <summary>
        /// Move to the next item.
        /// </summary>
        /// <returns><value>true</value> if there is a next item; 
        /// <value>false</value> if the sequence is already at the end.</returns>
        public bool MoveNext()
        {
            if (_isAtEnd || _origSeq == null)
                return false;

            if (_seq == null)
                _seq = _origSeq;
            else
            {
                _seq = _seq.next();
                if (_seq == null)
                    _isAtEnd = true;
            }
            return !_isAtEnd;
        }

        public void Reset()
        {
            _isAtEnd = _origSeq == null;
            _seq = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _origSeq = null;
            _seq = null;
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
        /// Construct one from a given sequence.
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public SeqEnumerator(ISeq seq)
            :base(seq)
        {
        }

        #endregion
    }

    public sealed class IMapEntrySeqEnumerator : TypedSeqEnumerator<IMapEntry>
    {
        #region C-tors

        /// <summary>
        /// Construct one from a given sequence.
        /// </summary>
        /// <param name="seq">The underlying sequence.</param>
        public IMapEntrySeqEnumerator(ISeq seq)
            :base(seq)
        {
        }

        #endregion
    }

}



