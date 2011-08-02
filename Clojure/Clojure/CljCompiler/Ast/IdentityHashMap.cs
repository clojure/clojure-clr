using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace clojure.lang.CljCompiler.Ast
{
    [Serializable]
    public class IdentityHashMap : Dictionary<Object,int>
    {
        class RefCmp : IEqualityComparer<Object>
        {
            #region IEqualityComparer<object> Members

            public new bool Equals(Object x, Object y)
            {
                return Object.ReferenceEquals(x, y);
            }
            
            public int GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }

            #endregion
        }

        public IdentityHashMap()
            : base(new RefCmp())
        {
        }

        public IdentityHashMap(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
