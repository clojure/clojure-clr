/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace clojure.lang
{
    public sealed class Stream : Seqable, Streamable, Sequential
    {
        #region Data

        static readonly ISeq NO_SEQ = new Cons(null, null);

        ISeq _seq = NO_SEQ;
        readonly IFn _src;
	    readonly IFn _xform;
        Cons _pushed = null;
        IFn _tap = null;

        #endregion

        #region C-tors and factory methods

        public Stream(IFn src)
        {
            _src = src;
            _xform = null;
        }

        public Stream(IFn xform, Stream src)
        {
            _src = src.tap();
            _xform = xform;
        }

        #endregion



        #region Seqable Members

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ISeq seq()
        {
            if (_seq == NO_SEQ)
            {
                tap();
                _seq = makeSeq(_tap);
            }
            return _seq;
        }

        class Seqer : AFn
        {
            IFn _tap;

            public Seqer(IFn tap)
            {
                _tap = tap;
            }

            public override object invoke()
            {
                object v;
                do
                {
                    v = _tap.invoke();
                } while (v == RT.SKIP);
                if (v == RT.EOS)
                    return null;
                return new Cons(v, new LazySeq(this));
            }
        }

        static ISeq makeSeq(IFn tap)
        {
            return RT.seq(new LazySeq(new Seqer(tap)));
        }

        #endregion

        #region Streamable Members

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Stream stream()
        {
            return this;
        }

        #endregion

        #region Tapping

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IFn tap() 
        {
        if (_tap != null)
            throw new InvalidOperationException("Stream already tapped");

        return _tap = makeTap(_xform, _src);
        }

        class Tapper : AFn
        {
            IFn _xform;
            IFn _src;

            public Tapper(IFn xform, IFn src)
            {
                _xform = xform;
                _src = src;
            }

            public override object invoke()
            {
                object v;
                do
                {
                    v = _src.invoke();
                } while (v == RT.SKIP);
                if (_xform == null || v == RT.EOS)
                    return v;
                return _xform.invoke(v);
            }

        }
        
        static IFn makeTap(IFn xform,  IFn src)        
        {
		return new Tapper(xform,src);
		}
	

        #endregion
    }
}
