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


using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using NExpect;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class ArraySeqTests
    {

        #region C-tor tests

        [Test]
        public void Create_on_nothing_returns_null()
        {
            IArraySeq a = ArraySeq.create();
            
            Expect(a).To.Be.Null();
        }

        [Test]
        public void Create_on_array_creates()
        {
            object[] array = new object[] { 1, 2, 3 };
            IArraySeq a = ArraySeq.create(array);

            Expect(a).Not.To.Be.Null();
        }

        [Test]
        public void Create_on_null_returns_null()
        {
            IArraySeq a = ArraySeq.create(null);
            
            Expect(a).To.Be.Null();
        }

        [Test]
        public void Create_on_array_has_no_meta()
        {
            object[] array = new object[] { 1, 2, 3 };
            IArraySeq a = ArraySeq.create(array);

            Expect(a.meta()).To.Be.Null();
        }

        [Test]
        public void Create_on_array_and_index_creates()
        {
            object[] array = new object[] { 1, 2, 3 };
            IArraySeq a = ArraySeq.create(array,0);

            Expect(a).Not.To.Be.Null();
        }

        [Test]
        public void Create_on_array_and_index_with_high_index_returns_null()
        {
            object[] array = new object[] { 1, 2, 3 };
            IArraySeq a = ArraySeq.create(array, 10);

            Expect(a).To.Be.Null();
        }

        [Test]
        public void Create_on_array_and_index_has_no_meta()
        {
            object[] array = new object[] { 1, 2, 3 };
            IArraySeq a = ArraySeq.create(array,0);

            Expect(a.meta()).To.Be.Null();
        }  

        #endregion

        #region IPersistentCollection tests

        [Test]
        public void ArraySeq_has_correct_count_1()
        {
            object[] array = new object[] { 1, 2, 3 };
            IArraySeq a = ArraySeq.create(array);

            Expect(((Counted)a).count()).To.Equal(3);
        }

        [Test]
        public void ArraySeq_has_correct_count_2()
        {
            object[] array = new object[] { 1, 2, 3 };
            IArraySeq a = ArraySeq.create(array,1);

            Expect(((Counted)a).count()).To.Equal(2);
        }

        #endregion

        #region IReduce tests

        [Test]
        public void ReduceWithNoStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            object[] array = new object[] { 2, 3, 4 };
            IArraySeq a = ArraySeq.create(array);
            object ret = a.reduce(fn);

            Expect(ret).To.Be.An.Instance.Of<long>();
            Expect((long)ret).To.Equal(9);
        }

        [Test]
        public void ReduceWithStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            object[] array = new object[] { 2, 3, 4 };
            IArraySeq a = ArraySeq.create(array);
            object ret = a.reduce(fn, 20);

            Expect(ret).To.Be.An.Instance.Of<long>();
            Expect((long)ret).To.Equal(29);
        }
        #endregion
    }

    [TestFixture]
    public class ArraySeq_ISeq_Tests : ISeqTestHelper
    {
        #region setup

        object[] _array0;
        object[] _array1;
        IArraySeq _a0;
        IArraySeq _a1;

        [SetUp]
        public void Setup()
        {
            _array0 = new object[] { 1, 2, 3 };
            _array1 = new object[] {    2, 3 };
            _a0 = ArraySeq.create(_array0);
            _a1 = ArraySeq.create(_array0, 1);
        }

        #endregion

        #region ISeq tests

        [Test]
        public void ArraySeq_ISeq_std_ctor_has_correct_elements()
        {
            VerifyISeqContents(_a0, _array0);
        }

        [Test]
        public void ArraySeq_ISeq_index_ctor_has_correct_elements()
        {
            VerifyISeqContents(_a1, _array1 );
        }

        [Test]
        public void ArraySeq_ISeq_std_ctor_conses()
        {
            VerifyISeqCons(_a0, 4, _array0);
        }

        [Test]
        public void ArraySeq_ISeq_index_ctor_conses()
        {
            VerifyISeqCons(_a1, 4, _array1);
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
            _expectedType = typeof(ArraySeq_object);
            _testNoChange = false;
        }
    }
}
