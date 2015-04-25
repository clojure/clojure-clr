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
    public class Iterate : ASeq, IReduce
    {
        #region Data

        readonly IFn _f;      // never null
        readonly Object _seed;
        volatile ISeq _next;  // cached

        #endregion

        #region Ctors and factories

        Iterate(IFn f, Object seed)
        {
            _f = f;
            _seed = seed;
        }

        private Iterate(IPersistentMap meta, IFn f, Object seed)
            :base(meta)
        {
            _f = f;
            _seed = seed;
        }

        public static ISeq create(IFn f, Object seed)
        {
            return new Iterate(f, seed);
        }

        #endregion

        #region ISeq

        public override object first()
        {
            return _seed;
        }

        public override ISeq next()
        {
            if (_next == null)
            {
                _next = new Iterate(_f, _f.invoke(_seed));
            }
            return _next;
        }

        #endregion

        #region IObj

        public override IObj withMeta(IPersistentMap meta)
        {
            return new Iterate(meta, _f, _seed);
        }

        #endregion

        #region IReduce

        public object reduce(IFn rf)
        {
            Object ret = _seed;
            Object v = _f.invoke(_seed);
            while (true)
            {
                ret = rf.invoke(ret, v);
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref();
                v = _f.invoke(v);
            }
        }

        public object reduce(IFn rf, object start)
        {
            Object ret = start;
            Object v = _seed;
            while (true)
            {
                ret = rf.invoke(ret, v);
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref();
                v = _f.invoke(v);
            }

        }

        #endregion
    }
}