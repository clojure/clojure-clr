using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using clojure.lang;

namespace Clojure.Tests.LibTests
{
    /// <summary>
    /// Dummy IFn to use in reduce tests
    /// </summary>
    public static class DummyFn
    {
        public static IFn CreateForReduce()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn2 = ( object x, object y ) => { return Numbers.addP(x,y); };
            return fn;
        }

        internal static IFn CreateForMetaAlter(IPersistentMap meta)
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { return meta; };
            fn._fn1 = (object x) => { return meta; };
            return fn;
        }
    }
}
