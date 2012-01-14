using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace clojure.lang.Runtime
{
    // Ripped off directly from IPy
    // I couldn't think of a better way to do it.
    // So the code below is under the following:

    /* ****************************************************************************
     *
     * Copyright (c) Microsoft Corporation. 
     *
     * This source code is subject to terms and conditions of the Microsoft Public License. A 
     * copy of the license can be found in the License.html file at the root of this distribution. If 
     * you cannot locate the  Microsoft Public License, please send an email to 
     * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
     * by the terms of the Microsoft Public License.
     *
     * You must not remove this notice, or any other, from this software.
     *
     *
     * ***************************************************************************/

    public class ListGenericWrapper<T> : IList<T>
    {
        private IList<object> _value;

        public ListGenericWrapper(IList<object> value) { this._value = value; }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return _value.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _value.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _value.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return (T)_value[index];
            }
            set
            {
                this._value[index] = value;
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            _value.Add(item);
        }

        public void Clear()
        {
            _value.Clear();
        }

        public bool Contains(T item)
        {
            return _value.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _value.Count; i++)
            {
                array[arrayIndex + i] = (T)_value[i];
            }
        }

        public int Count
        {
            get { return _value.Count; }
        }

        public bool IsReadOnly
        {
            get { return _value.IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return _value.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return new IEnumeratorOfTWrapper<T>(_value.GetEnumerator());
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        #endregion
    }


    public class DictionaryGenericWrapper<K, V> : IDictionary<K, V>
    {
        private IDictionary<object, object> self;

        public DictionaryGenericWrapper(IDictionary<object, object> self)
        {
            this.self = self;
        }

        #region IDictionary<K,V> Members

        public void Add(K key, V value)
        {
            self.Add(key, value);
        }

        public bool ContainsKey(K key)
        {
            return self.ContainsKey(key);
        }

        public ICollection<K> Keys
        {
            get
            {
                List<K> res = new List<K>();
                foreach (object o in self.Keys)
                {
                    res.Add((K)o);
                }
                return res;
            }
        }

        public bool Remove(K key)
        {
            return self.Remove(key);
        }

        public bool TryGetValue(K key, out V value)
        {
            object outValue;
            if (self.TryGetValue(key, out outValue))
            {
                value = (V)outValue;
                return true;
            }
            value = default(V);
            return false;
        }

        public ICollection<V> Values
        {
            get
            {
                List<V> res = new List<V>();
                foreach (object o in self.Values)
                {
                    res.Add((V)o);
                }
                return res;
            }
        }

        public V this[K key]
        {
            get
            {
                return (V)self[key];
            }
            set
            {
                self[key] = value;
            }
        }

        #endregion

        #region ICollection<KeyValuePair<K,V>> Members

        public void Add(KeyValuePair<K, V> item)
        {
            self.Add(new KeyValuePair<object, object>(item.Key, item.Value));
        }

        public void Clear()
        {
            self.Clear();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return self.Contains(new KeyValuePair<object, object>(item.Key, item.Value));
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<K, V> kvp in this)
            {
                array[arrayIndex++] = kvp;
            }
        }

        public int Count
        {
            get { return self.Count; }
        }

        public bool IsReadOnly
        {
            get { return self.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            return self.Remove(new KeyValuePair<object, object>(item.Key, item.Value));
        }

        #endregion

        #region IEnumerable<KeyValuePair<K,V>> Members

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            foreach (KeyValuePair<object, object> kv in self)
            {
                yield return new KeyValuePair<K, V>((K)kv.Key, (V)kv.Value);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return self.GetEnumerator();
        }

        #endregion
    }


    public class IEnumeratorOfTWrapper<T> : IEnumerator<T>
    {
        IEnumerator _enumerable;
        bool _disposed = false;


        public IEnumeratorOfTWrapper(IEnumerator enumerable)
        {
            this._enumerable = enumerable;
        }

        #region IEnumerator<T> Members

        public T Current
        {
            get { return (T)_enumerable.Current; }
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
                    ((IDisposable)_enumerable).Dispose();
                }

                _disposed = true;
            }
        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get { return _enumerable.Current; }
        }

        public bool MoveNext()
        {
            return _enumerable.MoveNext();
        }

        public void Reset()
        {
            _enumerable.Reset();
        }

        #endregion
    }

    public class IEnumerableOfTWrapper<T> : IEnumerable<T>, IEnumerable, IDisposable
    {
        IEnumerable _enumerable;
        bool _disposed = false;

        public IEnumerableOfTWrapper(IEnumerable enumerable)
        {
            this._enumerable = enumerable;
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return new IEnumeratorOfTWrapper<T>(_enumerable.GetEnumerator());
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
                    ((IDisposable)_enumerable).Dispose();
                }

                _disposed = true;
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
