using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace clojure.lang.CljCompiler.Ast
{
    /// <summary>
    /// Exception thrown during parsing
    /// </summary>
    [Serializable]
    public class ParseException : Exception
    {
        public ParseException()
        {
        }

        public ParseException(string message)
            : base(message)
        {
        }

        public ParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
