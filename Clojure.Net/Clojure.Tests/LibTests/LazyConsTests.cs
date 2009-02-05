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
    public class LazyConsTests : AssertionHelper
    {

        #region C-tor tests

        // Couldn't think of anything except to make sure it doesn't throw an exception.
        [Test]
        public void CtorWorks()
        {
            LazyCons lc = new LazyCons(null);

            Expect(lc, Not.Null);
        }

        #endregion

        #region ISeq tests

        [Test]
        public void FirstCallsFnOnceAndReturnsFnValue()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke()).Return(10);
            mocks.ReplayAll();

            LazyCons lc = new LazyCons(fn);

            Expect(lc.first(), EqualTo(10));
            mocks.VerifyAll();
        }

        [Test]
        public void FirstCachesResult()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke()).Return(10);
            mocks.ReplayAll();

            LazyCons lc = new LazyCons(fn);

            Expect(lc.first(), EqualTo(10));
            Expect(lc.first(), EqualTo(10));
            mocks.VerifyAll();
        }

        [Test]
        public void RestCallsFnTwiceAndReturnsSecondValue()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke()).Return(10);
            RMExpect.Call(fn.invoke(null)).Return(null);
            mocks.ReplayAll();

            LazyCons lc = new LazyCons(fn);

            Expect(lc.rest(), Null);
            mocks.VerifyAll();
        }

        public void RestCachesResult()
        {
            MockRepository mocks = new MockRepository();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke()).Return(10);
            RMExpect.Call(fn.invoke(null)).Return(null);
            mocks.ReplayAll();

            LazyCons lc = new LazyCons(fn);

            Expect(lc.rest(), Null);
            Expect(lc.rest(), Null);

            mocks.VerifyAll();
        }

        #endregion

    }

    [TestFixture]
    public class LazyCons_IObj_Tests : IObjTests
    {
        [SetUp]
        public void Setup()
        {
            MockRepository mocks = new MockRepository();
            IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
            IFn fn = mocks.StrictMock<IFn>();
            RMExpect.Call(fn.invoke()).Return(10);
            RMExpect.Call(fn.invoke(null)).Return(null);
            mocks.ReplayAll();

            _objWithNullMeta = new LazyCons(fn);
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(LazyCons);

            mocks.VerifyAll();
        }
    }
}
