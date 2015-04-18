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
    [Serializable]
    public abstract class APersistentSet : AFn, IPersistentSet, ICollection, ICollection<Object>, IHashEq  // , Set -- no equivalent,  Should we do ISet<Object>?
    {
        #region Data

        /// <summary>
        /// Caches the hash code, when computed.
        /// </summary>
        protected int _hash = -1;

        /// <summary>
        /// Caches the hashseq code, when computed.
        /// </summary>
        /// <remarks>The value <value>-1</value> indicates that the hasheq code has not been computed yet.</remarks>
        int _hasheq = -1;

        /// <summary>
        /// The underlying map that contains the set's elements.
        /// </summary>
        protected readonly IPersistentMap _impl;

        #endregion

        #region C-tors and factory methods

        /// <summary>
        /// Initialize an <cref see="APersistentSet">APersistentSet</cref> from the metadata map and the data map.
        /// </summary>
        /// <param name="impl">The underlying implementation map</param>
        protected APersistentSet(IPersistentMap impl)
        {
            _impl = impl;
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// Returns a string representing the current object.
        /// </summary>
        /// <returns>A string representing the current object.</returns>
        public override string ToString()
        {
            return RT.printString(this);
        }

        /// <summary>
        /// Computes a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>The hash code is value-based (based on the items in the set).  
        /// Once computed, the value is cached.</remarks>
        public override int GetHashCode()
        {
            if (_hash == -1)
            {
                int hash = 0;
                for (ISeq s = seq(); s != null; s = s.next())
                {
                    object e = s.first();
                    hash += Util.hash(e);
                }
                _hash = hash;
            }
            return _hash;
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
            return setEquals(this, obj);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "set")]
        public static bool setEquals(IPersistentSet s1, object obj)
        {
            // I really can't do what the Java version does.
            // It casts to a Set.  No such thing here.  We'll use IPersistentSet instead.
            

            if (s1 == obj)
                return true;

#if CLR2
            // No System.Collections.Generic.ISet<T>
#else
            ISet<Object> is2 = obj as ISet<Object>;
            if (is2 != null)
            {
                if (is2.Count != s1.count())
                    return false;
                foreach (Object elt in is2)
                    if (!s1.contains(elt))
                        return false;
                return true;
            }
#endif

            IPersistentSet s2 = obj as IPersistentSet;
            if (s2 == null)
                return false;

            if (s2.count() != s1.count())   
                return false;

            for (ISeq seq = s2.seq(); seq != null; seq = seq.next())
                if (!s1.contains(seq.first()))
                    return false;

            return true;
        }

        #endregion

        #region IPersistentSet Members

        /// <summary>
        /// Get a set with the given item removed.
        /// </summary>
        /// <param name="key">The item to remove.</param>
        /// <returns>A new set with the item removed.</returns>
        public abstract IPersistentSet disjoin(object key);

        /// <summary>
        /// Test if the set contains the key.
        /// </summary>
        /// <param name="key">The value to test for membership in the set.</param>
        /// <returns>True if the item is in the collection; false, otherwise.</returns>
        public bool contains(object key)
        {
            return _impl.containsKey(key);
        }

        /// <summary>
        /// Get the value for the key (= the key itself, or null if not present).
        /// </summary>
        /// <param name="key">The value to test for membership in the set.</param>
        /// <returns>the key if the key is in the set, else null.</returns>
        public object get(object key)
        {
            return _impl.valAt(key);
        }

        #endregion

        #region IPersistentCollection Members

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <returns>The number of items in the collection.</returns>
        public int count()
        {
            return _impl.count();
        }


        /// <summary>
        /// Gets an ISeq to allow first/rest iteration through the collection.
        /// </summary>
        /// <returns>An ISeq for iteration.</returns>
        public ISeq seq()
        {
            return APersistentMap.KeySeq.create(_impl.seq());
        }

        public abstract IPersistentCollection cons(object o);
        public abstract IPersistentCollection empty();

        /// <summary>
        /// Determine if an object is equivalent to this (handles all collections).
        /// </summary>
        /// <param name="o">The object to compare.</param>
        /// <returns><c>true</c> if the object is equivalent; <c>false</c> otherwise.</returns>
        public bool equiv(object o)
        {
            return setEquals(this,o);
        }

        #endregion

        #region IFn members

        public override object invoke(object arg1)
        {
            return get(arg1);
        }


        #endregion

        #region ICollection Members

        public void Add(object item)
        {
            throw new InvalidOperationException("Cannot modify an immutable set");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Cannot modify an immutable set");
        }

        public bool Contains(object item)
        {
            return contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            ISeq s = seq();
            if (s != null)
                ((ICollection)s).CopyTo(array, arrayIndex);
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

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public bool Remove(object item)
        {
            throw new InvalidOperationException("Cannot modify an immutable set");
        }

        public object SyncRoot
        {
            get { return this; }
        }

        #endregion

        #region IEnumerable Members
        
        IEnumerator<object> DirectEnumerator()
        {
            var e = _impl.GetEnumerator();
            while (e.MoveNext())
                yield return e.Current.key();
        }

        public IEnumerator<object> GetEnumerator()
        {
            var ime = _impl as IMapEnumerableTyped<Object,Object>;
            if (ime != null)
                return ime.tkeyEnumerator();

            return DirectEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IHashEq members

        public int hasheq()
        {
            if (_hasheq == -1)
            {
                //int hash = 0;
                //for (ISeq s = seq(); s != null; s = s.next())
                //{
                //    object e = s.first();
                //    hash += Util.hasheq(e);
                //}
                //_hasheq = hash;
                _hasheq = Murmur3.HashUnordered(this);
            }
            return _hasheq;
        }

        #endregion
    }
}
