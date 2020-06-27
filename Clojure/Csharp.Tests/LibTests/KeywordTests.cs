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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using NUnit.Framework;
using static NExpect.Expectations;
using clojure.lang;
using NExpect;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class KeywordTests
    {

        #region c-tor tests

        [Test]
        public void InternCreatesKeywordBasedOnSymbol()
        {
            Symbol sym = Symbol.intern("def","abc");
            Keyword k1 = Keyword.intern(sym);
            Expect(k1.Name).To.Equal(sym.Name);
            Expect(k1.Namespace).To.Equal(sym.Namespace);
        }

        [Test]
        public void InternReturnsSameKeywordOnEqualSym()
        {
            Symbol sym1 = Symbol.intern("def", "abc");
            Symbol sym2 = Symbol.intern("def", "abc");
            Keyword k1 = Keyword.intern(sym1);
            Keyword k2 = Keyword.intern(sym2);

            Expect(Object.ReferenceEquals(k1, k2));
        }

        [Test]
        public void Intern2CreatesKeywordBasedOnSymbol()
        {
            Keyword k1 = Keyword.intern("def","abc");
            Expect(k1.Name).To.Equal("abc");
            Expect(k1.Namespace).To.Equal("def");
        }

        [Test]
        public void Intern2ReturnsSameKeywordOnEqualSym()
        {
            Keyword k1 = Keyword.intern("def", "abc");
            Keyword k2 = Keyword.intern("def", "abc");

            Expect(Object.ReferenceEquals(k1, k2));
        }

        #endregion

        #region object override tests

        [Test]
        public void ToStringReturnsStringNameWithColonPrepended()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("abc/def");
            Keyword k1 = Keyword.intern(sym1);
            Keyword k2 = Keyword.intern(sym2);

            Expect(k1.ToString()).To.Equal(":abc");
            Expect(k2.ToString()).To.Equal(":abc/def");
        }

        [Test]
        public void EqualOnIdentityIsTrue()
        {
            Symbol sym1 = Symbol.intern("abc");
            Keyword k1 = Keyword.intern(sym1);

            Expect(k1.Equals(k1));
        }

        [Test]
        public void EqualsOnNonKeywordIsFalse()
        {
            Symbol sym1 = Symbol.intern("abc");
            Keyword k1 = Keyword.intern(sym1);

            Expect(k1.Equals(sym1)).To.Be.False();
        }

        //[Test]
        //public void EqualsDependsOnSym()
        //{
        //    Symbol sym1 = Symbol.intern("abc");
        //    Symbol sym2 = Symbol.intern("abc");
        //    Keyword k1 = Keyword.intern(sym1);
        //    Keyword k2 = Keyword.intern(sym2);
        //    // I don't know how we ever create two keywords that will force
        //    // the code to go into the sym.equals part of the code.
        //    // At least, not through the factory methods.
        //}

        [Test]
        public void HashCodeDependsOnValue()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("abc/def");
            Keyword k1 = Keyword.intern(sym1);
            Keyword k2 = Keyword.intern(sym2);

            Expect(k1.GetHashCode()).To.Not.Equal(k2.GetHashCode());
        }


        #endregion

        #region Named tests

        public void NameAndNamespaceComeFromTheSymbol()
        {
            Symbol sym1 = Symbol.intern("def", "abc");
            Keyword k1 = Keyword.intern(sym1);
            Symbol sym2 = Symbol.intern("abc");
            Keyword k2 = Keyword.intern(sym2);
            Expect(k1.Name).To.Equal("abc");
            Expect(k1.Namespace).To.Equal("def");
            Expect(k2.Name).To.Equal("abc");
            Expect(k2.Namespace).To.Be.Null();
        }

        #endregion
        
        #region IFn Tests

        [Test]
        public void Invoke2IndexesIntoItsFirstArg()
        {
            Keyword k1 = Keyword.intern(Symbol.intern("abc"));
            Keyword k2 = Keyword.intern(Symbol.intern("ab"));

            IDictionary dict = new Hashtable
            {
                [k1] = 7,
                ["abc"] = 8
            };

            Expect(k1.invoke(dict)).To.Equal(7);
            Expect(k2.invoke(dict)).To.Be.Null();
        }

        [Test]
        public void Invoke3IndexesIntoItsFirstArg()
        {
            Keyword k1 = Keyword.intern(Symbol.intern("abc"));
            Keyword k2 = Keyword.intern(Symbol.intern("ab"));

            IDictionary dict = new Hashtable
            {
                [k1] = 7,
                ["abc"] = 8
            };

            Expect(k1.invoke(dict, 20)).To.Equal(7);
            Expect(k2.invoke(dict, 20)).To.Equal(20);
        }

        [Test]
        [ExpectedException(typeof(ArityException))]
        public void InvokeOnNoArgsFails()
        {
            Keyword k1 = Keyword.intern(Symbol.intern("abc"));
            k1.invoke();
        }

        [Test]
        [ExpectedException(typeof(ArityException))]
        public void InvokeOnTooManyArgsFails()
        {
            Keyword k1 = Keyword.intern(Symbol.intern("abc"));
            IDictionary dict = new Hashtable
            {
                [k1] = 7,
                ["abc"] = 8
            };

            k1.invoke(dict, 20, null);
        }
  
        #endregion

        #region IComparable tests

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void CompareToNonKeywordFails()
        {
            Symbol s1 = Symbol.intern("abc");
            Keyword k1 = Keyword.intern(s1);
            k1.CompareTo(s1);
        }

        [Test]
        public void CompareToEqualKeywordIsZero()
        {
            Keyword k1 = Keyword.intern(Symbol.intern("abc"));
            Keyword k2 = Keyword.intern(Symbol.intern("abc"));

            Expect(k1.CompareTo(k2)).To.Equal(0);
        }

        [Test]
        public void CompareDependsOnSymbolCompare()
        {
            Symbol sym1 = Symbol.intern("abc");
            Symbol sym2 = Symbol.intern("a", "abc");
            Symbol sym3 = Symbol.intern("b", "abc");
            Keyword k1 = Keyword.intern(sym1);
            Keyword k2 = Keyword.intern(sym2);
            Keyword k3 = Keyword.intern(sym3);

            Expect(k1.CompareTo(k2)).To.Be.Less.Than(0);
            Expect(k2.CompareTo(k1)).To.Be.Greater.Than(0);
            Expect(k1.CompareTo(k3)).To.Be.Less.Than(0);
            Expect(k3.CompareTo(k1)).To.Be.Greater.Than(0);
        }


        #endregion

        #region Serializability tests

        [Test]
        public void Serialization_preserves_keyword_uniqueness()
        {
            MemoryStream ms = new MemoryStream();

            Keyword k1 = Keyword.intern("def", "abc");
            Keyword k2 = Keyword.intern("def", "xyz");
            List<Keyword> keywords = new List<Keyword>
            {
                k1,
                k2,
                k1,
                k2
            };

            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms,keywords);

            ms.Seek(0, SeekOrigin.Begin);

            List<Keyword> inKeys = (List<Keyword>)bf.Deserialize(ms);
            
            Expect(Object.ReferenceEquals(inKeys[0],k1));
            Expect(Object.ReferenceEquals(inKeys[1],k2));
            Expect(Object.ReferenceEquals(inKeys[2],k1));
            Expect(Object.ReferenceEquals(inKeys[3],k2));            
        }

        #endregion
    }

}
