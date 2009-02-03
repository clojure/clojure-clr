using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using Rhino.Mocks;

using clojure.lang;


namespace DataTests
{
    [TestFixture]
    public class ObjTests : IObjTests
    {

        class MockObj : Obj
        {
            public MockObj()
            {
            }

            public MockObj(IPersistentMap meta)
                : base(meta)
            {
            }

            public override IObj withMeta(IPersistentMap meta)
            {
                return meta == _meta
                    ? this
                    : new MockObj(meta);
            }
        }

        MockRepository _mocks;

        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            IPersistentMap meta = _mocks.StrictMock<IPersistentMap>();
            _mocks.ReplayAll();

            _objWithNullMeta = new MockObj();
            _obj = new MockObj(meta);
            _expectedType = typeof(MockObj);
        }
        
        [TearDown]
        public void Teardown()
        {
            _mocks.VerifyAll();
        }
    }
}

