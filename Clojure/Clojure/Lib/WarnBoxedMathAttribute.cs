using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    [AttributeUsage(AttributeTargets.Method)]
    public class WarnBoxedMathAttribute : Attribute
    {
        public bool Value { get; set; }

        public WarnBoxedMathAttribute(bool val)
        {
            Value = val;
        }
    }
}
