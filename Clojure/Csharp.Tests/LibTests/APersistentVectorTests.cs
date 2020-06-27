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

    // TODO: Add tests for APersistentVector.SubVector

    [TestFixture]
    public class APersistentVectorTests
    {
        #region Concrete persistent vector

        // Usually, we test the abstract classes via the simplest concrete class that derives from it.
        // For APersistentVector, all the concrete classes are fairly complicated.
        // Hence we create a test concrete implementation class.
        // This class has no guarantees of persistence/immutability, thread-safety,
        //   or much of anything else, certainly not efficiency.
        // We determined the methods to override by trying to compile the class with no methods.
        // Thus, we have implemented only the absolute minimum.
        // We will write tests for these methods, too.
        // This class just has an underlying  List<int> to hold values.
        class CPV : APersistentVector
        {
            object[] _values;

            public CPV(object[] values)
            {
                _values = values;
            }


            public CPV(IPersistentMap meta, object[] values)
            {
                _values = values;
            }

            //public override object applyTo(ISeq arglist)
            //{
            //    throw new NotImplementedException();
            //}

            public override IPersistentStack pop()
            {
                if (_values.Length == 0)
                    throw new InvalidOperationException("Can't pop a null stack.");

                object[] newArr = new object[_values.Length - 1];
                Array.Copy(_values, newArr, _values.Length - 1);

                return new CPV( newArr);
            }

            public override IPersistentVector cons(object o)
            {
                object[] newArr = new object[_values.Length + 1];
                _values.CopyTo(newArr, 0);
                newArr[_values.Length] = o;
                return new CPV(newArr);
            }

            public override IPersistentVector assocN(int i, object val)
            {
                if ( 0 <= i && i < _values.Length )
                {
                    object[] newArr = new object[_values.Length];
                    _values.CopyTo(newArr, 0);
                    newArr[i] = val;
                    return new CPV(newArr);
                }
                if ( i == _values.Length )
                    return cons(val);
                throw new IndexOutOfRangeException();
            }

            public override object nth(int i)
            {
                return _values[i];
            }

            public override int length()
            {
                return _values.Length;
            }

            private static readonly CPV EMPTY = new CPV(new object[0]);
            public override IPersistentCollection empty()
            {
                return EMPTY;
            }

            public override int count()
            {
                return _values.Length;
            }
        }

        #endregion

        #region C-tor tests

        //[Test]
        //public void NoMetaCtorHasNoMeta()
        //{
        //    CPV v = new CPV(new object[] { 1, 2, 3 });

        //    Expect(v.meta(),Null);
        //}

        //[Test]
        //public void MetaCtorHasMeta()
        //{
        //    MockRepository mocks = new MockRepository();
        //    IPersistentMap meta = mocks.StrictMock<IPersistentMap>();
        //    mocks.ReplayAll();

        //    CPV v = new CPV(meta,new object[] { 1, 2, 3 });

        //    Expect(v.meta(), meta));
        //    mocks.VerifyAll();
        //}

        #endregion

        #region Object tests

        [Test]
        public void ToStringMentionsTheCount()
        {
            CPV v = new CPV(new object[] { 1, 2, 3 });
            string str = v.ToString();

            Expect(str.Contains("3"));
        }

        [Test]
        public void HashCodeRepeats()
        {
            CPV v = new CPV(new object[] { 1, 2, 3 });

            Expect(v.GetHashCode()).To.Equal(v.GetHashCode());
        }

        [Test]
        public void HashCodeDependsOnItems()
        {
            CPV v1 = new CPV(new object[] { 1, 2, 3 });
            CPV v2 = new CPV(new object[] { 1, 2, 4 });

            Expect(v1.GetHashCode()).To.Not.Equal(v2.GetHashCode());
        }

        [Test]
        public void EqualsOnNonPersistentVectorIsFalse()
        {
            CPV v1 = new CPV(new object[] { 1, 2, 3 });

            Expect(v1.equiv(7)).To.Be.False();
        }

        [Test]
        public void EqualsOnPersistentVectorWithDifferentItemsIsFalse()
        {
            CPV v1 = new CPV(new object[] { 1, 2, 3 });
            CPV v2 = new CPV(new object[] { 1, 2, 4 });
            CPV v3 = new CPV(new object[] { 1, 2 });
            CPV v4 = new CPV(new object[] { 1, 2, 3, 4 });

            Expect(v1.equiv(v2)).To.Be.False();
            Expect(v1.equiv(v3)).To.Be.False();
            Expect(v1.equiv(v4)).To.Be.False();
        }

        [Test]
        public void EqualsOnPersistentVectorWithSameItemsIsTrue()
        {
            CPV v1 = new CPV(new object[] { 1, 2, 3 });
            CPV v2 = new CPV(new object[] { 1, 2, 3 });
            CPV v3 = new CPV(new object[] { 1 });
            CPV v4 = new CPV(new object[] { 1 });
            CPV v5 = new CPV(new object[] { });
            CPV v6 = new CPV(new object[] { });

            Expect(v1.equiv(v2));
            Expect(v3.equiv(v4));
            Expect(v5.equiv(v6));
        }

        [Test]
        public void EqualsOnSimilarISeqWorks()
        {
            CPV v1 = new CPV(new object[] { 'a', 'b', 'c' });
            StringSeq s1 = StringSeq.create("abc");

            Expect(v1.equiv(s1));
        }

        [Test]
        public void EqualsOnDissimilarISeqFails()
        {
            CPV v1 = new CPV(new object[] { 'a', 'b', 'c' });
            StringSeq s1 = StringSeq.create("ab");
            StringSeq s2 = StringSeq.create("abd");
            StringSeq s3 = StringSeq.create("abcd");

            Expect(v1.equiv(s1)).To.Be.False();
            Expect(v1.equiv(s2)).To.Be.False();
            Expect(v1.equiv(s3)).To.Be.False();
        }


        #endregion

        #region IFn tests

        [Test]
        public void InvokeCallsNth()
        {
            CPV v = new CPV(new object[] { 5, 6, 7 });

            Expect(v.invoke(0)).To.Equal(5);
            Expect(v.invoke(1)).To.Equal(6);
            Expect(v.invoke(2)).To.Equal(7);
            Expect(v.invoke("1")).To.Equal(6);
            Expect(v.invoke(1.0)).To.Equal(6);
            Expect(v.invoke(1.2)).To.Equal(6);
            Expect(v.invoke(1.8)).To.Equal(6); // Rounds or not-- should it?
            Expect(v.invoke(1.4M)).To.Equal(6);
        }


        #endregion

        #region IPersistentCollection tests

        [Test]
        public void SeqOnCount0YieldsNull()
        {
            CPV v = new CPV(new object[0]);

            Expect(v.seq()).To.Be.Null();
        }

        [Test]
        public void SeqOnPositiveCountYieldsNotNull()
        {
            CPV v = new CPV(new object[]{ 1,2,3});

            Expect(v.seq()).Not.To.Be.Null();
        }

        [Test]
        public void SeqOnPositiveCountYieldsValidSequence()
        {
            CPV v = new CPV(new object[] { 1, 2, 3 });
            ISeq s = v.seq();

            Expect(s.first()).To.Equal(1);
            Expect(s.next().first()).To.Equal(2);
            Expect(s.next().next().first()).To.Equal(3);
            Expect(s.next().next().next()).To.Be.Null();
        }

        [Test]
        public void Explicit_IPersistentCollection_cons_works()
        {
            CPV v = new CPV(new object[] { 1, 2 });
            IPersistentCollection c = v as IPersistentCollection;

            Expect(c).Not.To.Be.Null();

            IPersistentCollection c2 = c.cons(3);
            Expect(c2.count()).To.Equal(3);

            ISeq s2 = c2.seq();

            Expect(s2.first()).To.Equal(1);
            Expect(s2.next().first()).To.Equal(2);
            Expect(s2.next().next().first()).To.Equal(3);
            Expect(s2.next().next().next()).To.Be.Null();
        }

        #endregion

        #region Reversible tests

        [Test]
        public void RseqOnCount0YieldsNull()
        {
            CPV v = new CPV(new object[0]);

            Expect(v.rseq()).To.Be.Null();
        }

        [Test]
        public void RSeqOnPositiveCountYieldsNotNull()
        {
            CPV v = new CPV(new object[] { 1, 2, 3 });

            Expect(v.rseq()).Not.To.Be.Null();
        }

        [Test]
        public void RseqOnPositiveCountYieldsValidSequence()
        {
            CPV v = new CPV(new object[] { 1, 2, 3 });
            ISeq s = v.rseq();

            Expect(s.first()).To.Equal(3);
            Expect(s.next().first()).To.Equal(2);
            Expect(s.next().next().first()).To.Equal(1);
            Expect(s.next().next().next()).To.Be.Null();
        }


        #endregion

        #region Associative tests

        [Test]
        public void ContainsKeyOnNonNumericIsFalse()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            Expect(v.containsKey("a")).To.Be.False();
        }

        [Test]
        public void ContainsKeyOnIndexInRangeIsTrue()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            Expect(v.containsKey(1.2));
        }


        [Test]
        public void ContainsKeyOnIndexOutOfRangeIsFalse()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            Expect(v.containsKey(5)).To.Be.False();
        }

        [Test]
        public void EntryAtOnNonNumericReturnsNull()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            IMapEntry me = v.entryAt("a");

            Expect(me).To.Be.Null();
        }

        [Test]
        public void EntryAtOnIndexInRangeReturnsEntry()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            IMapEntry me = v.entryAt(1);

            Expect(me.key()).To.Equal(1);
            Expect(me.val()).To.Equal(5);
        }


        [Test]
        public void EntryAtOnIndexOutOfRangeReturnsNull()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            IMapEntry me = v.entryAt(5);

            Expect(me).To.Be.Null();
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AssocWithNonNumericKeyThrowsException()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });
            v.assoc("a", 7);
        }

        [Test]
        public void AssocWithNumericKeyInRangeChangesValue()
        {
            //This just checks that APersistentVector.assoc calls CPV.assocN
            CPV v = new CPV(new object[] { 4, 5, 6 });
            Associative a = v.assoc(1, 10);

            Expect(a.valAt(0)).To.Equal(4);
            Expect(a.valAt(1)).To.Equal(10);
            Expect(a.valAt(2)).To.Equal(6);
            Expect(a.count()).To.Equal(3);
        }

        [Test]
        public void AssocWithNumericKeyOnePastEndAddValue()
        {
            //This just checks that APersistentVector.assoc calls CPV.assocN
            CPV v = new CPV(new object[] { 4, 5, 6 });
            Associative a = v.assoc(3, 10);

            Expect(a.valAt(0)).To.Equal(4);
            Expect(a.valAt(1)).To.Equal(5);
            Expect(a.valAt(2)).To.Equal(6);
            Expect(a.valAt(3)).To.Equal(10);
            Expect(a.count()).To.Equal(4);
        }

        [Test]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void AssocWithNumericKeyOutOfRangeHighThrowsException()
        {
            //This just checks that APersistentVector.assoc calls CPV.assocN
            CPV v = new CPV(new object[] { 4, 5, 6 });
            v.assoc(4, 10);
        }

        [Test]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void AssocWithNumericKeyOutOfRangeLowThrowsException()
        {
            //This just checks that APersistentVector.assoc calls CPV.assocN
            CPV v = new CPV(new object[] { 4, 5, 6 });
            v.assoc(-1, 10);
        }

        [Test]
        public void ValAtOnNonNumericReturnsDefault()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            object val1 = v.valAt("a");
            object val2 = v.valAt("a", "abc");

            Expect(val1).To.Be.Null();
            Expect(val2).To.Equal("abc");
        }

        [Test]
        public void ValAtOnIndexInRangeReturnsEntry()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            object val1 = v.valAt(1);
            object val2 = v.valAt(1, "abc");

            Expect(val1).To.Equal(5);
            Expect(val2).To.Equal(5);
        }


        [Test]
        public void ValAtOnIndexOutOfRangeReturnsDefault()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });

            object val1 = v.valAt(4);
            object val2 = v.valAt(4, "abc");

            Expect(val1).To.Be.Null();
            Expect(val2).To.Equal("abc");
        }



        #endregion

        #region IPersistentStack tests

        [Test]
        public void PeekOnCount0ReturnsNull()
        {
            CPV v = new CPV(new object[] {});

            Expect(v.peek()).To.Be.Null();
        }

        [Test]
        public void PeekOnPositiveCountReturnsLastItem()
        {
            CPV v = new CPV(new object[] { 1, 2, 3 });

            Expect(v.peek()).To.Equal(3);
        }

        #endregion

        #region APersistentVector.Seq tests

        // We'll do all the tests indirectly.

        [Test]
        public void SeqFirstAndRestWork()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 }); 
            ISeq s = v.seq();

            Expect(s.first()).To.Equal(4);
            Expect(s.next().first()).To.Equal(5);
            Expect(s.next().next().first()).To.Equal(6);
            Expect(s.next().next().next()).To.Be.Null();
        }

        [Test]
        public void SeqIndexedWorks()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });
            ISeq s0 = v.seq();
            IndexedSeq i0 = s0 as IndexedSeq;

            ISeq s1 = s0.next();
            IndexedSeq i1 = s1 as IndexedSeq;

            Expect(i0.index()).To.Equal(0);
            Expect(i1.index()).To.Equal(1);
        }

        [Test]
        public void SeqCountWorks()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });
            ISeq s = v.seq();

            Expect(s.count()).To.Equal(3);
            Expect(s.next().count()).To.Equal(2);
            Expect(s.next().next().count()).To.Equal(1);
        }

        [Test]
        public void SeqWithMetaHasMeta()
        {
            IPersistentMap meta = new DummyMeta();

            CPV v = new CPV(new object[] { 4, 5, 6 });
            IObj s = (IObj)v.seq();
            IObj obj = s.withMeta(meta);

            Expect(Object.ReferenceEquals(obj.meta(),meta));
        }

        [Test]
        public void SeqReduceWithNoStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            CPV v = new CPV(new object[] { 1, 2, 3 });
            IReduce r = (IReduce)v.seq();
            object ret = r.reduce(fn);

            Expect(ret).To.Be.An.Instance.Of<long>();
            Expect((long)ret).To.Equal(6);
        }

        [Test]
        public void SeqReduceWithStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            CPV v = new CPV(new object[] { 1, 2, 3 });
            IReduce r = (IReduce)v.seq();
            object ret = r.reduce(fn, 20);

            Expect(ret).To.Be.An.Instance.Of<long>();
            Expect((long)ret).To.Equal(26);
        }

        #endregion

        #region APersistentVector.RSeq tests

        // We'll do all the tests indirectly.

        [Test]
        public void RSeqFirstAndRestWork()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });
            ISeq s = v.rseq();

            Expect(s.first()).To.Equal(6);
            Expect(s.next().first()).To.Equal(5);
            Expect(s.next().next().first()).To.Equal(4);
            Expect(s.next().next().next()).To.Be.Null();
        }

        [Test]
        public void RSeqIndexedWorks()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });
            ISeq s0 = v.rseq();
            IndexedSeq i0 = s0 as IndexedSeq;

            ISeq s1 = s0.next();
            IndexedSeq i1 = s1 as IndexedSeq;

            Expect(i0.index()).To.Equal(2);
            Expect(i1.index()).To.Equal(1);
        }

        [Test]
        public void RSeqCountWorks()
        {
            CPV v = new CPV(new object[] { 4, 5, 6 });
            ISeq s = v.rseq();

            Expect(s.count()).To.Equal(3);
            Expect(s.next().count()).To.Equal(2);
            Expect(s.next().next().count()).To.Equal(1);
        }

        [Test]
        public void RSeqWithMetaHasMeta()
        {
            IPersistentMap meta = new DummyMeta();

            CPV v = new CPV(new object[] { 4, 5, 6 });
            IObj s = (IObj)v.rseq();
            IObj obj = s.withMeta(meta);

            Expect(Object.ReferenceEquals(obj.meta(), meta));
        }

        [Test]
        public void RSeqReduceWithNoStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            CPV v = new CPV(new object[] { 1, 2, 3 });
            IReduce r = (IReduce)v.rseq();
            object ret = r.reduce(fn);

            Expect(ret).To.Be.An.Instance.Of<long>();
            Expect((long)ret).To.Equal(6);
        }

        [Test]
        public void RSeqReduceWithStartIterates()
        {
            IFn fn = DummyFn.CreateForReduce();

            CPV v = new CPV(new object[] { 1, 2, 3 });
            IReduce r = (IReduce)v.rseq();
            object ret = r.reduce(fn, 20);

            Expect(ret).To.Be.An.Instance.Of<long>();
            Expect((long)ret).To.Equal(26);
        }

        #endregion
    }
}
