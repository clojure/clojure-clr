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
    public class StringSeqTests : AssertionHelper
    {

        #region C-tor tests

        [Test]
        public void Create_on_empty_string_yields_null()
        {
            StringSeq s = StringSeq.create(String.Empty);

            Expect(s, Null);
        }

        [Test]
        public void Create_on_nonempty_string_yields_a_StringSeq()
        {
            StringSeq s = StringSeq.create("abcde");

            Expect(s, Not.Null);
        }

        #endregion

        #region IPersistentCollection tests

        [Test]
        public void Count_is_string_length()
        {
            StringSeq s = StringSeq.create("abcde");
            
            Expect(s.count(),EqualTo(5));
        }

        #endregion

        #region ISeq tests

        [Test]
        public void First_yields_first_character()
        {
            StringSeq s = StringSeq.create("abcde");

            Expect(s.first(), EqualTo('a'));
        }

        [Test]
        public void Rest_yields_stringSeq_with_correct_count()
        {
            StringSeq s = StringSeq.create("abcde");
            ISeq r = s.rest();

            Expect(r, TypeOf(typeof(StringSeq)));
            Expect(r.count(), EqualTo(4));
            Expect(r.first(), EqualTo('b'));
        }

        [Test]
        public void Repeated_rest_yields_null_eventually()
        {
            StringSeq s = StringSeq.create("abc");
            Expect(s.rest().rest().rest(), Null);
        }

        #endregion

        #region IndexedSeq tests

        [Test]
        public void Initial_index_is_zero()
        {
            StringSeq s = StringSeq.create("abc");

            Expect(s.index(), EqualTo(0));
        }

        [Test]
        public void Index_of_rest_is_one()
        {
            StringSeq s = StringSeq.create("abc");
            IndexedSeq i = (IndexedSeq)s.rest();

            Expect(i.index(), EqualTo(1));
        }

        #endregion

    }


    [TestFixture]
    public class StringSeq_IObj_Tests : IObjTests
    {
        MockRepository _mocks;

        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            IPersistentMap meta = _mocks.StrictMock<IPersistentMap>();
            _mocks.ReplayAll();

            StringSeq s = StringSeq.create("abcde");


            _objWithNullMeta = (IObj)s;
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(StringSeq);
        }

        [TearDown]
        public void Teardown()
        {
            _mocks.VerifyAll();
        }

    }

}
