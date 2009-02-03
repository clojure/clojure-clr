using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using Rhino.Mocks;

using clojure.lang;

using RMExpect = Rhino.Mocks.Expect;


namespace DataTests
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
            Expect(p.rest(), Null);
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
            Expect(s.rest().first(), EqualTo("abc"));
            Expect(s.rest().rest().first(), EqualTo(2));
            Expect(s.rest().rest().rest().first(), EqualTo("def"));
            Expect(s.rest().rest().rest().rest(), Null);
        }


        #endregion

        #region ISeq tests

        [Test]
        public void MultipleConsesOnPersistentList()
        {
            PersistentList p1 = new PersistentList("abc");
            PersistentList p2 = (PersistentList) p1.cons("def");
            PersistentList p3 = (PersistentList) p2.cons(7);

            Expect(p3.first(), EqualTo(7));
            Expect(p3.rest(), SameAs(p2));
        }

        [Test]
        public void ConsPreservesMeta()
        {
            MockRepository mocks = new MockRepository();
            IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
            mocks.ReplayAll();

            PersistentList p1 = (PersistentList) new PersistentList("abc").withMeta(meta);
            PersistentList p2 = (PersistentList)p1.cons("def");

            Expect(p2.meta(), SameAs(meta));
            mocks.VerifyAll();

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
            IPersistentStack s = p.pop().pop();
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
            MockRepository mocks = new MockRepository();
            IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
            mocks.ReplayAll();

            IPersistentCollection p = (IPersistentCollection)new PersistentList("abc").withMeta(meta);
            IObj obj = (IObj) p.empty();

            Expect(obj.meta(), SameAs(meta));
            mocks.VerifyAll();
        }

        #endregion

        #region IReduce  tests

        [Test]
        public void ReduceWithNoStartIterates()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(1, 2)).Return(5);
            RMExpect.Call(fn.invoke(5, 3)).Return(7);
            mocks.ReplayAll();

            PersistentList p = (PersistentList)PersistentList.create(new object[] { 1, 2, 3 });
            object ret = p.reduce(fn);

            Expect(ret, EqualTo(7));

            mocks.VerifyAll();
        }

        [Test]
        public void ReduceWithStartIterates()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(20, 1)).Return(10);
            RMExpect.Call(fn.invoke(10, 2)).Return(5);
            RMExpect.Call(fn.invoke(5, 3)).Return(7);
            mocks.ReplayAll();

            PersistentList p = (PersistentList)PersistentList.create(new object[] { 1, 2, 3 });
            object ret = p.reduce(fn, 20);

            Expect(ret, EqualTo(7));

            mocks.VerifyAll();
        }


        #endregion

    }

    [TestFixture]
    public class PersistentList_IObj_Tests : IObjTests
    {
        MockRepository _mocks;

        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            IPersistentMap meta = _mocks.StrictMock<IPersistentMap>();
            _mocks.ReplayAll();

            PersistentList p1 = (PersistentList)PersistentList.create(new object[] { "abc", "def" });


            _objWithNullMeta = (IObj)p1;
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(PersistentList);
        }

        [TearDown]
        public void Teardown()
        {
            _mocks.VerifyAll();
        }

    }
}
