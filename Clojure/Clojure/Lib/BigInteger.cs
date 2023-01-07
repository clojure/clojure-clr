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
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Extended precision integer.
    /// </summary>
    /// <remarks>
    /// <para>Inspired by the Microsoft.Scripting.Math.BigInteger code, 
    /// the java.math.BigInteger code, but mostly by Don Knuth's Art of Computer Programming, Volume 2.</para>
    /// <para>The same as most other BigInteger representations, this implementation uses a sign/magnitude representation.</para>
    /// <para>The magnitude is represented by an array of uints in big-endian order.  Noted in passing: the CLR implementation is little-endian uint[].  
    /// The Java implementation is int[] big-endian.</para>
    /// <para>BigIntegers are immutable.</para>
    /// </remarks>
    [Serializable]
    public class BigInteger: IComparable, IConvertible, IEquatable<BigInteger>
    {
        #region Data

        /// <summary>
        /// The number of bits in one 'digit' of the magnitude.
        /// </summary>
        protected const int BitsPerDigit = 32; // uint implementation

        /// <summary>
        /// The sign of the integer.  Must be -1, 0, +1.
        /// </summary>
        private readonly short _sign;

        /// <summary>
        /// The magnitude of the integer (big-endian).
        /// </summary>
        /// <remarks>
        /// <para>Big-endian = _data[0] is the most significant digit.</para>
        /// <para> Some invariants:</para>
        /// <list>
        /// <item>If the integer is zero, then _data must be length zero array and _sign must be zero.</item>
        /// <item>No leading zero uints.</item>
        /// <item>Must be non-null.  For zero, a zero-length array is used.</item>
        /// </list>
        /// These invariants imply a unique representation for every value.  
        /// They also force us to get rid of leading zeros after every operation that might create some.
        /// </remarks>
        private readonly uint[] _data;

        private static readonly BigInteger _zero = new BigInteger(0, Array.Empty<uint>());
        private static readonly BigInteger _one = new BigInteger(1, new uint[] { 1 });
        private static readonly BigInteger _two = new BigInteger(1, new uint[] { 2 });
        private static readonly BigInteger _five = new BigInteger(1, new uint[] { 5 });
        private static readonly BigInteger _ten = new BigInteger(1, new uint[] { 10 });
        private static readonly BigInteger _negativeOne = new BigInteger(-1, new uint[] { 1 });

        /// <summary>
        /// Zero
        /// </summary>
        public static BigInteger Zero { get {return _zero;} }

        /// <summary>
        /// One
        /// </summary>
        public static BigInteger One { get { return _one; } }

        /// <summary>
        /// Two
        /// </summary>
        public static BigInteger Two { get { return _two; } }

        /// <summary>
        /// Five
        /// </summary>
        public static BigInteger Five { get { return _five; } }

        /// <summary>
        /// Ten
        /// </summary>
        public static BigInteger Ten { get { return _ten; } }

        /// <summary>
        /// -1
        /// </summary>
        public static BigInteger NegativeOne { get { return _negativeOne; } }
     
        #endregion

        #region Factory methods

        /// <summary>
        /// Create a <see cref="BigInteger"/> from an unsigned long value. 
        /// </summary>
        /// <param name="v">The value</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        public static BigInteger Create(ulong v)
        {
            if (v == 0)
                return Zero;

            uint most = (uint)(v >> BitsPerDigit);
            if (most == 0)
                return new BigInteger(1, (uint)v);
            else
                return new BigInteger(1, most, (uint)v);
        }

        /// <summary>
        /// Create a <see cref="BigInteger"/> from an unsigned int value.
        /// </summary>
        /// <param name="v">The value</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        public static BigInteger Create(uint v)
        {
            if (v == 0)
                return Zero;
            else return new BigInteger(1, v);
        }

        /// <summary>
        /// Create a <see cref="BigInteger"/> from an (signed) long value.
        /// </summary>
        /// <param name="v">The value</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        public static BigInteger Create(long v)
        {
            if (v == 0)
                return Zero;
            else
            {
                short sign = 1;
                if (v < 0)
                {
                    sign = -1;
                    v = -v;
                }

                uint most = (uint)(v >> BitsPerDigit);
                if (most == 0)
                    return new BigInteger(sign, (uint)v);
                else
                    return new BigInteger(sign, most, (uint)v);
            }
        }

        /// <summary>
        /// Create a <see cref="BigInteger"/> from an (signed) int value.
        /// </summary>
        /// <param name="v">The value</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        public static BigInteger Create(int v)
        {
            return Create((long)v);
        }

        /// <summary>
        /// Create a <see cref="BigInteger"/> from a decimal value.
        /// </summary>
        /// <param name="v">The value</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        public static BigInteger Create(decimal v)
        {
            if ( v == 0 )
                return Zero;

            decimal t = Decimal.Truncate(v);
            int[] bits = Decimal.GetBits(t);

            // I could avoid creating the extra array, but do I care?

            uint[] data = new uint[3];
            data[0] = (uint)bits[2];
            data[1] = (uint)bits[1];
            data[2] = (uint)bits[0];

            return new BigInteger(v > 0 ? 1 : -1, RemoveLeadingZeros(data));
        }

        /// <summary>
        /// Create a <see cref="BigInteger"/> from a double value.
        /// </summary>
        /// <param name="v">The value</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        public static BigInteger Create(double v)
        {
            if (Double.IsNaN(v) || Double.IsInfinity(v))
            {
                throw new OverflowException();
            }

            byte[] dbytes = System.BitConverter.GetBytes(v);
            ulong significand = GetDoubleSignificand(dbytes);
            int exp = GetDoubleBiasedExponent(dbytes);

            if (significand == 0)
            {
                if (exp == 0)
                    return Zero;

                BigInteger result = v < 0.0 ? NegativeOne : One;
                // TODO: Avoid extra allocation
                result = result.LeftShift(exp - DoubleExponentBias);
                return result;
            }
            else
            {
                significand |= 0x10000000000000ul;
                BigInteger res = BigInteger.Create(significand);
                // TODO: Avoid extra allocation
                res = exp > 1075 ? res << (exp - DoubleShiftBias) : res >> (DoubleShiftBias - exp);
                return v < 0.0 ? res * (-1) : res;
            }
        }

        #endregion

        #region C-tors

        /// <summary>
        /// Create a copy of a <see cref="BigInteger"/>
        /// </summary>
        /// <param name="copy">The <see cref="BigInteger"/> to copy</param>
        public BigInteger(BigInteger copy)
        {
            _sign = copy._sign;
            _data = copy._data;
        }

        /// <summary>
        /// Create a BigInteger from sign/magnitude data.
        /// </summary>
        /// <param name="sign">The sign (-1, 0, +1)</param>
        /// <param name="data">The magnitude (big-endian)</param>
        /// <exception cref="System.ArgumentException">Thrown when the sign is not one of -1, 0, +1, 
        /// or if a zero sign is given on a non-empty magnitude.</exception>
        /// <remarks>
        /// <para>Leading zero (uint) digits will be removed.</para>
        /// <para>The sign will be set to zero if a zero-length array is passed.</para>
        /// </remarks>
        public BigInteger(int sign, params uint[] data)
        {
            if (sign < -1 || sign > 1)
                throw new ArgumentException("Sign must be -1, 0 +1");

            data = RemoveLeadingZeros(data);

            if (data.Length == 0)
                sign = 0;
            else if (sign == 0)
                throw new ArgumentException("Zero sign on non-zero data");

            _sign = (short)sign;
            _data = data;
        }

        #endregion

        #region Radix conversion
        
        /// <summary>
        /// Create a <see cref="BigInteger"/> from a string representation (radix 10).
        /// </summary>
        /// <param name="x">The string to convert</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        /// <exception cref="System.FormatException">Thrown if there is a bad minus sign (more than one or not leading)
        /// or if one of the digits in the string is noat valid for the given radix.</exception>
        public static BigInteger Parse(string x)
        {
            return Parse(x, 10);
        }

        /// <summary>
        /// Create a <see cref="BigInteger"/> from a string representation in the given radix.
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <param name="radix">The radix of the numeric representation</param>
        /// <returns>A <see cref="BigInteger"/></returns>
        /// <exception cref="System.FormatException">Thrown if there is a bad minus sign (more than one or not leading)
        /// or if one of the digits in the string is noat valid for the given radix.</exception>
        public static BigInteger Parse(string s, int radix)
        {
            if (TryParse(s, radix, out BigInteger v))
                return v;
            throw new FormatException();
        }

        /// <summary>
        /// Try to create a <see cref="BigInteger"/> from a string representation (radix 10)
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <param name="v">Set to the  <see cref="BigInteger"/> corresponding to the string, if possible; set to null otherwise</param>
        /// <returns><c>True</c> if the string is parsed successfully; <c>false</c> otherwise</returns>
        public static bool TryParse(string s, out BigInteger v)
        {
            return TryParse(s, 10, out v);
        }


        /// <summary>
        /// The minimum radix allowed in parsing.
        /// </summary>
        public const int MinRadix = 2;

        /// <summary>
        /// The maximum radix allowed in parsing.
        /// </summary>
        public const int MaxRadix = 36;

        /// <summary>
        /// Try to create a <see cref="BigInteger"/> from a string representation in the given radix)
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <param name="radix">The radix of the numeric representation</param>
        /// <param name="v">Set to the  <see cref="BigInteger"/> corresponding to the string, if possible; set to null otherwise</param>
        /// <returns><c>True</c> if the string is parsed successfully; <c>false</c> otherwise</returns>
        /// <remarks>
        /// <para>This is pretty much the same algorithm as in the Java implementation.
        /// That's pretty much what is in Knuth ACPv2ed3, Sec. 4.4, Method 1b.
        /// That's pretty much what you'd do by hand.</para>  
        /// <para>The only enhancement is that instead of doing one digit at a time, you translate a group of contiguous
        /// digits into a uint, then do a multiply by the radix and add of the uint.
        /// The size of each group of digits is the maximum number of digits in the radix
        /// that will fit into a uint.</para>
        /// <para>Once you've decided to make that enhancement to Knuth's algoorithm, you pretty much
        /// end up with the Java version's code.</para>
        /// </remarks>
        public static bool TryParse(string s, int radix, out BigInteger v)
        {
            if (radix < MinRadix || radix > MaxRadix)
            {
                v = null;
                return false;
            }

            short sign = 1;
            int len = s.Length;

            // zero length bad, 
            // hyphen only bad, plus only bad,
            // hyphen not leading bad, plus not leading bad
            // (overkill) both hyphen and minus present (one would be caught by the tests above)
            int minusIndex = s.LastIndexOf('-');
            int plusIndex = s.LastIndexOf('+');
            if (len == 0
                || (minusIndex == 0 && len == 1)
                || (plusIndex == 0 && len == 1)
                || (minusIndex > 0)
                || (plusIndex > 0))
            {
                v = null;
                return false;
            }

            int index = 0;
            if (plusIndex != -1)
                index = 1;
            else if (minusIndex != -1)
            {
                sign = -1;
                index = 1;
            }

            // skip leading zeros
            while (index < len && s[index] == '0')
                index++;

            if (index == len)
            {
                v = new BigInteger(Zero);
                return true;
            }

            int numDigits = len - index;

            // We can compute size of magnitude.  May be too large by one uint.
            int numBits = ((numDigits * BitsPerRadixDigit[radix]) >> 10) + 1;
            int numUints = (numBits + BitsPerDigit - 1) / BitsPerDigit;

            uint[] data = new uint[numUints];

            int groupSize = RadixDigitsPerDigit[radix];

            // the first group may be short
            // the first group is the initial value for _data.

            int firstGroupLen = numDigits % groupSize;
            if (firstGroupLen == 0)
                firstGroupLen = groupSize;
            if (!TryParseUInt(s, index, firstGroupLen, radix, out data[data.Length - 1]))
            {
                v = null;
                return false;
            }

            index += firstGroupLen;

            uint mult = SuperRadix[radix];
            for (; index < len; index += groupSize)
            {
                if (!TryParseUInt(s, index, groupSize, radix, out uint u))
                {
                    v = null;
                    return false;
                }
                InPlaceMulAdd(data, mult, u);
            }

            v = new BigInteger(sign,RemoveLeadingZeros(data));
            return true;
        }


        /// <summary>
        /// Converts the numeric value of this <see cref="BigInteger"/> to its string representation in radix 10.
        /// </summary>
        /// <returns>The string representation in radix 10</returns>
        public override string ToString()
        {
            return ToString(10);
        }

        /// <summary>
        /// Converts the numeric value of this <see cref="BigInteger"/> to its string representation in the given radix.
        /// </summary>
        /// <param name="radix">The radix for the conversion</param>
        /// <returns>The string representation in the given radix</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the radix is out of range [2,36].</exception>
        /// <remarks>
        /// <para>Compute a set of 'super digits' in a 'super radix' that is computed based on the <paramref name="radix"/>;
        /// specifically, it is based on how many digits in the given radix can fit into a uint when converted.  Each 'super digit'
        /// is then translated into a string of digits in the given radix and appended to the result string.
        /// </para>
        /// <para>The Java and the DLR code are very similar.</para>
        /// </remarks>
        public string ToString(uint radix)
        {
            if ( radix < MinRadix || radix > MaxRadix )
                throw new ArgumentOutOfRangeException(
                    String.Format("Radix {0} out of range [{1},{2}]",radix, MinRadix, MaxRadix));

            if ( _sign == 0 )
                return "0";

            int len = _data.Length;

            uint[] working = (uint[])_data.Clone();
            uint superRadix = SuperRadix[radix];

            // TODO: figure out max, pre-allocate space (in array)
            List<uint> rems = new List<uint>();

            int index = 0;
            while (index < len)
            {
                uint rem = InPlaceDivRem(working, ref index, superRadix);
                rems.Add(rem);
            }

            StringBuilder sb = new StringBuilder(rems.Count * RadixDigitsPerDigit[radix] + 1);

            if (_sign < 0)
                sb.Append('-');

            char[] charBuf = new char[RadixDigitsPerDigit[radix]];

            AppendDigit(sb, rems[rems.Count - 1], radix, charBuf, false);
            for (int i = rems.Count - 2; i >= 0; i--)
                AppendDigit(sb, rems[i], radix, charBuf, true);

            return sb.ToString();
        
        }

        /// <summary>
        /// Append a sequence of digits representing <paramref name="rem"/> to the <see cref="System.StringBuilder"/>,
        /// possibly adding leading null chars if specified.
        /// </summary>
        /// <param name="sb">The <see cref="System.StringBuilder"/> to append characters to</param>
        /// <param name="rem">The 'super digit' value to be converted to its string representation</param>
        /// <param name="radix">The radix for the conversion</param>
        /// <param name="charBuf">A character buffer used for temporary storage, big enough to hold the string
        /// representation of <paramref name="rem"/></param>
        /// <param name="leadingZeros">Whether or not to pad with the leading zeros if the value is not large enough to fill the buffer</param>
        /// <remarks>Pretty much identical to DLR BigInteger.AppendRadix</remarks>
        private static void AppendDigit(StringBuilder sb, uint rem, uint radix, char[] charBuf, bool leadingZeros)
        {
            const string symbols = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            int bufLen = charBuf.Length;
            int i;
            for (i=bufLen-1; i >= 0 && rem != 0; i-- )
            {
                uint digit = rem % radix;
                rem /= radix;
                charBuf[i] = symbols[(int)digit];
            }

            if ( leadingZeros )
            {
                for ( ; i>= 0; i-- )
                    charBuf[i] = '0';
                sb.Append(charBuf);
            }
            else 
                sb.Append(charBuf,i+1,bufLen-i-1);
        }


        /// <summary>
        /// The maximum number of digits in radix [i] that will fit into a uint.
        /// </summary>
        /// <remarks>
        /// <para>RadixDigitsPerDigit[i] = floor(log_i (2^32 - 1))</para>
        /// <para>See the radix.xlsx spreadsheet.</para>
        /// </remarks>
        static readonly int[] RadixDigitsPerDigit = { 0, 0, 
            31, 20, 15, 13, 12,
            11, 10, 10, 9, 9, 
            8, 8, 8, 8, 7,
            7, 7, 7, 7, 7,
            7, 7, 6, 6, 6,
            6, 6, 6, 6, 6,
            6, 6, 6, 6, 6
       };

        /// <summary>
        /// The super radix (power of given radix) that fits into a uint.
        /// </summary>
        /// <remarks>
        /// <para>SuperRadix[i] = 2 ^ RadixDigitsPerDigit[i]</para>
        /// <para>See the radix.xlsx spreadsheet.</para>
        /// </remarks>
        static readonly uint[] SuperRadix = { 0,0,
           0x80000000, 0xCFD41B91, 0x40000000, 0x48C27395, 0x81BF1000, 
           0x75DB9C97, 0x40000000, 0xCFD41B91, 0x3B9ACA00, 0x8C8B6D2B,
           0x19A10000, 0x309F1021, 0x57F6C100, 0x98C29B81, 0x10000000,
           0x18754571, 0x247DBC80, 0x3547667B, 0x4C4B4000, 0x6B5A6E1D,
           0x94ACE180, 0xCAF18367, 0xB640000, 0xE8D4A51, 0x1269AE40, 
           0x17179149, 0x1CB91000, 0x23744899, 0x2B73A840, 0x34E63B41, 
           0x40000000, 0x4CFA3CC1, 0x5C13D840, 0x6D91B519, 0x81BF1000
           };


        /// <summary>
        /// The number of bits in one digit of radix [i] times 1024.
        /// </summary>
        /// <remarks>    
        /// <para>BitsPerRadixDigit[i] = ceiling(1024*log_2(i))</para>
        /// <para>The value is multiplied by 1024 to avoid fractions.  Users will need to divide by 1024.</para>
        /// <para>See the radix.xlsx spreadsheet.</para>
        /// </remarks>
        static readonly int[] BitsPerRadixDigit = { 0, 0,
            1024, 1624, 2048, 2378, 2648, 
            2875, 3072, 3247, 3402, 3543, 
            3672, 3790, 3899, 4001, 4096,
            4186, 4271, 4350, 4426, 4498,
            4567, 4633, 4696, 4756, 4814,
            4870, 4923, 4975, 5025, 5074,
            5120, 5166, 5210, 5253, 5295
                                         };               
 

        /// <summary>
        /// Convert a substring in a given radix to its equivalent numeric value as a UInt32.
        /// </summary>
        /// <param name="val">The string containing the substring to convert</param>
        /// <param name="startIndex">The start index of the substring</param>
        /// <param name="len">The length of the substring</param>
        /// <param name="radix">The radix</param>
        /// <param name="u">Set to the converted value, or 0 if the conversion is unsuccessful</param>
        /// <returns><value>true</value> if successful, <value>false</value> otherwise</returns>
        /// <remarks>The length of the substring must be small enough that the converted value is guaranteed to fit
        /// into a uint.</remarks>
        static bool TryParseUInt(string val, int startIndex, int len, int radix, out uint u)
        {
            u = 0;
            ulong result = 0;
            for (int i = 0; i < len; i++)
            {
                if (!TryComputeDigitVal(val[startIndex + i], radix, out uint v))
                    return false;
                result = result * (uint)radix + v;
                if (result > UInt32.MaxValue)
                    return false;
            }
            u = (uint)result;
            return true;
        }


        /// <summary>
        /// Convert an (extended) digit to its value in the given radix.
        /// </summary>
        /// <param name="c">The character to convert</param>
        /// <param name="radix">The radix to interpret the character in</param>
        /// <param name="v">Set to the converted value</param>
        /// <returns><value>true</value> if the conversion is successful; <value>false</value> otherwise</returns>
        private static bool TryComputeDigitVal(char c, int radix, out uint v)
        {
            v = uint.MaxValue;

            if ('0' <= c && c <= '9')
                v = (uint)( c - '0');
            else if ('a' <= c && c <= 'z')
                v = (uint)(10 + c - 'a');
            else if ('A' <= c && c <= 'Z')
                v = (uint)(10 + c - 'A');

            return v < radix;
        }

        #endregion

        #region Conversion operators (to BigInteger)

        /// <summary>
        /// Implicitly convert from byte to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(byte v)
        {
            return Create((uint)v);
        }

        /// <summary>
        /// Implicitly convert from sbyte to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(sbyte v)
        {
            return Create((int)v);
        }

        /// <summary>
        /// Implicitly convert from short to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(short v)
        {
            return Create((int)v);
        }

        /// <summary>
        /// Implicitly convert from ushort to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(ushort v)
        {
            return Create((uint)v);
        }

        /// <summary>
        /// Implicitly convert from uint to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(uint v)
        {
            return Create(v);
        }

        /// <summary>
        /// Implicitly convert from int to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(int v)
        {
            return Create(v);
        }

        /// <summary>
        /// Implicitly convert from ulong to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(ulong v)
        {
            return Create(v);
        }

        /// <summary>
        /// Implicitly convert from long to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(long v)
        {
            return Create(v);
        }

        /// <summary>
        /// Implicitly convert from decimal to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static implicit operator BigInteger(decimal v)
        {
            return Create(v);
        }

        /// <summary>
        /// Explicitly convert from double to <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="v">The value to convert</param>
        /// <returns>The equivalent <see cref="BigInteger"/></returns>
        public static explicit operator BigInteger(double self)
        {
            return Create(self);
        }

        #endregion

        #region Conversion operators (from BigInteger)

        /// <summary>
        /// Implicitly convert from <see cref="BigInteger"/> to double.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent double</returns>
        public static explicit operator double(BigInteger i)
        {
             return i.ToFloat64();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to byte.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent byte</returns>
        public static explicit operator byte(BigInteger self)
        {
            if (self.AsInt32(out int tmp))
            {
                return checked((byte)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to sbyte.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent sbyte</returns>
        public static explicit operator sbyte(BigInteger self)
        {
            if (self.AsInt32(out int tmp))
            {
                return checked((sbyte)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to UInt16.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent UInt16</returns>
        public static explicit operator UInt16(BigInteger self)
        {
            if (self.AsInt32(out int tmp))
            {
                return checked((UInt16)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to Int16.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent Int16</returns>
        public static explicit operator Int16(BigInteger self)
        {
            if (self.AsInt32(out int tmp))
            {
                return checked((Int16)tmp);
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to UInt32.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent UInt32</returns>
        public static explicit operator UInt32(BigInteger self)
        {
            if (self.AsUInt32(out uint tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to Int32.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent Int32</returns>
        public static explicit operator Int32(BigInteger self)
        {
            if (self.AsInt32(out int tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to Int64.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent Int64</returns>
        public static explicit operator Int64(BigInteger self)
        {
            if (self.AsInt64(out long tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to UInt64.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent UInt64</returns>
        public static explicit operator UInt64(BigInteger self)
        {
            if (self.AsUInt64(out ulong tmp))
            {
                return tmp;
            }
            throw new OverflowException();
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to float.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent float</returns>
        public static explicit operator float(BigInteger self)
        {
            return checked((float)self.ToFloat64());
        }

        /// <summary>
        /// Explicitly convert from <see cref="BigInteger"/> to double.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert</param>
        /// <returns>The equivalent decimal</returns>
        public static explicit operator decimal(BigInteger self)
        {
            if (self.AsDecimal(out decimal res))
            {
                return res;
            }
            throw new OverflowException();
        }



        #endregion

        #region Comparison operators

        /// <summary>
        /// Compare two <see cref="BigInteger"/>s for equivalent numeric values.
        /// </summary>
        /// <param name="x">First value to compare</param>
        /// <param name="y">Second value to compare</param>
        /// <returns><value>true</value> if equivalent; <value>false</value> otherwise</returns>
        public static bool operator ==(BigInteger x, BigInteger y) {
            return Compare(x, y) == 0;
        }

        /// <summary>
        /// Compare two <see cref="BigInteger"/>s for non-equivalent numeric values.
        /// </summary>
        /// <param name="x">First value to compare</param>
        /// <param name="y">Second value to compare</param>
        /// <returns><value>true</value> if not equivalent; <value>false</value> otherwise</returns>
        public static bool operator !=(BigInteger x, BigInteger y)
        {
            return Compare(x, y) != 0;
        }

        /// <summary>
        /// Compare two <see cref="BigInteger"/>s for <.
        /// </summary>
        /// <param name="x">First value to compare</param>
        /// <param name="y">Second value to compare</param>
        /// <returns><value>true</value> if <; <value>false</value> otherwise</returns>
        public static bool operator <(BigInteger x, BigInteger y)
        {
            return Compare(x, y) < 0;
        }

        /// <summary>
        /// Compare two <see cref="BigInteger"/>s for <=.
        /// </summary>
        /// <param name="x">First value to compare</param>
        /// <param name="y">Second value to compare</param>
        /// <returns><value>true</value> if <=; <value>false</value> otherwise</returns>
        public static bool operator <=(BigInteger x, BigInteger y)
        {
            return Compare(x, y) <= 0;
        }

        /// <summary>
        /// Compare two <see cref="BigInteger"/>s for >.
        /// </summary>
        /// <param name="x">First value to compare</param>
        /// <param name="y">Second value to compare</param>
        /// <returns><value>true</value> if >; <value>false</value> otherwise</returns>
        public static bool operator >(BigInteger x, BigInteger y)
        {
            return Compare(x, y) > 0;
        }

        /// <summary>
        /// Compare two <see cref="BigInteger"/>s for >=.
        /// </summary>
        /// <param name="x">First value to compare</param>
        /// <param name="y">Second value to compare</param>
        /// <returns><value>true</value> if >=; <value>false</value> otherwise</returns>
        public static bool operator >=(BigInteger x, BigInteger y)
        {
            return Compare(x, y) >= 0;      
        }

        #endregion

        #region Arithmetic operators

        /// <summary>
        /// Compute <paramref name="x"/> + <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The sum</returns>
        public static BigInteger operator +(BigInteger x, BigInteger y)
        {
            return x.Add(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> - <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The difference</returns>
        public static BigInteger operator -(BigInteger x, BigInteger y)
        {
            return x.Subtract(y);
        }

        /// <summary>
        /// Compute the negation of <paramref name="x"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The negation</returns>
        public static BigInteger operator -(BigInteger x) 
        {
            return x.Negate();
        }

        /// <summary>
        /// Compute <paramref name="x"/> * <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The product</returns>
        public static BigInteger operator *(BigInteger x, BigInteger y)
        {
            return x.Multiply(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> / <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The quotient</returns>
        public static BigInteger operator /(BigInteger x, BigInteger y)
        {
            return x.Divide(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> % <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The modulus</returns>
        public static BigInteger operator %(BigInteger x, BigInteger y)
        {
            return x.Mod(y);
        }

        #endregion

        #region Arithmetic methods (static)


        /// <summary>
        /// Compute <paramref name="x"/> + <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The sum</returns>
        public static BigInteger Add(BigInteger x, BigInteger y)
        {
            return x.Add(y);
        }


        /// <summary>
        /// Compute <paramref name="x"/> - <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The difference</returns>
        public static BigInteger Subtract(BigInteger x, BigInteger y)
        {
            return x.Subtract(y);
        }

        
        /// <summary>
        /// Compute the negation of <paramref name="x"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The negation</returns>
        public static BigInteger Negate(BigInteger x)
        {
            return x.Negate();
        }

        /// <summary>
        /// Compute <paramref name="x"/> * <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The product</returns>
        public static BigInteger Multiply(BigInteger x, BigInteger y)
        {
            return x.Multiply(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> / <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The quotient</returns>
        public static BigInteger Divide(BigInteger x, BigInteger y)
        {
            return x.Divide(y);
        }

        /// <summary>
        /// Returns <paramref name="x"/> % <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The modulus</returns>
        public static BigInteger Mod(BigInteger x, BigInteger y)
        {
            return x.Mod(y);
        }

        /// <summary>
        /// Compute the quotient and remainder of dividing one <see cref="BigInteger"/> by another.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="remainder">Set to the remainder after division</param>
        /// <returns>The quotient</returns>
        public static BigInteger DivRem(BigInteger x, BigInteger y, out BigInteger remainder)
        {
            return x.DivRem(y, out remainder);
        }

        /// <summary>
        /// Compute the absolute value.
        /// </summary>
        /// <param name="x"></param>
        /// <returns>The absolute value</returns>
        public static BigInteger Abs(BigInteger x)
        {
            return x.Abs(); ;
        }

        /// <summary>
        /// Returns a <see cref="BigInteger"/> raised to an int power.
        /// </summary>
        /// <param name="x">The value to exponentiate</param>
        /// <param name="exp">The exponent</param>
        /// <returns>The exponent</returns>
        public static BigInteger Power(BigInteger x, int exp)
        {  
            return x.Power(exp);
        }

        /// <summary>
        /// Returns a <see cref="BigInteger"/> raised to an <see cref="BigInteger"/> power modulo another <see cref="BigInteger"/>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="power"></param>
        /// <param name="mod"></param>
        /// <returns> x ^ e mod m</returns>
        public static BigInteger ModPow(BigInteger x, BigInteger power, BigInteger mod)
        {
            return x.ModPow(power, mod);
        }


        /// <summary>
        /// Returns the greatest common divisor.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The greatest common divisor</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private static BigInteger Gcd(BigInteger x, BigInteger y)
        {
            return x.Gcd(y);
        }

        #endregion

        #region Bit operators

        /// <summary>
        /// Returns the bitwise-AND.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BigInteger operator &(BigInteger x, BigInteger y)
        {
            return x.BitwiseAnd(y);
        }

        /// <summary>
        /// Returns the bitwise-OR.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BigInteger operator |(BigInteger x, BigInteger y)
        {
            return x.BitwiseOr(y);
        }

        /// <summary>
        /// Returns the bitwise-complement.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static BigInteger operator ~(BigInteger x)
        {
            return x.OnesComplement();
        }


        /// <summary>
        /// Returns the bitwise-XOR.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BigInteger operator ^(BigInteger x, BigInteger y)
        {
            return x.Xor(y);
        }


        /// <summary>
        /// Returns the left-shift of a <see cref="BigInteger"/> by an int shift.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static BigInteger operator <<(BigInteger x, int shift)
        {
            return x.LeftShift(shift);
        }

        /// <summary>
        /// Returns the right-shift of a <see cref="BigInteger"/> by an int shift.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static BigInteger operator >>(BigInteger x, int shift)
        {
            return x.RightShift(shift);
        }

        #endregion

        #region Bit operation methods (static)

        /// <summary>
        /// Returns the bitwise-AND.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BigInteger BitwiseAnd(BigInteger x, BigInteger y)
        {
            return x.BitwiseAnd(y);
        }

        /// <summary>
        /// Returns the bitwise-OR.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BigInteger BitwiseOr(BigInteger x, BigInteger y)
        {
            return x.BitwiseOr(y);
        }

        /// <summary>
        /// Returns the bitwise-XOR.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BigInteger BitwiseXor(BigInteger x, BigInteger y)
        {
            return x.Xor(y);
        }

        /// <summary>
        /// Returns the bitwise complement.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static BigInteger BitwiseNot(BigInteger x)
        {
            return x.OnesComplement();
        }

        /// <summary>
        /// Returns the bitwise x & ~y.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static BigInteger BitwiseAndNot(BigInteger x, BigInteger y)
        {
            return x.BitwiseAndNot(y);
        }

        /// <summary>
        /// Returns  x << shift.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static BigInteger LeftShift(BigInteger x, int shift)
        {
            return x.LeftShift(shift);
        }

        /// <summary>
        /// Returns x >> shift.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static BigInteger RightShift(BigInteger x, int shift)
        {
            return x.RightShift(shift);
        }


        /// <summary>
        /// Test if a specified bit is set.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static bool TestBit(BigInteger x, int n)
        {
            return x.TestBit(n);
        }

        /// <summary>
        /// Set the specified bit.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static BigInteger SetBit(BigInteger x, int n)
        {
            return x.SetBit(n);
        }

        /// <summary>
        /// Set the specified bit to its negation.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static BigInteger FlipBit(BigInteger x, int n)
        {
            return x.FlipBit(n);
        }

        /// <summary>
        ///  Clear the specified bit.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static BigInteger ClearBit(BigInteger x, int n)
        {
            return x.ClearBit(n);
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
            switch (_data.Length)
            {
                case 0:
                    return true;
                case 1:
                    if (_data[0] > 0x80000000u)
                        return false;
                    if (_data[0] == 0x80000000u && _sign == 1)
                        return false;
                    ret = ((int)_data[0]) * _sign;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Try to convert to an Int64.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsInt64(out long ret)
        {
            ret = 0;
            switch (_data.Length)
            {
                case 0:

                    return true;
                case 1:
                    ret = _sign * (long)_data[0];
                    return true;
                case 2:
                    {
                        ulong tmp = (((ulong)_data[0]) << 32 | (ulong)_data[1]);
                        if (tmp > 0x8000000000000000u) 
                            return false;
                        if (tmp == 0x8000000000000000u && _sign == 1) 
                            return false;
                        ret = ((long)tmp) * _sign;
                        return true;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// Try to convert to an UInt32.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsUInt32(out uint ret)
        {
            ret = 0;
            if (_sign == 0) 
                return true;
            if (_sign < 0) 
                return false;
            if (_data.Length > 1) 
                return false;
            ret = _data[0];
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

            if (_sign < 0)
                return false;

            switch (_data.Length)
            {
                case 0:
                    return true;
                case 1:
                    ret = (ulong)_data[0];
                    return true;
                case 2:
                    ret = (ulong)_data[1] | ((ulong)_data[0]) << 32;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Try to convert to a Decimal.
        /// </summary>
        /// <param name="ret">Set to the converted value</param>
        /// <returns><value>true</value> if successful; <value>false</value> if the value cannot be represented.</returns>
        public bool AsDecimal(out Decimal ret)
        {
            if (_sign == 0)
            {
                ret = Decimal.Zero;
                return true;
            }

            int length = _data.Length;
            int mi = 0, hi = 0;
            int lo;
            switch (length)
            {
                case 1:
                    lo = (int)_data[0];
                    break;
                case 2:
                    lo = (int)_data[1];
                    mi = (int)_data[0];
                    break;
                case 3:
                    lo = (int)_data[2];
                    mi = (int)_data[1];
                    hi = (int)_data[0];
                    break;
                default:
                    ret = default;
                    return false;
            }

            ret = new Decimal(lo, mi, hi, _sign < 0, 0);
            return true;
        }

        #endregion

        #region Conversion methods

        /// <summary>
        /// Convert to an equivalent UInt32
        /// </summary>
        /// <returns>The equivalent value</returns>
        /// <exception cref="System.OverflowException">Thrown if the magnitude is too large for the conversion</exception>
        public uint ToUInt32()
        {
            if (AsUInt32(out uint ret)) return ret;
            throw new OverflowException("BigInteger magnitude too large for UInt32");
        }

        /// <summary>
        /// Convert to an equivalent Int32
        /// </summary>
        /// <returns>The equivalent value</returns>
        /// <exception cref="System.OverflowException">Thrown if the magnitude is too large for the conversion</exception>
        public int ToInt32()
        {
            if (AsInt32(out int ret)) return ret;
            throw new OverflowException("BigInteger magnitude too large for Int32");
        }

        /// <summary>
        /// Convert to an equivalent Decimal
        /// </summary>
        /// <returns>The equivalent value</returns>
        /// <exception cref="System.OverflowException">Thrown if the magnitude is too large for the conversion</exception>
        public decimal ToDecimal()
        {
            if (AsDecimal(out decimal ret)) return ret;
            throw new OverflowException("BigInteger magnitude too large for Decimal");
        }

        /// <summary>
        /// Convert to an equivalent UInt64
        /// </summary>
        /// <returns>The equivalent value</returns>
        /// <exception cref="System.OverflowException">Thrown if the magnitude is too large for the conversion</exception>
        public ulong ToUInt64()
        {
            if (AsUInt64(out ulong ret)) return ret;
            throw new OverflowException("BigInteger magnitude too large for UInt64");
        }

        /// <summary>
        /// Convert to an equivalent Int64
        /// </summary>
        /// <returns>The equivalent value</returns>
        /// <exception cref="System.OverflowException">Thrown if the magnitude is too large for the conversion</exception>
        public long ToInt64()
        {
            if (AsInt64(out long ret)) return ret;
            throw new OverflowException("BigInteger magnitude too large for Int64");
        }

        /// <summary>
        /// Convert to an equivalent Double
        /// </summary>
        /// <returns>The equivalent value</returns>
        /// <exception cref="System.OverflowException">Thrown if the magnitude is too large for the conversion</exception>
        public double ToFloat64()
        {
            return double.Parse(
                ToString(10),
                System.Globalization.CultureInfo.InvariantCulture.NumberFormat
                );
        }

        #endregion
 
        #region IComparable Members

        /// <summary>
        /// Compares this instance to a specified object and returns an indication of their relative values.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if a comparison is made against anything other than a <see cref="BigInteger"/></exception>
        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;


            return obj is BigInteger o ? Compare(this, o) : throw new ArgumentException("Expected a BigInteger to compare against");
        }

        #endregion

        #region IConvertible Members

        /// <summary>
        /// Returns the <see cref="System.TypeCode"/> for this instance.
        /// </summary>
        /// <returns>The <see cref="System.TypeCode"/></returns>
        public TypeCode GetTypeCode()
        {
           return TypeCode.Object;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent boolean value
        /// </summary>
        /// <param name="provider"></param>
        /// <returns><value>true</value> if the value is not zero; <value>false</value> otherwise</returns>
        public bool ToBoolean(IFormatProvider provider)
        {
            return this != Zero;
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent byte value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a byte.</exception>
        public byte ToByte(IFormatProvider provider)
        {
            if (AsUInt32(out uint ret) && ret <= 0xFF)
                return (byte)ret;
            throw new OverflowException("BigInteger value won't fit in byte");
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Character value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a char.</exception>
        public char ToChar(IFormatProvider provider)
        {
            if (AsInt32(out int ret) && Char.MinValue <= ret && ret <= Char.MaxValue)
                return (char)ret;
            throw new OverflowException("BigInteger value won't fit in char");
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Decimal value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a Decimal.</exception>
        public decimal ToDecimal(IFormatProvider provider)
        {
            return ToDecimal();
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Double value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a Double.</exception>
        public double ToDouble(IFormatProvider provider)
        {
            return ToFloat64();
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Int16 value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a Int16.</exception>
        public short ToInt16(IFormatProvider provider)
        {
            if (AsInt32(out int ret) && short.MinValue <= ret && ret <= short.MaxValue)
                return (short)ret;
            throw new OverflowException("BigInteger value won't fit in an Int16");
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Int32 value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a Int32.</exception>
        public int ToInt32(IFormatProvider provider)
        {
            return ToInt32();
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent Int64 value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a Int64.</exception>
        public long ToInt64(IFormatProvider provider)
        {
            return ToInt64();
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent sbyte value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a sbyte.</exception>
        public sbyte ToSByte(IFormatProvider provider)
        {
            if (AsInt32(out int ret) && sbyte.MinValue <= ret && ret <= sbyte.MaxValue)
                return (sbyte)ret;
            throw new OverflowException("BigInteger value won't fit in sbyte");
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent float value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a float.</exception>
        public float ToSingle(IFormatProvider provider)
        {
            return checked((float)ToDouble(provider));
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent string
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        /// <summary>
        /// Converts the value of this instance to the given type.  
        /// </summary>
        /// <param name="conversionType">Type to convert to (<see cref="BigInteger"/> only.</param>
        /// <param name="provider">(Ingored)</param>
        /// <returns>The converted value</returns>
        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(BigInteger))
                return this;
            else if (conversionType == typeof(BigInt))
                return BigInt.fromBigInteger(this);
            throw new InvalidCastException();
        }


        /// <summary>
        /// Converts the value of this instance to an equivalent UInt16 value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a UInt16.</exception>
        public ushort ToUInt16(IFormatProvider provider)
        {
            if (AsUInt32(out uint ret) && ret <= ushort.MaxValue)
                return (ushort)ret;
            throw new OverflowException("BigInteger value won't fit in UInt16");

        }

        /// <summary>
        /// Converts the value of this instance to an equivalent UInt132 value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a UInt32.</exception>
        public uint ToUInt32(IFormatProvider provider)
        {
            return ToUInt32();
        }

        /// <summary>
        /// Converts the value of this instance to an equivalent UInt64 value
        /// </summary>
        /// <param name="provider">(Ignored)</param>
        /// <returns>The converted value</returns>
        /// <exception cref="System.OverflowException">Thrown if the value cannot be represented in a UInt64.</exception>
        public ulong ToUInt64(IFormatProvider provider)
        {
            return ToUInt64();
        }

        #endregion

        #region IEquatable<BigInteger> Members

        /// <summary>
        /// Indicates whether this instance is equivalent to another object of the same type.
        /// </summary>
        /// <param name="other">The object to compare this instance against</param>
        /// <returns><value>true</value> if equivalent; <value>false</value> otherwise</returns>
        public bool Equals(BigInteger other)
        {
            if (other is null) 
                return false;
            return this == other;
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            return Equals(obj as BigInteger);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            for (int i = 0; i < _data.Length; i++)
                hashCode = (int)(31 * hashCode + (_data[i] & 0xffffffffL));

            return hashCode * _sign;
        }


        #endregion

        #region Comparison implementation

        /// <summary>
        /// Returns an indication of the relative values of the two <see cref="BigInteger"/>s
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns><value>-1</value> if the first is less than second; <value>0</value> if equal; <value>+1</value> if greater</returns>
        public static int Compare(BigInteger x, BigInteger y)
        {
            if (ReferenceEquals(x,y))
                return 0;

            if (x is null)
                return -1;

            if (y is null)
                return 1;

            return x._sign == y._sign 
                ? x._sign * Compare(x._data,y._data)
                : (x._sign < y._sign ? -1 : 1);
        }

        /// <summary>
        /// Return an indication of the relative values of two uint arrays treated as unsigned big-endian values.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns><value>-1</value> if the first is less than second; <value>0</value> if equal; <value>+1</value> if greater</returns>
        private static short Compare(uint[] x, uint[] y)
        {
            int xlen = x.Length;
            int ylen = y.Length;

            if ( xlen < ylen )
                return -1;

            if ( xlen > ylen )
                return 1;
            
            for ( int i=0; i<xlen; i++ )
            {
                if ( x[i] < y[i] )
                    return -1;
                if ( x[i] > y[i] )
                    return 1;
            }
            return 0;
        }
                 
        #endregion

        #region  Arithmetic methods

        /// <summary>
        /// Returns this + y.
        /// </summary>
        /// <param name="y">The augend.</param>
        /// <returns>The sum</returns>
        public BigInteger Add(BigInteger y)
        {
            if (this._sign == 0)
                return y;

            if (y._sign == 0)
                return this;

            if ( this._sign == y._sign )
                return new BigInteger(_sign, Add(this._data, y._data));
            else
            {
                int c = Compare(this._data,y._data);

                switch ( c ) 
                {
                    case -1:
                        return new BigInteger(-this._sign, Subtract(y._data, this._data));

                    case 0:
                    return new BigInteger(BigInteger.Zero);

                    case 1:
                    return new BigInteger(this._sign, Subtract(this._data, y._data));

                    default:
                        throw new InvalidOperationException("Bogus result from Compare");
                }
            }
        }

        /// <summary>
        /// Returns this - y
        /// </summary>
        /// <param name="y">The subtrahend</param>
        /// <returns>The difference</returns>
        public BigInteger Subtract(BigInteger y)
        {
            if (y._sign == 0)
                return this;

            if (this._sign == 0)
                return y.Negate();

            if (this._sign != y._sign)
                return new BigInteger(this._sign, Add(this._data, y._data));

            int cmp = Compare(this._data, y._data);

            if (cmp == 0)
                return Zero;

            uint[] mag = (cmp > 0 ? Subtract(this._data, y._data) : Subtract(y._data, this._data));
            return new BigInteger(cmp * _sign, mag);           
        }

        /// <summary>
        /// Returns the negation of this value.
        /// </summary>
        /// <returns>The negation</returns>
        public BigInteger Negate()
        {
            return new BigInteger(-this._sign, this._data);
        }

        /// <summary>
        /// Returns this * y
        /// </summary>
        /// <param name="y">The multiplicand</param>
        /// <returns>The product</returns>
        public BigInteger Multiply(BigInteger y)
        {
            if (this._sign == 0)
                return Zero;
            if (y._sign == 0)
                return Zero;

            uint[] mag = Multiply(this._data, y._data);
            return new BigInteger(this._sign * y._sign, mag);
        }

        /// <summary>
        /// Returns this / y.
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <returns>The quotient</returns>
        public BigInteger Divide(BigInteger y)
        {
            return DivRem(y, out _);
        }

        /// <summary>
        /// Returns this % y
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <returns>The modulus</returns>
        public BigInteger Mod(BigInteger y)
        {
            DivRem(y, out BigInteger rem);
            return rem;
        }

        /// <summary>
        /// Returns the quotient and remainder of this divided by another.
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <param name="remainder">The remainder</param>
        /// <returns>The quotient</returns>
        public BigInteger DivRem(BigInteger y, out BigInteger remainder)
        {
            DivMod(_data, y._data, out uint[] q, out uint[] r);

            remainder = new BigInteger(_sign, r);
            return new BigInteger(_sign * y._sign, q);
        }

        /// <summary>
        /// Returns the absolute value of this instance.
        /// </summary>
        /// <returns>The absolute value</returns>
        public BigInteger Abs()
        {
            return _sign >- 0 ? this : Negate();
        }

        /// <summary>
        /// Returns the value of this instance raised to an integral power.
        /// </summary>
        /// <param name="exp">The exponent</param>
        /// <returns>The exponetiated value</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the exponent is negative.</exception>
        public BigInteger Power(int exp)
        {
            if (exp < 0)
                throw new ArgumentOutOfRangeException(nameof(exp),"Exponent must be non-negative");

            if (exp == 0)
                return One;

            if (_sign == 0)
                return this;

            // Exponentiation by repeated squaring
            BigInteger mult = this;
            BigInteger result = One;
            while (exp != 0)
            {
                if ((exp & 1) != 0)
                    result *= mult;
                if (exp == 1)
                    break;
                mult *= mult;
                exp >>= 1;
            }
            return result;
        }


        /// <summary>
        /// Returns (this ^ power) % modulus
        /// </summary>
        /// <param name="power">The exponent</param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public BigInteger ModPow(BigInteger power, BigInteger mod)
        {
            // TODO: Look at Java implementation for a more efficient version
            if (power < 0)
                throw new ArgumentOutOfRangeException(nameof(power),"must be non-negative");

            if (power._sign == 0)
                return One;

            if (_sign == 0)
                return this;

            // Exponentiation by repeated squaring
            BigInteger mult = this;
            BigInteger result = One;
            while (power != Zero)
            {
                if (power.IsOdd)
                {
                    result *= mult;
                    result %= mod;
                }
                if (power == One)
                    break;
                mult *= mult;
                mult %= mod;
                power >>= 1;
            }
            return result;
        }

        /// <summary>
        /// Returns the greatest common divisor of this and another value.
        /// </summary>
        /// <param name="y">The other value</param>
        /// <returns>The greatest common divisor</returns>
        public BigInteger Gcd(BigInteger y)
        {
            // We follow Java and do a hybrid/binary gcd

            if (y._sign == 0)
                this.Abs();
            else if (this._sign == 0)
                return y.Abs();

            // TODO: get rid of unnecessary object creation?
            return HybridGcd(this.Abs(),y.Abs());
        }

        /// <summary>
        /// Compute the greatest common divisor of two <see cref="BigInteger"/> values.
        /// </summary>
        /// <param name="a">The first value</param>
        /// <param name="b">The second value</param>
        /// <returns>The greatest common divisor</returns>
        /// <remarks>Does the standard Euclidean algorithm until the two values are approximately
        /// the same length, then switches to a binary gcd algorithm.</remarks>
        private static BigInteger HybridGcd(BigInteger a, BigInteger b)
        {
            while ( b._data.Length != 0 ) 
            {
                if ( Math.Abs(a._data.Length - b._data.Length ) < 2 )
                    return BinaryGcd(a,b);
                a.DivRem(b, out BigInteger r);
                a = b;
                b = r;
            }
            return a;
        }

        /// <summary>
        /// Compute the greatest common divisor of two <see cref="BigInteger"/> values.
        /// </summary>
        /// <param name="a">The first value</param>
        /// <param name="b">The second value</param>
        /// <returns>The greatest common divisor</returns>
        /// <remarks>Intended for use when the two values have approximately the same magnitude.</remarks>
        private static BigInteger BinaryGcd(BigInteger a, BigInteger b)
        {
            // From Knuth, 4.5.5, Algorithm B

            // TODO: make this create fewer values, do more in-place manipulations

            // Step B1: Find power of 2
            int s1 = a.GetLowestSetBit();
            int s2 = b.GetLowestSetBit();
            int k = Math.Min(s1, s2);
            if (k != 0)
            {
                a = a.RightShift(k);
                b = b.RightShift(k);
            }

            // Step B2: Initialize
            BigInteger t;
            int tsign;

            if (k == s1)
            {
                t = b;
                tsign = -1;
            }
            else
            {
                t = a;
                tsign = 1;
            }

            int lb;
            while ((lb = t.GetLowestSetBit()) >= 0)
            {
                // Steps B3 and B4  Halve t until not even.
                t = t.RightShift(lb);
                //Step B5: reset max(u,v)
                if (tsign > 0)
                    a = t;
                else
                    b = t;

                //  One word?
                if (a.AsUInt32(out uint x) && b.AsUInt32(out uint y))
                {
                    x = BinaryGcd(x, y);
                    t = BigInteger.Create(x);
                    if (k > 0)
                        t = t.LeftShift(k);
                    return t;
                }

                // Step B6: Subtract
                // TODO: Clean up extra object creation here.
                t = a - b;
                if (t.IsZero)
                    break;

                if (t.IsPositive)
                    tsign = 1;
                else
                {
                    tsign = -1;
                    t = t.Abs();
                }
            }

            if (k > 0)
                a = a.LeftShift(k);
            return a;
        }

        /// <summary>
        /// Return the greatest common divisor of two uint values.
        /// </summary>
        /// <param name="a">The first value</param>
        /// <param name="b">The second value</param>
        /// <returns>The greatest common divisor</returns>
        /// <remarks>Uses Knuth, 4.5.5, Algorithm B, highly optimized for getting rid of powers of 2.
        /// </remarks>
        private static uint BinaryGcd(uint a, uint b)
        {
            // From Knuth, 4.5.5, Algorithm B
            if (b == 0)
                return a;
            if (a == 0)
                return b;

            uint x;
            int aZeros = 0;
            while ((x = a & 0xff) == 0 ) 
            {
                a >>= 8;
                aZeros += 8;
            }

            int y = TrailingZerosTable[x];
            aZeros += y;
            a >>= y;

            int bZeros = 0;
            while ((x = b & 0xff) == 0)
            {
                b >>= 8;
                bZeros += 8;
            }
            y = TrailingZerosTable[x];
            bZeros += y;
            b >>= y;

            int t = (aZeros < bZeros ? aZeros : bZeros);

            while (a != b)
            {
                if (a > b)
                {
                    a -= b;
                    while ((x = a & 0xff) == 0)
                        a >>= 8;
                    a >>= TrailingZerosTable[x];
                }
                else
                {
                    b -= a;
                    while ((x = b & 0xff) == 0)
                        b >>= 8;
                    b >>= TrailingZerosTable[x];
                }
            }
            return a << t;
        }

        /// <summary>
        /// Returns the number of trailing zero bits in a uint value.
        /// </summary>
        /// <param name="val">The value</param>
        /// <returns>The number of trailing zero bits </returns>
        static int TrailingZerosCount(uint val)
        {
            uint byteVal = val & 0xff;
            if (byteVal != 0)
                return TrailingZerosTable[byteVal];

            byteVal = (val >> 8) & 0xff;
            if (byteVal != 0)
                return TrailingZerosTable[byteVal] + 8;

            byteVal = (val >> 16) & 0xff;
            if (byteVal != 0)
                return TrailingZerosTable[byteVal] + 16;

            byteVal = (val >> 24) & 0xff;
            return TrailingZerosTable[byteVal] + 24;
        }

        /// <summary>
        /// The value at index i is the number of trailing zero bits in the value i.
        /// </summary>
        static readonly byte[] TrailingZerosTable = 
        {
            0, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            6, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            7, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            6, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0
        };

        /// <summary>
        /// Returns the index of the lowest set bit in this instance's magnitude.
        /// </summary>
        /// <returns>The index of the lowest set bit</returns>
        private int GetLowestSetBit()
        {
            if (_sign == 0)
                return -1;
            int j;
            for (j = _data.Length - 1; j > 0 && _data[j] == 0; --j)
                ;
   
            return ((_data.Length - j - 1) << 5) + TrailingZerosCount(_data[j]);
        }


        #endregion
 
        #region Bit operation methods -- Boolean

        /// <summary>
        /// Return the bitwise-AND of this instance and another <see cref="BigInteger"/>
        /// </summary>
        /// <param name="y">The value to AND to this instance.</param>
        /// <returns>The bitwise-AND</returns>
        public BigInteger BitwiseAnd(BigInteger y)
        {
            int rlen = Math.Max(_data.Length, y._data.Length);
            uint[] result = new uint[rlen];

            bool seenNonZeroX =false ;
            bool seenNonZeroY = false;
            for ( int i=0; i<rlen; i++ )
            {
                uint xdigit = Get2CDigit(i,ref seenNonZeroX);
                uint ydigit = y.Get2CDigit(i,ref seenNonZeroY);
                result[rlen-i-1] = xdigit & ydigit;
            }

            // result is negative only if both this and y are negative
            if ( IsNegative && y.IsNegative )
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
            else
                return new BigInteger(1, RemoveLeadingZeros(result));
        }

        /// <summary>
        /// Return the bitwise-OR of this instance and another <see cref="BigInteger"/>
        /// </summary>
        /// <param name="y">The value to OR to this instance.</param>
        /// <returns>The bitwise-OR</returns>
        public BigInteger BitwiseOr(BigInteger y)
        {
            int rlen = Math.Max(_data.Length, y._data.Length);
            uint[] result = new uint[rlen];

            bool seenNonZeroX = false;
            bool seenNonZeroY = false;
            for (int i = 0; i < rlen; i++)
            {
                uint xdigit = Get2CDigit(i, ref seenNonZeroX);
                uint ydigit = y.Get2CDigit(i, ref seenNonZeroY);
                result[rlen - i - 1] = xdigit | ydigit;
            }

            // result is negative only if either this or y is negative
            if (IsNegative || y.IsNegative)
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
            else
                return new BigInteger(1, RemoveLeadingZeros(result));
        }

        /// <summary>
        /// Return the bitwise-XOR of this instance and another <see cref="BigInteger"/>
        /// </summary>
        /// <param name="y">The value to XOR to this instance.</param>
        /// <returns>The bitwise-XOR</returns>
        public BigInteger Xor(BigInteger y)
        {
            int rlen = Math.Max(_data.Length, y._data.Length);
            uint[] result = new uint[rlen];

            bool seenNonZeroX = false;
            bool seenNonZeroY = false;
            for (int i = 0; i < rlen; i++)
            {
                uint xdigit = Get2CDigit(i, ref seenNonZeroX);
                uint ydigit = y.Get2CDigit(i, ref seenNonZeroY);
                result[rlen - i - 1] = xdigit ^ ydigit;
            }

            // result is negative only if either x and y have the same sign.
            if (this.Signum == y.Signum)
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
            else
                return new BigInteger(1, RemoveLeadingZeros(result));
        }


        /// <summary>
        /// Returns the bitwise complement of this instance.
        /// </summary>
        /// <returns>The bitwise complement</returns>
        public BigInteger OnesComplement()
        {
            int len = _data.Length;
            uint[] result = new uint[len];
            bool seenNonZero = false;
            for (int i = 0; i < len; i++)
            {
                uint xdigit = Get2CDigit(i, ref seenNonZero);
                result[len - i - 1] = ~xdigit;
            }

            if (IsNegative)
                return new BigInteger(1, RemoveLeadingZeros(result));
            else
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
        }

        /// <summary>
        /// Return the bitwise-AND-NOT of this instance and another <see cref="BigInteger"/>
        /// </summary>
        /// <param name="y">The value to OR to this instance.</param>
        /// <returns>The bitwise-AND-NOT</returns>

        public BigInteger BitwiseAndNot(BigInteger y)
        {
            int rlen = Math.Max(_data.Length, y._data.Length);
            uint[] result = new uint[rlen];

            bool seenNonZeroX = false;
            bool seenNonZeroY = false;
            for (int i = 0; i < rlen; i++)
            {
                uint xdigit = Get2CDigit(i, ref seenNonZeroX);
                uint ydigit = y.Get2CDigit(i, ref seenNonZeroY);
                result[rlen - i - 1] = xdigit & ~ydigit;
            }

            // result is negative only if either this is negative and y is positive
            if (IsNegative & y.IsPositive)
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
            else
                return new BigInteger(1, RemoveLeadingZeros(result));
        }

        #endregion

        #region Bit operation methods -- single bit

        /// <summary>
        /// Returns the value of the given bit in this instance.
        /// </summary>
        /// <param name="n">Index of the bit to check</param>
        /// <returns><value>true</value> if the bit is set; <value>false</value> otherwise</returns>
        /// <exception cref="System.ArithmeticException">Thrown if the index is negative.</exception>
        /// <remarks>The value is treated as if in twos-complement.</remarks>
        public bool TestBit(int n)
        {
            if (n < 0)
                throw new ArithmeticException("Negative bit address");

            return (Get2CDigit(n / 32) & (1 << (n % 32))) != 0;
        }

        /// <summary>
        /// Set the n-th bit.
        /// </summary>
        /// <param name="n">Index of the bit to set</param>
        /// <returns>An instance with the bit set</returns>
        /// <exception cref="System.ArithmeticException">Thrown if the index is negative.</exception>
        /// <remarks>The value is treated as if in twos-complement.</remarks>
        public BigInteger SetBit(int n)
        {
            // This will work if the bit is already set.
            if (TestBit(n))
                return this;

            int index = n / 32;
            uint[] result = new uint[Math.Max(_data.Length, index+1)];

            int len = result.Length;

            bool seenNonZero = false;
            for (int i = 0; i < len; i++)
                result[len - i - 1] = Get2CDigit(i, ref seenNonZero);

            result[len - index - 1] |= (1u << (n % 32));

            if ( IsNegative )
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
            else
                return new BigInteger(1, RemoveLeadingZeros(result));
        }

        /// <summary>
        /// Clears the n-th bit.
        /// </summary>
        /// <param name="n">Index of the bit to clear</param>
        /// <returns>An instance with the bit cleared</returns>
        /// <exception cref="System.ArithmeticException">Thrown if the index is negative.</exception>
        /// <remarks>The value is treated as if in twos-complement.</remarks>
        public BigInteger ClearBit(int n)
        {

            // This will work if the bit is already clear.
            if (!TestBit(n))
                return this;

            int index = n / 32;
            uint[] result = new uint[Math.Max(_data.Length, index+1)];

            int len = result.Length;

            bool seenNonZero = false;
            for (int i = 0; i < len; i++)
                result[len - i - 1] = Get2CDigit(i, ref seenNonZero);

            result[len - index - 1] &= ~(1u << (n % 32));

            if (IsNegative)
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
            else
                return new BigInteger(1, RemoveLeadingZeros(result));
        }

        /// <summary>
        /// Toggles the n-th bit.
        /// </summary>
        /// <param name="n">Index of the bit to toggle</param>
        /// <returns>An instance with the bit toggled</returns>
        /// <exception cref="System.ArithmeticException">Thrown if the index is negative.</exception>
        /// <remarks>The value is treated as if in twos-complement.</remarks>
        public BigInteger FlipBit(int n)
        {
            if (n < 0)
                throw new ArithmeticException("Negative bit address");

            int index = n / 32;
            uint[] result = new uint[Math.Max(_data.Length, index+1)];

            int len = result.Length;

            bool seenNonZero = false;
            for (int i = 0; i < len; i++)
                result[len - i - 1] = Get2CDigit(i, ref seenNonZero);

            result[len - index - 1] ^= (1u << (n % 32));

            if (IsNegative)
                return new BigInteger(-1, RemoveLeadingZeros(MakeTwosComplement(result)));
            else
                return new BigInteger(1, RemoveLeadingZeros(result));
        }

        #endregion

        #region Bit operation methods -- shifts

        /// <summary>
        /// Returns the value of this instance left-shifted by the given number of bits.
        /// </summary>
        /// <param name="shift">The number of bits to shift.</param>
        /// <returns>An instance with the magnitude shifted.</returns>
        /// <remarks><para>The value is treated as if in twos-complement.</para>
        /// <para>A negative shift count will be treated as a positive right shift.</para></remarks>
        public BigInteger LeftShift(int shift)
        {
            if (shift == 0)
                return this;

            if ( _sign == 0 )
                return this;

            if (shift < 0)
                return RightShift(-shift);

            int digitShift = shift / BitsPerDigit;
            int bitShift = shift % BitsPerDigit;

            int xlen = _data.Length;

            uint[] result;

            if (bitShift == 0)
            {
                result = new uint[xlen + digitShift];
                _data.CopyTo(result, 0);
            }
            else
            {
                int rShift = BitsPerDigit - bitShift;
                uint highBits = _data[0] >> rShift;
                int i;
                if ( highBits == 0 )
                {
                    result = new uint[xlen + digitShift];
                    i=0;
                }
                else
                {
                    result = new uint[xlen + digitShift+1];
                    result[0] = highBits;
                    i=1;
                }

                for (int j = 0; j < xlen - 1; j++, i++)
                    result[i] = _data[j] << bitShift | _data[j + 1] >> rShift;
                result[i] = _data[xlen - 1] << bitShift;
            }

            return new BigInteger(_sign, result);
        }

        /// <summary>
        /// Returns the value of this instance right-shifted by the given number of bits.
        /// </summary>
        /// <param name="shift">The number of bits to shift.</param>
        /// <returns>An instance with the magnitude shifted.</returns>
        /// <remarks><para>The value is treated as if in twos-complement.</para>
        /// <para>A negative shift count will be treated as a positive left shift.</para></remarks>
        public BigInteger RightShift(int shift)
        {
            if (shift == 0)
                return this;

            if (_sign == 0)
                return this;

            if (shift < 0)
                return LeftShift(-shift);

            int digitShift = shift / BitsPerDigit;
            int bitShift = shift % BitsPerDigit;

            int xlen = _data.Length;

            if (digitShift >= xlen)
                return _sign >= 0 ? Zero : NegativeOne;

            uint[] result;

            if ( bitShift == 0 )
            {
                int rlen = xlen - digitShift;
                result = new uint[rlen];
                for ( int i=0; i<rlen; i++)
                    result[i] = _data[i];
            }
            else
            {
                uint highBits = _data[0] >> bitShift;
                int rlen;
                int i;
                if (highBits == 0)
                {
                    rlen = xlen - digitShift - 1;
                    result = new uint[rlen];
                    i = 0;
                }
                else
                {
                    rlen = xlen - digitShift;
                    result = new uint[rlen];
                    result[0] = highBits;
                    i = 1;
                }

                int lShift = BitsPerDigit - bitShift;
                for (int j = 0; j < xlen - digitShift - 1; j++, i++)
                    result[i] = _data[j] << lShift | _data[j + 1] >> bitShift;
            }

            return new BigInteger(_sign, result);
        }


        #endregion

        #region  Bit operation -- supporting methods

        /// <summary>
        /// Returns the specified uint-digit pretending the number
        /// is a little-endian two's complement representation.
        /// </summary>
        /// <param name="n">The index of the digit to retrieve</param>
        /// <returns>The uint at the given index.</returns>
        /// <remarks>If iterating through the data array, better to use
        /// the incremental version that keeps track of whether or not
        ///  the first nonzero has been seen.</remarks>
        private uint Get2CDigit(int n)
        {
            if (n < 0)
                return 0;
            if (n >= _data.Length)
                return Get2CSignExtensionDigit();

            uint digit = _data[_data.Length - n - 1];

            if (_sign >= 0)
                return digit;

            if (n <= FirstNonzero2CDigitIndex())
                return (~digit) + 1;
            else
                return ~digit;
        }


        /// <summary>
        /// Returns the specified uint-digit pretending the number
        /// is a little-endian two's complement representation.
        /// </summary>
        /// <param name="n">The index of the digit to retrieve</param>
        /// <param name="seenNonZero">Set to true if a nonZero byte is seen</param>
        /// <returns>The uint at the given index.</returns>
        private uint Get2CDigit(int n, ref bool seenNonZero)
        {
           if (n < 0)
                return 0;
            if (n >= _data.Length)
                return Get2CSignExtensionDigit();

            uint digit = _data[_data.Length - n - 1];

            if (_sign >= 0)
                return digit;

            if (seenNonZero)
                return ~digit;
            else
            {
                if (digit == 0)
                    return 0;
                else
                {
                    seenNonZero = true;
                    return ~digit + 1;
                }
            }
        }

        /// <summary>
        /// Returns a uint of all zeros or all ones depending on the sign (pos, neg).
        /// </summary>
        /// <returns>The uint corresponding to the sign</returns>
        private uint Get2CSignExtensionDigit()
        {
            return _sign < 0 ? UInt32.MaxValue : 0;
        }

        /// <summary>
        /// Returns the index of the first nonzero digit (there must be one), pretending the value is little-endian.
        /// </summary>
        /// <returns></returns>
        private int FirstNonzero2CDigitIndex()
        {
            // The Java version caches this value on first computation
            int i;
            for ( i = _data.Length - 1 ; i >= 0 && _data[i] == 0; i--)
                ;
            return _data.Length - i - 1;
        }

        /// <summary>
        /// Return the twos-complement of the integer represented by the byte-array.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private static uint[] MakeTwosComplement(uint[] a)
        {
            int i = a.Length-1;
            uint digit = 0; // to prevent exit on first test
            for (; i >= 0 && digit == 0; i--)
            {
                digit = ~a[i] + 1;
                a[i] = digit;
            }

            for (; i >= 0; i-- )
                a[i] = ~a[i];


            return a;
        }



        #endregion

        #region Basic data ops

        /// <summary>
        /// Returns the sign (-1, 0, +1) of this instance.
        /// </summary>
        public int Signum
        {
            get { return _sign; }
        }
        
        /// <summary>
        /// Returns true if this instance is negative.
        /// </summary>
        public bool IsNegative
        {
            get { return _sign < 0; }
        }

        /// <summary>
        /// Returns true if this instance has value 0.
        /// </summary>
        public bool IsZero
        {
            get { return _sign == 0; }
        }


        /// <summary>
        /// Returns true if this instance is positive.
        /// </summary>
        public bool IsPositive
        {
            get { return _sign > 0; }
        }

        /// <summary>
        /// Return true if this instance has an odd value.
        /// </summary>
        public bool IsOdd
        {
            get
            {
                return (_data != null
                    && _data.Length > 0
                    && ((_data[_data.Length-1] & 1) != 0));
            }
        }

        /// <summary>
        /// Return the magnitude as a big-endian array of uints.
        /// </summary>
        /// <returns>The magnitude</returns>
        /// <remarks>The returned array can be manipulated as you like = unshared.</remarks>
        public uint[] GetMagnitude()
        {
            return (uint[])_data.Clone();
        }

        #endregion

        #region uint[] hacking

        /// <summary>
        /// Add two uint arrays (big-endian).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static uint[] Add(uint[] x, uint[] y)
        {
            // make sure x is longer, y shorter, swapping if necessary
            if (x.Length < y.Length)
            {
                uint[] temp = x;
                x = y;
                y = temp;
            }

            int xi = x.Length;
            int yi = y.Length;
            uint[] result = new uint[xi];

            ulong sum = 0;

            // add common parts, with carry
            while (yi > 0)
            {
                sum = (sum >> BitsPerDigit) + x[--xi] + y[--yi];
                result[xi] = (uint)sum;
            }

            // copy longer part of x, while carry required
            sum >>= BitsPerDigit;
            while (xi > 0 && sum != 0)
            {

                sum = ((ulong)x[--xi]) + 1;
                result[xi] = (uint)sum;
                sum >>= BitsPerDigit;
            }

            // copy remaining part, no carry required
            while (xi > 0)
                result[--xi] = x[xi];

            // if carry still required, we must grow
            if (sum != 0)
                result = AddSignificantDigit(result, (uint)sum);

            return result;
        }

        /// <summary>
        /// Add one digit.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="newDigit"></param>
        /// <returns></returns>
        private static uint[] AddSignificantDigit(uint[] x, uint newDigit)
        {
            uint[] result = new uint[x.Length + 1];
            result[0] = newDigit;
            for (int i = 0; i < x.Length; i++)
                result[i + 1] = x[i];
            return result;
        }


        /// <summary>
        /// Subtract one instance from another (larger first).
        /// </summary>
        /// <param name="xs"></param>
        /// <param name="ys"></param>
        /// <returns></returns>
        private static uint[] Subtract(uint[] xs, uint[] ys)
        {        // Assume xs > ys
            int xlen = xs.Length;
            int ylen = ys.Length;
            uint[] result = new uint[xlen];

            bool borrow = false;
            int ix = xlen-1;
            for ( int iy = ylen-1; iy >= 0; iy--, ix-- )
            {
                uint x = xs[ix];
                uint y = ys[iy];
                if ( borrow )
                {
                    if ( x == 0 )
                    {
                        x = 0xffffffff;
                        borrow = true;
                    }
                    else
                    {
                        x -= 1;
                        borrow = false;
                    }
                }
                borrow |= y > x;
                result[ix] = x-y;
            }

            for (; borrow && ix >= 0; ix--)
                borrow = (result[ix] = xs[ix] - 1) == 0xffffffff;

            for (; ix >= 0; ix--)
                result[ix] = xs[ix];
    
            return RemoveLeadingZeros(result);
        }

        /// <summary>
        ///  Multiply to big-endian uint arrays.
        /// </summary>
        /// <param name="xs"></param>
        /// <param name="ys"></param>
        /// <returns></returns>
        private static uint[] Multiply(uint[] xs, uint[] ys)
        {
            int xlen = xs.Length;
            int ylen = ys.Length;

            uint[] zs = new uint[xlen + ylen];

            for (int xi = xlen - 1; xi >= 0; xi--)
            {
                ulong x = xs[xi];
                int zi = xi + ylen;
                ulong product = 0;
                for (int yi = ylen - 1; yi >= 0; yi--, zi--)
                {
                    product = product + x * ys[yi] + zs[zi];
                    zs[zi] = (uint)product;
                    product >>= BitsPerDigit;
                }
                while (product != 0)
                {
                    product += zs[zi];
                    zs[zi++] = (uint)product;
                    product >>= BitsPerDigit;
                }
            }
            return RemoveLeadingZeros(zs);
        }

        /// <summary>
        /// Return the quotient and remainder of dividing one <see cref="BigInteger"/> by another.
        /// </summary>
        /// <param name="x">The dividend</param>
        /// <param name="y">The divisor</param>
        /// <param name="q">Set to the quotient</param>
        /// <param name="r">Set to the remainder</param>
        /// <remarks>Algorithm D in Knuth 4.3.1.</remarks>
        private static void DivMod(uint[] x, uint[] y, out uint[] q, out uint[] r)
        {
            // Handle some special cases first.

            int ylen = y.Length;

            // Special case: divisor = 0
            if ( ylen == 0 )
                throw new DivideByZeroException();

            int xlen = x.Length;

            // Special case: dividend == 0
            if (xlen == 0)
            {
                q = Array.Empty<uint>();
                r = Array.Empty<uint>();
                return;
            }

            int cmp = Compare(x,y);

            // Special case: dividend == divisor
            if ( cmp == 0 )
            {
                q = new uint[] { 1 };
                r = new uint[] { 0 };
                return;
            }

            // Special case: dividend < divisor
            if (cmp < 0 )
            {
                q = Array.Empty<uint>();
                r = (uint[])x.Clone();
                return;
            }

            // Special case: divide by single digit (uint)
            if ( ylen == 1 )
            {
                uint rem = CopyDivRem(x,y[0],out q);
                r = new uint[] {rem };
                return;
            }

            // Okay.
            // Special cases out of the way, let do Knuth's algorithm.
            // THis is almost exactly the same as in DLR's BigInteger.
            // TODO:  Look at the optimizations inthe Colin Plumb C library
            //        (used in the Java BigInteger code).

            // D1. Normalize
            // Using suggestion to take d = a power of 2 that makes v(n-1) >= b/2.

            int shift = (int)clojure.lang.Util.LeadingZeroCount(y[0]);

            uint[] xnorm = new uint[xlen + 1];
            uint[] ynorm = new uint[ylen];

            Normalize(xnorm, xlen+1, x, xlen, shift);
            Normalize(ynorm, ylen, y, ylen, shift);


            const ulong SuperB = 0x100000000u;

            q = new uint[xlen - ylen + 1];

            // Main loop: 
            //  D2: Initialize j
            //  D7: Loop on j
            // Our loop goes the opposite way because of big-endian
            for (int j = 0; j <= xlen-ylen; j++)
            {
                // D3: Calculate qhat.
                ulong toptwo = xnorm[j] * SuperB + xnorm[j + 1];
                ulong qhat = toptwo / ynorm[0];
                ulong rhat = toptwo % ynorm[0];

                // adjust if estimate is too big
                while (true)
                {
                    if (qhat < SuperB && qhat * ynorm[1] <= SuperB * rhat + xnorm[j + 2])
                        break;

                    qhat--;
                    rhat += (ulong)ynorm[0];
                    if (rhat >= SuperB)
                        break;
                }

                // D4: Multiply and subtract
                // Read Knuth very carefully when it comes to 
                //  possibly being too large, borrowing, readjusting.
                // It sucks.

                long borrow = 0;
                long temp;
                for (int k = ylen - 1; k >= 0; k--)
                {
                    int i = j + k + 1;
                    ulong val = ynorm[k] * qhat;
                    temp = (long)xnorm[i] - (long)(uint)val - borrow;
                    xnorm[i] = (uint)temp;
                    val >>= BitsPerDigit;
                    temp >>= BitsPerDigit;
                    borrow = (long)val - temp;
                }
                temp = (long)xnorm[j] - borrow;
                xnorm[j] = (uint)temp;


                // D5: Test remainder
                // We now know the quotient digit at this index
                q[j] = (uint)qhat;

                // D6: Add back
                // If we went negative, add ynorm back into the xnorm.
                if (temp < 0)
                {
                    q[j]--;
                    ulong carry = 0;
                    for (int k = ylen - 1; k >= 0; k--)
                    {
                        int i = j + k + 1;
                        carry = (ulong)ynorm[k] + xnorm[i] + carry;
                        xnorm[i] = (uint)carry;
                        carry >>= BitsPerDigit;
                    }
                    carry += (ulong)xnorm[j];
                    xnorm[j] = (uint)carry;
                }
            }


            Unnormalize(xnorm, out r, shift);


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xnorm"></param>
        /// <param name="xnlen"></param>
        /// <param name="x"></param>
        /// <param name="xlen"></param>
        /// <param name="shift"></param>
        /// <remarks>
        /// <para>Assume xnorm.Length == xlen + 1 | xlen;</para>
        /// <para>Assume shift in [0,31]</para>
        /// <para>This should be private, but I wanted to test it.</para></remarks>
        public static void Normalize(uint[] xnorm, int xnlen, uint[] x, int xlen, int shift)
        {
            bool sameLen = xnlen == xlen;
            int offset = (sameLen ? 0 : 1);
            if (shift == 0)
            {
                // just copy, with the added zero at the most significant end.
                if ( ! sameLen ) 
                    xnorm[0] = 0;
                for (int i = 0; i < xlen; i++)
                    xnorm[i+offset] = x[i];
                return;
            }

            int rshift = BitsPerDigit - shift;
            uint carry = 0;
            for (int i = xlen - 1; i >= 0; i-- )
            {
                uint xi = x[i];
                xnorm[i+offset] = (xi << shift) | carry;
                carry = xi >> rshift;
            }

            if (sameLen)
            {
                if (carry != 0)
                    throw new InvalidOperationException("Carry off left end.");
            }
            else 
                xnorm[0] = carry;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xnorm"></param>
        /// <param name="r"></param>
        /// <param name="shift"></param>
        private static void Unnormalize(uint[] xnorm, out uint[] r, int shift)
        {
            int len = xnorm.Length;
            r = new uint[len];

            if ( shift == 0 )
            {
                for ( int i=0; i< len; i++ )
                    r[i] = xnorm[i];
            }
            else
            {
                int lshift = BitsPerDigit - shift;
                uint carry = 0;
                for ( int i=0; i < len; i++ )
                {
                    uint val = xnorm[i];
                    r[i] = (val >> shift) | carry;
                    carry = val << lshift;
                }
            }

            r = RemoveLeadingZeros(r);
        }

        /// <summary>
        /// Do a multiplication and addition in place.
        /// </summary>
        /// <param name="data">The subject of the operation, receives the result</param>
        /// <param name="mult">The value to multiply by</param>
        /// <param name="addend">The value to add in</param>
        static void InPlaceMulAdd(uint[] data, uint mult, uint addend)
        {
            int len = data.Length;

            // Multiply
            ulong carry = 0;
            for (int i = len - 1; i >= 0; i--)
            {
                ulong product = ((ulong)data[i]) * mult + carry;
                data[i] = (uint)product;
                carry = product >> BitsPerDigit;
            }

            // Add
            ulong sum = ((ulong)data[len - 1]) + addend;
            data[len - 1] = (uint)sum;
            carry = sum >> BitsPerDigit;

            for (int i = len - 2; i >= 0 && carry > 0; i--)
            {
                sum = ((ulong)data[i]) + carry;
                data[i] = (uint)sum;
                carry = sum >> BitsPerDigit;
            }
        }

        /// <summary>
        /// Return a (possibly new) uint array with leading zero uints removed.
        /// </summary>
        /// <param name="data">The uint array to prune</param>
        /// <returns>A (possibly new) uint array with leading zero uints removed.</returns>
        static uint[] RemoveLeadingZeros(uint[] data)
        {
            int len = data.Length;

            int index;
            for (index = 0; index < len && data[index] == 0; index++)
                ;

            if (index == 0)
                return data;

            // we have leading zeros. Allocate new array.
            uint[] result = new uint[len - index];
            for (int i = 0; i < len - index; i++)
                result[i] = data[index + i];
            return result;
        }


        /// <summary>
        /// Do a division in place and return the remainder.
        /// </summary>
        /// <param name="data">The value to divide into, and where the result appears</param>
        /// <param name="index">Starting index in <paramref name="data"/> for the operation</param>
        /// <param name="divisor">The value to dif</param>
        /// <returns>The remainder</returns>
        /// <remarks>Pretty much identical to DLR BigInteger.div, except DLR's is little-endian
        /// and this is big-endian.</remarks>
        static uint InPlaceDivRem(uint[] data, ref int index, uint divisor)
        {
            ulong rem = 0;
            bool seenNonZero = false;
            int len = data.Length;
            for ( int i=index; i<len; i++ )
            {
                rem <<= BitsPerDigit;
                rem |= data[i];
                uint q = (uint)(rem/divisor);
                data[i] = q;
                if (  q == 0 )
                {
                    if ( ! seenNonZero )
                        index++;
                }
                else
                    seenNonZero = true;
                rem %= divisor;
            }
            return (uint)rem;
        }

        /// <summary>
        /// Divide a big-endian uint array by a uint divisor, returning the quotient and remainder.
        /// </summary>
        /// <param name="data">A big-endian uint array</param>
        /// <param name="divisor">The value to divide by</param>
        /// <param name="quotient">Set to the quotient (newly allocated)</param>
        /// <returns>The remainder</returns>
        static uint CopyDivRem(uint[] data, uint divisor, out uint[] quotient)
        {
            quotient = new uint[data.Length];

            ulong rem = 0;
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                rem <<= BitsPerDigit;
                rem |= data[i];
                uint q = (uint)(rem / divisor);
                quotient[i] = q;
                rem %= divisor;
            }

            quotient = RemoveLeadingZeros(quotient);
            return (uint)rem;
        }

        #endregion

        #region Double value hacking

        /// <summary>
        /// Exponent bias in the 64-bit floating point representation.
        /// </summary>
        public const int DoubleExponentBias = 1023;

        /// <summary>
        /// The size in bits of the significand in the 64-bit floating point representation.
        /// </summary>
        const int DoubleSignificandBitLength = 52;

        /// <summary>
        /// How much to shift to accommodate the exponent and the binary digits of the significand.
        /// </summary>
        public const int DoubleShiftBias = DoubleExponentBias + DoubleSignificandBitLength;


        /// <summary>
        /// Extract the sign bit from a byte-array representaition of a double.
        /// </summary>
        /// <param name="v">A byte-array representation of a double</param>
        /// <returns>The sign bit, either 0 (positive) or 1 (negative)</returns>
        public  static int GetDoubleSign(byte[] v)
        {
            return v[7] & 0x80;
        }

        /// <summary>
        /// Extract the significand (AKA mantissa, coefficient) from a byte-array representation of a double.
        /// </summary>
        /// <param name="v">A byte-array representation of a double</param>
        /// <returns>The significand</returns>
        public static ulong GetDoubleSignificand(byte[] v)
        {
            uint i1 = ((uint)v[0] | ((uint)v[1] << 8) | ((uint)v[2] << 16) | ((uint)v[3] << 24));
            uint i2 = ((uint)v[4] | ((uint)v[5] << 8) | ((uint)(v[6] & 0xF) << 16));

            return (ulong)((ulong)i1 | ((ulong)i2 << 32));
        }

        /// <summary>
        /// Extract the exponent from a byte-array representaition of a double.
        /// </summary>
        /// <param name="v">A byte-array representation of a double</param>
        /// <returns>The exponent</returns>
        public static ushort GetDoubleBiasedExponent(byte[] v)
        {
            return (ushort)((((ushort)(v[7] & 0x7F)) << (ushort)4) | (((ushort)(v[6] & 0xF0)) >> 4));
        }

        #endregion

        #region Precision

        // Support for BigDecimal, to compute precision.

        public uint Precision
        {
            get
            {
                if (IsZero)
                    return 1;  // 0 is one digit

                uint digits = 0;
                uint[] work = GetMagnitude();  // need a working copy.
                int index=0;
                while (index < work.Length-1 )
                {
                    InPlaceDivRem(work,ref index,1000000000U);
                    digits += 9;
                }

                if (index == work.Length - 1)
                    digits += UIntPrecision(work[index]);

                return digits;                    
            }
        }

        static readonly uint[] UIntLogTable = 
        {
            0,
            9,
            99,
            999,
            9999,
            99999,
            999999,
            9999999,
            99999999,
            999999999,
            UInt32.MaxValue
        };

        // Algorithm from Hacker's Delight, section 11-4
        public static uint UIntPrecision(uint v)
        {
            for ( uint i=1; ; i++ )
                if ( v <= UIntLogTable[i] )
                    return i;
        }

        #endregion
    }
}
