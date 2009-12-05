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
//using BigDecimal = java.math.BigDecimal;

namespace clojure.lang
{
    public class Numbers
    {

        #region Enums for arithmetic opcodes

        public enum UnaryOpCode
        {
            Inc,
            Dec,
            Negate
        }

        public enum BoolUnaryOpCode
        {
            IsZero,
            IsPos,
            IsNeg
        }

        public enum BinaryOpCode
        {
            Add,
            Mult,
            Divide,
            Quotient,
            Remainder
        }

        public enum BoolBinaryOpCode
        {
            Equiv,
            Lt
        }

        #endregion

        #region Base class for operations

        abstract class Ops<T>
        {
            public bool Do(T x, T y, BoolBinaryOpCode code)
            {
                switch (code)
                {
                    case BoolBinaryOpCode.Equiv:
                        return equiv(x, y);
                    case BoolBinaryOpCode.Lt:
                        return lt(x, y);
                }
                throw new InvalidProgramException("Bad BoolBinaryOpCode -- internal error");
            }

            public bool Do(T x, BoolUnaryOpCode code)
            {
                switch (code)
                {
                    case BoolUnaryOpCode.IsNeg:
                        return isNeg(x);
                    case BoolUnaryOpCode.IsPos:
                        return isPos(x);
                    case BoolUnaryOpCode.IsZero:
                        return isZero(x);
                }
                throw new InvalidProgramException("Bad BoolBinaryOpCode -- internal error");
            }


            public object Do(T x, UnaryOpCode code)
            {
                switch (code)
                {
                    case UnaryOpCode.Dec:
                        return dec(x);
                    case UnaryOpCode.Inc:
                        return inc(x);
                    case UnaryOpCode.Negate:
                        return negate(x);
                }
                throw new InvalidProgramException("Bad BoolBinaryOpCode -- internal error");
            }

            public object Do(T x, T y, BinaryOpCode code)
            {
                switch (code)
                {
                    case BinaryOpCode.Add:
                        return add(x,y);
                    case BinaryOpCode.Divide:
                        return divide(x, y);
                    case BinaryOpCode.Mult:
                        return multiply(x, y);
                    case BinaryOpCode.Quotient:
                        return quotient(x, y);
                    case BinaryOpCode.Remainder:
                        return remainder(x, y);
                }
                throw new InvalidProgramException("Bad BoolBinaryOpCode -- internal error");
            }

            public abstract bool isZero(T x);
            public abstract bool isPos(T x);
            public abstract bool isNeg(T x);
            public abstract object add(T x, T y);
            public abstract object multiply(T x, T y);
            public abstract object divide(T x, T y);
            public abstract object quotient(T x, T y);
            public abstract object remainder(T x, T y);
            public abstract bool equiv(T x, T y);
            public abstract bool lt(T x, T y);
            public abstract object negate(T x);
            public abstract object inc(T x);
            public abstract object dec(T x);
        }

        #endregion

        #region BitOps interface

        interface BitOps
        {
            BitOps combine(BitOps y);
            BitOps bitOpsWith(IntegerBitOps x);
            BitOps bitOpsWith(LongBitOps x);
            BitOps bitOpsWith(BigIntegerBitOps x);
            object not(object x);
            object and(object x, object y);
            object or(object x, object y);
            object xor(object x, object y);
            object andNot(object x, object y);
            object clearBit(object x, int n);
            object setBit(object x, int n);
            object flipBit(object x, int n);
            bool testBit(object x, int n);
            object shiftLeft(object x, int n);
            object shiftRight(object x, int n);
        }

        #endregion

        #region Basic Ops operations

        public static bool isZero(object x)
        {
            return DoOp(x,BoolUnaryOpCode.IsZero);
        }

        public static bool isPos(object x)
        {
            return DoOp(x, BoolUnaryOpCode.IsPos);
        }

        public static bool isNeg(object x)
        {
            return DoOp(x, BoolUnaryOpCode.IsNeg);
        }

        public static object minus(object x)
        {
            return DoOp(x, UnaryOpCode.Negate);
        }

        public static object inc(object x)
        {
            return DoOp(x, UnaryOpCode.Inc);
        }

        public static object dec(object x)
        {
            return DoOp(x, UnaryOpCode.Dec);
        }

        public static object add(object x, object y)
        {
            return DoOp(x, y, BinaryOpCode.Add);
        }

        public static object minus(object x, object y)
        {
            return DoOp(x, DoOp(y, UnaryOpCode.Negate), BinaryOpCode.Add);
        }

        public static object multiply(object x, object y)
        {
            return DoOp(x, y, BinaryOpCode.Mult);
        }

        public static object divide(object x, object y)
        {
            if ( isZero(y) )
                throw new ArithmeticException("Divide by zero");
            return DoOp(x, y, BinaryOpCode.Divide);
        }

        public static object quotient(object x, object y)
        {
            if (isZero(y))
                throw new ArithmeticException("Divide by zero");
            return reduce(DoOp(x, y, BinaryOpCode.Quotient));
        }

        public static object remainder(object x, object y)
        {
            if (isZero(y))
                throw new ArithmeticException("Divide by zero");
            return reduce(DoOp(x, y, BinaryOpCode.Remainder));
        }


        static object DQuotient(double n, double d)
        {
            double q = n / d;
            if (q <= Int32.MaxValue && q >= Int32.MinValue)
                return (int)q;
            else
                // bigint quotient
                return reduce(BigDecimal.Create(q).ToBigInteger());
        }

        static object DRemainder(double n, double d)
        {
            double q = n / d;
            if (q <= Int32.MaxValue && q >= Int32.MinValue)
                return n - ((int)q) * d;
            else
            {
                // bigint quotient
                object bq = reduce(BigDecimal.Create(q).ToBigInteger());
                return n - ((double)bq) * d;
            }
        }

        public static bool equiv(object x, object y)
        {
            return Util.IsNumeric(x)
                && Util.IsNumeric(y)
                && DoOp(x, y, BoolBinaryOpCode.Equiv);
        }

        internal static bool EquivArg1Numeric(object x, object y)
        {
            return Util.IsNumeric(y)
                && DoOp(x, y, BoolBinaryOpCode.Equiv);
        }

        internal static bool EquivArg2Numeric(object x, object y)
        {
            return Util.IsNumeric(x)
                && DoOp(x, y, BoolBinaryOpCode.Equiv);
        }

        internal static bool EquivBothArgsNumeric(object x, object y)
        {
            return DoOp(x, y, BoolBinaryOpCode.Equiv);
        }


        public static bool lt(object x, object y)
        {
            return DoOp(x, y, BoolBinaryOpCode.Lt);
        }

        public static bool lte(object x, object y)
        {
            return !lt(y, x);
        }

        public static bool gt(object x, object y)
        {
            return lt(y, x);
        }

        public static bool gte(object x, object y)
        {
            return !lt(x, y);
        }

        public static int compare(object x, object y)
        {
            if (lt(x, y))
                return -1;
            else if (lt(y, x))
                return 1;
            else
                return 0;
        }


        public static object DoOp(object x, UnaryOpCode code)
        {
            int ix;
            if (TryAsInt(x, out ix))
                return INTEGER_OPS.Do(ix, code);

            long lx;
            if (TryAsLong(x, out lx))
                return LONG_OPS.Do(lx, code);

            if ( x is double )
                return DOUBLE_OPS.Do((double)x, code);

            if ( x is float )
                return FLOAT_OPS.Do((float)x, code);

            if (x is Ratio)
                return RATIO_OPS.Do((Ratio)x, code);

            if (x is BigInteger)
                return BIGINTEGER_OPS.Do((BigInteger)x, code);

            if (x is UInt64)
                return BIGINTEGER_OPS.Do(BigInteger.Create((ulong)x), code);

            // TODO: decimal

            if (x is BigDecimal)
                return BIGDECIMAL_OPS.Do((BigDecimal)x, code);

            return INTEGER_OPS.Do(Util.ConvertToInt(x), code);
        }

