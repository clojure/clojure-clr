using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    public class ClojureException : Exception
    {
        public ClojureException()
        {
        }

        public ClojureException(string msg)
            : base(msg)
        {
        }

        public ClojureException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }
    }
}
