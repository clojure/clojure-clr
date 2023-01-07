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

using System.Collections.Generic;

namespace clojure.lang
{
    /// <summary>
    /// Faking a few of the methods from the Java ConcurrentHashTable class.
    /// </summary>
    public class JavaConcurrentDictionary<TKey, TValue>
    {
        readonly Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        public TValue Get(TKey key)
        {
            lock (_dict)
            {
                return _dict.TryGetValue(key, out TValue val) ? val : default;
            }
        }

        public TValue PutIfAbsent(TKey key, TValue val)
        {
            lock (_dict)
            {
                if (_dict.TryGetValue(key, out TValue existingVal))
                    return existingVal;
                else
                {
                    _dict[key] = val;
                    return default;
                }
            }
        }

        public TValue Remove(TKey key)
        {
            lock ( _dict )
            {
                if (_dict.TryGetValue(key, out TValue existingVal))
                {
                    _dict.Remove(key);
                    return existingVal;
                }
                else
                    return default;
            }
        }

        public TValue[] Values
        {
            get
            {
                lock (_dict)
                {
                    Dictionary<TKey, TValue>.ValueCollection coll = _dict.Values;
                    TValue[] values = new TValue[coll.Count];
                    coll.CopyTo(values, 0);
                    return values;
                }
            }
        }

    }
}
