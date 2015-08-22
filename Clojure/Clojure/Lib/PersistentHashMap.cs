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
using System.Threading;
using System.Collections.Generic;

namespace clojure.lang
{

    // TODO: Get rid of Box in favor of 'out' parameer.


    /// <summary>
    /// A persistent rendition of Phil Bagwell's Hash Array Mapped Trie
    /// </summary>
    /// <remarks>
    /// <para>Uses path copying for persistence.</para>
    /// <para>HashCollision leaves vs extended hashing</para>
    /// <para>Node polymorphism vs conditionals</para>
    /// <para>No sub-tree pools or root-resizing</para>
    /// <para>Any errors are Rich Hickey's (so he says), except those that I introduced.</para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1708:IdentifiersShouldDifferByMoreThanCase")]
    [Serializable]
    public class PersistentHashMap : APersistentMap, IEditableCollection, IObj, IMapEnumerable, IMapEnumerableTyped<Object, Object>, IEnumerable, IEnumerable<IMapEntry>, IKVReduce
    {
        #region Data

        /// <summary>
        /// The number of entries in the map.
        /// </summary>
        readonly int _count;

        /// <summary>
        /// The root of the trie.
        /// </summary>
        readonly INode _root;

        /// <summary>
        /// Indicates if the map has the null value as a key.
        /// </summary>
        readonly bool _hasNull;

        /// <summary>
        /// The value associated with the null key, if present.
        /// </summary>
        readonly object _nullValue;

        readonly IPersistentMap _meta;

        /// <summary>
        /// An empty <see cref="PersistentHashMap">PersistentHashMap</see>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EMPTY")]
        public static readonly PersistentHashMap EMPTY = new PersistentHashMap(0, null, false, null);


        static readonly object NotFoundValue = new object();

        #endregion

        #region C-tors & factory methods

        /// <summary>
        /// Create a <see cref="PersistentHashMap">PersistentHashMap</see> initialized from a CLR dictionary.
        /// </summary>
        /// <param name="other">The dictionary to copy from.</param>
        /// <returns>A <see cref="PersistentHashMap">PersistentHashMap</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static IPersistentMap create(IDictionary other)
        {
            ITransientMap ret = (ITransientMap)EMPTY.asTransient();
            foreach (DictionaryEntry e in other)
                ret = ret.assoc(e.Key, e.Value);
            return ret.persistent();
        }

