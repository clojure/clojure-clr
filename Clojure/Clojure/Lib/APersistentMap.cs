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
using System.Reflection;

namespace clojure.lang
{
    /// <summary>
    /// Provides a basic impelmentation of <see cref="IPersistentMap">IPersistentMap</see> functionality.
    /// </summary>
    [Serializable]
    public abstract class APersistentMap: AFn, IPersistentMap, IDictionary, IEnumerable<IMapEntry>, MapEquivalence, IDictionary<Object,Object>, IHashEq
    {
        #region  Data
        
        /// <summary>
        /// Caches the hash code, when computed.
        /// </summary>
        /// <remarks>The value <value>-1</value> indicates that the hash code has not been computed yet.</remarks>
        int _hash = -1;

        /// <summary>
        /// Caches the hashseq code, when computed.
        /// </summary>
        /// <remarks>The value <value>-1</value> indicates that the hasheq code has not been computed yet.</remarks>
        int _hasheq = -1;

        #endregion

        #region object overrides

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return RT.printString(this);
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the current Object.
        /// </summary>
        /// <param name="obj">The Object to compare with the current Object. </param>
        /// <returns>true if the specified Object is equal to the current Object; 
        /// otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return mapEquals(this, obj);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "map")]
        public static bool mapEquals(IPersistentMap m1, Object obj)
        {
            if (m1 == obj)
                return true;

            //if(!(obj instanceof Map))
            //    return false;
            //Map m = (Map) obj;

            IDictionary d = obj as IDictionary;
            if (d == null)
                return false;

            // Java had the following.
            // This works on other APersistentMap implementations, but not on
            //  arbitrary dictionaries.
            //if (d.Count != m1.Count || d.GetHashCode() != m1.GetHashCode())
            //    return false;

            if (d.Count != m1.count())
                return false;

            for (ISeq s = m1.seq(); s != null; s = s.next())
            {
                IMapEntry me = (IMapEntry)s.first();
                bool found = d.Contains(me.key());
                if (!found || !Util.equals(me.val(), d[me.key()]))
                    return false;
            }
            return true;
        }

 
        /// <summary>
        /// Gets a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>Valud-based = relies on all entries.  Once computed, it is cached.</remarks>
        public override int GetHashCode()
        {
            if (_hash == -1 )
                _hash = mapHash(this);
            return _hash;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "map")]
        public static int mapHash(IPersistentMap m)
        {
            int hash = 0;
            for (ISeq s = m.seq(); s != null; s = s.next())
            {
                IMapEntry me = (IMapEntry)s.first();
                hash += (me.key() == null ? 0 : me.key().GetHashCode())
                    ^ (me.val() == null ? 0 : me.val().GetHashCode());
            }
            return hash;
        }


        #endregion

        #region Associative methods

        abstract public bool containsKey(object key);
        abstract public IMapEntry entryAt(object key);
        Associative Associative.assoc(object key, object val)
        {
            return assoc(key, val);
        }
        abstract public object valAt(object key);
        abstract public object valAt(object key, object notFound);

        #endregion

        #region Seqable members

        abstract public ISeq seq();

        #endregion

        #region IPersistentCollection Members

        /// <summary>
        /// Returns a new collection that has the given element cons'd on front of the eixsting collection.
        /// </summary>
        /// <param name="o">An item to put at the front of the collection.</param>
        /// <returns>A new immutable collection with the item added.</returns>
        IPersistentCollection IPersistentCollection.cons(object o)
        {
            return cons(o);
        }

        abstract public int count();
        abstract public IPersistentCollection empty();

        /// <summary>
        /// Determine if an object is equivalent to this (handles all collections).
        /// </summary>
        /// <param name="o">The object to compare.</param>
        /// <returns><c>true</c> if the object is equivalent; <c>false</c> otherwise.</returns>
        /// <remarks>
        /// In Java Rev 1215, Added equiv.  Same as the definition in Equals, as in they took out the hashcode comparison.
        /// Different, as in Util.Equal above became Util.equals. and below it is Util.equiv.
        /// </remarks> 
        public bool equiv(object o)
        {
            //if(!(obj instanceof Map))
            //    return false;
            //Map m = (Map) obj;

            if (o is IPersistentMap && !(o is MapEquivalence))
                return false;

            IDictionary d = o as IDictionary;
            if (d == null)
                return false;

            // Java had the following.
            // This works on other APersistentMap implementations, but not on
            //  arbitrary dictionaries.
            //if (d.Count != this.Count || d.GetHashCode() != this.GetHashCode())
            //    return false;

            if (d.Count != this.Count)
                return false;

            for (ISeq s = seq(); s != null; s = s.next())
            {
                IMapEntry me = (IMapEntry)s.first();
                bool found = d.Contains(me.key());
                if (!found || !Util.equiv(me.val(), d[me.key()]))
                    return false;
            }
            return true;
        }



        #endregion

