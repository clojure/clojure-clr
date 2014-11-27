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
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace clojure.lang
{
    [Serializable]
    public sealed class LazySeq : Obj, ISeq, Sequential, ICollection, IList, IList<Object>, IPending, IHashEq  // Should we do IList -- has index accessor
    {
        #region Data

        private IFn _fn;
        private object _sv;
        private ISeq _s;

        #endregion

        #region C-tors & factory methods

        public LazySeq(IFn fn)
        {
            _fn = fn;
        }

        private LazySeq(IPersistentMap meta, ISeq s)
            : base(meta)
        {
            _fn = null;
            _s = s;
        }

        #endregion

        #region Object overrides

        public override int GetHashCode()
        {
            ISeq s = seq();
            if (s == null)
                return 1;
            return Util.hash(s);
        }

        public override bool Equals(object obj)
        {
            ISeq s = seq();
            if (s != null)
                return s.Equals(obj);
            else
                return (obj is Sequential || obj is IList) && RT.seq(obj) == null;
        }

        #endregion

        #region IObj members

        public override IObj withMeta(IPersistentMap meta)
        {
           return new LazySeq(meta,seq());
        }

        #endregion

        #region Seqable Members

        /// <summary>
        /// Gets an <see cref="ISeq"/>to allow first/rest/next iteration through the collection.
        /// </summary>
        /// <returns>An <see cref="ISeq"/> for iteration.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public ISeq seq()
        {
            sval();
            if (_sv != null)
            {
                object ls = _sv;
                _sv = null;
                while (ls is LazySeq)
                    ls = ((LazySeq)ls).sval();
                _s = RT.seq(ls);
            }
            return _s;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        object sval()
        {
            if (_fn != null)
            {
                    _sv = _fn.invoke();
                    _fn = null;
            }
            if ( _sv != null )
                return _sv;

            return _s;
        }

        #endregion
        
        #region IPersistentCollection Members

        public int count()
        {
            int c = 0;
            for (ISeq s = seq(); s != null; s = s.next())
                ++c;
            return c;
        }

        IPersistentCollection IPersistentCollection.cons(object o)
        {
            return cons(o);
        }

        public IPersistentCollection empty()
        {
            return PersistentList.EMPTY;
        }

        public bool equiv(object o)
        {
            ISeq s = seq();
            if (s != null)
                return s.equiv(o);
            else
                return (o is Sequential || o is IList) && RT.seq(o) == null;
        }

        #endregion
        
        #region ISeq Members

        public object first()
        {
            seq();
            if (_s == null)
                return null;
            return _s.first();
        }

        public ISeq next()
        {
            seq();
            if (_s == null)
                return null;
            return _s.next();
        }

        public ISeq more()
        {
            seq();
            if (_s == null)
                return PersistentList.EMPTY;
            return _s.more();
        }

        public ISeq cons(object o)
        {
            return RT.cons(o, seq());
        }

        #endregion

        #region IPending members

        public bool isRealized()
        {
            return _fn == null;
        }

        #endregion

        #region IList Members

        public void Add(object item)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        int IList.Add(object value)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public bool Contains(object value)
        {
            for (ISeq s = seq(); s != null; s = s.next())
                if (Util.equiv(s.first(), value))
                    return true;
            return false;
        }

        public int IndexOf(object value)
        {
            ISeq s = seq();
            for (int i = 0; s != null; s = s.next(), i++)
                if (Util.equiv(s.first(), value))
                    return i;
            return -1;
        }

        public void Insert(int index, object value)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(object item)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        void IList.Remove(object value)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public object this[int index]
        {
            get
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException("index","Index must be non-negative.");

                ISeq s = seq();
                for (int i = 0; s != null; s = s.next(), i++)
                    if (i == index)
                        return s.first();
                throw new ArgumentOutOfRangeException("index", "Index past end of sequence.");
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(object[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", "must be non-negative.");
            if (array.Rank > 1)
                throw new ArgumentException("must not be multidimensional","array" );
            if (arrayIndex >= array.Length)
                throw new ArgumentException("must be less than the length", "arrayIndex");
            if (count() > array.Length - arrayIndex)
                throw new InvalidOperationException("Not enough available space from index to end of the array.");

            ISeq s = seq();
            for (int i = arrayIndex; s != null; ++i, s = s.next())
                array[i] = s.first();
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index","must be non-negative.");
            if (array.Rank > 1)
                throw new ArgumentException("must not be multidimensional.", "array");
            if (index >= array.Length)
                throw new ArgumentException("must be less than the length", "index");
            if (count() > array.Length - index)
                throw new InvalidOperationException("Not enough available space from index to end of the array.");

            ISeq s = seq();
            for (int i = index; s != null; ++i, s = s.next())
                array.SetValue(s.first(), i);
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

        #region IEnumerable Members

        public IEnumerator<object> GetEnumerator()
        {
            return new SeqEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SeqEnumerator(this);
        }

        #endregion

        #region IHashEq members

        public int hasheq()
        {
            return Murmur3.HashOrdered(this);
        }

        #endregion

    }
}
