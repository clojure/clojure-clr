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

using System.Collections;
using System;
using System.Collections.Generic;

namespace clojure.lang
{

    /// <summary>
    /// Provides an IDictionaryEnumerator running over a <see cref="SeqEnumerator">SeqEnumerator</see>.
    /// </summary>
    /// <remarks>
    /// <para>Mirrors <see cref="SeqEnumerator">SeqEnumerator</see>.</para> 
    /// <para>No equivalent in Java version.</para>
    /// <para>The map provides <see cref="IMapEntry">IMapEntry</see> key/value pairs.  
    /// This converts an <see cref="IMapEntry">IMapEntry</see> to a DictionaryEntry.
    /// </para>
    /// </remarks>
    public sealed class MapEnumerator : IDictionaryEnumerator, IDisposable, IEnumerator, IEnumerator<IMapEntry>
    {
        #region Data

        /// <summary>
        /// The <see cref="SeqEnumerator">SeqEnumerator</see> to iterate over.
        /// </summary>
        private readonly SeqEnumerator _seqEnum;

        /// <summary>
        /// The key of the current entry.
        /// </summary>
        private object CurrentKey
        {
            get { return ((IMapEntry)_seqEnum.Current).key(); }
        }

        /// <summary>
        /// The value of the current entry.
        /// </summary>
        private object CurrentVal
        {
            get { return ((IMapEntry)_seqEnum.Current).val(); }
        }

        bool _disposed = false;

        #endregion

        #region C-tor

        /// <summary>
        /// Construct a <see cref="MapEnumerator">MapEnumerator</see> from a persistent map.
        /// </summary>
        /// <param name="map">The map to iterate over.</param>
        public MapEnumerator(IPersistentMap map)
        {
            _seqEnum = new SeqEnumerator(map.seq());
        }

        #endregion

        #region IDictionaryEnumerator Members

        /// <summary>
        /// The current entry.
        /// </summary>
        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(CurrentKey, CurrentVal); }
        }

        /// <summary>
        /// The current key.
        /// </summary>
        public object Key
        {
            get { return CurrentKey; }
        }

        /// <summary>
        /// The current value.
        /// </summary>
        public object Value
        {
            get { return CurrentVal; }
        }

        #endregion

        #region IEnumerator Members

        public IMapEntry Current
        {
            get { return (IMapEntry)_seqEnum.Current; }
        }

        /// <summary>
        /// The current entry.
        /// </summary>
        object IEnumerator.Current
        {
            get { return _seqEnum.Current; }
        }

        /// <summary>
        /// Advance to the next item.
        /// </summary>
        /// <returns><value>true</value> if there is a next value; <value>false</value> otherwise.</returns>
        public bool MoveNext()
        {
            return _seqEnum.MoveNext();
        }

        /// <summary>
        /// Reset the enumerator.
        /// </summary>
        public void Reset()
        {
            _seqEnum.Reset();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if ( _seqEnum != null )
                        _seqEnum.Dispose();
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
