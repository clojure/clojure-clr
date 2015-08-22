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
using System.Text;
using System.Globalization;


namespace clojure.lang
{
    
    /// <summary>
    /// Immutable, arbitrary precision, signed decimal.
    /// </summary>
    /// <remarks>
    /// <para>This class is inspired by the General Decimal Arithmetic Specification (http://speleotrove.com/decimal/decarith.html, 
    /// (PDF: http://speleotrove.com/decimal/decarith.pdf).  However, at the moment, the interface and capabilities comes closer
    /// to java.math.BigDecimal, primarily because I only needed to mimic j.m.BigDecimal's capabilities to provide a minimum set
    /// of functionality for ClojureCLR.</para>
    /// <para>Because of this, as in j.m.BigDecimal, the implementation is closest to the X3.274 subset described in Appendix A
    /// of the GDAS: infinite values, NaNs, subnormal values and negative zero are not represented, and most conditions throw exceptions. 
    /// Exponent limits in the context are not implemented, except a limit to the range of an Int32. 
    /// However, we do not do "conversion to shorter" for arith ops.</para>
    /// <para>It is our long term intention to convert this to a complete implementation of the standard.</para>
    /// <para>The representation is an arbitrary precision integer (the signed coefficient, also called the unscaled value) 
    /// and an exponent.  The exponent is limited to the range of an Int32. 
    /// The value of a BigDecimal representation is <c>coefficient * 10^exponent</c>. </para>
    /// <para> Note: the representation in the GDAS is
    /// [sign,coefficient,exponent] with sign = 0/1 for (pos/neg) and an unsigned coefficient. 
    /// This yields signed zero, which we do not have.  
    /// We used a BigInteger for the signed coefficient.  
    /// That class does not have a representation for signed zero.</para>
    /// <para>Note: Compared to j.m.BigDecimal, our coefficient = their <c>unscaledValue</c> 
    /// and our exponent is the negation of their <c>scale</c>.</para>
    /// <para>The representation also track the number of significant digits.  This is usually the number of digits in the coefficient,
    /// except when the coeffiecient is zero.  This value is computed lazily and cached.</para>
    /// <para>This is not a clean-room implementation.  
    /// I examined at other code, especially OpenJDK implementation of java.math.BigDecimal, 
    /// to look for special cases and other gotchas.  Then I looked away.  
    /// I have tried to give credit in the few places where I pretty much did unthinking translation.  
    /// However, there are only so many ways to skim certain cats, so some similarities are unavoidable.</para>
    /// </remarks>
    [Serializable]
    public class BigDecimal : IComparable, IComparable<BigDecimal>, IEquatable<BigDecimal>, IConvertible
    {
        #region Rounding mode

        /// <summary>
        /// Indicates the rounding algorithm to use.
        /// </summary>
        /// <remarks>I have not implemented the round-05up algorithm mentioned in GDAS.</remarks>
        public enum RoundingMode
        {
            /// <summary>
            /// Round away from 0.
            /// </summary>
            Up,

            /// <summary>
            /// Truncate (round toward 0). 
            /// </summary>
            Down,

            /// <summary>
            /// Round toward positive infinity.
            /// </summary>
            Ceiling,

            /// <summary>
            /// Round toward negative infinity.
            /// </summary>
            Floor,

            /// <summary>
            /// Round to nearest neighbor, round up if equidistant.
            /// </summary>
            HalfUp,

            /// <summary>
            /// Round to nearest neighbor, round down if equidistant.
            /// </summary>
            HalfDown,

            /// <summary>
            /// Round to nearest neighbor, round to even neighbor if equidistant.
            /// </summary>
            HalfEven,

            /// <summary>
            /// Do not do any rounding.
            /// </summary>
            /// <remarks>This value is not part of the GDAS, but is in java.math.BigDecimal.</remarks>
            Unnecessary
        }

        #endregion

        #region Context

        [Serializable]
        public struct Context : IEquatable<Context>
        {
            #region Data

            /// <summary>
            /// The number of digits to be used.  (0 = unlimited)
            /// </summary>
            readonly uint _precision;

            /// <summary>
            /// The number of digits to be used.  (0 = unlimited)
            /// </summary>
            public uint Precision
            {
                get { return _precision; }
            }

            /// <summary>
            ///  The rounding algorithm to be used.
            /// </summary>
            readonly RoundingMode _roundingMode;


            /// <summary>
            ///  The rounding algorithm to be used.
            /// </summary>
            public RoundingMode RoundingMode
            {
                get { return _roundingMode; }
            }

            #endregion

            #region C-tors and factory methods

            static readonly Context BASIC_DEFAULT = new Context(9, RoundingMode.HalfUp);
            public static readonly Context Decimal32 = new Context(7, RoundingMode.HalfEven);
            public static readonly Context Decimal64 = new Context(16, RoundingMode.HalfEven);
            public static readonly Context Decimal128 = new Context(34, RoundingMode.HalfEven);
            public static readonly Context Unlimited = new Context(0, RoundingMode.HalfUp);

            public static Context BasicDefault() { return BASIC_DEFAULT; }

            public static Context ExtendedDefault(uint precision)
            {
                return new Context(precision, RoundingMode.HalfEven);
            }

            public Context(uint precision, RoundingMode mode)
            {
                if (precision < 0)
                    throw new ArgumentException("Precision < 0");

                _precision = precision;
                _roundingMode = mode;
            }

            public Context(uint precision)
                : this(precision, RoundingMode.HalfUp)
            {
            }

            #endregion

            #region Object overrides

            public override bool Equals(object obj)
            {
                if (!(obj is Context))
                    return false;

                return Equals((Context)obj);
            }

            public static bool operator ==(Context c1, Context c2)
            {
                if ( ReferenceEquals(c1,c2) )
                    return true;

                if (((object)c1 == null) || ((object)c2 == null))
                    return false;

                return c1.Equals(c2);
            }

            public static bool operator !=(Context c1, Context c2)
            {
                return !(c1 == c2);
            }

            public override int GetHashCode()
            {
                return (int)(_precision + ((uint)_roundingMode) * 59);  // Same as Java
            }

            public override string ToString()
            {
                return String.Format(CultureInfo.InvariantCulture,"precision={0} roundingMode={1}", _precision, _roundingMode);
            }

            #endregion

            #region other

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "bi"), 
             System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            public bool RoundingNeeded(BigInteger bi)
            {
                // TODO: Really
                return true;
            }

            #endregion

            #region IEquatable<Context> methods

            public bool Equals(Context other)
            {
                return other._precision == _precision && other._roundingMode == _roundingMode;

            }

            #endregion
        }

        #endregion

        #region Data

        /// <summary>
        /// The coeffienct of this BigDecimal.
        /// </summary>
        BigInteger _coeff;

        /// <summary>
        /// The coeffienct of this BigDecimal.
        /// </summary>
        public BigInteger Coefficient
        {
            get { return _coeff; }
        }

        /// <summary>
        ///  The exponent of this BigDecimal.
        /// </summary>
        int _exp = 0;

        /// <summary>
        ///  The exponent of this BigDecimal.
        /// </summary>
        public int Exponent
        {
            get { return _exp; }
        }

        /// <summary>
        /// Get the precision (number of decimal digits) of this BigDecimal.
        /// </summary>
        /// <remarks>The value 0 indicated that the number is not known.</remarks>
        uint _precision = 0;

        /// <summary>
        /// Get the (number of decimal digits) of this BigDecimal.  Will trigger computation if not already known.
        /// </summary>
        /// <returns>The precision.</returns>
        public uint GetPrecision()
        {
                if (_precision == 0)
                {
                    if (_coeff.IsZero)
                        _precision = 1;
                    else
                        _precision = _coeff.Precision;
                }
                return _precision; 
        }


        private static readonly BigDecimal _zero = new BigDecimal(BigInteger.Zero, 0, 1);
        private static readonly BigDecimal _one = new BigDecimal(BigInteger.One, 0, 1);
        private static readonly BigDecimal _ten = new BigDecimal(BigInteger.Ten, 0, 2);

        /// <summary>
        /// A BigDecimal representation of zero with precision 1.
        /// </summary>
        public static BigDecimal Zero { get { return _zero; } }

        /// <summary>
        /// A BigDecimal representation of one.
        /// </summary>
        public static BigDecimal One { get { return _one; } }

        /// <summary>
        /// A BigDecimal representation of ten.
        /// </summary>
        public static BigDecimal Ten { get { return _ten; } }

