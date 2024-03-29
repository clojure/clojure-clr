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

namespace clojure.lang
{
    /// <summary>
    /// Implements an persistent, ordered set.
    /// </summary>
    [Serializable]
    public class PersistentTreeSet : APersistentSet, IObj, Reversible, Sorted
    {
        #region Data

        readonly IPersistentMap _meta;

        /// <summary>
        /// An empty <see cref="PersistentTreeSet">PersistentTreeSet</see>.
        /// </summary>
        public static readonly PersistentTreeSet EMPTY = new PersistentTreeSet(null, PersistentTreeMap.EMPTY);

        #endregion

        #region Ctors and factory methods

        /// <summary>
        /// Create a <see cref="PersistentTreeSet"/> initialized from an <see cref="ISeq"/>.
        /// </summary>
        /// <param name="init">A sequence of elements.</param>
        /// <returns>A <see cref="PersistentTreeSet">PersistentTreeSet</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentTreeSet create(ISeq init)
        {
            PersistentTreeSet ret = EMPTY;
           for (ISeq s = init; s != null; s = s.next() )
                ret = (PersistentTreeSet)ret.cons(s.first());
            return ret;
        }


        /// <summary>
        /// Create a <see cref="PersistentTreeSet"/> using a given comparator function initialized from an <see cref="ISeq"/>.
        /// </summary>
        /// <param name="comp"></param>
        /// <param name="init"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentTreeSet create(IComparer comp, ISeq init)
        {
            PersistentTreeSet ret = new PersistentTreeSet(null, new PersistentTreeMap(null, comp));
            for (ISeq s = init; s != null; s = s.next())
                ret = (PersistentTreeSet)ret.cons(s.first());
            return ret;
        }

        /// <summary>
        /// Initialize a <see cref="PersistentTreeSet">PersistentTreeSet</see> using given metadata and underlying implementation map.
        /// </summary>
        /// <param name="meta">The metadata to attach</param>
        /// <param name="impl">A map to implement the set.</param>
        PersistentTreeSet(IPersistentMap meta, IPersistentMap impl)
            : base(impl)
        {
            _meta = meta;
        }

        #endregion

        #region Equal/equiv overrides

        public override bool Equals(object obj)
        {
            try
            {
                return base.Equals(obj);
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        public override bool equiv(Object obj)
        {
            try
            {
                return base.equiv(obj);
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        #endregion

        #region IObj members

        /// <summary>
        /// Create a copy with new metadata.
        /// </summary>
        /// <param name="meta">The new metadata.</param>
        /// <returns>A copy of the object with new metadata attached.</returns>
        public IObj withMeta(IPersistentMap meta)
        {
            return meta == _meta
                ? this
                : new PersistentTreeSet(meta, _impl);
        }

        #endregion

        #region IMeta Members

        public IPersistentMap meta()
        {
            return _meta;
        }

        #endregion

        #region IPersistentCollection members

        /// <summary>
        /// Returns a new collection that has the given element cons'd on front of the existing collection.
        /// </summary>
        /// <param name="o">An item to put at the front of the collection.</param>
        /// <returns>A new immutable collection with the item added.</returns>
        public override IPersistentCollection cons(object o)
        {
            return contains(o)
                ? this
                : new PersistentTreeSet(meta(), _impl.assoc(o, o));
        }

        /// <summary>
        /// Gets an empty collection of the same type.
        /// </summary>
        /// <returns>An emtpy collection.</returns>
        public override IPersistentCollection empty()
        {
            return new PersistentTreeSet(meta(), (PersistentTreeMap)_impl.empty());
        }

        #endregion

        #region IPersistentSet members

        /// <summary>
        /// Get a set with the given item removed.
        /// </summary>
        /// <param name="key">The item to remove.</param>
        /// <returns>A new set with the item removed.</returns>
        public override IPersistentSet disjoin(object key)
        {
            return (contains(key))
                ? new PersistentTreeSet(meta(), _impl.without(key))
                : this;
        }

        #endregion

        #region Reversible Members

        /// <summary>
        /// Gets an <see cref="ISeq">ISeq</see> to travers the sequence in reverse.
        /// </summary>
        /// <returns>An <see cref="ISeq">ISeq</see> .</returns>
        public ISeq rseq()
        {
            return APersistentMap.KeySeq.create(((Reversible)_impl).rseq());
        }

        #endregion

        #region Sorted Members

        /// <summary>
        /// Returns the comparer used to sort the elements in the collection.
        /// </summary>
        /// <returns>The <c>IComparer</c> used to sort the items.</returns>
        /// <remarks>Would be called <c>Comparer</c> except we need to match the JVM name.</remarks>
        public System.Collections.IComparer comparator()
        {
            return ((Sorted)_impl).comparator();
        }

        /// <summary>
        /// Returns the key to be passed to the comparator to sort the element.
        /// </summary>
        /// <param name="entry">An element in the collection.</param>
        /// <returns>The key used to sort the element.</returns>
        public object entryKey(object entry)
        {
            return entry;
        }

        /// <summary>
        /// Returns an <see cref="ISeq">ISeq</see> to iterate through the collection in the designated direction. 
        /// </summary>
        /// <param name="ascending">A flag indicating if the iteration is ascending or descending.</param>
        /// <returns>A sequence for first/rest iteration.</returns>
        public ISeq seq(bool ascending)
        {
            PersistentTreeMap m = (PersistentTreeMap)_impl;
            return RT.keys(m.seq(ascending));
        }

        /// <summary>
        /// Returns an <see cref="ISeq">ISeq</see> to iterate through the collection in the designated direction starting from a particular key. 
        /// </summary>
        /// <param name="key">The key at which to start the iteration.</param>
        /// <param name="ascending">A flag indicating if the iteration is ascending or descending.</param>
        /// <returns>A sequence for first/rest iteration.</returns>
        /// <remarks>The key need not be in the collection.  If not present, the iteration will start with 
        /// the first element with a key greater than (if asscending) or less than (if descending) the given key.</remarks>
        public ISeq seqFrom(object key, bool ascending)
        {
            PersistentTreeMap m = (PersistentTreeMap)_impl;
            return RT.keys(m.seqFrom(key,ascending));
        }

        #endregion
    }
}