        public static bool DoOp(object x, BoolUnaryOpCode code)
        {
            int ix;
            if (TryAsInt(x, out ix))
                return INTEGER_OPS.Do(ix, code);

            long lx;
            if (TryAsLong(x, out lx))
                return LONG_OPS.Do(lx, code);

            if ( x is double )
                return DOUBLE_OPS.Do((double)x, code);

            if ( x is float )
                return FLOAT_OPS.Do((float)x, code);

            if (x is Ratio)
                return RATIO_OPS.Do((Ratio)x, code);

            if (x is BigInteger)
                return BIGINTEGER_OPS.Do((BigInteger)x, code);

            if (x is UInt64)
                return BIGINTEGER_OPS.Do(BigInteger.Create((ulong)x), code);

            // TODO: decimal

            if (x is BigDecimal)
                return BIGDECIMAL_OPS.Do((BigDecimal)x, code);

            return INTEGER_OPS.Do(Util.ConvertToInt(x), code);
        }


        public static bool DoOp(object x, object y, BoolBinaryOpCode code)
        {
           if ( x is double )
                return DOUBLE_OPS.Do((double)x, Util.ConvertToDouble(y), code);

            if ( y is double )
                return DOUBLE_OPS.Do(Util.ConvertToDouble(x), (double)y, code);

           if ( x is float )
                return FLOAT_OPS.Do((float)x, Util.ConvertToFloat(y), code);

            if ( y is float )
                return FLOAT_OPS.Do(Util.ConvertToFloat(x), (float)y, code);

            if ( x is Ratio )
                return RATIO_OPS.Do((Ratio)x,toRatio(y),code);

            if ( y is Ratio )
                return RATIO_OPS.Do(toRatio(x), (Ratio)y, code);

            if ( x is BigDecimal )
                return BIGDECIMAL_OPS.Do((BigDecimal)x,toBigDecimal(y),code);

            if ( y is BigDecimal )
                return BIGDECIMAL_OPS.Do(toBigDecimal(x), (BigDecimal)y, code);

            if ( x is BigInteger )
                return BIGINTEGER_OPS.Do((BigInteger)x,toBigInteger(y),code);

            if ( y is BigInteger )
                return BIGINTEGER_OPS.Do(toBigInteger(x), (BigInteger)y, code); 

            long lval;

            if ( TryAsLong(x,out lval ) )
                return LONG_OPS.Do(lval,Util.ConvertToLong(y),code);

            if ( TryAsLong(y,out lval ) )
                return LONG_OPS.Do(Util.ConvertToLong(x),lval,code);

            return INTEGER_OPS.Do(Util.ConvertToInt(x),Util.ConvertToInt(y),code);
        }


        public static object DoOp(object x, object y, BinaryOpCode code)
        {
            if (x is double)
                return DOUBLE_OPS.Do((double)x, Util.ConvertToDouble(y), code);

            if (y is double)
                return DOUBLE_OPS.Do(Util.ConvertToDouble(x), (double)y, code);

            if (x is float)
                return FLOAT_OPS.Do((float)x, Util.ConvertToFloat(y), code);

            if (y is float)
                return FLOAT_OPS.Do(Util.ConvertToFloat(x), (float)y, code);

            if (x is Ratio)
                return RATIO_OPS.Do((Ratio)x, toRatio(y), code);

            if (y is Ratio)
                return RATIO_OPS.Do(toRatio(x), (Ratio)y, code);

            if (x is BigDecimal)
                return BIGDECIMAL_OPS.Do((BigDecimal)x, toBigDecimal(y), code);

            if (y is BigDecimal)
                return BIGDECIMAL_OPS.Do(toBigDecimal(x), (BigDecimal)y, code);

            if (x is BigInteger)
                return BIGINTEGER_OPS.Do((BigInteger)x, toBigInteger(y), code);

            if (y is BigInteger)
                return BIGINTEGER_OPS.Do(toBigInteger(x), (BigInteger)y, code);

            long lval;

            if (TryAsLong(x, out lval))
                return LONG_OPS.Do(lval, Util.ConvertToLong(y), code);

            if (TryAsLong(y, out lval))
                return LONG_OPS.Do(Util.ConvertToLong(x), lval, code);

            return INTEGER_OPS.Do(Util.ConvertToInt(x), Util.ConvertToInt(y), code);
        }


        static bool TryAsInt(object x, out int ix)
        {
            //Type type = Util.GetNonNullableType(x.GetType());
            Type type = x.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                    ix = (int)(char)x;
                    return true;
                case TypeCode.SByte:
                    ix = (int)(sbyte)x;
                    return true;
                case TypeCode.Byte:
                    ix = (int)(byte)x;
                    return true;
                case TypeCode.Int16:
                    ix = (int)(short)x;
                    return true;
                case TypeCode.UInt16:
                    ix = (int)(ushort)x;
                    return true;
                case TypeCode.Int32:
                    ix = (int)x;
                    return true;
            }

            ix = 0;
            return false;
        }

        static bool TryAsLong(object x, out long lx)
        {
            if (x is long)
            {
                lx = (long)x;
                return true;
            }
            else if (x is uint)
            {
                lx = (long)(uint)x;
                return true;
            }
            else
            {
                lx = 0;
                return false;
            }
        }


        #endregion

        #region  utility methods

        static BigInteger toBigInteger(object x)
        {
            if (x is BigInteger)
                return (BigInteger)x;
            else
                return BigInteger.Create(Util.ConvertToLong(x)); // convert fix
        }

        static BigDecimal toBigDecimal(object x)
        {
            if (x is BigDecimal)
                return (BigDecimal)x;
            else if ( x is BigInteger)
                return BigDecimal.Create((BigInteger)x);
            else
                return BigDecimal.Create(Util.ConvertToLong(x)); // convert fix

        }

        static Ratio toRatio(object x)
        {
            if (x is Ratio)
                return (Ratio)x;
            else if (x is BigDecimal)
            {
                BigDecimal bx = (BigDecimal)x;
                int exp = bx.Exponent;
                if (exp >= 0)
                    return new Ratio(bx.ToBigInteger(), BigInteger.ONE);
                else
                    return new Ratio(bx.MovePointRight(-exp).ToBigInteger(), BigInteger.TEN.Power(-exp));
            }
            return new Ratio(toBigInteger(x), BigInteger.ONE);
        }

        public static object rationalize(object x)
        {
            if (x is float)                                     // convert fix
                return rationalize(BigDecimal.Create((float)x));   // convert fix
            else if (x is double)                               // convert fix
                return rationalize(BigDecimal.Create((double)x));  // convert fix
            else if (x is BigDecimal)
            {
                BigDecimal bx = (BigDecimal)x;
                int exp = bx.Exponent;
                if (exp >= 0)
                    return bx.ToBigInteger();
                else
                    //return divide(bx.movePointRight(scale).toBigInteger(), BigIntegerTen.pow(scale));
                    return divide(bx.MovePointRight(-exp).ToBigInteger(), BigInteger.TEN.Power(-exp));
            }
            return x;
        }

        public static object reduce(object val)
        {
            if (val is long)
                return reduce((long)val);
            else if (val is BigInteger)
                return reduce((BigInteger)val);
            else
                return val;
        }

        public static object reduce(BigInteger val)
        {
            int ival;
            if (val.AsInt32(out ival))
                return ival;

            long lval;
            if (val.AsInt64(out lval))
                return lval;

            return val;
        }

        public static object reduce(long val)
        {
            if (val >= Int32.MinValue && val <= Int32.MaxValue)
                return (int)val;
            else
                return val;
        }

        public static object BIDivide(BigInteger n, BigInteger d)
        {
            if (d.Equals(BigInteger.ZERO))
                throw new ArithmeticException("Divide by zero");
            BigInteger gcd = n.Gcd(d);
            if (gcd.Equals(BigInteger.ZERO))
                return 0;
            n = n / gcd;
            d = d / gcd;

            if (d.Equals(BigInteger.ONE))
                return reduce(n);

            return new Ratio((d.Signum < 0 ? -n : n), d.Abs());
        }

        #endregion

        #region Basic BitOps operations

        public static object not(object x)
        {
            return bitOps(x).not(x);
        }

        public static object and(object x, object y)
        {
            return bitOps(x).combine(bitOps(y)).and(x, y);
        }

        public static object or(object x, object y)
        {
            return bitOps(x).combine(bitOps(y)).or(x, y);
        }

        public static object xor(object x, object y)
        {
            return bitOps(x).combine(bitOps(y)).xor(x, y);
        }

        public static object andNot(object x, object y)
        {
            return bitOps(x).combine(bitOps(y)).andNot(x, y);
        }


        public static object clearBit(object x, int n)
        {
            if (n < 0)
                throw new ArithmeticException("Negative bit index");
            return bitOps(x).clearBit(x, n);
        }

