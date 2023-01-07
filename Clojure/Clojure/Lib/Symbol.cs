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
using System.Runtime.Serialization;

namespace clojure.lang
{
    /// <summary>
    /// Represents a symbol.
    /// </summary>
    /// <remarks>See the Clojure documentation for more information.</remarks>
    [Serializable]
    public class Symbol: AFn, IObj, Named, IComparable, ISerializable, IHashEq
    {
        #region Instance variables

        /// <summary>
        /// The name of the namespace for this symbol (if namespace-qualified).
        /// </summary>
        protected readonly  string _ns;

        /// <summary>
        /// The name of the symbol.
        /// </summary>
        protected readonly string _name;

        /// <summary>
        /// The cached hashcode.
        /// </summary>
        protected int _hasheq = 0;

        readonly IPersistentMap _meta;

        // cache ToString/ToStringEscaped if called
        [NonSerialized]
        string _str;

        [NonSerialized]
        string _strEsc;

        #endregion

        #region C-tors & factory methods

        // the create thunks preserve binary compatibility with code compiled
        // against earlier version of Clojure and can be removed (at some point).
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Symbol create(String ns, String name)
        {
            return Symbol.intern(ns, name);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Symbol create(String nsname)
        {
            return Symbol.intern(nsname);
        }


        /// <summary>
        /// Intern a symbol with the given name  and namespace-name.
        /// </summary>
        /// <param name="ns">The name of the namespace.</param>
        /// <param name="name">The name of the symbol.</param>
        /// <returns>A new symbol.</returns>
        /// <remarks>
        /// Interning here does not imply uniquifying.  
        /// The strings for the namespace-name and the symbol-name are uniquified.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        static public Symbol intern(string ns, string name)
        {
            return new Symbol(ns, name);
        }

        /// <summary>
        /// Intern a symbol with the given name (extracting the namespace if name is of the form ns/name).
        /// </summary>
        /// <param name="nsname">The (possibly qualified) name</param>
        /// <returns>A new symbol.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        static public Symbol intern(string nsname)
        {
            int i = nsname.IndexOf('/');
            return i == -1 || nsname.Equals("/")
                ? new Symbol(null, nsname)
                : new Symbol(nsname.Substring(0, i),
                             nsname.Substring(i + 1));
        }

        /// <summary>
        /// Construct a symbol from interned namespace name and symbol name.
        /// </summary>
        /// <param name="ns_interned">The (interned) namespace name.</param>
        /// <param name="name_interned">The (interned) symbol name.</param>
        private Symbol(string ns_interned, string name_interned) 
            : base()
        {
            _meta = null;
            _name = name_interned;
            _ns = ns_interned;
        }

        /// <summary>
        /// Construct a symbol from interned namespace name and symbol name,  with given metadata.
        /// </summary>
        /// <param name="meta">The metadata to attach.</param>
        /// <param name="ns_interned">The (interned) namespace name.</param>
        /// <param name="name_interned">The (interned) symbol name.</param>
        private Symbol(IPersistentMap meta, string ns_interned, string name_interned)
        {
            _meta = meta;
            _name = name_interned;
            _ns = ns_interned;
        }

        /// <summary>
        /// Construct a Symbol during deserialization.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected Symbol (SerializationInfo info, StreamingContext context)
        {
            _name = String.Intern(info.GetString("_name"));

            string nsStr = info.GetString("_ns");
            _ns = nsStr == null ? null : String.Intern(nsStr);
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// Return  a string representing the symbol.
        /// </summary>
        /// <returns>A string representing the symbol.</returns>
        public override string ToString()
        {
            if (_str == null)
            {
                if (_ns != null)
                    _str = _ns + "/" + _name;
                else
                    _str = _name;
            }
            return _str;
        }

        private static string NameMaybeEscaped(string s)
        {
            return LispReader.NameRequiresEscaping(s) ? LispReader.VbarEscape(s) : s;
        }

        public string ToStringEscaped()
        {
            if (_strEsc == null)
            {
                if (_ns != null)
                    _strEsc = NameMaybeEscaped(_ns) + "/" + NameMaybeEscaped(_name);
                else
                    _strEsc = NameMaybeEscaped(_name);

            }
            return _strEsc;
        }

        /// <summary>
        /// Determine if an object is equal to this symbol.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><value>true</value> if they are the same;<value>false</value> otherwise.</returns>
        /// <remarks>Uses value semantics, value determined by namespace name and symbol name.</remarks>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this,obj))
                return true;

            if (!(obj is Symbol sym))
                return false;

            return Util.equals(_ns, sym._ns) && _name.Equals(sym._name);
        }

        /// <summary>
        /// Get the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Util.hashCombine(_name.GetHashCode(), Util.hash(_ns));
        }