        #endregion

        #region Factory methods

        // I went with factory methods rather than constructors so that I could, if I wanted,
        // return cached values for things such as zero, one, etc.


        /// <summary>
        /// Create a BigDecimal from a double.
        /// </summary>
        /// <param name="v">The double value</param>
        /// <returns>A BigDecimal corresponding to the double value.</returns>
        /// <remarks>Watch out!  BigDecimal.Create(0.1) is not the same as BigDecimal.Parse("0.1").  
        /// We create exact representations of doubles,
        /// and 1/10 does not have an exact representation as a double.  So the double 1.0 is not exactly 1/10.</remarks>
        public static BigDecimal Create(double v)
        {
            if (Double.IsNaN(v) || Double.IsInfinity(v))
            {
                throw new ArithmeticException("Infinity/NaN not supported in BigDecimal (yet)");
            }

            byte[] dbytes = System.BitConverter.GetBytes(v);
            ulong significand = BigInteger.GetDoubleSignificand(dbytes);
            int biasedExp = BigInteger.GetDoubleBiasedExponent(dbytes);
            int leftShift = biasedExp - BigInteger.DoubleShiftBias;

            BigInteger coeff;
            if (significand == 0)
            {
                if (biasedExp == 0)
                    return new BigDecimal(BigInteger.Zero, 0, 1);

                coeff = v < 0.0 ? BigInteger.NegativeOne : BigInteger.One;
                leftShift = biasedExp - BigInteger.DoubleExponentBias;
            }
            else
            {
                significand |= 0x10000000000000ul;
                coeff = BigInteger.Create(significand);
                // TODO: avoid extra allocation
                if (v < 0.0)
                    coeff = coeff * -1;
            }

            // at this point v = coeff * 2 ** exp
            // need to convert to appropriate exponent of 10.

            int expToUse = 0;
            if (leftShift < 0)
            {
                coeff = coeff.Multiply(BigInteger.Five.Power(-leftShift));
                expToUse = leftShift;
            }
            else if (leftShift > 0)
                coeff = coeff << leftShift;

            return new BigDecimal(coeff, expToUse);
        }

