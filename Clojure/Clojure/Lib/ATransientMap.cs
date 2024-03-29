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
    public abstract class ATransientMap : AFn, ITransientMap, ITransientAssociative2
    {
        #region Methods to be supplied by derived classes

        abstract protected void EnsureEditable();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        abstract protected ITransientMap doAssoc(object key, object val);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        abstract protected ITransientMap doWithout(object key);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        abstract protected object doValAt(object key, object notFound);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        abstract protected int doCount();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        abstract protected IPersistentMap doPersistent();

        #endregion

        IPersistentCollection ITransientCollection.persistent()
        {
            return persistent();
        }

        ITransientCollection ITransientCollection.conj(object val)
        {
            return conj(val);
        }

        ITransientAssociative ITransientAssociative.assoc(object key, object val)
        {
            return assoc(key, val);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public ITransientMap conj(object val)
        {
            EnsureEditable();

            {
                if (val is IMapEntry e)
                    return assoc(e.key(), e.val());
            }

            if (val is DictionaryEntry de)
            {
                return assoc(de.Key, de.Value);
            }

            {
                if (val is IPersistentVector v)
                {
                    if (v.count() != 2)
                        throw new ArgumentException("Vector arg to map conj must be a pair");
                    return assoc(v.nth(0), v.nth(1));
                }
            }

            // TODO: also handle KeyValuePair?
            ITransientMap ret = this;
            for (ISeq es = RT.seq(val); es != null; es = es.next())
            {
                IMapEntry e = (IMapEntry)es.first();
                ret = ret.assoc(e.key(), e.val());
            }
            return ret;
        }


        #region IFn overloads

        public override object invoke(object arg1)
        {
            return valAt(arg1);
        }

        public override object invoke(object arg1, object arg2)
        {
            return valAt(arg1, arg2);
        }

        #endregion

        public object valAt(object key)
        {
            return valAt(key, null);
        }

        public object valAt(object key, object notFound)
        {
            EnsureEditable();
            return doValAt(key, notFound);
        }

        public ITransientMap assoc(object key, object val)
        {
            EnsureEditable();
            return doAssoc(key, val);
        }

        public ITransientMap without(object key)
        {
            EnsureEditable();
            return doWithout(key);
        }

        public IPersistentMap persistent()
        {
            EnsureEditable();
            return doPersistent();
        }

        public int count()
        {
            EnsureEditable();
            return doCount();
        }

        private static readonly Object NOT_FOUND = new object();

        public bool containsKey(object key)
        {
            return valAt(key, NOT_FOUND) != NOT_FOUND;
        }

        public IMapEntry entryAt(object key)
        {
            Object v = valAt(key, NOT_FOUND);
            if (v != NOT_FOUND)
                return MapEntry.create(key, v);
            return null;
        }
    }
}
