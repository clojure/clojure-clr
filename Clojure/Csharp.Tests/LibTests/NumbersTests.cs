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

using NUnit.Framework;

using clojure.lang;


namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class NumbersTests : AssertionHelper
    {
        #region Helpers

        private void ExpectInt32(object x)
        {
            Expect(x, TypeOf(typeof(Int32)));
        }

        private void ExpectSameObject(object x, object y)
        {
            Expect(x, SameAs(y));
        }

        private void ExpectEqualObject(object x, object y)
        {
            Expect(x, EqualTo(y));
        }

        #endregion

        #region reduce tests

        //[Test]
        //public void ReduceOnBigIntReducesSmallerValues()
        //{
        //    //BigInteger b1 = new BigInteger("123");
        //    //BigInteger b2 = new BigInteger("0");
        //    //BigInteger b3 = new BigInteger(Int32.MaxValue.ToString());
        //    //BigInteger b4 = new BigInteger(Int32.MinValue.ToString()); BigInteger b1 = new BigInteger("123");
        //    BigInteger b1 = BigInteger.Create(123);
        //    BigInteger b2 = BigInteger.Create(0);
        //    BigInteger b3 = BigInteger.Create(Int32.MaxValue);
        //    BigInteger b4 = BigInteger.Create(Int32.MinValue);
                
        //    ExpectInt32(Numbers.reduceBigInt(b1));
        //    ExpectInt32(Numbers.reduceBigIntege(b2));
        //    ExpectInt32(Numbers.reduceBigInteger(b3));
        //    ExpectInt32(Numbers.reduceBigInteger(b4));
        //}

        //[Test]
        //public void ReduceOnBigIntReturnsLargerValues()
        //{
        //    //BigInteger b1 = new BigInteger("100000000000000000000", 16);
        //    //BigInteger b2 = b1.negate();
        //    //BigInteger b3 = new BigInteger("123456789012345678901234567890");
        //    //BigInteger b4 = b3.negate();
        //    BigInteger b1 = BigInteger.Parse("100000000000000000000", 16);
        //    BigInteger b2 = b1.Negate();
        //    BigInteger b3 = BigInteger.Parse("123456789012345678901234567890");
        //    BigInteger b4 = b3.Negate();

        //    ExpectSameObject(b1, Numbers.reduceBigInteger(b1));
        //    ExpectSameObject(b2, Numbers.reduceBigInteger(b2));
        //    ExpectSameObject(b3, Numbers.reduceBigInteger(b3));
        //    ExpectSameObject(b4, Numbers.reduceBigInteger(b4));
        //}

        //[Test]
        //public void ReduceOnLongReducesSmallerValues()
        //{
        //    long b1 = 123;
        //    long b2 = 0;
        //    long b3 = Int32.MaxValue;
        //    long b4 = Int32.MinValue;

        //    ExpectInt32(Numbers.reduce(b1));
        //    ExpectInt32(Numbers.reduce(b2));
        //    ExpectInt32(Numbers.reduce(b3));
        //    ExpectInt32(Numbers.reduce(b4));
        //}


        //[Test]
        //public void ReduceOnLongReturnsLargerValues()
        //{
        //    long b1 = ((long)Int32.MaxValue) + 1;
        //    long b2 = ((long)Int32.MinValue) - 1;
        //    long b3 = 123456789000;
        //    long b4 = -b3;

        //    ExpectEqualObject(b1, Numbers.reduce(b1));
        //    ExpectEqualObject(b2, Numbers.reduce(b2));
        //    ExpectEqualObject(b3, Numbers.reduce(b3));
        //    ExpectEqualObject(b4, Numbers.reduce(b4));
        //}

        #endregion

        #region divide tests

        [Test]
        [ExpectedException(typeof(ArithmeticException))]
        public void DivideByZeroFails()
        {
           Numbers.BIDivide(BigInteger.One, BigInteger.Zero);
        }

        [Test]
        public void DivideReducesToIntOnDenomOne()
        {
            object o = Numbers.BIDivide(BigInteger.Create(75), BigInteger.Create(25));
            Expect(o, EqualTo(BigInt.fromLong(3)));
        }

        [Test]
        public void DivideReturnsReducedRatio()
        {
            object o = Numbers.BIDivide(BigInteger.Create(42), BigInteger.Create(30));
            
            Expect(o, TypeOf(typeof(Ratio)));
            
            Ratio r = o as Ratio;
            Expect(r.numerator==BigInteger.Create(7));
            Expect(r.denominator==BigInteger.Create(5));
        }

        #endregion

        #region rationalize tests

        [Test]
        public void RationalizeTakesIntegerToBigInteger()
        {
            BigDecimal bd = BigDecimal.Parse("12345");
            object r = Numbers.rationalize(bd);
            Expect(r,InstanceOf(typeof(BigInt)));
            BigInt bi = (BigInt)r;
            Expect(bi,EqualTo(BigInt.fromBigInteger(BigInteger.Parse("12345"))));
        }

        [Test]
        public void RationalizeTakesFractionToBigInteger()
        {
            BigDecimal bd = BigDecimal.Parse("123.45");
            object r = Numbers.rationalize(bd);
            Expect(r, InstanceOf(typeof(Ratio)));
            Ratio rr = (Ratio)r;
            Ratio ratio = new Ratio(BigInteger.Create(12345/5), BigInteger.Create(100/5));
            Expect(rr, EqualTo(ratio));
        }

        [Test]
        public void RaionalizeWorksOnDoubles()
        {
            object r = Numbers.rationalize(0.0625);
            Expect(r, InstanceOf(typeof(Ratio)));
            Ratio rr = (Ratio)r;
            Ratio ratio = new Ratio(BigInteger.Create(1), BigInteger.Create(16));
            Expect(rr, EqualTo(ratio));

        }

        #endregion
    }
}
