using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    [AttributeUsage(AttributeTargets.Method)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    public sealed class WarnBoxedMathAttribute : Attribute
    {
        public bool Value { get; private set; }

        public WarnBoxedMathAttribute(bool val)
        {
            Value = val;
        }
    }
}
