﻿/**
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
    public class PersistentListTests : AssertionHelper
    {
        #region C-tor tests

        [Test]
        public void OneArgCtorConstructsListOfOneElement()
        {
            PersistentList p = new PersistentList("abc");

            Expect(p.first(), EqualTo("abc"));
            Expect(p.next(), Null);
            Expect(p.count(), EqualTo(1));
        }

        [Test]
        public void ListCtorConstructsListOfSeveralElements()
        {
            object[] items = new object[] { 1, "abc", 2, "def" };
            IPersistentList p = PersistentList.create(items);

            Expect(p.count(), EqualTo(4));

            ISeq s = p.seq();
            Expect(s.first(), EqualTo(1));
            Expect(s.next().first(), EqualTo("abc"));
            Expect(s.next().next().first(), EqualTo(2));
            Expect(s.next().next().next().first(), EqualTo("def"));
            Expect(s.next().next().next().next(), Null);
        }


        #endregion

        #region IPersistentStack tests

        [Test]
        public void PeekYieldsFirstElementAndListUnchanged()
        {
            PersistentList p = (PersistentList)PersistentList.create(new object[] { "abc", 1, "def" });

            Expect(p.peek(), EqualTo("abc"));
            Expect(p.count(), EqualTo(3));
        }

        [Test]
        public void PopLosesfirstElement()
        {
            PersistentList p = (PersistentList)PersistentList.create(new object[]{"abc", 1, "def"});
            PersistentList p2 = (PersistentList)p.pop();
            Expect(p2.count(), EqualTo(2));
            Expect(p2.peek(), EqualTo(1));
        }

        [Test]
        public void PopOnSingletonListYieldsEmptyList()
        {
            PersistentList p = new PersistentList("abc");
            IPersistentStack s = p.pop();
            Expect(s.count(), EqualTo(0));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DoublePopOnSingletonListYieldsException()
        {
            PersistentList p = new PersistentList("abc");
            p.pop().pop();
        }

        #endregion

        #region IPersistentCollection tests

        [Test]
        public void EmptyHasNoElements()
        {
            PersistentList p = new PersistentList("abc");
            IPersistentCollection c = p.empty();

            Expect(c.count(), EqualTo(0));
        }

        [Test]
        public void EmptyPreservesMeta()
        {
            IPersistentMap meta = new DummyMeta();

            IPersistentCollection p = (IPersistentCollection)new PersistentList("abc").withMeta(meta);
            IObj obj = (IObj) p.empty();

            Expect(obj.meta(), SameAs(meta));
        }

        #endregion

        #region IReduce  tests

        [Test]
        public void ReduceWithNoStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            PersistentList p = (PersistentList)PersistentList.create(new object[] { 1, 2, 3 });
            object ret = p.reduce(fn);

            Expect(ret, EqualTo(6));
        }

        [Test]
        public void ReduceWithStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            PersistentList p = (PersistentList)PersistentList.create(new object[] { 1, 2, 3 });
            object ret = p.reduce(fn, 20);

            Expect(ret, EqualTo(26));
        }


        #endregion
    }

    [TestFixture]
    public class PersistentList_ISeq_Tests : ISeqTestHelper
    {
        #region setup

        PersistentList _pl;
        PersistentList _plWithMeta;
        object[] _values;


        [SetUp]
        public void Setup()
        {
            PersistentList p1 = new PersistentList("abc");
            PersistentList p2 = (PersistentList)p1.cons("def");
            _pl = (PersistentList)p2.cons(7);
            _values = new object[] { 7, "def", "abc" };
            _plWithMeta = (PersistentList)_pl.withMeta(PersistentHashMap.create("a", 1));
        }

        #endregion

        #region ISeq tests

        [Test]
        public void ISeq_has_correct_valuess()
        {
            VerifyISeqContents(_pl, _values);
        }

        [Test]
        public void ISeq_with_meta_has_correct_valuess()
        {
            VerifyISeqContents(_plWithMeta, _values);
        }

        [Test]
        public void Rest_has_correct_type()
        {
            VerifyISeqRestTypes(_pl, typeof(PersistentList));
        }

        [Test]
        public void Cons_works()
        {
            VerifyISeqCons(_pl, "pqr", _values);
        }

        [Test]
        public void ConsPreservesMeta()
        {
            PersistentList p2 = (PersistentList)_plWithMeta.cons("def");
            Expect(p2.meta(), SameAs(_plWithMeta.meta()));
        }

        #endregion
    }


    [TestFixture]
    public class PersistentList_IObj_Tests : IObjTests
    {
        [SetUp]
        public void Setup()
        {
            IPersistentMap meta = new DummyMeta();

            PersistentList p1 = (PersistentList)PersistentList.create(new object[] { "abc", "def" });

            _objWithNullMeta = (IObj)p1;
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(PersistentList);
        }
    }
}
