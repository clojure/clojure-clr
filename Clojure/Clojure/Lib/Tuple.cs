/**
 * Copyright (c) Rich Hickey. All rights reserved.
 * The use and distribution terms for this software are covered by the
 * Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 * which can be found in the file epl-v10.html at the root of this distribution.
 * By using this software in any fashion, you are agreeing to be bound by
 * the terms of this license.
 * You must not remove this notice, or any other, from this software.
 **/

/* rich 7/16/15 */
// proposed by Zach Tellman

/**
 *   Author: David Miller
 **/

using System;
using System.Collections;

namespace clojure.lang
{
    public class Tuple
    {
        #region Data

        public const int MAX_SIZE = 6;
        public static readonly IPersistentVector EMPTY = new T0();

        #endregion

        #region Ctors and factories

        public static IPersistentVector create() { return EMPTY; }
        public static T1 create(Object v0) { return new T1(v0); }
        public static T2 create(Object v0, Object v1) { return new T2(v0, v1); }
        public static T3 create(Object v0, Object v1, Object v2) { return new T3(v0, v1, v2); }
        public static T4 create(Object v0, Object v1, Object v2, Object v3) { return new T4(v0, v1, v2, v3); }
        public static T5 create(Object v0, Object v1, Object v2, Object v3, Object v4) { return new T5(v0, v1, v2, v3, v4); }
        public static T6 create(Object v0, Object v1, Object v2, Object v3, Object v4, Object v5) { return new T6(v0, v1, v2, v3, v4, v5); }

        public static IPersistentVector createFromArray(Object[] items)
        {
            if (items.Length <= Tuple.MAX_SIZE)
            {
                switch (items.Length)
                {
                    case 0:
                        return EMPTY;
                    case 1:
                        return create(items[0]);
                    case 2:
                        return create(items[0], items[1]);
                    case 3:
                        return create(items[0], items[1], items[2]);
                    case 4:
                        return create(items[0], items[1], items[2], items[3]);
                    case 5:
                        return create(items[0], items[1], items[2], items[3], items[4]);
                    case 6:
                        return create(items[0], items[1], items[2], items[3], items[4], items[5]);
                }
            }
            throw new InvalidOperationException("Too large an array for tuple");
        }

        public static IPersistentVector createFromColl(Object coll)
        {
            if (RT.SupportsRandomAccess(coll))               // Java has: coll is RandomAccess
            {
                switch (RT.count(coll))                     // Java has: (ICollection)coll).Count but failes for null, strings, and some others.
                {
                    case 0:
                        return EMPTY;
                    case 1:
                        return create(RT.nth(coll, 0));
                    case 2:
                        return create(RT.nth(coll, 0), RT.nth(coll, 1));
                    case 3:
                        return create(RT.nth(coll, 0), RT.nth(coll, 1), RT.nth(coll, 2));
                    case 4:
                        return create(RT.nth(coll, 0), RT.nth(coll, 1), RT.nth(coll, 2), RT.nth(coll, 3));
                    case 5:
                        return create(RT.nth(coll, 0), RT.nth(coll, 1), RT.nth(coll, 2), RT.nth(coll, 3), RT.nth(coll, 4));
                    case 6:
                        return create(RT.nth(coll, 0), RT.nth(coll, 1), RT.nth(coll, 2), RT.nth(coll, 3), RT.nth(coll, 4), RT.nth(coll, 5));
                }
            }
            return createFromArray(RT.toArray(coll));
        }

        #endregion

        #region support classes

        #region ATuple

        [Serializable]
        public abstract class ATuple : APersistentVector, IObj, IEditableCollection, IKVReduce /* , IReduceInit */  /* in JVM, but no impl */
        {
            protected PersistentVector vec()
            {
                return PersistentVector.adopt(ToArray());
            }

            public IObj withMeta(IPersistentMap meta)
            {
                if (meta == null)
                    return this;

                return vec().withMeta(meta);
            }

            public IPersistentMap meta()
            {
                return null;
            }

            public override IPersistentVector assocN(int i, Object val)
            {
                return vec().assocN(i, val);
            }