        public static object setBit(object x, int n)
        {
            if (n < 0)
                throw new ArithmeticException("Negative bit index");
            return bitOps(x).setBit(x, n);
        }

        public static object flipBit(object x, int n)
        {
            if (n < 0)
                throw new ArithmeticException("Negative bit index");
            return bitOps(x).flipBit(x, n);
        }

        public static bool testBit(object x, int n)
        {
            if (n < 0)
                throw new ArithmeticException("Negative bit index");
            return bitOps(x).testBit(x, n);
        }

        public static object shiftLeft(object x, int n)
        {
            return bitOps(x).shiftLeft(x, n);
        }

        public static object shiftRight(object x, int n)
        {
            return bitOps(x).shiftRight(x, n);
        }

        #endregion

        #region Ops/BitOps dispatching

        static readonly IntegerOps INTEGER_OPS = new IntegerOps();
        static readonly LongOps LONG_OPS = new LongOps();
        static readonly FloatOps FLOAT_OPS = new FloatOps();
        static readonly DoubleOps DOUBLE_OPS = new DoubleOps();
        static readonly RatioOps RATIO_OPS = new RatioOps();
        static readonly BigIntegerOps BIGINTEGER_OPS = new BigIntegerOps();
        static readonly BigDecimalOps BIGDECIMAL_OPS = new BigDecimalOps();

        static readonly IntegerBitOps INTEGER_BITOPS = new IntegerBitOps();
        static readonly LongBitOps LONG_BITOPS = new LongBitOps();
        static readonly BigIntegerBitOps BIGINTEGER_BITOPS = new BigIntegerBitOps();

 

