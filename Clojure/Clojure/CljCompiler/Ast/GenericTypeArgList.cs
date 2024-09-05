using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace clojure.lang.CljCompiler.Ast
{
    // For manipulating a list of types to be used as generic type arguments
    // Really it is just a wrapper around a List<Type>, but I had problems at some point in the code between passing around empty lists and nulls.
    // Using this consistently should help avoid those problems.

    public class GenericTypeArgList
    {

        // The list of types.  Will be non-null, but may be empty.
        private readonly List<Type> _typeArgs;

        // private constructor.  Use Create() to create an instance.
        private GenericTypeArgList(List<Type> typeArgs)
        {
            _typeArgs = typeArgs;
        }

        // An empty instance
        // In the semantics of (type-args ...), (type-args) is equivalent to not having any type args at all -- it is ignored.

        public readonly static GenericTypeArgList Empty = new GenericTypeArgList(new List<Type>());

        // Some places need a list of the types, some places need an array.`
        public Type[] ToArray() => _typeArgs.ToArray();
        public IReadOnlyList<Type> ToList() => _typeArgs.AsReadOnly();

        public bool IsEmpty => !_typeArgs.Any();
        public int Count => _typeArgs.Count;

        public static GenericTypeArgList Create(ISeq targs)
        {
            List<Type> types = new List<Type>();

            for (ISeq s = targs; s != null; s = s.next())
            {
                object arg = s.first();
                if (!(arg is Symbol))
                    throw new ArgumentException("Malformed generic method designator: type arg must be a Symbol");
                Type t = HostExpr.MaybeType(arg, false);
                if (t == null)
                    throw new ArgumentException("Malformed generic method designator: invalid type arg");
                types.Add(t);
            }

            return new GenericTypeArgList(types);
        }

        // Just a little convenience function for error messages.
        public string GenerateGenericTypeArgsString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<");
            for (int i = 0; i < _typeArgs.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(_typeArgs[i].Name);
            }
            sb.Append(">");
            return sb.ToString();
        }


    }
}
