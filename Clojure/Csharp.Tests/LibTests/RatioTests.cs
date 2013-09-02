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
    public class RatioTests : AssertionHelper
    {
        [Test]
        public void GetsThePiecesFromTheConstructedThing()
        {
            BigInteger n = BigInteger.Create(30);
            BigInteger d = BigInteger.Create(60);
            Ratio r = new Ratio(n, d);
            Expect(r.numerator, SameAs(n));
            Expect(r.denominator, SameAs(d));
        }

        [Test]
        public void EqualsReturnsTrueOnEquals()
        {
            BigInteger n1 = BigInteger.Create(30);
            BigInteger d1 = BigInteger.Create(60);
            Ratio r1 = new Ratio(n1, d1);
            BigInteger n2 = BigInteger.Create(30);
            BigInteger d2 = BigInteger.Create(60);
            Ratio r2 = new Ratio(n2, d2);
            Expect(r1.Equals(r2));
         }

        [Test]
        public void EqualsReturnsFalseOnEquals()
        {
            BigInteger n1 = BigInteger.Create(30);
            BigInteger d1 = BigInteger.Create(60);
            Ratio r1 = new Ratio(n1, d1);
            BigInteger n2 = BigInteger.Create(31);
            BigInteger d2 = BigInteger.Create(60);
            Ratio r2 = new Ratio(n2, d2);
            Expect(r1.Equals(r2),False);
        }

        [Test]
        public void ToStringDoesSomethingReasonable()
        {
            BigInteger n1 = BigInteger.Create(30);
            BigInteger d1 = BigInteger.Create(60);
            Ratio r1 = new Ratio(n1, d1);
            Expect(r1.ToString(), EqualTo("30/60"));
        }

        [Test]
        public void ToBigDecimalWithNoContextAndNoRoundingRequiredWorks()
        {
            BigInteger n1 = BigInteger.Create(1);
            BigInteger d1 = BigInteger.Create(4);
            Ratio r1 = new Ratio(n1, d1);
            BigDecimal bd = r1.ToBigDecimal();

            Expect(bd, EqualTo(BigDecimal.Parse("0.25")));
        }

        [Test]
        [ExpectedException(typeof(ArithmeticException))]
        public void ToBigDecimalWithNoContextThrowsIfRoundingIsRequired()
        {
            BigInteger n1 = BigInteger.Create(1);
            BigInteger d1 = BigInteger.Create(3);
            Ratio r1 = new Ratio(n1, d1);
            r1.ToBigDecimal();
        }

        [Test]
        public void ToBigDecimalWithContextWorks()
        {
            BigInteger n1 = BigInteger.Create(1);
            BigInteger d1 = BigInteger.Create(3);
            Ratio r1 = new Ratio(n1, d1);
            BigDecimal.Context c = new BigDecimal.Context(6, BigDecimal.RoundingMode.HalfUp);
            BigDecimal bd = r1.ToBigDecimal(c);
            Expect(bd, EqualTo(BigDecimal.Parse("0.333333")));
        }


        [Test]
        public void ToDoubleWorks()
        {
            BigInteger n1 = BigInteger.Create(1);
            BigInteger d1 = BigInteger.Create(3);
            Ratio r1 = new Ratio(n1, d1);
            double d = r1.ToDouble(null);
            Expect(d, EqualTo(0.3333333333333333));  // precision = 16
        }
    }
}
