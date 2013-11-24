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

namespace clojure.api
{
    public static class API
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
        public static IFn var(object ns, object name)
        {
            return Var.intern(asSym(ns), asSym(name));
        }

        /// <summary>
        /// Read one object from the string s. Reads data in the edn format (http://edn-format.org).
        /// </summary>
        /// <param name="s">a string</param>
        /// <returns>an Object or nil</returns>
        public static object read(string s)
        {
            return EdnReadString.invoke(s);
        }

        static API()
        {
            Symbol edn = (Symbol)var("clojure.core", "symbol").invoke("clojure.edn");
            var("clojure.core", "require").invoke(edn);
        }

        private static readonly IFn EdnReadString = var("clojure.edn", "read-string");

    }
}
