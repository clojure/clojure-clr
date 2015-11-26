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
using System.Threading;

namespace clojure.lang
{

    /// <summary>
    /// Implements a persistent map as an array of alternating keys/values(suitable for small maps only).
    /// </summary>
    /// <remarks>
    /// <para>Note that instances of this class are constant values, i.e., add/remove etc return new values.</para>
    /// <para>Copies the array on every change, so only appropriate for <i>very small</i> maps</para>
    /// <para><value>null</value> keys and values are okay, 
    /// but you won't be able to distinguish a <value>null</value> value via <see cref="valAt">valAt</see> --
    /// use <see cref="contains">contains</see> or <see cref="entryAt">entryAt</see>.</para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1708:IdentifiersShouldDifferByMoreThanCase")]
    [Serializable]
    public class PersistentArrayMap : APersistentMap, IObj, IEditableCollection, IMapEnumerable, IMapEnumerableTyped<Object,Object>, IEnumerable, IEnumerable<IMapEntry>, IKVReduce
    {
        #region Data

        /// <summary>
        /// The maximum number of entries to hold using this implementation.
        /// </summary>
        /// <remarks>
        /// <para>Operations adding more than this number of entries should switch to another implementation.</para>
        /// <para>The value was changed from 8 to 16 in Java Rev 1159 to improve proxy perf -- we don't have proxy yet,
        /// but I changed it here anyway.</para>
        /// </remarks>
        internal const int HashtableThreshold = 16;

        /// <summary>
        /// The array holding the key/value pairs.
        /// </summary>
        /// <remarks>The i-th pair is in _array[2*i] and _array[2*i+1].</remarks>
        protected readonly object[] _array;

        /// <summary>
        /// An empty <see cref="PersistentArrayMap">PersistentArrayMap</see>. Constant.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EMPTY")]
        public static readonly PersistentArrayMap EMPTY = new PersistentArrayMap();


        readonly IPersistentMap _meta;

        #endregion

        #region C-tors and factory methods

        /// <summary>
        /// Create a <see cref="PersistentArrayMap">PersistentArrayMap</see> (if small enough, else create a <see cref="PersistentHashMap">PersistentHashMap</see>.
        /// </summary>
        /// <param name="other">The BCL map to initialize from</param>
        /// <returns>A new persistent map.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static IPersistentMap create(IDictionary other)
        {
            ITransientMap ret = (ITransientMap)EMPTY.asTransient();
            foreach (DictionaryEntry de in other)
                ret = ret.assoc(de.Key, de.Value);
            return ret.persistent();

        }

