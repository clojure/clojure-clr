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
using System.Collections;

using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using NExpect;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class ConsTests
    {
        #region C-tor tests

        [Test]
        public void NoMetaCtorHasNoMeta()
        {
            Cons c = new Cons("abc",null);
            Expect(c.meta()).To.Be.Null();
        }

        [Test]
        public void MetaCtorHasMeta()
        {
            IPersistentMap meta = new DummyMeta();
            Cons c = new Cons(meta, "abc", null);
            Expect(Object.ReferenceEquals(c.meta(), meta));
        }

        #endregion

        #region IPersistentCollection tests

        [Test]
        public void CountOfOneItem()
        {
            Cons c = new Cons("abc", null);

            Expect(c.count()).To.Equal(1);
        }

        [Test]
        public void CountOfTwoItems()
        {
            Cons c1 = new Cons("abc", null);
            Cons c2 = new Cons("def", c1);

            Expect(c2.count()).To.Equal(2);
        }

        [Test]
        public void SeqReturnsSelf()
        {
            Cons c1 = new Cons("abc", null);

            Expect(Object.ReferenceEquals(c1.seq(), c1));
        }

        [Test]
        public void EmptyIsNull()
        {
            // Test of ASeq
            Cons c = new Cons("abc", null);
            IPersistentCollection empty = c.empty();
            Expect(Object.ReferenceEquals(empty, PersistentList.EMPTY));
        }

        [Test]
        public void IPC_Cons_works()
        {
            Cons c1 = new Cons("abc", null);
            IPersistentCollection ipc1 = c1 as IPersistentCollection;

            IPersistentCollection ipc2 = ipc1.cons("def");
            ISeq s = ipc2.seq();

            Expect(s.first()).To.Equal("def");
            Expect(Object.ReferenceEquals(s.next(), c1));
        }

        #endregion

        #region ASeq tests
        
        // Some aspects of ASeq have been tested above.  
        // Here are the remaining bits

        private Cons CreateComplicatedCons()
        {
            Cons c1 = new Cons(1, null);
            Cons c2 = new Cons(2, c1);
            Cons c3 = new Cons("abc", null);
            Cons c4 = new Cons(c3, c2);
            Cons c5 = new Cons("def", c4);

            return c5;
        }

        [Test]
        public void EqualsDoesValueComparison()
        {
            Cons a = CreateComplicatedCons();
            Cons b = CreateComplicatedCons();

            Expect(a.Equals(b));
            Expect(a).To.Equal(b);
        }

        [Test]
        public void GetHashCodeComputesOnValue()
        {
            Cons a = CreateComplicatedCons();
            Cons b = CreateComplicatedCons();

            Expect(a.GetHashCode()).To.Equal(b.GetHashCode());
        }


        #endregion

        #region ASeq.ICollection tests

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ASeqICollCopyToFailsOnNullArray()
        {
            ICollection ic = new Cons(1, null);
            ic.CopyTo(null, 0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ASeqICollCopyToFailsOnInsufficientSpace()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3,null)));
            Array arr = new object[2];
            ic.CopyTo(arr, 0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ASeqICollCopyToFailsOnInsufficientSpace2()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            Array arr = new object[10];
            ic.CopyTo(arr, 8);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ASeqICollCopyToFailsOnMultidimArray()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            Array arr = Array.CreateInstance(typeof(int), 4, 4);
            ic.CopyTo(arr, 0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ASeqICollCopyToFailsOnNegativeIndex()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            Array arr = new object[10];
            ic.CopyTo(arr, -1);
        }

        [Test]
        public void ASeqICollCopyToCopiesToIndex0()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            int[] arr = new int[4];
            ic.CopyTo(arr, 0);

            Expect(arr[0]).To.Equal(1);
            Expect(arr[1]).To.Equal(2);
            Expect(arr[2]).To.Equal(3);
            Expect(arr[3]).To.Equal(0);
        }

        [Test]
        public void ASeqICollCopyToCopiesToIndexPositive()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            int[] arr = new int[4];
            ic.CopyTo(arr, 1);

            Expect(arr[0]).To.Equal(0);
            Expect(arr[1]).To.Equal(1);
            Expect(arr[2]).To.Equal(2);
            Expect(arr[3]).To.Equal(3);
        }

        [Test]
        public void ASeqICollCountWorks()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));

            Expect(ic.Count).To.Equal(3);
        }

        [Test]
        public void ASeqICollIsSynchronized()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            Expect(ic.IsSynchronized);
        }

        [Test]
        public void ASeqICollImplementsSyncRoot()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            Object o = ic.SyncRoot;
            Expect(Object.ReferenceEquals(o, ic));
        }

        [Test]
        public void ASeqIEnumWorks()
        {
            ICollection ic = new Cons(1, new Cons(2, new Cons(3, null)));
            IEnumerator e = ic.GetEnumerator();

            Expect(e.MoveNext());
            Expect(e.Current).To.Equal(1);
            Expect(e.MoveNext());
            Expect(e.Current).To.Equal(2);
            Expect(e.MoveNext());
            Expect(e.Current).To.Equal(3);
            Expect(e.MoveNext()).To.Be.False(); 
        }


        #endregion

        #region SeqIterator tests

        [Test]
        public void SeqIteratorOnEmptySeqGoesNowhere()
        {
            SeqEnumerator s = new SeqEnumerator(null);

            Expect(s.MoveNext()).To.Be.False();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SeqIteratorOnEmptyHasNoCurrent()
        {
            SeqEnumerator s = new SeqEnumerator(null);
            
            // Have to assign to access
            object o = s.Current;

        }

        [Test]
        public void SeqIteratorIterates()
        {
            Cons c = new Cons(1, new Cons(2, null));
            SeqEnumerator s = new SeqEnumerator(c);

            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(1);
            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(2);
            Expect(s.Current).To.Equal(2);
            Expect(s.MoveNext()).To.Be.False();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SeqIteratorMovedToEmptyHasNoCurrent()
        {
            Cons c = new Cons(1, new Cons(2, null));
            SeqEnumerator s = new SeqEnumerator(c);
            
            s.MoveNext();
            s.MoveNext();
            s.MoveNext();
            
            // Have to assign to access
            object o = s.Current;
        }


        [ExpectedException(typeof(InvalidOperationException))]
        public void SeqIteratorResetAtBeginningWorks()
        {
            Cons c = new Cons(1, new Cons(2, null));
            SeqEnumerator s = new SeqEnumerator(c);

            s.Reset();

            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(1);
            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(2);
            Expect(s.Current).To.Equal(2);
            Expect(s.MoveNext()).To.Be.False();
        }

        [Test]
        public void SeqIteratorResetAtFirstWorks()
        {
            Cons c = new Cons(1, new Cons(2, null));
            SeqEnumerator s = new SeqEnumerator(c);

            s.MoveNext();
            s.Reset();

            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(1);
            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(2);
            Expect(s.Current).To.Equal(2);
            Expect(s.MoveNext()).To.Be.False();
        }

        [Test]
        public void SeqIteratorResetInMiddleWorks()
        {
            Cons c = new Cons(1, new Cons(2, null));
            SeqEnumerator s = new SeqEnumerator(c);

            s.MoveNext();
            s.MoveNext();
            s.Reset();

            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(1);
            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(2);
            Expect(s.Current).To.Equal(2);
            Expect(s.MoveNext()).To.Be.False();
        }

        [Test]
        public void SeqIteratorResetAtEndWorks()
        {
            Cons c = new Cons(1, new Cons(2, null));
            SeqEnumerator s = new SeqEnumerator(c);

            s.MoveNext();
            s.MoveNext();
            s.MoveNext();
            s.Reset();

            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(1);
            Expect(s.MoveNext());
            Expect(s.Current).To.Equal(2);
            Expect(s.Current).To.Equal(2);
            Expect(s.MoveNext()).To.Be.False();
        }

        #endregion
    }

    [TestFixture]
    public class Cons_ISeq_Tests : ISeqTestHelper
    {
        #region ISeq tests

        [Test]
        public void Cons_ISeq_has_correct_values()
        {
            Cons c1 = new Cons("def", null);
            Cons c2 = new Cons("abc", c1);

            VerifyISeqContents(c2, new object[] { "abc", "def" });
        }

        [Test]
        public void Cons_ISeq_with_meta_has_correct_values()
        {
            IPersistentMap meta = new DummyMeta();

            Cons c1 = new Cons("def", null);
            Cons c2 = new Cons(meta,"abc", c1);

            VerifyISeqContents(c2, new object[] { "abc", "def" });
        }

        [Test]
        public void Cons_ISeq_conses()
        {
            Cons c1 = new Cons("def", null);
            Cons c2 = new Cons("abc", c1);

            VerifyISeqCons(c2, "ghi", new object[] { "abc", "def" });
        }

        #endregion
    }


    [TestFixture]
    public class Cons_IMeta_Tests : IObjTests
    {
        [SetUp]
        public void Setup()
        {
            IPersistentMap meta = new DummyMeta();

            Cons c1 = new Cons("abc", null);
            Cons c2 = new Cons(meta,"def", c1);
            _obj = c2;
            _objWithNullMeta = c1;
            _expectedType = typeof(Cons);
        }
    }
}
