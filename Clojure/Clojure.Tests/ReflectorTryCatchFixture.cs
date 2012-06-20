using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public sealed class Cookies : Exception
        {
            public Cookies(String msg) : base(msg) { }
        }
    }
}
