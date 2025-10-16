/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections;

namespace clojure.lang
{
    // TODO: This is a cache for a type=>IFn map.  Should be replaced by the DLR CallSite mechanism
    public sealed class MethodImplCache
    {
        public sealed class Entry
        {
            readonly Type _t;
            public Type T => _t;

            readonly IFn _fn;
            public IFn Fn => _fn;

            public Entry(Type t, IFn fn)
            {
                _t = t;
                _fn = fn;
            }
        }

        #region Data

        private readonly IPersistentMap _protocol;
        private readonly Keyword _methodk;
        private readonly Symbol _sym;
        public readonly int _shift;
        public readonly int _mask;
        private readonly object[] _table;    //[class, entry. class, entry ...]
        public readonly IDictionary _map;
        Entry _mre;

        // Accessors

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IPersistentMap protocol => _protocol;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Keyword methodk => _methodk;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Symbol sym => _sym;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IDictionary map => _map;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public object[] table => _table;

        #endregion

        #region C-tors

        public MethodImplCache(Symbol sym, IPersistentMap protocol, Keyword methodk)
            : this(sym, protocol, methodk, 0, 0, RT.EmptyObjectArray)
        {
        }

        public MethodImplCache(Symbol sym, IPersistentMap protocol, Keyword methodk, int shift, int mask, Object[] table)
        {
            _sym = sym;
            _protocol = protocol;
            _methodk = methodk;
            _shift = shift;
            _mask = mask;
            _table = table;
            _map = null;
        }

        public MethodImplCache(Symbol sym, IPersistentMap protocol, Keyword methodk, IDictionary map)
        {
            _sym = sym;
            _protocol = protocol;
            _methodk = methodk;
            _shift = 0;
            _mask = 0;
            _table = null;
            _map = map;
        }

        #endregion

        #region Implementation

        // initial lowercase for core.clj compatibility
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IFn fnFor(Type t)
        {
            Entry last = _mre;
            if (last != null && last.T == t)
                return last.Fn;
            return FindFnFor(t);
        }

        IFn FindFnFor(Type t)
        {
            if (_map != null)
            {
                Entry e = (Entry)_map[t];
                _mre = e;
                return e?.Fn;
            }
            else
            {
                int idx = ((Util.hash(t) >> _shift) & _mask) << 1;
                if (idx < _table.Length && ((Type)_table[idx]) == t)
                {
                    Entry e = ((Entry)table[idx + 1]);
                    _mre = e;
                    return e?.Fn;
                }
                return null;
            }
        }

        #endregion
    }
}
