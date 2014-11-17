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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace clojure.lang
{
    public sealed class LazyTransformer : Obj, ISeq, Sequential, ICollection, IList, IList<Object>, IPending, IHashEq
    {
        #region Data

        IStepper _stepper;
        object _first = null;
        LazyTransformer _rest = null;

        #endregion

        #region Ctors, factories

        public LazyTransformer(IStepper stepper)
        {
            _stepper = stepper;
        }

        public static LazyTransformer create(IFn xform, Object coll)
        {
            return new LazyTransformer(new Stepper(xform, RT.iter(coll)));
        }

        public static LazyTransformer createMulti(IFn xform, Object[] colls)
        {
            IEnumerator[] enums = new IEnumerator[colls.Length];
            for (int i = 0; i < colls.Length; i++)
            {
                enums[i] = RT.iter(colls[i]);
            }
            return new LazyTransformer(new MultiStepper(xform, enums));
        }

        LazyTransformer(IPersistentMap meta, object first, LazyTransformer rest)
            : base(meta)
        {
            _stepper = null;
            _first = first;
            _rest = rest;
        }

        #endregion

        #region Object overrides

        public override int GetHashCode()
        {
            ISeq s = seq();
            if (s == null)
                return 1;
            return Util.hash(s);
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!(obj is Sequential || obj is IList))
                return false;
            ISeq ms = RT.seq(obj);
            for (ISeq s = seq(); s != null; s = s.next(), ms = ms.next())
            {
                if (ms == null || !Util.equiv(s.first(), ms.first()))
                    return false;
            }
            return ms == null;
        }

        #endregion

        #region IObj methods

        public override IObj withMeta(IPersistentMap meta)
        {
            seq();
            return new LazyTransformer(meta, _first, _rest);
        }

        #endregion

        #region Sequable methods

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ISeq seq()
        {
            if (_stepper != null)
                _stepper.step(this);
            if (_rest == null)
                return null;
            return this;
        }

        #endregion

        #region IPersistentCollection Members

        public int count()
        {
            int c = 0;
            for (ISeq s = seq(); s != null; s = s.next())
                ++c;
            return c;
        }

        IPersistentCollection IPersistentCollection.cons(object o)
        {
            return cons(o);
        }

        public IPersistentCollection empty()
        {
            return PersistentList.EMPTY;
        }

        public bool equiv(object o)
        {
            if (this == o)
                return true;
            if (!(o is Sequential || o is IList))
                return false;
            ISeq ms = RT.seq(o);
            for (ISeq s = seq(); s != null; s = s.next(), ms = ms.next())
            {
                if (ms == null || !Util.equiv(s.first(), ms.first()))
                    return false;
            }
            return ms == null;
        }

        #endregion

        #region ISeq members

        public object first()
        {
            if (_stepper != null)
                seq();
            if (_rest == null)
                return null;
            return _first;
        }

        public ISeq next()
        {
            if (_stepper != null)
                seq();
            if (_rest == null)
                return null;
            return _rest.seq();
        }

        public ISeq more()
        {
            if (_stepper != null)
                seq();
            if (_rest == null)
                return PersistentList.EMPTY;
            return _rest;
        }

        public ISeq cons(object o)
        {
            return RT.cons(o, seq());
        }

        #endregion

        #region IPending members

        public bool isRealized()
        {
            return _stepper == null;
        }

        #endregion

        #region IList members

        public int Add(object value)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public bool Contains(object value)
        {
            for (ISeq s = seq(); s != null; s = s.next())
                if (Util.equiv(s.first(), value))
                    return true;
            return false;
        }

        public int IndexOf(object value)
        {
            ISeq s = seq();
            for (int i = 0; s != null; s = s.next(), i++)
                if (Util.equiv(s.first(), value))
                    return i;
            return -1;
        }

        public void Insert(int index, object value)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public void Remove(object value)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public object this[int index]
        {
            get
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException("index", "Index must be non-negative.");

                ISeq s = seq();
                for (int i = 0; s != null; s = s.next(), i++)
                    if (i == index)
                        return s.first();
                throw new ArgumentOutOfRangeException("index", "Index past end of sequence.");
            }
            set
            {
                throw new InvalidOperationException("Cannot modify immutable sequence");
            }
        }

        #endregion

        #region ICollection members

        void ICollection<object>.Add(object item)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", "must be non-negative.");
            if (array.Rank > 1)
                throw new ArgumentException("must not be multidimensional", "array");
            if (arrayIndex >= array.Length)
                throw new ArgumentException("must be less than the length", "arrayIndex");
            if (count() > array.Length - arrayIndex)
                throw new InvalidOperationException("Not enough available space from index to end of the array.");

            ISeq s = seq();
            for (int i = arrayIndex; s != null; ++i, s = s.next())
                array[i] = s.first();
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", "must be non-negative.");
            if (array.Rank > 1)
                throw new ArgumentException("must not be multidimensional.", "array");
            if (index >= array.Length)
                throw new ArgumentException("must be less than the length", "index");
            if (count() > array.Length - index)
                throw new InvalidOperationException("Not enough available space from index to end of the array.");

            ISeq s = seq();
            for (int i = index; s != null; ++i, s = s.next())
                array.SetValue(s.first(), i);
        }

        public int Count
        {
            get { return count(); }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        bool ICollection<object>.Remove(object item)
        {
            throw new InvalidOperationException("Cannot modify immutable sequence");
        }

        public object SyncRoot
        {
            get { return this; }
        }

        #endregion

        #region IEnumerable methods

        public IEnumerator GetEnumerator()
        {
            return new SeqEnumerator(this);
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return new SeqEnumerator(this);
        }

        #endregion

        #region IHashEq methods

        public int hasheq()
        {
            return Murmur3.HashOrdered(this);
        }

        #endregion

        #region steppers

        public interface IStepper
        {
            void step(LazyTransformer lt);
        }

        class StepperHelper : AFn
        {
            public override object invoke(object result)
            {
                LazyTransformer lt = (LazyTransformer) result;
                lt._stepper = null;
                return result;
            }

            public override object invoke(object result, object input)
            {
                LazyTransformer lt = (LazyTransformer)result;
                lt._first = input;
                lt._rest = new LazyTransformer(lt._stepper);
                lt._stepper = null;
                return lt._rest;
            }
        }

        class Stepper : IStepper
        {
            IEnumerator _ie;
            IFn _xform;
            static StepperHelper _helper = new StepperHelper();
            
            public Stepper(IFn xform, IEnumerator ie)
            {
                _ie = ie;
                _xform = (IFn)xform.invoke(_helper);
            }

            public void step(LazyTransformer lt)
            {
                while (lt._stepper != null && _ie.MoveNext())
                {
                    if (RT.isReduced(_xform.invoke(lt, _ie.Current)))
                    {
                        lt._stepper = null;
                        LazyTransformer et = lt;
                        while (et._rest != null)
                        {
                            et = et._rest;
                            et._stepper = null;
                        }
                        _xform.invoke(et);
                        return;
                    }
                }
                if ( lt._stepper != null )
                    _xform.invoke(lt);
            }
        }

        class MultiStepperHelper : AFn
        {
            public override Object invoke(Object result)
            {
                LazyTransformer lt = (LazyTransformer)result;
			    lt._stepper = null;
			    return lt;
            }

		    public override Object invoke(Object result, Object input)
            {
                LazyTransformer lt = (LazyTransformer)result;
                lt._first = input;
                lt._rest = new LazyTransformer(lt._stepper);
                lt._stepper = null;
                return lt._rest;
            }
        }

        class MultiStepper : IStepper
        {

            IEnumerator[] _ies;
            object[] _nexts;
            IFn _xform;
            static MultiStepperHelper _helper = new MultiStepperHelper();

            public MultiStepper(IFn xform, IEnumerator[] ies)
            {
                _ies = ies;
                _nexts = new object[_ies.Length];
                _xform = (IFn)xform.invoke(_helper);
            }

            bool MoveNext()
            {
                foreach (IEnumerator ie in _ies)
                    if (!ie.MoveNext())
                        return false;
                return true;
            }

            // TODO: THIS IS DEFINITELY WRONG, NEED TO TRACK CURRENT POSITION
            ISeq Current()
            {
                for (int i = 0; i < _ies.Length; i++)
                    _nexts[i] = _ies[i].Current;
                return new ArraySeq_object(null, _nexts, 0);
            }

            public void step(LazyTransformer lt)
            {
                while (lt._stepper != null && MoveNext())
                {
                    if (RT.isReduced(_xform.applyTo(RT.cons(lt, Current()))))
                    {
                        lt._stepper = null;
                        LazyTransformer et = lt;
                        while (et._rest != null)
                        {
                            et = et._rest;
                            et._stepper = null;
                        }
                        _xform.invoke(et);
                        return;
                    }
                }
                if (lt._stepper != null)
                    _xform.invoke(lt);
            }
        }

        #endregion
    }
}