        #region IObj members

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "with")]
        abstract public IObj withMeta(IPersistentMap meta);

        #endregion

        #region IPersistentMap members

        abstract public IPersistentMap assoc(object key, object val);
        abstract public IPersistentMap assocEx(object key, object val);
        abstract public IPersistentMap without(object key);

        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="o">The key/value pair to add.</param>
        /// <returns>A new map with key+value pair added.</returns>
        public IPersistentMap cons(object o)
        {
            IMapEntry e = o as IMapEntry;
            if (e != null)
                return assoc(e.key(), e.val());

            if (o is DictionaryEntry)
            {
                DictionaryEntry de = (DictionaryEntry)o;
                return assoc(de.Key, de.Value);
            }

            if (o != null)
            {
                Type t = o.GetType();
                if (t.IsGenericType && t.Name == "KeyValuePair`2")
                {
                    object key = t.InvokeMember("Key", BindingFlags.GetProperty, null, o, null);
                    object val = t.InvokeMember("Value", BindingFlags.GetProperty, null, o, null);
                    return assoc(key, val);
                }
            }

            IPersistentVector v = o as IPersistentVector;
            if (v != null)
            {
                if (v.count() != 2)
                    throw new ArgumentException("Vector arg to map cons must be a pair");
                return assoc(v.nth(0), v.nth(1));
            }

            IPersistentMap ret = this;
            for (ISeq s = RT.seq(o); s != null; s = s.next())
            {
                IMapEntry me = (IMapEntry)s.first();
                ret = ret.assoc(me.key(), me.val());
            }
            return ret;
        }

        #endregion

        #region IFn members

        public override object invoke(object arg1)
        {
            return valAt(arg1);
        }

        public override object invoke(object arg1, object arg2)
        {
            return valAt(arg1, arg2);
        }

        #endregion

        #region IDictionary<Object, Object>, IDictionary Members

        public void Add(KeyValuePair<object, object> item)
        {
            throw new InvalidOperationException("Cannot modify an immutable map");
        }

        public void Add(object key, object value)
        {
            throw new InvalidOperationException("Cannot modify an immutable map");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Cannot modify an immutable map");
        }

        public bool Contains(KeyValuePair<object, object> item)
        {
            object value;
            if (!TryGetValue(item.Key, out value))
                return false;

            if (value == null)
                return item.Value == null;

            return value.Equals(item.Value);
        }

        public bool ContainsKey(object key)
        {
            return containsKey(key);
        }

        public bool Contains(object key)
        {
            return this.containsKey(key);
        }


        public virtual IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            for (ISeq s = seq(); s != null; s = s.next())
            {
                IMapEntry entry = (IMapEntry)s.first();
                yield return new KeyValuePair<object, object>(entry.key(), entry.val());
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IMapEntry>)this).GetEnumerator();
        }

        IEnumerator<IMapEntry> IEnumerable<IMapEntry>.GetEnumerator()
        {
            for (ISeq s = seq(); s != null; s = s.next())
                yield return (IMapEntry)s.first();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new MapEnumerator(this);
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }


        public ICollection<object> Keys
        {
            get { return KeySeq.create(seq()); }
        }

        ICollection IDictionary.Keys
        {
            get { return KeySeq.create(seq()); }
        }


        public bool Remove(KeyValuePair<object, object> item)
        {
            throw new InvalidOperationException("Cannot modify an immutable map");
        }

        public bool Remove(object key)
        {
            throw new InvalidOperationException("Cannot modify an immutable map");
        }

        void IDictionary.Remove(object key)
        {
            throw new InvalidOperationException("Cannot modify an immutable map");
        }

        public ICollection<object> Values
        {
            get { return ValSeq.create(seq()); }
        }

        ICollection IDictionary.Values
        {
            get { return ValSeq.create(seq()); }
        }

        public object this[object key]
        {
            get
            {
                return valAt(key);
            }
            set
            {
                throw new InvalidOperationException("Cannot modify an immutable map");
            }
        }

        static readonly object _missingValue = new object();

        public bool TryGetValue(object key, out object value)
        {
            object found = valAt(key, _missingValue);
            if ( found == _missingValue)
            {
                value = null;
                return false;
            }

            value = found;
            return true;
        }

        #endregion

        #region ICollection Members

        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
        }

        public void CopyTo(Array array, int index)
        {
            ISeq s = seq();
            if (s != null)
                ((ICollection)s).CopyTo(array, index);
        }

        public int Count
        {
            get { return count(); }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        #endregion

        #region IHashEq

        public int hasheq()
        {
            if (_hasheq == -1)
            {
                //_hasheq = mapHasheq(this);
                _hasheq = Murmur3.HashUnordered(this);
            }
            return _hasheq;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "map")]
        public static int mapHasheq(IPersistentMap m)
        {
            int hash = 0;
            for (ISeq s = m.seq(); s != null; s = s.next())
            {
                IMapEntry e = (IMapEntry)s.first();
                hash += Util.hasheq(e.key()) ^ Util.hasheq(e.val());
            }
            return hash;
        }

        #endregion

        #region Key and value sequences

        /// <summary>
        /// Implements a sequence across the keys of map.
        /// </summary>
        [Serializable]
        public sealed class KeySeq : ASeq, IEnumerable
        {
            #region Data

            readonly ISeq _seq;
            readonly IEnumerable _enumerable;

            #endregion

            #region C-tors & factory methods

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
            static public KeySeq create(ISeq seq)
            {
                if (seq == null)
                    return null;
                return new KeySeq(seq, null);
            }


            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
            static public KeySeq createFromMap(IPersistentMap map)
            {
                if (map == null)
                    return null;
                ISeq seq = map.seq();
                if (seq == null)
                    return null;
                return new KeySeq(seq, map);
            }

            private KeySeq(ISeq seq, IEnumerable enumerable)
            {
                _seq = seq;
                _enumerable = enumerable;
            }

            private KeySeq(IPersistentMap meta, ISeq seq, IEnumerable enumerable)
                : base(meta)
            {
                _seq = seq;
                _enumerable = enumerable;
            }

            #endregion

            #region ISeq members

            public override object first()
            {
                object entry = _seq.first();
                IMapEntry me = entry as IMapEntry;

                if (me != null)
                    return me.key();
                else if (entry is DictionaryEntry)
                    return ((DictionaryEntry)entry).Key;
                throw new InvalidCastException("Cannot convert hashtable entry to IMapEntry or DictionaryEntry");
            }

            public override ISeq next()
            {
                return create(_seq.next());
            }

            #endregion

            #region IObj methods

            public override IObj withMeta(IPersistentMap meta)
            {
                return new KeySeq(meta, _seq, _enumerable);
            }

            #endregion

            #region IEnumerable members

            IEnumerator<Object> KeyIteratorT(IEnumerable enumerable)
            {
                foreach (Object item in enumerable)
                    yield return ((IMapEntry)item).key();
            }

            public override IEnumerator<object> GetEnumerator()
            {
                if (_enumerable == null)
                    return base.GetEnumerator();

                IMapEnumerableTyped<Object,Object> imit = _enumerable as IMapEnumerableTyped<Object,Object>;
                if (imit != null)
                    return (IEnumerator<object>)imit.tkeyEnumerator();


                IMapEnumerable imi = _enumerable as IMapEnumerable;
                if (imi != null)
                    return (IEnumerator<object>)imi.keyEnumerator();

                return KeyIteratorT(_enumerable);
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>A <see cref="SeqEnumerator">SeqEnumerator</see> that iterates through the sequence.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

        }

        /// <summary>
        /// Implements a sequence across the values of a map.
        /// </summary>
        [Serializable]
        public sealed class ValSeq : ASeq, IEnumerable
        {
            #region Data

            readonly ISeq _seq;
            readonly IEnumerable _enumerable;

            #endregion

            #region C-tors & factory methods

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
            static public ValSeq create(ISeq seq)
            {
                if (seq == null)
                    return null;
                return new ValSeq(seq, null);
            }


            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create")]
            static public ValSeq createFromMap(IPersistentMap map)
            {
                if (map == null)
                    return null;
                ISeq seq = map.seq();
                if (seq == null)
                    return null;
                return new ValSeq(seq, map);
            }

            private ValSeq(ISeq seq, IEnumerable enumerable)
            {
                _seq = seq;
                _enumerable = enumerable;
            }

            private ValSeq(IPersistentMap meta, ISeq seq, IEnumerable enumerable)
                : base(meta)
            {
                _seq = seq;
                _enumerable = enumerable;
            }

            #endregion

            #region ISeq members

            public override object first()
            {
                object entry = _seq.first();

                {
                    IMapEntry me = entry as IMapEntry;
                    if (me != null)
                        return me.val();
                }

                if (entry is DictionaryEntry)
                    return ((DictionaryEntry)entry).Value;

                throw new InvalidCastException("Cannot convert hashtable entry to IMapEntry or DictionaryEntry");
            }

            public override ISeq next()
            {
                return create(_seq.next());
            }

            #endregion

            #region IObj methods

            public override IObj withMeta(IPersistentMap meta)
            {
                return new ValSeq(meta, _seq, _enumerable);
            }

            #endregion

            #region IEnumerable members

            IEnumerator<Object> KeyIteratorT(IEnumerable enumerable)
            {
                foreach (Object item in enumerable)
                    yield return ((IMapEntry)item).val();
            }

            public override IEnumerator<object> GetEnumerator()
            {
                if (_enumerable == null)
                    return base.GetEnumerator();

                IMapEnumerableTyped<Object, Object> imit = _enumerable as IMapEnumerableTyped<Object, Object>;
                if (imit != null)
                    return (IEnumerator<object>)imit.tvalEnumerator();


                IMapEnumerable imi = _enumerable as IMapEnumerable;
                if (imi != null)
                    return (IEnumerator<object>)imi.valEnumerator();

                return KeyIteratorT(_enumerable);
            }

            #endregion

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>A <see cref="SeqEnumerator">SeqEnumerator</see> that iterates through the sequence.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion
    }
}
