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

namespace clojure.lang
{
    public interface IArraySeq : IObj, ISeq, IList, IndexedSeq, IReduce
    {
        object[] ToArray();
        object Array();
        int Index();
    }

    public static class ArraySeq
    {
        #region C-tors and factory methods

        static public IArraySeq create()
        {
            return null;
        }

        static public IArraySeq create(params object[] array)
        {
            return (array == null || array.Length == 0)
                ? null
                : new TypedArraySeq<Object>(null,array, 0);
        }

        // Not in the Java version, but I can really use this
        static public IArraySeq create(object[] array, int firstIndex)
        {
            return (array == null || array.Length <= firstIndex )
                ? null
                : new TypedArraySeq<Object>(null,array, firstIndex);
        }

        internal static IArraySeq createFromObject(Object array)
        {
            Array aa = (Array)array;

            if (array == null || aa.Length == 0)
                return null;

            Type elementType = array.GetType().GetElementType();
            switch (Type.GetTypeCode(elementType))
            {
                case TypeCode.Boolean:
                    return new TypedArraySeq<bool>(null, (bool[])aa, 0);
                case TypeCode.Byte:
                    return new TypedArraySeq<byte>(null, (byte[])aa, 0);
                case TypeCode.Char:
                    return new TypedArraySeq<char>(null, (char[])aa, 0);
                case TypeCode.Decimal:
                    return new TypedArraySeq<decimal>(null, (decimal[])aa, 0);
                case TypeCode.Double:
                    return new TypedArraySeq<double>(null, (double[])aa, 0);
                case TypeCode.Int16:
                    return new TypedArraySeq<short>(null, (short[])aa, 0);
                case TypeCode.Int32:
                    return new TypedArraySeq<int>(null, (int[])aa, 0);
                case TypeCode.Int64:
                    return new TypedArraySeq<long>(null, (long[])aa, 0);
                case TypeCode.SByte:
                    return new TypedArraySeq<sbyte>(null, (sbyte[])aa, 0);
                case TypeCode.Single:
                    return new TypedArraySeq<float>(null, (float[])aa, 0);
                case TypeCode.UInt16:
                    return new TypedArraySeq<ushort>(null, (ushort[])aa, 0);
                case TypeCode.UInt32:
                    return new TypedArraySeq<uint>(null, (uint[])aa, 0);
                case TypeCode.UInt64:
                    return new TypedArraySeq<ulong>(null, (ulong[])aa, 0);
                default:
                    if (elementType == typeof(object))
                        return new TypedArraySeq<Object>(null, (object[])aa, 0);
                    else
                        return new UntypedArraySeq(array, 0);
            }
        }

        #endregion
    }

    [Serializable]
    public class UntypedArraySeq : ASeq, IArraySeq
    {
        #region Data

        private readonly Array _a;
        private readonly int _i;

        #endregion

        #region Ctors

        public UntypedArraySeq(object array, int i)
        {
            _a = (Array)array;
            _i = i;
        }

        public UntypedArraySeq(IPersistentMap meta, object array, int i)
            : base(meta)
        {
            _a = (Array)array;
            _i = i;
        }

        #endregion

        #region ISeq members

        public override object first()
        {
            return Reflector.prepRet(_a.GetValue(_i));
        }

        public override ISeq next()
        {
            if (_i + 1 < _a.Length)
                return new UntypedArraySeq(_a, _i + 1);
            return null;
        }

        #endregion

        #region IPersistentCollection members

        public override int count()
        {
            return _a.Length - _i;
        }

        #endregion

        #region IObj members

        public override IObj withMeta(IPersistentMap meta)
        {
            return new UntypedArraySeq(meta, _a, _i);
        }

        #endregion

        #region IndexedSeq Members

        public int index()
        {
            return _i;
        }

        #endregion

        #region IReduce Members

        public object reduce(IFn f)
        {
            object ret = RT.prepRet(_a.GetValue(_i));
            for (int x = _i + 1; x < _a.Length; x++)
                ret = f.invoke(ret, RT.prepRet(_a.GetValue(x)));
            return ret;
        }

        public object reduce(IFn f, object start)
        {
            object ret = f.invoke(start, RT.prepRet(_a.GetValue(_i)));
            for (int x = _i + 1; x < _a.Length; x++)
                ret = f.invoke(ret, RT.prepRet(_a.GetValue(x)));
            return ret;
        }

        #endregion

        #region IList members

        public override int IndexOf(object value)
        {
            int n = _a.Length;
            for (int j = _i; j < n; j++)
                if (Util.equals(value, Reflector.prepRet(_a.GetValue(j))))
                    return j - _i;
            return -1;
        }

        #endregion

        #region IArraySeq members

        public object[] ToArray()
        {
              object[] items = new object[_a.Length];
              for (int i = 0; i < _a.Length; i++)
                  items[i] = _a.GetValue(i);
                return items;
        }

        public object Array()
        {
            return _a;
        }

        public int Index()
        {
            return _i;
        }

        #endregion
    }

    [Serializable]
    public class TypedArraySeq<T> : ASeq, IArraySeq
    {
        #region Data

        readonly T[] _array;
        readonly int _i;

        #endregion

        #region C-tors

        public TypedArraySeq(IPersistentMap meta, T[] array, int i)
            : base(meta)
        {
            _array = array;
            _i = i;
        }

        #endregion

        #region IPersistentCollection members

        public override int count()
        {
            return _array.Length - _i;
        }

        IPersistentCollection IPersistentCollection.cons(object o)
        {
            return cons(o);
        }

        #endregion

        #region ISeq members

        public override object first()
        {
            return _array[_i];
        }

        public override ISeq next()
        {
            if (_i + 1 < _array.Length)
                return new TypedArraySeq<T>(meta(), _array, _i + 1);
            return null;
        }

        #endregion

        #region IObj members

        public override IObj withMeta(IPersistentMap meta)
        {
            return new TypedArraySeq<T>(meta, _array, _i);
        }

        #endregion

        #region IndexedSeq Members

        public int index()
        {
            return _i;
        }

        #endregion

        #region IReduce Members

        public object reduce(IFn f)
        {
            object ret = _array[_i];
            for (int x = _i + 1; x < _array.Length; x++)
                ret = f.invoke(ret, _array[x]);
            return ret;
        }

        public object reduce(IFn f, object start)
        {
            object ret = f.invoke(start,_array[_i]);
            for (int x = _i + 1; x < _array.Length; x++)
                ret = f.invoke(ret, _array[x]);
            return ret;
        }

        #endregion

        #region IList members

        public override int IndexOf(object value)
        {
            for (int j = _i; j < _array.Length; j++)
                if (value.Equals(_array[j]))
                    return j - _i;

            return -1;
        }
      
        #endregion

        #region IArraySeq members

        public object[] ToArray()
        {
            if (typeof(T) == typeof(object))
                return (object[])(object)_array;

            object[] items = new object[_array.Length];
            for (int i = 0; i < _array.Length; i++)
                items[i] = _array[i];
            return items;
        }

        public object Array()
        {
            return _array;
        }

        public int Index()
        {
            return _i;
        }

        #endregion
    }

}
