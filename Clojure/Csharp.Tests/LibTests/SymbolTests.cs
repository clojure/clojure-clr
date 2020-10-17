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
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using NExpect;
using System.Text.Json;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class SymbolTests
    { 
        #region c-tor tests

        [Test]
        public void Intern2CreatesSymbolWithNoNS()
        {
            Symbol sym = Symbol.intern(null, "abc");

            Expect(sym.Name).To.Equal("abc");
            Expect(sym.Namespace).To.Be.Null();
            Expect(sym.meta()).To.Be.Null();
        }

        [Test]
        public void Intern2CreatesSymbolWithNS()
        {
            Symbol sym = Symbol.intern("def", "abc");

            Expect(sym.Name).To.Equal("abc");
            Expect(sym.Namespace).To.Equal("def");
            Expect(sym.meta()).To.Be.Null();
        }


        [Test]
        public void Intern1CreatesSymbolWithNoNS()
        {
            Symbol sym = Symbol.intern("abc");

            Expect(sym.Name).To.Equal("abc");
            Expect(sym.Namespace).To.Be.Null();
            Expect(sym.meta()).To.Be.Null();
        }

        [Test]
        public void Intern1CreatesSymbolWithNS()
        {
            Symbol sym = Symbol.intern("def/abc");

            Expect(sym.Name).To.Equal("abc");
            Expect(sym.Namespace).To.Equal("def");
            Expect(sym.meta()).To.Be.Null();
        }

        #endregion

        #region Object overrides

        [Test]
        public void SymToStringWithNoNSIsJustName()
        {
            Symbol sym = Symbol.intern("abc");
            Expect(sym.ToString()).To.Equal("abc");
        }

        [Test]
        public void SymToStringWithNsConcatenatesNames()
        {
            Symbol sym = Symbol.intern("def", "abc");
            Expect(sym.ToString()).To.Equal("def/abc");
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
            Expect(sym.Equals("abc")).To.Be.False();
        }

        [Test]
        public void EqualsOnDissimilarSymbolIsFalse()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("ab");
            Symbol sym3 = Symbol.intern("def", "abc");
            Symbol sym4 = Symbol.intern("de","abc");

            Expect(sym1.Equals(sym2)).To.Be.False();
            Expect(sym1.Equals(sym3)).To.Be.False();
            Expect(sym3.Equals(sym4)).To.Be.False();
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

            Expect(sym1.GetHashCode()).To.Equal(sym2.GetHashCode());
            Expect(sym3.GetHashCode()).To.Equal(sym4.GetHashCode());
            Expect(sym1.GetHashCode()).To.Not.Equal(sym3.GetHashCode());
            Expect(sym1.GetHashCode()).To.Not.Equal(sym5.GetHashCode());
            Expect(sym3.GetHashCode()).To.Not.Equal(sym6.GetHashCode());
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

            Expect(sym1.invoke(dict)).To.Equal(7);
            Expect(sym2.invoke(dict)).To.Equal(7);
            Expect(sym3.invoke(dict)).To.Be.Null();
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

            Expect(sym1.invoke(dict,20)).To.Equal(7);
            Expect(sym2.invoke(dict,20)).To.Equal(7);
            Expect(sym3.invoke(dict,20)).To.Equal(20);
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

            Expect(sym1.CompareTo(sym2)).To.Equal(0);
        }

        [Test]
        public void NullNSIsLessThanNonNullNS()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("a", "abc");

            Expect(sym1.CompareTo(sym2)).To.Be.Less.Than(0);
            Expect(sym2.CompareTo(sym1)).To.Be.Greater.Than(0);
        }

        [Test]
        public void DissimilarNSCompareOnNS()
        {
            Symbol sym1 = Symbol.intern("a", "abc");
            Symbol sym2 = Symbol.intern("b", "abc");

            Expect(sym1.CompareTo(sym2)).To.Be.Less.Than(0);
            Expect(sym2.CompareTo(sym1)).To.Be.Greater.Than(0);
        }

        #endregion

        #region Serialization test

        //[Test]
        //public void Serialization_preserves_keyword_uniqueness()
        //{

        //    // With BinaryFormatter deprecated, we are advised to switch to JsonSerializer or some other methd.
        //    // This would require a default constructor.
        //    // TODO:  Add default ctor to Symbol so we can use the serializer.


        //    Symbol s1 = Symbol.intern("def", "abc");
        //    Symbol s2 = Symbol.intern("def", "xyz");
        //    List<Symbol> symbols = new List<Symbol>();
        //    symbols.Add(s1);
        //    symbols.Add(s2);
        //    symbols.Add(s1);
        //    symbols.Add(s2);

        //    string serializedData = JsonSerializer.Serialize(symbols);
        //    List<Symbol> inSyms = JsonSerializer.Deserialize<List<Symbol>>(serializedData);

        //    Expect(Object.ReferenceEquals(inSyms[0].Name, s1.Name));
        //    Expect(Object.ReferenceEquals(inSyms[0].Namespace, s1.Namespace));
        //    Expect(Object.ReferenceEquals(inSyms[1].Name, s2.Name));
        //    Expect(Object.ReferenceEquals(inSyms[1].Namespace, s2.Namespace));
        //    Expect(Object.ReferenceEquals(inSyms[2].Name, s1.Name));
        //    Expect(Object.ReferenceEquals(inSyms[2].Namespace, s1.Namespace));
        //    Expect(Object.ReferenceEquals(inSyms[3].Name, s2.Name));
        //    Expect(Object.ReferenceEquals(inSyms[3].Namespace, s2.Namespace));

        //}

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
