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

    public class BigInt
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


        public static readonly BigInt ZERO = new BigInt(0, null);
        public static readonly BigInt ONE = new BigInt(1, null);

        #endregion

        #region Ctors and factory methods

        private BigInt(long lpart, BigInteger bipart)
        {
            _lpart = lpart;
            _bipart = bipart;
        }

        public static BigInt fromBigInteger(BigInteger val)
        {
            long n;
            if (val.AsInt64(out n))
                return new BigInt(n, null);
            return new BigInt(0, val);
        }

        public static BigInt fromLong(long val)
        {
            return new BigInt(val, null);
        }

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
            if (obj is BigInt)
            {
                BigInt o = (BigInt)obj;
                if (_bipart == null)
                    return o._bipart == null && _lpart == o._lpart;
                return o._bipart != null && _bipart.Equals(o._bipart);
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
            if ( _bipart == null )
                return _lpart.ToString();
            return _bipart.ToString();
        }

        #endregion

        #region Conversions

        public BigInteger toBigInteger()
        {
            if (_bipart == null)
                return BigInteger.Create(_lpart);
            else
                return _bipart;
        }

        public int intValue()
        {
            if (_bipart == null)
                return (int)_lpart;
            else
                return _bipart.ToInt32();
        }
        
        public long longValue()
        {
            if (_bipart == null)
                return _lpart;
            else
                return _bipart.ToInt64();
        }

        public float floatValue()
        {
            if (_bipart == null)
                return _lpart;
            else
                return _bipart.ToSingle(null);
        }

        public double doubleValue()
        {
            if (_bipart == null)
                return _lpart;
            else
                return _bipart.ToDouble(null);
        }

        public byte byteValue()
        {
            if (_bipart == null)
                return (byte)_lpart;
            else
                return _bipart.ToByte(null);
        }

        public short shortValue()
        {
            if (_bipart == null)
                return (short)_lpart;
            else
                return _bipart.ToInt16(null);
        }

        #endregion
    }
}
