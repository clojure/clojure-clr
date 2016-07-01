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

namespace clojure.test
{
    // This is pretty irrelevant for CLR.  Trying to deal with checked exceptions in the JVM code.
    // But no harm in matching their code.

    public class ReflectorTryCatchFixture
    {
        public static void fail(long x)
        {
            throw new Cookies("Long");
        }

        public static void fail(double y)
        {
            throw new Cookies("Double");
        }

        public void failWithCause(Double y)
        {
            throw new Cookies("Wrapped", new Cookies("Cause"));
        }
  

        [Serializable]
        public sealed class Cookies : Exception
        {
            public Cookies(String msg) : base(msg) { }
            public Cookies(String msg, Exception cause) : base(msg, cause) { }
        }
    }
}