        /// <summary>
        /// Create a <see cref="PersistentArrayMap">PersistentArrayMap</see> with new data but same metadata as the current object.
        /// </summary>
        /// <param name="init">The new key/value array</param>
        /// <returns>A new <see cref="PersistentArrayMap">PersistentArrayMap</see>.</returns>
        /// <remarks>The array is used directly.  Do not modify externally or immutability is sacrificed.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        PersistentArrayMap create(params object[] init)
        {
            return new PersistentArrayMap(meta(), init);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static PersistentArrayMap createWithCheck(Object[] init)
        {
            for (int i = 0; i < init.Length; i += 2)
            {
                for (int j = i + 2; j < init.Length; j += 2)
                {
                    if (EqualKey(init[i], init[j]))
                        throw new ArgumentException("Duplicate key: " + init[i]);
                }
            }
            return new PersistentArrayMap(init);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "AsIf"), 
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
        public static PersistentArrayMap createAsIfByAssoc(Object[] init)
        {
            if ((init.Length & 1) == 1)
                throw new ArgumentException(String.Format("No value supplied for key: {0}", init[init.Length - 1]), "init");

            // ClojureJVM says: If this looks like it is doing busy-work, it is because it
            // is achieving these goals: O(n^2) run time like
            // createWithCheck(), never modify init arg, and only
            // allocate memory if there are duplicate keys.
            int n = 0;
            for (int i = 0; i < init.Length; i += 2)
            {
                bool duplicateKey = false;
                for (int j = 0; j < i; j += 2)
                {
                    if (EqualKey(init[i], init[j]))
                    {
                        duplicateKey = true;
                        break;
                    }
                }
                if (!duplicateKey)
                    n += 2;
            }
            if (n < init.Length)
            {
                // Create a new shorter array with unique keys, and
                // the last value associated with each key.  To behave
                // like assoc, the first occurrence of each key must
                // be used, since its metadata may be different than
                // later equal keys.
                Object[] nodups = new Object[n];
                int m = 0;
                for (int i = 0; i < init.Length; i += 2)
                {
                    bool duplicateKey = false;
                    for (int j = 0; j < m; j += 2)
                    {
                        if (EqualKey(init[i], nodups[j]))
                        {
                            duplicateKey = true;
                            break;
                        }
                    }
                    if (!duplicateKey)
                    {
                        int j;
                        for (j = init.Length - 2; j >= i; j -= 2)
                        {
                            if (EqualKey(init[i], init[j]))
                            {
                                break;
                            }
                        }
                        nodups[m] = init[i];
                        nodups[m + 1] = init[j + 1];
                        m += 2;
                    }
                }
                if (m != n)
                    throw new ArgumentException("Internal error: m=" + m);
                init = nodups;
            }
            return new PersistentArrayMap(init);
        }

        /// <summary>
        /// Create an empty <see cref="PersistentArrayMap">PersistentArrayMap</see>.
        /// </summary>
        protected PersistentArrayMap()
        {
            _meta = null;
            _array = new object[] { };
        }

        /// <summary>
        /// Initializes a <see cref="PersistentArrayMap">PersistentArrayMap</see> to use the supplied key/value array.
        /// </summary>
        /// <param name="init">An array with alternating keys and values.</param>
        /// <remarks>The array is used directly.  Do not modify externally or immutability is sacrificed.</remarks>
        public  PersistentArrayMap(object[] init)
        {
            _meta = null;
            
            // The Java version doesn't seem to care.  Why should I?
            //if (init.Length % 2 != 0)
            //    throw new ArgumentException("Key/value array must have an even number of elements.");
            _array = init;

        }

        /// <summary>
        /// Initializes a <see cref="PersistentArrayMap">PersistentArrayMap</see> to use the supplied key/value array and metadata.
        /// </summary>
        /// <param name="meta">The metadata to attach.</param>
        /// <param name="init">An array with alternating keys and values.</param>
        /// <remarks>The array is used directly.  Do not modify externally or immutability is sacrificed.</remarks>
        protected PersistentArrayMap(IPersistentMap meta, object[] init)
        {
            _meta = meta;

            // The Java version doesn't seem to care.  Why should I?
            //if (init.Length % 2 != 0)
            //    throw new ArgumentException("Key/value array must have an even number of elements.");

            _array = init;
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
            // Java version as follows
            //return new PersistentArrayMap(meta, _array);
            // But the usual pattern is this:

            return meta == _meta 
                ? this
                : new PersistentArrayMap(meta, _array);
        }


        #endregion

        #region IMeta Members

        public IPersistentMap meta()
        {
            return _meta;
        }

        #endregion

        #region Associative members

        /// <summary>
        /// Gets the index of the key in the array.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>The index of the key if found; -1 otherwise.</returns>
        private int IndexOfObject(object key)
        {
            Util.EquivPred ep = Util.GetEquivPred(key);
            for (int i = 0; i < _array.Length; i += 2)
                if (ep(key, _array[i]))
                    return i;
            return -1;
        }

        /// <summary>
        /// Gets the index of the key in the array.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>The index of the key if found; -1 otherwise.</returns>
        private int IndexOfKey(object key)
        {
            if (key is Keyword)
            {
                for (int i = 0; i < _array.Length; i += 2)
                    if (key == _array[i])
                        return i;
                return -1;
            }

            else
                return IndexOfObject(key);
        }

        /// <summary>
        /// Compare two keys for equality.
        /// </summary>
        /// <param name="k1">The first key to compare.</param>
        /// <param name="k2">The second key to compare.</param>
        /// <returns></returns>
        /// <remarks>Handles nulls properly.</remarks>
        static bool EqualKey(object k1, object k2)
        {
            if (k1 is Keyword)
                return k1 == k2;
            return Util.equiv(k1, k2);
        }

        /// <summary>
        /// Test if the map contains a key.
        /// </summary>
        /// <param name="key">The key to test for membership</param>
        /// <returns>True if the key is in this map.</returns>
        public override bool containsKey(object key)
        {
            return IndexOfKey(key) >= 0;
        }

        /// <summary>
        /// Returns the key/value pair for this key.
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <returns>The key/value pair for the key, or null if the key is not in the map.</returns>
        public override IMapEntry entryAt(object key)
        {
            int i = IndexOfKey(key);
            return i >= 0
                ? (IMapEntry)Tuple.create(_array[i], _array[i + 1])
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
            int i = IndexOfKey(key);
            return i >= 0
                ? _array[i + 1]
                : notFound;
        }

        #endregion

        #region IPersistentCollection members

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <returns>The number of items in the collection.</returns>
        public override int count()
        {
            return _array.Length / 2;
        }

        /// <summary>
        /// Gets an ISeq to allow first/rest iteration through the collection.
        /// </summary>
        /// <returns>An ISeq for iteration.</returns>
        public override ISeq seq()
        {
            return _array.Length > 0
                ? new Seq(_array, 0)
                : null;
        }

        /// <summary>
        /// Gets an empty collection of the same type.
        /// </summary>
        /// <returns>An emtpy collection.</returns>
        public override IPersistentCollection empty()
        {
            return (IPersistentCollection)EMPTY.withMeta(meta());
        }

        #endregion

        #region IPersistentMap members

        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The value</param>
        /// <returns>A new map with key+value added.</returns>
        /// <remarks>Overwrites an exising value for the <paramref name="key"/>, if present.</remarks>
        public override IPersistentMap assoc(object key, object val)
        {
            int i = IndexOfKey(key);
            object[] newArray;
            if (i >= 0)
            {
                // already have key, same sized replacement
                if (_array[i + 1] == val) // no change, no-op
                    return this;
                newArray = (object[]) _array.Clone();
                newArray[i + 1] = val;
            }
            else
            {
                // new key, grow
                if (_array.Length > HashtableThreshold)
                    return createHT(_array).assoc(key, val);
                newArray = new object[_array.Length + 2];
                if (_array.Length > 0)
                    Array.Copy(_array, 0, newArray, 0, _array.Length);
                newArray[newArray.Length-2] = key;
                newArray[newArray.Length - 1] = val;
            }
            return create(newArray);
        }

        /// <summary>
        /// Create an <see cref="IPersistentMap">IPersistentMap</see> to hold the data when 
        /// an operation causes the threshhold size to be exceeded.
        /// </summary>
        /// <param name="init">The array of key/value pairs.</param>
        /// <returns>A new <see cref="IPersistentMap">IPersistentMap</see>.</returns>
        private IPersistentMap createHT(object[] init)
        {
            return PersistentHashMap.create(meta(), init);
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
            int i = IndexOfKey(key);
            if (i >= 0)
                throw new InvalidOperationException("Key already present.");
            return assoc(key, val);
        }

        /// <summary>
        /// Remove a key entry.
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <returns>A new map with the key removed (or the same map if the key is not contained).</returns>
        public override IPersistentMap without(object key)
        {
            int i = IndexOfKey(key);
            if (i >= 0)
            {
                // key exists, remove
                int newlen = _array.Length - 2;
                if (newlen == 0)
                    return (IPersistentMap)empty();
                object[] newArray = new object[newlen];
                Array.Copy(_array, 0, newArray, 0, i);
                Array.Copy(_array,i+2,newArray,i,newlen-i);
                return create(newArray);
            }
            else
                return this;             
        }

        #endregion

       
        /// <summary>
        /// Internal class providing an <see cref="ISeq">ISeq</see> 
        /// for <see cref="PersistentArrayMap">PersistentArrayMap</see>s.
        /// </summary>
        [Serializable]
        protected sealed class Seq : ASeq, Counted
        {
            #region Data

            /// <summary>
            /// The array to iterate over.
            /// </summary>
            private readonly object[] _array;

            /// <summary>
            /// Current index position in the array.
            /// </summary>
            private readonly int _i;

            #endregion

            #region C-tors & factory methods

            /// <summary>
            /// Initialize the sequence to a given array and index.
            /// </summary>
            /// <param name="array">The array being sequenced over.</param>
            /// <param name="i">The current index.</param>
            public Seq(object[] array, int i)
            {
                _array = array;
                _i = i;
            }

            /// <summary>
            /// Initialize the sequence with given metatdata and array/index.
            /// </summary>
            /// <param name="meta">The metadata to attach.</param>
            /// <param name="array">The array being sequenced over.</param>
            /// <param name="i">The current index.</param>
            public Seq(IPersistentMap meta, object[] array, int i)
                : base(meta)
            {
                _array = array;
                _i = i;
            }

            #endregion

            #region ISeq members

            /// <summary>
            /// Gets the first item.
            /// </summary>
            /// <returns>The first item.</returns>
            public override object first()
            {
                return Tuple.create(_array[_i], _array[_i + 1]);
            }

            /// <summary>
            /// Return a seq of the items after the first.  Calls <c>seq</c> on its argument.  If there are no more items, returns nil."
            /// </summary>
            /// <returns>A seq of the items after the first, or <c>nil</c> if there are no more items.</returns>
            public override ISeq next()
            {
                return _i + 2 < _array.Length
                    ? new Seq(_array, _i + 2)
                    : null;
            }

            #endregion

            #region IPersistentCollection members
            /// <summary>
            /// Gets the number of items in the collection.
            /// </summary>
            /// <returns>The number of items in the collection.</returns>
            public override int count()
            {
                return (_array.Length - _i) / 2;
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
                return new Seq(meta, _array, _i);
            }

            #endregion

        }

        #region IEditableCollection Members

        public ITransientCollection asTransient()
        {
            return new TransientArrayMap(_array);
        }

        #endregion

        #region TransientArrayMap class

        sealed class TransientArrayMap : ATransientMap
        {
            #region Data

            volatile int _len;
            readonly object[] _array;
            
            [NonSerialized] volatile Thread _owner;

            #endregion

            #region Ctors


            public TransientArrayMap(object[] array)
            {
                _owner = Thread.CurrentThread;
                _array = new object[Math.Max(HashtableThreshold, array.Length)];
                Array.Copy(array, _array, array.Length);
                _len = array.Length;
            }

            #endregion

            #region

            /// <summary>
            /// Gets the index of the key in the array.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <returns>The index of the key if found; -1 otherwise.</returns>
            private int IndexOfKey(object key)
            {
                for (int i = 0; i < _len; i += 2)
                    if (EqualKey(_array[i], key))
                        return i;
                return -1;
            }

            protected override void EnsureEditable()
            {
                if (_owner == null )
                    throw new InvalidOperationException("Transient used after persistent! call");
            }

            protected override ITransientMap doAssoc(object key, object val)
            {
                int i = IndexOfKey(key);
                if (i >= 0) //already have key,
                {
                    if (_array[i + 1] != val) //no change, no op
                        _array[i + 1] = val;
                }
                else //didn't have key, grow
                {
                    if (_len >= _array.Length)
                        return ((ITransientMap)PersistentHashMap.create(_array).asTransient()).assoc(key, val);
                    _array[_len++] = key;
                    _array[_len++] = val;
                }
                return this;
            }


            protected override ITransientMap doWithout(object key)
            {
                int i = IndexOfKey(key);
                if (i >= 0) //have key, will remove
                {
                    if (_len >= 2)
                    {
                        _array[i] = _array[_len - 2];
                        _array[i + 1] = _array[_len - 1];
                    }
                    _len -= 2;
                }
                return this;
            }

            protected override object doValAt(object key, object notFound)
            {
                int i = IndexOfKey(key);
                if (i >= 0)
                    return _array[i + 1];
                return notFound;
            }

            protected override int doCount()
            {
                return _len / 2;
            }

            protected override IPersistentMap doPersistent()
            {
                EnsureEditable();
                _owner = null;
                object[] a = new object[_len];
                Array.Copy(_array, a, _len);
                return new PersistentArrayMap(a);
            }

            #endregion
        }

        #endregion

        #region kvreduce

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "kvreduce")]
        public object kvreduce(IFn f, object init)
        {
            for (int i = 0; i < _array.Length; i += 2)
            {
                init = f.invoke(init, _array[i], _array[i + 1]);
                if (RT.isReduced(init))
                    return ((IDeref)init).deref();
            }
            return init;
        }

        #endregion

        #region IMapEnumerable, IMapEnumerableTyped, IEnumerable ...

        public IEnumerator keyEnumerator()
        {
            return tkeyEnumerator();
        }

        public IEnumerator valEnumerator()
        {
            return tvalEnumerator();
        }

        public IEnumerator<object> tkeyEnumerator()
        {
            for (int i = 0; i < _array.Length; i += 2)
                yield return _array[i];
        }

        public IEnumerator<object> tvalEnumerator()
        {
            for (int i = 0; i < _array.Length; i += 2)
                yield return _array[i + 1];
        }


        public override IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            for (int i = 0; i < _array.Length; i += 2)
                yield return new KeyValuePair<object, object>(_array[i], _array[i + 1]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IMapEntry>)this).GetEnumerator();
        }

        IEnumerator<IMapEntry> IEnumerable<IMapEntry>.GetEnumerator()
        {
            for (int i = 0; i < _array.Length; i += 2)
                yield return (IMapEntry) Tuple.create(_array[i], _array[i + 1]);
        }

        #endregion

    }
}
