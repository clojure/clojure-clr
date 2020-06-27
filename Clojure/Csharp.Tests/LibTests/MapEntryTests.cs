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
    public class MapEntryTests
    {
        #region C-tor tests

        [Test]
        public void CtorCreatesEntryWithProperKeyVal()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.key()).To.Equal(1);
            Expect(me.val()).To.Equal("abc");
        }

        #endregion
        
        #region Object override tests

        [Test]
        public void HashCodeSameAsPersistentVector()
        {
            MapEntry me = new MapEntry(1, "abc");
            PersistentVector v = PersistentVector.create(1, "abc");

            Expect(me.GetHashCode()).To.Equal(v.GetHashCode());
        }

        [Test]
        public void HashCodeFalseOnDifferentValues()
        {
            MapEntry me = new MapEntry(1, "abc");
            PersistentVector v = PersistentVector.create(1, "abcd");

            Expect(me.GetHashCode()).To.Not.Equal(v.GetHashCode());
        }

        [Test]
        public void EqualsWorksOnPersistentVector()
        {
            MapEntry me = new MapEntry(1, "abc");
            PersistentVector v = PersistentVector.create(1, "abc");

            Expect(me.Equals(v));
        }

        [Test]
        public void EqualsWorksFalseOnDifferentValues()
        {
            MapEntry me = new MapEntry(1, "abc");
            PersistentVector v = PersistentVector.create(1, "abcd");

            Expect(me.Equals(v)).To.Be.False();
        }

        
        #endregion
        
        #region IMapEntry tests
        
        #endregion
        
        #region IPersistentVector tests

        [Test]
        public void LengthIs2()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.length()).To.Equal(2);
        }

        [Test]
        public void NthInRangeWorks()
        {

            MapEntry me = new MapEntry(1, "abc");

            Expect(me.nth(0)).To.Equal(1);
            Expect(me.nth(1)).To.Equal("abc");
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NthOutOfRangeLowFails()
        {
            MapEntry me = new MapEntry(1, "abc");
            me.nth(-4);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NthOutOfRangeHighFails()
        {
            MapEntry me = new MapEntry(1, "abc");
            me.nth(4);
        }

        [Test]
        public void AssocNInRangeModifies()
        {
            MapEntry me = new MapEntry(1, "abc");
            IPersistentVector v1 = me.assocN(0, 2);
            IPersistentVector v2 = me.assocN(1, "def");
            IPersistentVector v3 = me.assocN(2, "ghi");

            Expect(me.count()).To.Equal(2);
            Expect(me.key()).To.Equal(1);
            Expect(me.val()).To.Equal("abc");

            Expect(v1.count()).To.Equal(2);
            Expect(v1.nth(0)).To.Equal(2);
            Expect(v1.nth(1)).To.Equal("abc");

            Expect(v2.count()).To.Equal(2);
            Expect(v2.nth(0)).To.Equal(1);
            Expect(v2.nth(1)).To.Equal("def");

            Expect(v3.count()).To.Equal(3);
            Expect(v3.nth(0)).To.Equal(1);
            Expect(v3.nth(1)).To.Equal("abc");
            Expect(v3.nth(2)).To.Equal("ghi");
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AssocNOutOfRangeLowThrows()
        {
            MapEntry me = new MapEntry(1, "abc");
            me.assocN(-4, 2);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AssocNOutOfRangeHighThrows()
        {
            MapEntry me = new MapEntry(1, "abc");
            me.assocN(4, 2);
        }

        [Test]
        public void ConsWorks()
        {
            MapEntry me = new MapEntry(1, "abc");
            IPersistentVector v1 = me.cons(2);

            Expect(me.count()).To.Equal(2);
            Expect(me.key()).To.Equal(1);
            Expect(me.val()).To.Equal("abc");


            Expect(v1.count()).To.Equal(3);
            Expect(v1.nth(0)).To.Equal(1);
            Expect(v1.nth(1)).To.Equal("abc");
            Expect(v1.nth(2)).To.Equal(2);
        }

        #endregion
        
        #region Associative tests

        [Test]
        public void ContainsKeyOnExistingKeyWorks()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.containsKey(0));
            Expect(me.containsKey(1));
        }

        [Test]
        public void ContainsKeyOutOfRangeIsFalse()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.containsKey(-4)).To.Be.False();
            Expect(me.containsKey(4)).To.Be.False();
        }


        [Test]
        public void EntryAtOnExistingKeyWorks()
        {
            MapEntry me = new MapEntry(1, "abc");
            IMapEntry me1 = me.entryAt(0);
            IMapEntry me2 = me.entryAt(1);

            Expect(me1.key()).To.Equal(0);
            Expect(me1.val()).To.Equal(1);
            Expect(me2.key()).To.Equal(1);
            Expect(me2.val()).To.Equal("abc");
        }

        [Test]
        public void EntryAtOutOfRangeLowReturnsNull()
        {
            MapEntry me = new MapEntry(1, "abc");
            IMapEntry me1 = me.entryAt(-4);

            Expect(me1).To.Be.Null();
        }

        [Test]
        public void EntryAtOutOfRangeHighReturnsNull()
        {
            MapEntry me = new MapEntry(1, "abc");
            IMapEntry me1 = me.entryAt(4);

            Expect(me1).To.Be.Null();
        }

        [Test]
        public void ValAtOnExistingKeyReturnsValue()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.valAt(0)).To.Equal(1);
            Expect(me.valAt(1)).To.Equal("abc");
        }

        [Test]
        public void ValAtOnMissingKeyReturnsNull()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.valAt(-4)).To.Be.Null();
            Expect(me.valAt(4)).To.Be.Null();
        }

        [Test]
        public void ValAtWithDefaultOnExistingKeyReturnsValue()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.valAt(0,7)).To.Equal(1);
            Expect(me.valAt(1,7)).To.Equal("abc");
        }

        [Test]
        public void ValAtWithDefaultOnMissingKeyReturnsDefault()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.valAt(-4,7)).To.Equal(7);
            Expect(me.valAt(4, 7)).To.Equal(7);
        }

        #endregion
        
        #region Reversible tests

        [Test]
        public void RseqReturnReverseSeq()
        {
            MapEntry me = new MapEntry(1, "abc");

            ISeq s = me.rseq();

            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal("abc");
            Expect(s.next().first()).To.Equal(1);
            Expect(s.next().next()).To.Be.Null();
        }
        
        #endregion
        
        #region IPersistentCollection tests

        [Test]
        public void CountIs2()
        {
            MapEntry me = new MapEntry(1, "abc");
            Expect(me.count()).To.Equal(2);
        }

        [Test]
        public void SeqReturnsASeq()
        {
            MapEntry me = new MapEntry(1, "abc");
            ISeq s = me.seq();

            Expect(s.count()).To.Equal(2);
            Expect(s.first()).To.Equal(1);
            Expect(s.next().first()).To.Equal("abc");
            Expect(s.next().next()).To.Be.Null();
        }

        [Test]
        public void EmptyReutrnsNull()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.empty()).To.Be.Null();
        }


        [Test]
        public void ExplictIPersistentCollectionConsWorks()
        {
            MapEntry me = new MapEntry(1, "abc");
            IPersistentCollection c = (IPersistentCollection)me;
            ISeq s = c.cons(2).seq();

            Expect(me.count()).To.Equal(2);
            Expect(me.key()).To.Equal(1);
            Expect(me.val()).To.Equal("abc");

            Expect(s.count()).To.Equal(3);
            Expect(s.first()).To.Equal(1);
            Expect(s.next().first()).To.Equal("abc");
            Expect(s.next().next().first()).To.Equal(2);
            Expect(s.next().next().next()).To.Be.Null();
        }
        
        #endregion
        
        #region IPersistentStack tests

        [Test]
        public void PeekReturnsVal()
        {
            MapEntry me = new MapEntry(1, "abc");

            Expect(me.peek()).To.Equal("abc");
            Expect(me.count()).To.Equal(2);
            Expect(me.key()).To.Equal(1);
            Expect(me.val()).To.Equal("abc");
        }

        [Test]
        public void PopLosesTheValue()
        {
            MapEntry me = new MapEntry(1, "abc");
            IPersistentVector v = (IPersistentVector)me.pop();

            Expect(v.length()).To.Equal(1);
            Expect(v.nth(0)).To.Equal(1);
        }
    


        
        #endregion

    }
}