        /// <summary>
        /// Create a <see cref="PersistentHashMap">PersistentHashMap</see> initialized from an array of alternating keys and values.
        /// </summary>
        /// <param name="init">An array of alternating keys and values.</param>
        /// <returns>A <see cref="PersistentHashMap">PersistentHashMap</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static PersistentHashMap create(params object[] init)
        {
            ITransientMap ret = (ITransientMap)EMPTY.asTransient();
            for (int i = 0; i < init.Length; i += 2)
                ret = ret.assoc(init[i], init[i + 1]);
            return (PersistentHashMap)ret.persistent();
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static PersistentHashMap createWithCheck(params object[] init)
        {
            ITransientMap ret = (ITransientMap)EMPTY.asTransient();
            for (int i = 0; i < init.Length; i += 2)
            {
                ret = ret.assoc(init[i], init[i + 1]);
                if (ret.count() != i / 2 + 1)
                    throw new ArgumentException("Duplicate key: " + init[i]);
            }
            return (PersistentHashMap)ret.persistent();
        }

        /// <summary>
        /// Create a <see cref="PersistentHashMap">PersistentHashMap</see> initialized from an IList of alternating keys and values.
        /// </summary>
        /// <param name="init">An IList of alternating keys and values.</param>
        /// <returns>A <see cref="PersistentHashMap">PersistentHashMap</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static PersistentHashMap create1(IList init)
        {
            ITransientMap ret = (ITransientMap)EMPTY.asTransient();
            for (IEnumerator i = init.GetEnumerator(); i.MoveNext(); )
            {
                object key = i.Current;
                if (!i.MoveNext())
                    throw new ArgumentException(String.Format("No value supplied for key: {0}", key));
                object val = i.Current;
                ret = ret.assoc(key, val);
            }
            return (PersistentHashMap)ret.persistent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        static public PersistentHashMap createWithCheck(ISeq items)
        {
            ITransientMap ret = (ITransientMap)EMPTY.asTransient();
            for (int i = 0; items != null; items = items.next().next(), ++i)
            {
                if (items.next() == null)
                    throw new ArgumentException(String.Format("No value supplied for key: {0}", items.first()));
                ret = ret.assoc(items.first(), RT.second(items));
                if (ret.count() != i + 1)
                    throw new ArgumentException("Duplicate key: " + items.first());
            }
            return (PersistentHashMap)ret.persistent();
        }

        /// <summary>
        /// Create a <see cref="PersistentHashMap">PersistentHashMap</see> initialized from 
        /// an <see cref="ISeq">ISeq</see> of alternating keys and values.
        /// </summary>
        /// <param name="items">An <see cref="ISeq">ISeq</see> of alternating keys and values.</param>
        /// <returns>A <see cref="PersistentHashMap">PersistentHashMap</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static PersistentHashMap create(ISeq items)
        {
            ITransientMap ret = (ITransientMap)EMPTY.asTransient();
            for (; items != null; items = items.next().next())
            {
                if ( items.next() == null )
                    throw new ArgumentException(String.Format("No value supplied for key: {0}", items.first()));
                ret = ret.assoc(items.first(), RT.second(items) );
            }
            return (PersistentHashMap)ret.persistent();
        }


        /// <summary>
        /// Create a <see cref="PersistentHashMap">PersistentHashMap</see> with given metadata initialized from an array of alternating keys and values.
        /// </summary>
        /// <param name="meta">The metadata to attach.</param>
        /// <param name="init">An array of alternating keys and values.</param>
        /// <returns>A <see cref="PersistentHashMap">PersistentHashMap</see>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static PersistentHashMap create(IPersistentMap meta, params object[] init)
        {
            return (PersistentHashMap)create(init).withMeta(meta);
        }

        /// <summary>
        /// Initialize a <see cref="PersistentHashMap">PersistentHashMap</see> with a given count and root node.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="root">The root node.</param>
        /// <param name="hasNull"></param>
        /// <param name="nullValue"></param>
        PersistentHashMap(int count, INode root, bool hasNull, object nullValue)
        {
            _meta = null;
            _count = count;
            _root = root;
            _hasNull = hasNull;
            _nullValue = nullValue;
        }

        /// <summary>
        /// Initialize a <see cref="PersistentHashMap">PersistentHashMap</see> with given metadata, count and root node.
        /// </summary>
        /// <param name="meta">The metadata to attach</param>
        /// <param name="count">The count.</param>
        /// <param name="root">The root node.</param>
        /// <param name="hasNull"></param>
        /// <param name="nullValue"></param>
        PersistentHashMap(IPersistentMap meta, int count, INode root, bool hasNull, object nullValue)
        {
            _meta = meta;
            _count = count;
            _root = root;
            _hasNull = hasNull;
            _nullValue = nullValue;
        }

        #endregion

        #region hashing

        static int Hash(object k)
        {
            return Util.hasheq(k);
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
            return new PersistentHashMap(meta, _count, _root, _hasNull, _nullValue);
        }

        #endregion

        #region IMeta Members

        public IPersistentMap meta()
        {
            return _meta;
        }

        #endregion
        
        #region Associative

         /// <summary>
         /// Test if the map contains a key.
         /// </summary>
         /// <param name="key">The key to test for membership</param>
         /// <returns>True if the key is in this map.</returns>
         public override bool containsKey(object key)
        {
            if (key == null)
                return _hasNull;
            return (_root != null) 
                ? _root.Find(0, Hash(key), key, NotFoundValue) != NotFoundValue 
                : false;
        }

        /// <summary>
        /// Returns the key/value pair for this key.
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <returns>The key/value pair for the key, or null if the key is not in the map.</returns>
        public override IMapEntry entryAt(object key)
        {
            if (key == null)
                return _hasNull ? (IMapEntry)Tuple.create(null, _nullValue) : null;
            return (_root != null)
                ? _root.Find(0,Hash(key),key)
                : null;
        }

        /// <summary>
        /// Gets the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The associated value. (Throws an exception if key is not present.)</returns>
        public override object valAt(object key)
        {
            return valAt(key, null);
        }

        /// <summary>
        /// Gets the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="notFound">The value to return if the key is not present.</param>
        /// <returns>The associated value (or <c>notFound</c> if the key is not present.</returns>
        public override object valAt(object key, object notFound)
        {
            if (key == null)
                return _hasNull ? _nullValue : notFound;

            return (_root != null)
                ? _root.Find(0, Hash(key), key, notFound)
                : notFound;
        }

        #endregion

        #region IPersistentMap

        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The value</param>
        /// <returns>A new map with key+value added.</returns>
        /// <remarks>Overwrites an exising value for the <paramref name="key"/>, if present.</remarks>
        public override IPersistentMap assoc(object key, object val)
        {
            if (key == null)
            {
                if (_hasNull && val == _nullValue)
                    return this;
                return new PersistentHashMap(meta(), _hasNull ? _count : _count + 1, _root, true, val);
            }
            Box addedLeaf = new Box(null);
            INode newroot = (_root == null ? BitmapIndexedNode.EMPTY : _root)
                .Assoc(0, Hash(key), key, val, addedLeaf);
            return newroot == _root
                ? this
                : new PersistentHashMap(meta(), addedLeaf.Val == null ? _count : _count + 1, newroot, _hasNull, _nullValue);
        }


        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The value</param>
        /// <returns>A new map with key+value added.</returns>
        /// <remarks>Throws an exception if <paramref name="key"/> has a value already.</remarks>
        public override IPersistentMap assocEx(object key, object val)
        {
            if (containsKey(key))
                throw new InvalidOperationException("Key already present");
            return assoc(key, val);
        }


        /// <summary>
        /// Remove a key entry.
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <returns>A new map with the key removed (or the same map if the key is not contained).</returns>
        public override IPersistentMap without(object key)
        {
            if (key == null)
                return _hasNull ? new PersistentHashMap(meta(), _count - 1, _root, false, null) : this;
            if (_root == null)
                return this;
            INode newroot = _root.Without(0, Hash(key), key);
            if (newroot == _root)
                return this;
            return new PersistentHashMap(meta(), _count - 1, newroot, _hasNull, _nullValue); 
        }

        #endregion

        #region IPersistentCollection

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <returns>The number of items in the collection.</returns>
        public override int count()
        {
            return _count;
        }

        /// <summary>
        /// Gets an ISeq to allow first/rest iteration through the collection.
        /// </summary>
        /// <returns>An ISeq for iteration.</returns>
        public override ISeq seq()
        {
            ISeq s = _root != null ? _root.GetNodeSeq() : null;
            return _hasNull ? new Cons(Tuple.create(null, _nullValue), s) : s;
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
            return new TransientHashMap(this);
        }

        #endregion

        #region TransientHashMap class

        sealed class TransientHashMap : ATransientMap
        {
            #region Data

            [NonSerialized] readonly AtomicReference<Thread> _edit;
            volatile INode _root;
            volatile int _count;
            volatile bool _hasNull;
            volatile object _nullValue;
            readonly Box _leafFlag = new Box(null);

            #endregion

            #region Ctors

            public TransientHashMap(PersistentHashMap m)
                : this(new AtomicReference<Thread>(Thread.CurrentThread), m._root, m._count, m._hasNull, m._nullValue)
            {
            }

            TransientHashMap(AtomicReference<Thread> edit, INode root, int count, bool hasNull, object nullValue)
            {
                _edit = edit;
                _root = root;
                _count = count;
                _hasNull = hasNull;
                _nullValue = nullValue;
            }

            #endregion

            #region ITransientMap Members

            protected override ITransientMap doAssoc(object key, object val)
            {
                if (key == null)
                {
                    if (_nullValue != val)
                        _nullValue = val;
                    if (!_hasNull)
                    {
                        _count++;
                        _hasNull = true;
                    }
                    return this;
                }
                _leafFlag.Val = null;
                INode n = (_root == null ? BitmapIndexedNode.EMPTY : _root)
                    .Assoc(_edit, 0, Hash(key), key, val, _leafFlag);
                if (n != _root)
                    _root = n;
                if (_leafFlag.Val != null)
                    _count++;
                return this;
            }

            protected override ITransientMap doWithout(object key)
            {
                if (key == null)
                {
                    if (!_hasNull)
                        return this;
                    _hasNull = false;
                    _nullValue = null;
                    _count--;
                    return this;
                }

                if (_root == null)
                    return this;

                _leafFlag.Val = null;
                INode n = _root.Without(_edit, 0, Hash(key), key, _leafFlag);
                if (n != _root)
                    _root = n;
               if (_leafFlag.Val != null) 
                    _count--;
                return this;
            }

            protected override IPersistentMap doPersistent()
            {
                _edit.Set(null);
                return new PersistentHashMap(_count, _root, _hasNull, _nullValue);
            }

            #endregion

            #region ILookup Members

            protected override object doValAt(object key, object notFound)
            {
                if (key == null)
                    if (_hasNull)
                        return _nullValue;
                    else
                        return notFound;
                if (_root == null)
                    return notFound;
                return _root.Find(0, Hash(key), key, notFound);                
            }

            //// not part of this interface, but I don't know a better place for it
            //IMapEntry entryAt(Object key)
            //{
            //    return (IMapEntry)_root.find(Hash(key), key);
            //}

            #endregion

            #region Counted Members

            protected override int doCount()
            {
                return _count;
            }

            #endregion

            #region Implementation details

            protected override void EnsureEditable()
            {
                if (_edit.Get() == null )
                    throw new InvalidOperationException("Transient used after persistent! call");
            }

            #endregion
         }

        #endregion

        #region IMapEnumerable, IMapEnumerableTyped, IEnumerable, ... 

        public delegate T KVMangleDel<T>(object k, object v);

        static IEnumerator EmptyEnumerator()
        {
            return EmptyEnumeratorT<Object>();
        }

        static IEnumerator<T> EmptyEnumeratorT<T>()
        {
            yield break;
        }

        static IEnumerator NullIterator(KVMangleDel<Object> d, object nullValue, IEnumerator root)
        {
            yield return d(null, nullValue);
            while (root.MoveNext())
                yield return root.Current;
        }

        static IEnumerator<T> NullIteratorT<T>(KVMangleDel<T> d, object nullValue, IEnumerator<T> root)
        {
            yield return d(null, nullValue);
            while (root.MoveNext())
                yield return root.Current;
        }

        public IEnumerator MakeEnumerator(KVMangleDel<Object> d)
        {
            IEnumerator rootIter = (_root == null ? EmptyEnumerator() : _root.Iterator(d));
            if (!_hasNull)
                return rootIter;
        
            return NullIterator(d,_nullValue, rootIter);
        }

        public IEnumerator<T> MakeEnumeratorT<T>(KVMangleDel<T> d)
        {
            IEnumerator<T> rootIter = (_root == null ? EmptyEnumeratorT<T>() : _root.IteratorT<T>(d));
            if (!_hasNull)
                return rootIter;

            return NullIteratorT(d, _nullValue, rootIter);
        }

        public IEnumerator keyEnumerator()
        {
            return MakeEnumerator((k,v) => k);
        }

        public IEnumerator valEnumerator()
        {
            return MakeEnumerator((k, v) => v);
        }

        public IEnumerator<object> tkeyEnumerator()
        {
            return MakeEnumeratorT((k, v) => k);
        }

        public IEnumerator<object> tvalEnumerator()
        {
            return MakeEnumeratorT((k, v) => v);
        }

        public override IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return MakeEnumeratorT((k, v) => new KeyValuePair<Object,Object>(k,v));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return MakeEnumeratorT((k, v) => Tuple.create(k,v));
        }

        IEnumerator<IMapEntry> IEnumerable<IMapEntry>.GetEnumerator()
        {
            return MakeEnumeratorT((k, v) => (IMapEntry) Tuple.create(k, v));
        }


        #endregion

        #region kvreduce & fold

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "kvreduce")]
        public object kvreduce(IFn f, object init)
        {
            init = _hasNull ? f.invoke(init,null,_nullValue) : init;
            if (RT.isReduced(init))
                return ((IDeref)init).deref();
            if (_root != null)
            {
                init = _root.KVReduce(f, init);
                if (RT.isReduced(init))
                    return ((IDeref)init).deref();
                else
                    return init;
            }
            return init;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "n"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "fold")]
        public object fold(long n, IFn combinef, IFn reducef, IFn fjinvoke, IFn fjtask, IFn fjfork, IFn fjjoin)
        {
            // JVM: we are ignoring n for now
            Func<object> top = new Func<object>(() =>
            {
                object ret = combinef.invoke();
                if (_root != null)
                    ret = combinef.invoke(ret, _root.Fold(combinef, reducef, fjtask, fjfork, fjjoin));
                return _hasNull
                    ? combinef.invoke(ret, reducef.invoke(combinef.invoke(), null, _nullValue))
                    : ret;
            });

            return fjinvoke.invoke(top);
        }

        #endregion
        
        #region INode

        /// <summary>
        /// Interface for all nodes in the trie.
        /// </summary>
        public interface  INode
        {
            /// <summary>
            /// Return a trie with a new key/value pair.
            /// </summary>
            /// <param name="shift"></param>
            /// <param name="hash"></param>
            /// <param name="key"></param>
            /// <param name="val"></param>
            /// <param name="addedLeaf"></param>
            /// <returns></returns>
            INode Assoc(int shift, int hash, object key, object val, Box addedLeaf);

            /// <summary>
            /// Return a trie with the given key removed.
            /// </summary>
            /// <param name="shift"></param>
            /// <param name="hash"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            INode Without(int shift, int hash, object key);

            /// <summary>
            /// Gets the entry containing a given key and its value.
            /// </summary>
            /// <param name="shift"></param>
            /// <param name="hash"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            IMapEntry Find(int shift, int hash, object key);

            /// <summary>
            /// Gets the value associated with a given key, or return a default value if not found.
            /// </summary>
            /// <param name="shift"></param>
            /// <param name="hash"></param>
            /// <param name="key"></param>
            /// <param name="notFound"></param>
            /// <returns></returns>
            object Find(int shift, int hash, object key, object notFound);

            /// <summary>
            /// Return an <see cref="ISeq">ISeq</see> with iterating the tree defined by the current node.
            /// </summary>
            /// <returns>An <see cref="ISeq">ISeq</see> </returns>
            ISeq GetNodeSeq();

            ///// <summary>
            ///// Get the hash for the current ndoe.
            ///// </summary>
            ///// <returns></returns>
            //int getHash();

            /// <summary>
            /// Return a trie with a new key/value pair.
            /// </summary>
            /// <param name="edit"></param>
            /// <param name="shift"></param>
            /// <param name="hash"></param>
            /// <param name="key"></param>
            /// <param name="val"></param>
            /// <param name="addedLeaf"></param>
            /// <returns></returns>
            INode Assoc(AtomicReference<Thread> edit, int shift, int hash, object key, object val, Box addedLeaf);

            /// <summary>
            /// Return a trie with the given key removed.
            /// </summary>
            /// <param name="edit"></param>
            /// <param name="shift"></param>
            /// <param name="hash"></param>
            /// <param name="key"></param>
            /// <param name="removedLeaf"></param>
            /// <returns></returns>
            INode Without(AtomicReference<Thread> edit, int shift, int hash, object key, Box removedLeaf);

            /// <summary>
            /// Perform key-value reduce.
            /// </summary>
            /// <param name="f"></param>
            /// <param name="init"></param>
            /// <returns></returns>
            object KVReduce(IFn f, Object init);

            /// <summary>
            /// Fold
            /// </summary>
            /// <param name="combinef"></param>
            /// <param name="reducef"></param>
            /// <param name="fjtask"></param>
            /// <param name="fjfork"></param>
            /// <param name="fjjoin"></param>
            /// <returns></returns>
            object Fold(IFn combinef, IFn reducef, IFn fjtask, IFn fjfork, IFn fjjoin);

            IEnumerator Iterator(KVMangleDel<Object> d);
            IEnumerator<T> IteratorT<T>(KVMangleDel<T> d);
        }

        #endregion

        #region Array manipulation

        static INode[] CloneAndSet(INode[] array, int i, INode a)
        {
            INode[] clone = (INode[])array.Clone();
            clone[i] = a;
            return clone;
        }

        static object[] CloneAndSet(object[] array, int i, object a)
        {
            Object[] clone = (object[])array.Clone();
            clone[i] = a;
            return clone;
        }

        static object[] CloneAndSet(object[] array, int i, object a, int j, object b)
        {
            object[] clone = (object[])array.Clone();
            clone[i] = a;
            clone[j] = b;
            return clone;
        }

        private static object[] RemovePair(object[] array, int i)
        {
            object[] newArray = new Object[array.Length - 2];
            Array.Copy(array, 0, newArray, 0, 2 * i);
            Array.Copy(array, 2 * (i + 1), newArray, 2 * i, newArray.Length - 2 * i);
            return newArray;
        }

 

        #endregion

        #region Node factories

        private static INode CreateNode(int shift, object key1, object val1, int key2hash, object key2, object val2)
        {
            int key1hash = Hash(key1);
            if (key1hash == key2hash)
                return new HashCollisionNode(null, key1hash, 2, new object[] { key1, val1, key2, val2 });
            Box _ = new Box(null);
            AtomicReference<Thread> edit = new AtomicReference<Thread>();
            return BitmapIndexedNode.EMPTY
                .Assoc(edit, shift, key1hash, key1, val1, _)
                .Assoc(edit, shift, key2hash, key2, val2, _);
        }

        private static INode CreateNode(AtomicReference<Thread> edit, int shift, Object key1, Object val1, int key2hash, Object key2, Object val2)
        {
            int key1hash = Hash(key1);
            if (key1hash == key2hash)
                return new HashCollisionNode(null, key1hash, 2, new Object[] { key1, val1, key2, val2 });
            Box _ = new Box(null);
            return BitmapIndexedNode.EMPTY
                .Assoc(edit, shift, key1hash, key1, val1, _)
                .Assoc(edit, shift, key2hash, key2, val2, _);
        }

        #endregion

        #region Other details

        static int Bitpos(int hash, int shift)
        {
            return 1 << Util.Mask(hash, shift);
        }

        #endregion

        #region ArrayNode

        [Serializable]
        sealed class ArrayNode : INode
        {
            #region Data

            int _count;
            readonly INode[] _array;
            [NonSerialized]
            readonly AtomicReference<Thread> _edit;

            #endregion

            #region C-tors

            public ArrayNode(AtomicReference<Thread> edit, int count, INode[] array)
            {
                _array = array;
                _edit = edit;
                _count = count;
            }

            #endregion

            #region INode Members

            public INode Assoc(int shift, int hash, object key, object val, Box addedLeaf)
            {
                int idx = Util.Mask(hash, shift);
                INode node = _array[idx];
                if (node == null)
                    return new ArrayNode(null, _count + 1, CloneAndSet(_array, idx, BitmapIndexedNode.EMPTY.Assoc(shift + 5, hash, key, val, addedLeaf)));
                INode n = node.Assoc(shift + 5, hash, key, val, addedLeaf);
                if (n == node)
                    return this;
                return new ArrayNode(null, _count, CloneAndSet(_array, idx, n));
            }

            public INode Without(int shift, int hash, object key)
            {
                int idx = Util.Mask(hash, shift);
                INode node = _array[idx];
                if (node == null)
                    return this;
                INode n = node.Without(shift + 5, hash, key);
                if (n == node)
                    return this;
                if (n == null)
                {
                    if (_count <= 8) // shrink
                        return pack(null, idx);
                    return new ArrayNode(null, _count - 1, CloneAndSet(_array, idx, n));
                }
                else
                    return new ArrayNode(null, _count, CloneAndSet(_array, idx, n));
            }

            public IMapEntry Find(int shift, int hash, object key)
            {
                int idx = Util.Mask(hash, shift);
                INode node = _array[idx];
                if (node == null)
                    return null;
                return node.Find(shift + 5, hash, key);
            }

            public object Find(int shift, int hash, object key, object notFound)
            {
                int idx = Util.Mask(hash, shift);
                INode node = _array[idx];
                if (node == null)
                    return notFound;
                return node.Find(shift + 5, hash, key, notFound);
            }

            public ISeq GetNodeSeq()
            {
                return Seq.create(_array);
            }

            public INode Assoc(AtomicReference<Thread> edit, int shift, int hash, object key, object val, Box addedLeaf)
            {
                int idx = Util.Mask(hash, shift);
                INode node = _array[idx];
                if (node == null)
                {
                    ArrayNode editable = EditAndSet(edit, idx, BitmapIndexedNode.EMPTY.Assoc(edit, shift + 5, hash, key, val, addedLeaf));
                    editable._count++;
                    return editable;
                }
                INode n = node.Assoc(edit, shift + 5, hash, key, val, addedLeaf);
                if (n == node)
                    return this;
                return EditAndSet(edit, idx, n);
            }

            public INode Without(AtomicReference<Thread> edit, int shift, int hash, object key, Box removedLeaf)
            {
                int idx = Util.Mask(hash, shift);
                INode node = _array[idx];
                if (node == null)
                    return this;
                INode n = node.Without(edit, shift + 5, hash, key, removedLeaf);
                if (n == node)
                    return this;
                if (n == null)
                {
                    if (_count <= 8) // shrink
                        return pack(edit, idx);
                    ArrayNode editable = EditAndSet(edit, idx, n);
                    editable._count--;
                    return editable;
                }
                return EditAndSet(edit, idx, n);
            }

            public object KVReduce(IFn f, object init)
            {
                foreach (INode node in _array)
                {
                    if (node != null)
                    {
                        init = node.KVReduce(f, init);
                        if (RT.isReduced(init))
                            return init;
                    }
                }
                return init;
            }

            public object Fold(IFn combinef, IFn reducef, IFn fjtask, IFn fjfork, IFn fjjoin)
            {
                List<Func<object>> tasks = new List<Func<object>>();
                foreach (INode node in _array) 
                {
                    tasks.Add(() =>
                        {
                            return node.Fold(combinef, reducef, fjtask, fjfork, fjjoin);
                        }
                    );
                }

                return FoldTasks(tasks, combinef, fjtask, fjfork, fjjoin);
            }

            static object FoldTasks(List<Func<object>> tasks, IFn combinef, IFn fjtask, IFn fjfork, IFn fjjoin)
            {

                if (tasks.Count == 0)
                    return combinef.invoke();

                if (tasks.Count == 1 )
                    return tasks[0].Invoke();
                
                int half = tasks.Count / 2;
                List<Func<object>> t1 = tasks.GetRange(0, half);
                List<Func<object>> t2 = tasks.GetRange(half, tasks.Count - half);
                object forked = fjfork.invoke(fjtask.invoke(new Func<object>(() => { return FoldTasks(t2, combinef, fjtask, fjfork, fjjoin); })));

                return combinef.invoke(FoldTasks(t1, combinef, fjtask, fjfork, fjjoin), fjjoin.invoke(forked));
            }

            #endregion

            #region Implementation details

            ArrayNode EnsureEditable(AtomicReference<Thread> edit)
            {
                if (_edit == edit)
                    return this;
                return new ArrayNode(edit, _count, (INode[])_array.Clone());
            }

            ArrayNode EditAndSet(AtomicReference<Thread> edit, int i, INode n)
            {
                ArrayNode editable = EnsureEditable(edit);
                editable._array[i] = n;
                return editable;
            }

            INode pack(AtomicReference<Thread> edit, int idx)
            {
                Object[] newArray = new Object[2 * (_count - 1)];
                int j = 1;
                int bitmap = 0;
                for (int i = 0; i < idx; i++)
                    if (_array[i] != null)
                    {
                        newArray[j] = _array[i];
                        bitmap |= 1 << i;
                        j += 2;
                    }
                for (int i = idx + 1; i < _array.Length; i++)
                    if (_array[i] != null)
                    {
                        newArray[j] = _array[i];
                        bitmap |= 1 << i;
                        j += 2;
                    }
                return new BitmapIndexedNode(edit, bitmap, newArray);
            }

            #endregion

            #region Seq implementation

            [Serializable]
            class Seq : ASeq
            {
                #region Data

                readonly INode[] _nodes;
                readonly int _i;
                readonly ISeq _s;

                #endregion

                #region C-tors

                static public ISeq create(INode[] nodes)
                {
                    return create(null, nodes, 0, null);
                }

                static ISeq create(IPersistentMap meta, INode[] nodes, int i, ISeq s)
                {
                    if (s != null)
                        return new Seq(meta, nodes, i, s);
                    for (int j = i; j < nodes.Length; j++)
                        if (nodes[j] != null)
                        {
                            ISeq ns = nodes[j].GetNodeSeq();
                            if (ns != null)
                                return new Seq(meta, nodes, j + 1, ns);
                        }
                    return null;
                }

                Seq(IPersistentMap meta, INode[] nodes, int i, ISeq s)
                    : base(meta)
                {
                    _nodes = nodes;
                    _i = i;
                    _s = s;
                }

                #endregion

                #region IObj methods

                public override IObj withMeta(IPersistentMap meta)
                {
                    return new Seq(meta, _nodes, _i, _s);
                }

                #endregion

                #region ISeq methods

                public override object first()
                {
                    return _s.first();
                }

                public override ISeq next()
                {
                    return create(null, _nodes, _i, _s.next());
                }

                #endregion
            }

            #endregion

            #region iterators

            public IEnumerator Iterator(KVMangleDel<Object> d)
            {
                foreach (INode node in _array)
                {
                    if (node != null)
                    {
                        IEnumerator ie = node.Iterator(d);

                        while (ie.MoveNext())
                            yield return ie.Current;
                    }
                }
            }

            public IEnumerator<T> IteratorT<T>(KVMangleDel<T> d)
            {
                foreach (INode node in _array)
                {
                    if (node != null)
                    {
                        IEnumerator<T> ie = node.IteratorT(d);

                        while (ie.MoveNext())
                            yield return ie.Current;
                    }
                }
            }

            #endregion
        }

        #endregion

        #region BitmapIndexNode

        /// <summary>
        ///  Represents an internal node in the trie, not full.
        /// </summary>
        [Serializable]
        sealed class BitmapIndexedNode : INode
        {
            #region Data

            internal static readonly BitmapIndexedNode EMPTY = new BitmapIndexedNode(null, 0, new object[0]);

            int _bitmap;
            object[] _array;
            [NonSerialized]
            readonly AtomicReference<Thread> _edit;

            #endregion

            #region Calculations

            int Index(int bit)
            {
                return Util.BitCount(_bitmap & (bit - 1));
            }

            #endregion

            #region C-tors & factory methods

            internal BitmapIndexedNode(AtomicReference<Thread> edit, int bitmap, object[] array)
            {
                _bitmap = bitmap;
                _array = array;
                _edit = edit;
            }

            #endregion

            #region INode Members

            public INode Assoc(int shift, int hash, object key, object val, Box addedLeaf)
            {
                int bit = Bitpos(hash, shift);
                int idx = Index(bit);
                if ((_bitmap & bit) != 0)
                {
                    object keyOrNull = _array[2 * idx];
                    object valOrNode = _array[2 * idx + 1];
                    if (keyOrNull == null)
                    {
                        INode n = ((INode)valOrNode).Assoc(shift + 5, hash, key, val, addedLeaf);
                        if (n == valOrNode)
                            return this;
                        return new BitmapIndexedNode(null, _bitmap, CloneAndSet(_array, 2 * idx + 1, n));
                    }
                    if ( Util.equiv(key,keyOrNull))
                    {
                        if ( val == valOrNode)
                            return this;
                        return new BitmapIndexedNode(null,_bitmap,CloneAndSet(_array,2*idx+1,val));
                    }
                    addedLeaf.Val = addedLeaf;
                    return new BitmapIndexedNode(null,_bitmap,
                        CloneAndSet(_array,
                                    2*idx,
                                    null,
                                    2*idx+1,
                                    CreateNode(shift+5,keyOrNull,valOrNode,hash,key,val)));
                }
                else
                {
                    int n = Util.BitCount(_bitmap);
                    if ( n >= 16 )
                    {
                        INode [] nodes = new INode[32];
                        int jdx = Util.Mask(hash,shift);
                        nodes[jdx] = EMPTY.Assoc(shift+5,hash,key,val,addedLeaf);
                        int j=0;
                        for ( int i=0; i < 32; i++ )
                            if ( ( (_bitmap >>i) & 1) != 0 )
                            {
                                if ( _array[j] ==  null )
                                   nodes[i] = (INode) _array[j+1];
                                else
                                    nodes[i] = EMPTY.Assoc(shift+5,Hash(_array[j]),_array[j],_array[j+1], addedLeaf);
                                j += 2;
                            }
                        return new ArrayNode(null,n+1,nodes);
                    }
                    else
                    {
                        object[] newArray = new object[2*(n+1)];
                        Array.Copy(_array, 0, newArray, 0, 2*idx);
                        newArray[2*idx] = key;
                        addedLeaf.Val = addedLeaf;
                        newArray[2*idx+1] = val;
                        Array.Copy(_array, 2*idx, newArray, 2*(idx + 1), 2*(n - idx));
                        return new BitmapIndexedNode(null, _bitmap | bit, newArray);
                    }           
                }
            }


            public INode Without(int shift, int hash, object key)
            {
                int bit = Bitpos(hash, shift);
                if ((_bitmap & bit) == 0)
                    return this;

                int idx = Index(bit);
                object keyOrNull = _array[2 * idx];
                object valOrNode = _array[2 * idx + 1];
                if ( keyOrNull == null )
                {
                    INode n = ((INode)valOrNode).Without(shift+5,hash,key);
                    if ( n == valOrNode)
                        return this;
                    if ( n != null )
                        return new BitmapIndexedNode(null,_bitmap,CloneAndSet(_array,2*idx+1,n));
                    if ( _bitmap == bit )
                        return null;
                    return new BitmapIndexedNode(null,_bitmap^bit,RemovePair(_array,idx));
                }
                if ( Util.equiv(key,keyOrNull))
                    // TODO: Collapse  (TODO in Java code)
                    return new BitmapIndexedNode(null,_bitmap^bit,RemovePair(_array,idx));
                return this;
            }

            public IMapEntry Find(int shift, int hash, object key)
            {
                int bit = Bitpos(hash, shift);
                if ((_bitmap & bit) == 0)
                    return null;
                int idx = Index(bit);
                 object keyOrNull = _array[2 * idx];
                object valOrNode = _array[2 * idx + 1];
                if ( keyOrNull == null )
                    return ((INode)valOrNode).Find(shift+5,hash,key);
                if ( Util.equiv(key,keyOrNull))
                    return (IMapEntry) Tuple.create(keyOrNull,valOrNode);
                return null;
            }


            public Object Find(int shift, int hash, Object key, Object notFound)
            {
                int bit = Bitpos(hash, shift);
                if ((_bitmap & bit) == 0)
                    return notFound;
                int idx = Index(bit);
                Object keyOrNull = _array[2 * idx];
                Object valOrNode = _array[2 * idx + 1];
                if (keyOrNull == null)
                    return ((INode)valOrNode).Find(shift + 5, hash, key, notFound);
                if (Util.equiv(key, keyOrNull))
                    return valOrNode;
                return notFound;
            }

            public ISeq GetNodeSeq()
            {
                return NodeSeq.Create(_array);
            }

            public INode Assoc(AtomicReference<Thread> edit, int shift, int hash, object key, object val, Box addedLeaf)
            {
                int bit = Bitpos(hash, shift);
                int idx = Index(bit);
                if ((_bitmap & bit) != 0)
                {
                    object keyOrNull = _array[2 * idx];
                    object valOrNode = _array[2 * idx + 1];
                    if (keyOrNull == null)
                    {
                        INode n = ((INode)valOrNode).Assoc(edit, shift + 5, hash, key, val, addedLeaf);
                        if (n == valOrNode)
                            return this;
                        return EditAndSet(edit, 2 * idx + 1, n);
                    }
                    if (Util.equiv(key, keyOrNull))
                    {
                        if (val == valOrNode)
                            return this;
                        return EditAndSet(edit, 2 * idx + 1, val);
                    }
                    addedLeaf.Val = addedLeaf;
                    return EditAndSet(edit,
                        2*idx,null,
                        2*idx+1,CreateNode(edit,shift+5,keyOrNull,valOrNode,hash,key,val));
                }
                else
                {int n = Util.BitCount(_bitmap);
                    if ( n*2 < _array.Length )
                    {
                        addedLeaf.Val = addedLeaf;
                        BitmapIndexedNode editable = EnsureEditable(edit);
                        Array.Copy(editable._array,2*idx,editable._array,2*(idx+1),2*(n-idx));
                        editable._array[2*idx] = key;
                        editable._array[2*idx+1] = val;
                        editable._bitmap |= bit;
                        return editable;
                    }
                    if ( n >= 16 )
                    {
                        INode[] nodes = new INode[32];
                        int jdx = Util.Mask(hash,shift);
                        nodes[jdx] = EMPTY.Assoc(edit,shift+5,hash,key,val,addedLeaf);
                        int j=0;
                        for ( int i=0; i<32; i++ )
                            if (((_bitmap>>i) & 1) != 0 )
                            {
                                if ( _array[j] == null )
                                    nodes[i] = (INode)_array[j+1];
                                else
                                    nodes[i] = EMPTY.Assoc(edit,shift+5,Hash(_array[j]), _array[j], _array[j+1], addedLeaf);
                                j += 2;
                            }
                        return new ArrayNode(edit,n+1,nodes);
                    }
                    else
                    {
                        object[] newArray = new object[2*(n+4)];
                        Array.Copy(_array,0,newArray,0,2*idx);
                        newArray[2*idx] = key;
                        addedLeaf.Val = addedLeaf;
                        newArray[2 * idx + 1] = val;
                        Array.Copy(_array,2*idx,newArray,2*(idx+1),2*(n-idx));
                        BitmapIndexedNode editable = EnsureEditable(edit);
                        editable._array = newArray;
                        editable._bitmap |= bit;
                        return editable;
                    }
                }
            }



            public INode Without(AtomicReference<Thread> edit, int shift, int hash, object key, Box removedLeaf)
            {
                int bit = Bitpos(hash, shift);
                if ((_bitmap & bit) == 0)
                    return this;
                int idx = Index(bit);
                Object keyOrNull = _array[2 * idx];
                Object valOrNode = _array[2 * idx + 1];
                if (keyOrNull == null)
                {
                    INode n = ((INode)valOrNode).Without(edit, shift + 5, hash, key, removedLeaf);
                    if (n == valOrNode)
                        return this;
                    if (n != null)
                        return EditAndSet(edit, 2 * idx + 1, n);
                    if (_bitmap == bit)
                        return null;
                    return EditAndRemovePair(edit, bit, idx);
                }
                if (Util.equiv(key, keyOrNull))
                {
                    removedLeaf.Val = removedLeaf;
                    // TODO: collapse
                    return EditAndRemovePair(edit, bit, idx);
                }
                return this;
            }

            public object KVReduce(IFn f, object init)
            {
                return NodeSeq.KvReduce(_array, f, init);
            }

            public object Fold(IFn combinef, IFn reducef, IFn fjtask, IFn fjfork, IFn fjjoin)
            {
                return NodeSeq.KvReduce(_array, reducef, combinef.invoke());
            }


            #endregion

            #region Implementation

            private BitmapIndexedNode EditAndSet(AtomicReference<Thread> edit, int i, Object a)
            {
                BitmapIndexedNode editable = EnsureEditable(edit);
                editable._array[i] = a;
                return editable;
            }

            private BitmapIndexedNode EditAndSet(AtomicReference<Thread> edit, int i, Object a, int j, Object b)
            {
                BitmapIndexedNode editable = EnsureEditable(edit);
                editable._array[i] = a;
                editable._array[j] = b;
                return editable;
            }

            private BitmapIndexedNode EditAndRemovePair(AtomicReference<Thread> edit, int bit, int i)
            {
                if (_bitmap == bit)
                    return null;
                BitmapIndexedNode editable = EnsureEditable(edit);
                editable._bitmap ^= bit;
                Array.Copy(editable._array, 2 * (i + 1), editable._array, 2 * i, editable._array.Length - 2 * (i + 1));
                editable._array[editable._array.Length - 2] = null;
                editable._array[editable._array.Length - 1] = null;
                return editable;
            }

            BitmapIndexedNode EnsureEditable(AtomicReference<Thread> edit)
            {
                if (_edit == edit)
                    return this;
                int n = Util.BitCount(_bitmap);
                object[] newArray = new Object[n >= 0 ? 2 * (n + 1) : 4];  // make room for next assoc
                Array.Copy(_array, newArray, 2 * n);
                return new BitmapIndexedNode(edit, _bitmap, newArray);
            }

            #endregion

            #region iterators

            public IEnumerator Iterator(KVMangleDel<Object> d)
           { 
                return NodeIter.GetEnumerator(_array, d);
            }

            public IEnumerator<T> IteratorT<T>(KVMangleDel<T> d)
            {
                return NodeIter.GetEnumeratorT(_array, d);
            }

            #endregion
        }

        #endregion

        #region HashCollisionNode

        /// <summary>
        /// Represents a leaf node corresponding to multiple map entries, all with keys that have the same hash value.
        /// </summary>
        [Serializable]
        sealed class HashCollisionNode : INode
        {
            #region Data

            readonly int _hash;
            int _count;
            object[] _array;
            [NonSerialized]
            readonly AtomicReference<Thread> _edit;

            #endregion

            #region C-tors

            public HashCollisionNode(AtomicReference<Thread> edit, int hash, int count, params object[] array)
            {
                _edit = edit;
                _hash = hash;
                _count = count;
                _array = array;
            }

            #endregion

            #region details

            int FindIndex(object key)
            {
                for (int i = 0; i < 2 * _count; i += 2)
                {
                    if (Util.equiv(key, _array[i]))
                        return i;
                }
                return -1;
            }

            #endregion

            #region INode Members

            public INode Assoc(int shift, int hash, object key, object val, Box addedLeaf)
            {
                if (_hash == hash)
                {
                    int idx = FindIndex(key);
                    if (idx != -1)
                    {
                        if (_array[idx + 1] == val)
                            return this;
                        return new HashCollisionNode(null, hash, _count, CloneAndSet(_array, idx + 1, val));
                    }
                    Object[] newArray = new Object[2 * (_count + 1)];
                    Array.Copy(_array, 0, newArray, 0, 2 * _count);
                    newArray[2 * _count] = key;
                    newArray[2 * _count + 1] = val;
                    addedLeaf.Val = addedLeaf;
                    return new HashCollisionNode(_edit, hash, _count + 1, newArray);
                }
                // nest it in a bitmap node
                return new BitmapIndexedNode(null, Bitpos(_hash, shift), new object[] { null, this })
                    .Assoc(shift, hash, key, val, addedLeaf);
            }

            public INode Without(int shift, int hash, object key)
            {
                int idx = FindIndex(key);
                if (idx == -1)
                    return this;
                if (_count == 1)
                    return null;
                return new HashCollisionNode(null, hash, _count - 1, RemovePair(_array, idx / 2));
            }

            public IMapEntry Find(int shift, int hash, object key)
            {
                int idx = FindIndex(key);
                if (idx < 0)
                    return null;
                if (Util.equiv(key, _array[idx]))
                    return (IMapEntry) Tuple.create(_array[idx], _array[idx + 1]);
                return null;
            }

            public Object Find(int shift, int hash, Object key, Object notFound)
            {
                int idx = FindIndex(key);
                if (idx < 0)
                    return notFound;
                if (Util.equiv(key, _array[idx]))
                    return _array[idx + 1];
                return notFound;
            }

            public ISeq GetNodeSeq()
            {
                return NodeSeq.Create(_array);
            }

            public INode Assoc(AtomicReference<Thread> edit, int shift, int hash, Object key, Object val, Box addedLeaf)
            {
                if (hash == _hash)
                {
                    int idx = FindIndex(key);
                    if (idx != -1)
                    {
                        if (_array[idx + 1] == val)
                            return this;
                        return EditAndSet(edit, idx + 1, val);
                    }
                    if (_array.Length > 2 * _count)
                    {
                        addedLeaf.Val = addedLeaf;
                        HashCollisionNode editable = EditAndSet(edit, 2 * _count, key, 2 * _count + 1, val);
                        editable._count++;
                        return editable;
                    }
                    object[] newArray = new object[_array.Length + 2];
                    Array.Copy(_array, 0, newArray, 0, _array.Length);
                    newArray[_array.Length] = key;
                    newArray[_array.Length + 1] = val;
                    addedLeaf.Val = addedLeaf;
                    return EnsureEditable(edit, _count + 1, newArray);
                }
                // nest it in a bitmap node
                return new BitmapIndexedNode(edit, Bitpos(_hash, shift), new object[] { null, this, null, null })
                    .Assoc(edit, shift, hash, key, val, addedLeaf);
            }

            public INode Without(AtomicReference<Thread> edit, int shift, int hash, Object key, Box removedLeaf)
            {
                int idx = FindIndex(key);
                if (idx == -1)
                    return this;
                removedLeaf.Val = removedLeaf;
                if (_count == 1)
                    return null;
                HashCollisionNode editable = EnsureEditable(edit);
                editable._array[idx] = editable._array[2 * _count - 2];
                editable._array[idx + 1] = editable._array[2 * _count - 1];
                editable._array[2 * _count - 2] = editable._array[2 * _count - 1] = null;
                editable._count--;
                return editable;
            }

            public object KVReduce(IFn f, object init)
            {
                return NodeSeq.KvReduce(_array, f, init);
            }

            public object Fold(IFn combinef, IFn reducef, IFn fjtask, IFn fjfork, IFn fjjoin)
            {
                return NodeSeq.KvReduce(_array, reducef, combinef.invoke());
            }

            #endregion

            #region Implementation

            HashCollisionNode EnsureEditable(AtomicReference<Thread> edit)
            {
                if (_edit == edit)
                    return this;
                object[] newArray = new Object[2 * (_count + 1)];  // make room for next assoc
                System.Array.Copy(_array, 0, newArray, 0, 2 * _count);
                return new HashCollisionNode(edit, _hash, _count, newArray);
            }

            HashCollisionNode EnsureEditable(AtomicReference<Thread> edit, int count, Object[] array)
            {
                if (_edit == edit)
                {
                    _array = array;
                    _count = count;
                    return this;
                }
                return new HashCollisionNode(edit, _hash, count, array);
            }

            HashCollisionNode EditAndSet(AtomicReference<Thread> edit, int i, Object a)
            {
                HashCollisionNode editable = EnsureEditable(edit);
                editable._array[i] = a;
                return editable;
            }

            HashCollisionNode EditAndSet(AtomicReference<Thread> edit, int i, Object a, int j, Object b)
            {
                HashCollisionNode editable = EnsureEditable(edit);
                editable._array[i] = a;
                editable._array[j] = b;
                return editable;
            }

            #endregion

            #region iterators

            public IEnumerator Iterator(KVMangleDel<Object> d)
            {
                return NodeIter.GetEnumerator(_array, d);
            }

            public IEnumerator<T> IteratorT<T>(KVMangleDel<T> d)
            {
                return NodeIter.GetEnumeratorT(_array, d);
            }

            #endregion

        }

        #endregion

        #region NodeIter

        static class NodeIter
        {
            public static IEnumerator GetEnumerator(object[] array, KVMangleDel<Object> d)
            {
                for ( int i=0; i< array.Length; i+=2)
                {
                    object key = array[i];
                    object nodeOrVal = array[i+1];
                    if (key != null)
                        yield return d(key, nodeOrVal);
                    else if ( nodeOrVal != null )
                    {
                        IEnumerator ie = ((INode)nodeOrVal).Iterator(d);
                        while (ie.MoveNext())
                            yield return ie.Current;
                    }
                }
            }

            public static IEnumerator<T> GetEnumeratorT<T>(object[] array, KVMangleDel<T> d)
            {
                for (int i = 0; i < array.Length; i += 2)
                {
                    object key = array[i];
                    object nodeOrVal = array[i + 1];
                    if (key != null)
                        yield return d(key, nodeOrVal);
                    else if (nodeOrVal != null)
                    {
                        IEnumerator<T> ie = ((INode)nodeOrVal).IteratorT(d);
                        while (ie.MoveNext())
                            yield return ie.Current;
                    }
                }
            }
        }

        #endregion

        #region NodeSeq

        [Serializable]
        sealed class NodeSeq : ASeq
        {

            #region Data

            readonly object[] _array;
            readonly int _i;
            readonly ISeq _s;

            #endregion

            #region Ctors

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            NodeSeq(object[] array, int i)
                : this(null, array, i, null)
            {
            }

            public static ISeq Create(object[] array)
            {
                return Create(array, 0, null);
            }

            private static ISeq Create(object[] array, int i, ISeq s)
            {
                if (s != null)
                    return new NodeSeq(null, array, i, s);
                for (int j = i; j < array.Length; j += 2)
                {
                    if (array[j] != null)
                        return new NodeSeq(null, array, j, null);
                    INode node = (INode)array[j + 1];
                    if (node != null)
                    {
                        ISeq nodeSeq = node.GetNodeSeq();
                        if (nodeSeq != null)
                            return new NodeSeq(null, array, j + 2, nodeSeq);
                    }
                }
                return null;
            }

            NodeSeq(IPersistentMap meta, Object[] array, int i, ISeq s)
                : base(meta)
            {
                _array = array;
                _i = i;
                _s = s;
            }

            #endregion

            #region IObj methods

            public override IObj withMeta(IPersistentMap meta)
            {
                return new NodeSeq(meta, _array, _i, _s);
            }

            #endregion

            #region ISeq methods

            public override object first()
            {
                if (_s != null)
                    return _s.first();
                return Tuple.create(_array[_i], _array[_i + 1]);
            }

            public override ISeq next()
            {
                if (_s != null)
                    return Create(_array, _i, _s.next());
                return Create(_array, _i + 2, null);
            }
            #endregion

            #region KvReduce

            static public object KvReduce(object[] array, IFn f, object init)
            {
                for (int i = 0; i < array.Length; i += 2)
                {
                    if (array[i] != null)
                        init = f.invoke(init, array[i], array[i + 1]);
                    else
                    {
                        INode node = (INode)array[i + 1];
                        if (node != null)
                            init = node.KVReduce(f, init);
                    }
                    if (RT.isReduced(init))
                        return init;
                }
                return init;
            }

            #endregion
        }

        #endregion
    }
}