        #endregion

        #region IObj members

        /// <summary>
        /// Create a copy with new metadata.
        /// </summary>
        /// <param name="meta">The new metadata.</param>
        /// <returns>A copy of the object with new metadata attached.</returns>
        public IObj withMeta(IPersistentMap meta)
        {
            return meta == _meta
                ? this
                : new Symbol(meta, _ns, _name);
        }

        #endregion

        #region IMeta Members

        public IPersistentMap meta()
        {
            return _meta;
        }

        #endregion
        
        #region Named members

        // I prefer to use these internally.

        /// <summary>
        /// Get the namespace name.
        /// </summary>
        public string Namespace
        {
            get { return _ns; }
        }

        /// <summary>
        /// Get the symbol name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        } 

        // the following are in the interface
        
        /// <summary>
        /// Get the namespace name.
        /// </summary>
        /// <returns>The namespace name.</returns>
        public string getNamespace()
        {
            return _ns;
        }

        /// <summary>
        /// Gets the symbol name.
        /// </summary>
        /// <returns>The symbol name.</returns>
        public string getName()
        {
            return _name;
        }

        #endregion

        #region IFn members


        public override object invoke(Object obj)
        {
            return RT.get(obj, this);
        }


        public override object invoke(Object obj, Object notFound)
        {
            return RT.get(obj, this, notFound);
        }

        #endregion

        #region IComparable Members

        /// <summary>
        /// Compare this symbol to another object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>neg,zero,pos semantics.</returns>
        public int CompareTo(object obj)
        {
            if (!(obj is Symbol s))
                throw new ArgumentException("Must compare to non-null Symbol", "obj");

            if (Equals(s))
                return 0;
            if (_ns == null && s._ns != null)
                return -1;
            if (_ns != null)
            {
                if (s._ns == null)
                    return 1;
                int nsc = _ns.CompareTo(s._ns);
                if (nsc != 0)
                    return nsc;
            }
            return _name.CompareTo(s._name);
        }

        #endregion

        #region operator overloads

        public static bool operator ==(Symbol x, Symbol y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if ((x is null) || (y is null))
                return false;

            return x.CompareTo(y) == 0;
        }

        public static bool operator !=(Symbol x, Symbol y)
        {
            if (ReferenceEquals(x,y))
                return false;

            if ((x is null) || (y is null))
                return true;

            return x.CompareTo(y) != 0;
        }

        public static bool operator <(Symbol x, Symbol y)
        {
            if (ReferenceEquals(x, y))
                return false; 
            
            if (x is null)
                throw new ArgumentNullException("x");

            return x.CompareTo(y) < 0;
        }

        public static bool operator >(Symbol x, Symbol y)
        {
            if (ReferenceEquals(x, y))
                return false;

            if (x is null)
                throw new ArgumentNullException("x");

            return x.CompareTo(y) > 0;
        }

        #endregion

        #region Other

        ///// <summary>
        ///// Create a copy of this symbol.
        ///// </summary>
        ///// <returns>A copy of this symbol.</returns>
        //private object readResolve()
        //{
        //    return intern(_ns, _name);
        //}

        #endregion

        #region ISerializable Members

        [System.Security.SecurityCritical]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_name",_name);
            info.AddValue("_ns", _ns);
        }

        #endregion

        #region IHashEq members

        public int hasheq()
        {
            if (_hasheq == 0)
            {
                _hasheq = Util.hashCombine(Murmur3.HashString(_name), Util.hasheq(_ns));
            }
            return _hasheq;
        }

        #endregion
    }
}
