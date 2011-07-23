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


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create", Justification="Compatibility with clojure.core")]
        static public IArraySeq create()
        {
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create", Justification = "Compatibility with clojure.core")]
        static public IArraySeq create(params object[] array)
        {
            return (array == null || array.Length == 0)
                ? null
                : new ArraySeq_object(null,array, 0);
        }

        // Not in the Java version, but I can really use this
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "create", Justification = "Compatibility with clojure.core")]
        static public IArraySeq create(object[] array, int firstIndex)
        {
            return (array == null || array.Length <= firstIndex )
                ? null
                : new ArraySeq_object(null, array, firstIndex);
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
                    return new ArraySeq_bool(null, (bool[])aa, 0);
                case TypeCode.Byte:
                    return new ArraySeq_byte(null, (byte[])aa, 0);
                case TypeCode.Char:
                    return new ArraySeq_char(null, (char[])aa, 0);
                case TypeCode.Decimal:
                    return new ArraySeq_decimal(null, (decimal[])aa, 0);
                case TypeCode.Double:
                    return new ArraySeq_double(null, (double[])aa, 0);
                case TypeCode.Int16:
                    return new ArraySeq_short(null, (short[])aa, 0);
                case TypeCode.Int32:
                    return new ArraySeq_int(null, (int[])aa, 0);
                case TypeCode.Int64:
                    return new ArraySeq_long(null, (long[])aa, 0);
                case TypeCode.SByte:
                    return new ArraySeq_sbyte(null, (sbyte[])aa, 0);
                case TypeCode.Single:
                    return new ArraySeq_float(null, (float[])aa, 0);
                case TypeCode.UInt16:
                    return new ArraySeq_ushort(null, (ushort[])aa, 0);
                case TypeCode.UInt32:
                    return new ArraySeq_uint(null, (uint[])aa, 0);
                case TypeCode.UInt64:
                    return new ArraySeq_ulong(null, (ulong[])aa, 0);
                default:
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
        private readonly Type _ct;

        #endregion

        #region Ctors

        public UntypedArraySeq(object array, int index)
        {
            _a = (Array)array;
            _i = index;
            _ct = array.GetType().GetElementType();
        }

        public UntypedArraySeq(IPersistentMap meta, object array, int index)
            : base(meta)
        {
            _a = (Array)array;
            _i = index;
            _ct = array.GetType().GetElementType();
        }

        #endregion

        #region ISeq members

        public override object first()
        {
            return Reflector.prepRet(_ct,_a.GetValue(_i));
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
            object ret = Reflector.prepRet(_ct,_a.GetValue(_i));
            for (int x = _i + 1; x < _a.Length; x++)
                ret = f.invoke(ret, Reflector.prepRet(_ct,_a.GetValue(x)));
            return ret;
        }

        public object reduce(IFn f, object start)
        {
            object ret = f.invoke(start, Reflector.prepRet(_ct,_a.GetValue(_i)));
            for (int x = _i + 1; x < _a.Length; x++)
                ret = f.invoke(ret, Reflector.prepRet(_ct,_a.GetValue(x)));
            return ret;
        }

        #endregion

        #region IList members

        public override int IndexOf(object value)
        {
            int n = _a.Length;
            for (int j = _i; j < n; j++)
                if (Util.equals(value, Reflector.prepRet(_ct,_a.GetValue(j))))
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
    public abstract class TypedArraySeq<T> : ASeq, IArraySeq
    {
        #region Data

        protected readonly T[] _array;
        protected readonly int _i;
        protected readonly Type _ct;

        #endregion

        #region C-tors

        protected TypedArraySeq(IPersistentMap meta, T[] array, int index)
            : base(meta)
        {
            _array = array;
            _i = index;
            _ct = array.GetType().GetElementType();
        }

        #endregion

        #region Abstract methods

        protected abstract T ConvertNum(object x);
        protected abstract ISeq NextOne();
        protected abstract IObj DuplicateWithMeta(IPersistentMap meta);

        // TODO: first/reduce do a Numbers.num(x) conversion  -- do we need that?

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
            return Reflector.prepRet(_ct,_array[_i]);
        }

        public override ISeq next()
        {
            if (_i + 1 < _array.Length)
                return NextOne();
            return null;
        }

        #endregion

        #region IObj members

        public override IObj withMeta(IPersistentMap meta)
        {
            return DuplicateWithMeta(meta);
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
            object ret = Reflector.prepRet(_ct,_array[_i]);
            for (int x = _i + 1; x < _array.Length; x++)
                ret = f.invoke(ret, Reflector.prepRet(_ct,_array[x]));
            return ret;
        }

        public object reduce(IFn f, object start)
        {
            object ret = f.invoke(start, Reflector.prepRet(_ct, _array[_i]));
            for (int x = _i + 1; x < _array.Length; x++)
                ret = f.invoke(ret, Reflector.prepRet(_ct,_array[x]));
            return ret;
        }

        #endregion

        #region IList members

        public override int IndexOf(object value)
        {
            if (Util.IsNumeric(value))
            {
                T v = ConvertNum(value);
                for (int j = _i; j < _array.Length; j++)
                    if (v.Equals(_array[j]))
                        return j - _i;
            }

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

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",Justification="Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_byte : TypedArraySeq<byte>
    {
        public ArraySeq_byte(IPersistentMap meta, byte[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override byte ConvertNum(object x)
        {
            return Util.ConvertToByte(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_byte(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_byte(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "sbyte")]
    public class ArraySeq_sbyte : TypedArraySeq<sbyte>
    {

        public ArraySeq_sbyte(IPersistentMap meta, sbyte[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override sbyte ConvertNum(object x)
        {
            return Util.ConvertToSByte(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_sbyte(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_sbyte(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_short : TypedArraySeq<short>
    {
        public ArraySeq_short(IPersistentMap meta, short[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override short ConvertNum(object x)
        {
            return Util.ConvertToShort(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_short(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_short(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ushort")]
    public class ArraySeq_ushort : TypedArraySeq<ushort>
    {
        public ArraySeq_ushort(IPersistentMap meta, ushort[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override ushort ConvertNum(object x)
        {
            return Util.ConvertToUShort(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_ushort(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_ushort(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_int : TypedArraySeq<int>
    {
        public ArraySeq_int(IPersistentMap meta, int[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override int ConvertNum(object x)
        {
            return Util.ConvertToInt(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_int(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_int(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "uint")]
    public class ArraySeq_uint : TypedArraySeq<uint>
    {
        public ArraySeq_uint(IPersistentMap meta, uint[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override uint ConvertNum(object x)
        {
            return Util.ConvertToUInt(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_uint(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_uint(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_long : TypedArraySeq<long>
    {
        public ArraySeq_long(IPersistentMap meta, long[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override long ConvertNum(object x)
        {
            return Util.ConvertToLong(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_long(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_long(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ulong")]
    public class ArraySeq_ulong : TypedArraySeq<ulong>
    {
        public ArraySeq_ulong(IPersistentMap meta, ulong[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override ulong ConvertNum(object x)
        {
            return Util.ConvertToULong(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_ulong(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_ulong(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_float : TypedArraySeq<float>
    {
        public ArraySeq_float(IPersistentMap meta, float[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override float ConvertNum(object x)
        {
            return Util.ConvertToFloat(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_float(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_float(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_double : TypedArraySeq<double>
    {
        public ArraySeq_double(IPersistentMap meta, double[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override double ConvertNum(object x)
        {
            return Util.ConvertToDouble(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_double(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_double(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_char : TypedArraySeq<char>
    {
        public ArraySeq_char(IPersistentMap meta, char[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override char ConvertNum(object x)
        {
            return Util.ConvertToChar(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_char(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_char(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_bool : TypedArraySeq<bool>
    {
        public ArraySeq_bool(IPersistentMap meta, bool[] array, int index)
            : base(meta,array,index)
        {
        }

        protected override bool ConvertNum(object x)
        {
            return RT.booleanCast(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_bool(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_bool(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_decimal : TypedArraySeq<decimal>
    {
        public ArraySeq_decimal(IPersistentMap meta, decimal[] array, int index)
            : base(meta, array, index)
        {
        }

        protected override decimal ConvertNum(object x)
        {
            return Util.ConvertToDecimal(x);
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_decimal(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_decimal(meta, _array, _i);
        }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Compatibility with clojure.core")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class ArraySeq_object : TypedArraySeq<object>
    {
        public ArraySeq_object(IPersistentMap meta, object[] array, int index)
            : base(meta, array, index)
        {
        }

        protected override object ConvertNum(object x)
        {
            return x;
        }

        public override int IndexOf(object value)
        {
                for (int j = _i; j < _array.Length; j++)
                    if (value.Equals(_array[j]))
                        return j - _i;
            return -1;
        }

        protected override ISeq NextOne()
        {
            return new ArraySeq_object(_meta, _array, _i + 1);
        }

        protected override IObj DuplicateWithMeta(IPersistentMap meta)
        {
            return new ArraySeq_object(meta, _array, _i);
        }
    }
}
