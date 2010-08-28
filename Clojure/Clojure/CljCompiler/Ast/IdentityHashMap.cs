using System;
using System.Collections.Generic;

namespace clojure.lang.CljCompiler.Ast
{
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
    }
}
