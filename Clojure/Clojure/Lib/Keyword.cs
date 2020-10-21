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
using System.Security.Permissions;

namespace clojure.lang
{
    /// <summary>
    /// Represents a keyword
    /// </summary>
    [Serializable]
    public sealed class Keyword: AFn, Named, IComparable, ISerializable, IHashEq // ??JAVA only used IFn, not AFn.  NOt sure why.
    {
        #region Data

        /// <summary>
        /// The symbol giving the namespace/name (without :) of the keyword.
        /// </summary>
        private readonly Symbol _sym;

        /// <summary>
        /// Caches the hasheq value for the keyword.
        /// </summary>
        readonly int _hasheq;

        /// <summary>
        /// Map from symbol to keyword to uniquify keywords.
        /// </summary>
        /// <remarks>Why introduce the JavaConcurrentDictionary?  
        /// We really only need a synchronized hash table with one operation: PutIfAbsent.</remarks>
        private static readonly JavaConcurrentDictionary<Symbol,WeakReference> _symKeyMap
            = new JavaConcurrentDictionary<Symbol, WeakReference>();

        internal Symbol Symbol
        {
          get { return _sym; }
        }

        // cache ToString if called
        [NonSerialized]
        string _str;

        #endregion

        #region C-tors and factory methods

        /// <summary>
        /// Create (or find existing) keyword with given symbol's namespace/name.
        /// </summary>
        /// <param name="sym">The symbol giving the keyword's namespace/name.</param>
        /// <returns>A keyword</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Keyword intern(Symbol sym)
        {
            Keyword k = null;
            WeakReference existingRef = _symKeyMap.Get(sym);
            if (existingRef == null)
            {
                if (sym.meta() != null)
                    sym = (Symbol)sym.withMeta(null);
                k = new Keyword(sym);

                WeakReference wr = new WeakReference(k)
                {
                    Target = k
                };
                existingRef = _symKeyMap.PutIfAbsent(sym, wr);
            }
            if (existingRef == null)
                return k;
            Keyword existingk = (Keyword)existingRef.Target;
            if (existingk != null)
                return existingk;
            // entry died in the interim, do over
            // let's not get confused, remove it.  (else infinite loop).
            _symKeyMap.Remove(sym);
            return intern(sym);
        }

        /// <summary>
        /// Create (or find existing) keyword with given namespace/name.
        /// </summary>
        /// <param name="ns">The keyword's namespace name.</param>
        /// <param name="name">The keyword's name.</param>
        /// <returns>A keyword</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Keyword intern(string ns, string name)
        {
            return intern(Symbol.intern(ns, name));
        }

        /// <summary>
        /// Create (or find existing) keyword with the given name.
        /// </summary>
        /// <param name="nsname">The keyword's name</param>
        /// <returns>A keyword</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Keyword intern(String nsname)
        {
            return intern(Symbol.intern(nsname));
        }

        /// <summary>
        /// Construct a keyword based on a symbol.
        /// </summary>
        /// <param name="sym">A symbol giving namespace/name.</param>
        private Keyword(Symbol sym)
        {
            _sym = sym;
            _hasheq = (int)(_sym.hasheq() + 0x9e3779b9);
        }


        #endregion

        #region Object overrides

        /// <summary>
        /// Returns a string representing the keyword.
        /// </summary>
        /// <returns>A string representing the keyword.</returns>
        public override string ToString()
        {
            if (_str == null)
                _str = ":" + _sym.ToString();
            return _str;
        }

        /// <summary>
        /// Determines if an object is equal to this keyword.  Value semantics.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><value>true</value> if equal; <value>false</value> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if ( ReferenceEquals(this,obj) ) 
                return true;

            if (!(obj is Keyword keyword))
                return false;

            return _sym.Equals(keyword.Symbol);
        }

