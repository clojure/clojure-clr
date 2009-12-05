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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{

    public class ArrayStreamBase<T> : AFn
    {
        #region Data

        int _i = 0;
        readonly T[] _array;

        #endregion

        #region C-tors

        public ArrayStreamBase(T[] array)
        {
            _array = array;
        }

        #endregion

        #region Implementation

        public override object invoke()
        {
            if (_i < _array.Length)
                return _array[_i++];
            return RT.EOS;
        }
        #endregion
    }

    public class ArrayStream : ArrayStreamBase<Object>
    {

        #region C-tors & factory methods

        public ArrayStream(Object[] array)
            : base(array)
        {
        }

        public static Stream createFromObject(object array)
        {
            Type aType = array.GetType();
            if (!aType.IsArray)
                throw new ArgumentException("Must be an array");

            if (((Array)array).Rank != 1)
                throw new ArgumentException("Array must have rank 1");

            Type eType = aType.GetElementType();

            if (!eType.IsPrimitive)
                return new Stream(new ArrayStream((Object[])array));

            switch (Type.GetTypeCode(eType))
            {
                case TypeCode.Char:
                    return new Stream(new ArrayStreamBase<char>((char[])array));
                case TypeCode.SByte:
                    return new Stream(new ArrayStreamBase<sbyte>((sbyte[])array));
                case TypeCode.Byte:
                    return new Stream(new ArrayStreamBase<byte>((byte[])array));
                case TypeCode.Int16:
                    return new Stream(new ArrayStreamBase<short>((short[])array));
                case TypeCode.Int32:
                    return new Stream(new ArrayStreamBase<int>((int[])array));
                case TypeCode.Int64:
                    return new Stream(new ArrayStreamBase<long>((long[])array));
                case TypeCode.Double:
                    return new Stream(new ArrayStreamBase<double>((double[])array));
                case TypeCode.Single:
                    return new Stream(new ArrayStreamBase<float>((float[])array));
                case TypeCode.UInt16:
                    return new Stream(new ArrayStreamBase<ushort>((ushort[])array));
                case TypeCode.UInt32:
                    return new Stream(new ArrayStreamBase<uint>((uint[])array));
                case TypeCode.UInt64:
                    return new Stream(new ArrayStreamBase<ulong>((ulong[])array));
                case TypeCode.Decimal:
                    return new Stream(new ArrayStreamBase<decimal>((decimal[])array));
                case TypeCode.Boolean:
                    return new Stream(new ArrayStreamBase<bool>((bool[])array));

            }

            throw new ArgumentException(String.Format("Unsupported array type %s", array.GetType()));
        }

        #endregion

    }
}
