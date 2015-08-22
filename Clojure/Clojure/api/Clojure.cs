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
using clojure.lang;


namespace clojure.clr.api
{
    /// <summary>
    /// Provides a minimal interface to bootstrap Clojure access from other CLR languages.
    /// </summary>
    /// <remarks>
    /// <para>The Clojure class provides a minimal interface to bootstrap Clojure access 
    /// from other JVM languages. It provides:</para>
    /// <list type=">">
    /// <item>The ability to use Clojure's namespaces to locate an arbitrary
    /// <a href="http://clojure.org/vars">var</a>, returning the
    /// var's clojure.lang.IFn interface.</item>
    /// <item>A convenience method <c>read</c> for reading data using
    /// Clojure's edn reader</item>
    /// </list>
    /// <para>To lookup and call a Clojure function:</para>
    /// <code>
    /// IFn plus = Clojure.var("clojure.core", "+");
    /// plus.invoke(1, 2);
    /// </code>
    /// <para>Functions in <c>clojure.core</c> are automatically loaded. Other
    /// namespaces can be loaded via <c>require</c>:</para>
    /// <code>
    /// IFn require = Clojure.var("clojure.core", "require");
    /// require.invoke(Clojure.read("clojure.set"));
    /// </code>
    /// <para><c>IFn</c>s can be passed to higher order functions, e.g. the
    /// example below passes <c>plus</c> to <c>read</c>:</para>
    /// <code>
    /// IFn map = Clojure.var("clojure.core", "map");
    /// IFn inc = Clojure.var("clojure.core", "inc");
    /// map.invoke(inc, Clojure.read("[1 2 3]"));
    /// </code>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public static class Clojure
    {
        private static Symbol asSym(object o)
        {
            String str = o as String;
            Symbol s = str != null ? Symbol.intern(str) : (Symbol)o;
            return s;
        }

        /// <summary>
        /// Returns the var associated with qualifiedName.
        /// </summary>
        /// <param name="qualifiedName">a String or clojure.lang.Symbol</param>
        /// <returns>a clojure.lang.IFn</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "var")]
        public static IFn var(object qualifiedName)
        {
            Symbol s = asSym(qualifiedName);
            return var(s.Namespace, s.Name);
        }

        /// <summary>
        /// Returns an IFn associated with the namespace and nanme
        /// </summary>
        /// <param name="ns">a String or clojure.lang.Symbol</param>
        /// <param name="name">a String or clojure.lang.Symbol</param>
        /// <returns>a clojure.lang.IFn</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "var")] 
        public static IFn var(object ns, object name)
        {
            return Var.intern(asSym(ns), asSym(name));
        }

        /// <summary>
        /// Read one object from the string s. Reads data in the edn format (http://edn-format.org).
        /// </summary>
        /// <param name="s">a string</param>
        /// <returns>an Object or nil</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "read")]
        public static object read(string s)
        {
            return EdnReadString.invoke(s);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Clojure()
        {
            Symbol edn = (Symbol)var("clojure.core", "symbol").invoke("clojure.edn");
            var("clojure.core", "require").invoke(edn);
        }

        private static readonly IFn EdnReadString = var("clojure.edn", "read-string");

    }
}