        /// <summary>
        /// Gets a hash code for the keyword.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            return (int)(_sym.GetHashCode() + 0x9e3779b9);
        }

        #endregion

        #region Named Members

        //  I prefer to use the following internally.

        /// <summary>
        /// The namespace name.
        /// </summary>
        public string Namespace
        {
            get { return _sym.Namespace; }
        }

        /// <summary>
        /// The name.
        /// </summary>
        public string Name
        {
            get { return _sym.Name; }
        }

        // the following are in the interface


        /// <summary>
        /// Gets the namespace name.
        /// </summary>
        /// <returns>The namespace name.</returns>
        public string getNamespace()
        {
            return _sym.Namespace;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string getName()
        {
            return _sym.Name;
        }

        #endregion

        #region IFn Members

        /// <summary>
        /// (:keyword arg)  => (get arg :keyword)
        /// </summary>
        /// <param name="arg1">The object to access.</param>
        /// <returns>The value mapped to the keyword.</returns>
        public sealed override object invoke(object arg1)
        {
            if (arg1 is ILookup ilu)
                return ilu.valAt(this);

            return RT.get(arg1, this);
        }


        /// <summary>
        /// (:keyword arg default) => (get arg :keyword default)
        /// </summary>
        /// <param name="arg1">The object to access.</param>
        /// <param name="arg2">Default value if not found.</param>
        /// <returns></returns>

        public sealed override object invoke(object arg1, object notFound)
        {
            if (arg1 is ILookup ilu)
                return ilu.valAt(this, notFound);

            return RT.get(arg1, this, notFound);
        }


        #endregion

        #region IComparable Members

        /// <summary>
        /// Compare this to another object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>neg,zero,pos for &lt; = &gt;</returns>
        public int CompareTo(object obj)
        {
            if (!(obj is Keyword k))
                throw new ArgumentException("Cannot compare to null or non-Keyword", "obj");

            return _sym.CompareTo(k._sym);
        }

        #endregion

        #region Operator overloads

        public static bool operator ==(Keyword k1, Keyword k2)
        {
            if (ReferenceEquals(k1, k2))
                return true;

            if (k1 is null || k2 is null)
                return false;

            return k1.CompareTo(k2) == 0;
        }

        public static bool operator !=(Keyword k1, Keyword k2)
        {
            if (ReferenceEquals(k1, k2))
                return false;

            if (k1 is null || k2 is null)
                return true;

            return k1.CompareTo(k2) != 0;
        }

        public static bool operator <(Keyword k1, Keyword k2)
        {
            if (ReferenceEquals(k1, k2))
                return false;

            if (k1 is null)
                throw new ArgumentNullException("k1");

            return k1.CompareTo(k2) < 0;
        }

        public static bool operator >(Keyword k1, Keyword k2)
        {
            if (ReferenceEquals(k1, k2))
                return false;

            if (k1 is null)
                throw new ArgumentNullException("k1");

            return k1.CompareTo(k2) > 0;
        }

        #endregion

        #region ISerializable Members

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Instead of serializing the keyword,
            // serialize a KeywordSerializationHelper instead
            info.SetType(typeof(KeywordSerializationHelper));
            info.AddValue("_sym", _sym);
        }

        #endregion

        #region other

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Keyword find(Symbol sym)
        {
            WeakReference wr = _symKeyMap.Get(sym);
            if (wr != null)
                return (Keyword)wr.Target;
            else
                return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Keyword find(String ns, String name)
        {
            return find(Symbol.intern(ns, name));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Keyword find(String nsname)
        {
            return find(Symbol.intern(nsname));
        }

        #endregion

        #region IHashEq members

        public int hasheq()
        {
            return _hasheq;
        }

        #endregion
    }

    [Serializable]
    sealed class KeywordSerializationHelper : IObjectReference
    {

        #region Data

        readonly Symbol _sym;

        #endregion

        #region c-tors

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Standard API")]
        KeywordSerializationHelper(SerializationInfo info, StreamingContext context)
        {
            _sym = (Symbol)info.GetValue("_sym", typeof(Symbol));
        }

        #endregion

        #region IObjectReference Members

        public object GetRealObject(StreamingContext context)
        {
            return Keyword.intern(_sym);
        }

        #endregion
    }
}
