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
using System.Linq;
using System.Reflection;

namespace clojure.lang.CljCompiler
{

    internal class MethodSignature : IComparable<MethodSignature>, IComparable
    {
        #region Data

        readonly string _name;

        public string Name
        {
            get { return _name; }
        }

        readonly Type[] _paramTypes;

        public Type[] ParamTypes
        {
            get { return _paramTypes; }
        }

        readonly Type _returnType;

        public Type ReturnType
        {
            get { return _returnType; }
        }

        readonly bool _isStatic;

        public bool IsStatic
        {
            get { return _isStatic; }
        }

        readonly string _source;

        public string Source
        {
            get { return _source; }
        }

        readonly MethodInfo _method;

        public MethodInfo Method
        {
            get { return _method; }
        }

        readonly PropertyInfo _property;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public PropertyInfo Property
        {
            get { return _property; }
        } 

        #endregion

        #region Ctors

        public MethodSignature(MethodInfo m)
            : this(m, String.Empty)
        {
        }

        public MethodSignature(MethodInfo m, string source)
        {
            _name = m.Name;
            _paramTypes = m.GetParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray<Type>();
            _returnType = m.ReturnType;
            _isStatic = true;
            _source = source;
            _method = m;
            _property = null;
        }


        public MethodSignature(PropertyInfo pi)
            : this(pi, String.Empty)
        {
        }

        public MethodSignature(PropertyInfo pi, string source)
        {
            _name = pi.Name;
            _paramTypes = pi.GetIndexParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray<Type>();
            _returnType = pi.PropertyType;
            _isStatic = false;
            _source = source;
            _method = null;
            _property = pi;
        }

        public MethodSignature(string name, Type[] paramTypes, Type returnType, bool isStatic, string source)
        {
            _name = name;
            _paramTypes = paramTypes;
            _returnType = returnType;
            _isStatic = isStatic;
            _source = source;
            _method = null;
            _property = null;
        }

        #endregion

        #region IComparable<Sig> Members

        public int CompareTo(MethodSignature other)
        {
            int c = _name.CompareTo(other._name);
            if (c != 0)
                return c;

            // names are same, do lexicographic ordering on the types.
            int n1 = _paramTypes.Length;
            int n2 = other._paramTypes.Length;
            int n = Math.Min(n1, n2);
            for (int i = 0; i < n; i++)
            {
                c = _paramTypes[i].FullName.CompareTo(other._paramTypes[i].FullName);
                if (c != 0)
                    return c;
            }

            // equal through length of smallest. smallest wins
            if (n1 < n2)
                return -1;
            else if (n1 > n2)
                return 1;
            else
                return 0;
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            MethodSignature sig = obj as MethodSignature;
            if ( sig == null )
                throw new ArgumentException("Must compare to a Sig");

            return CompareTo(sig);
        }

        #endregion

        #region Object overrides

        public override bool Equals(object obj)
        {
            MethodSignature sig = obj as MethodSignature;
            if (sig == null)
                return false;

            return CompareTo(sig) == 0;
        }

        public override int GetHashCode()
        {
            int h = _name.GetHashCode();
            foreach (Type t in _paramTypes)
                h = Util.hashCombine(h, t.GetHashCode());
            return h;
        }

        #endregion
    }
}
