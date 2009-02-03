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
    public class ArraySeqTests : AssertionHelper
    {

        #region C-tor tests

        [Test]
        public void Create_on_nothing_returns_null()
        {
            ArraySeq a = ArraySeq.create();
            
            Expect(a, Null);
        }

        [Test]
        public void Create_on_array_creates()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array);

            Expect(a, Not.Null);
        }

        [Test]
        public void Create_on_null_returns_null()
        {
            ArraySeq a = ArraySeq.create(null);
            
            Expect(a, Null);
        }

        [Test]
        public void Create_on_array_has_no_meta()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array);

            Expect(a.meta(), Null);
        }

        [Test]
        public void Create_on_array_and_index_creates()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array,0);

            Expect(a, Not.Null);
        }

        [Test]
        public void Create_on_array_and_index_with_high_index_returns_null()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array, 10);

            Expect(a, Null);
        }

        [Test]
        public void Create_on_array_and_index_has_no_meta()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array,0);

            Expect(a.meta(), Null);
        }  

        #endregion

        #region IPersistentCollection tests

        [Test]
        public void ArraySeq_has_correct_count_1()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array);

            Expect(a.count(), EqualTo(3));
        }

        [Test]
        public void ArraySeq_has_correct_count_2()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array,1);

            Expect(a.count(), EqualTo(2));
        }

        #endregion

        #region ISeq tests

        [Test]
        public void First_yields_first_element_1()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array);

            Expect(a.first(), EqualTo(1));
        }

        public void First_yields_first_element_2()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array,1);

            Expect(a.first(), EqualTo(2));
        }


        public void Rest_has_correct_count_and_first_element_1()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array);
            ISeq s = a.rest();

            Expect(s.first(), EqualTo(2));
            Expect(s.count(), EqualTo(2));
        }

        public void Rest_has_correct_count_and_first_element_2()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array,1);
            ISeq s = a.rest();

            Expect(s.first(), EqualTo(3));
            Expect(s.count(), EqualTo(1));
        }

        public void Rest_eventually_yields_null()
        {
            object[] array = new object[] { 1, 2, 3 };
            ArraySeq a = ArraySeq.create(array);

            Expect(a.first(), EqualTo(1));
            Expect(a.rest().first(), EqualTo(2));
            Expect(a.rest().rest().first(), EqualTo(3));
            Expect(a.rest().rest().rest(), Null);
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

            object[] array = new object[] { 2, 3, 4 };
            ArraySeq a = ArraySeq.create(array);
            object ret = a.reduce(fn);

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

            object[] array = new object[] { 2, 3, 4 };
            ArraySeq a = ArraySeq.create(array);
            object ret = a.reduce(fn, 20);

            Expect(ret, EqualTo(7));

            mocks.VerifyAll();
        }
        #endregion

    }

    [TestFixture]
    public class ArraySeq_IObj_Tests : IObjTests
    {
        [SetUp]
        public void Setup()
        {
            object[] array = new object[] { 1, 2, 3 };
            _objWithNullMeta = _obj = ArraySeq.create(array, 0);
            _expectedType = typeof(ArraySeq);
        }
    }
}
