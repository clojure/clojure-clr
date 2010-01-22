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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    // TODO: This is a cache for a type=>IFn map.  Should be replaced by the DLR CallSite mechanism
   public sealed class MethodImplCache
   {
       #region Data

        public readonly IPersistentMap _protocol;
        public readonly Keyword _methodk;
        public readonly int _shift;
        public readonly int _mask;
        public readonly object[] _table;    //[class, fn. class, fn ...]

        //these are not volatile by design
        public object _lastType;
        public IFn _lastImpl;

       #endregion

       #region C-tors

        public MethodImplCache(IPersistentMap protocol, Keyword methodk)
            : this(protocol, methodk, 0, 0, RT.EMPTY_OBJECT_ARRAY)
        {
        }

        public MethodImplCache(IPersistentMap protocol, Keyword methodk, int shift, int mask, Object[] table)
        {
            _protocol = protocol;
            _methodk = methodk;
            _shift = shift;
            _mask = mask;
            _table = table;
            _lastType = this;
        }

       #endregion


       #region Implementation
        
       public IFn FnFor(Type t)
        {
            if (t == _lastType)
                return _lastImpl;
            int idx = ((Util.hash(t) >> _shift) & _mask) << 1;
            if (idx < _table.Length && _table[idx] == t)
            {
                _lastType = t;
                return _lastImpl =
                        (IFn)_table[idx + 1];
            }
            return null;
        }

       #endregion
   }
}
