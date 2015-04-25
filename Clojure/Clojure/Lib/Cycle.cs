/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/* Alex Miller, Dec 5, 2014 */

/**
 *   Author: David Miller
 **/

using System;

namespace clojure.lang
{
    public class Cycle : ASeq, IReduce
    {
        #region Data

        readonly ISeq _all;      // never null
        readonly ISeq _current;  // never null
        volatile ISeq _next;  // cached

        #endregion

        #region Ctors and factories

        private Cycle(ISeq all, ISeq current)
        {
            _all = all;
            _current = current;
        }

        private Cycle(IPersistentMap meta, ISeq all, ISeq current)
            :base(meta)
        {
            _all = all;
            _current = current;
        }

        public static ISeq create(ISeq vals)
        {
            if (vals == null)
                return PersistentList.EMPTY;
            return new Cycle(vals, vals);
        }

        #endregion

        #region ISeq methods

        public override object first()
        {
            return _current.first();
        }

        public override ISeq next()
        {
            if (_next == null)
            {
                ISeq next = _current.next();
                if (next != null)
                    _next = new Cycle(_all, next);
                else
                    _next = new Cycle(_all, _all);
            }
            return _next;
        }

        #endregion

        #region IObj methods

        public override IObj withMeta(IPersistentMap meta)
        {
            return new Cycle(meta, _all, _current);
        }

        #endregion

        #region IReduce methods

        public object reduce(IFn f)
        {
            Object ret = _current.first();
            ISeq s = _current;
            while (true)
            {
                s = s.next();
                if (s == null)
                    s = _all;
                ret = f.invoke(ret, s.first());
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref();
            }
        }

        public object reduce(IFn f, object start)
        {
            Object ret = start;
            ISeq s = _current;
            while (true)
            {
                ret = f.invoke(ret, s.first());
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref();
                s = s.next();
                if (s == null)
                    s = _all;
            }
        }

        #endregion
    }
}