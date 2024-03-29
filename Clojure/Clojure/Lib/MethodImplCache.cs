﻿/**
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

namespace clojure.lang
{
    // TODO: This is a cache for a type=>IFn map.  Should be replaced by the DLR CallSite mechanism
   public sealed class MethodImplCache
   {
       #region nested class

       public sealed class Entry
       {
           #region Data

           readonly Type _t;

           public Type T
           {
               get { return _t; }
           }

           readonly IFn _fn;

            public IFn Fn
           {
               get { return _fn; }
           }

           #endregion

           #region C-tors

           public Entry(Type t, IFn fn)
           {
               _t = t;
               _fn = fn;
           }

           #endregion
       }

       #endregion

       #region Data

       private readonly IPersistentMap _protocol;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IPersistentMap protocol
       {
           get { return _protocol; }
       }

       private readonly Keyword _methodk;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Keyword methodk
       {
           get { return _methodk; }
       }

       private readonly Symbol _sym;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Symbol sym
       {
           get { return _sym; }
       }

       public readonly int _shift;
       public readonly int _mask;
       private readonly object[] _table;    //[class, entry. class, entry ...]
       public readonly IDictionary _map;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IDictionary map
       {
           get { return _map; }
       }

       Entry _mre;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
       public object[] table
        {
            get { return _table; }
        } 


       // //these are not volatile by design
       // private object _lastType;

       //// core_deftype.clj compatibility
       // public object lastClass
       // {
       //     get { return _lastType; }
       //     set { _lastType = value; }  
       // }
       // private IFn _lastImpl;

       // // core_deftype.clj compatibility 
       // public IFn lastImpl
       // {
       //     get { return _lastImpl; }
       //     set { _lastImpl = value; }
        //}

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
