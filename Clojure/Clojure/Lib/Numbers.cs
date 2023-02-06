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
    public static class Numbers
    {
        /* 
         * Random notes so I don't forget this stuff when I return here.
         * 
         * Interface Ops provides the basic arithmetic operations.
         * It has specialized implementations based on the type of arguments.
         * For these notes, we will designate these implentations as follows:
         *   L (Long, for signed integer types)
         *   D (Double, for floating point types)
         *   R (Ratio)
         *   BI (BigInteger)
         *   BD (BigDouble)
         *   
         * The categories listed above are in the JVM version.  
         * I'm writing down this analysis in part so I can figure
         * out how to add in some some types that the CLR has and the JVM doesn't:
         * 
         *   UL (ULong, for unsigned integer types)
         *   CD (CLR decimal type)
         *   
         * Ops works with objects, so the primitive arguments  are going to be boxed.
         * This inefficiency is reduced by an analysis done at a compile time that 
         * looks at argument types and uses CLR-intrinsic ops directly to avoid boxing.
         * Here, we are concerned with the versions that get invoked with generally 
         * incomplete type information or operations/types that do not map to CLR intrinsics.
         * 
         * Ops has operations in the following categories:
         * 
         *    value:  isZero, isPos, isNeg
         *    basic arithmetic, checked, non-promoting:  add, multiply, negate, inc, dec
         *    basic artithmetic, unchecked, non-promoting: unchecked_add, unchecked_multiply, unchecked_negate, unchecked_inc, unchecked_dec
         *    basic arithmetic, promoting: addP, multiplyP, negateP, incP, decP
         *    other arithmetic: divide, quotient, remainder
         *    comparisons:  equiv, lt, lte gte
         * 
         * The abstract class OpsP is the base class for promoting types.
         * It provides implementations for the unchecked and promoting ops that default to the standard ones:
         *     addP,      unchecked_add          => add
         *     multiplyP, unchecked_multiply     => multiply
         *     negateP,   unchecked_negate       => negate
         *     incP,      unchecked_inc          => inc
         *     decP,      unchecked_dec          => dec
         *  
         * The implementations for D, R, BI, BD build on OpsP.
         * The implementation for L implements Ops directly, so it has to provide implementations for the 10 methods listed above.
         * 
         * By analogy, we will make UL implement Ops directly.
         * As for CD, the question is more interesting.  
         * Ignoring D, the others--R, BI, BD--effectivly cannot overflow -- they do not have max or min values.
         * In this respect, CD is more like L and UL.  It can overflow.
         * However, unlike L/UL, it does not have checked primitve ops.
         * We have to do our own detection.
         * At any rate, CD implements Ops and does not use OpsP as a base.
         * 
         * 
         * Promotion/contagion are handled by 'combining.
         * Ops provides the methods:
         * 
         *             Ops combine(Ops y);
         *             Ops opsWith(LongOps x);
         *             Ops opsWith(ULongOps x);
         *             Ops opsWith(DoubleOps x);
         *             Ops opsWith(RatioOps x);
         *             Ops opsWith(ClrDecimalOps x); 
         *             Ops opsWith(BigIntOps x);
         *             Ops opsWith(BigDecimalOps x);
         * 
         * A unary operation dispatches on the type of the argument and defers to the appropriate implentaton.
         * For example: 
         * 
         *          public static bool isZero(object x)
         *          {
         *              return  ops(x).isZero(x);
         *          }

         * A binary operation gets the appropriate implementation for the type of each argument, 
         * then combines them to get the contagion/promotion implementation type
         * 
         *         public static object add(object x, object y)
         *         {
         *             return ops(x).combine(ops(y)).add(x, y);
         *         }
         *         
         * So each implementation encodes the promotion/contagion against the second argument, via the combine method.
         * Fortunately, as implemented, this is symmetric.
         * For the types shared with the JVM, we have this:
         * 
         *         |  L |  D |  R | BI | BD |
         *    ===============================
         *     L   |  L |  D |  R | BI | BD |
         *    ===============================
         *     D   |  D |  D |  D |  D |  D |
         *    ===============================
         *     R   |  R |  D |  R |  R | BD |
         *    ===============================
         *     BI  | BI |  D |  R | BI | BD |
         *    ===============================
         *     BD  | BD |  D | BD | BD | BD |
         *    ===============================
         *    
         * We can that Double contaminates everything.
         * The other conversions are promotions according to:
         *    L => BI => R => BD
         * which makes sense in terms of convertibility/representation of values,
         * i.e., these are widening conversion.
         * 
         * To add UL and CD to this, we should follow this pattern as much as possible.Neither conversion is widening.
         * Each has values that cannot be represented in the other.
         * A more sophisticated analysis might look at unsigned short and say it could be converted to either unsigned long or to long.
         * However, as this code has been constructed, that's a bridge too far.  We will consider unsigned as one category, signed as another.
         * That means that a L/UL combination should go to the type that can represent both of them, which is BI.
         * L, UL to CD is a widening conversion, so that is okay.  Other combinations with CD should go up into BD.
         * And D is still fatally contagious.  
         * 
         * That leads us to this completion:
         * 
         *      *  |  L |  D |  R | BI | BD | UL | CD |
         *    =========================================
         *     L   |  L |  D |  R | BI | BD | BI | CD |
         *    =========================================
         *     D   |  D |  D |  D |  D |  D |  D |  D |
         *    =========================================
         *     R   |  R |  D |  R |  R | BD |  R | BD |
         *    =========================================
         *     BI  | BI |  D |  R | BI | BD | BI | BD |
         *    =========================================
         *     BD  | BD |  D | BD | BD | BD | BD | BD |
         *    =========================================
         *     UL  | BI |  D |  R | BI | BD | UL | CD |
         *    =========================================
         *     CD  | CD |  D | BD | BD | BD | CD | CD |
         *    =========================================
         * 
         * 
         * 
         */

        #region Ops interface

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        interface Ops
        {
            Ops combine(Ops y);
            Ops opsWith(LongOps x);
            Ops opsWith(ULongOps x); 
            Ops opsWith(DoubleOps x);
            Ops opsWith(RatioOps x);
            Ops opsWith(ClrDecimalOps x); 
            Ops opsWith(BigIntOps x);
            Ops opsWith(BigDecimalOps x);

            bool isZero(object x);
            bool isPos(object x);
            bool isNeg(object x);
            object add(object x, object y);
            object addP(object x, object y);
            object unchecked_add(object x, object y);
            object multiply(object x, object y);
            object multiplyP(object x, object y);
            object unchecked_multiply(object x, object y);
            object divide(object x, object y);
            object quotient(object x, object y);
            object remainder(object x, object y);
            bool equiv(object x, object y);
            bool lt(object x, object y);
            bool lte(object x, object y);
            bool gte(object x, object y);
            object negate(object x);
            object negateP(object x);
            object unchecked_negate(object x);
            object inc(object x);
            object incP(object x);
            object unchecked_inc(object x);
            object dec(object x);
            object decP(object x);
            object unchecked_dec(object x);

            object abs(object x);
        }

        #endregion

        #region OpsP class

        abstract class OpsP : Ops
        {
            public object addP(object x, object y)
            {
                return add(x, y);
            }

            public object unchecked_add(object x, object y)
            {
                return add(x, y);
            }

            public object multiplyP(object x, object y)
            {
                return multiply(x, y);
            }

            public object unchecked_multiply(object x, object y)
            {
                return multiply(x, y);
            }

            public object negateP(object x)
            {
                return negate(x);
            }

            public object unchecked_negate(object x)
            {
                return negate(x);
            }

            public object incP(object x)
            {
                return inc(x);
            }
            
            public object unchecked_inc(object x)
            {
                return inc(x);
            }

            public object decP(object x)
            {
                return dec(x);
            }


            public object unchecked_dec(object x)
            {
                return dec(x);
            }

            public abstract Ops combine(Ops y);
            public abstract Ops opsWith(LongOps x);
            public abstract Ops opsWith(ULongOps x);
            public abstract Ops opsWith(DoubleOps x);
            public abstract Ops opsWith(RatioOps x);
            public abstract Ops opsWith(ClrDecimalOps x);
            public abstract Ops opsWith(BigIntOps x);
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
            public abstract bool lte(object x, object y);
            public abstract bool gte(object x, object y); 
            public abstract object negate(object x);
            public abstract object inc(object x);
            public abstract object dec(object x);
            public abstract object abs(object x);
        }

        #endregion

        #region Basic Ops operations
#pragma warning disable IDE1006 // Naming Styles
        #region isZero, isPos, isNeg



        public static bool isZero(object x) { return ops(x).isZero(x); }



        public static bool isZero(double x) { return x == 0; }


        public static bool isZero(long x) { return x == 0; }


        public static bool isZero(ulong x) { return x == 0; }


        public static bool isZero(decimal x) { return x == 0m;  }




        public static bool isPos(object x) { return ops(x).isPos(x); }


        public static bool isPos(double x) { return x > 0; }


        public static bool isPos(long x) { return x > 0; }


        public static bool isPos(ulong x) { return x > 0; }


        public static bool isPos(decimal x) { return x > 0m; }




        public static bool isNeg(object x) { return ops(x).isNeg(x); }


        public static bool isNeg(double x) { return x < 0; }


        public static bool isNeg(long x) { return x < 0; }


#pragma warning disable IDE0060 // Remove unused parameter
        public static bool isNeg(ulong x) { return false; }
#pragma warning restore IDE0060 // Remove unused parameter


        public static bool isNeg(decimal x) { return x < 0m; }

        #endregion

        #region add, addP, unchecked_add


        public static object add(object x, object y) { return ops(x).combine(ops(y)).add(x, y); }


        public static double add(double x, double y) { return x + y; }


        public static long add(long x, long y)
        {
            long ret = x + y;
            if ((ret ^ x) < 0 && (ret ^ y) < 0)
                return ThrowIntOverflow();
            return ret;
        }


        public static ulong add(ulong x, ulong y)
        {
            if (x > UInt64.MaxValue - y)
                throw new ArithmeticException("integer overflow");
            return x + y; ;
        }


        public static decimal add(decimal x, decimal y) { return x + y; }


        public static double add(double x, Object y) { return add(x, Util.ConvertToDouble(y)); }


        public static double add(Object x, double y) { return add(Util.ConvertToDouble(x), y); }


        public static double add(double x, long y) { return x + y; }


        public static double add(long x, double y) { return x + y; }


        public static double add(double x, ulong y) { return x + y; }


        public static double add(ulong x, double y) { return x + y; }


        public static object add(long x, Object y) { return add((Object)x, y); }


        public static object add(Object x, long y) { return add(x, (Object)y); }


        public static object add(ulong x, Object y) { return add((Object)x, y); }


        public static object add(Object x, ulong y) { return add(x, (Object)y); }

        // Interesting problem mixing long/ulong -- what should the semantics be?
        // easiest is to punt.

        public static object add(long x, ulong y) { return add((Object)x, (Object)y); }


        public static object add(ulong x, long y) { return add((Object)x, (Object)y); }




        public static object addP(object x, object y) { return ops(x).combine(ops(y)).addP(x, y); }


        public static double addP(double x, double y) { return x + y; }


        public static object addP(long x, long y)
        {
            long ret = x + y;
            if ((ret ^ x) < 0 && (ret ^ y) < 0)
                return addP((object)x, (object)y);
            return num(ret);
        }


        public static object addP(ulong x, ulong y)
        {
            if (x > UInt64.MaxValue - y)
                return addP((object)x, (object)y);
            return num(x + y);
        }


        public static object addP(decimal x, decimal y)
        {
            if (x > 0m && y > 0m && x > Decimal.MaxValue - y)
                return addP((object)x, (object)y);
            else if (x < 0m && y < 0m && x < Decimal.MinValue - y)
                return addP((object)x, (object)y);
            return num(x + y);
        }


        public static double addP(double x, Object y) { return addP(x, Util.ConvertToDouble(y)); }


        public static double addP(Object x, double y) { return addP(Util.ConvertToDouble(x), y); }


        public static double addP(double x, long y) { return x + y; }


        public static double addP(ulong x, double y) { return x + y; }


        public static double addP(double x, ulong y) { return x + y; }


        public static double addP(long x, double y) { return x + y; }


        public static object addP(long x, Object y) { return addP((Object)x, y); }


        public static object addP(Object x, long y) { return addP(x, (Object)y); }


        public static object addP(ulong x, Object y) { return addP((Object)x, y); }


        public static object addP(Object x, ulong y) { return addP(x, (Object)y); }


        public static object addP(long x, ulong y) { return addP((Object)x, (Object)y); }


        public static object addP(ulong x, long y) { return addP((Object)x, (Object)y); }




        public static object unchecked_add(object x, object y) { return ops(x).combine(ops(y)).unchecked_add(x, y); }



        public static double unchecked_add(double x, double y) { return add(x, y); }



        public static long unchecked_add(long x, long y) { return x + y; }



        public static ulong unchecked_add(ulong x, ulong y) { return x + y; }



        public static decimal unchecked_add(decimal x, decimal y) { return x + y; }



        public static double unchecked_add(double x, object y) { return add(x, y); }



        public static double unchecked_add(object x, double y) { return add(x, y); }



        public static double unchecked_add(double x, long y) { return add(x, y); }



        public static double unchecked_add(long x, double y) { return add(x, y); }



        public static double unchecked_add(double x, ulong y) { return add(x, y); }



        public static double unchecked_add(ulong x, double y) { return add(x, y); }



        public static object unchecked_add(long x, object y) { return unchecked_add((Object)x, y); }



        public static object unchecked_add(object x, long y) { return unchecked_add(x, (Object)y); }



        public static object unchecked_add(ulong x, object y) { return unchecked_add((Object)x, y); }



        public static object unchecked_add(object x, ulong y) { return unchecked_add(x, (Object)y); }



        public static object unchecked_add(long x, ulong y) { return unchecked_add((Object)x, (Object)y); }



        public static object unchecked_add(ulong x, long y) { return unchecked_add((Object)x, (Object)y); }

        #endregion

        #region minus, minusP, unchecked_minus


        public static object minus(object x) { return ops(x).negate(x); }


        public static double minus(double x) { return -x; }


        public static long minus(long x)
        {
            if (x == Int64.MinValue)
                return ThrowIntOverflow();
            return -x;
        }


        public static ulong minus(ulong x)
        {
            if (x == 0)
                return x;
            throw new ArithmeticException("integer overflow");
        }


        public static decimal minus(decimal x) { return -x; }




        public static object minusP(object x) { return ops(x).negateP(x); }


        public static double minusP(double x) { return -x; }
        

        public static object minusP(long x)
        {
            if (x == Int64.MinValue)
                return BigInt.fromBigInteger(BigInteger.Create(x).Negate());
            return num(-x);
        }


        public static object minusP(ulong x)
        {
            if (x == 0)
                return x;
            return BigInt.fromBigInteger(BigInteger.Create(x).Negate());
        }


        public static decimal minusP(decimal x) { return -x; }


        public static object minus(object x, object y)
        {
            Ops yops = ops(y);
            Ops xyops = ops(x).combine(yops);

            if (yops is ULongOps && xyops is ULongOps)
                return Numbers.minus(Util.ConvertToULong(x), Util.ConvertToULong(y));
            else if (yops is ULongOps)
            {
                object negativeY = xyops.negate(y);
                Ops negativeYOps = ops(negativeY);
                return ops(x).combine(negativeYOps).add(x, negativeY);
            }

            else
                return xyops.add(x, yops.negate(y));
        }

        public static double minus(double x, double y) { return x - y; }

        public static long minus(long x, long y)
        {
            long ret = x - y;
            if (((ret ^ x) < 0 && (ret ^ ~y) < 0))
                return ThrowIntOverflow();
            return ret;
        }


        public static ulong minus(ulong x, ulong y)
        {
            if (y > x)
                throw new ArithmeticException("integer overflow");
            return x - y;
        }


        public static decimal minus(decimal x, decimal y) { return x - y; }


        public static double minus(double x, Object y) { return minus(x, Util.ConvertToDouble(y)); }


        public static double minus(Object x, double y) { return minus(Util.ConvertToDouble(x), y); }


        public static double minus(double x, long y) { return x - y; }


        public static double minus(long x, double y) { return x - y; }


        public static double minus(double x, ulong y) { return x - y; }


        public static double minus(ulong x, double y) { return x - y; }


        public static object minus(long x, Object y) { return minus((Object)x, y); }


        public static object minus(Object x, long y) { return minus(x, (Object)y); }


        public static object minus(ulong x, Object y) { return minus((Object)x, y); }


        public static object minus(Object x, ulong y) { return minus(x, (Object)y); }


        public static object minus(long x, ulong y) { return minus((Object)x, (Object)y); }


        public static object minus(ulong x, long y) { return minus((Object)x, (Object)y); }

        

        public static object minusP(object x, object y)
        {

            Ops yops = ops(y);
            Ops xyops = ops(x).combine(yops);

            if (yops is ULongOps && xyops is ULongOps)
                return Numbers.minusP(Util.ConvertToULong(x), Util.ConvertToULong(y));
            else if (yops is ULongOps)
            {
                object negativeY = xyops.negateP(y);
                Ops negativeYOps = ops(negativeY);
                return ops(x).combine(negativeYOps).addP(x, negativeY);
            }
            else
            {
                object negativeY = yops.negateP(y);
                Ops negativeYOps = ops(negativeY);
                return ops(x).combine(negativeYOps).addP(x, negativeY);
            }
        }


        public static double minusP(double x, double y) { return x - y; }


        public static object minusP(long x, long y)
        {
            long ret = x - y;
            if (((ret ^ x) < 0 && (ret ^ ~y) < 0))
                return minusP((object)x, (object)y);
            return num(ret);
        }


        public static object minusP(ulong x, ulong y)
        {
            if (y > x)
                return minusP((object)x, (object)y);
            ulong ret = x - y;
            return num(ret);
        }


        public static object minusP(decimal x, decimal y)
        {
            if (x > 0m && y < 0m && x > Decimal.MaxValue + y)
                return minusP((object)x, (object)y);
            else if (x < 0m && y > 0m && x < Decimal.MinValue + y)
                return minusP((object)x, (object)y);
            return num(x - y);
        }


        public static double minusP(double x, Object y) { return minusP(x, Util.ConvertToDouble(y)); }


        public static double minusP(Object x, double y) { return minusP(Util.ConvertToDouble(x), y); }


        public static double minusP(double x, long y) { return x - y; }


        public static double minusP(long x, double y) { return x - y; }


        public static double minusP(double x, ulong y) { return x - y; }


        public static double minusP(ulong x, double y) { return x - y; }


        public static object minusP(long x, Object y) { return minusP((Object)x, y); }


        public static object minusP(Object x, long y) { return minusP(x, (Object)y); }


        public static object minusP(ulong x, Object y) { return minusP((Object)x, y); }


        public static object minusP(Object x, ulong y) { return minusP(x, (Object)y); }


        public static object minusP(long x, ulong y) { return minusP((Object)x, (Object)y); }


        public static object minusP(ulong x, long y) { return minusP((Object)x, (Object)y); }





        public static object unchecked_minus(object x) { return ops(x).unchecked_negate(x); }



        public static double unchecked_minus(double x) { return minus(x); }



        public static long unchecked_minus(long x) { return -x; }



        public static ulong unchecked_minus(ulong x) { if (x == 0) return x; throw new ArithmeticException("Minus not supported on unsigned integer types"); }



        public static decimal unchecked_minus(decimal x) { return -x; }



        public static object unchecked_minus(object x, object y)
        {
            Ops yops = ops(y);
            Ops xyops = ops(x).combine(yops);

            if (yops is ULongOps && xyops is ULongOps)
                return Numbers.unchecked_minus(Util.ConvertToULong(x), Util.ConvertToULong(y));
            else if (yops is ULongOps)
            {
                object negativeY = xyops.unchecked_negate(y);
                Ops negativeYOps = ops(negativeY);
                return ops(x).combine(negativeYOps).unchecked_add(x, negativeY);
            }

            else
                return ops(x).combine(yops).unchecked_add(x, yops.unchecked_negate(y));
        }



        public static double unchecked_minus(double x, double y) { return minus(x, y); }



        public static long unchecked_minus(long x, long y) { return x - y; }



        public static ulong unchecked_minus(ulong x, ulong y) { return x - y; }



        public static decimal unchecked_minus(decimal x, decimal y) { return x - y; }



        public static double unchecked_minus(double x, object y) { return minus(x, y); }



        public static double unchecked_minus(object x, double y) { return minus(x, y); }



        public static double unchecked_minus(double x, long y) { return minus(x, y); }



        public static double unchecked_minus(long x, double y) { return minus(x, y); }



        public static double unchecked_minus(double x, ulong y) { return minus(x, y); }



        public static double unchecked_minus(ulong x, double y) { return minus(x, y); }



        public static object unchecked_minus(long x, object y) { return unchecked_minus((Object)x, y); }



        public static object unchecked_minus(object x, long y) { return unchecked_minus(x, (Object)y); }



        public static object unchecked_minus(ulong x, object y) { return unchecked_minus((Object)x, y); }



        public static object unchecked_minus(object x, ulong y) { return unchecked_minus(x, (Object)y); }



        public static object unchecked_minus(long x, ulong y) { return unchecked_minus((Object)x, (Object)y); }



        public static object unchecked_minus(ulong x, long y) { return unchecked_minus((Object)x, (Object)y); }

        #endregion

        #region multiply, multiplyP, unchecked_multiply


        public static object multiply(object x, object y) { return ops(x).combine(ops(y)).multiply(x, y); }


        public static double multiply(double x, double y) { return x * y; }


        public static long multiply(long x, long y)
        {
            if (x == Int64.MinValue && y < 0)
                return ThrowIntOverflow();
            long ret = x * y;
            if (y != 0 && ret / y != x)
                return ThrowIntOverflow();
            return ret;
        }


        public static ulong multiply(ulong x, ulong y)
        {
            ulong ret = x * y;
            if (y != 0 && ret / y != x)
                throw new ArithmeticException("integer overflow");
            return ret;
        }


        public static decimal multiply(decimal x, decimal y) { return x * y; }


        public static double multiply(double x, Object y) { return multiply(x, Util.ConvertToDouble(y)); }


        public static double multiply(Object x, double y) { return multiply(Util.ConvertToDouble(x), y); }


        public static double multiply(double x, long y) { return x * y; }


        public static double multiply(long x, double y) { return x * y; }


        public static double multiply(double x, ulong y) { return x * y; }


        public static double multiply(ulong x, double y) { return x * y; }


        public static object multiply(long x, Object y) { return multiply((Object)x, y); }


        public static object multiply(Object x, long y) { return multiply(x, (Object)y); }


        public static object multiply(ulong x, Object y) { return multiply((Object)x, y); }


        public static object multiply(Object x, ulong y) { return multiply(x, (Object)y); }


        public static object multiply(long x, ulong y) { return multiply((Object)x, (Object)y); }


        public static object multiply(ulong x, long y) { return multiply((Object)x, (Object)y); }




        public static object multiplyP(object x, object y) { return ops(x).combine(ops(y)).multiplyP(x, y); }


        public static double multiplyP(double x, double y) { return x * y; }


        public static object multiplyP(long x, long y)
        {
            if (x == Int64.MinValue && y < 0)
                return multiplyP((object)x, (object)y);
            long ret = x * y;
            if (y != 0 && ret / y != x)
                return multiplyP((object)x, (object)y);
            return num(ret);
        }


        public static object multiplyP(ulong x, ulong y)
        {
            ulong ret = x * y;
            if (y != 0 && ret / y != x)
                return BIGINT_OPS.multiply(x, y);
            return num(ret);
        }


        public static object multiplyP(decimal x, decimal y)
        {
            try
            {
                return x * y;
            }
            catch (OverflowException)
            {
                return BIGDECIMAL_OPS.multiply(x, y);
            }
        }


        public static double multiplyP(double x, Object y) { return multiplyP(x, Util.ConvertToDouble(y)); }


        public static double multiplyP(Object x, double y) { return multiplyP(Util.ConvertToDouble(x), y); }


        public static double multiplyP(double x, long y) { return x * y; }


        public static double multiplyP(long x, double y) { return x * y; }


        public static double multiplyP(double x, ulong y) { return x * y; }


        public static double multiplyP(ulong x, double y) { return x * y; }


        public static object multiplyP(long x, Object y) { return multiplyP((Object)x, y); }


        public static object multiplyP(Object x, long y) { return multiplyP(x, (Object)y); }


        public static object multiplyP(ulong x, Object y) { return multiplyP((Object)x, y); }


        public static object multiplyP(Object x, ulong y) { return multiplyP(x, (Object)y); }


        public static object multiplyP(long x, ulong y) { return multiplyP((Object)x, (Object)y); }


        public static object multiplyP(ulong x, long y) { return multiplyP((Object)x, (Object)y); }





        public static object unchecked_multiply(object x, object y) { return ops(x).combine(ops(y)).unchecked_multiply(x, y); }



        public static double unchecked_multiply(double x, double y) { return multiply(x, y); }



        public static long unchecked_multiply(long x, long y) { return x * y; }



        public static ulong unchecked_multiply(ulong x, ulong y) { return x * y; }



        public static decimal unchecked_multiply(decimal x, decimal y) { return x * y; }



        public static double unchecked_multiply(double x, object y) { return multiply(x, y); }



        public static double unchecked_multiply(object x, double y) { return multiply(x, y); }



        public static double unchecked_multiply(double x, long y) { return multiply(x, y); }



        public static double unchecked_multiply(long x, double y) { return multiply(x, y); }



        public static double unchecked_multiply(double x, ulong y) { return multiply(x, y); }



        public static double unchecked_multiply(ulong x, double y) { return multiply(x, y); }



        public static object unchecked_multiply(long x, object y) { return unchecked_multiply((Object)x, y); }



        public static object unchecked_multiply(object x, long y) { return unchecked_multiply(x, (Object)y); }



        public static object unchecked_multiply(ulong x, object y) { return unchecked_multiply((Object)x, y); }



        public static object unchecked_multiply(object x, ulong y) { return unchecked_multiply(x, (Object)y); }



        public static object unchecked_multiply(long x, ulong y) { return unchecked_multiply((Object)x, (Object)y); }



        public static object unchecked_multiply(ulong x, long y) { return unchecked_multiply((Object)x, (Object)y); }

        #endregion

        #region divide,quotient, remainder


        public static object divide(object x, object y)
        {
            if (IsNaN(x))
                return x;
            else if (IsNaN(y))
                return y;

            Ops yops = ops(y);
            if (yops.isZero(y))
                throw new ArithmeticException("Divide by zero");
            return ops(x).combine(yops).divide(x, y);
        }


        public static double divide(double x, double y) { return x / y; }


        public static object divide(long x, long y) { return divide((object)x, (object)y); }


        public static double divide(double x, Object y) { return x / Util.ConvertToDouble(y); }


        public static double divide(Object x, double y) { return Util.ConvertToDouble(x) / y; }


        public static double divide(double x, long y) { return x / y; }


        public static double divide(long x, double y) { return x / y; }


        public static object divide(long x, Object y) { return divide((Object)x, y); }


        public static object divide(Object x, long y) { return divide(x, (Object)y); }


        // missing divide: uu du ud lu ul uo ou



        public static object quotient(object x, object y)
        {
            Ops yops = ops(y);
            if (yops.isZero(y))
                throw new ArithmeticException("Divide by zero");
            return ops(x).combine(yops).quotient(x, y);
        }


        public static double quotient(double n, double d)
        {
            if (d == 0)
                throw new ArithmeticException("Divide by zero");

            double q = n / d;
            if (q <= Int64.MaxValue && q >= Int64.MinValue)
                return (double)((long)q);
            else
                // bigint quotient
                return BigDecimal.Create(q).ToBigInteger().ToDouble(null);
        }


        public static long quotient(long x, long y) { return x / y; }


        public static ulong quotient(ulong x, ulong y) { return x / y; }


        public static object quotient(double x, Object y) { return quotient((Object)x, y); }


        public static object quotient(Object x, double y) { return quotient(x, (Object)y); }


        public static double quotient(double x, long y) { return quotient(x, (double)y); }


        public static double quotient(long x, double y) { return quotient((double)x, y); }
        

        public static object quotient(long x, Object y) { return quotient((Object)x, y); }


        public static object quotient(Object x, long y) { return quotient(x, (Object)y); }


        // missing divide:  du ud lu ul uo ou



        public static object remainder(object x, object y)
        {
            Ops yops = ops(y);
            if (yops.isZero(y))
                throw new ArithmeticException("Divide by zero");
            return ops(x).combine(yops).remainder(x, y);
        }


        public static double remainder(double n, double d)
        {
            if (d == 0)
                throw new ArithmeticException("Divide by zero");

            double q = n / d;
            if (q <= Int64.MaxValue && q >= Int64.MinValue)
                return n - ((long)q) * d;
            else
            {
                // bigint quotient
                var bq = BigDecimal.Create(q).ToBigInteger();
                return n - bq.ToDouble(null) * d;
            }
        }


        public static long remainder(long x, long y) { return x % y; }


        public static ulong remainder(ulong x, ulong y) { return x % y; }


        public static object remainder(double x, Object y) { return remainder((Object)x, y); }


        public static object remainder(Object x, double y) { return remainder(x, (Object)y); }
        

        public static double remainder(double x, long y) { return remainder(x, (double)y); }


        public static double remainder(long x, double y) { return remainder((double)x, y); }


        public static object remainder(long x, Object y) { return remainder((Object)x, y); }


        public static object remainder(Object x, long y) { return remainder(x, (Object)y); }

        // missing remainder:  du ud lu ul uo ou

        #endregion

        #region inc, incP, unchecked_inc, dec, decP, unchecked_dec


        public static object inc(object x) { return ops(x).inc(x); }


        public static double inc(double x) { return x + 1; }



        public static long inc(long x)
        {
            if (x == Int64.MaxValue)
                return ThrowIntOverflow();
            return x + 1;
        }


        public static ulong inc(ulong x)
        {
            if (x == UInt64.MaxValue)
                throw new ArithmeticException("integer overflow");
            return x + 1;
        }


        public static decimal inc(decimal x) { return x + 1m; }


        public static object incP(object x) { return ops(x).incP(x); }


        public static double incP(double x) { return x + 1; }


        public static object incP(long x)
        {
            if (x == Int64.MaxValue)
                return BIGINT_OPS.inc((object)x);
            return num(x + 1);
        }


        public static object incP(ulong x)
        {
            if (x == UInt64.MaxValue)
                return BIGINT_OPS.inc((object)x);
            return num(x + 1);
        }


        public static object incP(decimal x)
        {
            return addP(x, 1m);
        }


        public static object dec(object x) { return ops(x).dec(x); }


        public static double dec(double x) { return x - 1; }


        public static long dec(long x)
        {
            if (x == Int64.MinValue)
                return ThrowIntOverflow();
            return x - 1;
        }


        public static ulong dec(ulong x)
        {
            if (x == 0)
                throw new ArithmeticException("integer overflow");
            return x - 1;
        }


        public static decimal dec(decimal x) { return x - 1; }


        public static object decP(object x) { return ops(x).decP(x); }


        public static double decP(double x) { return x - 1; }


        public static object decP(long x)
        {
            if (x == Int64.MinValue)
                return BIGINT_OPS.dec((object)x);
            return num(x - 1);
        }


        public static object decP(ulong x)
        {
            if (x == 0)
                return BIGINT_OPS.dec((object)x);
            return num(x - 1);
        }


        public static object decP(decimal x)
        {
            return addP(x, -1m);
        }



        public static object unchecked_inc(object x) { return ops(x).unchecked_inc(x); }



        public static double unchecked_inc(double x) { return inc(x); }       
        


        public static long unchecked_inc(long x) { return x + 1; }



        public static ulong unchecked_inc(ulong x) { return x + 1; }



        public static decimal unchecked_inc(decimal x) { return inc(x); }  
        


        public static object unchecked_dec(object x) { return ops(x).unchecked_dec(x); }



        public static double unchecked_dec(double x) { return dec(x); }



        public static long unchecked_dec(long x) { return x - 1; }



        public static ulong unchecked_dec(ulong x) { return x - 1; }
        


        public static decimal unchecked_dec(decimal x) { return dec(x); }  

        #endregion

        #region equal, compare


        public static bool equal(object x, object y)
        {
            return category(x) == category(y)
                && ops(x).combine(ops(y)).equiv(x, y);
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

        #region equiv, lt, lte, gt, gte


        public static bool equiv(object x, object y) { return ops(x).combine(ops(y)).equiv(x, y); }


        public static bool equiv(double x, double y) { return x == y; }


        public static bool equiv(long x, long y) { return x == y; }


        public static bool equiv(ulong x, ulong y) { return x == y; }


        public static bool equiv(decimal x, decimal y) { return x == y; }


        public static bool equiv(double x, Object y) { return x == Util.ConvertToDouble(y); }


        public static bool equiv(Object x, double y) { return Util.ConvertToDouble(x) == y; }


        public static bool equiv(double x, long y) { return x == y; }


        public static bool equiv(long x, double y) { return x == y; }


        public static bool equiv(double x, ulong y) { return x == y; }


        public static bool equiv(ulong x, double y) { return x == y; }


        public static bool equiv(long x, Object y) { return equiv((Object)x, y); }


        public static bool equiv(Object x, long y) { return equiv(x, (Object)y); }


        public static bool equiv(ulong x, Object y) { return equiv((Object)x, y); }


        public static bool equiv(Object x, ulong y) { return equiv(x, (Object)y); }


        public static bool equiv(long x, ulong y) { return equiv((Object)x, (Object)y); }


        public static bool equiv(ulong x, long y) { return equiv((Object)x, (Object)y); }




        public static bool lt(object x, object y) { return ops(x).combine(ops(y)).lt(x, y); }


        public static bool lt(double x, double y) { return x < y; }
      

        public static bool lt(long x, long y) { return x < y; }


        public static bool lt(ulong x, ulong y) { return x < y; }


        public static bool lt(decimal x, decimal y) { return x < y; }


        public static bool lt(double x, Object y) { return x < Util.ConvertToDouble(y); }


        public static bool lt(Object x, double y) { return Util.ConvertToDouble(x) < y; }


        public static bool lt(double x, long y) { return x < y; }


        public static bool lt(long x, double y) { return x < y; }


        public static bool lt(double x, ulong y) { return x < y; }


        public static bool lt(ulong x, double y) { return x < y; }


        public static bool lt(long x, Object y) { return lt((Object)x, y); }


        public static bool lt(Object x, long y) { return lt(x, (Object)y); }


        public static bool lt(ulong x, Object y) { return lt((Object)x, y); }


        public static bool lt(Object x, ulong y) { return lt(x, (Object)y); }


        public static bool lt(long x, ulong y) { return lt((Object)x, (Object)y); }


        public static bool lt(ulong x, long y) { return lt((Object)x, (Object)y); }




        public static bool lte(object x, object y) { return ops(x).combine(ops(y)).lte(x, y); }


        public static bool lte(double x, double y) { return x <= y; }


        public static bool lte(long x, long y) { return x <= y; }


        public static bool lte(ulong x, ulong y) { return x <= y; }


        public static bool lte(decimal x, decimal y) { return x <= y; }


        public static bool lte(double x, Object y) { return x <= Util.ConvertToDouble(y); }


        public static bool lte(Object x, double y) { return Util.ConvertToDouble(x) <= y; }


        public static bool lte(double x, long y) { return x <= y; }


        public static bool lte(long x, double y) { return x <= y; }


        public static bool lte(double x, ulong y) { return x <= y; }


        public static bool lte(ulong x, double y) { return x <= y; }


        public static bool lte(long x, Object y) { return lte((Object)x, y); }


        public static bool lte(Object x, long y) { return lte(x, (Object)y); }


        public static bool lte(ulong x, Object y) { return lte((Object)x, y); }


        public static bool lte(Object x, ulong y) { return lte(x, (Object)y); }


        public static bool lte(long x, ulong y) { return lte((Object)x, (Object)y); }


        public static bool lte(ulong x, long y) { return lte((Object)x, (Object)y); }




        public static bool gt(object x, object y) { return ops(x).combine(ops(y)).lt(y, x); }


        public static bool gt(double x, double y) { return x > y; }


        public static bool gt(long x, long y) { return x > y; }


        public static bool gt(ulong x, ulong y) { return x > y; }


        public static bool gt(decimal x, decimal y) { return x > y; }


        public static bool gt(double x, Object y) { return x > Util.ConvertToDouble(y); }


        public static bool gt(Object x, double y) { return Util.ConvertToDouble(x) > y; }


        public static bool gt(double x, long y) { return x > y; }


        public static bool gt(long x, double y) { return x > y; }


        public static bool gt(double x, ulong y) { return x > y; }


        public static bool gt(ulong x, double y) { return x > y; }


        public static bool gt(long x, Object y) { return gt((Object)x, y); }


        public static bool gt(Object x, long y) { return gt(x, (Object)y); }


        public static bool gt(ulong x, Object y) { return gt((Object)x, y); }


        public static bool gt(Object x, ulong y) { return gt(x, (Object)y); }


        public static bool gt(long x, ulong y) { return gt((Object)x, (Object)y); }


        public static bool gt(ulong x, long y) { return gt((Object)x, (Object)y); }
        


        public static bool gte(object x, object y) { return ops(x).combine(ops(y)).gte(x, y); }


        public static bool gte(double x, double y) { return x >= y; }


        public static bool gte(long x, long y) { return x >= y; }


        public static bool gte(ulong x, ulong y) { return x >= y; }


        public static bool gte(decimal x, decimal y) { return x >= y; }


        public static bool gte(double x, Object y) { return x >= Util.ConvertToDouble(y); }


        public static bool gte(Object x, double y) { return Util.ConvertToDouble(x) >= y; }


        public static bool gte(double x, long y) { return x >= y; }


        public static bool gte(long x, double y) { return x >= y; }


        public static bool gte(double x, ulong y) { return x >= y; }


        public static bool gte(ulong x, double y) { return x >= y; }


        public static bool gte(long x, Object y) { return gte((Object)x, y); }


        public static bool gte(Object x, long y) { return gte(x, (Object)y); }


        public static bool gte(ulong x, Object y) { return gte((Object)x, y); }


        public static bool gte(Object x, ulong y) { return gte(x, (Object)y); }


        public static bool gte(long x, ulong y) { return gte((Object)x, (Object)y); }


        public static bool gte(ulong x, long y) { return gte((Object)x, (Object)y); }

        #endregion

        #region min/max

        static bool IsNaN(object x)
        {
            return (x is double d && double.IsNaN(d))
                || (x is float f && float.IsNaN(f));
        }


        public static Object max(Object x, Object y)
        {
            if (IsNaN(x))
                return x;
            else if (IsNaN(y))
                return y;

            if (gt(x, y))
                return x;
            else
                return y;
        }


        public static double max(double x, double y) { return Math.Max(x, y); }


        public static long max(long x, long y)
        {
            return Math.Max(x, y);
        }


        public static ulong max(ulong x, ulong y)
        {
            return Math.Max(x, y);
        }


        public static decimal max(decimal x, decimal y)
        {
            return Math.Max(x, y);
        }


        public static Object max(double x, Object y)
        {
            if (Double.IsNaN(x))
                return x;
            else if (IsNaN(y))
                return y;

            if (x > Util.ConvertToDouble(y))
                return x;
            else
                return y;
        }


        public static Object max(Object x, double y)
        {
            if (IsNaN(x))
                return x;
            else if (Double.IsNaN(y))
                return y;

            if (Util.ConvertToDouble(x) > y)
                return x;
            else
                return y;
        }


        public static Object max(double x, long y)
        {
            if (Double.IsNaN(x))
                return x;

            if (x > y)
                return x;
            else
                return y;
        }


        public static Object max(long x, double y)
        {
            if (Double.IsNaN(y))
                return y;

            if (x > y)
                return x;
            else
                return y;
        }


        public static Object max(double x, ulong y)
        {
            if (Double.IsNaN(x))
                return x;

            if (x > y)
                return x;
            else
                return y;
        }


        public static Object max(ulong x, double y)
        {
            if (Double.IsNaN(y))
                return y;

            if (x > y)
                return x;
            else
                return y;
        }


        public static Object max(long x, Object y)
        {
            if (IsNaN(y))
                return y;

            if (gt(x, y))
                return x;
            else
                return y;
        }


        public static Object max(Object x, long y)
        {
            if (IsNaN(x))
                return x;

            if (gt(x, y))
                return x;
            else
                return y;
        }


        public static Object max(ulong x, Object y)
        {
            if (IsNaN(y))
                return y;

            if (gt(x, y))
                return x;
            else
                return y;
        }


        public static Object max(Object x, ulong y)
        {
            if (IsNaN(x))
                return x;

            if (gt(x, y))
                return x;
            else
                return y;
        }


        public static Object max(long x, ulong y)
        {
            if (gt(x, y))
                return x;
            else
                return y;
        }


        public static Object max(ulong x, long y)
        {
            if (gt(x, y))
                return x;
            else
                return y;
        }




        public static Object min(Object x, Object y)
        {
            if (IsNaN(x))
                return x;
            else if (IsNaN(y))
                return y;

            if (lt(x, y))
                return x;
            else
                return y;
        }


        public static double min(double x, double y) { return Math.Min(x, y); }


        public static long min(long x, long y)
        {
            return Math.Min(x, y);
        }


        public static ulong min(ulong x, ulong y)
        {
            return Math.Min(x, y);      
        }


        public static decimal min(decimal x, decimal y)
        {
            return Math.Min(x, y);
        }


        public static Object min(double x, Object y)
        {
            if (Double.IsNaN(x))
                return x;
            else if (IsNaN(y))
                return y;

            if (x < Util.ConvertToDouble(y))
                return x;
            else
                return y;
        }


        public static Object min(Object x, double y)
        {
            if (IsNaN(x))
                return x;
            else if (double.IsNaN(y))
                return y;

            if (Util.ConvertToDouble(x) < y)
                return x;
            else
                return y;
        }


        public static Object min(double x, long y)
        {
            if (Double.IsNaN(x))
                return x;

            if (x < y)
                return x;
            else
                return y;
        }


        public static Object min(long x, double y)
        {
            if (Double.IsNaN(y))
                return y;

            if (x < y)
                return x;
            else
                return y;
        }


        public static Object min(double x, ulong y)
        {
            if (Double.IsNaN(x))
                return x;

            if (x < y)
                return x;
            else
                return y;
        }


        public static Object min(ulong x, double y)
        {
            if (Double.IsNaN(y))
                return y;

            if (x < y)
                return x;
            else
                return y;
        }



        public static Object min(long x, Object y)
        {
            if (IsNaN(y))
                return y;

            if (lt(x, y))
                return x;
            else
                return y;
        }


        public static Object min(Object x, long y)
        {
            if (IsNaN(x))
                return x;

            if (lt(x, y))
                return x;
            else
                return y;
        }


        public static Object min(ulong x, Object y)
        {
            if (IsNaN(y))
                return y;

            if (lt(x, y))
                return x;
            else
                return y;
        }


        public static Object min(Object x, ulong y)
        {
            if (IsNaN(x))
                return x;

            if (lt(x, y))
                return x;
            else
                return y;
        }


        public static Object min(long x, ulong y)
        {
            if (lt(x, y))
                return x;
            else
                return y;
        }


        public static Object min(ulong x, long y)
        {
            if (lt(x, y))
                return x;
            else
                return y;
        }


        public static long abs(long x)
        {
            return Math.Abs(x);
        }

        public static double abs(double x)
        {
            return Math.Abs(x);
        }

        public static ulong abs(ulong x)
        {
            return x;
        }

        public static decimal abs(decimal x)
        {
            return Math.Abs(x);
        }

        static public object abs(object x)
        {
            return ops(x).abs(x);
        }


        #endregion

        #region Int overloads for basic ops

        public static int ThrowIntOverflow() { throw new ArithmeticException("integer overflow"); }

        //public static object num(int x)
        //{
        //    return x;
        //}



        public static int unchecked_int_add(int x, int y) { return x + y; }



        public static int unchecked_int_subtract(int x, int y) { return x - y; }



        public static int unchecked_int_negate(int x) { return -x; }


        public static int unchecked_int_inc(int x) { unchecked { return x + 1; } }



        public static int unchecked_int_dec(int x) { unchecked { return x - 1; } }



        public static int unchecked_int_multiply(int x, int y) { return x * y; }



        public static int unchecked_int_divide(int x, int y) { return x / y; }



        public static int unchecked_int_remainder(int x, int y) { return x % y; }

        #endregion

        #region converters to object


        public static object num(object x) { return x; }


        public static object num(float x) { return x; }


        public static object num(double x) { return x; }


        public static object num(long x) { return x; }


        public static object num(ulong x) { return x; }


        public static object num(decimal x) { return x; }

        #endregion

        #endregion

        #region  utility methods

        // TODO: ToBigInt, ToBigInteger, ToBigDecimal will fail on unsigned long, e.g.
        // FIX!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        [WarnBoxedMath(false)]
        static BigInt ToBigInt(Object x)
        {
            if (x is BigInt bigInt)
                return bigInt;

            BigInteger bigInteger = x as BigInteger;
            if (bigInteger != null)
                return BigInt.fromBigInteger(bigInteger);

            return BigInt.fromLong(Util.ConvertToLong(x));
        }

        [WarnBoxedMath(false)]
        static BigInteger ToBigInteger(object x)
        {
            BigInteger bigInteger = x as BigInteger;
            if (bigInteger != null)
                return bigInteger;

            if (x is BigInt bigInt)
                return bigInt.toBigInteger();

            return BigInteger.Create(Util.ConvertToLong(x));
        }


        [WarnBoxedMath(false)]
        static BigDecimal ToBigDecimal(object x)
        {
            BigDecimal bigDec = x as BigDecimal;
            if (bigDec != null)
                return bigDec;

            if (x is BigInt bigInt)
            {
                if (bigInt.Bipart == null)
                    return BigDecimal.Create(bigInt.Lpart);
                else
                    return BigDecimal.Create(bigInt.Bipart);
            }

            BigInteger bigInteger = x as BigInteger;
            if (bigInteger != null)
                return BigDecimal.Create(bigInteger);

            if (x is double d)
                return BigDecimal.Create(d);

            if (x is float f)
                return BigDecimal.Create((double)f);

            if (x is Ratio r)
                return (BigDecimal)divide(BigDecimal.Create(r.numerator), r.denominator);

            if (x is decimal cd)
                return BigDecimal.Create(cd);

            return BigDecimal.Create(Util.ConvertToLong(x));
        }

        [WarnBoxedMath(false)]
        public static Ratio ToRatio(object x)
        {
            if (x is Ratio r)
                return r;

            BigDecimal bx = x as BigDecimal;
            if ( bx != null )
            {
                int exp = bx.Exponent;
                if (exp >= 0)
                    return new Ratio(bx.ToBigInteger(), BigInteger.One);
                else
                    return new Ratio(bx.MovePointRight(-exp).ToBigInteger(), BigInteger.Ten.Power(-exp));
            }

            return new Ratio(ToBigInteger(x), BigInteger.One);
        }


        [WarnBoxedMath(false)]
        public static object rationalize(object x)
        {
            if (x is float f)                              
                return rationalize(BigDecimal.Create(f));

            if (x is double d)                        
                return rationalize(BigDecimal.Create(d));

            BigDecimal bx = (BigDecimal)x;
            if (bx != null)
            {
                int exp = bx.Exponent;
                if (exp >= 0)
                    return BigInt.fromBigInteger(bx.ToBigInteger());
                else
                    return divide(bx.MovePointRight(-exp).ToBigInteger(), BigInteger.Ten.Power(-exp));
            }

            return x;
        }

        #endregion

        #region More BigInteger support

        [WarnBoxedMath(false)]
        public static object ReduceBigInt(BigInt val)
        {
            if (val.Bipart == null)
                return num(val.Lpart);
            return val.Bipart;
        }

        public static object BIDivide(BigInteger n, BigInteger d)
        {
            if (d.Equals(BigInteger.Zero))
                throw new ArithmeticException("Divide by zero");
            BigInteger gcd = n.Gcd(d);
            if (gcd.Equals(BigInteger.Zero))
                return BigInt.ZERO;
            n /= gcd;
            d /= gcd;

            if (d.Equals(BigInteger.One))
                return BigInt.fromBigInteger(n);
            else if (d.Equals(BigInteger.NegativeOne))
                return BigInt.fromBigInteger(n.Negate());

            return new Ratio((d.Signum < 0 ? -n : n), d.Abs());
        }

        #endregion

        #region Basic bit operations


        public static int shiftLeftInt(int x, int n)
        {
            //return n >= 0 ? x << n : x >> -n;
            return x << n;
        }


        public static object shiftLeft(object x, object n)
        {
            return shiftLeft(bitOpsCast(x),bitOpsCast(n));
        }


        public static long shiftLeft(object x, long n)
        {
            return shiftLeft(bitOpsCast(x), n);
        }


        public static long shiftLeft(long x, object n)
        {
            return shiftLeft(x, bitOpsCast(n));
        }


        public static long shiftLeft(long x, long n)
        {
            return shiftLeft(x, (int)n);
        }


        public static long shiftLeft(long x, int n)
        {
            //return n >= 0 ? x << n : x >> -n;
            return x << n;
        }



        public static int shiftRightInt(int x, int n)
        {
            // return n >= 0 ? x >> n : x << -n;
            return x >> n;
        }


        public static long shiftRight(object x, object n)
        {
            return shiftRight(bitOpsCast(x), bitOpsCast(n));
        }


        public static long shiftRight(object x, long n)
        {
            return shiftRight(bitOpsCast(x), n);
        }


        public static long shiftRight(long x, object n)
        {
            return shiftRight(x, bitOpsCast(n));
        }


        public static long shiftRight(long x, long n)
        {
            return shiftRight(x, (int)n);
        }


        public static long shiftRight(long x, int n)
        {
            // return n >= 0 ? x >> n : x << -n;
            return x >> n;
        }


        public static int unsignedShiftRightInt(int x, int n)
        {
            return (int)((uint)x >> n);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        static long unsignedShiftRight(Object x, Object y)
        {
            return unsignedShiftRight(bitOpsCast(x), bitOpsCast(y));
        }


        public static long unsignedShiftRight(Object x, long y)
        {
            return unsignedShiftRight(bitOpsCast(x), y);
        }


        public static long unsignedShiftRight(long x, Object y)
        {
            return unsignedShiftRight(x, bitOpsCast(y));
        }


        public static long unsignedShiftRight(long x, long n)
        {
            return (long)((ulong)x >> (int)n);
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

            public Ops opsWith(BigIntOps x)
            {
                return BIGINT_OPS;
            }

            public Ops opsWith(BigDecimalOps x)
            {
                return BIGDECIMAL_OPS;
            }

            public Ops opsWith(ULongOps x)
            {
                return BIGINT_OPS;
            }

            public Ops opsWith(ClrDecimalOps x)
            {
                return CLRDECIMAL_OPS;
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
                 return num(Numbers.add( Util.ConvertToLong(x), Util.ConvertToLong(y)));
            }

            public object addP(object x, object y)
            {
                long lx = Util.ConvertToLong(x);
                long ly = Util.ConvertToLong(y);
                long ret = lx + ly;
                if ((ret ^ lx) < 0 && (ret ^ ly) < 0)
                    return BIGINT_OPS.add(x, y);
                return num(ret);
            }

            public object unchecked_add(object x, object y)
            {
                return num(Numbers.unchecked_add(Util.ConvertToLong(x), Util.ConvertToLong(y)));
            }

            public object multiply(object x, object y)
            {
                return num(Numbers.multiply(Util.ConvertToLong(x), Util.ConvertToLong(y)));
            }

            public object multiplyP(object x, object y)
            {
                long lx = Util.ConvertToLong(x);
                long ly = Util.ConvertToLong(y);

                if (lx == Int64.MinValue && ly < 0)
                    return BIGINT_OPS.multiply(x, y);

                long ret = lx * ly;
                if (ly != 0 && ret / ly != lx)
                    return BIGINT_OPS.multiply(x, y);
                return num(ret);
            }

            public object unchecked_multiply(object x, object y)
            {
                return num(Numbers.unchecked_multiply(Util.ConvertToLong(x), Util.ConvertToLong(y)));

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
                    return num(0);

                n /= gcd1;
                long d = val / gcd1;
                if (d == 1)
                    return num(n);
                if (d < 0)
                {
                    n = -n;
                    d = -d;
                }
                return new Ratio(BigInteger.Create(n), BigInteger.Create(d));
            }

            public object quotient(object x, object y)
            {
                return num(Util.ConvertToLong(x) / Util.ConvertToLong(y));
            }

            public object remainder(object x, object y)
            {
                return num(Util.ConvertToLong(x) % Util.ConvertToLong(y));
            }

            public bool equiv(object x, object y)
            {
                return Util.ConvertToLong(x) == Util.ConvertToLong(y);
            }

            public bool lt(object x, object y)
            {
                return Util.ConvertToLong(x) < Util.ConvertToLong(y);
            }

            public bool lte(object x, object y)
            {
                return Util.ConvertToLong(x) <= Util.ConvertToLong(y);
            }

            public bool gte(object x, object y)
            {
                return Util.ConvertToLong(x) >= Util.ConvertToLong(y);
            }
            	
            public object negate(object x)
            {
                long val = Util.ConvertToLong(x);
                return num(Numbers.minus(val));
            }

            public object negateP(object x)
            {
                long val = Util.ConvertToLong(x);

                if (val > Int64.MinValue)
                    return num(-val);
                return BigInt.fromBigInteger(-BigInteger.Create(val));
            }

            public object unchecked_negate(object x)
            {
                long val = Util.ConvertToLong(x);
                return num(Numbers.unchecked_minus(val));
            }


            public object inc(object x)
            {
                long val = Util.ConvertToLong(x);
                return num(Numbers.inc(val));
            }

            public object incP(object x)
            {
                long val = Util.ConvertToLong(x);

                if (val < Int64.MaxValue)
                    return num(val + 1);
                return BIGINT_OPS.inc(x);
            }

            public object unchecked_inc(object x)
            {
                long val = Util.ConvertToLong(x);
                return num(Numbers.unchecked_inc(val));
            }

            public object dec(object x)
            {
                long val = Util.ConvertToLong(x);
                return num(Numbers.dec(val));
            }

            public object decP(object x)
            {
                long val = Util.ConvertToLong(x);

                if (val > Int64.MinValue)
                    return num(val - 1);
                return BIGINT_OPS.dec(x);
            }

            public object unchecked_dec(object x)
            {
                long val = Util.ConvertToLong(x);
                return num(Numbers.unchecked_dec(val));
            }

            public object abs(object x)
            {
                long val = Util.ConvertToLong(x);
                return num(Math.Abs(val));
            }
            #endregion
        }

        #endregion

        #region ULongOps

        sealed class ULongOps : Ops
        {
            #region Ops Members

            public Ops combine(Ops y)
            {
                return y.opsWith(this);
            }

            public Ops opsWith(LongOps x)
            {
                return BIGINT_OPS;
            }

            public Ops opsWith(DoubleOps x)
            {
                return DOUBLE_OPS;
            }

            public Ops opsWith(RatioOps x)
            {
                return RATIO_OPS;
            }

            public Ops opsWith(BigIntOps x)
            {
                return BIGINT_OPS;
            }

            public Ops opsWith(BigDecimalOps x)
            {
                return BIGDECIMAL_OPS;
            }

            public Ops opsWith(ULongOps x)
            {
                return this;
            }

            public Ops opsWith(ClrDecimalOps x)
            {
                return CLRDECIMAL_OPS;
            }

            public bool isZero(object x)
            {
                return Util.ConvertToULong(x) == 0;
            }

            public bool isPos(object x)
            {
                return Util.ConvertToULong(x) > 0;
            }

            public bool isNeg(object x)
            {
                return Util.ConvertToULong(x) < 0;
            }

            public object add(object x, object y)
            {
                return num(Numbers.add(Util.ConvertToULong(x), Util.ConvertToULong(y)));
            }

            public object addP(object x, object y)
            {
                ulong ulx = Util.ConvertToULong(x);
                ulong uly = Util.ConvertToULong(y);
                if ( ulx > ulong.MaxValue - uly )
                    return BIGINT_OPS.add(x, y);
                return num(ulx+uly);
            }

            public object unchecked_add(object x, object y)
            {
                return num(Numbers.unchecked_add(Util.ConvertToULong(x), Util.ConvertToULong(y)));
            }

            public object multiply(object x, object y)
            {
                return num(Numbers.multiply(Util.ConvertToULong(x), Util.ConvertToULong(y)));
            }

            public object multiplyP(object x, object y)
            {
                ulong ulx = Util.ConvertToULong(x);
                ulong uly = Util.ConvertToULong(y);

                ulong ret = ulx * uly;
                if (uly != 0 && ret / uly != ulx)
                    return BIGINT_OPS.multiply(x, y);
                return num(ret);
            }

            public object unchecked_multiply(object x, object y)
            {
                return num(Numbers.unchecked_multiply(Util.ConvertToULong(x), Util.ConvertToULong(y)));

            }

            static ulong gcd(ulong u, ulong v)
            {
                while (v != 0)
                {
                    ulong r = u % v;
                    u = v;
                    v = r;
                }
                return u;
            }

            public object divide(object x, object y)
            {
                ulong n = Util.ConvertToULong(x);
                ulong val = Util.ConvertToULong(y);
                ulong gcd1 = gcd(n, val);
                if (gcd1 == 0)
                    return num(0);

                n /= gcd1;
                ulong d = val / gcd1;
                if (d == 1)
                    return num(n);
 
                return new Ratio(BigInteger.Create(n), BigInteger.Create(d));
            }

            public object quotient(object x, object y)
            {
                return num(Util.ConvertToULong(x) / Util.ConvertToULong(y));
            }

            public object remainder(object x, object y)
            {
                return num(Util.ConvertToULong(x) % Util.ConvertToULong(y));
            }

            public bool equiv(object x, object y)
            {
                return Util.ConvertToULong(x) == Util.ConvertToULong(y);
            }

            public bool lt(object x, object y)
            {
                return Util.ConvertToULong(x) < Util.ConvertToULong(y);
            }

            public bool lte(object x, object y)
            {
                return Util.ConvertToULong(x) <= Util.ConvertToULong(y);
            }

            public bool gte(object x, object y)
            {
                return Util.ConvertToULong(x) >= Util.ConvertToULong(y);
            }

            public object negate(object x)
            {
                ulong val = Util.ConvertToULong(x);
                if (val == 0)
                    return x;
                throw new ArithmeticException("Checked operation error: negation of non-zero unsigned");
            }

            public object negateP(object x)
            {
                ulong val = Util.ConvertToULong(x);
                if (val == 0)
                    return x;
                return BigInt.fromBigInteger(-BigInteger.Create(val));
            }

            public object unchecked_negate(object x)
            {
                ulong val = Util.ConvertToULong(x);
                return num(Numbers.unchecked_minus(val));
            }


            public object inc(object x)
            {
                ulong val = Util.ConvertToULong(x);
                return num(Numbers.inc(val));
            }

            public object incP(object x)
            {
                ulong val = Util.ConvertToULong(x);

                if (val < UInt64.MaxValue)
                    return num(val + 1);
                return BIGINT_OPS.inc(x);
            }

            public object unchecked_inc(object x)
            {
                ulong val = Util.ConvertToULong(x);
                return num(Numbers.unchecked_inc(val));
            }

            public object dec(object x)
            {
                ulong val = Util.ConvertToULong(x);
                return num(Numbers.dec(val));
            }

            public object decP(object x)
            {
                ulong val = Util.ConvertToULong(x);

                if (val > 0)
                    return num(val - 1);
                return BIGINT_OPS.dec(x);
            }

            public object unchecked_dec(object x)
            {
                ulong val = Util.ConvertToULong(x);
                return num(Numbers.unchecked_dec(val));
            }

            public object abs(object x)
            {
                ulong val = Util.ConvertToULong(x);
                return val;   // well, we are unsigned, after all
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

            public override Ops opsWith(BigIntOps x)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps x)
            {
                return this;
            }

            public override Ops opsWith(ULongOps x)
            {
                return this;
            }

            public override Ops opsWith(ClrDecimalOps x)
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
                return Numbers.quotient(Util.ConvertToDouble(x), Util.ConvertToDouble(y));
            }

            public override object remainder(object x, object y)
            {
                return Numbers.remainder(Util.ConvertToDouble(x), Util.ConvertToDouble(y));
            }

            public override bool equiv(object x, object y)
            {
                return Util.ConvertToDouble(x) == Util.ConvertToDouble(y);
            }

            public override bool lt(object x, object y)
            {
                return Util.ConvertToDouble(x) < Util.ConvertToDouble(y);
            }

            public override bool lte(object x, object y)
            {
                return Util.ConvertToDouble(x) <= Util.ConvertToDouble(y);
            }

            public override bool gte(object x, object y)
            {
                return Util.ConvertToDouble(x) >= Util.ConvertToDouble(y);
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

            public override object abs(object x)
            {
                return Math.Abs(Util.ConvertToDouble(x));
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

            public override Ops opsWith(BigIntOps x)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps x)
            {
                return BIGDECIMAL_OPS;
            }

            public override Ops opsWith(ULongOps x)
            {
                return this;
            }

            public override Ops opsWith(ClrDecimalOps x)
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
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                // add NormalizeRet
                return Numbers.divide(
                    ry.numerator * rx.denominator + rx.numerator * ry.denominator,
                    ry.denominator * rx.denominator);
            }

            public override object multiply(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                // add NormalizeRet
                return Numbers.divide(
                    ry.numerator * rx.numerator,
                    ry.denominator * rx.denominator);
            }

            public override object divide(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                // add NormalizeRet
                return Numbers.divide(
                    ry.denominator * rx.numerator,
                    ry.numerator * rx.denominator);
            }

            public override object quotient(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                // add NormalizeRet
                BigInteger q = (rx.numerator * ry.denominator) / (rx.denominator * ry.numerator);
                return q;
            }

            public override object remainder(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                // add NormalizeRet
                BigInteger q = (rx.numerator * ry.denominator) / (rx.denominator * ry.numerator);
                return Numbers.minus(rx, Numbers.multiply(q, ry));
            }

            public override bool equiv(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);
                
                return rx.numerator.Equals(ry.numerator)
                    && rx.denominator.Equals(ry.denominator);
            }

            public override bool lt(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                return rx.numerator * ry.denominator < ry.numerator * rx.denominator;
            }

            public override bool lte(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                return rx.numerator * ry.denominator <= ry.numerator * rx.denominator;
            }

            public override bool gte(object x, object y)
            {
                Ratio rx = ToRatio(x);
                Ratio ry = ToRatio(y);

                return rx.numerator * ry.denominator >= ry.numerator * rx.denominator;
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

            public override object abs(object x)
            {
                Ratio rx = (Ratio)(x);
                return new Ratio(rx.numerator.Abs(), rx.denominator); ;
            }

            #endregion
        }

        #endregion

        #region ClrDecimalOps

        class ClrDecimalOps : Ops
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
                return BIGDECIMAL_OPS;
            }

            public Ops opsWith(BigIntOps x)
            {
                return BIGDECIMAL_OPS;
            }

            public Ops opsWith(BigDecimalOps x)
            {
                return BIGDECIMAL_OPS;
            }

            public Ops opsWith(ULongOps x)
            {
                return this;
            }

            public Ops opsWith(ClrDecimalOps x)
            {
                return this;
            }

            public bool isZero(object x)
            {
                return Util.ConvertToDecimal(x) == 0;
            }

            public bool isPos(object x)
            {
                return Util.ConvertToDecimal(x) > 0;
            }

            public bool isNeg(object x)
            {
                return Util.ConvertToDecimal(x) < 0;
            }

            public object add(object x, object y)
            {
                decimal dx = Util.ConvertToDecimal(x);
                decimal dy = Util.ConvertToDecimal(y);
                return dx + dy;
            }

            public object addP(object x, object y)
            {
                decimal dx = Util.ConvertToDecimal(x);
                decimal dy = Util.ConvertToDecimal(y);

                try
                {
                    decimal ret = dx + dy;
                    return ret;
                }
                catch (OverflowException)
                {
                    return BIGDECIMAL_OPS.add(x, y);
                }
            }

            public object unchecked_add(object x, object y)
            {
                return addP(x, y);
            }

            public object multiply(object x, object y)
            {
                decimal dx = Util.ConvertToDecimal(x);
                decimal dy = Util.ConvertToDecimal(y);
                return dx * dy;
            }

            public object multiplyP(object x, object y)
            {
                decimal dx = Util.ConvertToDecimal(x);
                decimal dy = Util.ConvertToDecimal(y);

                try
                {
                    decimal ret = dx * dy;
                    return ret;
                }
                catch (OverflowException)
                {
                    return BIGDECIMAL_OPS.multiply(x, y);
                }
            }

            public object unchecked_multiply(object x, object y)
            {
                return multiplyP(x, y);

            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
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
                decimal dx = Util.ConvertToDecimal(x);
                decimal dy = Util.ConvertToDecimal(y);

                try
                {
                    decimal ret = dx / dy;
                    return ret;
                }
                catch (OverflowException)
                {
                    return BigDecimal.Create(dx).Divide(BigDecimal.Create(dy));
                }
          
            }

            public object quotient(object x, object y)
            {
                decimal dx = Util.ConvertToDecimal(x);
                decimal dy = Util.ConvertToDecimal(y);
                return dx / dy;
            }

            public object remainder(object x, object y)
            {
                decimal dx = Util.ConvertToDecimal(x);
                decimal dy = Util.ConvertToDecimal(y);
                return dx % dy;
            }

            public bool equiv(object x, object y)
            {
                return Util.ConvertToDecimal(x) == Util.ConvertToDecimal(y);
            }

            public bool lt(object x, object y)
            {
                return Util.ConvertToDecimal(x) < Util.ConvertToDecimal(y);
            }

            public bool lte(object x, object y)
            {
                return Util.ConvertToDecimal(x) <= Util.ConvertToDecimal(y);
            }

            public bool gte(object x, object y)
            {
                return Util.ConvertToDecimal(x) >= Util.ConvertToDecimal(y);
            }

            public object negate(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                return -val;
            }

            public object negateP(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                return -val; 
            }

            public object unchecked_negate(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                return -val; 
            }


            public object inc(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                return val + 1; 
            }

            public object incP(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                try
                {
                    return val + 1;
                }
                catch (OverflowException)
                {
                    return BIGDECIMAL_OPS.inc(x);
                }
            }

            public object unchecked_inc(object x)
            {
                return incP(x);
            }

            public object dec(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                return val - 1; 
            }

            public object decP(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                try
                {
                    return val - 1;
                }
                catch (OverflowException)
                {
                    return BIGDECIMAL_OPS.inc(x);
                }
            }

            public object unchecked_dec(object x)
            {
                return decP(x);
            }

            public object abs(object x)
            {
                decimal val = Util.ConvertToDecimal(x);
                return Math.Abs(val);
            }

            #endregion
        }

        #endregion

        #region BigIntOps

        class BigIntOps : OpsP
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

            public override Ops opsWith(BigIntOps ops)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps ops)
            {
                return BIGDECIMAL_OPS;
            }

            public override Ops opsWith(ULongOps x)
            {
                return this;
            }

            public override Ops opsWith(ClrDecimalOps x)
            {
                return BIGDECIMAL_OPS;
            }


            public override bool isZero(object x)
            {
                BigInt bx = ToBigInt(x);
                if (bx.Bipart == null)
                    return bx.Lpart == 0;
                return bx.Bipart.IsZero;
            }

            public override bool isPos(object x)
            {
                BigInt bx = ToBigInt(x);
                if (bx.Bipart == null)
                    return bx.Lpart > 0;
                return bx.Bipart.IsPositive;
            }

            public override bool isNeg(object x)
            {
                BigInt bx = ToBigInt(x);
                if (bx.Bipart == null)
                    return bx.Lpart < 0;
                return bx.Bipart.IsNegative;
            }

            public override object add(object x, object y)
            {
                return ToBigInt(x).add(ToBigInt(y));
            }

            public override object multiply(object x, object y)
            {
                return ToBigInt(x).multiply(ToBigInt(y));
            }

            public override object divide(object x, object y)
            {
                return BIDivide(ToBigInteger(x),ToBigInteger(y));
            }

            public override object quotient(object x, object y)
            {
                return ToBigInt(x).quotient(ToBigInt(y));
            }

            public override object remainder(object x, object y)
            {
                return ToBigInt(x).remainder(ToBigInt(y));
            }

            public override bool equiv(object x, object y)
            {
                return ToBigInt(x).Equals(ToBigInt(y));
            }

            public override bool lt(object x, object y)
            {
                return ToBigInt(x).lt(ToBigInt(y));
            }

            public override bool lte(object x, object y)
            {
                return ToBigInteger(x) <= ToBigInteger(y);
            }

            public override bool gte(object x, object y)
            {
                return ToBigInteger(x) >= ToBigInteger(y);
            }

            public override object negate(object x)
            {
                return BigInt.fromBigInteger(-ToBigInteger(x));
            }

            public override object inc(object x)
            {
                return BigInt.fromBigInteger(ToBigInteger(x) + BigInteger.One);
            }

            public override object dec(object x)
            {
                return BigInt.fromBigInteger(ToBigInteger(x) - BigInteger.One);
            }

            public override object abs(object x)
            {
                return BigInt.fromBigInteger(ToBigInteger(x).Abs());
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

            public override Ops opsWith(BigIntOps x)
            {
                return this;
            }

            public override Ops opsWith(BigDecimalOps x)
            {
                return this;
            }

            public override Ops opsWith(ULongOps x)
            {
                return this;
            }

            public override Ops opsWith(ClrDecimalOps x)
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
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                return c == null
                    ? ToBigDecimal(x).Add(ToBigDecimal(y))
                    : ToBigDecimal(x).Add(ToBigDecimal(y), c.Value);
            }

            public override object multiply(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                return c == null
                   ? ToBigDecimal(x).Multiply(ToBigDecimal(y))
                   : ToBigDecimal(x).Multiply(ToBigDecimal(y), c.Value);
            }

            public override object divide(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                return c == null
                    ? ToBigDecimal(x).Divide(ToBigDecimal(y))
                    : ToBigDecimal(x).Divide(ToBigDecimal(y), c.Value);
            }

            public override object quotient(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                return c == null
                    ? ToBigDecimal(x).DivideInteger(ToBigDecimal(y))
                    : ToBigDecimal(x).DivideInteger(ToBigDecimal(y), c.Value);
            }

            public override object remainder(object x, object y)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                return c == null
                    ? ToBigDecimal(x).Mod(ToBigDecimal(y))
                    : ToBigDecimal(x).Mod(ToBigDecimal(y), c.Value);
            }

            public override bool equiv(object x, object y)
            {
                return ToBigDecimal(x).CompareTo(ToBigDecimal(y)) == 0;
            }

            public override bool lt(object x, object y)
            {
                return ToBigDecimal(x).CompareTo(ToBigDecimal(y)) < 0;
            }

            public override bool lte(object x, object y)
            {
                return ToBigDecimal(x).CompareTo(ToBigDecimal(y)) <= 0;
            }

            public override bool gte(object x, object y)
            {
                return ToBigDecimal(x).CompareTo(ToBigDecimal(y)) >= 0;
            }

            public override object negate(object x)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                return c == null
                    ? ((BigDecimal)x).Negate()
                    : ((BigDecimal)x).Negate(c.Value);
            }

            public override object inc(object x)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                BigDecimal bx = (BigDecimal)x;
                return c == null
                    ? bx.Add(BigDecimal.One)
                    : bx.Add(BigDecimal.One, c.Value);
            }

            public override object dec(object x)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                BigDecimal bx = (BigDecimal)x;
                return c == null
                    ? bx.Subtract(BigDecimal.One)
                    : bx.Subtract(BigDecimal.One, c.Value);
            }

            public override object abs(object x)
            {
                BigDecimal.Context? c = (BigDecimal.Context?)RT.MathContextVar.deref();
                BigDecimal bx = (BigDecimal)x;
                return c == null
                        ? bx.Abs()
                        : bx.Abs(c.Value);
            }

            #endregion
        }

        #endregion

        #region Ops/BitOps dispatching

        static readonly LongOps LONG_OPS = new LongOps();
        static readonly ULongOps ULONG_OPS = new ULongOps();
        static readonly DoubleOps DOUBLE_OPS = new DoubleOps();
        static readonly RatioOps RATIO_OPS = new RatioOps();
        static readonly ClrDecimalOps CLRDECIMAL_OPS = new ClrDecimalOps();
        static readonly BigIntOps BIGINT_OPS = new BigIntOps();
        static readonly BigDecimalOps BIGDECIMAL_OPS = new BigDecimalOps();

        public enum Category { Integer, Floating, Decimal, Ratio }  // TODO

        static Ops ops(Object x)
        {
            Type xc = x.GetType();

            switch (Type.GetTypeCode(xc))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return LONG_OPS;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return ULONG_OPS;

                case TypeCode.Single:
                case TypeCode.Double:
                    return DOUBLE_OPS;

                case TypeCode.Decimal:
                    return CLRDECIMAL_OPS;

                default:
                    if (xc == typeof(BigInt))
                        return BIGINT_OPS;
                    else if (xc == typeof(BigInteger))
                        return BIGINT_OPS;
                    else if (xc == typeof(Ratio))
                        return RATIO_OPS;
                    else if (xc == typeof(BigDecimal))
                        return BIGDECIMAL_OPS;
                    else
                        return LONG_OPS;
            }
        }


        [WarnBoxedMath(false)]
        public static int hasheqFrom(object x, Type xc)
        {
            if (   xc == typeof(int)
                || xc == typeof(short)
                || xc == typeof(byte)
                || xc == typeof(ulong)
                || xc == typeof(uint)
                || xc == typeof(ushort)
                || xc == typeof(sbyte)
                || (xc == typeof(BigInteger) && lte(x,Int64.MaxValue) && gte(x,Int64.MinValue)))
            {
                long lpart = Util.ConvertToLong(x);
                return Murmur3.HashLong(lpart);
                //return (int)(lpart ^ (lpart >> 32));
            }

            //{
            //    // Make BigInteger conform with Int64 when in Int64 range
            //    long lval;
            //    BigInteger bi = x as BigInteger;
            //    if (bi != null && bi.AsInt64(out lval))
            //        return Murmur3.HashLong(lval);
            //}

            if (xc == typeof(BigDecimal))
            {
                // stripTrailingZeros() to make all numerically equal
                // BigDecimal values come out the same before calling
                // hashCode.  Special check for 0 because
                // stripTrailingZeros() does not do anything to values
                // equal to 0 with different scales.
                if (isZero(x))
                    return BigDecimal.Zero.GetHashCode();
                else
                {
                    BigDecimal tmp = ((BigDecimal)x).StripTrailingZeros();
                    return tmp.GetHashCode();
                }
            }

            if ( xc == typeof(float) && x.Equals(-0.0f))
            {
                return 0; // match 0.0f
            }

            return x.GetHashCode();
        }


        [WarnBoxedMath(false)]
        public static int hasheq(object x)
        {
            Type xc = x.GetType();

            if (xc == typeof(long))
            {
                //return (int)(lpart ^ (lpart >> 32));
                return Murmur3.HashLong((long) x);
            }
            if (xc == typeof(double))
            {
                if (x.Equals(-0.0))
                    return 0;  // match 0.0
                return x.GetHashCode();
            }

            return hasheqFrom(x, xc);
        }


        static Category category(object x)
        {
            Type xc = x.GetType();
            if (xc == typeof(Int32) || xc == typeof(Int64))
                return Category.Integer;
            else if (xc == typeof(float) || xc == typeof(double))
                return Category.Floating;
            else if (xc == typeof(BigInt))
                return Category.Integer;
            else if (xc == typeof(Ratio))
                return Category.Ratio;
            else if (xc == typeof(BigDecimal) || xc == typeof(decimal))
                return Category.Decimal;
            else
                return Category.Integer;
        }

        static long bitOpsCast(object x)
        {
            Type xt = x.GetType();
            if (xt == typeof(long) || xt == typeof(int) || xt == typeof(short) || xt == typeof(byte) || xt == typeof(ulong) || xt == typeof(uint) || xt == typeof(ushort) || xt == typeof(sbyte))
                return RT.longCast(x);

            // no bignums, no decimals
            throw new ArgumentException("bit operations not supported for: " + xt);
        }

        #endregion
       
        #region Array c-tors



        [WarnBoxedMath(false)]
        public static float[] float_array(int size, object init)
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



        [WarnBoxedMath(false)]
        public static float[] float_array(Object sizeOrSeq)
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



        [WarnBoxedMath(false)]
        public static double[] double_array(int size, Object init)
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



        [WarnBoxedMath(false)]
        public static double[] double_array(Object sizeOrSeq)
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



        [WarnBoxedMath(false)]
        public static int[] int_array(int size, Object init)
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



        [WarnBoxedMath(false)]
        public static int[] int_array(Object sizeOrSeq)
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




        [WarnBoxedMath(false)]
        public static uint[] uint_array(int size, Object init)
        {
            uint[] ret = new uint[size];
            if (Util.IsNumeric(init))
            {
                uint f = Util.ConvertToUInt(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToUInt(s.first());
            }
            return ret;
        }



        [WarnBoxedMath(false)]
        public static uint[] uint_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new uint[Util.ConvertToUInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                uint[] ret = new uint[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToUInt(s.first());
                return ret;
            }
        }



        [WarnBoxedMath(false)]
        public static long[] long_array(int size, Object init)
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



        [WarnBoxedMath(false)]
        public static long[] long_array(Object sizeOrSeq)
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



        [WarnBoxedMath(false)]
        public static ulong[] ulong_array(int size, Object init)
        {
            ulong[] ret = new ulong[size];
            if (Util.IsNumeric(init))
            {
                ulong f = Util.ConvertToULong(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToULong(s.first());
            }
            return ret;
        }



        [WarnBoxedMath(false)]
        public static ulong[] ulong_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new ulong[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                ulong[] ret = new ulong[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToULong(s.first());
                return ret;
            }
        }



        [WarnBoxedMath(false)]
        public static short[] short_array(int size, Object init)
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



        [WarnBoxedMath(false)]
        public static short[] short_array(Object sizeOrSeq)
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




        [WarnBoxedMath(false)]
        public static ushort[] ushort_array(int size, Object init)
        {
            ushort[] ret = new ushort[size];
            if (Util.IsNumeric(init))
            {
                ushort f = Util.ConvertToUShort(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToUShort(s.first());
            }
            return ret;
        }



        [WarnBoxedMath(false)]
        public static ushort[] ushort_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new ushort[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                ushort[] ret = new ushort[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToUShort(s.first());
                return ret;
            }
        }




        [WarnBoxedMath(false)]
        public static char[] char_array(int size, Object init)
        {
            char[] ret = new char[size];
            if (Util.IsNumeric(init) || init is Char)
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



        [WarnBoxedMath(false)]
        public static char[] char_array(Object sizeOrSeq)
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




        [WarnBoxedMath(false)]
        public static byte[] byte_array(int size, Object init)
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



        [WarnBoxedMath(false)]
        public static byte[] byte_array(Object sizeOrSeq)
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




        [WarnBoxedMath(false)]
        public static sbyte[] sbyte_array(int size, Object init)
        {
            sbyte[] ret = new sbyte[size];
            if (Util.IsNumeric(init))
            {
                sbyte f = Util.ConvertToSByte(init);
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = f;
            }
            else
            {
                ISeq s = RT.seq(init);
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToSByte(s.first());
            }
            return ret;
        }



        [WarnBoxedMath(false)]
        public static sbyte[] sbyte_array(Object sizeOrSeq)
        {
            if (Util.IsNumeric(sizeOrSeq))
                return new sbyte[Util.ConvertToInt(sizeOrSeq)];
            else
            {
                ISeq s = RT.seq(sizeOrSeq);
                int size = RT.count(s);
                sbyte[] ret = new sbyte[size];
                for (int i = 0; i < size && s != null; i++, s = s.next())
                    ret[i] = Util.ConvertToSByte(s.first());
                return ret;
            }
        }




        [WarnBoxedMath(false)]
        public static bool[] boolean_array(int size, Object init)
        {
            bool[] ret = new bool[size];
            if (init is bool f)
            {
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



        [WarnBoxedMath(false)]
        public static bool[] boolean_array(Object sizeOrSeq)
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



        [WarnBoxedMath(false)]
        public static bool[] booleans(Object array)
        {
            return (bool[])array;
        }


        [WarnBoxedMath(false)]
        public static byte[] bytes(Object array)
        {
            return (byte[])array;
        }


        [WarnBoxedMath(false)]
        public static sbyte[] sbytes(Object array)
        {
            return (sbyte[])array;
        }


        [WarnBoxedMath(false)]
        public static char[] chars(Object array)
        {
            return (char[])array;
        }


        [WarnBoxedMath(false)]
        public static short[] shorts(Object array)
        {
            return (short[])array;
        }


        [WarnBoxedMath(false)]
        public static ushort[] ushorts(Object array)
        {
            return (ushort[])array;
        }


        [WarnBoxedMath(false)]
        public static float[] floats(Object array)
        {
            return (float[])array;
        }


        [WarnBoxedMath(false)]
        public static double[] doubles(Object array)
        {
            return (double[])array;
        }


        [WarnBoxedMath(false)]
        public static int[] ints(Object array)
        {
            return (int[])array;
        }


        [WarnBoxedMath(false)]
        public static uint[] uints(Object array)
        {
            return (uint[])array;
        }


        [WarnBoxedMath(false)]
        public static long[] longs(Object array)
        {
            return (long[])array;
        }


        [WarnBoxedMath(false)]
        public static ulong[] ulongs(Object array)
        {
            return (ulong[])array;
        }

        #endregion

        #region Bit ops

        #region not


        static public long not(object x)
        {
            return not(bitOpsCast(x));
        }


        static public long not(long x)
        {
            return ~x;
        }

        #endregion

        #region and


        public static long and(object x, object y)
        {
            return and(bitOpsCast(x), bitOpsCast(y));
        }


        public static long and(object x, long y)
        {
            return and(bitOpsCast(x), y);
        }


        public static long and(long x, object y)
        {
            return and(x, bitOpsCast(y));
        }


        public static long and(long x, long y)
        {
            return x & y;
        }

        #endregion

        #region or


        public static long or(object x, object y)
        {
            return or(bitOpsCast(x),bitOpsCast(y));
        }


        public static long or(object x, long y)
        {
            return or(bitOpsCast(x), y);
        }


        public static long or(long x, object y)
        {
            return or(x, bitOpsCast(y));
        }


        public static long or(long x, long y)
        {
            return x | y;
        }

        #endregion

        #region xor


        public static long xor(object x, object y)
        {
            return xor(bitOpsCast(x), bitOpsCast(y));
        }


        public static long xor(object x, long y)
        {
            return xor(bitOpsCast(x), y);
        }


        public static long xor(long x, object y)
        {
            return xor(x, bitOpsCast(y));
        }


        public static long xor(long x, long y)
        {
            return x ^ y;
        }

        #endregion

        #region andNot


        public static long andNot(object x, object y)
        {
            return andNot(bitOpsCast(x), bitOpsCast(y));
        }


        public static long andNot(object x, long y)
        {
            return andNot(bitOpsCast(x), y);
        }


        public static long andNot(long x, object y)
        {
            return andNot(x, bitOpsCast(y));
        }


        public static long andNot(long x, long y)
        {
            return x & ~y;
        }

        #endregion

        #region clearBit


        public static long clearBit(object x, object y)
        {
            return clearBit(bitOpsCast(x), bitOpsCast(y));
        }


        public static long clearBit(object x, long y)
        {
            return clearBit(bitOpsCast(x), y);
        }


        public static long clearBit(long x, object y)
        {
            return clearBit(x, bitOpsCast(y));
        }


        public static long clearBit(long x, long n)
        {
            return clearBit(x, (int)n);
        }


        public static long clearBit(long x, int n)
        {
            return x & ~(1L << n);
        }

        #endregion

        #region setBit


        public static long setBit(object x, object y)
        {
            return setBit(bitOpsCast(x), bitOpsCast(y));
        }


        public static long setBit(object x, long y)
        {
            return setBit(bitOpsCast(x), y);
        }


        public static long setBit(long x, object y)
        {
            return setBit(x, bitOpsCast(y));
        }


        public static long setBit(long x, long n)
        {
            return setBit(x, (int)n);
        }


        public static long setBit(long x, int n)
        {
            return x | (1L << n);
        }

        #endregion

        #region flipBit


        public static long flipBit(object x, object y)
        {
            return flipBit(bitOpsCast(x), bitOpsCast(y));
        }


        public static long flipBit(object x, long y)
        {
            return flipBit(bitOpsCast(x), y);
        }


        public static long flipBit(long x, object y)
        {
            return flipBit(x, bitOpsCast(y));
        }


        public static long flipBit(long x, long n)
        {
            return flipBit(x, (int)n);
        }


        public static long flipBit(long x, int n)
        {
            return x ^ (1L << n);
        }

        #endregion

        #region testBit


        public static bool testBit(object x, object y)
        {
            return testBit(bitOpsCast(x), bitOpsCast(y));
        }


        public static bool testBit(object x, long y)
        {
            return testBit(bitOpsCast(x), y);
        }


        public static bool testBit(long x, object y)
        {
            return testBit(x, bitOpsCast(y));
        }


        public static bool testBit(long x, long n)
        {
            return testBit(x, (int)n);
        }


        public static bool testBit(long x, int n)
        {
            return (x & (1L << n)) != 0;
        }

        #endregion
#pragma warning restore IDE1006 // Naming Styles
        #endregion
    }
}