        static BitOps bitOps(object x)
        {
            //Type type = Util.GetNonNullableType(x.GetType());
            Type type = x.GetType();

            //if (!type.IsEnum)     // convert fix
            //{

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int32:
                        return INTEGER_BITOPS;
                    case TypeCode.Int64:
                        return LONG_BITOPS;
                    default:
                        if (type == typeof(BigInteger))
                            return BIGINTEGER_BITOPS;
                        else if (Util.IsNumeric(x) || (type == typeof(BigDecimal)) || (type == typeof(Ratio)))
                            throw new ArithmeticException("bit operation on non integer type: " + type);
                        break;
                }
            //}
            return INTEGER_BITOPS;
        }

        #endregion

        sealed class IntegerOps : Ops<int>
        {
            #region Ops Members


            public override bool isZero(int x)
            {
                return x == 0;  // convert fix
            }

            public override bool isPos(int x)
            {
                return x > 0;  // convert fix
            }

            public override bool isNeg(int x)
            {
                return x < 0;  // convert fix
            }

            public override object add(int x, int y)
            {
                long ret = (long)x + (long)y;
                if (ret <= Int32.MaxValue && ret >= Int32.MinValue)
                    return (int)ret;
                return ret;

            }

            public override object multiply(int x, int y)
            {
                long ret = (long)x * (long)y;
                if (ret <= Int32.MaxValue && ret >= Int32.MinValue)
                    return (int)ret;
                return ret;
            }

            static int gcd(int u, int v)
            {
                while (v != 0)
                {
                    int r = u % v;
                    u = v;
                    v = r;
                }
                return u;
            }

            public override object divide(int n, int val)
            {
                 int gcd1 = gcd(n, val);
                if (gcd1 == 0)
                    return 0;

                n = n / gcd1;
                int d = val / gcd1;
                if (d == 1)
                    return n;
                if (d < 0)
                {
                    n = -n;
                    d = -d;
                }
                return new Ratio(BigInteger.Create(n), BigInteger.Create(d));
            }

            public override object quotient(int x, int y)
            {
                //return Convert.ToInt32(x) / Convert.ToInt32(y);
                return x / y;
            }

            public override object remainder(int x, int y)
            {
                return x % y;     // convert fix
            }

            public override  bool equiv(int x, int y)
            {
                return x == y;     // convert fix
            }

            public override bool lt(int x, int y)
            {
                return x < y;     // convert fix
            }

            public override object negate(int x)
            {
                if (x > Int32.MinValue)
                    return -x;
                return -((long)x);
            }

            public override object inc(int x)
            {
                if (x < Int32.MaxValue)
                    return x + 1;
                return ((long)x) + 1;
            }

            public override object dec(int x)
            {
                if (x > Int32.MinValue)
                    return x - 1;
                return ((long)x) - 1;
            }

            #endregion
        }

        sealed class LongOps : Ops<long>
        {
            #region Ops Members
            
            public override bool isZero(long x)
            {
                return x == 0;      // convert fix
            }

            public override bool isPos(long x)
            {
                return x > 0;      // convert fix
            }

            public override bool isNeg(long x)
            {
                return x < 0;      // convert fix
            }

            public override object add(long x, long y)
            {
                long ret = x + y;
                if ((ret ^ x) < 0 && (ret ^ y) < 0)
                    return BIGINTEGER_OPS.add(x, y);
                return ret;
            }

            public override object multiply(long x, long y)
            {
                long ret = x * y;
                if (y != 0 && ret / y != x)
                    return BIGINTEGER_OPS.multiply(x, y);
                return ret;
            }
            
            static long gcd(long u, long v)
            {
                while (v != 0)
                {
                    long r = u % v;
                    u = v;
                    v = r;
                }
                return u;
            }

            public override object divide(long n, long val)
            {
                long gcd1 = gcd(n, val);
                if (gcd1 == 0)
                    return 0;

                n = n / gcd1;
                long d = val / gcd1;
                if (d == 1)
                    return n;
                if (d < 0)
                {
                    n = -n;
                    d = -d;
                }
                return new Ratio(BigInteger.Create(n), BigInteger.Create(d));
            }

            public override object quotient(long x, long y)
            {
                return x / y;       // convert fix
            }

            public override object remainder(long x, long y)
            {
                return x % y;       // convert fix
            }

            public override bool equiv(long x, long y)
            {
                return x == y;       // convert fix
            }

            public override bool lt(long x, long y)
            {
                return x < y;       // convert fix
            }

            public override object negate(long x)
            {
                if (x > Int64.MinValue)
                    return -x;
                return -BigInteger.Create(x);
            }

            public override object inc(long x)
            {
                if (x < Int64.MaxValue)
                    return x + 1;
                return BIGINTEGER_OPS.inc(x);
            }

            public override object dec(long x)
            {
                if (x > Int64.MinValue)
                    return x - 1;
                return BIGINTEGER_OPS.dec(x);
            }

            #endregion
        }

        sealed class FloatOps : Ops<float>
        {
            #region Ops Members
 
            public override bool isZero(float x)
            {
                return x == 0;     // convert fix
            }

            public override bool isPos(float x)
            {
                return x > 0;     // convert fix
            }

            public override bool isNeg(float x)
            {
                return x < 0;     // convert fix
            }

            public override object add(float x, float y)
            {
                return x + y;     // convert fix
            }

            public override object multiply(float x, float y)
            {
                return x * y;     // convert fix
            }

            public override object divide(float x, float y)
            {
                return x / y;     // convert fix
            }

            public override object quotient(float x, float y)
            {
                return Numbers.DQuotient((double)x, (double)y);
            }

            public override object remainder(float x, float y)
            {
                return Numbers.DRemainder((double)x,(double)y);
            }

            public override bool equiv(float x, float y)
            {
                return x == y;        // convert fix
            }

            public override bool lt(float x, float y)
            {
                return x < y;
            }

            public override object negate(float x)
            {
                return -x;
            }

            public override object inc(float x)
            {
                return x + 1;    // convert fix
            }

            public override object dec(float x)
            {
                return x - 1;    // convert fix
            }

            #endregion
        }

        sealed class DoubleOps : Ops<double>
        {
            #region Ops Members

            public override bool isZero(double x)
            {
                return x == 0;        // convert fix
            }

            public override bool isPos(double x)
            {
                return x > 0;        // convert fix
            }

            public override bool isNeg(double x)
            {
                return x < 0;        // convert fix
            }

            public override object add(double x, double y)
            {
                return x + y;       // convert fix

            }

            public override object multiply(double x, double y)
            {
                return x * y;
            }

            public override object divide(double x, double y)
            {
                return x / y;
            }

            public override object quotient(double x, double y)
            {
                return Numbers.DQuotient(x,y);     // convert fix
            }

            public override object remainder(double x, double y)
            {
                return Numbers.DRemainder(x,y);     // convert fix
            }

            public override bool equiv(double x, double y)
            {
                return x == y;       // convert fix
            }

            public override bool lt(double x, double y)
            {
                return x < y;
            }

            public override object negate(double x)
            {
                return -x;
            }

            public override object inc(double x)
            {
                return x + 1;     // convert fix
            }

            public override object dec(double x)
            {
                return x - 1;     // convert fix
            }

            #endregion
        }

        class RatioOps : Ops<Ratio>
        {
            #region Ops Members

 

            public override bool isZero(Ratio r)
            {
                return r.numerator.Signum == 0;
            }

            public override bool isPos(Ratio r)
            {
                return r.numerator.Signum > 0;
            }

            public override bool isNeg(Ratio r)
            {
                return r.numerator.Signum < 0;
            }

            public override object add(Ratio rx, Ratio ry)
            {
                return Numbers.divide(
                    ry.numerator * rx.denominator + rx.numerator * ry.denominator,
                    ry.denominator * rx.denominator);
            }

            public override object multiply(Ratio rx, Ratio ry)
            {
                return Numbers.divide(
                    ry.numerator * rx.numerator,
                    ry.denominator * rx.denominator);
            }

            public override object divide(Ratio rx, Ratio ry)
            {
                return Numbers.divide(
                    ry.denominator * rx.numerator,
                    ry.numerator * rx.denominator);
            }

            public override object quotient(Ratio rx, Ratio ry)
            {
                BigInteger q = (rx.numerator * ry.denominator) / (rx.denominator * ry.numerator);
                return reduce(q);
            }

            public override object remainder(Ratio rx, Ratio ry)
            {
                BigInteger q = (rx.numerator * ry.denominator) / (rx.denominator * ry.numerator);
                return Numbers.minus(rx, Numbers.multiply(q, ry));
            }

            public override bool equiv(Ratio rx, Ratio ry)
            {
                return rx.numerator.Equals(ry.numerator)
                    && rx.denominator.Equals(ry.denominator);
            }

            public override bool lt(Ratio rx, Ratio ry)
            {
                return rx.numerator * ry.denominator < ry.numerator * rx.denominator;
            }

            public override object negate(Ratio rx)
            {
                return new Ratio(-rx.numerator, rx.denominator);
            }

            static readonly Ratio ONE = new Ratio(BigInteger.ONE,BigInteger.ONE);
            static readonly Ratio MINUS_ONE = new Ratio(BigInteger.NEGATIVE_ONE, BigInteger.ONE);

            public override object inc(Ratio x)
            {
                return add(x, ONE);
            }

            public override object dec(Ratio x)
            {
                return add(x, MINUS_ONE);
            }

            #endregion
        }

        class BigIntegerOps : Ops<BigInteger>
        {
            #region Ops Members
  
            public override bool isZero(BigInteger bx)
            {
                return bx.IsZero;
            }

            public override bool isPos(BigInteger bx)
            {
                return bx.IsPositive;
            }

            public override bool isNeg(BigInteger bx)
            {
                return bx.IsNegative;
            }

            public override object add(BigInteger x, BigInteger y)
            {
                //return reduce(toBigInteger(x).add(toBigInteger(y)));
                return reduce(x+y);
            }

            public override object multiply(BigInteger x, BigInteger y)
            {
                //return reduce(toBigInteger(x).multiply(toBigInteger(y)));
                return reduce(x*y);

            }

            public override object divide(BigInteger x, BigInteger y)
            {
                return BIDivide(x,y);
            }

            public override object quotient(BigInteger x, BigInteger y)
            {
                return x/y;
            }

            public override object remainder(BigInteger x, BigInteger y)
            {
                return x % y;
            }

            public override bool equiv(BigInteger x, BigInteger y)
            {
                return x.Equals(y);
            }

            public override bool lt(BigInteger x, BigInteger y)
            {
                return x < y;
            }

            public override object negate(BigInteger x)
            {
                return -x;
            }

            public override object inc(BigInteger bx)
            {
                return reduce(bx + BigInteger.ONE);
            }

            public override object dec(BigInteger bx)
            {
                return reduce(bx - BigInteger.ONE);
            }

            #endregion
        }

        class BigDecimalOps : Ops<BigDecimal>
        {
            #region Ops Members

            public override bool isZero(BigDecimal bx)
            {
                return bx.IsZero;
            }

            public override bool isPos(BigDecimal bx)
            {
                return bx.IsPositive;
            }

            public override bool isNeg(BigDecimal bx)
            {
                return bx.IsNegative;
            }

            public override object add(BigDecimal x, BigDecimal y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? x.Add(y)
                    : x.Add(y, c.Value);
            }

            public override object multiply(BigDecimal x, BigDecimal y)
            {
                 BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                 return c == null
                    ? x.Multiply(y)
                    : x.Multiply(y, c.Value);
            }

            public override object divide(BigDecimal x, BigDecimal y)
            {
                 BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                 return c == null
                     ? x.Divide(y)
                     : x.Divide(y, c.Value);
            }

            public override object quotient(BigDecimal x, BigDecimal y)
            {
                 BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                 return c == null
                     ? x.DivideInteger(y)
                     : x.DivideInteger(y, c.Value);
            }

            public override object remainder(BigDecimal x, BigDecimal y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? x.Mod(y)
                    : x.Mod(y, c.Value);
            }

            public override bool equiv(BigDecimal x, BigDecimal y)
            {
                return x.Equals(y);
            }

            public override bool lt(BigDecimal x, BigDecimal y)
            {
                return x.CompareTo(y) < 0;
            }

            public override object negate(BigDecimal x)
            {
                 BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                 return c == null
                     ? x.Negate()
                     : x.Negate(c.Value);
            }

            public override object inc(BigDecimal bx)
            {
                 BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                 return c == null
                     ? bx.Add(BigDecimal.ONE)
                     : bx.Add(BigDecimal.ONE, c.Value);
            }

            public override object dec(BigDecimal bx)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? bx.Subtract(BigDecimal.ONE)
                    : bx.Subtract(BigDecimal.ONE, c.Value);
            }

            #endregion
        }

        class IntegerBitOps : BitOps
        {
            #region BitOps Members

            public BitOps combine(BitOps y)
            {
                return y.bitOpsWith(this); ;
            }

            public BitOps bitOpsWith(IntegerBitOps x)
            {
                return this;
            }

            public BitOps bitOpsWith(LongBitOps x)
            {
                return LONG_BITOPS;
            }

            public BitOps bitOpsWith(BigIntegerBitOps x)
            {
                return BIGINTEGER_BITOPS;
            }

            public object not(object x)
            {
                //return ~Convert.ToInt32(x);
                return ~Util.ConvertToInt(x);   // convert fix
            }

            public object and(object x, object y)
            {
                //return Convert.ToInt32(x) & Convert.ToInt32(y);
                return Util.ConvertToInt(x) & Util.ConvertToInt(y);     // convert fix
            }

            public object or(object x, object y)
            {
                //return Convert.ToInt32(x) | Convert.ToInt32(y);
                return Util.ConvertToInt(x) | Util.ConvertToInt(y);     // convert fix
            }

            public object xor(object x, object y)
            {
                //return Convert.ToInt32(x) ^ Convert.ToInt32(y);
                return Util.ConvertToInt(x) ^ Util.ConvertToInt(y);     // convert fix
            }

            public object andNot(object x, object y)
            {
                //return Convert.ToInt32(x) & ~Convert.ToInt32(y);
                return Util.ConvertToInt(x) & ~Util.ConvertToInt(y);     // convert fix
            }

            public object clearBit(object x, int n)
            {
                if (n < 31)
                    //return Convert.ToInt32(x) & ~(1 << n);
                    return Util.ConvertToInt(x) & ~(1 << n);    // convert fix
                else if (n < 63)
                    //return Convert.ToInt64(x) & ~(1L << n);
                    return Util.ConvertToLong(x) & ~(1L << n);    // convert fix
                else
                    //return toBigInteger(x).clearBit(n);
                    return toBigInteger(x).ClearBit(n);
            }

            public object setBit(object x, int n)
            {
                if (n < 31)
                    //return Convert.ToInt32(x) | (1 << n);
                    return Util.ConvertToInt(x) | (1 << n);    // convert fix
                else if (n < 63)
                    //return Convert.ToInt64(x) | (1L << n);
                    return Util.ConvertToLong(x) | (1L << n);    // convert fix
                else
                    //return toBigInteger(x).setBit(n);
                    return toBigInteger(x).SetBit(n);
            }

            public object flipBit(object x, int n)
            {
                if (n < 31)
                    //return Convert.ToInt32(x) ^ (1 << n);
                    return Util.ConvertToInt(x) ^ (1 << n);    // convert fix

                else if (n < 63)
                    //return Convert.ToInt64(x) ^ (1L << n);
                    return Util.ConvertToLong(x) ^ (1L << n);    // convert fix
                else
                    //return toBigInteger(x).flipBit(n);
                    return toBigInteger(x).FlipBit(n);
            }

            public bool testBit(object x, int n)
            {
                if (n < 31)
                    //return (Convert.ToInt32(x) & (1 << n)) != 0;
                    return (Util.ConvertToInt(x) & (1 << n)) != 0;    // convert fix
                else if (n < 63)
                    //return (Convert.ToInt64(x) & (1L << n)) != 0;
                    return (Util.ConvertToLong(x) & (1L << n)) != 0;    // convert fix
                else
                    //return toBigInteger(x).testBit(n);
                    return toBigInteger(x).TestBit(n);
            }

            public object shiftLeft(object x, int n)
            {
                if (n < 32)
                    return (n < 0)
                        ? shiftRight(x, -n)
                        //: reduce(Convert.ToInt64(x) << n);
                        : reduce(Util.ConvertToLong(x) << n);       // convert fix
                else
                    //return reduce(toBigInteger(x).shiftLeft(n));
                    return reduce(toBigInteger(x) << n);
            }

            public object shiftRight(object x, int n)
            {
                return (n < 0)
                   ? shiftLeft(x, -n)
                    //: Convert.ToInt32(x) >> n;
                   : Util.ConvertToInt(x) >> n;
            }

            #endregion
        }

        class LongBitOps : BitOps
        {
            #region BitOps Members

            public BitOps combine(BitOps y)
            {
                return y.bitOpsWith(this);
            }

            public BitOps bitOpsWith(IntegerBitOps x)
            {
                return this;
            }

            public BitOps bitOpsWith(LongBitOps x)
            {
                return this;
            }

            public BitOps bitOpsWith(BigIntegerBitOps x)
            {
                return BIGINTEGER_BITOPS;
            }

            public object not(object x)
            {
                //return ~Convert.ToInt64(x);
                return ~Util.ConvertToLong(x);      // convert fix
            }

            public object and(object x, object y)
            {
                //return Convert.ToInt64(x) & Convert.ToInt64(y);
                return Util.ConvertToLong(x) & Util.ConvertToLong(y);       // convert fix
            }

            public object or(object x, object y)
            {
                //return Convert.ToInt64(x) | Convert.ToInt64(y);
                return Util.ConvertToLong(x) | Util.ConvertToLong(y);       // convert fix
            }

            public object xor(object x, object y)
            {
                //return Convert.ToInt64(x) ^ Convert.ToInt64(y);
                return Util.ConvertToLong(x) ^ Util.ConvertToLong(y);       // convert fix
            }

            public object andNot(object x, object y)
            {
                //return Convert.ToInt64(x) & ~Convert.ToInt64(y);
                return Util.ConvertToLong(x) & ~Util.ConvertToLong(y);       // convert fix
            }

            public object clearBit(object x, int n)
            {
                if (n < 63)
                    //return Convert.ToInt64(x) & ~(1L << n);
                    return Util.ConvertToLong(x) & ~(1L << n);      // convert fix
                else
                    //return toBigInteger(x).clearBit(n);
                    return toBigInteger(x).ClearBit(n);
            }

            public object setBit(object x, int n)
            {
                if (n < 63)
                    //return Convert.ToInt64(x) | (1L << n);
                    return Util.ConvertToLong(x) | (1L << n);      // convert fix
                else
                    //return toBigInteger(x).setBit(n);
                    return toBigInteger(x).SetBit(n);
            }

            public object flipBit(object x, int n)
            {
                if (n < 63)
                    //return Convert.ToInt64(x) ^ (1L << n);
                    return Util.ConvertToLong(x) ^ (1L << n);      // convert fix
                else
                    //return toBigInteger(x).flipBit(n);
                    return toBigInteger(x).FlipBit( n);
            }

            public bool testBit(object x, int n)
            {
                if (n < 63)
                    //return (Convert.ToInt64(x) & (1L << n)) != 0;
                    return (Util.ConvertToLong(x) & (1L << n)) != 0;      // convert fix
                else
                    //return toBigInteger(x).testBit(n);
                    return toBigInteger(x).TestBit( n);
            }

            public object shiftLeft(object x, int n)
            {
                return n < 0
                    ? shiftRight(x, -n)
                    //: reduce(toBigInteger(x).shiftLeft(n));
                    : reduce(toBigInteger(x) << n);
            }

            public object shiftRight(object x, int n)
            {
                return n < 0
                     ? shiftLeft(x, -n)
                    //: Convert.ToInt64(x) >> n;
                     : Util.ConvertToLong(x) >> n;      // convert fix
            }

            #endregion
        }

        class BigIntegerBitOps : BitOps
        {
            #region BitOps Members

            public BitOps combine(BitOps y)
            {
                return y.bitOpsWith(this);
            }

            public BitOps bitOpsWith(IntegerBitOps x)
            {
                return this;
            }

            public BitOps bitOpsWith(LongBitOps x)
            {
                return this;
            }

            public BitOps bitOpsWith(BigIntegerBitOps x)
            {
                return this;
            }

            public object not(object x)
            {
                //return toBigInteger(x).not();
                return ~toBigInteger(x);
            }

            public object and(object x, object y)
            {
                //return toBigInteger(x).and(toBigInteger(y));
                return toBigInteger(x) & toBigInteger(y);
            }

            public object or(object x, object y)
            {
                //return toBigInteger(x).or(toBigInteger(y));
                return toBigInteger(x) | toBigInteger(y);

            }

            public object xor(object x, object y)
            {
                //return toBigInteger(x).xor(toBigInteger(y));
                return toBigInteger(x) ^ toBigInteger(y);
            }

            public object andNot(object x, object y)
            {
                //return toBigInteger(x).andNot(toBigInteger(y));
                return toBigInteger(x).BitwiseAndNot(toBigInteger(y));
            }

            public object clearBit(object x, int n)
            {
                //return toBigInteger(x).clearBit(n);
                return toBigInteger(x).ClearBit(n);
            }

            public object setBit(object x, int n)
            {
                //return toBigInteger(x).setBit(n);
                return toBigInteger(x).SetBit(n);
            }

            public object flipBit(object x, int n)
            {
                //return toBigInteger(x).flipBit(n);
                return toBigInteger(x).FlipBit(n);
            }

            public bool testBit(object x, int n)
            {
                //return toBigInteger(x).testBit(n);
                return toBigInteger(x).TestBit(n);
            }

            public object shiftLeft(object x, int n)
            {
                //return toBigInteger(x).shiftLeft(n);
                return toBigInteger(x) << n;
            }

            public object shiftRight(object x, int n)
            {
                //return toBigInteger(x).shiftRight(n);
                return toBigInteger(x) >> n;
            }

            #endregion
        }
        
        #region Array c-tors

        static public float[] float_array(int size, object init)
        {
            float[] ret = new float[size];
            if (Util.IsNumeric(init))
            {
                float f = Util.ConvertToFloat(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToFloat(s.first());
            }
            return ret;
        }

        static public float[] float_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new float[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                float[] ret = new float[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToFloat(s.first());
                return ret;
            }
        }

        static public double[] double_array(int size, Object init)
        {
            double[] ret = new double[size];
            if (Util.IsNumeric(init))
            {
                double f = Util.ConvertToDouble(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToDouble(s.first());
            }
            return ret;
        }

        static public double[] double_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new double[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                double[] ret = new double[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToDouble(s.first());
                return ret;
            }
        }

        static public int[] int_array(int size, Object init)
        {
            int[] ret = new int[size];
            if (Util.IsNumeric(init))
            {
                int f = Util.ConvertToInt(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToInt(s.first());
            }
            return ret;
        }

        static public int[] int_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new int[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                int[] ret = new int[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToInt(s.first());
                return ret;
            }
        }

        static public long[] long_array(int size, Object init)
        {
            long[] ret = new long[size];
            if (Util.IsNumeric(init))
            {
                long f = Util.ConvertToLong(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToLong(s.first());
            }
            return ret;
        }

        static public long[] long_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new long[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                long[] ret = new long[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToLong(s.first());
                return ret;
            }
        }


        static public short[] short_array(int size, Object init)
        {
            short[] ret = new short[size];
            if (Util.IsNumeric(init))
            {
                short f = Util.ConvertToShort(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToShort(s.first());
            }
            return ret;
        }

        static public short[] short_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new short[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                short[] ret = new short[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToShort(s.first());
                return ret;
            }
        }


        static public char[] char_array(int size, Object init)
        {
            char[] ret = new char[size];
            if (Util.IsNumeric(init))
            {
                char f = Util.ConvertToChar(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToChar(s.first());
            }
            return ret;
        }

        static public char[] char_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new char[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                char[] ret = new char[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToChar(s.first());
                return ret;
            }
        }


        static public byte[] byte_array(int size, Object init)
        {
            byte[] ret = new byte[size];
            if (Util.IsNumeric(init))
            {
                byte f = Util.ConvertToByte(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToByte(s.first());
            }
            return ret;
        }

        static public byte[] byte_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new byte[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                byte[] ret = new byte[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToByte(s.first());
                return ret;
            }
        }


        static public bool[] boolean_array(int size, Object init)
        {
            bool[] ret = new bool[size];
            if (init is bool)
            {
                bool f = (bool)init;
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = (bool)s.first();
            }
            return ret;
        }

        static public bool[] boolean_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new bool[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                bool[] ret = new bool[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = (bool)s.first();
                return ret;
            }
        }


        static public bool[] booleans(Object array)
        {
            return (bool[])array;
        }
        
        static public byte[] bytes(Object array)
        {
            return (byte[])array;
        }
        
        static public char[] chars(Object array)
        {
            return (char[])array;
        }
        
        static public short[] shorts(Object array)
        {
            return (short[])array;
        }

        static public float[] floats(Object array)
        {
            return (float[])array;
        }

        static public double[] doubles(Object array)
        {
            return (double[])array;
        }

        static public int[] ints(Object array)
        {
            return (int[])array;
        }

        static public long[] longs(Object array)
        {
            return (long[])array;
        }

        #endregion

        #region Float overloads for basic ops

        static public float add(float x, float y)
        {
            return x + y;
        }

        static public float minus(float x, float y)
        {
            return x - y;
        }

        static public float minus(float x)
        {
            return -x;
        }

        static public float inc(float x)
        {
            return x + 1;
        }

        static public float dec(float x)
        {
            return x - 1;
        }

        static public float multiply(float x, float y)
        {
            return x * y;
        }

        static public float divide(float x, float y)
        {
            return x / y;
        }

        static public bool equiv(float x, float y)
        {
            return x == y;
        }

        static public bool lt(float x, float y)
        {
            return x < y;
        }

        static public bool lte(float x, float y)
        {
            return x <= y;
        }

        static public bool gt(float x, float y)
        {
            return x > y;
        }

        static public bool gte(float x, float y)
        {
            return x >= y;
        }

        static public bool isPos(float x)
        {
            return x > 0;
        }

        static public bool isNeg(float x)
        {
            return x < 0;
        }

        static public bool isZero(float x)
        {
            return x == 0;
        }

        #endregion

        #region Double overloads for basic ops

        static public double add(double x, double y)
        {
            return x + y;
        }

        static public double minus(double x, double y)
        {
            return x - y;
        }

        static public double minus(double x)
        {
            return -x;
        }

        static public double inc(double x)
        {
            return x + 1;
        }

        static public double dec(double x)
        {
            return x - 1;
        }

        static public double multiply(double x, double y)
        {
            return x * y;
        }

        static public double divide(double x, double y)
        {
            return x / y;
        }

        static public bool equiv(double x, double y)
        {
            return x == y;
        }

        static public bool lt(double x, double y)
        {
            return x < y;
        }

        static public bool lte(double x, double y)
        {
            return x <= y;
        }

        static public bool gt(double x, double y)
        {
            return x > y;
        }

        static public bool gte(double x, double y)
        {
            return x >= y;
        }

        static public bool isPos(double x)
        {
            return x > 0;
        }

        static public bool isNeg(double x)
        {
            return x < 0;
        }

        static public bool isZero(double x)
        {
            return x == 0;
        }

        #endregion

        #region Int overloads for basic ops

        static int throwIntOverflow()
        {
            throw new ArithmeticException("integer overflow");
        }

        static public int unchecked_add(int x, int y)
        {
            return x + y;
        }

        static public int unchecked_subtract(int x, int y)
        {
            return x - y;
        }

        static public int unchecked_negate(int x)
        {
            return -x;
        }

        static public int unchecked_inc(int x)
        {
            return x + 1;
        }

        static public int unchecked_dec(int x)
        {
            return x - 1;
        }

        static public int unchecked_multiply(int x, int y)
        {
            return x * y;
        }

        static public int add(int x, int y)
        {
            int ret = x + y;
            if ((ret ^ x) < 0 && (ret ^ y) < 0)
                return throwIntOverflow();
            return ret;
        }

        static public int not(int x)
        {
            return ~x;
        }

        static public int and(int x, int y)
        {
            return x & y;
        }

        static public int or(int x, int y)
        {
            return x | y;
        }

        static public int xor(int x, int y)
        {
            return x ^ y;
        }

        static public int minus(int x, int y)
        {
            int ret = x - y;
            if (((ret ^ x) < 0 && (ret ^ ~y) < 0))
                return throwIntOverflow();
            return ret;
        }

        static public int minus(int x)
        {
            if (x == Int32.MinValue)
                return throwIntOverflow();
            return -x;
        }

        static public int inc(int x)
        {
            if (x == Int32.MaxValue)
                return throwIntOverflow();
            return x + 1;
        }

        static public int dec(int x)
        {
            if (x == Int32.MinValue)
                return throwIntOverflow();
            return x - 1;
        }

        static public int multiply(int x, int y)
        {
            int ret = x * y;
            if (y != 0 && ret / y != x)
                return throwIntOverflow();
            return ret;
        }

        static public int unchecked_divide(int x, int y)
        {
            return x / y;
        }

        static public int unchecked_remainder(int x, int y)
        {
            return x % y;
        }

        static public bool equiv(int x, int y)
        {
            return x == y;
        }

        static public bool lt(int x, int y)
        {
            return x < y;
        }

        static public bool lte(int x, int y)
        {
            return x <= y;
        }

        static public bool gt(int x, int y)
        {
            return x > y;
        }

        static public bool gte(int x, int y)
        {
            return x >= y;
        }

        static public bool isPos(int x)
        {
            return x > 0;
        }

        static public bool isNeg(int x)
        {
            return x < 0;
        }

        static public bool isZero(int x)
        {
            return x == 0;
        }

        #endregion

        #region Long overloads for basic ops

        static public long unchecked_add(long x, long y)
        {
            return x + y;
        }

        static public long unchecked_subtract(long x, long y)
        {
            return x - y;
        }

        static public long unchecked_negate(long x)
        {
            return -x;
        }

        static public long unchecked_inc(long x)
        {
            return x + 1;
        }

        static public long unchecked_dec(long x)
        {
            return x - 1;
        }

        static public long unchecked_multiply(long x, long y)
        {
            return x * y;
        }

        static public long add(long x, long y)
        {
            long ret = x + y;
            if ((ret ^ x) < 0 && (ret ^ y) < 0)
                return throwIntOverflow();
            return ret;
        }

        static public long minus(long x, long y)
        {
            long ret = x - y;
            if (((ret ^ x) < 0 && (ret ^ ~y) < 0))
                return throwIntOverflow();
            return ret;
        }

        static public long minus(long x)
        {
            if (x == Int64.MinValue)
                return throwIntOverflow();
            return -x;
        }

        static public long inc(long x)
        {
            if (x == Int64.MaxValue)
                return throwIntOverflow();
            return x + 1;
        }

        static public long dec(long x)
        {
            if (x == Int64.MinValue)
                return throwIntOverflow();
            return x - 1;
        }

        static public long multiply(long x, long y)
        {
            long ret = x * y;
            if (y != 0 && ret / y != x)
                return throwIntOverflow();
            return ret;
        }

        static public long unchecked_divide(long x, long y)
        {
            return x / y;
        }

        static public long unchecked_remainder(long x, long y)
        {
            return x % y;
        }

        static public bool equiv(long x, long y)
        {
            return x == y;
        }

        static public bool lt(long x, long y)
        {
            return x < y;
        }

        static public bool lte(long x, long y)
        {
            return x <= y;
        }

        static public bool gt(long x, long y)
        {
            return x > y;
        }

        static public bool gte(long x, long y)
        {
            return x >= y;
        }

        static public bool isPos(long x)
        {
            return x > 0;
        }

        static public bool isNeg(long x)
        {
            return x < 0;
        }

        static public bool isZero(long x)
        {
            return x == 0;
        }

        #endregion

        #region Overload resolution

        static public object add(int x, Object y)
        {
            return add((Object)x, y);
        }

        static public object add(Object x, int y)
        {
            return add(x, (Object)y);
        }

        static public object and(int x, Object y)
        {
            return and((Object)x, y);
        }

        static public object and(Object x, int y)
        {
            return and(x, (Object)y);
        }

        static public object or(int x, Object y)
        {
            return or((Object)x, y);
        }

        static public object or(Object x, int y)
        {
            return or(x, (Object)y);
        }

        static public object xor(int x, Object y)
        {
            return xor((Object)x, y);
        }

        static public object xor(Object x, int y)
        {
            return xor(x, (Object)y);
        }

        static public object add(float x, Object y)
        {
            return add((Object)x, y);
        }

        static public object add(Object x, float y)
        {
            return add(x, (Object)y);
        }

        static public object add(long x, Object y)
        {
            return add((Object)x, y);
        }

        static public object add(Object x, long y)
        {
            return add(x, (Object)y);
        }

        static public object add(double x, Object y)
        {
            return add((Object)x, y);
        }

        static public object add(Object x, double y)
        {
            return add(x, (Object)y);
        }

        static public object minus(int x, Object y)
        {
            return minus((Object)x, y);
        }

        static public object minus(Object x, int y)
        {
            return minus(x, (Object)y);
        }

        static public object minus(float x, Object y)
        {
            return minus((Object)x, y);
        }

        static public object minus(Object x, float y)
        {
            return minus(x, (Object)y);
        }

        static public object minus(long x, Object y)
        {
            return minus((Object)x, y);
        }

        static public object minus(Object x, long y)
        {
            return minus(x, (Object)y);
        }

        static public object minus(double x, Object y)
        {
            return minus((Object)x, y);
        }

        static public object minus(Object x, double y)
        {
            return minus(x, (Object)y);
        }

        static public object multiply(int x, Object y)
        {
            return multiply((Object)x, y);
        }

        static public object multiply(Object x, int y)
        {
            return multiply(x, (Object)y);
        }

        static public object multiply(float x, Object y)
        {
            return multiply((Object)x, y);
        }

        static public object multiply(Object x, float y)
        {
            return multiply(x, (Object)y);
        }

        static public object multiply(long x, Object y)
        {
            return multiply((Object)x, y);
        }

        static public object multiply(Object x, long y)
        {
            return multiply(x, (Object)y);
        }

        static public object multiply(double x, Object y)
        {
            return multiply((Object)x, y);
        }

        static public object multiply(Object x, double y)
        {
            return multiply(x, (Object)y);
        }

        static public object divide(int x, Object y)
        {
            return divide((Object)x, y);
        }

        static public object divide(Object x, int y)
        {
            return divide(x, (Object)y);
        }

        static public object divide(float x, Object y)
        {
            return divide((Object)x, y);
        }

        static public object divide(Object x, float y)
        {
            return divide(x, (Object)y);
        }

        static public object divide(long x, Object y)
        {
            return divide((Object)x, y);
        }

        static public object divide(Object x, long y)
        {
            return divide(x, (Object)y);
        }

        static public object divide(double x, Object y)
        {
            return divide((Object)x, y);
        }

        static public object divide(Object x, double y)
        {
            return divide(x, (Object)y);
        }

        static public bool lt(int x, Object y)
        {
            return lt((Object)x, y);
        }

        static public bool lt(Object x, int y)
        {
            return lt(x, (Object)y);
        }

        static public bool lt(float x, Object y)
        {
            return lt((Object)x, y);
        }

        static public bool lt(Object x, float y)
        {
            return lt(x, (Object)y);
        }

        static public bool lt(long x, Object y)
        {
            return lt((Object)x, y);
        }

        static public bool lt(Object x, long y)
        {
            return lt(x, (Object)y);
        }

        static public bool lt(double x, Object y)
        {
            return lt((Object)x, y);
        }

        static public bool lt(Object x, double y)
        {
            return lt(x, (Object)y);
        }

        static public bool lte(int x, Object y)
        {
            return lte((Object)x, y);
        }

        static public bool lte(Object x, int y)
        {
            return lte(x, (Object)y);
        }

        static public bool lte(float x, Object y)
        {
            return lte((Object)x, y);
        }

        static public bool lte(Object x, float y)
        {
            return lte(x, (Object)y);
        }

        static public bool lte(long x, Object y)
        {
            return lte((Object)x, y);
        }

        static public bool lte(Object x, long y)
        {
            return lte(x, (Object)y);
        }

        static public bool lte(double x, Object y)
        {
            return lte((Object)x, y);
        }

        static public bool lte(Object x, double y)
        {
            return lte(x, (Object)y);
        }

        static public bool gt(int x, Object y)
        {
            return gt((Object)x, y);
        }

        static public bool gt(Object x, int y)
        {
            return gt(x, (Object)y);
        }

        static public bool gt(float x, Object y)
        {
            return gt((Object)x, y);
        }

        static public bool gt(Object x, float y)
        {
            return gt(x, (Object)y);
        }

        static public bool gt(long x, Object y)
        {
            return gt((Object)x, y);
        }

        static public bool gt(Object x, long y)
        {
            return gt(x, (Object)y);
        }

        static public bool gt(double x, Object y)
        {
            return gt((Object)x, y);
        }

        static public bool gt(Object x, double y)
        {
            return gt(x, (Object)y);
        }

        static public bool gte(int x, Object y)
        {
            return gte((Object)x, y);
        }

        static public bool gte(Object x, int y)
        {
            return gte(x, (Object)y);
        }

        static public bool gte(float x, Object y)
        {
            return gte((Object)x, y);
        }

        static public bool gte(Object x, float y)
        {
            return gte(x, (Object)y);
        }

        static public bool gte(long x, Object y)
        {
            return gte((Object)x, y);
        }

        static public bool gte(Object x, long y)
        {
            return gte(x, (Object)y);
        }

        static public bool gte(double x, Object y)
        {
            return gte((Object)x, y);
        }

        static public bool gte(Object x, double y)
        {
            return gte(x, (Object)y);
        }


        static public bool equiv(int x, Object y)
        {
            //return equiv((Object)x, y);
            return EquivArg1Numeric(x, y);  // still boxes
        }

        static public bool equiv(Object x, int y)
        {
            //return equiv(x, (Object)y);
            return EquivArg2Numeric(x, y);
        }

        static public bool equiv(float x, Object y)
        {
            //return equiv((Object)x, y);
            return EquivArg1Numeric(x, y);  // still boxes
        }

        static public bool equiv(Object x, float y)
        {
            //return equiv(x, (Object)y);
            return EquivArg2Numeric(x, y);
        }

        static public bool equiv(long x, Object y)
        {
            //return equiv((Object)x, y);
            return EquivArg1Numeric(x, y);  // still boxes
        }

        static public bool equiv(Object x, long y)
        {
            //return equiv(x, (Object)y);
            return EquivArg2Numeric(x, y);
        }

        static public bool equiv(double x, Object y)
        {
            //return equiv((Object)x, y);
            return EquivArg1Numeric(x, y);  // still boxes
        }

        static public bool equiv(Object x, double y)
        {
            //return equiv(x, (Object)y);
            return EquivArg2Numeric(x, y);
        }


        static public float add(int x, float y)
        {
            return add((float)x, y);
        }

        static public float add(float x, int y)
        {
            return add(x, (float)y);
        }

        static public double add(int x, double y)
        {
            return add((double)x, y);
        }

        static public double add(double x, int y)
        {
            return add(x, (double)y);
        }

        static public long add(int x, long y)
        {
            return add((long)x, y);
        }

        static public long add(long x, int y)
        {
            return add(x, (long)y);
        }

        static public float add(long x, float y)
        {
            return add((float)x, y);
        }

        static public float add(float x, long y)
        {
            return add(x, (float)y);
        }

        static public double add(long x, double y)
        {
            return add((double)x, y);
        }

        static public double add(double x, long y)
        {
            return add(x, (double)y);
        }

        static public double add(float x, double y)
        {
            return add((double)x, y);
        }

        static public double add(double x, float y)
        {
            return add(x, (double)y);
        }

        static public float minus(int x, float y)
        {
            return minus((float)x, y);
        }

        static public float minus(float x, int y)
        {
            return minus(x, (float)y);
        }

        static public double minus(int x, double y)
        {
            return minus((double)x, y);
        }

        static public double minus(double x, int y)
        {
            return minus(x, (double)y);
        }

        static public long minus(int x, long y)
        {
            return minus((long)x, y);
        }

        static public long minus(long x, int y)
        {
            return minus(x, (long)y);
        }

        static public float minus(long x, float y)
        {
            return minus((float)x, y);
        }

        static public float minus(float x, long y)
        {
            return minus(x, (float)y);
        }

        static public double minus(long x, double y)
        {
            return minus((double)x, y);
        }

        static public double minus(double x, long y)
        {
            return minus(x, (double)y);
        }

        static public double minus(float x, double y)
        {
            return minus((double)x, y);
        }

        static public double minus(double x, float y)
        {
            return minus(x, (double)y);
        }

        static public float multiply(int x, float y)
        {
            return multiply((float)x, y);
        }

        static public float multiply(float x, int y)
        {
            return multiply(x, (float)y);
        }

        static public double multiply(int x, double y)
        {
            return multiply((double)x, y);
        }

        static public double multiply(double x, int y)
        {
            return multiply(x, (double)y);
        }

        static public long multiply(int x, long y)
        {
            return multiply((long)x, y);
        }

        static public long multiply(long x, int y)
        {
            return multiply(x, (long)y);
        }

        static public float multiply(long x, float y)
        {
            return multiply((float)x, y);
        }

        static public float multiply(float x, long y)
        {
            return multiply(x, (float)y);
        }

        static public double multiply(long x, double y)
        {
            return multiply((double)x, y);
        }

        static public double multiply(double x, long y)
        {
            return multiply(x, (double)y);
        }

        static public double multiply(float x, double y)
        {
            return multiply((double)x, y);
        }

        static public double multiply(double x, float y)
        {
            return multiply(x, (double)y);
        }

        static public float divide(int x, float y)
        {
            return divide((float)x, y);
        }

        static public float divide(float x, int y)
        {
            return divide(x, (float)y);
        }

        static public double divide(int x, double y)
        {
            return divide((double)x, y);
        }

        static public double divide(double x, int y)
        {
            return divide(x, (double)y);
        }

        static public float divide(long x, float y)
        {
            return divide((float)x, y);
        }

        static public float divide(float x, long y)
        {
            return divide(x, (float)y);
        }

        static public double divide(long x, double y)
        {
            return divide((double)x, y);
        }

        static public double divide(double x, long y)
        {
            return divide(x, (double)y);
        }

        static public double divide(float x, double y)
        {
            return divide((double)x, y);
        }

        static public double divide(double x, float y)
        {
            return divide(x, (double)y);
        }

        static public bool lt(int x, float y)
        {
            return lt((float)x, y);
        }

        static public bool lt(float x, int y)
        {
            return lt(x, (float)y);
        }

        static public bool lt(int x, double y)
        {
            return lt((double)x, y);
        }

        static public bool lt(double x, int y)
        {
            return lt(x, (double)y);
        }

        static public bool lt(int x, long y)
        {
            return lt((long)x, y);
        }

        static public bool lt(long x, int y)
        {
            return lt(x, (long)y);
        }

        static public bool lt(long x, float y)
        {
            return lt((float)x, y);
        }

        static public bool lt(float x, long y)
        {
            return lt(x, (float)y);
        }

        static public bool lt(long x, double y)
        {
            return lt((double)x, y);
        }

        static public bool lt(double x, long y)
        {
            return lt(x, (double)y);
        }

        static public bool lt(float x, double y)
        {
            return lt((double)x, y);
        }

        static public bool lt(double x, float y)
        {
            return lt(x, (double)y);
        }


        static public bool lte(int x, float y)
        {
            return lte((float)x, y);
        }

        static public bool lte(float x, int y)
        {
            return lte(x, (float)y);
        }

        static public bool lte(int x, double y)
        {
            return lte((double)x, y);
        }

        static public bool lte(double x, int y)
        {
            return lte(x, (double)y);
        }

        static public bool lte(int x, long y)
        {
            return lte((long)x, y);
        }

        static public bool lte(long x, int y)
        {
            return lte(x, (long)y);
        }

        static public bool lte(long x, float y)
        {
            return lte((float)x, y);
        }

        static public bool lte(float x, long y)
        {
            return lte(x, (float)y);
        }

        static public bool lte(long x, double y)
        {
            return lte((double)x, y);
        }

        static public bool lte(double x, long y)
        {
            return lte(x, (double)y);
        }

        static public bool lte(float x, double y)
        {
            return lte((double)x, y);
        }

        static public bool lte(double x, float y)
        {
            return lte(x, (double)y);
        }

        static public bool gt(int x, float y)
        {
            return gt((float)x, y);
        }

        static public bool gt(float x, int y)
        {
            return gt(x, (float)y);
        }

        static public bool gt(int x, double y)
        {
            return gt((double)x, y);
        }

        static public bool gt(double x, int y)
        {
            return gt(x, (double)y);
        }

        static public bool gt(int x, long y)
        {
            return gt((long)x, y);
        }

        static public bool gt(long x, int y)
        {
            return gt(x, (long)y);
        }

        static public bool gt(long x, float y)
        {
            return gt((float)x, y);
        }

        static public bool gt(float x, long y)
        {
            return gt(x, (float)y);
        }

        static public bool gt(long x, double y)
        {
            return gt((double)x, y);
        }

        static public bool gt(double x, long y)
        {
            return gt(x, (double)y);
        }

        static public bool gt(float x, double y)
        {
            return gt((double)x, y);
        }

        static public bool gt(double x, float y)
        {
            return gt(x, (double)y);
        }

        static public bool gte(int x, float y)
        {
            return gte((float)x, y);
        }

        static public bool gte(float x, int y)
        {
            return gte(x, (float)y);
        }

        static public bool gte(int x, double y)
        {
            return gte((double)x, y);
        }

        static public bool gte(double x, int y)
        {
            return gte(x, (double)y);
        }

        static public bool gte(int x, long y)
        {
            return gte((long)x, y);
        }

        static public bool gte(long x, int y)
        {
            return gte(x, (long)y);
        }

        static public bool gte(long x, float y)
        {
            return gte((float)x, y);
        }

        static public bool gte(float x, long y)
        {
            return gte(x, (float)y);
        }

        static public bool gte(long x, double y)
        {
            return gte((double)x, y);
        }

        static public bool gte(double x, long y)
        {
            return gte(x, (double)y);
        }

        static public bool gte(float x, double y)
        {
            return gte((double)x, y);
        }

        static public bool gte(double x, float y)
        {
            return gte(x, (double)y);
        }

        static public bool equiv(int x, float y)
        {
            return equiv((float)x, y);
        }

        static public bool equiv(float x, int y)
        {
            return equiv(x, (float)y);
        }

        static public bool equiv(int x, double y)
        {
            return equiv((double)x, y);
        }

        static public bool equiv(double x, int y)
        {
            return equiv(x, (double)y);
        }

        static public bool equiv(int x, long y)
        {
            return equiv((long)x, y);
        }

        static public bool equiv(long x, int y)
        {
            return equiv(x, (long)y);
        }

        static public bool equiv(long x, float y)
        {
            return equiv((float)x, y);
        }

        static public bool equiv(float x, long y)
        {
            return equiv(x, (float)y);
        }

        static public bool equiv(long x, double y)
        {
            return equiv((double)x, y);
        }

        static public bool equiv(double x, long y)
        {
            return equiv(x, (double)y);
        }

        static public bool equiv(float x, double y)
        {
            return equiv((double)x, y);
        }

        static public bool equiv(double x, float y)
        {
            return equiv(x, (double)y);
        }


        #endregion

    }
}
