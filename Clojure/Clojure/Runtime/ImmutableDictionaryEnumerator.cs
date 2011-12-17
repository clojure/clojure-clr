using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace clojure.lang.Runtime
{
    /// <summary>
    /// Enumerator for IDictionary objects that are immutable.  (No caching, not explicitly made thread-safe.)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1038:EnumeratorsShouldBeStronglyTyped")]
    public class ImmutableDictionaryEnumerator : IDictionaryEnumerator
    {
        #region Data

        IDictionary _dict;
        object[] _keys;
        Int32 _index = -1;

        #endregion

        #region C-tors

        public ImmutableDictionaryEnumerator(IDictionary d)
        {
            // Make a copy of the dictionary entries currently in the SimpleDictionary object.
            _dict = d;
            _keys = new object[d.Count];
            int i = 0;
            foreach (object key in d.Keys)
                _keys[i++] = key;            
        }

        #endregion

        #region IDictionaryEnumerator members

        // Return the current item.
        public Object Current { get { return Entry;  } }

        // Return the current dictionary entry.
        public DictionaryEntry Entry
        {
            get
            {
                ValidateIndex();
                object key = _keys[_index];
                return new DictionaryEntry(key, _dict[key]);
            }
        }

        // Return the key of the current item.
        public Object Key { get { ValidateIndex(); return _keys[_index]; } }

        // Return the value of the current item.
        public Object Value { get { ValidateIndex();  return _dict[_keys[_index]]; } }

        // Advance to the next item.
        public Boolean MoveNext()
        {
            if (_index < _keys.Length - 1) { _index++; return true; }
            return false;
        }

        // Validate the enumeration index and throw an exception if the index is out of range.
        private void ValidateIndex()
        {
            if (_index < 0 || _index >= _keys.Length)
            throw new InvalidOperationException("Enumerator is before or after the collection.");
        }

        // Reset the index to restart the enumeration.
        public void Reset()
        {
            _index = -1;
        }

        #endregion
    }
}
