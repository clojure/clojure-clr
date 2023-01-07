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
//using BigDecimal = java.math.BigDecimal;


namespace clojure.lang
{
    /// <summary>
    /// Represents a rational number.
    /// </summary>
    [Serializable]
    public sealed class Ratio: IComparable, IConvertible
    {
        #region Data

        /// <summary>
        /// The numerator.
        /// </summary>
        private readonly BigInteger _numerator;

        /// <summary>
        /// Get the numerator.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public BigInteger numerator
        {
            get { return _numerator; }
        }

        /// <summary>
        ///  The denominator.
        /// </summary>
        private readonly BigInteger _denominator;

        /// <summary>
        /// Get the denominator.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public BigInteger denominator
        {
            get { return _denominator; }
        } 


        #endregion

        #region C-tors

        /// <summary>
        /// Initialize a Ratio from num/denom.
        /// </summary>
        /// <param name="numerator">The numerator.</param>
        /// <param name="denominator">The denominator.</param>
        public Ratio(BigInteger numerator, BigInteger denominator)
        {
            _numerator = numerator;
            _denominator = denominator;
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// Determines of an object is equal to this object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><value>true</value> if the object is equal to this object; <value>false</value> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is Ratio r && r._numerator.Equals(_numerator) && r._denominator.Equals(_denominator);
        }

        /// <summary>
        /// Gets a hash code for this.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            return _numerator.GetHashCode() ^ _denominator.GetHashCode();
        }

        /// <summary>
        /// Returns a string representing the ratio.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString()
        {
            return _numerator.ToString() + "/" + _denominator.ToString();
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return Numbers.compare(this, obj);
        }

        #endregion

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return ! _numerator.Equals(BigInteger.Zero);
        }

        public byte ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(ToDouble(provider));
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(ToDouble(provider));
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(ToDouble(provider));
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            //return Convert.ToDecimal(ToDouble(provider));
            return ToBigDecimal(BigDecimal.Context.Decimal128).ToDecimal(provider);
        }

        public double ToDouble(IFormatProvider provider)
        {
            //return _numerator.ToDouble(provider) / _denominator.ToDouble(provider);
            return ToBigDecimal(BigDecimal.Context.Decimal64).ToDouble(provider);
        }

        public short ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(ToDouble(provider));
        }

        public int ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(ToDouble(provider));
        }

        public long ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(BigIntegerValue());
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(ToDouble(provider));
        }

        public float ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(ToDouble(provider));
        }

        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(ToDouble(provider), conversionType);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(ToDouble(provider));
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(ToDouble(provider));
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(ToDouble(provider));
        }


        public BigDecimal ToBigDecimal()
        {
            BigDecimal numerator = BigDecimal.Create(this.numerator);
            BigDecimal denominator = BigDecimal.Create(this.denominator);
            return numerator.Divide(denominator);
        }

        public BigDecimal ToBigDecimal(BigDecimal.Context c)
        {
            BigDecimal numerator = BigDecimal.Create(this.numerator);
            BigDecimal denominator = BigDecimal.Create(this.denominator);
            return numerator.Divide(denominator,c);
        }

        #endregion

        #region Other

        public BigInteger BigIntegerValue()
        {
            return _numerator.Divide(_denominator);
        }

        #endregion
    }
}
