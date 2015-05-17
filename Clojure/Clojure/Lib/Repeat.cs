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
    public class Repeat : ASeq, IReduce
    {
        #region Data

        const long INFINITE = -1;

        readonly long _count;  // always INFINITE or >0
        readonly Object _val;
        volatile ISeq _next;  // cached

        #endregion

        #region Ctors and factories

        Repeat(long count, Object val)
        {
            _count = count;
            _val = val;
        }

        Repeat(IPersistentMap meta, long count, Object val)
            :base(meta)
        {
            _count = count;
            _val = val;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static Repeat create(Object val)
        {
            return new Repeat(INFINITE, val);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(long count, Object val)
        {
            if (count <= 0)
                return PersistentList.EMPTY;
            return new Repeat(count, val);
        }

        #endregion

        #region ISeq methods

        public override object first()
        {
            return _val;
        }

        public override ISeq next()
        {
            if (_next == null)
            {
                if (_count > 1)
                    _next = new Repeat(_count - 1, _val);
                else if (_count == INFINITE)
                    _next = this;
            }
            return _next;
        }

        #endregion

        #region IObj methodsw

        public override IObj withMeta(IPersistentMap meta)
        {
            return new Repeat(meta, _count, _val);
        }

        #endregion

        #region IReduce methods

        public object reduce(IFn f)
        {
            Object ret = _val;
            if (_count == INFINITE)
            {
                while (true)
                {
                    ret = f.invoke(ret, _val);
                    if (RT.isReduced(ret))
                        return ((IDeref)ret).deref();
                }
            }
            else
            {
                for (long i = 1; i < _count; i++)
                {
                    ret = f.invoke(ret, _val);
                    if (RT.isReduced(ret))
                        return ((IDeref)ret).deref();
                }
                return ret;
            }

        }

        public object reduce(IFn f, object start)
        {
            Object ret = start;
            if (_count == INFINITE)
            {
                while (true)
                {
                    ret = f.invoke(ret, _val);
                    if (RT.isReduced(ret))
                        return ((IDeref)ret).deref();
                }
            }
            else
            {
                for (long i = 0; i < _count; i++)
                {
                    ret = f.invoke(ret, _val);
                    if (RT.isReduced(ret))
                        return ((IDeref)ret).deref();
                }
                return ret;
            }
        }

        #endregion
    }
}