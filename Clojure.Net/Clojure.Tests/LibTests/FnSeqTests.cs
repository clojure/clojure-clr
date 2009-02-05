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
    public class FnSeqTests : AssertionHelper
    {

        #region C-tor tests

        // Couldn't think of anything except to make sure it doesn't throw an exception.
        [Test]
        public void CtorWorks()
        {
            FnSeq fs = new FnSeq("abc",null);

            Expect(fs, Not.Null);
        }

        #endregion

        #region IObj tests

        [Test]
        public void BasicFnSeqHasNoMeta()
        {
            FnSeq fs = new FnSeq("abc", null);

            Expect(fs.meta(), Null);
        }


        [Test]
        public void WithMetaAddsMeta()
        {
            MockRepository mocks = new MockRepository();
            IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(null)).Return(null);
            mocks.ReplayAll();

            FnSeq fs = new FnSeq("abc", fn);
            IObj obj = fs.withMeta(meta);

            Expect(obj.meta(), EqualTo(meta));
            mocks.VerifyAll();
        }

        [Test]
        public void WithMetaDuplicateReturnsSame()
        {
            MockRepository mocks = new MockRepository();
            IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(null)).Return(null);
            mocks.ReplayAll();

            FnSeq fs = new FnSeq("abc", fn);
            IObj obj1 = fs.withMeta(meta);
            IObj obj2 = obj1.withMeta(meta);

            Expect(obj2, SameAs(obj1));
            mocks.VerifyAll();
        }

        #endregion

        #region ISeq tests

        [Test]
        public void FirstReturnsCachedValue()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            mocks.ReplayAll();

            FnSeq fs = new FnSeq("abc", fn);

            Expect(fs.first(), EqualTo("abc"));
            mocks.VerifyAll();
        }


        [Test]
        public void RestCallsFnAndReturnsValue()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(null)).Return(null);
            mocks.ReplayAll();

            FnSeq fs = new FnSeq("abc", fn);

            Expect(fs.rest(), Null);
            mocks.VerifyAll();
        }

        public void RestCachesResult()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(null)).Return(null);
            mocks.ReplayAll();

            FnSeq fs = new FnSeq("abc", fn);

            Expect(fs.rest(), Null);
            Expect(fs.rest(), Null);

            mocks.VerifyAll();
        }

        #endregion

    }

    [TestFixture]
    public class FnSeq_IObj_Tests : IObjTests
    {
        MockRepository _mocks;

        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            IFn fn = _mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke(null)).Return(null);
            _mocks.ReplayAll();

            FnSeq fs = new FnSeq("abc", fn);

            _obj = _objWithNullMeta = fs;
            _expectedType = typeof(FnSeq);
        }

        [TearDown]
        public void TearDown()
        {
            _mocks.ReplayAll();
        }

    }
}
