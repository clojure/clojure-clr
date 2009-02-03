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
    public class RangeTests : AssertionHelper
    {
        #region C-tor tests

        [Test]
        public void Basic_ctor_has_no_meta()
        {
            Range r = new Range(2, 5);
            Expect(r.meta(), Null);
        }

        [Test]
        public void Meta_ctor_has_meta()
        {
            MockRepository mocks = new MockRepository();
            IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
            mocks.ReplayAll();

            Range r = new Range(meta, 2, 5);

            Expect(r.meta(), EqualTo(meta));

            mocks.VerifyAll();
        }

        #endregion

        #region IPersistentCollection tests

        [Test]
        public void Range_has_correct_count()
        {
            Range r = new Range(2, 20);

            Expect(r.count(), EqualTo(18));
        }


        #endregion

        #region ISeq tests

        [Test]
        public void First_yields_start_of_range()
        {
            Range r = new Range(2, 5);

            Expect(r.first(), EqualTo(2));
        }

        [Test]
        public void Rest_yields_another_range()
        {
            Range r = new Range(2, 5);
            ISeq s = r.rest();

            Expect(s, TypeOf(typeof(Range)));
        }

        [Test]
        public void Rest_range_has_correct_count_and_first_element()
        {
            Range r = new Range(2, 5);
            ISeq s = r.rest();

            Expect(s.first(), EqualTo(3));
            Expect(s.count(), EqualTo(2));
        }

        [Test]
        public void Rest_eventually_yields_null()
        {
            Range r = new Range(2, 4);

            Expect(r.first(), EqualTo(2));
            Expect(r.rest().first(), EqualTo(3));
            Expect(r.rest().rest(), Null);
        }

        [Test]
        public void Rest_preserves_meta()
        {
            MockRepository mocks = new MockRepository();
            IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
            mocks.ReplayAll();

            Range r = new Range(meta, 2, 5);
            IObj obj = (IObj) r.rest();

            Expect(obj.meta(), EqualTo(meta));

            mocks.VerifyAll();
        }


        [Test]
        public void Cons_puts_anything_on_front_from_ASeq()
        {
            Range r = new Range(2, 4);
            ISeq s = r.cons("abc");

            Expect(s.first(), EqualTo("abc"));
            Expect(s.rest(),SameAs(r));
         }


        #endregion

        #region IReduce tests

        [Test]
        public void ReduceWithNoStartIterates()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(2, 3)).Return(5);
            RMExpect.Call(fn.invoke(5, 4)).Return(7);
            mocks.ReplayAll();

            Range r = new Range(2, 5);
            object ret = r.reduce(fn);

            Expect(ret, EqualTo(7));

            mocks.VerifyAll();
        }

        [Test]
        public void ReduceWithStartIterates()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(20, 2)).Return(10);
            RMExpect.Call(fn.invoke(10, 3)).Return(5);
            RMExpect.Call(fn.invoke(5, 4)).Return(7);
            mocks.ReplayAll();

            Range r = new Range(2, 5);
            object ret = r.reduce(fn, 20);

            Expect(ret, EqualTo(7));

            mocks.VerifyAll();
        }

        #endregion

        // TODO: test stream capability of Range
        
    }


    [TestFixture]
    public class Range_IObj_Tests : IObjTests
    {
        MockRepository _mocks;

        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            IPersistentMap meta = _mocks.StrictMock<IPersistentMap>();
            _mocks.ReplayAll();

            Range r = new Range(2, 5);

            _objWithNullMeta = (IObj)r;
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(Range);
        }

        [TearDown]
        public void Teardown()
        {
            _mocks.VerifyAll();
        }

    }
}
