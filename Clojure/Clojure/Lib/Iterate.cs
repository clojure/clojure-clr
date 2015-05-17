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
    public class Iterate : ASeq, IReduce, IPending
    {
        #region Data

        static readonly Object UNREALIZED_SEED = new Object();
        readonly IFn _f;      // never null
        readonly Object _prevSeed;
        volatile Object _seed; // lazily realized
        volatile ISeq _next;  // cached

        #endregion

        #region Ctors and factories

        Iterate(IFn f, Object prevSeed, Object seed)
        {
            _f = f;
            _prevSeed = prevSeed;
            _seed = seed;
        }

        private Iterate(IPersistentMap meta, IFn f, Object prevSeed, Object seed, ISeq next)
            :base(meta)
        {
            _f = f;
            _prevSeed = prevSeed;
            _seed = seed;
            _next = next;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static ISeq create(IFn f, Object seed)
        {
            return new Iterate(f, null, seed);
        }

        #endregion

        #region ISeq

        public override object first()
        {
            if (_seed == UNREALIZED_SEED)
                _seed = _f.invoke(_prevSeed);

            return _seed;
        }

        public override ISeq next()
        {
            if (_next == null)
            {
                _next = new Iterate(_f, first(), UNREALIZED_SEED);
            }
            return _next;
        }

        #endregion

        #region IObj

        public override IObj withMeta(IPersistentMap meta)
        {
            return new Iterate(meta, _f, _prevSeed, _seed, _next);
        }

        #endregion

        #region IReduce

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#")]
        public object reduce(IFn rf)
        {
            Object ff = first();
            Object ret = ff;
            Object v = _f.invoke(ff);
            while (true)
            {
                ret = rf.invoke(ret, v);
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref();
                v = _f.invoke(v);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#")]
        public object reduce(IFn rf, object start)
        {
            Object ret = start;
            Object v = first();
            while (true)
            {
                ret = rf.invoke(ret, v);
                if (RT.isReduced(ret))
                    return ((IDeref)ret).deref();
                v = _f.invoke(v);
            }

        }

        #endregion

        #region IPending methods

        public bool isRealized()
        {
            return _seed != UNREALIZED_SEED;
        }

        #endregion
    }
}