        /// <summary>
        /// Create a BigDecimal from a double, rounded as specified.
        /// </summary>
        /// <param name="v">The double value</param>
        /// <param name="c">The rounding context</param>
        /// <returns>A BigDecimal corresponding to the double value, rounded as specified.</returns>
        /// <remarks>Watch out!  BigDecimal.Create(0.1) is not the same as BigDecimal.Parse("0.1").  
        /// We create exact representations of doubles,
        /// and 1/10 does not have an exact representation as a double.  So the double 1.0 is not exactly 1/10.</remarks>
        public static BigDecimal Create(double v, Context c)
        {
            BigDecimal d = BigDecimal.Create(v);
            d.RoundInPlace(c);
            return d;
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given Int32.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <returns>A BigDecimal with the same value.</returns>
        public static BigDecimal Create(int v)
        {
            return new BigDecimal(BigInteger.Create(v), 0);
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given Int32, rounded appropriately.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <param name="c">The rounding context</param>
        /// <returns>A BigDecimal with the same value, appropriately rounded</returns>
        public static BigDecimal Create(int v, Context c)
        {
            BigDecimal d = new BigDecimal(BigInteger.Create(v), 0);
            d.RoundInPlace(c);
            return d;
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given Int64.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <returns>A BigDecimal with the same value.</returns>
        public static BigDecimal Create(long v)
        {
            return new BigDecimal(BigInteger.Create(v), 0);
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given Int64, rounded appropriately.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <param name="c">The rounding context</param>
        /// <returns>A BigDecimal with the same value, appropriately rounded</returns>
        public static BigDecimal Create(long v, Context c)
        {
            BigDecimal d = new BigDecimal(BigInteger.Create(v), 0);
            d.RoundInPlace(c);
            return d;
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given UInt64.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <returns>A BigDecimal with the same value.</returns>
        public static BigDecimal Create(ulong v)
        {
            return new BigDecimal(BigInteger.Create(v), 0);
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given UInt64, rounded appropriately.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <param name="c">The rounding context</param>
        /// <returns>A BigDecimal with the same value, appropriately rounded</returns>
        public static BigDecimal Create(ulong v, Context c)
        {
            BigDecimal d = new BigDecimal(BigInteger.Create(v), 0);
            d.RoundInPlace(c);
            return d;
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given Decimal.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <returns>A BigDecimal with the same value.</returns>
        public static BigDecimal Create(decimal v)
        {
            int[] bits = Decimal.GetBits(v);

            uint[] data = new uint[3];
            data[0] = (uint)bits[2];
            data[1] = (uint)bits[1];
            data[2] = (uint)bits[0];

            int sign = (bits[3] & 0x80000000) == 0 ? 1 : -1;
            int exp = (bits[3] & 0x00FF0000) >> 16;

            bool isZero = data[0] == 0U && data[1] == 0U && data[2] == 0U;

            BigInteger coeff = isZero ? BigInteger.Zero : new BigInteger(sign, data);

            return new BigDecimal(coeff, -exp);
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given UInt64, rounded appropriately.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <param name="c">The rounding context</param>
        /// <returns>A BigDecimal with the same value, appropriately rounded</returns>
        public static BigDecimal Create(decimal v, Context c)
        {
            BigDecimal d = Create(v);
            d.RoundInPlace(c);
            return d;
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given BigInteger.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <returns>A BigDecimal with the same value.</returns>
        public static BigDecimal Create(BigInteger v)
        {
            return new BigDecimal(v, 0);
        }

        /// <summary>
        /// Create a BigDecimal with the same value as the given BigInteger, rounded appropriately.
        /// </summary>
        /// <param name="v">The initial value</param>
        /// <param name="c">The rounding context</param>
        /// <returns>A BigDecimal with the same value, appropriately rounded</returns>
        public static BigDecimal Create(BigInteger v, Context c)
        {
            BigDecimal d = new BigDecimal(v, 0);
            d.RoundInPlace(c);
            return d;
        }

        /// <summary>
        /// Create a BigDecimal by parsing a string.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static BigDecimal Create(String v)
        {
            return BigDecimal.Parse(v);
        }


        /// <summary>
        /// Create a BigDecimal by parsing a string.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>        
        public static BigDecimal Create(String v, Context c)
        {
            return BigDecimal.Parse(v, c);
        }


        /// <summary>
        /// Create a BigDecimal by parsing a character array.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static BigDecimal Create(char[] v)
        {
            return BigDecimal.Parse(v);
        }


        /// <summary>
        /// Create a BigDecimal by parsing a character array.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static BigDecimal Create(char[] v, Context c)
        {
            return BigDecimal.Parse(v,c);
        }


        /// <summary>
        /// Create a BigDecimal by parsing a segment of character array.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static BigDecimal Create(char[] v, int offset, int len)
        {
            return BigDecimal.Parse(v,offset,len);
        }

        /// <summary>
        /// Create a BigDecimal by parsing a segment of character array.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static BigDecimal Create(char[] v, int offset, int len, Context c)
        {
            return BigDecimal.Parse(v, offset, len, c);
        }

        #endregion

        #region C-tors

        /// <summary>
        /// Creates a copy of given BigDecimal.
        /// </summary>
        /// <param name="copy">A copy of the given BigDecimal</param>
        /// <remarks>Really only needed internally.  BigDecimals are immutable, so why copy?  
        /// Internally, we sometimes need to copy and modify before releasing into the wild.</remarks>
        BigDecimal(BigDecimal copy)
            : this(copy._coeff,copy._exp,copy._precision)
        {
        }

        /// <summary>
        /// Create a BigDecimal with given coefficient, exponent, and precision.
        /// </summary>
        /// <param name="coeff">The coefficient</param>
        /// <param name="exp">The exponent</param>
        /// <param name="precision">The precision</param>
        /// <remarks>For internal use only.  We can't trust someone outside to set the precision for us.
        /// Only for use when we know the precision explicitly.</remarks>
        BigDecimal(BigInteger coeff, int exp, uint precision)
        {
            _coeff = coeff;
            _exp = exp;
            _precision = precision;
        }

        /// <summary>
        /// Create a BigDecimal with given coefficient and exponent.
        /// </summary>
        /// <param name="coeff">The coefficient</param>
        /// <param name="exp">The exponent</param>
        public BigDecimal(BigInteger coeff, int exp)
            : this(coeff, exp, 0)
        {
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            BigDecimal d = obj as BigDecimal;
            if (d == null)
                return false;

            return Equals(d);
        }

        public override int GetHashCode()
        {
            return Util.hashCombine(_coeff.GetHashCode(),_exp.GetHashCode());
        }

        #endregion

        #region String parsing

        /// <summary>
        /// Create a BigDecimal from a string representation
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static BigDecimal Parse(string s)
        {
            BigDecimal v;
            DoParse(s.ToCharArray(), 0, s.Length, true, out v);
            return v;
        }

        /// <summary>
        /// Create a BigDecimal from a string representation, rounded as indicated.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static BigDecimal Parse(string s, Context c)
        {
            BigDecimal v;
            DoParse(s.ToCharArray(), 0, s.Length, true, out v);
            v.RoundInPlace(c);
            return v;
        }

        /// <summary>
        /// Try to create a BigDecimal from a string representation.
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <param name="v">Set to the BigDecimal corresponding to the string.</param>
        /// <returns>True if successful, false if there is an error parsing.</returns>
        public static bool TryParse(string s, out BigDecimal v)
        {
            return DoParse(s.ToCharArray(),0, s.Length, false, out v);
        }


        /// <summary>
        /// Try to create a BigDecimal from a string representation, rounded as indicated.
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <param name="c">The rounding context</param>
        /// <param name="v">Set to the BigDecimal corresponding to the string.</param>
        /// <returns></returns>
        public static bool TryParse(string s, Context c, out BigDecimal v)
        {
            bool result;
            if ((result = DoParse(s.ToCharArray(), 0, s.Length, false, out v)))
                v.RoundInPlace(c);
            return result;
        }

        /// <summary>
        /// Create a BigDecimal from an array of characters.
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static BigDecimal Parse(char[] buf)
        {
            BigDecimal v;
            DoParse(buf, 0, buf.Length, true, out v);
            return v;
        }

        /// <summary>
        /// Create a BigDecimal from an array of characters, rounded as indicated.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static BigDecimal Parse(char[] buf, Context c)
        {
            BigDecimal v;
            DoParse(buf, 0, buf.Length,true, out v);
            v.RoundInPlace(c);
            return v;
        }

        /// <summary>
        /// Try to create a BigDecimal from an array of characters.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="v"></param>
        /// <returns>True if successful; false otherwise</returns>      
        public static bool TryParse(char[] buf, out BigDecimal v)
        {
            return DoParse(buf, 0, buf.Length, false, out v);
        }

        /// <summary>
        /// Try to create a BigDecimal from an array of characters, rounded as indicated.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="c"></param>
        /// <param name="v"></param>
        /// <returns>True if successful; false otherwise</returns>      
        public static bool TryParse(char[] buf, Context c, out BigDecimal v)
        {
            bool result;
            if ((result = DoParse(buf, 0, buf.Length, false, out v)))
                v.RoundInPlace(c);
            return result;
        }


        /// <summary>
        /// Create a BigDecimal corresponding to a sequence of characters from an array.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static BigDecimal Parse(char[] buf, int offset, int len)
        {
            BigDecimal v;
            DoParse(buf, offset, len, true, out v);
            return v;
        }

        /// <summary>
        /// Create a BigDecimal corresponding to a sequence of characters from an array, rounded as indicated.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static BigDecimal Parse(char[] buf, int offset, int len, Context c)
        {
            BigDecimal v;
            DoParse(buf, offset, len, true, out v);
            v.RoundInPlace(c);
            return v;
        }

        /// <summary>
        /// Try to create a BigDecimal corresponding to a sequence of characters from an array.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool TryParse(char[] buf, int offset, int len, out BigDecimal v)
        {
            return DoParse(buf, offset, len, false, out v);
        }

        /// <summary>
        /// Try to create a BigDecimal corresponding to a sequence of characters from an array.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <param name="c"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool TryParse(char[] buf, int offset, int len, Context c, out BigDecimal v)
        {
            bool result;
            if ((result = DoParse(buf, offset, len, false, out v)))
                v.RoundInPlace(c);
            return result;
        }

        /// <summary>
        /// Parse a substring of a character array as a BigDecimal.
        /// </summary>
        /// <param name="buf">The character array to parse</param>
        /// <param name="offset">Start index for parsing</param>
        /// <param name="len">Number of chars to parse.</param>
        /// <param name="throwOnError">If true, an error causes an exception to be thrown. If false, false is returned.</param>
        /// <param name="v">The BigDecimal corresponding to the characters.</param>
        /// <returns>True if successful, false if not (or throws if throwOnError is true).</returns>
        /// <remarks> Ugly. We could use a RegEx, but trying to avoid unnecessary allocation, I guess.
        /// [+-]?\d*(\.\d*)?([Ee][+-]?\d+)?  with additional constraint that one of the two d* must have at least one char.
        ///</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static bool DoParse(char[] buf, int offset, int len, bool throwOnError, out BigDecimal v)
        {
            v = null;

            // Make sure we have some characters
            if (len == 0)
            {
                if (throwOnError)
                    throw new FormatException("Empty string");
                return false;
            }

            // Make sure we're not going past the end of the array
            if (offset + len > buf.Length)
            {
                if (throwOnError)
                    throw new FormatException("offset+len past the end of the char array");
                return false;
            }


            int mainOffset = offset;

            // optional leading sign
            bool hasSign = false;
            char c = buf[offset];

            if (c == '-' || c == '+')
            {
                hasSign = true;
                offset++;
                len--;
            }

            // parse first set of digits
            for (; len > 0 && Char.IsDigit(buf[offset]); offset++, len--)
                ;
            int signedMainLen = offset - mainOffset;
            int mainLen = offset - mainOffset - (hasSign ? 1 : 0);

            // parse the optional fraction
            int fractionOffset = offset;
            int fractionLen = 0;

            if (len > 0 && buf[offset] == '.')
            {
                offset++;
                len--;
                fractionOffset = offset;
                for (; len > 0 && Char.IsDigit(buf[offset]); offset++, len--)
                    ;
                fractionLen = offset - fractionOffset;
            }
                
            // Parse the optional exponent.
            int expOffset = -1;
            int expLen = 0;

            if (len > 0 && (buf[offset] == 'e' || buf[offset] == 'E'))
            {
                offset++;
                len--;

                expOffset = offset;

                if (len == 0)
                {
                    if (throwOnError)
                        throw new FormatException("Missing exponent");
                    return false;
                }

                // Parse the optional sign;
                c = buf[offset];
                if (c == '-' || c == '+')
                {
                    offset++;
                    len--;
                }

                if (len == 0)
                {
                    if (throwOnError)
                        throw new FormatException("Missing exponent");
                    return false;
                }

                for (; len > 0 && Char.IsDigit(buf[offset]); offset++, len--)
                    ;
                expLen = offset - expOffset;
                
                if ( expLen == 0  )
                {
                    if (throwOnError)
                        throw new FormatException("Missing exponent");
                    return false;
                }
            }

            // we should be at the end
            if (len != 0)
            {
                if (throwOnError)
                    throw new FormatException("Unused characters at end");
                return false;
            }

            int precision =  mainLen + fractionLen;
            if (precision == 0)
            {
                if (throwOnError)
                    throw new FormatException("No digits in coefficient");
                return false;
            }
            char[] digits = new char[signedMainLen + fractionLen];

            Array.Copy(buf, mainOffset, digits, 0, signedMainLen);
            if ( fractionLen > 0 )
                Array.Copy(buf, fractionOffset, digits, signedMainLen, fractionLen);
            BigInteger val = BigInteger.Parse(new String(digits));

            int exp = 0;
            if (expLen > 0)
            {
                char[] expDigits = new char[expLen];
                Array.Copy(buf, expOffset, expDigits, 0, expLen);
                if (throwOnError)
                    exp = Int32.Parse(new String(expDigits), System.Globalization.CultureInfo.InvariantCulture);
                else
                {
                    if (!Int32.TryParse(new String(expDigits), System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out exp))
                        return false;
                }
            }

            int expToUse = mainLen-precision;
            if ( exp != 0 )
                try
                {
                    expToUse = CheckExponent(expToUse + exp, val.IsZero);
                }
                catch (ArithmeticException)
                {
                    if ( throwOnError)
                        throw;
                    return false;
                }

            // Remove leading zeros from precision count.
            for (int i = (hasSign ? 1 : 0); i < signedMainLen + fractionLen && precision > 1 && digits[i] == '0'; i++)
                precision--;

            v = new BigDecimal(val, expToUse, (uint)precision);
            return true;
        }

        #endregion

        #region Conversion to string

        /// <summary>
        /// Create the canonical string representation for a BigDecimal.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int64.ToString")]
        public string ToScientificString()
        {
            StringBuilder sb = new StringBuilder(_coeff.ToString());
            int coeffLen = sb.Length;
            int negOffset = 0;
            if (_coeff.IsNegative)
            {
                coeffLen--;
                negOffset = 1;
            }

            long adjustedExp = (long)_exp + (coeffLen - 1);
            if (_exp <= 0 && adjustedExp >= -6)
            {
                // not using exponential notation

                if (_exp != 0)
                {
                    // We do need a decimal point.
                    int numDec = -_exp;
                    if (numDec < coeffLen)
                        sb.Insert(coeffLen - numDec + negOffset, '.');
                    else if (numDec == coeffLen)
                        sb.Insert(negOffset, "0.");
                    else
                    {
                        int numZeros = numDec - coeffLen;
                        sb.Insert(negOffset, "0", numZeros);
                        sb.Insert(negOffset, "0.");
                    }
                }
            }
            else
            {
                // using exponential notation
                if (coeffLen > 1)
                    sb.Insert(negOffset+1, '.');
                sb.Append('E');
                if (adjustedExp >= 0)
                    sb.Append('+');
                sb.Append(adjustedExp.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Return a string representing the BigDecimal value.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToScientificString();
        }

        //public string ToEngineeringString()
        //{

        //}


        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            BigDecimal d = obj as BigDecimal;

            if (Object.ReferenceEquals(d, null))
                throw new ArgumentException("Expected a BigDecimal to compare against");

            return CompareTo(d);
        }

        #endregion

        #region IComparable<BigDecimal> Members

        public int CompareTo(BigDecimal other)
        {
            BigDecimal d1 = this;
            BigDecimal d2 = other;
            Align(ref d1, ref d2);
            return d1._coeff.CompareTo(d2._coeff);
        }

        #endregion

        #region IEquatable<BigDecimal> Members

        public bool Equals(BigDecimal other)
        {
            if ( Object.ReferenceEquals(other,null) )
                return false;
            if (_exp != other._exp)
                return false;
            return _coeff.Equals(other._coeff);
        }

        #endregion

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return !IsZero;
        }

        public byte ToByte(IFormatProvider provider)
        {
            return ToBigInteger().ToByte(provider);
        }

        public char ToChar(IFormatProvider provider)
        {
            return ToBigInteger().ToChar(provider);
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            // TODO: improve this.
            // Ideally, we create a string with no exponent, provided we are in decimal range.
            return Convert.ToDecimal(ToDouble(provider), provider);
        }

        public double ToDouble(IFormatProvider provider)
        {
            // As j.m.BigDecimal puts it: "Somewhat inefficient, but guaranteed to work."
            // However, JVM's double parser goes to +/- Infinity when out of range,
            // while CLR's throws an exception.
            // Hate dealing with that.
            try
            {
                return Double.Parse(ToString(), provider);
            }
            catch (OverflowException)
            {
                return IsNegative ? Double.NegativeInfinity : Double.PositiveInfinity;
            }
        }

        public short ToInt16(IFormatProvider provider)
        {
            return ToBigInteger().ToInt16(provider);
        }

        public int ToInt32(IFormatProvider provider)
        {
            return ToBigInteger().ToInt32(provider);
        }

        public long ToInt64(IFormatProvider provider)
        {
            return ToBigInteger().ToInt64(provider);
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return ToBigInteger().ToSByte(provider);
        }

        public float ToSingle(IFormatProvider provider)
        {
            return (float)ToDouble(provider);
        }

        public string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(ToDouble(provider), conversionType, provider);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return ToBigInteger().ToUInt16(provider);
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return ToBigInteger().ToUInt32(provider);
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return ToBigInteger().ToUInt64(provider);
        }

        public BigInteger ToBigInteger()
        {
            return Rescale(this, 0, RoundingMode.Down)._coeff;
        }

        #endregion

        #region Arithmetic operators

        public static bool operator ==(BigDecimal x, BigDecimal y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (((object)x == null) || ((object)y == null))
                return false; 

            return x.Equals(y);
        }

        public static bool operator !=(BigDecimal x, BigDecimal y)
        {
            return !(x == y);
        }

        public static bool operator <(BigDecimal x, BigDecimal y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator >(BigDecimal x, BigDecimal y)
        {
            return x.CompareTo(y) > 0;
        }


        /// <summary>
        /// Compute <paramref name="x"/> + <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The sum</returns>
        public static BigDecimal operator +(BigDecimal x, BigDecimal y)
        {
            return x.Add(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> - <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The difference</returns>
        public static BigDecimal operator -(BigDecimal x, BigDecimal y)
        {
            return x.Subtract(y);
        }

        /// <summary>
        /// Compute the negation of <paramref name="x"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The negation</returns>
        public static BigDecimal operator -(BigDecimal x)
        {
            return x.Negate();
        }

        /// <summary>
        /// Compute <paramref name="x"/> * <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The product</returns>
        public static BigDecimal operator *(BigDecimal x, BigDecimal y)
        {
            return x.Multiply(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> / <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The quotient</returns>
        public static BigDecimal operator /(BigDecimal x, BigDecimal y)
        {
            return x.Divide(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> % <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The modulus</returns>
        public static BigDecimal operator %(BigDecimal x, BigDecimal y)
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
        public static BigDecimal Add(BigDecimal x, BigDecimal y)
        {
            return x.Add(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> + <paramref name="y"/> with the result rounded per the context.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The sum</returns>
        public static BigDecimal Add(BigDecimal x, BigDecimal y, Context c)
        {
            return x.Add(y,c);
        }

        /// <summary>
        /// Compute <paramref name="x"/> - <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The difference</returns>
        public static BigDecimal Subtract(BigDecimal x, BigDecimal y)
        {
            return x.Subtract(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> - <paramref name="y"/> with the result rounded per the context.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="mc"></param>
        /// <returns></returns>
        public static BigDecimal Subtract(BigDecimal x, BigDecimal y, Context c)
        {
            return x.Subtract(y,c);
        }

        /// <summary>
        /// Compute the negation of <paramref name="x"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The negation</returns>
        public static BigDecimal Negate(BigDecimal x)
        {
            return x.Negate();
        }

        /// <summary>
        /// Compute the negation of <paramref name="x"/>, with result rounded according to the context
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The negation</returns>
        public static BigDecimal Negate(BigDecimal x, Context c)
        {
            return x.Negate(c);
        }

        /// <summary>
        /// Compute <paramref name="x"/> * <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The product</returns>
        public static BigDecimal Multiply(BigDecimal x, BigDecimal y)
        {
            return x.Multiply(y);
        }


        /// <summary>
        /// Compute <paramref name="x"/> * <paramref name="y"/>, with result rounded according to the context.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The product</returns>
        public static BigDecimal Multiply(BigDecimal x, BigDecimal y, Context c)
        {
            return x.Multiply(y,c);
        }

        /// <summary>
        /// Compute <paramref name="x"/> / <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The quotient</returns>
        public static BigDecimal Divide(BigDecimal x, BigDecimal y)
        {
            return x.Divide(y);
        }

        /// <summary>
        /// Compute <paramref name="x"/> / <paramref name="y"/>, with result rounded according to the context.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The quotient</returns>
        public static BigDecimal Divide(BigDecimal x, BigDecimal y, Context c)
        {
            return x.Divide(y,c);
        }

        /// <summary>
        /// Returns <paramref name="x"/> % <paramref name="y"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The modulus</returns>
        public static BigDecimal Mod(BigDecimal x, BigDecimal y)
        {
            return x.Mod(y);
        }


        /// <summary>
        /// Returns <paramref name="x"/> % <paramref name="y"/>, with result rounded according to the context.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>The modulus</returns>
        public static BigDecimal Mod(BigDecimal x, BigDecimal y, Context c)
        {
            return x.Mod(y,c);
        }

        /// <summary>
        /// Compute the quotient and remainder of dividing one <see cref="BigInteger"/> by another.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="remainder">Set to the remainder after division</param>
        /// <returns>The quotient</returns>
        public static BigDecimal DivRem(BigDecimal x, BigDecimal y, out BigDecimal remainder)
        {
            return x.DivRem(y, out remainder);
        }
        
        /// <summary>
        /// Compute the quotient and remainder of dividing one <see cref="BigInteger"/> by another, with result rounded according to the context.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="remainder">Set to the remainder after division</param>
        /// <returns>The quotient</returns>
        public static BigDecimal DivRem(BigDecimal x, BigDecimal y, Context c, out BigDecimal remainder)
        {
            return x.DivRem(y, c, out remainder);
        }


        /// <summary>
        /// Compute the absolute value.
        /// </summary>
        /// <param name="x"></param>
        /// <returns>The absolute value</returns>
        public static BigDecimal Abs(BigDecimal x)
        {
            return x.Abs(); ;
        }


        /// <summary>
        /// Compute the absolute value, with result rounded according to the context.
        /// </summary>
        /// <param name="x"></param>
        /// <returns>The absolute value</returns>
        public static BigDecimal Abs(BigDecimal x, Context c)
        {
            return x.Abs(c); ;
        }

        /// <summary>
        /// Returns a <see cref="BigInteger"/> raised to an int power.
        /// </summary>
        /// <param name="x">The value to exponentiate</param>
        /// <param name="exp">The exponent</param>
        /// <returns>The exponent</returns>
        public static BigDecimal Power(BigDecimal x, int exp)
        {
            return x.Power(exp);
        }


        /// <summary>
        /// Returns a <see cref="BigInteger"/> raised to an int power, with result rounded according to the context.
        /// </summary>
        /// <param name="x">The value to exponentiate</param>
        /// <param name="exp">The exponent</param>
        /// <returns>The exponent</returns>
        public static BigDecimal Power(BigDecimal x, int exp, Context c)
        {
            return x.Power(exp,c);
        }


        /// <summary>
        /// Returns this.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static BigDecimal Plus(BigDecimal x)
        {
            return x;
        }

        public static BigDecimal Plus(BigDecimal x, Context c)
        {
            return x.Plus(c);
        }

        /// <summary>
        /// Returns the negation of this.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static BigDecimal Minus(BigDecimal x)
        {
            return x.Negate();
        }

        public static BigDecimal Minus(BigDecimal x, Context c)
        {
            return x.Negate(c);
        }


        #endregion

        #region Arithmetic methods

        /// <summary>
        /// Returns this + y.
        /// </summary>
        /// <param name="y">The augend.</param>
        /// <returns>The sum</returns>
        public BigDecimal Add(BigDecimal y)
        {
            BigDecimal x = this;
            Align(ref x, ref y);

            return new BigDecimal(x._coeff + y._coeff, x._exp);
        }


        /// <summary>
        /// Returns this + y, with the result rounded according to the context.
        /// </summary>
        /// <param name="y">The augend.</param>
        /// <returns>The sum</returns>
        /// <remarks>Translated the Sun Java code pretty directly.</remarks>
        public BigDecimal Add(BigDecimal y, Context c)
        {
            // TODO: Optimize for one arg or the other being zero.
            // TODO: Optimize for differences in exponent along with the desired precision is large enough that the add is irreleveant
            BigDecimal result = Add(y);

            if (c.Precision == 0 || c.RoundingMode == RoundingMode.Unnecessary)
                return result;

            return result.Round(c);
        }
    
        /// <summary>
        /// Change either x or y by a power of 10 in order to align them.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        static void Align(ref BigDecimal x, ref BigDecimal y)
        {
            if (y._exp > x._exp)
                y = ComputeAlign(y, x);
            else if (x._exp > y._exp)
                x = ComputeAlign(x, y);
        }

        /// <summary>
        /// Modify a larger BigDecimal to have the same exponent as a smaller one by multiplying the coefficient by a power of 10.
        /// </summary>
        /// <param name="big"></param>
        /// <param name="small"></param>
        /// <returns></returns>
        static BigDecimal ComputeAlign(BigDecimal big, BigDecimal small)
        {
            int deltaExp = big._exp - small._exp;
            return new BigDecimal(big._coeff.Multiply(BIPowerOfTen(deltaExp)), small._exp);            
        }


        /// <summary>
        /// Returns this - y
        /// </summary>
        /// <param name="y">The subtrahend</param>
        /// <returns>The difference</returns>
        public BigDecimal Subtract(BigDecimal y)
        {
            BigDecimal x = this;
            Align(ref x, ref y);

            return new BigDecimal(x._coeff - y._coeff, x._exp);
        }

        /// <summary>
        /// Returns this - y
        /// </summary>
        /// <param name="y">The subtrahend</param>
        /// <returns>The difference</returns>
        public BigDecimal Subtract(BigDecimal y, Context c)
        {
            // TODO: Optimize for one arg or the other being zero.
            // TODO: Optimize for differences in exponent along with the desired precision is large enough that the add is irreleveant
            BigDecimal result = Subtract(y);

            if (c.Precision == 0 || c.RoundingMode == RoundingMode.Unnecessary)
                return result;

            return result.Round(c);
        }

        /// <summary>
        /// Returns the negation of this value.
        /// </summary>
        /// <returns>The negation</returns>
        public BigDecimal Negate()
        {
            if (_coeff.IsZero)
                return this;
            return new BigDecimal(_coeff.Negate(), _exp, _precision);
        }

        /// <summary>
        /// Returns the negation of this value, with result rounded according to the context.
        /// </summary>
        /// <param name="mc"></param>
        /// <returns></returns>
        public BigDecimal Negate(Context c)
        {
            return Round(Negate(),c);
        }


        /// <summary>
        /// Returns this * y
        /// </summary>
        /// <param name="y">The multiplicand</param>
        /// <returns>The product</returns>
        public BigDecimal Multiply(BigDecimal y)
        {
            return new BigDecimal(_coeff.Multiply(y._coeff), _exp + y._exp);
        }

        /// <summary>
        /// Returns this * y
        /// </summary>
        /// <param name="y">The multiplicand</param>
        /// <returns>The product</returns>
        public BigDecimal Multiply(BigDecimal y, Context c)
        {
            BigDecimal d = Multiply(y);
            d.RoundInPlace(c);
            return d;
        }

        /// <summary>
        /// Returns this / y.
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <returns>The quotient</returns>
        /// <exception cref="ArithmeticException">If rounding mode is RoundingMode.UNNECESSARY and we have a repeating fraction"</exception>
        /// <remarks>I completely ripped off the OpenJDK implementation.  
        /// Their analysis of the basic algorithm I could not compete with.</remarks>
        public BigDecimal Divide(BigDecimal divisor)
        {
            BigDecimal dividend = this;

            if ( divisor._coeff.IsZero ) // x/0
            { 
                if ( dividend._coeff.IsZero ) // 0/0
                    throw new ArithmeticException("Division undefined (0/0)"); // NaN
                throw new ArithmeticException("Division by zero"); // INF
            }

            // Calculate preferred exponent
            int preferredExp = 
                (int)Math.Max(Math.Min((long)dividend._exp - divisor._exp,
                                        Int32.MaxValue),
                              Int32.MinValue);

            if ( dividend._coeff.IsZero )  // 0/y
                return new BigDecimal(BigInteger.Zero,preferredExp);



            /*  OpenJDK says:
             * If the quotient this/divisor has a terminating decimal
             * expansion, the expansion can have no more than
             * (a.precision() + ceil(10*b.precision)/3) digits.
             * Therefore, create a MathContext object with this
             * precision and do a divide with the UNNECESSARY rounding
             * mode.
             */
            Context c = new Context( (uint)Math.Min(dividend.GetPrecision() +
                                                   (long)Math.Ceiling(10.0*divisor.GetPrecision()/3.0),
                                                            Int32.MaxValue),
                                              RoundingMode.Unnecessary);
            BigDecimal quotient;
            try
            {
                quotient = dividend.Divide(divisor, c);
            }
            catch (ArithmeticException )
            {
                throw new ArithmeticException("Non-terminating decimal expansion; no exact representable decimal result.");
            }

            int quotientExp = quotient._exp;

            // divide(BigDecimal, mc) tries to adjust the quotient to
            // the desired one by removing trailing zeros; since the
            // exact divide method does not have an explicit digit
            // limit, we can add zeros too.

            if (preferredExp < quotientExp)
                return Rescale(quotient, preferredExp, RoundingMode.Unnecessary);

            return quotient;
        }


        /// <summary>
        /// Returns this / y.
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <param name="mc">The context</param>
        /// <returns>The quotient</returns>
        /// <remarks>
        /// <para>The specification talks about the division algorithm in terms of repeated subtraction.
        /// I'll try to re-analyze this in terms of divisions on integers.</para>
        /// <para>Assume we want to divide one BigDecimal by another:</para>
        /// <code> [x,a] / [y,b] = [(x/y), a-b]</code>
        /// <para>where [x,a] signifies x is integer, a is exponent so [x,a] has value x * 10^a.
        /// Here, (x/y) indicates a result rounded to the desired precision p. For the moment, assume x, y non-negative.</para>
        /// <para>We want to compute (x/y) using integer-only arithmetic, yielding a quotient+remainder q+r
        /// where q has up to p precision and r is used to compute the rounding.  So actually, the result will be [q, a-b+c],
        /// where c is some adjustment factor to make q be in the range [0,10^0).</para>
        /// <para>We will need to adjust either x or y to make sure we can compute x/y and make q be in this range.</para>
        /// <para>Let px be the precision of x (number of digits), let py be the precision of y. Then </para>
        /// <code>
        /// x = x' * 10^px
        /// y = y' * 10^py
        /// </code>
        /// <para>where x' and y' are in the range [.1,1).  However, we'd really like to have:</para>
        /// <code>
        /// (a) x' in [.1,1)
        /// (b) y' in [x',10*x')
        /// </code>
        /// <para>So that  x'/y' is in the range (.1,1].  
        /// We can use y' as defined above if y' meets (b), else multiply y' by 10 (and decrease py by 1). 
        /// Having done this, we now have</para>
        /// <code>
        ///  x/y = (x'/y') * 10^(px-py)
        /// </code>
        /// <para>
        /// This gives us  10^(px-py-1) &lt; x/y &lt 10^(px-py).
        /// We'd like q to have p digits of precision.  So,
        /// <code>
        /// if px-py = p, ok.
        /// if px-py &lt; p, multiply x by 10^(p - (px-py)).
        /// if px-py &gt; p, multiply y by 10^(px-py-p).
        /// </code>
        /// <para>Using these adjusted values of x and y, divide to get q and r, round using those, then adjust the exponent.</para>
        /// </remarks>
        public BigDecimal Divide(BigDecimal rhs, Context c)
        {
            if (c.Precision == 0)
                return Divide(rhs);

            BigDecimal lhs = this;

            long preferredExp = (long)lhs._exp - rhs._exp;

            // Deal with x or y being zero.

            if (rhs._coeff.IsZero)
            {      // x/0
                if (lhs._coeff.IsZero)    // 0/0
                    throw new ArithmeticException("Division undefined");  // NaN
                throw new ArithmeticException("Division by zero");  // Inf
            }
            if (lhs._coeff.IsZero)        // 0/y
                return new BigDecimal(BigInteger.Zero,
                                      (int)Math.Max(Math.Min(preferredExp,
                                                             Int32.MaxValue),
                                                    Int32.MinValue));
            int xprec = (int)lhs.GetPrecision();
            int yprec = (int)rhs.GetPrecision();

            // Determine if we need to make an adjustment to get x', y' into relation (b).
            BigInteger x = lhs._coeff;
            BigInteger y = rhs._coeff;

            BigInteger xtest = x.Abs();
            BigInteger ytest = y.Abs();
            if (xprec < yprec)
                xtest = x.Multiply(BIPowerOfTen(yprec - xprec));
            else if (xprec > yprec)
                ytest = y.Multiply(BIPowerOfTen(xprec - yprec));


            int adjust = 0;
            if (ytest < xtest)
            {
                y = y.Multiply(BigInteger.Ten);
                adjust = 1;
            }

            // Now make sure x and y themselves are in the proper range.

            int delta = (int)c.Precision - (xprec - yprec);
            if ( delta > 0 )
                x = x.Multiply(BIPowerOfTen(delta));
            else if ( delta < 0 )
                y = y.Multiply(BIPowerOfTen(-delta));

            BigInteger roundedInt = RoundingDivide2(x, y, c.RoundingMode);

            int exp = CheckExponent(preferredExp - delta + adjust, roundedInt.IsZero);

            BigDecimal result = new BigDecimal(roundedInt, exp);

            result.RoundInPlace(c);


            // Thanks to the OpenJDK implementation for pointing this out.
            // TODO: Have ROundingDivide2 return a flag indicating if the remainder is 0.  Then we can lose the multiply.
            if (result.Multiply(rhs).CompareTo(this) == 0)
            {
                // Apply preferred scale rules for exact quotients
                return result.StripZerosToMatchExponent(preferredExp);
            }
            else
            {
                return result;
            }      

    
           // if (c.RoundingMode == RoundingMode.Ceiling ||
           //     c.RoundingMode == RoundingMode.Floor)
           // {
           //     // OpenJDK code says:
           //     // The floor (round toward negative infinity) and ceil
           //     // (round toward positive infinity) rounding modes are not
           //     // invariant under a sign flip.  If xprime/yprime has a
           //     // different sign than lhs/rhs, the rounding mode must be
           //     // changed.
           //     if ((xprime._coeff.Signum != lhs._coeff.Signum) ^
           //         (yprime._coeff.Signum != rhs._coeff.Signum))
           //     {
           //         c = new Context(c.Precision,
           //                              (c.RoundingMode == RoundingMode.Ceiling) ?
           //                              RoundingMode.Floor : RoundingMode.Ceiling);
           //     }
           // }
        }



        /// <summary>
        /// Returns this % y
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <returns>The modulus</returns>
        public BigDecimal Mod(BigDecimal y)
        {
            BigDecimal r;
            DivRem(y,out r);
            return r;
        }


        /// <summary>
        /// Returns this % y
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <returns>The modulus</returns>
        public BigDecimal Mod(BigDecimal y, Context c)
        {
            BigDecimal r;
            DivRem(y, c, out r);
            return r;
        }

        /// <summary>
        /// Returns the quotient and remainder of this divided by another.
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <param name="remainder">The remainder</param>
        /// <returns>The quotient</returns>
        public BigDecimal DivRem(BigDecimal y, out BigDecimal remainder)
        {
            // x = q * y + r
            BigDecimal q = this.DivideInteger(y);
            remainder = this - q * y;
            return q;
        }


        /// <summary>
        /// Returns the quotient and remainder of this divided by another.
        /// </summary>
        /// <param name="y">The divisor</param>
        /// <param name="remainder">The remainder</param>
        /// <returns>The quotient</returns>
        public BigDecimal DivRem(BigDecimal y, Context c, out BigDecimal remainder)
        {
            // x = q * y + r
            if (c.RoundingMode == RoundingMode.Unnecessary)
                return DivRem(y, out remainder);

            BigDecimal q = this.DivideInteger(y,c);
            remainder = this - q * y;
            return q;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="y"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        /// <remarks>I am indebted to the OpenJDK implementation for the algorithm.
        /// <para>However, the spec I'm working from specifies an exponent of zero always!
        /// The OpenJDK implementation does otherwise.  So I've modified it to yield a zero exponent.</para>
        /// </remarks>
        public BigDecimal DivideInteger(BigDecimal y, Context c)
        {
            if (c.Precision == 0 ||                        // exact result
                (this.Abs().CompareTo(y.Abs()) < 0)) // zero result
                return DivideInteger(y);

            // Calculate preferred scale
            //int preferredExp = (int)Math.Max(Math.Min((long)this._exp - y._exp,
            //                                            Int32.MaxValue),Int32.MinValue);
            int preferredExp = 0;

            /*  OpenJKD says:
             * Perform a normal divide to mc.precision digits.  If the
             * remainder has absolute value less than the divisor, the
             * integer portion of the quotient fits into mc.precision
             * digits.  Next, remove any fractional digits from the
             * quotient and adjust the scale to the preferred value.
             */
            BigDecimal result = this.Divide(y, new Context(c.Precision,RoundingMode.Down));
            int resultExp = result._exp;

            if (resultExp > 0)
            {
                /*
                 * Result is an integer. See if quotient represents the
                 * full integer portion of the exact quotient; if it does,
                 * the computed remainder will be less than the divisor.
                 */
                BigDecimal product = result.Multiply(y);
                // If the quotient is the full integer value,
                // |dividend-product| < |divisor|.
                if (this.Subtract(product).Abs().CompareTo(y.Abs()) >= 0)
                {
                    throw new ArithmeticException("Division impossible");
                }
            }
            else if (resultExp < 0)
            {
                /*
                 * Integer portion of quotient will fit into precision
                 * digits; recompute quotient to scale 0 to avoid double
                 * rounding and then try to adjust, if necessary.
                 */
                result = Rescale(result,0, RoundingMode.Down);
            }
            // else resultExp == 0;

            //int precisionDiff;
            if ((preferredExp < resultExp) &&
                (/*precisionDiff = */(int)(c.Precision - result.GetPrecision())) > 0)
            {
                //return Rescale(result, resultExp + Math.Max(precisionDiff, preferredExp - resultExp), RoundingMode.Unnecessary);
                return Rescale(result, 0, RoundingMode.Unnecessary);

            }
            else
                return result.StripZerosToMatchExponent(preferredExp);
        }


        /// <summary>
        /// Return the integer part of this / y.
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        /// <remarks>I am indebted to the OpenJDK implementation for the algorithm.
        /// <para>However, the spec I'm working from specifies an exponent of zero always!
        /// The OpenJDK implementation does otherwise.  So I've modified it to yield a zero exponent.</para>
        /// </remarks>
        public BigDecimal DivideInteger(BigDecimal y)
        {

            // Calculate preferred exponent
            //int preferredExp = (int)Math.Max(Math.Min((long)this._exp - y._exp,
            //                                            Int32.MaxValue), Int32.MinValue);
            int preferredExp = 0;

            if (Abs().CompareTo(y.Abs()) < 0)
            {
                return new BigDecimal(BigInteger.Zero, preferredExp);
            }

            if (this._coeff.IsZero && !y._coeff.IsZero)
                return Rescale(this, preferredExp, RoundingMode.Unnecessary);

            // Perform a divide with enough digits to round to a correct
            // integer value; then remove any fractional digits

            int maxDigits = (int)Math.Min(this.GetPrecision() +
                                          (long)Math.Ceiling(10.0 * y.GetPrecision() / 3.0) +
                                          Math.Abs((long)this._exp - y._exp) + 2,
                                          Int32.MaxValue);

            BigDecimal quotient = this.Divide(y, new Context((uint)maxDigits, RoundingMode.Down));
            if (y._exp < 0)
            {
                quotient = Rescale(quotient, 0, RoundingMode.Down).StripZerosToMatchExponent(preferredExp);
            }

            if (quotient._exp > preferredExp)
            {
                // pad with zeros if necessary
                quotient = Rescale(quotient, preferredExp, RoundingMode.Unnecessary);
            }

            return quotient;
        }        

        /// <summary>
        /// Returns the absolute value of this instance.
        /// </summary>
        /// <returns>The absolute value</returns>
        public BigDecimal Abs()
        {
            if (_coeff.IsNegative)
                return Negate();
            return this;
        }

        /// <summary>
        /// Returns the absolute value of this instance.
        /// </summary>
        /// <returns>The absolute value</returns>
        public BigDecimal Abs(Context c)
        {
            if (_coeff.IsNegative)
                return Negate(c);
            return Round(this,c);
        }



        /// <summary>
        /// Returns the value of this instance raised to an integral power.
        /// </summary>
        /// <param name="exp">The exponent</param>
        /// <returns>The exponetiated value</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the exponent is negative.</exception>
        public BigDecimal Power(int n)
        {
            if (n < 0 || n > 999999999)
                throw new ArithmeticException("Invalid operation");

            int exp = CheckExponent((long)_exp * n);
            return new BigDecimal(_coeff.Power(n), exp);
        }

        /// <summary>
        /// Returns the value of this instance raised to an integral power.
        /// </summary>
        /// <param name="exp">The exponent</param>
        /// <returns>The exponetiated value</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the exponent is negative.</exception>
        /// <remarks><para>Follows the OpenJKD implementation.  This is an implementation of the X3.274-1996 algorithm:</para>
        /// <list>
        ///   <item> An ArithmeticException exception is thrown if
        ///     <list>
        ///       <item>abs(n) > 999999999</item>
        ///       <item>c.precision == 0 and code n &lt; 0</item>
        ///       <item>c.precision > 0 and n has more than c.precision decimal digits</item>
        ///     </list>
        ///   </item>
        ///   <item>if n is zero, ONE is returned even if this is zero, otherwise
        ///     <list>        
        ///       <item>if n is positive, the result is calculated via
        ///             the repeated squaring technique into a single accumulator.
        ///             The individual multiplications with the accumulator use the
        ///             same math context settings as in c except for a
        ///             precision increased to c.precision + elength + 1
        ///             where elength is the number of decimal digits in n.
        ///       </item>       
        ///       <item>if n is negative, the result is calculated as if
        ///             n were positive; this value is then divided into one
        ///             using the working precision specified above.
        ///       </item>
        ///       <item>The final value from either the positive or negative case
        ///              is then rounded to the destination precision.
        ///        </item>
        ///     </list>
        ///  </list>
        /// </remarks>
        public BigDecimal Power(int n, Context c)
        {
            if (c.Precision == 0)
                return Power(n);
            if (n < -999999999 || n > 999999999)
                throw new ArithmeticException("Invalid operation");
            if (n == 0)
                return One;                      
            BigDecimal lhs = this;
            Context workc = c;           
            int mag = Math.Abs(n);               
            if (c.Precision > 0)
            {
                int elength = (int)BigInteger.UIntPrecision((uint)mag);
                if (elength > c.Precision)        // X3.274 rule
                    throw new ArithmeticException("Invalid operation");
                workc = new Context((uint)(c.Precision + elength + 1),c.RoundingMode);
            }

            BigDecimal acc = One;           
            bool seenbit = false;        
            for (int i = 1; ; i++)
            {            
                mag += mag;                 // shift left 1 bit
                if (mag < 0)
                {              // top bit is set
                    seenbit = true;         // OK, we're off
                    acc = acc.Multiply(lhs, workc); // acc=acc*x
                }
                if (i == 31)
                    break;                  // that was the last bit
                if (seenbit)
                    acc = acc.Multiply(acc, workc);   // acc=acc*acc [square]
                // else (!seenbit) no point in squaring ONE
            }
            // if negative n, calculate the reciprocal using working precision
            if (n < 0)                          // [hence mc.precision>0]
                acc = One.Divide(acc, workc);
            // round to final precision and strip zeros
            return acc.Round(c);
        }


        public BigDecimal Plus()
        {
            return this;
        }

        public BigDecimal Plus(Context c)
        {
            if ( c.Precision == 0 )
                return this;
            return this.Round(c);
        }

        
        public BigDecimal Minus()
        {
            return Negate();
        }

        public BigDecimal Minus(Context c)
        {
            return Negate(c);
        }
        

        #endregion

        #region Other computed values

        /// <summary>
        /// Does this BigDecimal have a zero value?
        /// </summary>
        public bool IsZero
        {
            get { return _coeff.IsZero; }
        }

        /// <summary>
        /// Does this BigDecimal represent a positive value?
        /// </summary>
        public bool IsPositive
        {
            get { return _coeff.IsPositive; }
        }

        /// <summary>
        /// Does this BigDecimal represent a negative value?
        /// </summary>
        public bool IsNegative
        {
            get { return _coeff.IsNegative; }
        }

        /// <summary>
        /// Returns the sign (-1, 0, +1) of this BigDecimal.
        /// </summary>
        public int Signum
        {
            get { return _coeff.Signum; }
        }


        public BigDecimal MovePointRight(int n)
        {
            int newExp = CheckExponent((long)_exp + n);
            BigDecimal d = new BigDecimal(_coeff, newExp);
            return d;
        }

        public BigDecimal MovePointLeft(int n)
        {
            int newExp = CheckExponent((long)_exp - n);
            BigDecimal d = new BigDecimal(_coeff, newExp);
            return d;
        }

        



        #endregion

        #region Exponent computations

        /// <summary>
        /// Check to see if the result of exponent arithmetic is valid.
        /// </summary>
        /// <param name="candidate">The value resulting from exponent arithmetic.</param>
        /// <param name="isZero">Are we computing an exponent for a zero coefficient?</param>
        /// <param name="exponent">The exponent to use</param>
        /// <returns>True if the candidate is valid, false otherwise.</returns>
        /// <remarks>
        /// <para>Exponent arithmetic during various operations may result in values
        /// that are out of range of an Int32.  We can do the computation as a long,
        /// then use this to make sure the result is okay to use.</para>
        /// <para>If the exponent is out of range, but the coefficient is zero,
        /// the exponent in some sense is not that relevant, so we just clamp to 
        /// the appropriate (pos/neg) extreme value for Int32.  (This handling inspired by 
        /// the OpenJKD implementation.)</para>
        /// </remarks>
        static bool CheckExponent(long candidate, bool isZero, out int exponent)
        {
            exponent = (int)candidate;
            if (exponent == candidate)
                return true;

            // We have underflow/overflow.
            // If Zero, use the max value of the appropriate sign.
            if (isZero)
            {
                exponent = candidate > Int32.MaxValue ? Int32.MaxValue : Int32.MinValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reduce exponent to Int32.  Throw error if out of range.
        /// </summary>
        /// <param name="candidate">The value resulting from exponent arithmetic.</param>
        /// <param name="isZero">Are we computing an exponent for a zero coefficient?</param>
        /// <returns>The exponent to use</returns>
        static int CheckExponent(long candidate, bool isZero)
        {
            int exponent;

            bool result = CheckExponent(candidate, isZero, out exponent);
            if (result)
                return exponent;

            // Report error condition
            if (candidate > Int32.MaxValue)
                throw new ArithmeticException("Overflow in scale");
            else
                throw new ArithmeticException("Underflow in scale");
        }


        bool CheckExponent(long candidate, out int exponent)
        {
            return CheckExponent(candidate, _coeff.IsZero, out exponent);
        }

        int CheckExponent(long candidate)
        {
            return CheckExponent(candidate, _coeff.IsZero);
        }
  
        static BigInteger BIPowerOfTen(int n)
        {
            if ( n < 0 )
                throw new ArgumentException("Power of ten must be non-negative");

            if (n < _maxCachedPowerOfTen)
                return _biPowersOfTen[n];

            char[] buf = new char[n + 1];
            buf[0] = '1';
            for (int i = 1; i <= n; i++)
                buf[i] = '0';
            return BigInteger.Parse(new String(buf));
        }

        static BigInteger[] _biPowersOfTen = new BigInteger[] {
            BigInteger.One,
            BigInteger.Ten,
            BigInteger.Create(100),
            BigInteger.Create(1000),
            BigInteger.Create(10000),
            BigInteger.Create(100000),
            BigInteger.Create(1000000),
            BigInteger.Create(10000000),
            BigInteger.Create(100000000),
            BigInteger.Create(1000000000),
            BigInteger.Create(10000000000),
            BigInteger.Create(100000000000)
        };
        
        static readonly int _maxCachedPowerOfTen = _biPowersOfTen.Length;

        /// <summary>
        /// Remove insignificant trailing zeros from this BigDecimal until the 
        /// preferred exponent is reached or no more zeros can be removed.
        /// </summary>
        /// <param name="preferredExp"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Took this one from OpenJDK implementation, with some minor edits.</para>
        /// <para>Modifies its argument.  Use only on a new BigDecimal.</para>
        /// </remarks>
        private BigDecimal StripZerosToMatchExponent(long preferredExp)
        {
            while (_coeff.Abs().CompareTo(BigInteger.Ten) >= 0 && _exp < preferredExp)
            {
                if (_coeff.IsOdd)
                    break;                  // odd number.  cannot end in 0
                BigInteger rem;
                BigInteger quo = _coeff.DivRem(BigInteger.Ten, out rem);
                if (!rem.IsZero)
                    break;   // non-0 remainder
                _coeff = quo;
                _exp = CheckExponent((long)_exp + 1);// could overflow
                if (_precision > 0)  // adjust precision if known
                    _precision--;
            }

            return this;
        }


        /// <summary>
        /// Returns a BigDecimal numerically equal to this one, but with 
        /// any trailing zeros removed.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Ended up needing this in ClojureCLR, grabbed from OpenJDK.</remarks>
        public BigDecimal StripTrailingZeros()
        {    
            BigDecimal result = new BigDecimal(this._coeff,this._exp);
            result.StripZerosToMatchExponent(Int64.MaxValue);
            return result;
        }

        #endregion

        #region Rounding/quantize/rescale

        void RoundInPlace(Context c)
        {
            BigDecimal v = Round(this, c);
            if (v != this)
            {
                _coeff = v._coeff;
                _exp = v._exp;
                _precision = v._precision;
            }
        }

        public BigDecimal Round(Context c)
        {
            return Round(this, c);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        /// <remarks>The OpenJDK implementation has an efficiency hack to only compute the precision 
        /// (call to .GetPrecision) if the value is outside the range of the context's precision 
        /// (-10^precision to 10^precision), with those bounds being cached on the Context.
        /// TODO: See if it is worth implementing the hack.
        /// </remarks>
        public static BigDecimal Round(BigDecimal v, Context c)
        {
            //if (c.Precision == 0)
            //    return v;

            if (v.GetPrecision() < c.Precision)
                return v;

            int drop = (int)(v._precision - c.Precision);

            if (drop <= 0)
                return v;

            // we need to lose some digits on the coefficient. 
            BigInteger divisor = BIPowerOfTen(drop);

            BigInteger roundedInteger = RoundingDivide2(v._coeff, divisor, c.RoundingMode);

            int exp = CheckExponent((long)v._exp + drop, roundedInteger.IsZero);

            BigDecimal result = new BigDecimal(roundedInteger, exp);

            if (c.Precision > 0)
                result.RoundInPlace(c);

            return result;
        }

        /// <summary>
        /// Assuming 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        static BigInteger RoundingDivide2(BigInteger x, BigInteger y, RoundingMode mode)
        {
            BigInteger r;
            BigInteger q = x.DivRem(y, out r);

            bool increment = false;
            if (!r.IsZero)  // we need to pay attention
            {
                bool isNeg = q.IsNegative;

                switch (mode)
                {
                    case RoundingMode.Unnecessary:
                        throw new ArithmeticException("Rounding is required, but prohibited.");
                    case RoundingMode.Ceiling:
                        increment = !isNeg;
                        break;
                    case RoundingMode.Floor:
                        increment = isNeg;
                        break;
                    case RoundingMode.Down:
                        increment = false;
                        break;
                    case RoundingMode.Up:
                        increment = true;
                        break;

                    default:
                        {
                            int cmp = (r + r).Abs().CompareTo(y);
                            switch (mode)
                            {
                                case RoundingMode.HalfDown:
                                    increment = cmp > 0;
                                    break;
                                case RoundingMode.HalfUp:
                                    increment = cmp >= 0;
                                    break;
                                case RoundingMode.HalfEven:
                                    increment = cmp > 0 || (cmp == 0 && q.TestBit(0));
                                    break;
                            }
                        }
                        break;
                }


                if (increment)
                    if (q.IsNegative || (q.IsZero && x.IsNegative))
                        q = q - BigInteger.One;
                    else
                        q = q + BigInteger.One;
            }

            return q;
        }



        public static BigDecimal Quantize(BigDecimal lhs, BigDecimal rhs, RoundingMode mode)
        {
            return Rescale(lhs, rhs._exp, mode);
        }


        public BigDecimal Quantize(BigDecimal v, RoundingMode mode)
        {
            return Quantize(this, v, mode);
        }

        public static BigDecimal Rescale(BigDecimal lhs, int newExponent, RoundingMode mode)
        {

            int delta = CheckExponent((long)lhs._exp - newExponent, false);

            if ( delta == 0 )
                // no change
                return lhs;

            if ( lhs._coeff.IsZero )
                return new BigDecimal(BigInteger.Zero,newExponent);  // Not clear on the precision

            if (delta < 0)
            {
                // Essentially, we have to round to a new precision.
                // we need this new precision to be non-zero, else we are zero.
                int decrease = -delta;
                uint p = lhs.GetPrecision();
                
                if  ( p < decrease )
                    return new BigDecimal(BigInteger.Zero,newExponent);

                uint newPrecision = (uint)(p - decrease);

                BigDecimal r = lhs.Round(new Context(newPrecision, mode));
                if (r._exp == newExponent)
                    return r;
                else
                    return Rescale(r, newExponent, mode);  // happens for example with 9.9999 & 1e-2 where we have a round 10.0 
            }

            // decreasing the exponent (delta is positive)
            // multiply by an appropriate power of 10
            // Make sure we don't underflow

            BigInteger newCoeff = lhs._coeff * BIPowerOfTen(delta);
            uint newPrec = lhs._precision;
            if (newPrec != 0)
                newPrec += (uint)delta;
            return new BigDecimal(newCoeff, newExponent, newPrec);
        }

        #endregion
    }
}