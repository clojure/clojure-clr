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

namespace clojure.lang
{
    // Copying JVM's clojure.lang.BigInt class as closely as possible

    public class BigInt : IConvertible, IHashEq
    {
        #region Data

        readonly long _lpart;

        public long Lpart
        {
            get { return _lpart; }
        }

        readonly BigInteger _bipart;

        public BigInteger Bipart
        {
            get { return _bipart; }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ZERO")]
        public static readonly BigInt ZERO = new BigInt(0, null);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ONE")]
        public static readonly BigInt ONE = new BigInt(1, null);

        #endregion

        #region Ctors and factory methods

        private BigInt(long lpart, BigInteger bipart)
        {
            _lpart = lpart;
            _bipart = bipart;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "from")]
        public static BigInt fromBigInteger(BigInteger val)
        {
            long n;
            if (val.AsInt64(out n))
                return new BigInt(n, null);
            return new BigInt(0, val);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "from")]
        public static BigInt fromLong(long val)
        {
            return new BigInt(val, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "value")]
        public static BigInt valueOf(long val)
        {
            return new BigInt(val, null);
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            BigInt bi = obj as BigInt;

            if (bi != null)
            {
                if (_bipart == null)
                    return bi._bipart == null && _lpart == bi._lpart;
                return bi._bipart != null && _bipart.Equals(bi._bipart);
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (_bipart == null)
                return _lpart.GetHashCode();

            return _bipart.GetHashCode();
        }

        public override string ToString()
        {
            if (_bipart == null)
                return _lpart.ToString();
            return _bipart.ToString();
        }

        #endregion

        #region Attempted conversion methods

        /// <summary>
        /// Try to convert to an Int32.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsInt32(out int ret)
        {
            ret = 0;
            if (_bipart != null)
                return false;
            if (_lpart < int.MinValue || _lpart > int.MaxValue)
                return false;

            ret = (int)_lpart;
            return true;
        }

        /// <summary>
        /// Try to convert to an Int64.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsInt64(out long ret)
        {
            ret = 0;
            if (_bipart != null)
                return false;

            ret = _lpart;
            return true;
        }

        /// <summary>
        /// Try to convert to an UInt32.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsUInt32(out uint ret)
        {
            ret = 0;
            if (_bipart != null)
                return false;
            if (_lpart < uint.MinValue || _lpart > uint.MaxValue)
                return false;

            ret = (uint)_lpart;
            return true;
        }

        /// <summary>
        /// Try to convert to an UInt64.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsUInt64(out ulong ret)
        {
            ret = 0;
            if (_bipart != null)
                return _bipart.AsUInt64(out ret);
            if (_lpart < 0)
                return false;

            ret = (ulong)_lpart;
            return true;
        }

        /// <summary>
        /// Try to convert to a Decimal.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsDecimal(out Decimal ret)
        {
            ret = ToDecimal(null);
            return true;
        }

        #endregion

        #region Conversions

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "to")]
        public BigInteger toBigInteger()
        {
            if (_bipart == null)
                return BigInteger.Create(_lpart);
            else
                return _bipart;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "int")]
        public int intValue()
        {
            if (_bipart == null)
                return (int)_lpart;
            else
                return _bipart.ToInt32();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "long")]
        public long longValue()
        {
            if (_bipart == null)
                return _lpart;
            else
                return _bipart.ToInt64();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "float")]
        public float floatValue()
        {
            if (_bipart == null)
                return _lpart;
            else
                return _bipart.ToSingle(null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "double")]
        public double doubleValue()
        {
            if (_bipart == null)
                return _lpart;
            else
                return _bipart.ToDouble(null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "byte")]
        public byte byteValue()
        {
            if (_bipart == null)
                return (byte)_lpart;
            else
                return _bipart.ToByte(null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "short")]
        public short shortValue()
        {
            if (_bipart == null)
                return (short)_lpart;
            else
                return _bipart.ToInt16(null);
        }

        #endregion

        #region Conversion operators (to BigInt)

        /// <summary>
        /// Implicitly convert from byte to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(byte v)
        {
            return fromLong(v);
        }

        /// <summary>
        /// Implicitly convert from sbyte to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(sbyte v)
        {
            return fromLong(v);
        }

        /// <summary>
        /// Implicitly convert from short to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(short v)
        {
            return fromLong(v);
        }

        /// <summary>
        /// Implicitly convert from ushort to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(ushort v)
        {
            return fromLong(v);
        }

        /// <summary>
        /// Implicitly convert from uint to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(uint v)
        {
            return fromLong(v);
        }

        /// <summary>
        /// Implicitly convert from int to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(int v)
        {
            return fromLong(v);
        }

        /// <summary>
        /// Implicitly convert from ulong to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(ulong v)
        {
            if (v > long.MaxValue)
                return fromBigInteger(BigInteger.Create(v));
            return fromLong((long)v);
        }

        /// <summary>
        /// Implicitly convert from long to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(long v)
        {
            return fromLong(v);
        }

        /// <summary>
        /// Implicitly convert from decimal to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static implicit operator BigInt(decimal v)
        {
            return fromBigInteger(BigInteger.Create(v));
        }

        /// <summary>
        /// Explicitly convert from double to <see cref="BigInt"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInt"/></returns>
        public static explicit operator BigInt(double v)
        {
            return fromBigInteger(BigInteger.Create(v));
        }

        #endregion

        #region Conversion operators (from BigInt)

        /// <summary>
        /// Implicitly convert from <see cref="BigInt"/> to double.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent double</returns>
        public static explicit operator double(BigInt i)
        {
            return i.ToDouble(null);
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to byte.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent byte</returns>
        public static explicit operator byte(BigInt self)
        {
            int tmp;
            if (self.AsInt32(out tmp))
            {
                return checked((byte)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to sbyte.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent sbyte</returns>
        public static explicit operator sbyte(BigInt self)
        {
            int tmp;
            if (self.AsInt32(out tmp))
            {
                return checked((sbyte)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to UInt16.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent UInt16</returns>
        public static explicit operator UInt16(BigInt self)
        {
            int tmp;
            if (self.AsInt32(out tmp))
            {
                return checked((UInt16)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to Int16.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent Int16</returns>
        public static explicit operator Int16(BigInt self)
        {
            int tmp;
            if (self.AsInt32(out tmp))
            {
                return checked((Int16)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to UInt32.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent UInt32</returns>
        public static explicit operator UInt32(BigInt self)
        {
            uint tmp;
            if (self.AsUInt32(out tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to Int32.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent Int32</returns>
        public static explicit operator Int32(BigInt self)
        {
            int tmp;
            if (self.AsInt32(out tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to Int64.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent Int64</returns>
        public static explicit operator Int64(BigInt self)
        {
            long tmp;
            if (self.AsInt64(out tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to UInt64.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent UInt64</returns>
        public static explicit operator UInt64(BigInt self)
        {
            ulong tmp;
            if (self.AsUInt64(out tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to float.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent float</returns>
        public static explicit operator float(BigInt self)
        {
            return checked((float)self.ToDouble(null));
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInt"/> to double.
        /// </summary>
        /// <param name="i">The <see cref="BigInt"/> to convert</param>
        /// <returns>The equivalent decimal</returns>
        public static explicit operator decimal(BigInt self)
        {
            decimal res;
            if (self.AsDecimal(out res))
            {
                return res;
            }
            throw new OverflowException();
        }

        #endregion

        #region IConvertible methods

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return _bipart == null ? _lpart != 0 : _bipart.ToBoolean(provider);
        }

        public byte ToByte(IFormatProvider provider)
        {
            return _bipart == null ? (byte)_lpart : _bipart.ToByte(provider);
        }

        public char ToChar(IFormatProvider provider)
        {
            return _bipart == null ? (char)_lpart : _bipart.ToChar(provider);
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return _bipart == null ? (decimal)_lpart : _bipart.ToDecimal(provider);
        }

        public double ToDouble(IFormatProvider provider)
        {
            return _bipart == null ? (double)_lpart : _bipart.ToDouble(provider);
        }

        public short ToInt16(IFormatProvider provider)
        {
            return _bipart == null ? (short)_lpart : _bipart.ToInt16(provider);
        }

        public int ToInt32(IFormatProvider provider)
        {
            return _bipart == null ? (int)_lpart : _bipart.ToInt32(provider);
        }

        public long ToInt64(IFormatProvider provider)
        {
            return _bipart == null ? (long)_lpart : _bipart.ToInt64(provider);
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return _bipart == null ? (sbyte)_lpart : _bipart.ToSByte(provider);
        }

        public float ToSingle(IFormatProvider provider)
        {
            return _bipart == null ? (float)_lpart : _bipart.ToSingle(provider);
        }

        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(BigInt))
                return this;
            else if (conversionType == typeof(BigInteger))
                return toBigInteger();
            throw new InvalidCastException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return _bipart == null ? (ushort)_lpart : _bipart.ToUInt16(provider);
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return _bipart == null ? (uint)_lpart : _bipart.ToUInt32(provider);
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return _bipart == null ? (ulong)_lpart : _bipart.ToUInt64(provider);
        }

        public BigDecimal ToBigDecimal()
        {
            return _bipart == null ? BigDecimal.Create(_lpart) : BigDecimal.Create(_bipart);
        }

        #endregion

        #region Arithmetic operations

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "add")]
        public BigInt add(BigInt y)
        {
            if ((_bipart == null) && (y._bipart == null))
            {
                long ret = _lpart + y._lpart;
                if ((ret ^ _lpart) >= 0 || (ret ^ y._lpart) >= 0)
                    return BigInt.valueOf(ret);
            }
            return BigInt.fromBigInteger(this.toBigInteger().Add(y.toBigInteger()));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "multiply")]
        public BigInt multiply(BigInt y)
        {
            if ((_bipart == null) && (y._bipart == null))
            {
                long ret = _lpart * y._lpart;
                if (y._lpart == 0 
                    || (_lpart != Int64.MinValue && unchecked(ret / y._lpart) == _lpart ))
                    return BigInt.valueOf(ret);
            }
            return BigInt.fromBigInteger(this.toBigInteger().Multiply(y.toBigInteger()));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "quotient")]
        public BigInt quotient(BigInt y)
        {
            if ((_bipart == null) && (y._bipart == null))
            {
                if (_lpart == Int64.MinValue && y._lpart == -1)
                    return BigInt.fromBigInteger(this.toBigInteger().Negate());
                return BigInt.valueOf(_lpart / y._lpart);
            }
            return BigInt.fromBigInteger(this.toBigInteger().Divide(y.toBigInteger()));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "remainder")]
        public BigInt remainder(BigInt y)
        {
            if ((_bipart == null) && (y._bipart == null))
            {
                return BigInt.valueOf(_lpart % y._lpart);
            }
            return BigInt.fromBigInteger(this.toBigInteger().Mod(y.toBigInteger()));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "lt")]
        public bool lt(BigInt y)
        {
            if ((_bipart == null) && (y._bipart == null))
            {
                return _lpart < y._lpart;
            }
            return this.toBigInteger().CompareTo(y.toBigInteger()) < 0;
        }

        #endregion

        #region IHashEq

        public int hasheq()
        {
            if (_bipart == null)
                return Murmur3.HashLong(_lpart);

            return _bipart.GetHashCode();
        }

        #endregion
    }
}
