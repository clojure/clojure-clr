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
    public class Cycle : ASeq, IReduce, IPending
    {
        #region Data

        readonly ISeq _all;      // never null
        readonly ISeq _prev;
        volatile ISeq _current;  // lazily realized
        volatile ISeq _next;  // cached

        #endregion

        #region Ctors and factories

        private Cycle(ISeq all, ISeq prev, ISeq current)
        {
            _all = all;
            _prev = prev;
            _current = current;
        }

        private Cycle(IPersistentMap meta, ISeq all, ISeq prev, ISeq current, ISeq next)
            :base(meta)
        {
            _all = all;
            _prev = prev;
            _current = current;
            _next = next;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(ISeq vals)
        {
            if (vals == null)
                return PersistentList.EMPTY;
            return new Cycle(vals, null, vals);
        }

        #endregion

        #region ISeq methods

        // realization for use of current
        ISeq Current()
        {
            if (_current == null)
            {
                ISeq c = _prev.next();
                _current = (c == null) ? _all : c;
            }
            return _current;
        }

        public override object first()
        {
            return Current().first();
        }

        public override ISeq next()
        {
            if (_next == null)
                _next = new Cycle(_all, Current(), null);
            return _next;
        }

        #endregion

        #region IObj methods

        public override IObj withMeta(IPersistentMap meta)
        {
            return new Cycle(meta, _all, _prev, _current, _next);
        }

        #endregion

        #region IReduce methods

        public object reduce(IFn f)
        {
            ISeq s = Current();
            Object ret = s.first();
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
            ISeq s = Current();
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

        #region IPending methods

        public bool isRealized()
        {
            return _current != null;
        }

        #endregion
    }
}