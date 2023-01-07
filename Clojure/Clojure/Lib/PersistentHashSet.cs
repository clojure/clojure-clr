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


namespace clojure.lang
{
    /// <summary>
    /// A persistent set built on a <see cref="IPersistentMap">IPersistentMap</see>.
    /// </summary>
    [Serializable]
    public class PersistentHashSet: APersistentSet, IObj, IEditableCollection
    {
        #region Data

        readonly IPersistentMap _meta;

        /// <summary>
        /// An empty <see cref="PersistentHashSet">PersistentHashSet</see>.
        /// </summary>
        public static readonly PersistentHashSet EMPTY = new PersistentHashSet(null, PersistentHashMap.EMPTY);

        #endregion

        #region C-tors & factory methods

        /// <summary>
        /// Create a <see cref="PersistentHashSet">PersistentHashSet</see> initialized from an array of items.
        /// </summary>
        /// <param name="init">An array of items.</param>
        /// <returns>A <see cref="PersistentHashSet">PersistentHashSet</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentHashSet create(params object[] init)
        {
            ITransientSet ret = (ITransientSet)EMPTY.asTransient();
            for (int i = 0; i < init.Length; ++i)
                ret = (ITransientSet)ret.conj(init[i]);
            return (PersistentHashSet)ret.persistent();
        }

        /// <summary>
        /// Create a <see cref="PersistentHashSet">PersistentHashSet</see> initialized from an IList of items.
        /// </summary>
        /// <param name="init">An IList of items.</param>
        /// <returns>A <see cref="PersistentHashSet">PersistentHashSet</see>.</returns>
        /// <remarks>This is called just 'create' in the Java version.  CLR can't handle this overload when called on something that is 
        /// both an IList and an ISeq, such as any ASeq.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentHashSet create(IList init)
        {
            ITransientSet ret = (ITransientSet)EMPTY.asTransient();
            foreach (object obj in init)
                ret = (ITransientSet)ret.conj(obj);
            return (PersistentHashSet)ret.persistent();
        }

        /// <summary>
        /// Create a <see cref="PersistentHashSet">PersistentHashSet</see> initialized from an <see cref="ISeq">ISeq</see> of items.
        /// </summary>
        /// <param name="items">An <see cref="ISeq">ISeq</see> of items</param>
        /// <returns>A <see cref="PersistentHashSet">PersistentHashSet</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentHashSet create(ISeq items)
        {
            ITransientSet ret = (ITransientSet)EMPTY.asTransient(); 
            for (; items != null; items = items.next())
                ret = (ITransientSet)ret.conj(items.first());
            return (PersistentHashSet)ret.persistent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentHashSet createWithCheck(params object[] init)
        {
            ITransientSet ret = (ITransientSet)EMPTY.asTransient(); 
            for (int i = 0; i < init.Length; i++)
            {
                ret = (ITransientSet)ret.conj(init[i]);
                if (ret.count() != i + 1)
                    throw new ArgumentException("Duplicate key: " + init[i]);
            }
            return (PersistentHashSet)ret.persistent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentHashSet createWithCheck(IList init)
        {
            ITransientSet ret = (ITransientSet)EMPTY.asTransient(); 
            int i = 0;
            foreach (Object key in init)
            {
                ret = (ITransientSet)ret.conj(key);
                if (ret.count() != i + 1)
                    throw new ArgumentException("Duplicate key: " + key);
                ++i;
            }
            return (PersistentHashSet)ret.persistent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static PersistentHashSet createWithCheck(ISeq items)
        {
            ITransientSet ret = (ITransientSet)EMPTY.asTransient(); 
            for (int i = 0; items != null; items = items.next(), ++i)
            {
                ret = (ITransientSet)ret.conj(items.first());
                if (ret.count() != i + 1)
                    throw new ArgumentException("Duplicate key: " + items.first());
            }
            return (PersistentHashSet)ret.persistent();
        }



        /// <summary>
        /// Initialize a <see cref="PersistentHashSet">PersistentHashSet</see> to use given metadata and underlying map.
        /// </summary>
        /// <param name="meta">The metadata to attach.</param>
        /// <param name="impl">The implementating map.</param>
        PersistentHashSet(IPersistentMap meta, IPersistentMap impl)
            : base(impl)
        {
            _meta = meta;
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
            return (_meta == meta)
                ? this
                : new PersistentHashSet(meta, _impl);
        }

        #endregion
        
        #region IMeta Members

        public IPersistentMap meta()
        {
            return _meta;
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
            return contains(key)
                ? new PersistentHashSet(meta(), _impl.without(key))
                : this;
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
                : new PersistentHashSet(meta(), _impl.assoc(o, o));
        }

        /// <summary>
        /// Gets an empty collection of the same type.
        /// </summary>
        /// <returns>An emtpy collection.</returns>
        public override IPersistentCollection empty()
        {
            return (IPersistentCollection) EMPTY.withMeta(meta());
        }

        #endregion

        #region IEditableCollection Members

        public ITransientCollection asTransient()
        {
            return new TransientHashSet((ITransientMap)((PersistentHashMap)_impl).asTransient());
        }

        #endregion

        #region Transient support

        class TransientHashSet : ATransientSet
        {
            public TransientHashSet(ITransientMap impl)
                : base(impl)
            {
            }

            public override IPersistentCollection persistent()
            {
                return new PersistentHashSet(null, _impl.persistent());
            }
        }


        #endregion
    }
}
