using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using Rhino.Mocks;

using clojure.lang;

using RMExpect = Rhino.Mocks.Expect;


namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class LazilyPersistentVectorTests : AssertionHelper
    {
        #region C-tor tests

        [Test] 
        public void CreateOwningOnNoParamsReturnsEmptyVector()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning();
            Expect(v.count(),EqualTo(0));
        }

        [Test]
        public void CreatingOwningOnParamsReturnsVector()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            Expect(v.count(), EqualTo(3));
            Expect(v.nth(0), EqualTo(1));
            Expect(v.nth(1), EqualTo(2));
            Expect(v.nth(2), EqualTo(3));
        }

        [Test]
        public void CreateOnEmptySeqReturnsEmptyVector()
        {
            IPersistentVector v = LazilyPersistentVector.create(new object[] {});
            Expect(v.count(), EqualTo(0));
        }

        [Test]
        public void CreateOnNonEmptyCollectionReturnsVector()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(new object[] {1, 2, 3});
            Expect(v.count(), EqualTo(3));
            Expect(v.nth(0), EqualTo(1));
            Expect(v.nth(1), EqualTo(2));
            Expect(v.nth(2), EqualTo(3));
        }


        #endregion

        #region IPersistentVector tests

        [Test]
        public void NthInRangeWorks()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
 
            Expect(v.count(), EqualTo(3));
            Expect(v.nth(0), EqualTo(1));
            Expect(v.nth(1), EqualTo(2));
            Expect(v.nth(2), EqualTo(3));
        }

        [Test]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void NthOutOfRangeLowFails()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            
            Expect(v.nth(-4), EqualTo(1));
        }

        [Test]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void NthOutOfRangeHighFails()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);

            Expect(v.nth(4), EqualTo(1));
        }

        [Test]
        public void AssocnInRangeModifies()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            IPersistentVector v2 = v.assocN(1, 4);

            Expect(v.count(), EqualTo(3));
            Expect(v.nth(0), EqualTo(1));
            Expect(v.nth(1), EqualTo(2));
            Expect(v.nth(2), EqualTo(3));

            Expect(v2.count(), EqualTo(3));
            Expect(v2.nth(0), EqualTo(1));
            Expect(v2.nth(1), EqualTo(4));
            Expect(v2.nth(2), EqualTo(3));
        }

        [Test]
        public void AssocnAtEndModifies()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            IPersistentVector v2 = v.assocN(3, 4);

            Expect(v.count(), EqualTo(3));
            Expect(v.nth(0), EqualTo(1));
            Expect(v.nth(1), EqualTo(2));
            Expect(v.nth(2), EqualTo(3));

            Expect(v2.count(), EqualTo(4));
            Expect(v2.nth(0), EqualTo(1));
            Expect(v2.nth(1), EqualTo(2));
            Expect(v2.nth(2), EqualTo(3));
            Expect(v2.nth(3), EqualTo(4));
        }

        [Test]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void AssocNOutOfRangeLowFails()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            IPersistentVector v2 = v.assocN(-4, 4);
        }

        [Test]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void AssocNOutOfRangeHighFails()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            IPersistentVector v2 = v.assocN(4, 4);
        }


        [Test]
        public void ConsAddsAtEnd()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            IPersistentVector v2 = v.cons(4);

            Expect(v.count(), EqualTo(3));
            Expect(v.nth(0), EqualTo(1));
            Expect(v.nth(1), EqualTo(2));
            Expect(v.nth(2), EqualTo(3));

            Expect(v2.count(), EqualTo(4));
            Expect(v2.nth(0), EqualTo(1));
            Expect(v2.nth(1), EqualTo(2));
            Expect(v2.nth(2), EqualTo(3));
            Expect(v2.nth(3), EqualTo(4));
        }

        [Test]
        public void LengthWorks()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);

            Expect(v.length(), EqualTo(3));
        }

        #endregion

        #region IPersistentStack tests

        [Test]
        public void PopOnNonEmptyWorks()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);
            IPersistentVector v2 = (IPersistentVector)((IPersistentStack)v).pop();

            Expect(v.count(), EqualTo(3));
            Expect(v.nth(0), EqualTo(1));
            Expect(v.nth(1), EqualTo(2));
            Expect(v.nth(2), EqualTo(3));

            Expect(v2.count(), EqualTo(2));
            Expect(v2.nth(0), EqualTo(1));
            Expect(v2.nth(1), EqualTo(2));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PopOnEmptyFails()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning();
            IPersistentStack v2 = ((IPersistentStack)v).pop();
        }

        #endregion
    }

    [TestFixture]
    public class LazilyPersistentVector_IObj_Tests : IObjTests
    {
        [SetUp]
        public void Setup()
        {
            IPersistentVector v = LazilyPersistentVector.createOwning(1, 2, 3);

            _obj = _objWithNullMeta = (IObj)v;
            _expectedType = typeof(LazilyPersistentVector);
        }
    }
}
