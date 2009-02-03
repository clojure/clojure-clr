using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using java.math;

namespace clojure.lang
{
    /// <summary>
    /// Represents a rational number.
    /// </summary>
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
        public BigInteger Numerator
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
        public BigInteger Denominator
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
            Ratio r = obj as Ratio;
            return r != null && r._numerator.Equals(_numerator) && r._denominator.Equals(_denominator);
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
            return ! _numerator.Equals(Numbers.BigIntegerZero);
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
            return Convert.ToDecimal(ToDouble(provider));
        }

        public double ToDouble(IFormatProvider provider)
        {
            return _numerator.ToDouble(provider) / _denominator.ToDouble(provider);
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
            return Convert.ToInt64(ToDouble(provider));
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

        #endregion
    }
}
