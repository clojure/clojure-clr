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
    public sealed class KeywordLookupSite: ILookupSite, ILookupThunk
    {
        #region Data

        readonly int _n;
        readonly Keyword _k;

        #endregion

        #region C-tors

        public KeywordLookupSite(int n, Keyword k)
        {
            _n = n;
            _k = k;
        }

        #endregion

        #region ILookupSite Members

        public object fault(object target, ILookupHost host)
        {
            if (target is IKeywordLookup)
                return Install(target, host);
            else if (target is ILookup)
            {
                host.swapThunk(_n, CreateThunk(target.GetType()));
                return ((ILookup)target).valAt(_k);
            }
            host.swapThunk(_n, this);
            return RT.get(target, _k);
        }

        #endregion

        #region ILookupThunk Members

        public object get(object target)
        {
            if (target is IKeywordLookup || target is ILookup)
                return this;
            return RT.get(target, _k);
        }

        #endregion

        #region Implementation

        private object Install(object target, ILookupHost host)
        {
            ILookupThunk t = ((IKeywordLookup)target).getLookupThunk(_k);
            if (t != null)
            {
                host.swapThunk(_n, t);
                return t.get(target);
            }
            host.swapThunk(_n, CreateThunk(target.GetType()));
            return ((ILookup)target).valAt(_k);
        }

        private ILookupThunk CreateThunk(Type type)
        {
            return new SimpleThunk(type,_k);

        }

        class SimpleThunk : ILookupThunk
        {
            Type _type;
            Keyword _kw;

            public SimpleThunk(Type type, Keyword kw)
            {
                _type = type;
                _kw = kw;
            }



            #region ILookupThunk Members

            public object get(object target)
            {
                if (target != null && target.GetType() == _type)
                    return ((ILookup)target).valAt(_kw);
                return this;  

            }

            #endregion
        }

        #endregion
    }
}
