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
    /// Provides a basic implementation of <see cref="IPersistentVector">IPersistentVector</see> functionality.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1708:IdentifiersShouldDifferByMoreThanCase")]
    [Serializable]
    public abstract class APersistentVector: AFn, IPersistentVector, IList, IMapEntry, IComparable, IList<Object>, IComparable<Object>, IHashEq
    {
        #region Data

        /// <summary>
        ///  Caches the hash code, once computed.
        /// </summary>
        int _hash = -1;

        /// <summary>
        /// Caches the hashseq code, when computed.
        /// </summary>
        /// <remarks>The value <value>-1</value> indicates that the hasheq code has not been computed yet.</remarks>
        int _hasheq = -1;

        #endregion

        #region Object overrides

        /// <summary>
        /// Returns a string representing the object.
        /// </summary>
        /// <returns>A string representing the object.</returns>
        public override string ToString()
        {
            return RT.printString(this);
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the current Object.
        /// </summary>
        /// <param name="obj">The Object to compare with the current Object.</param>
        /// <returns><value>true</value> if the specified Object is equal to the current Object; 
        /// otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            return doEquals(this, obj);
        }

        /// <summary>
        /// Compares an <see cref="IPersistentVector">IPersistentVector</see> to another object for equality.
        /// </summary>
        /// <param name="v">The <see cref="IPersistentVector">IPersistentVector</see> to compare.</param>
        /// <param name="obj">The other object to compare.</param>
        /// <returns><value>true</value> if the specified Object is equal to the current Object; 
        /// otherwise, <value>false</value>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        static public bool doEquals(IPersistentVector v, object obj)
        {
            if (v == obj)
                return true;

            IPersistentVector ipv = obj as IPersistentVector;

            if (ipv != null)
            {
                if (ipv.count() != v.count())
                    return false;

                for (int i = 0; i < v.count(); i++)
                {
                    if (!Util.equals(v.nth(i), ipv.nth(i)))
                        return false;
                }
                return true;
            }

            IList ilist = obj as IList;

            if (ilist != null)
            {
                if (ilist.Count != v.count())   // THis test in the JVM code can't be right:  || ma.GetHashCode() != v.GetHashCode())
                    return false;

                for (int i = 0; i < v.count(); i++)
                {
                    if (!Util.equals(v.nth(i), ilist[i]))
                        return false;
                }
                return true;
            }

            if (!(obj is Sequential))
                return false;

            ISeq ms = RT.seq(obj);

            for (int i = 0; i < v.count(); i++, ms = ms.next())
            {
                if (ms == null || !Util.equals(v.nth(i), ms.first()))
                    return false;
            }
            if (ms != null)
                return false;

            return true;
        }


        /// <summary>
        /// Compute a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            if (_hash == -1)
            {
                int hash = 1;
                for (int i = 0; i < count(); i++)
                {
                    Object obj = nth(i);
                    hash = 31 * hash + (obj == null ? 0 : obj.GetHashCode());
                }
                this._hash = hash;
            }
            return _hash;
        } 


        #endregion

        #region IFn members

        public override object invoke(object arg1)
        {
            return nth(Util.ConvertToInt(arg1));
        }

        #endregion

        #region IPersistentCollection Members

        public abstract int count();
        public abstract IPersistentCollection empty();

        /// <summary>
        /// Gets an ISeq to allow first/rest iteration through the collection.
        /// </summary>
        /// <returns>An ISeq for iteration.</returns>
        public virtual ISeq seq()
        {
            return count() > 0
                ? new Seq(this, 0)
                : null;
        }

        /// <summary>
        /// Returns a new collection that has the given element cons'd on front of the eixsting collection.
        /// </summary>
        /// <param name="o">An item to put at the front of the collection.</param>
        /// <returns>A new immutable collection with the item added.</returns>
        IPersistentCollection IPersistentCollection.cons(object o)
        {
            return cons(o);
        }


        /// <summary>
        /// Determine if an object is equivalent to this (handles all collections).
        /// </summary>
        /// <param name="o">The object to compare.</param>
        /// <returns><c>true</c> if the object is equivalent; <c>false</c> otherwise.</returns>
        public virtual bool equiv(object o)
        {
            if (this == o)
                return true;
            return doEquiv(this, o);
        }



        static bool doEquiv(IPersistentVector v, object obj)
        {
            IPersistentVector ipv = obj as IPersistentVector;

            if (ipv != null)
            {
                if (ipv.count() != v.count())
                    return false;

                for (int i = 0; i < v.count(); i++)
                {
                    if (!Util.equiv(v.nth(i), ipv.nth(i)))
                        return false;
                }
                return true;
            }

            IList ilist = obj as IList;

            if (ilist != null)
            {
                if (ilist.Count != v.count())   // THis test in the JVM code can't be right:  || ma.GetHashCode() != v.GetHashCode())
                    return false;

                for (int i = 0; i < v.count(); i++)
                {
                    if (!Util.equiv(v.nth(i), ilist[i]))
                        return false;
                }
                return true;
            }

            if (!(obj is Sequential))
                return false;

            ISeq ms = RT.seq(obj);

            for (int i = 0; i < v.count(); i++, ms = ms.next())
            {
                if (ms == null || !Util.equiv(v.nth(i), ms.first()))
                    return false;
            }
            if (ms != null)
                return false;

            return true;

        }


        #endregion

        #region Reversible members

        /// <summary>
        /// Gets an <see cref="ISeq">ISeq</see> to travers the sequence in reverse.
        /// </summary>
        /// <returns>An <see cref="ISeq">ISeq</see> .</returns>
        public ISeq rseq()
        {
            return count() > 0
                ? new RSeq(this, count() - 1)
                : null;
        }

        #endregion

        #region IPersistentVector Members

        /// <summary>
        /// Gets the number of items in the vector.
        /// </summary>
        /// <returns>The number of items.</returns>
        public virtual int length()
        {
            return count();
        }

        abstract public object nth(int i);
        abstract public IPersistentVector assocN(int i, object val);
        abstract public IPersistentVector cons(object o);

        public virtual object nth(int i, object notFound)
        {
            if (i >= 0 && i < count())
                return nth(i);
            return notFound;
        }

        #endregion

        #region Associative Members

        /// <summary>
        /// Test if the map contains a key.
        /// </summary>
        /// <param name="key">The key to test for membership</param>
        /// <returns>True if the key is in this map.</returns>
        public virtual bool containsKey(object key)
        {
            if (!Util.IsNumeric(key))
                return false;
            int i = Util.ConvertToInt(key);
            return i >= 0 && i < count();
        }

        /// <summary>
        /// Returns the key/value pair for this key.
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <returns>The key/value pair for the key, or null if the key is not in the map.</returns>
        public virtual IMapEntry entryAt(object key)
        {
            if (Util.IsNumeric(key))
            {
                int i = Util.ConvertToInt(key);
                if (i >= 0 && i < count())
                    return (IMapEntry) Tuple.create(key, nth(i));
            }
            return null;
        }

        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The value</param>
        /// <returns>A new map with the key/value added.</returns>
        public virtual Associative assoc(object key, object val)
        {
            if (Util.IsNumeric(key))
            {
                int i = Util.ConvertToInt(key);
                return assocN(i, val);
            }
            throw new ArgumentException("Key must be an integer");
        }

        /// <summary>
        /// Gets the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The associated value. (Throws an exception if key is not present.)</returns>
        public virtual object valAt(object key)
        {
            return valAt(key, null);
        }

        /// <summary>
        /// Gets the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="notFound">The value to return if the key is not present.</param>
        /// <returns>The associated value (or <c>notFound</c> if the key is not present.</returns>
        public virtual object valAt(object key, object notFound)
        {
            if (Util.IsNumeric(key))
            {
                int i = Util.ConvertToInt(key);
                if (i >= 0 && i < count())
                    return nth(i);
            }
            return notFound;
        }

        #endregion

        #region IPersistentStack Members

        /// <summary>
        /// Peek at the top (first) element in the stack.
        /// </summary>
        /// <returns>The top (first) element.</returns>
        public virtual object peek()
        {
            return (count() > 0)
                ? nth(count() - 1)
                : null;
        }

        public abstract IPersistentStack pop();


        #endregion 

        #region IList<Object>, IList members

        public void Add(object item)
        {
            throw new InvalidOperationException("Cannot modify an immutable vector");
        }


        int IList.Add(object value)
        {
            throw new InvalidOperationException("Cannot modify an immutable vector");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Cannot modify an immutable vector");
        }

        public bool Contains(object item)
        {
            for (ISeq s = seq(); s != null; s = s.next())
                if (Util.equals(s.first(), item))
                    return true;
            return false;
        }

        public int IndexOf(object item)
        {
            for (int i = 0; i < count(); i++)
                if (Util.equals(nth(i), item))
                    return i;
            return -1;
        }

        public void Insert(int index, object item)
        {
            throw new InvalidOperationException("Cannot modify an immutable vector");
        }

        public object this[int index]
        {
            get
            {
                return nth(index);
            }
            set
            {
                throw new InvalidOperationException("Cannot modify an immutable vector");
            }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool Remove(object item)
        {
            throw new InvalidOperationException("Cannot modify an immutable vector");
        }

        void IList.Remove(object value)
        {
            throw new InvalidOperationException("Cannot modify an immutable vector");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Cannot modify an immutable vector");
        }

        #endregion

        #region ICollection Members

        public void CopyTo(object[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex","must be non-negative");


            int cnt = count();

            if (cnt == 0)
                return;

            if (arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex", "is past the end");

            if (arrayIndex >= array.Length )
                throw new InvalidOperationException("Not enough room in array");

            for (int i = 0; i < cnt; i++)
                array[i + arrayIndex] = nth(i);
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index","Must be non-negative");

            if (array.Rank > 1)
                throw new ArgumentException("must be 1-dimensional","array" );

            int cnt = count();

            if (cnt == 0)
                return;

            if (index >= array.Length)
                throw new ArgumentOutOfRangeException("index","must be in array");

            if ( array.Length - index < cnt)
                throw new InvalidOperationException("Not enough room in array");

            for (int i = 0; i < cnt; i++)
                array.SetValue(nth(i), i + index);
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

        #region IEnumerable<Object>, IEnumerable Members

        public virtual IEnumerator<object> GetEnumerator()
        {
            for (ISeq s = seq(); s != null; s = s.next())
                yield return s.first();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (ISeq s = seq(); s != null; s = s.next())
                yield return s.first();
        }

        #endregion

        #region IMapEntry methods

        public virtual object key()
        {
            if (count() == 2)
                return nth(0);
            throw new InvalidOperationException();
        }

        public virtual object val()
        {
            if (count() == 2)
                return nth(1);
            throw new InvalidOperationException();
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object other)
        {
            IPersistentVector v =  (IPersistentVector)other;

            if ( v == null )
                return 1;

            if (count() < v.count())
                return -1;
            else if (count() > v.count())
                return 1;
            for (int i = 0; i < count(); i++)
            {
                int c = Util.compare(nth(i), v.nth(i));
                if (c != 0)
                    return c;
            }
            return 0;
        }
        

        int IComparable.CompareTo(object obj)
        {
            return CompareTo(obj);
        }

        #endregion

        #region IHashEq

        public int hasheq()
        {
            if (_hasheq == -1)
            {
                int n;
                int hash = 1;

                for (n = 0; n < count(); ++n)
                {
                    hash = 31 * hash + Util.hasheq(nth(n));
                }

                _hasheq = Murmur3.MixCollHash(hash, n);
            }
            return _hasheq;
        }

        #endregion

        #region Ranged iterator

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End")]
        public virtual IEnumerator RangedIterator(int start, int end)
        {
            for (int i = start; i < end; i++)
                yield return nth(i);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "End")]
        public virtual IEnumerator<object> RangedIteratorT(int start, int end)
        {
            for (int i = start; i < end; i++)
                yield return nth(i);
        }

        #endregion

        #region Helpers

        public Object[] ToArray()
        {
            Object[] ret = new Object[count()];
            for (int i = 0; i < count(); i++)
                ret[i] = nth(i);
            return ret;
        }

        #endregion

        /// <summary>
        /// Internal class providing <see cref="ISeq">ISeq</see> functionality for <see cref="APersistentVector">APersistentVector</see>.
        /// </summary>
        /// <remarks>This class should be private.  Public only for DLR debugging output.</remarks>
        [Serializable]
        public sealed class Seq : ASeq, IndexedSeq, IReduce, Counted  // Counted left out of Java version
        {
            // TODO: something more efficient  (todo = from Java)

            #region Data

            /// <summary>
            /// The <see cref="IPersistentVector">IPersistentVector</see> this sequence is iterating over.
            /// </summary>
            readonly IPersistentVector _v;

            /// <summary>
            /// The current index into the vector.
            /// </summary>
            readonly int _i;

            #endregion

            #region C-tors and factory methods

            /// <summary>
            /// Initialize a sequence over a vector with the first element at a given index.
            /// </summary>
            /// <param name="v">The vector to sequence over.</param>
            /// <param name="i">The index to start at.</param>
            public Seq(IPersistentVector v, int i)
            {
                this._v = v;
                this._i = i;
            }

            /// <summary>
            /// Initialize a sequence over a vector with the first element at a given index, with the given metadata.
            /// </summary>
            /// <param name="meta">The metadata to attach.</param>
            /// <param name="v">The vector to sequence over.</param>
            /// <param name="i">The index to start at.</param>
            Seq(IPersistentMap meta, IPersistentVector v, int i)
                : base(meta)
            {
                this._v = v;
                this._i = i;
            }

            #endregion

            #region ISeq members

            /// <summary>
            /// Gets the first item.
            /// </summary>
            /// <returns>The first item.</returns>
            public override object first()
            {
                return _v.nth(_i);
            }

            /// <summary>
            /// Return a seq of the items after the first.  Calls <c>seq</c> on its argument.  If there are no more items, returns nil."
            /// </summary>
            /// <returns>A seq of the items after the first, or <c>nil</c> if there are no more items.</returns>
            public override ISeq next()
            {
                return _i + 1 < _v.count()
                    ? new Seq(_v, _i+1)
                    : null;
            }
            
            #endregion

            #region IndexSeq members

            /// <summary>
            /// Gets the index associated with this sequence.
            /// </summary>
            /// <returns>The index associated with this sequence.</returns>
            public int index() 
            {
                return _i;
            }

            #endregion

            #region IPersistentCollection members

            /// <summary>
            /// Gets the number of items in the collection.
            /// </summary>
            /// <returns>The number of items in the collection.</returns>
            public override int count()
            {
                return _v.count() - _i;
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
                return new Seq(meta, _v, _i);
            }

            #endregion

            #region IReduce members

            /// <summary>
            /// Reduce the collection using a function.
            /// </summary>
            /// <param name="f">The function to apply.</param>
            /// <returns>The reduced value</returns>
            public object reduce(IFn f)
            {
                object ret = _v.nth(_i);
                for (int x = _i + 1; x < _v.count(); x++)
                {
                    ret = f.invoke(ret, _v.nth(x));
                    if (RT.isReduced(ret))
                        return ((IDeref)ret).deref();
                }
                return ret;
            }

            /// <summary>
            /// Reduce the collection using a function.
            /// </summary>
            /// <param name="f">The function to apply.</param>
            /// <param name="start">An initial value to get started.</param>
            /// <returns>The reduced value</returns>
            public object reduce(IFn f, object start)
            {
                object ret = f.invoke(start, _v.nth(_i));
                for (int x = _i + 1; x < _v.count(); x++) {
                    if (RT.isReduced(ret))
                        return ((IDeref)ret).deref(); 
                    ret = f.invoke(ret, _v.nth(x));
                }
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref(); 
                return ret;
            }

            #endregion

        }  // nested class Seq

        /// <summary>
        /// Internal class providing reverse <see cref="ISeq">ISeq</see> functionality for <see cref="APersistentVector">APersistentVector</see>.
        /// </summary>
        /// <remarks>This class should be private.  Public only for DLR debugging output.</remarks>
        [Serializable]
        public sealed class RSeq : ASeq, IndexedSeq, IReduce, Counted  // IReduce left out of Java version
        {

            #region Data

            /// <summary>
            /// The <see cref="IPersistentVector">IPersistentVector</see> this sequence is iterating over.
            /// </summary>
            readonly IPersistentVector _v;

            /// <summary>
            /// The current index into the vector.
            /// </summary>    
            readonly int _i;

            #endregion

            #region C-tors and factory methods

            /// <summary>
            /// Initialize a reverse sequence over a vector with the first element at a given index.
            /// </summary>
            /// <param name="v">The vector to sequence over.</param>
            /// <param name="i">The index to start at.</param>
            public RSeq(IPersistentVector v, int i)
            {
                this._v = v;
                this._i = i;
            }

            /// <summary>
            /// Initialize a reverse sequence over a vector with the first element at a given index, with the given metadata.
            /// </summary>
            /// <param name="meta">The metadata to attach.</param>
            /// <param name="v">The vector to sequence over.</param>
            /// <param name="i">The index to start at.</param>
            RSeq(IPersistentMap meta, IPersistentVector v, int i)
                : base(meta)
            {
                this._v = v;
                this._i = i;
            }

            #endregion

            #region ISeq members

            /// <summary>
            /// Gets the first item.
            /// </summary>
            /// <returns>The first item.</returns>
            public override object first()
            {
                return _v.nth(_i);
            }

            /// <summary>
            /// Return a seq of the items after the first.  Calls <c>seq</c> on its argument.  If there are no more items, returns nil."
            /// </summary>
            /// <returns>A seq of the items after the first, or <c>nil</c> if there are no more items.</returns>
            public override ISeq next()
            {
                 return _i > 0
                    ? new RSeq(_v, _i-1)
                    : null;
            }
            
            #endregion

            #region IndexSeq members

            /// <summary>
            /// Gets the index associated with this sequence.
            /// </summary>
            /// <returns>The index associated with this sequence.</returns>
            public int index() 
            {
                return _i;
            }

            #endregion

            #region IPersistentCollection members

            /// <summary>
            /// Gets the number of items in the collection.
            /// </summary>
            /// <returns>The number of items in the collection.</returns>
            public override int count()
            {
                return _i+1;
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
                return new RSeq(meta, _v, _i);
            }

            #endregion

            #region IReduce members

            // Not in Java original

            /// <summary>
            /// Reduce the collection using a function.
            /// </summary>
            /// <param name="f">The function to apply.</param>
            /// <returns>The reduced value</returns>
            public object reduce(IFn f)
            {
                object ret = _v.nth(_i);
                for (int x = _i-1; x >= 0; x--)
                    ret = f.invoke(ret, _v.nth(x));
                return ret;
            }

            /// <summary>
            /// Reduce the collection using a function.
            /// </summary>
            /// <param name="f">The function to apply.</param>
            /// <param name="start">An initial value to get started.</param>
            /// <returns>The reduced value</returns>
            public object reduce(IFn f, object start)
            {
                object ret = start;
                for (int x = _i; x >= 0; x--)
                   ret = f.invoke(ret, _v.nth(x));
                return ret;
            }

            #endregion

        } // nested class RSeq

        /// <summary>
        /// Internal class providing subvector functionality for <see cref="APersistentVector">APersistentVector</see>.
        /// </summary>
        [Serializable]
        public sealed class SubVector : APersistentVector, IPersistentCollection, IObj, IEnumerable
        {
            #region Data

            /// <summary>
            /// The vector being subvectored.
            /// </summary>
            readonly IPersistentVector _v;

            /// <summary>
            /// The vector being subvectored.
            /// </summary>
            public IPersistentVector V { get { return _v; } }

            /// <summary>
            /// The start index of the subvector.
            /// </summary>
            readonly int _start;

            /// <summary>
            /// The start index of the subvector.
            /// </summary>
            /// public int Start { get { return _start; } }

            /// <summary>
            /// The end index of the subvector.
            /// </summary>
            readonly int _end;

            /// <summary>
            /// The end index of the subvector.
            /// </summary>
            public int End { get { return _end; } }

            readonly IPersistentMap _meta;

            #endregion

            #region C-tors and factory methods

            /// <summary>
            /// Initialize a subvector, with the given metadata and start/end indices.
            /// </summary>
            /// <param name="meta">The metatdata to attach.</param>
            /// <param name="v">The vector to subvector.</param>
            /// <param name="start">The start index of the subvector.</param>
            /// <param name="end">The end index of the subvector.</param>
            public SubVector(IPersistentMap meta, IPersistentVector v, int start, int end)
            {
                _meta = meta;

                SubVector sv = v as SubVector;
                if (sv != null)
                {
                    start += sv._start;
                    end += sv._start;
                    v = sv._v;
                }
                _v = v;
                _start = start;
                _end = end;
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
                    : new SubVector(meta, _v, _start, _end);
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
            /// Gets the number of items in the collection.
            /// </summary>
            /// <returns>The number of items in the collection.</returns>
            public override int count()
            {
                return _end - _start;
            }

            /// <summary>
            /// Gets an empty collection of the same type.
            /// </summary>
            /// <returns>An emtpy collection.</returns>
            public override IPersistentCollection empty()
            {
                return (IPersistentCollection)PersistentVector.EMPTY.withMeta(_meta);
            }

            /// <summary>
            /// Returns a new collection that has the given element cons'd on front of the eixsting collection.
            /// </summary>
            /// <param name="o">An item to put at the front of the collection.</param>
            /// <returns>A new immutable collection with the item added.</returns>
            IPersistentCollection IPersistentCollection.cons(object o)
            {
                return cons(o);
            }

            #endregion

            #region IPersistentVector members

            /// <summary>
            /// Gets the number of items in the vector.
            /// </summary>
            /// <returns>The number of items.</returns>
            /// <remarks>Not sure why you wouldn't use <c>count()</c> intead.</remarks>
            public override int length()
            {
                return count();
            }

            /// <summary>
            /// Get the i-th item in the vector.
            /// </summary>
            /// <param name="i">The index of the item to retrieve/</param>
            /// <returns>The i-th item</returns>
            /// <remarks>Throws an exception if the index <c>i</c> is not in the range of the vector's elements.</remarks>
            public override object nth(int i)
            {
                if (_start + i >= _end || i < 0)
                    throw new ArgumentOutOfRangeException("i");
                return _v.nth(_start + i);
            }

            /// <summary>
            /// Return a new vector with the i-th value set to <c>val</c>.
            /// </summary>
            /// <param name="i">The index of the item to set.</param>
            /// <param name="val">The new value</param>
            /// <returns>A new (immutable) vector v with v[i] == val.</returns>
            public override IPersistentVector assocN(int i, object val)
            {
                if (_start + i > _end)
                    throw new ArgumentOutOfRangeException("i");
                else if (_start + i == _end)
                    return cons(val);
                else
                    return new SubVector(_meta, _v.assocN(_start + i, val), _start, _end);
            }

            /// <summary>
            /// Creates a new vector with a new item at the end.
            /// </summary>
            /// <param name="o">The item to add to the vector.</param>
            /// <returns>A new (immutable) vector with the objected added at the end.</returns>
            public override IPersistentVector cons(object o)
            {
                return new SubVector(_meta, _v.assocN(_end, o), _start, _end + 1);
            }

            #endregion

            #region IPersistentStack members

            /// <summary>
            /// Returns a new stack with the top element popped.
            /// </summary>
            /// <returns>The new stack</returns>
            public override IPersistentStack pop()
            {
                return (_end - 1 == _start)
                    ? (IPersistentStack)PersistentVector.EMPTY
                    : new SubVector(_meta, _v, _start, _end - 1);
            }

            #endregion

            #region IEnumerable members

            IEnumerator IEnumerable.GetEnumerator()
            {
                APersistentVector av = _v as APersistentVector;
                if ( av != null )
                    return av.RangedIterator(_start, _end);
                return base.GetEnumerator();    
            }

            public override IEnumerator<object> GetEnumerator()
            {
                APersistentVector av = _v as APersistentVector;
                if (av != null)
                    return av.RangedIteratorT(_start, _end);
                return base.GetEnumerator();    
            }

            #endregion
        }
    }
}

