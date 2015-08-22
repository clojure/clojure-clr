/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/* Alex Miller 3/3/15 */

/**
 *   CLR version author: David Miller
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !CLR2
using System.Collections.Concurrent;
#endif

namespace clojure.lang
{
    public sealed class TransformerEnumerator : IEnumerator, IEnumerator<Object>
    {
        #region Interfaces

        private interface IBuffer
        {
            IBuffer Add(Object o);
            Object Remove();
            bool IsEmpty();
        }

        #endregion

        #region Transform helper class

        private class TransformHelper : AFn
        {
            Action<Object> _f;

            public TransformHelper(Action<Object> f)
            {
                _f = f; 
            }

            public override object invoke()
            {
                return null;
            }

            public override object invoke(object acc)
            {
                return acc;
            }

            public override object invoke(object acc, object o)
            {
                _f(o);
                return acc;
            }
        }

        #endregion

        #region Data

        static readonly IBuffer EMPTY = new Empty();
        static readonly Object NONE = new Object();

        // Source
        readonly IEnumerator _sourceEnum;
        readonly IFn _xf;
        readonly bool _multi;


        // Iteration state
        volatile IBuffer _buffer = EMPTY;
        volatile Object _next = NONE;
        volatile bool _completed = false;

        #endregion

        #region Ctors and factories

        TransformerEnumerator(IFn xform, IEnumerator sourceEnum, bool multi)
        {
            _sourceEnum = sourceEnum;
            _xf = (IFn)xform.invoke(new TransformHelper(o => _buffer = _buffer.Add(o)));
            _multi = multi;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static IEnumerator create(IFn xform, IEnumerator source)
        {
            return new TransformerEnumerator(xform, source, false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static IEnumerator createMulti(IFn xform, ICollection sources)
        {
            IEnumerator[] iters = new IEnumerator[sources.Count];
            int i = 0;
            foreach (IEnumerator item in sources)
                iters[i++] = item;
            return new TransformerEnumerator(xform, new MultiEnumerator(iters), true);
        }

        #endregion

        #region IEnumerator implementation

        private bool Step()
        {
            _next = NONE;

            while (_next == NONE)
            {
                if (_buffer.IsEmpty())
                {
                    if (_completed)
                    {
                        return false;
                    }
                    else if (_sourceEnum.MoveNext())
                    {
                        Object iter = null;
                        if (_multi)
                            iter = _xf.applyTo(RT.cons(null, _sourceEnum.Current));
                        else
                            iter = _xf.invoke(null, _sourceEnum.Current);

                        if (RT.isReduced(iter))
                        {
                            _xf.invoke(null);
                            _completed = true;
                        }
                    }
                    else
                    {
                        _xf.invoke(null);
                        _completed = true;
                    }
                }
                else
                {
                    _next = _buffer.Remove();
                }
            }
            return true;
        }

        public object Current
        {
            get
            {

                if (_next == NONE)
                    throw new InvalidOperationException("At end of collection");
                return _next;
            }
        }

        public bool MoveNext()
        {
            return Step();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void Reset()
        {
            throw new NotImplementedException();
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }

        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Empty IBuffer

        private class Empty : IBuffer
        {
            public IBuffer Add(Object o)
            {
                return new Single(o);
            }

            public Object Remove()
            {
                throw new InvalidOperationException("Removing object from empty buffer");
            }

            public bool IsEmpty()
            {
                return true;
            }

            public override String ToString()
            {
                return "Empty";
            }
        }

        #endregion

        #region Single IBuffer

        private class Single : IBuffer
        {
            volatile Object _val;

            public Single(Object o)
            {
                _val = o;
            }

            public IBuffer Add(Object o)
            {
                if (_val == NONE)
                {
                    _val = o;
                    return this;
                }
                else
                {
                    return new Many(_val, o);
                }
            }

            public Object Remove()
            {
                if (_val == NONE)
                {
                    throw new InvalidOperationException("Removing object from empty buffer");
                }
                Object ret = _val;
                _val = NONE;
                return ret;
            }

            public bool IsEmpty()
            {
                return _val == NONE;
            }

            public override String ToString()
            {
                return "Single: " + _val;
            }
        }

        #endregion

        #region Many IBuffer

        private class Many : IBuffer
        {

#if CLR2
            readonly Queue _vals = Queue.Synchronized(new Queue());
#else
            readonly ConcurrentQueue<Object> _vals = new ConcurrentQueue<object>();
#endif

            public Many(Object o1, Object o2)
            {
                _vals.Enqueue(o1);
                _vals.Enqueue(o2);
            }

            public IBuffer Add(Object o)
            {
                _vals.Enqueue(o);
                return this;
            }

            public Object Remove()
            {
#if CLR2
                try
                {
                    return _vals.Dequeue();
                }
                catch (InvalidOperationException)
                {
                    // continue
                }
#else
                object val;
                if (_vals.TryDequeue(out val))
                    return val;
#endif
                return null;
            }

            public bool IsEmpty()
            {
#if CLR2
                return _vals.Count == 0;
#else
                return _vals.IsEmpty;
#endif
            }

            public override String ToString()
            {
                return "Many: " + _vals.ToString();
            }
        }
        #endregion

        #region MultiEnumerator class

        private class MultiEnumerator : IEnumerator
        {
            private readonly IEnumerator[] _enumerators;

            public MultiEnumerator(IEnumerator[] enumerators)
            {
                _enumerators = enumerators;
            }

            public object Current
            {
                get
                {
                    object[] currents = new object[_enumerators.Length];
                    for (int i = 0; i < _enumerators.Length; i++)
                        currents[i] = _enumerators[i].Current;
                    return new ArraySeq_object(null, currents, 0);
                }
            }

            public bool MoveNext()
            {
                foreach (IEnumerator ie in _enumerators)
                    if (!ie.MoveNext())
                        return false;
                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IDispose methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "disposing"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private void Dispose(bool disposing)
        {
        }

        #endregion

    }
}
