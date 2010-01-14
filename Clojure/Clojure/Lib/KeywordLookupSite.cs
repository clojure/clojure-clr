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
    public sealed class KeywordLookupSite: ILookupSite   /* , ILookupThunk -- this interface has been replaced by a delegate */
    {
        // Where 'this' is used as an ILookupThunk, create a delegate on the 'get' method.

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
            host.swapThunk(_n, this.Get);
            return RT.get(target, _k);
        }

        #endregion

        #region ILookupThunk Members -- not really

        public object Get(object target)
        {
            if (target is IKeywordLookup || target is ILookup)
                return this;
            return RT.get(target, _k);
        }

        #endregion

        #region Implementation

        private object Install(object target, ILookupHost host)
        {
            // JVM: ILookupThunk t = ((IKeywordLookup)target).getLookupThunk(_k);
            LookupThunkDelegate t = ((IKeywordLookup)target).getLookupThunk(_k);
            if (t != null)
            {
                host.swapThunk(_n, t);
                // JVM: return t.get(target);
                return t(target);
            }
            host.swapThunk(_n, CreateThunk(target.GetType()));
            return ((ILookup)target).valAt(_k);
        }

        private LookupThunkDelegate CreateThunk(Type type)
        {
            //return new ILookupThunk(){
            //        public Object get(Object target){
            //            if(target != null && target.getClass() == c)
            //                return ((ILookup) target).valAt(k);
            //            return this;
            //        }
            //    };       
            return (object target) => { 
                if (target != null && target.GetType() == type )
                    return ((ILookup)target).valAt(_k);
                return this;
            };
        }

        

        #endregion
    }
}
