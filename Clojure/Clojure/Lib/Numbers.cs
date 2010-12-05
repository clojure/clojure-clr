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
    public class Numbers
    {
        #region Ops interface

        interface Ops
        {
            Ops combine(Ops y);
            Ops opsWith(LongOps x);
            Ops opsWith(DoubleOps x);
            Ops opsWith(RatioOps x);
            Ops opsWith(BigIntegerOps x);
            Ops opsWith(BigDecimalOps x);

            bool isZero(object x);
            bool isPos(object x);
            bool isNeg(object x);
            object add(object x, object y);
            object addP(object x, object y);
            object multiply(object x, object y);
            object multiplyP(object x, object y);
            object divide(object x, object y);
            object quotient(object x, object y);
            object remainder(object x, object y);
            bool equiv(object x, object y);
            bool lt(object x, object y);
            object negate(object x);
            object negateP(object x);
            object inc(object x);
            object incP(object x);
            object dec(object x);
            object decP(object x);
        }

        #endregion

        #region OpsP interface

        abstract class OpsP : Ops
        {
            public object addP(object x, object y)
            {
                return add(x, y);
            }

            public object multiplyP(object x, object y)
            {
                return multiply(x, y);
            }

            public object negateP(object x)
            {
                return negate(x);
            }

            public object incP(object x)
            {
                return inc(x);
            }

            public object decP(object x)
            {
                return dec(x);
            }

            public abstract Ops combine(Ops y);
            public abstract Ops opsWith(LongOps x);
            public abstract Ops opsWith(DoubleOps x);
            public abstract Ops opsWith(RatioOps x);
            public abstract Ops opsWith(BigIntegerOps x);
            public abstract Ops opsWith(BigDecimalOps x);
            public abstract bool isZero(object x);
            public abstract bool isPos(object x);
            public abstract bool isNeg(object x);
            public abstract object add(object x, object y);
            public abstract object multiply(object x, object y);
            public abstract object divide(object x, object y);
            public abstract object quotient(object x, object y);
            public abstract object remainder(object x, object y);
            public abstract bool equiv(object x, object y);
            public abstract bool lt(object x, object y);
            public abstract object negate(object x);
            public abstract object inc(object x);
            public abstract object dec(object x);
        }

        #endregion

        #region BitOps interface

        interface BitOps
        {
            BitOps combine(BitOps y);
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
            return  ops(x).isZero(x);
        }

        public static bool isPos(object x)
        {
            return ops(x).isPos(x);
        }

        public static bool isNeg(object x)
        {
            return ops(x).isNeg(x);
        }

        public static object minus(object x)
        {
            return ops(x).negate(x);
        }

        public static object minusP(object x)
        {
            return ops(x).negateP(x);
        }

        public static object inc(object x)
        {
            return ops(x).inc(x);
        }

        public static object incP(object x)
        {
            return ops(x).incP(x);
        }

        public static object dec(object x)
        {
            return ops(x).dec(x);
        }

        public static object decP(object x)
        {
            return ops(x).decP(x);
        }

        public static object add(object x, object y)
        {
            return ops(x).combine(ops(y)).add(x, y);
        }

        public static object addP(object x, object y)
        {
            return ops(x).combine(ops(y)).addP(x, y);
        }

        public static object minus(object x, object y)
        {
            Ops yops = ops(y);
            return ops(x).combine(yops).add(x, yops.negate(y));
        }

        public static object minusP(object x, object y)
        {
            Ops yops = ops(y);
            return ops(x).combine(yops).addP(x, yops.negateP(y));
        }

        public static object multiply(object x, object y)
        {
            return ops(x).combine(ops(y)).multiply(x, y);
        }

        public static object multiplyP(object x, object y)
        {
            return ops(x).combine(ops(y)).multiplyP(x, y);
        }

        public static object divide(object x, object y)
        {
            Ops yops = ops(y);
            if ( yops.isZero(y) )
                throw new ArithmeticException("Divide by zero");
            return ops(x).combine(yops).divide(x, y);
        }

        public static object quotient(object x, object y)
        {
            Ops yops = ops(y);
            if (yops.isZero(y))
                throw new ArithmeticException("Divide by zero");
            return ops(x).combine(yops).quotient(x, y);
        }

        public static object remainder(object x, object y)
        {
            Ops yops = ops(y);
            if (yops.isZero(y))
                throw new ArithmeticException("Divide by zero");
            return ops(x).combine(yops).remainder(x, y);
        }


        static double DQuotient(double n, double d)
        {
            double q = n / d;
            if (q <= Int64.MaxValue && q >= Int64.MinValue)
                return (double)((long)q);
            else
                // bigint quotient
                return BigDecimal.Create(q).ToBigInteger().ToDouble(null);
        }

        static double DRemainder(double n, double d)
        {
            double q = n / d;
            if (q <= Int64.MaxValue && q >= Int64.MinValue)
                return n - ((long)q) * d;
            else
            {
                // bigint quotient
                object bq = BigDecimal.Create(q).ToBigInteger();
                return n - ((double)bq) * d;
            }
        }

        public static bool equiv(object x, object y)
        {
            return ops(x).combine(ops(y)).equiv(x,y);
        }

        public static bool lt(object x, object y)
        {
            return ops(x).combine(ops(y)).lt(x,y);
        }

        public static bool lte(object x, object y)
        {
            return !ops(x).combine(ops(y)).lt(y,x);
        }

        public static bool gt(object x, object y)
        {
            return ops(x).combine(ops(y)).lt(y,x);
        }

        public static bool gte(object x, object y)
        {
            return !ops(x).combine(ops(y)).lt(x, y);
        }

        public static int compare(object x, object y)
        {
            Ops xyops = ops(x).combine(ops(y));
            if (xyops.lt(x, y))
                return -1;
            else if (xyops.lt(y, x))
                return 1;
            else
                return 0;
        }

 
        #endregion

        #region  utility methods

        static BigInteger toBigInteger(object x)
        {
            if (x is BigInteger)
                return (BigInteger)x;
            else
                return BigInteger.Create(Util.ConvertToLong(x));
        }

        static BigDecimal toBigDecimal(object x)
        {
            if (x is BigDecimal)
                return (BigDecimal)x;
            else if (x is BigInteger)
                return BigDecimal.Create((BigInteger)x);
            else if (x is double)
                return BigDecimal.Create((double)x);
            else if (x is float)
                return BigDecimal.Create((double)(float)x);
            else if (x is Ratio)
            {
                Ratio r = (Ratio)x;
                return (BigDecimal)divide(BigDecimal.Create(r.numerator), r.denominator);
            }
            else
                return BigDecimal.Create(Util.ConvertToLong(x));
        }

        public static Ratio toRatio(object x)
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
            if (x is float)                              
                return rationalize(BigDecimal.Create((float)x));
            else if (x is double)                        
                return rationalize(BigDecimal.Create((double)x));
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


        #endregion

        #region Boxing

        static Object box(int val)
        {
            return val;
        }

        static Object box(long val)
        {
            // JVM still has this
            //if (val >= Int32.MinValue && val <= Int32.MaxValue)
            //    return (int)val;
            //else
                return val;
        }

        static Object box(double val)
        {
            return val;
        }

        static Object box(float val)
        {
            return (double)val;
        }


        #endregion

        #region More BigInteger support

        public static object reduceBigInteger(BigInteger val)
        {
            //int ival;
            //if (val.AsInt32(out ival))
            //    return ival;

            long lval;
            if (val.AsInt64(out lval))
                return lval;

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
                return reduceBigInteger(n);
            else if (d.Equals(BigInteger.NEGATIVE_ONE))
                return reduceBigInteger(n.Negate());

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

        public static object shiftLeft(object x, object n)
        {
            return bitOps(x).shiftLeft(x, Util.ConvertToInt(n));
        }

        public static int shiftLeftInt(int x, int n)
        {
            return n >= 0 ? x << n : x >> -n;
        }

        public static long shiftLeft(long x, int n)
        {
            return n >= 0 ? x << n : x >> -n;
        }


        public static object shiftRight(object x, object n)
        {
            return bitOps(x).shiftRight(x, Util.ConvertToInt(n));
        }

        public static int shiftRightInt(int x, int n)
        {
            return n >= 0 ? x >> n : x << -n;
        }

        public static long shiftRight(long x, int n)
        {
            return n >= 0 ? x >> n : x << -n;
        }

        #endregion

        #region LongOps

        sealed class LongOps : Ops
        {
            #region Ops Members

            public Ops combine(Ops y)
            {
                return y.opsWith(this);
            }

            public Ops opsWith(LongOps x)
            {
                return this;
            }

            public Ops opsWith(DoubleOps x)
            {
                return DOUBLE_OPS;
            }

            public Ops opsWith(RatioOps x)
            {
                return RATIO_OPS;
            }

            public Ops opsWith(BigIntegerOps x)
            {
                return BIGINTEGER_OPS;
            }

            public Ops opsWith(BigDecimalOps x)
            {
                return BIGDECIMAL_OPS;
            }

            public bool isZero(object x)
            {
                return Util.ConvertToLong(x) == 0;
            }

            public bool isPos(object x)
            {
                return Util.ConvertToLong(x) > 0;
            }

            public bool isNeg(object x)
            {
                return Util.ConvertToLong(x) < 0;
            }

            public object add(object x, object y)
            {
                 return box(Numbers.add( Util.ConvertToLong(x), Util.ConvertToLong(y)));
            }

            public object addP(object x, object y)
            {
                long lx = Util.ConvertToLong(x);
                long ly = Util.ConvertToLong(y);
                long ret = lx + ly;
                if ((ret ^ lx) < 0 && (ret ^ ly) < 0)
                    return BIGINTEGER_OPS.add(x, y);
                return box(ret);
            }

            public object multiply(object x, object y)
            {
                return box(Numbers.multiply(Util.ConvertToLong(x), Util.ConvertToLong(y)));
            }

            public object multiplyP(object x, object y)
            {
                long lx = Util.ConvertToLong(x);
                long ly = Util.ConvertToLong(y);

                long ret = lx * ly;
                if (ly != 0 && ret / ly != lx)
                    return BIGINTEGER_OPS.multiply(x, y);
                return box(ret);
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

            public object divide(object x, object y)
            {
                long n = Util.ConvertToLong(x);
                long val = Util.ConvertToLong(y);
                long gcd1 = gcd(n, val);
                if (gcd1 == 0)
                    return 0;

                n = n / gcd1;
                long d = val / gcd1;
                if (d == 1)
                    return box(n);
                if (d < 0)
                {
                    n = -n;
                    d = -d;
                }
                return new Ratio(BigInteger.Create(n), BigInteger.Create(d));
            }

            public object quotient(object x, object y)
            {
                return box(Util.ConvertToLong(x) / Util.ConvertToLong(y));
            }

            public object remainder(object x, object y)
            {
                return box(Util.ConvertToLong(x) % Util.ConvertToLong(y));
            }

            public bool equiv(object x, object y)
            {
                return Util.ConvertToLong(x) == Util.ConvertToLong(y);
            }

            public bool lt(object x, object y)
            {
                return Util.ConvertToLong(x) < Util.ConvertToLong(y);
            }

            public object negate(object x)
            {
                long val = Util.ConvertToLong(x);
                return box(Numbers.minus(val));
            }

            public object negateP(object x)
            {
                long val = Util.ConvertToLong(x);

                if (val > Int64.MinValue)
                    return box(-val);
                return -BigInteger.Create(val);
            }

            public object inc(object x)
            {
                long val = Util.ConvertToLong(x);
                return box(Numbers.inc(val));
            }

            public object incP(object x)
            {
                long val = Util.ConvertToLong(x);

                if (val < Int64.MaxValue)
                    return box(val + 1);
                return BIGINTEGER_OPS.inc(x);
            }

            public object dec(object x)
            {
                long val = Util.ConvertToLong(x);
                return box(Numbers.dec(val));
            }

            public object decP(object x)
            {
                long val = Util.ConvertToLong(x);

                if (val > Int64.MinValue)
                    return box(val - 1);
                return BIGINTEGER_OPS.dec(x);
            }

            #endregion
        }

        #endregion

        #region DoubleOps

        sealed class DoubleOps : OpsP
        {
            #region Ops Members

            public override Ops combine(Ops y)
            {
                return y.opsWith(this);
            }

            public override Ops opsWith(LongOps x)
            {
                return this;
            }

            public override Ops opsWith(DoubleOps x)
            {
                return this;
            }

            public override Ops opsWith(RatioOps x)
            {
                return this;
            }

            public override Ops opsWith(BigIntegerOps x)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps x)
            {
                return this;
            }

            public override bool isZero(object x)
            {
                return Util.ConvertToDouble(x) == 0;
            }

            public override bool isPos(object x)
            {
                return Util.ConvertToDouble(x) > 0;
            }

            public override bool isNeg(object x)
            {
                return Util.ConvertToDouble(x) < 0;
            }

            public override object add(object x, object y)
            {
                return Util.ConvertToDouble(x) + Util.ConvertToDouble(y);

            }

            public override object multiply(object x, object y)
            {
                return Util.ConvertToDouble(x) * Util.ConvertToDouble(y);
            }

            public override object divide(object x, object y)
            {
                return Util.ConvertToDouble(x) / Util.ConvertToDouble(y);
            }

            public override object quotient(object x, object y)
            {
                return Numbers.DQuotient(Util.ConvertToDouble(x), Util.ConvertToDouble(y));
            }

            public override object remainder(object x, object y)
            {
                return Numbers.DRemainder(Util.ConvertToDouble(x), Util.ConvertToDouble(y));
            }

            public override bool equiv(object x, object y)
            {
                return Util.ConvertToDouble(x) == Util.ConvertToDouble(y);
            }

            public override bool lt(object x, object y)
            {
                return Util.ConvertToDouble(x) < Util.ConvertToDouble(y);
            }

            public override object negate(object x)
            {
                return -Util.ConvertToDouble(x);
            }

            public override object inc(object x)
            {
                return Util.ConvertToDouble(x) + 1;
            }

            public override object dec(object x)
            {
                return Util.ConvertToDouble(x) - 1;
            }

            #endregion
        }

        #endregion

        #region RatioOps

        sealed class RatioOps : OpsP
        {
            #region Ops Members

            public override Ops combine(Ops y)
            {
                return y.opsWith(this);
            }

            public override Ops opsWith(LongOps x)
            {
                return this;
            }

            public override Ops opsWith(DoubleOps x)
            {
                return DOUBLE_OPS;
            }

            public override Ops opsWith(RatioOps x)
            {
                return this;
            }

            public override Ops opsWith(BigIntegerOps x)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps x)
            {
                return BIGDECIMAL_OPS;
            }

            //static object NormalizeRet(object ret, object x, object y)
            //{
            //    if (ret is BigInteger && !(x is BigInteger || y is BigInteger))
            //        return reduceBigInteger((BigInteger)ret);
            //    return ret;
            //}

            public override bool isZero(object x)
            {
                Ratio r = (Ratio)x;
                return r.numerator.Signum == 0;
            }

            public override bool isPos(object x)
            {
                Ratio r = (Ratio)x;
                return r.numerator.Signum > 0;
            }

            public override bool isNeg(object x)
            {
                Ratio r = (Ratio)x;
                return r.numerator.Signum < 0;
            }

            public override object add(object x, object y)
            {
                Ratio rx = toRatio(x);
                Ratio ry = toRatio(y);

                // add NormalizeRet
                return Numbers.divide(
                    ry.numerator * rx.denominator + rx.numerator * ry.denominator,
                    ry.denominator * rx.denominator);
            }

            public override object multiply(object x, object y)
            {
                Ratio rx = toRatio(x);
                Ratio ry = toRatio(y);

                // add NormalizeRet
                return Numbers.divide(
                    ry.numerator * rx.numerator,
                    ry.denominator * rx.denominator);
            }

            public override object divide(object x, object y)
            {
                Ratio rx = toRatio(x);
                Ratio ry = toRatio(y);

                // add NormalizeRet
                return Numbers.divide(
                    ry.denominator * rx.numerator,
                    ry.numerator * rx.denominator);
            }

            public override object quotient(object x, object y)
            {
                Ratio rx = toRatio(x);
                Ratio ry = toRatio(y);

                // add NormalizeRet
                BigInteger q = (rx.numerator * ry.denominator) / (rx.denominator * ry.numerator);
                return q;
            }

            public override object remainder(object x, object y)
            {
                Ratio rx = toRatio(x);
                Ratio ry = toRatio(y);

                // add NormalizeRet
                BigInteger q = (rx.numerator * ry.denominator) / (rx.denominator * ry.numerator);
                return Numbers.minus(rx, Numbers.multiply(q, ry));
            }

            public override bool equiv(object x, object y)
            {
                Ratio rx = toRatio(x);
                Ratio ry = toRatio(y);
                
                return rx.numerator.Equals(ry.numerator)
                    && rx.denominator.Equals(ry.denominator);
            }

            public override bool lt(object x, object y)
            {
                Ratio rx = toRatio(x);
                Ratio ry = toRatio(y);

                return rx.numerator * ry.denominator < ry.numerator * rx.denominator;
            }

            public override object negate(object x)
            {
                Ratio rx = (Ratio)x;    
                return new Ratio(-rx.numerator, rx.denominator);
            }

            //static readonly Ratio ONE = new Ratio(BigInteger.ONE, BigInteger.ONE);
            //static readonly Ratio MINUS_ONE = new Ratio(BigInteger.NEGATIVE_ONE, BigInteger.ONE);

            public override object inc(object x)
            {
                return Numbers.add(x, 1);
            }

            public override object dec(object x)
            {
                return Numbers.add(x, -1);
            }

            #endregion
        }

        #endregion

        #region BigIntegerOps

        class BigIntegerOps : OpsP
        {
            #region Ops Members

            public override Ops combine(Ops y)
            {
                return y.opsWith(this);
            }

            public override Ops opsWith(LongOps x)
            {
                return this;
            }

            public override Ops opsWith(DoubleOps x)
            {
                return DOUBLE_OPS;
            }

            public override Ops opsWith(RatioOps x)
            {
                return RATIO_OPS;
            }

            public override Ops opsWith(BigIntegerOps ops)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps ops)
            {
                return BIGDECIMAL_OPS;
            }

            public override bool isZero(object x)
            {
                BigInteger bx = toBigInteger(x);
                return bx.IsZero;
            }

            public override bool isPos(object x)
            {
                BigInteger bx = toBigInteger(x);
                return bx.IsPositive;
            }

            public override bool isNeg(object x)
            {
                BigInteger bx = toBigInteger(x);
                return bx.IsNegative;
            }

            public override object add(object x, object y)
            {
                return reduceBigInteger(toBigInteger(x) + toBigInteger(y));
            }

            public override object multiply(object x, object y)
            {
                return reduceBigInteger(toBigInteger(x) * toBigInteger(y));
            }

            public override object divide(object x, object y)
            {
                return BIDivide(toBigInteger(x),toBigInteger(y));
            }

            public override object quotient(object x, object y)
            {
                return reduceBigInteger(toBigInteger(x) / toBigInteger(y));
            }

            public override object remainder(object x, object y)
            {
                return reduceBigInteger(toBigInteger(x) % toBigInteger(y));
            }

            public override bool equiv(object x, object y)
            {
                return toBigInteger(x).Equals(toBigInteger(y));
            }

            public override bool lt(object x, object y)
            {
                return toBigInteger(x) < toBigInteger(y);
            }

            public override object negate(object x)
            {
                return reduceBigInteger(-toBigInteger(x));
            }

            public override object inc(object x)
            {
                return reduceBigInteger(toBigInteger(x) + BigInteger.ONE);
            }

            public override object dec(object x)
            {
                return reduceBigInteger(toBigInteger(x) - BigInteger.ONE);
            }

            #endregion
        }

        #endregion

        #region BigDecimalOps

        class BigDecimalOps : OpsP
        {
            #region Ops Members

            public override Ops combine(Ops y)
            {
                return y.opsWith(this);
            }

            public override Ops opsWith(LongOps x)
            {
                return this;
            }

            public override Ops opsWith(DoubleOps x)
            {
                return DOUBLE_OPS;
            }

            public override Ops opsWith(RatioOps x)
            {
                return this;
            }

            public override Ops opsWith(BigIntegerOps x)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps x)
            {
                return this;
            }

            public override bool isZero(object x)
            {
                BigDecimal bx = (BigDecimal)x;
                return bx.IsZero;
            }

            public override bool isPos(object x)
            {
                BigDecimal bx = (BigDecimal)x;
                return bx.IsPositive;
            }

            public override bool isNeg(object x)
            {
                BigDecimal bx = (BigDecimal)x;
                return bx.IsNegative;
            }

            public override object add(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? toBigDecimal(x).Add(toBigDecimal(y))
                    : toBigDecimal(x).Add(toBigDecimal(y), c.Value);
            }

            public override object multiply(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                   ? toBigDecimal(x).Multiply(toBigDecimal(y))
                   : toBigDecimal(x).Multiply(toBigDecimal(y), c.Value);
            }

            public override object divide(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? toBigDecimal(x).Divide(toBigDecimal(y))
                    : toBigDecimal(x).Divide(toBigDecimal(y), c.Value);
            }

            public override object quotient(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? toBigDecimal(x).DivideInteger(toBigDecimal(y))
                    : toBigDecimal(x).DivideInteger(toBigDecimal(y), c.Value);
            }

            public override object remainder(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? toBigDecimal(x).Mod(toBigDecimal(y))
                    : toBigDecimal(x).Mod(toBigDecimal(y), c.Value);
            }

            public override bool equiv(object x, object y)
            {
                return toBigDecimal(x).Equals(toBigDecimal(y));
            }

            public override bool lt(object x, object y)
            {
                return toBigDecimal(x).CompareTo(toBigDecimal(y)) < 0;
            }

            public override object negate(object x)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                return c == null
                    ? ((BigDecimal)x).Negate()
                    : ((BigDecimal)x).Negate(c.Value);
            }

            public override object inc(object x)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                BigDecimal bx = (BigDecimal)x;
                return c == null
                    ? bx.Add(BigDecimal.ONE)
                    : bx.Add(BigDecimal.ONE, c.Value);
            }

            public override object dec(object x)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MATH_CONTEXT.deref();
                BigDecimal bx = (BigDecimal)x;
                return c == null
                    ? bx.Subtract(BigDecimal.ONE)
                    : bx.Subtract(BigDecimal.ONE, c.Value);
            }

            #endregion
        }

        #endregion

        #region LongBitOps

        class LongBitOps : BitOps
        {
            #region BitOps Members

            public BitOps combine(BitOps y)
            {
                return y.bitOpsWith(this);
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
                 return box(~Util.ConvertToLong(x));
            }

            public object and(object x, object y)
            {
                return box(Util.ConvertToLong(x) & Util.ConvertToLong(y));
            }

            public object or(object x, object y)
            {
                return box(Util.ConvertToLong(x) | Util.ConvertToLong(y));
            }

            public object xor(object x, object y)
            {
                return box(Util.ConvertToLong(x) ^ Util.ConvertToLong(y));
            }

            public object andNot(object x, object y)
            {
                return box(Util.ConvertToLong(x) & ~Util.ConvertToLong(y));
            }

            public object clearBit(object x, int n)
            {
                if (n < 63)
                    return box(Util.ConvertToLong(x) & ~(1L << n));
                else
                    return toBigInteger(x).ClearBit(n);
            }

            public object setBit(object x, int n)
            {
                if (n < 63)
                    return box(Util.ConvertToLong(x) | (1L << n)); 
                else
                    return toBigInteger(x).SetBit(n);
            }

            public object flipBit(object x, int n)
            {
                if (n < 63)
                    return box(Util.ConvertToLong(x) ^ (1L << n));
                else
                    return toBigInteger(x).FlipBit(n);
            }

            public bool testBit(object x, int n)
            {
                if (n < 63)
                    return (Util.ConvertToLong(x) & (1L << n)) != 0;
                else
                    return toBigInteger(x).TestBit(n);
            }

            public object shiftLeft(object x, int n)
            {
                return n < 0
                    ? shiftRight(x, -n)
                    : box(Numbers.shiftLeft(Util.ConvertToLong(x), n));
            }

            public object shiftRight(object x, int n)
            {
                return n < 0
                     ? shiftLeft(x, -n)
                     : box(Util.ConvertToLong(x) >> n);
            }

            #endregion
        }

        #endregion

        #region BigIntegerBitOps

        class BigIntegerBitOps : BitOps
        {
            #region BitOps Members

            public BitOps combine(BitOps y)
            {
                return y.bitOpsWith(this);
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
                return ~toBigInteger(x);
            }

            public object and(object x, object y)
            {
                return toBigInteger(x) & toBigInteger(y);
            }

            public object or(object x, object y)
            {
                return toBigInteger(x) | toBigInteger(y);

            }

            public object xor(object x, object y)
            {
                return toBigInteger(x) ^ toBigInteger(y);
            }

            public object andNot(object x, object y)
            {
                return toBigInteger(x).BitwiseAndNot(toBigInteger(y));
            }

            public object clearBit(object x, int n)
            {
                return toBigInteger(x).ClearBit(n);
            }

            public object setBit(object x, int n)
            {
                return toBigInteger(x).SetBit(n);
            }

            public object flipBit(object x, int n)
            {
                return toBigInteger(x).FlipBit(n);
            }

            public bool testBit(object x, int n)
            {
                return toBigInteger(x).TestBit(n);
            }

            public object shiftLeft(object x, int n)
            {
                return toBigInteger(x) << n;
            }

            public object shiftRight(object x, int n)
            {
                return toBigInteger(x) >> n;
            }

            #endregion
        }

        #endregion

        #region Ops/BitOps dispatching

        static readonly LongOps LONG_OPS = new LongOps();
        static readonly DoubleOps DOUBLE_OPS = new DoubleOps();
        static readonly RatioOps RATIO_OPS = new RatioOps();
        static readonly BigIntegerOps BIGINTEGER_OPS = new BigIntegerOps();
        static readonly BigDecimalOps BIGDECIMAL_OPS = new BigDecimalOps();

        static readonly LongBitOps LONG_BITOPS = new LongBitOps();
        static readonly BigIntegerBitOps BIGINTEGER_BITOPS = new BigIntegerBitOps();

        static Ops ops(Object x)
        {
            Type xc = x.GetType();

            if (xc == typeof(long))
                return LONG_OPS;
            else if (xc == typeof(double))
                return DOUBLE_OPS;
            else if (xc == typeof(int))
                return LONG_OPS;
            else if (xc == typeof(float))
                return DOUBLE_OPS;
            else if (xc == typeof(BigInteger))
                return BIGINTEGER_OPS;
            else if (xc == typeof(Ratio))
                return RATIO_OPS;
            else if (xc == typeof(BigDecimal))
                return BIGDECIMAL_OPS;
            else
                return LONG_OPS;
        }

        static BitOps bitOps(object x)
        {
            //Type type = Util.GetNonNullableType(x.GetType());
            Type type = x.GetType();

            //if (!type.IsEnum)     // convert fix
            //{

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int32:
                        return LONG_BITOPS;
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
            return LONG_BITOPS;
        }

        #endregion
       
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

        #region Double overloads for basic ops

        public static object num(double x)
        {
            return x;
        }

        static public double add(double x, double y)
        {
            return x + y;
        }

        static public double addP(double x, double y)
        {
            return x + y;
        }

        static public double minus(double x, double y)
        {
            return x - y;
        }

        static public double minusP(double x, double y)
        {
            return x - y;
        }

        static public double minus(double x)
        {
            return -x;
        }

        static public double minusP(double x)
        {
            return -x;
        }

        static public double inc(double x)
        {
            return x + 1;
        }

        static public double incP(double x)
        {
            return x + 1;
        }

        static public double dec(double x)
        {
            return x - 1;
        }

        static public double decP(double x)
        {
            return x - 1;
        }

        static public double multiply(double x, double y)
        {
            return x * y;
        }

        static public double multiplyP(double x, double y)
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

        public static object num(int x)
        {
            return x;
        }

        static public int unchecked_int_add(int x, int y)
        {
            return x + y;
        }

        static public int unchecked_int_subtract(int x, int y)
        {
            return x - y;
        }

        static public int unchecked_int_negate(int x)
        {
            return -x;
        }

        static public int unchecked_int_inc(int x)
        {
            return x + 1;
        }

        static public int unchecked_int_dec(int x)
        {
            return x - 1;
        }

        static public int unchecked_int_multiply(int x, int y)
        {
            return x * y;
        }

        static public int unchecked_int_divide(int x, int y)
        {
            return x / y;
        }

        static public int unchecked_int_remainder(int x, int y)
        {
            return x % y;
        }

        #endregion

        #region Long overloads for basic ops

        public static object num(long x)
        {
            return x;
        }

        static public long unchecked_long_add(long x, long y)
        {
            return x + y;
        }

        static public long unchecked_long_subtract(long x, long y)
        {
            return x - y;
        }

        static public long unchecked_long_negate(long x)
        {
            return -x;
        }

        static public long unchecked_long_inc(long x)
        {
            return x + 1;
        }

        static public long unchecked_long_dec(long x)
        {
            return x - 1;
        }

        static public long unchecked_long_multiply(long x, long y)
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

        static public object addP(long x, long y)
        {
            long ret = x + y;
            if ((ret ^ x) < 0 && (ret ^ y) < 0)
                return addP((object)x,(object)y);
            return box(ret);
        }

        static public long minus(long x, long y)
        {
            long ret = x - y;
            if (((ret ^ x) < 0 && (ret ^ ~y) < 0))
                return throwIntOverflow();
            return ret;
        }

        static public object minusP(long x, long y)
        {
            long ret = x - y;
            if (((ret ^ x) < 0 && (ret ^ ~y) < 0))
                return minusP((object)x, (object)y);
            return box(ret);
        }

        static public long minus(long x)
        {
            if (x == Int64.MinValue)
                return throwIntOverflow();
            return -x;
        }

        static public object minusP(long x)
        {
            if (x == Int64.MinValue)
                return BigInteger.Create(x).Negate();
            return box(-x);
        }

        static public long inc(long x)
        {
            if (x == Int64.MaxValue)
                return throwIntOverflow();
            return x + 1;
        }

        static public object incP(long x)
        {
            if (x == Int64.MaxValue)
                return BIGINTEGER_OPS.inc((object)x);
            return box(x + 1);
        }

        static public long dec(long x)
        {
            if (x == Int64.MinValue)
                return throwIntOverflow();
            return x - 1;
        }

        static public object decP(long x)
        {
            if (x == Int64.MinValue)
                return BIGINTEGER_OPS.dec((object)x);
            return box(x - 1);
        }

        static public long multiply(long x, long y)
        {
            long ret = x * y;
            if (y != 0 && ret / y != x)
                return throwIntOverflow();
            return ret;
        }

        static public object multiplyP(long x, long y)
        {
            long ret = x * y;
            if (y != 0 && ret / y != x)
                return multiplyP((object)x, (object)y);
            return box(ret);
        }

        static public long unchecked_long_divide(long x, long y)
        {
            return x / y;
        }

        static public long unchecked_long_remainder(long x, long y)
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

        static public object add(long x, Object y)
        {
            return add((Object)x, y);
        }

        static public object add(Object x, long y)
        {
            return add(x, (Object)y);
        }

        static public object addP(long x, Object y)
        {
            return addP((Object)x, y);
        }

        static public object addP(Object x, long y)
        {
            return addP(x, (Object)y);
        }

        static public double add(double x, Object y)
        {
            return add(x, Util.ConvertToDouble(y));
        }

        static public double add(Object x, double y)
        {
            return add(Util.ConvertToDouble(x), y);
        }

        static public double add(double x, long y)
        {
            return x + y;
        }

        static public double add(long x, double y)
        {
            return x + y;
        }

        static public double addP(double x, Object y)
        {
            return addP(x, Util.ConvertToDouble(y));
        }

        static public double addP(Object x, double y)
        {
            return addP(Util.ConvertToDouble(x), y);
        }

        static public double addP(double x, long y)
        {
            return x + y;
        }

        static public double addP(long x, double y)
        {
            return x + y;
        }

        static public object minus(long x, Object y)
        {
            return minus((Object)x, y);
        }

        static public object minus(Object x, long y)
        {
            return minus(x, (Object)y);
        }

        static public object minusP(long x, Object y)
        {
            return minusP((Object)x, y);
        }

        static public object minusP(Object x, long y)
        {
            return minusP(x, (Object)y);
        }

        static public double minus(double x, Object y)
        {
            return minus(x, Util.ConvertToDouble(y));
        }

        static public double minus(Object x, double y)
        {
            return minus(Util.ConvertToDouble(x), y);
        }

        static public double minus(double x, long y)
        {
            return x - y;
        }

        static public double minus(long x, double y)
        {
            return x - y;
        }

        static public double minusP(double x, Object y)
        {
            return minusP(x, Util.ConvertToDouble(y));
        }

        static public double minusP(Object x, double y)
        {
            return minusP(Util.ConvertToDouble(x), y);
        }

        static public double minusP(double x, long y)
        {
            return x - y;
        }

        static public double minusP(long x, double y)
        {
            return x - y;
        }

        static public object multiply(long x, Object y)
        {
            return multiply((Object)x, y);
        }

        static public object multiply(Object x, long y)
        {
            return multiply(x, (Object)y);
        }

        static public object multiplyP(long x, Object y)
        {
            return multiplyP((Object)x, y);
        }

        static public object multiplyP(Object x, long y)
        {
            return multiplyP(x, (Object)y);
        }

        static public double multiply(double x, Object y)
        {
            return multiply(x, Util.ConvertToDouble(y));
        }

        static public double multiply(Object x, double y)
        {
            return multiply(Util.ConvertToDouble(x), y);
        }

        static public double multiply(double x, long y)
        {
            return x * y;
        }

        static public double multiply(long x, double y)
        {
            return x*y;
        }

        static public double multiplyP(double x, Object y)
        {
            return multiplyP(x, Util.ConvertToDouble(y));
        }

        static public double multiplyP(Object x, double y)
        {
            return multiplyP(Util.ConvertToDouble(x), y);
        }

        static public double multiplyP(double x, long y)
        {
            return x * y;
        }

        static public double multiplyP(long x, double y)
        {
            return x * y;
        }

        static public object divide(long x, Object y)
        {
            return divide((Object)x, y);
        }

        static public object divide(Object x, long y)
        {
            return divide(x, (Object)y);
        }

        static public double divide(double x, Object y)
        {
            return x / Util.ConvertToDouble(y);
        }

        static public double divide(Object x, double y)
        {
            return Util.ConvertToDouble(x) / y;
        }

        static public double divide(double x, long y)
        {
            return x / y;
        }

        static public double divide(long x, double y)
        {
            return x / y;
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
            return x < Util.ConvertToDouble(y);
        }

        static public bool lt(Object x, double y)
        {
            return Util.ConvertToDouble(x) < y;
        }

        static public bool lt(double x, long y)
        {
            return x < y;
        }

        static public bool lt(long x, double y)
        {
            return x < y;
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
            return x <= Util.ConvertToDouble(y);
        }

        static public bool lte(Object x, double y)
        {
            return Util.ConvertToDouble(x) <= y;
        }

        static public bool lte(double x, long y)
        {
            return x <= y;
        }

        static public bool lte(long x, double y)
        {
            return x <= y;
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
            return x > Util.ConvertToDouble(y);
        }

        static public bool gt(Object x, double y)
        {
            return Util.ConvertToDouble(x) > y;
        }

        static public bool gt(double x, long y)
        {
            return x > y;
        }

        static public bool gt(long x, double y)
        {
            return x > y;
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
            return x >= Util.ConvertToDouble(y);
        }

        static public bool gte(Object x, double y)
        {
            return Util.ConvertToDouble(x) >= y;
        }

        static public bool gte(double x, long y)
        {
            return x >= y;
        }

        static public bool gte(long x, double y)
        {
            return x >= y;
        }

        static public bool equiv(long x, Object y)
        {
            return equiv((Object)x, y);
        }

        static public bool equiv(Object x, long y)
        {
            return equiv(x, (Object)y);
        }

        static public bool equiv(double x, Object y)
        {
            return x == Util.ConvertToDouble(y);
        }

        static public bool equiv(Object x, double y)
        {
            return Util.ConvertToDouble(x) == y;
        }

        static public bool equiv(double x, long y)
        {
            return x == y;
        }

        static public bool equiv(long x, double y)
        {
            return x == y;
        }

        #endregion

    }
}
