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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using NUnit.Framework;

using clojure.lang;


namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class SymbolTests : AssertionHelper
    {

        #region c-tor tests

        [Test]
        public void Intern2CreatesSymbolWithNoNS()
        {
            Symbol sym = Symbol.intern(null, "abc");

            Expect(sym.Name, EqualTo("abc"));
            Expect(sym.Namespace, Null);
            Expect(sym.meta(), Null);
        }

        [Test]
        public void Intern2CreatesSymbolWithNS()
        {
            Symbol sym = Symbol.intern("def", "abc");

            Expect(sym.Name, EqualTo("abc"));
            Expect(sym.Namespace, EqualTo("def"));
            Expect(sym.meta(), Null);
        }


        [Test]
        public void Intern1CreatesSymbolWithNoNS()
        {
            Symbol sym = Symbol.intern("abc");

            Expect(sym.Name, EqualTo("abc"));
            Expect(sym.Namespace, Null);
            Expect(sym.meta(), Null);
        }

        [Test]
        public void Intern1CreatesSymbolWithNS()
        {
            Symbol sym = Symbol.intern("def/abc");

            Expect(sym.Name, EqualTo("abc"));
            Expect(sym.Namespace, EqualTo("def"));
            Expect(sym.meta(), Null);
        }

        #endregion

        #region Object overrides

        [Test]
        public void SymToStringWithNoNSIsJustName()
        {
            Symbol sym = Symbol.intern("abc");
            Expect(sym.ToString(), EqualTo("abc"));
        }

        [Test]
        public void SymToStringWithNsConcatenatesNames()
        {
            Symbol sym = Symbol.intern("def", "abc");
            Expect(sym.ToString(), EqualTo("def/abc"));
        }

        [Test]
        public void EqualsOnIdentityIsTrue()
        {
            Symbol sym = Symbol.intern("abc");
            Expect(sym.Equals(sym));
        }

        [Test]
        public void EqualsOnNonSymbolIsFalse()
        {
            Symbol sym = Symbol.intern("abc");
            Expect(sym.Equals("abc"), False);
        }

        [Test]
        public void EqualsOnDissimilarSymbolIsFalse()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("ab");
            Symbol sym3 = Symbol.intern("def", "abc");
            Symbol sym4 = Symbol.intern("de","abc");

            Expect(sym1.Equals(sym2), False);
            Expect(sym1.Equals(sym3), False);
            Expect(sym3.Equals(sym4), False);
        }

        [Test]
        public void EqualsOnSimilarSymbolIsTrue()
        {

            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("abc");
            Symbol sym3 = Symbol.intern("def", "abc");
            Symbol sym4 = Symbol.intern("def", "abc");

            Expect(sym1.Equals(sym2));
            Expect(sym3.Equals(sym4));
        }

        [Test]
        public void HashCodeDependsOnNames()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("abc");
            Symbol sym3 = Symbol.intern("def", "abc");
            Symbol sym4 = Symbol.intern("def", "abc");
            Symbol sym5 = Symbol.intern("ab");
            Symbol sym6 = Symbol.intern("de", "abc");

            Expect(sym1.GetHashCode(), EqualTo(sym2.GetHashCode()));
            Expect(sym3.GetHashCode(), EqualTo(sym4.GetHashCode()));
            Expect(sym1.GetHashCode(), Not.EqualTo(sym3.GetHashCode()));
            Expect(sym1.GetHashCode(), Not.EqualTo(sym5.GetHashCode()));
            Expect(sym3.GetHashCode(), Not.EqualTo(sym6.GetHashCode()));
        }

        #endregion

        #region Named tests

        // We've been testing these all along.

        #endregion

        #region IFn tests

        [Test]
        public void Invoke2IndexesIntoItsFirstArg()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("abc");
            Symbol sym3 = Symbol.intern("ab");

            IDictionary dict = new Hashtable();
            dict[sym1] = 7;
            dict["abc"] = 8;

            Expect(sym1.invoke(dict), EqualTo(7));
            Expect(sym2.invoke(dict), EqualTo(7));
            Expect(sym3.invoke(dict), Null);
        }

        [Test]
        public void Invoke3IndexesIntoItsFirstArg()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("abc");
            Symbol sym3 = Symbol.intern("ab");

            IDictionary dict = new Hashtable();
            dict[sym1] = 7;
            dict["abc"] = 8;

            Expect(sym1.invoke(dict,20), EqualTo(7));
            Expect(sym2.invoke(dict,20), EqualTo(7));
            Expect(sym3.invoke(dict,20), EqualTo(20));
        }

        [Test]
        [ExpectedException(typeof(ArityException))]
        public void InvokeOnNoArgsFails()
        {
            Symbol sym1 = Symbol.intern("abc");
            sym1.invoke();
        }

        [Test]
        [ExpectedException(typeof(ArityException))]
        public void InvokeOnTooManyArgsFails()
        {
            Symbol sym1 = Symbol.intern("abc");
            IDictionary dict = new Hashtable();
            dict[sym1] = 7;
            dict["abc"] = 8;

            sym1.invoke(dict,20,null);
        }
  
        #endregion

        #region IComparable tests

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void CompareToNonSymbolFails()
        {
            Symbol sym1 = Symbol.intern("abc");
            sym1.CompareTo("abc");
        }

        [Test]
        public void CompareToEqualSymbolIsZero()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("abc");

            Expect(sym1.CompareTo(sym2), EqualTo(0));
        }

        [Test]
        public void NullNSIsLessThanNonNullNS()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("a", "abc");

            Expect(sym1.CompareTo(sym2), LessThan(0));
            Expect(sym2.CompareTo(sym1), GreaterThan(0));
        }

        [Test]
        public void DissimilarNSCompareOnNS()
        {
            Symbol sym1 = Symbol.intern("a", "abc");
            Symbol sym2 = Symbol.intern("b", "abc");

            Expect(sym1.CompareTo(sym2), LessThan(0));
            Expect(sym2.CompareTo(sym1), GreaterThan(0));
        }

        #endregion

        #region Serialization test

        [Test]
        public void Serialization_preserves_keyword_uniqueness()
        {
            MemoryStream ms = new MemoryStream();

            Symbol s1 = Symbol.intern("def", "abc");
            Symbol s2 = Symbol.intern("def", "xyz");
            List<Symbol> symbols = new List<Symbol>();
            symbols.Add(s1);
            symbols.Add(s2);
            symbols.Add(s1);
            symbols.Add(s2);

            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, symbols);

            ms.Seek(0, SeekOrigin.Begin);

            List<Symbol> inSyms = (List<Symbol>)bf.Deserialize(ms);

            Expect(Object.ReferenceEquals(inSyms[0].Name, s1.Name));
            Expect(Object.ReferenceEquals(inSyms[0].Namespace, s1.Namespace));
            Expect(Object.ReferenceEquals(inSyms[1].Name, s2.Name));
            Expect(Object.ReferenceEquals(inSyms[1].Namespace, s2.Namespace));
            Expect(Object.ReferenceEquals(inSyms[2].Name, s1.Name));
            Expect(Object.ReferenceEquals(inSyms[2].Namespace, s1.Namespace));
            Expect(Object.ReferenceEquals(inSyms[3].Name, s2.Name));
            Expect(Object.ReferenceEquals(inSyms[3].Namespace, s2.Namespace));

        }

        #endregion

    }

    [TestFixture]
    public class Symbol_IObj_Tests : IObjTests
    {
        [SetUp]
        public void Setup()
        {
            IPersistentMap meta = new DummyMeta();

            Symbol sym1 = Symbol.intern("def", "abc");

            _objWithNullMeta = (IObj)sym1;
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(Symbol);
        }
    }
}
