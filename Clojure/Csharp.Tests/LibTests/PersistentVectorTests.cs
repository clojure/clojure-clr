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

using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using NExpect;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class PersistentVectorTests
    {

        #region C-tor tests

        [Test]
        public void CreateOnISeqReturnsCorrectCount()
        {
            ISeq r = LongRange.create(2,5);
            PersistentVector v = PersistentVector.create(r);

            Expect(v.count()).To.Equal(r.count());
        }

        [Test]
        public void CreateOnISeqHasItems()
        {
            ISeq r = LongRange.create(2, 5);
            PersistentVector v = PersistentVector.create(r);

            Expect((long)v.nth(0)).To.Equal(2);
            Expect((long)v.nth(1)).To.Equal(3);
            Expect((long)v.nth(2)).To.Equal(4);
        }

        [Test]
        public void CreateOnISeqWithManyItemsWorks()
        {
            // Want to bust out of the first tail, so need to insert more than 32 elements.
            ISeq r = LongRange.create(2, 1000);

            PersistentVector v = PersistentVector.create(r);

            Expect(v.count()).To.Equal(r.count());
            for (int i = 0; i < v.count(); ++i)
                Expect((long)v.nth(i)).To.Equal(i + 2L);
        }

        [Test]
        public void CreateOnISeqWithManyManyItemsWorks()
        {
            // Want to bust out of the first tail, so need to insert more than 32 elements.
            // Let's get out of the second level, too.

            ISeq r = LongRange.create(2, 100000);
            PersistentVector v = PersistentVector.create(r);

            Expect(v.count()).To.Equal(r.count());
            for (int i = 0; i < v.count(); ++i)
                Expect((long)v.nth(i)).To.Equal(i + 2L);
        }

        [Test]
        public void CreateOnMultipleItemsWorks()
        {
            PersistentVector v = PersistentVector.create(2,3,4);

            Expect(v.count()).To.Equal(3);
            Expect(v.nth(0)).To.Equal(2);
            Expect(v.nth(1)).To.Equal(3);
            Expect(v.nth(2)).To.Equal(4);
        }

        #endregion

        #region IPersistentVector tests


        // nth - tested in c-tor tests


        [Test]
        public void CountYieldsLength()
        {
            PersistentVector v = PersistentVector.create(1, 2, 3);

            Expect(v.length()).To.Equal(3);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NthOutOfRangeLowFails()
        {
            PersistentVector v = PersistentVector.create(1, 2, 3);
            v.nth(-4);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NthOutOfRangeHighFails()
        {
            PersistentVector v = PersistentVector.create(1, 2, 3);
            v.nth(4);
        }

        [Test]
        public void AssocNReplacesInRangeForSmall()
        {
            ISeq r = LongRange.create(2, 5); 
            PersistentVector v1 = PersistentVector.create(r);
            IPersistentVector v2 = v1.assocN(1,10L);

            Expect((long)v1.nth(0)).To.Equal(2);
            Expect((long)v1.nth(1)).To.Equal(3);
            Expect((long)v1.nth(2)).To.Equal(4);
            Expect((long)v1.count()).To.Equal(3);
            Expect((long)v2.nth(0)).To.Equal(2);
            Expect((long)v2.nth(1)).To.Equal(10);
            Expect((long)v2.nth(2)).To.Equal(4);
            Expect((long)v2.count()).To.Equal(3);
        }

        [Test]
        public void AssocNAddsAtEndForSmall()
        {
            ISeq r = LongRange.create(2, 5);
            PersistentVector v1 = PersistentVector.create(r);
            IPersistentVector v2 = v1.assocN(3, 10L);

            Expect((long)v1.nth(0)).To.Equal(2);
            Expect((long)v1.nth(1)).To.Equal(3);
            Expect((long)v1.nth(2)).To.Equal(4);
            Expect((long)v1.count()).To.Equal(3);
            Expect((long)v2.nth(0)).To.Equal(2);
            Expect((long)v2.nth(1)).To.Equal(3);
            Expect((long)v2.nth(2)).To.Equal(4);
            Expect((long)v2.nth(3)).To.Equal(10);
            Expect((long)v2.count()).To.Equal(4);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AssocNOutOfRangeLowThrowsException()
        {
            ISeq r = LongRange.create(2, 5);
            PersistentVector v1 = PersistentVector.create(r);
            v1.assocN(-4, 10);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AssocNOutOfRangeHighThrowsException()
        {
            ISeq r = LongRange.create(2, 5);
            PersistentVector v1 = PersistentVector.create(r);
            v1.assocN(4, 10);
        }

        [Test]
        public void AssocNAddsAtEndForEmpty()
        {
            PersistentVector v1 = PersistentVector.create();
            IPersistentVector v2 = v1.assocN(0, "abc");

            Expect(v1.count()).To.Equal(0);
            Expect(v2.count()).To.Equal(1);
            Expect(v2.nth(0)).To.Equal("abc");
        }

        [Test]
        public void AssocNChangesForBig()
        {
            ISeq r = LongRange.create(2, 100000);
            PersistentVector v1 = PersistentVector.create(r);
            IPersistentVector v2 = v1;

            for (int i = 0; i < 110000; i++)
                v2 = v2.assocN(i, i + 20L);

            for ( int i=0; i<v1.count(); ++i )
                Expect((long)v1.nth(i)).To.Equal(i+2L);

            for (int i = 0; i < v2.count(); ++i)
            {
                object o = v2.nth(i);
                Expect(o).To.Be.An.Instance.Of<long>();
                Expect((long)o).To.Equal(i + 20L);
            }
        }

        [Test]
        public void ConsWorks()
        {
            PersistentVector v1 = PersistentVector.create(2,3,4);
            IPersistentVector v2 = v1;

            for (int i = 3; i < 100000; i++)
                v2 = v2.cons(i+2);

            Expect(v1.count()).To.Equal(3);
            Expect(v2.count()).To.Equal(100000);

            for (int i = 0; i < v2.count(); ++i)
                Expect(v2.nth(i)).To.Equal(i + 2);
        }

        #endregion

        #region IPersistentCollection tests

        [Test]
        public void EmptyReturnsEmptyCollection()
        {
            PersistentVector v = PersistentVector.create(1, 2, 3);
            IPersistentCollection e = v.empty();

            Expect(e.count()).To.Equal(0);
        }

        [Test]
        public void EmptyCopiesMeta()
        {
            IPersistentMap meta = new DummyMeta();

            PersistentVector v1 = PersistentVector.create(1, 2, 3);
            IPersistentCollection e1 = v1.empty();

            PersistentVector v2 = (PersistentVector) v1.withMeta(meta);
            IPersistentCollection e2 = v2.empty();

            Expect(((IObj)e1).meta()).To.Be.Null();
            Expect(Object.ReferenceEquals(((IObj)e2).meta(), meta));
        }


        #endregion

        #region IPersistentStack tests

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void PopOnEmptyThrowsException()
        {
            PersistentVector v = PersistentVector.create();
            v.pop();
        }

        [Test]
        public void PopOnSizeOneReturnsEmpty()
        {
            PersistentVector v = PersistentVector.create(1);
            IPersistentStack s = v.pop();

            Expect(s.count()).To.Equal(0);
        }

        [Test]
        public void PopOnSmallReturnsOneLess()
        {
            ISeq r = LongRange.create(2, 20);
            PersistentVector v = PersistentVector.create(r);
            IPersistentStack s = v.pop();

            Expect(v.count()).To.Equal(r.count());
            Expect(s.count()).To.Equal(v.count()-1);
        }

        [Test]
        public void PopOnBigWorks()
        {
            ISeq r = LongRange.create(0,100000);
            PersistentVector v = PersistentVector.create(r);
            IPersistentStack s = v;
            for (int i = 16; i < 100000; i++)
                s = s.pop();

            Expect(v.count()).To.Equal(r.count());
            Expect(s.count()).To.Equal(16);
        }

        #endregion

        #region IFn tests

        #endregion
    }

    [TestFixture]
    public class PersistentVector_IObj_Tests : IObjTests
    {
        [SetUp]
        public void Setup()
        {
            IPersistentMap meta = new DummyMeta();

            PersistentVector v = PersistentVector.create(2, 3, 4);

            _testNoChange = false;
            _objWithNullMeta = (IObj)v;
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(PersistentVector);
        }
    }
}