            public override  IPersistentCollection empty()
            {
                return EMPTY;
            }

            public override IPersistentStack pop()
            {
                return vec().pop();
            }

            public ITransientCollection asTransient()
            {
                return vec().asTransient();
            }

            public Object kvreduce(IFn f, Object init)
            {
                for (int i = 0; i < count(); i++)
                {
                    init = f.invoke(init, i, nth(i));
                    if (init is Reduced)
                        return ((IDeref)init).deref();
                }
                return init;
            }
        }

        #endregion

        #region T0
        
        [Serializable]
        public sealed class T0 : ATuple
        {
            public override int count()
            {
                return 0;
            }

            public override Object nth(int i)
            {
                throw new ArgumentOutOfRangeException("i");
            }

            public override IPersistentVector cons(Object o)
            {
                return create(o);
            }

            public override bool equiv(Object obj)
            {
                if (obj is T0)
                    return true;
                return base.equiv(obj);
            }
        }
        
        #endregion

        #region T1

        [Serializable]
        public sealed class T1 : ATuple
        {
            public readonly object _v0;

            public T1(Object v0)
            {
                _v0 = v0;
            }

            public override int count()
            {
                return 1;
            }

            public override Object nth(int i)
            {
                switch (i)
                {
                    case 0:
                        return _v0;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override IPersistentVector cons(Object o)
            {
                return create(_v0, o);
            }

            public override bool equiv(Object obj)
            {
                if (this == obj) return true;

                T1 o1 = obj as T1;
                if (o1 != null)
                {
                      return Util.equiv(_v0, o1._v0);
                }
                return base.equiv(obj);
            }
        }

        #endregion

        #region T2

        [Serializable]
        public sealed class T2 : ATuple, IMapEntry
        {
            public readonly object _v0;
            public readonly object _v1;

            public T2(Object v0, Object v1)
            {
                _v0 = v0;
                _v1 = v1;
            }

            public override int count()
            {
                return 2;
            }

            public override Object nth(int i)
            {
                switch (i)
                {
                    case 0:
                        return _v0;
                    case 1:
                        return _v1;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override IPersistentVector cons(Object o)
            {
                return create(_v0, _v1, o);
            }

            public override bool equiv(Object obj)
            {
                if (this == obj) return true;

                T2 o2 = obj as T2;
                if (o2 != null)
                {
                    return Util.equiv(_v0, o2._v0) &&
                           Util.equiv(_v1, o2._v1);
                }
                return base.equiv(obj);
            }


            public Object key()
            {
                return _v0;
            }

            public Object val()
            {
                return _v1;
            }

            public Object getKey()
            {
                return _v0;
            }

            public Object getValue()
            {
                return _v1;
            }

            public Object setValue(Object value)
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        #region T3

        [Serializable]
        public sealed class T3 : ATuple
        {
            public readonly object _v0;
            public readonly object _v1;
            public readonly object _v2;

            public T3(Object v0, Object v1, Object v2)
            {
                _v0 = v0;
                _v1 = v1;
                _v2 = v2;
            }

            public override int count()
            {
                return 3;
            }

            public override Object nth(int i)
            {
                switch (i)
                {
                    case 0:
                        return _v0;
                    case 1:
                        return _v1;
                    case 2:
                        return _v2;
                    default:
                        throw new ArgumentOutOfRangeException("i");
                }
            }

            public override IPersistentVector cons(Object o)
            {
                return create(_v0, _v1, _v2, o);
            }

            public override bool equiv(Object obj)
            {
                if (this == obj) return true;

                T3 o3 = obj as T3;
                if (o3 != null)
                {
                    return Util.equiv(_v0, o3._v0) &&
                           Util.equiv(_v1, o3._v1) &&
                           Util.equiv(_v2, o3._v2);
                }
                return base.equiv(obj);
            }
        }

        #endregion

        #region T4

        [Serializable]
        public sealed class T4 : ATuple
        {
            public readonly object _v0;
            public readonly object _v1;
            public readonly object _v2;
            public readonly object _v3;

            public T4(Object v0, Object v1, Object v2, Object v3)
            {
                _v0 = v0;
                _v1 = v1;
                _v2 = v2;
                _v3 = v3;
            }

            public override int count()
            {
                return 4;
            }

            public override Object nth(int i)
            {
                switch (i)
                {
                    case 0:
                        return _v0;
                    case 1:
                        return _v1;
                    case 2:
                        return _v2;
                    case 3:
                        return _v3;
                    default:
                        throw new ArgumentOutOfRangeException("i");
                }
            }

            public override IPersistentVector cons(Object o)
            {
                return create(_v0, _v1, _v2, _v3, o);
            }

            public override bool equiv(Object obj)
            {
                if (this == obj) return true;

                T4 o4 = obj as T4;
                if (o4 != null)
                {
                    return Util.equiv(_v0, o4._v0) &&
                           Util.equiv(_v1, o4._v1) &&
                           Util.equiv(_v2, o4._v2) &&
                           Util.equiv(_v3, o4._v3);
                }
                return base.equiv(obj);
            }
        }

        #endregion

        #region T5

        [Serializable]
        public sealed class T5 : ATuple
        {
            public readonly object _v0;
            public readonly object _v1;
            public readonly object _v2;
            public readonly object _v3;
            public readonly object _v4;

            public T5(Object v0, Object v1, Object v2, Object v3, Object v4)
            {
                _v0 = v0;
                _v1 = v1;
                _v2 = v2;
                _v3 = v3;
                _v4 = v4;
            }

            public override int count()
            {
                return 5;
            }

            public override Object nth(int i)
            {
                switch (i)
                {
                    case 0:
                        return _v0;
                    case 1:
                        return _v1;
                    case 2:
                        return _v2;
                    case 3:
                        return _v3;
                    case 4:
                        return _v4;
                    default:
                        throw new ArgumentOutOfRangeException("i");
                }
            }

            public override IPersistentVector cons(Object o)
            {
                return create(_v0, _v1, _v2, _v3, _v4, o);
            }

            public override bool equiv(Object obj)
            {
                if (this == obj) return true;

                T5 o5 = obj as T5;
                if (o5 != null)
                {
                    return Util.equiv(_v0, o5._v0) &&
                           Util.equiv(_v1, o5._v1) &&
                           Util.equiv(_v2, o5._v2) &&
                           Util.equiv(_v3, o5._v3) &&
                           Util.equiv(_v4, o5._v4);
                }
                return base.equiv(obj);
            }
        }

        #endregion

        #region T6

        [Serializable]
        public sealed class T6 : ATuple
        {
            public readonly object _v0;
            public readonly object _v1;
            public readonly object _v2;
            public readonly object _v3;
            public readonly object _v4;
            public readonly object _v5;

            public T6(Object v0, Object v1, Object v2, Object v3, Object v4, Object v5)
            {
                _v0 = v0;
                _v1 = v1;
                _v2 = v2;
                _v3 = v3;
                _v4 = v4;
                _v5 = v5;
            }

            public override int count()
            {
                return 6;
            }

            public override Object nth(int i)
            {
                switch (i)
                {
                    case 0:
                        return _v0;
                    case 1:
                        return _v1;
                    case 2:
                        return _v2;
                    case 3:
                        return _v3;
                    case 4:
                        return _v4;
                    case 5:
                        return _v5;
                    default:
                        throw new ArgumentOutOfRangeException("i");
                }
            }

            public override IPersistentVector cons(Object o)
            {
                return vec().cons(o);
            }

            public override bool equiv(Object obj)
            {
                if (this == obj) return true;

                T6 o6 = obj as T6;
                if (o6 != null)
                {
                    return Util.equiv(_v0, o6._v0) &&
                           Util.equiv(_v1, o6._v1) &&
                           Util.equiv(_v2, o6._v2) &&
                           Util.equiv(_v3, o6._v3) &&
                           Util.equiv(_v4, o6._v4) &&
                           Util.equiv(_v5, o6._v5);
                }
                return base.equiv(obj);
            }
        }

        #endregion

        #endregion
    }
}