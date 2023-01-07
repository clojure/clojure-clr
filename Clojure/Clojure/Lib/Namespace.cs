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
    /// Represents a namespace for holding symbol-&gt;reference mappings.
    /// </summary>
    /// <remarks>
    /// <para>Symbol to reference mappings come in several flavors:
    /// <list>
    /// <item><b>Simple:</b> <see cref="Symbol">Symbol</see> to a <see cref="Var">Var</see> in the namespace.</item>
    /// <item><b>Use/refer:</b> <see cref="Symbol">Symbol</see> to a <see cref="Var">Var</see> that is homed in another namespace.</item>
    /// <item>Import:</item> <see cref="Symbol">Symbol</see> to a Type
    /// </list>
    /// </para>
    /// <para>One namespace can also refer to another namespace by an alias.</para>
    /// </remarks>
    [Serializable]
    public class Namespace : AReference, ISerializable
    {
        #region Data

        /// <summary>
        /// The namespace's name.
        /// </summary>
        private readonly Symbol _name;

        /// <summary>
        /// The namespace's name.
        /// </summary>
        public  Symbol Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Maps <see cref="Symbol">Symbol</see>s to their values (Types, <see cref="Var">Var</see>s, or arbitrary).
        /// </summary>
        [NonSerialized]
        private readonly AtomicReference<IPersistentMap> _mappings = new AtomicReference<IPersistentMap>();

        /// <summary>
        /// Maps <see cref="Symbol">Symbol</see>s to other namespaces (aliases).
        /// </summary>
        [NonSerialized]
        private readonly AtomicReference<IPersistentMap> _aliases = new AtomicReference<IPersistentMap>();


        // Why not use one of the IPersistentMap implementations?
        /// <summary>
        /// All namespaces, keyed by <see cref="Symbol">Symbol</see>.
        /// </summary>
        private static readonly JavaConcurrentDictionary<Symbol, Namespace> _namespaces
            = new JavaConcurrentDictionary<Symbol, Namespace>();

   
        /// <summary>
        /// Get the variable-to-value map.
        /// </summary>
        private IPersistentMap Mappings
        {
            get { return _mappings.Get(); }
            //set { mappings = value; }
        }
        /// <summary>
        /// Get the variable-to-namespace alias map.
        /// </summary>
        private IPersistentMap Aliases
        {
            get { return _aliases.Get(); }
        }

        /// <summary>
        /// Get all namespaces.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static ISeq all
        {
            get
            { return RT.seq(_namespaces.Values); }
        }

        #endregion

        #region C-tors & factory methods

        /// <summary>
        /// Find or create a namespace named by the symbol.
        /// </summary>
        /// <param name="name">The symbol naming the namespace.</param>
        /// <returns>An existing or new namespace</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Namespace findOrCreate(Symbol name)
        {
            Namespace ns = _namespaces.Get(name);
            if (ns != null)
                return ns;
            Namespace newns = new Namespace(name);
            ns = _namespaces.PutIfAbsent(name, newns);
            return ns ?? newns;
        }

        /// <summary>
        /// Remove a namespace (by name).
        /// </summary>
        /// <param name="name">The (Symbol) name of the namespace to remove.</param>
        /// <returns>The namespace that was removed.</returns>
        /// <remarks>Trying to remove the clomure namespace throws an exception.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Namespace remove(Symbol name)
        {
            if (name.Equals(RT.ClojureNamespace.Name))
                throw new ArgumentException("Cannot remove clojure namespace");
            return _namespaces.Remove(name);
        }

        /// <summary>
        /// Find the namespace with a given name.
        /// </summary>
        /// <param name="name">The name of the namespace to find.</param>
        /// <returns>The namespace with the given name, or <value>null</value> if no such namespace exists.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Namespace find(Symbol name)
        {
            return _namespaces.Get(name);
        }
        

        /// <summary>
        /// Construct a namespace with a given name.
        /// </summary>
        /// <param name="name">The name.</param>
        Namespace(Symbol name)
            : base(name.meta())
        {
            _name = name;
            _mappings.Set(RT.DefaultImports);
            _aliases.Set(RT.map());
        }

         #endregion

        #region Object overrides

        /// <summary>
        /// Returns a string representing the namespace.
        /// </summary>
        /// <returns>A string representing the namespace.</returns>
        public override string ToString()
        {
            return _name.ToString();
        }

        #endregion

        #region Interning 


        public static bool AreDifferentInstancesOfSameClassName(Type t1, Type t2)
        {
            return (t1 != t2) && (t1.FullName.Equals(t2.FullName));
        }

        Type ReferenceClass(Symbol sym, Type val)
        {
            if (sym.Namespace != null)
            {
                throw new ArgumentException("Can't intern namespace-qualified symbol");
            }
            IPersistentMap map = getMappings();
            Type c = map.valAt(sym) as Type;
            while ((c == null) || (AreDifferentInstancesOfSameClassName(c, val)))
            {
                IPersistentMap newMap = map.assoc(sym, val);
                _mappings.CompareAndSet(map, newMap);
                map = getMappings();
                c = (Type)map.valAt(sym);
            }
            if (c == val)
                return c;

            throw new InvalidOperationException(sym + " already refers to: " + c + " in namespace: " + Name);
        }

        /// <summary>
        /// Determine if a mapping is interned
        /// </summary>
        /// <remarks>
        /// An interned mapping is one where a var's ns matches the current ns and its sym matches the mapping key.
        /// Once established, interned mappings should never change.
        /// </remarks>
        private bool IsInternedMapping(Symbol sym, Object o)
        {
            return o is Var v && v.ns == this && v.sym.Equals(sym);
        }

        /// <summary>
        /// Intern a <see cref="Symbol">Symbol</see> in the namespace, with a (new) <see cref="Var">Var</see> as its value.
        /// </summary>
        /// <param name="sym">The symbol to intern.</param>
        /// <returns>The <see cref="Var">Var</see> associated with the symbol.</returns>
        /// <remarks>
        /// <para>It is an error to intern a symbol with a namespace.</para>
        /// <para>This has to deal with other threads also interning.</para>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Var intern(Symbol sym)
        {
            if (sym.Namespace != null)
                throw new ArgumentException("Can't intern a namespace-qualified symbol");

            IPersistentMap map = Mappings;
            object o;
            Var v = null;
            // race condition
            while ((o = map.valAt(sym)) == null)
            {
                if (v == null)
                    v = new Var(this, sym);
                IPersistentMap newMap = map.assoc(sym, v);
                _mappings.CompareAndSet(map, newMap);
                map = Mappings;
            }

            if (IsInternedMapping(sym,o))
                return o as Var;

            if (v == null)
                v = new Var(this, sym);

            if (CheckReplacement(sym, o, v))
            {
                while (!_mappings.CompareAndSet(map, map.assoc(sym, v)))
                    map = Mappings;

                return v;
            }

            return o as Var;
        }


        /*
         This method checks if a namespace's mapping is applicable and warns on problematic cases.
         It will return a boolean indicating if a mapping is replaceable.
         The semantics of what constitutes a legal replacement mapping is summarized as follows:
        | classification | in namespace ns        | newval = anything other than ns/name | newval = ns/name                    |
        |----------------+------------------------+--------------------------------------+-------------------------------------|
        | native mapping | name -> ns/name        | no replace, warn-if newval not-core  | no replace, warn-if newval not-core |
        | alias mapping  | name -> other/whatever | warn + replace                       | warn + replace                      |
        */
        private bool CheckReplacement(Symbol sym, Object old, Object neu)
        {
            if (old is Var ovar)
            {
                Namespace ons = ovar.Namespace;
                Namespace nns = (neu is Var nvar) ? nvar.ns : null;

                if (IsInternedMapping(sym, old))
                {
                    if (nns != RT.ClojureNamespace)
                    {
                        RT.errPrintWriter().WriteLine("REJECTED: attempt to replace interned var {0} with {1} in {2}, you must ns-unmap first",
                            old, neu, _name);
                        RT.errPrintWriter().Flush();
                        return false;
                    }
                    else
                        return false;
                }
            }

            RT.errPrintWriter().WriteLine("WARNING: {0} already refers to: {1} in namespace: {2}, being replaced by: {3}",
                sym, old, _name, neu);
            RT.errPrintWriter().Flush();

            return true;
        }

        /// <summary>
        /// Intern a symbol with a specified value.
        /// </summary>
        /// <param name="sym">The symbol to intern.</param>
        /// <param name="val">The value to associate with the symbol.</param>
        /// <returns>The value that is associated. (only guaranteed == to the value given).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        object reference(Symbol sym, object val)
        {
            if ( sym.Namespace != null )
                throw new ArgumentException("Can't intern a namespace-qualified symbol");

            IPersistentMap map = Mappings;
            object o;

            // race condition
            while ((o = map.valAt(sym)) == null)
            {
                IPersistentMap newMap = map.assoc(sym, val);
                _mappings.CompareAndSet(map, newMap);
                map = Mappings;
            }

            if ( o == val )
                return o;

            if (CheckReplacement(sym, o, val))
            {
                while (!_mappings.CompareAndSet(map, map.assoc(sym, val)))
                    map = Mappings;
                return val;
            }

            return o;
        }

        /// <summary>
        /// Remove a symbol mapping from the namespace.
        /// </summary>
        /// <param name="sym">The symbol to remove.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public void unmap(Symbol sym)
        {
            if (sym.Namespace != null)
                throw new ArgumentException("Can't unintern a namespace-qualified symbol");

            IPersistentMap map = Mappings;
            while (map.containsKey(sym))
            {
                IPersistentMap newMap = map.without(sym);
                _mappings.CompareAndSet(map, newMap);
                map = Mappings;
            }
        }

        /// <summary>
        /// Map a symbol to a Type (import).
        /// </summary>
        /// <param name="sym">The symbol to associate with a Type.</param>
        /// <param name="t">The type to associate with the symbol.</param>
        /// <returns>The Type.</returns>
        /// <remarks>Named importClass instead of ImportType for core.clj compatibility.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Type importClass(Symbol sym, Type t)
        {
            return ReferenceClass(sym, t);
        }


        /// <summary>
        /// Map a symbol to a Type (import) using the type name for the symbol name.
        /// </summary>
        /// <param name="t">The type to associate with the symbol</param>
        /// <returns>The Type.</returns>
        /// <remarks>Named importClass instead of ImportType for core.clj compatibility.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Type importClass(Type t)
        {
            string n = Util.NameForType(t);   
            return importClass(Symbol.intern(n), t);
        }

        /// <summary>
        /// Add a <see cref="Symbol">Symbol</see> to <see cref="Var">Var</see> reference.
        /// </summary>
        /// <param name="sym"></param>
        /// <param name="var"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Var refer(Symbol sym, Var var)
        {
            return (Var)reference(sym, var);
        }

        #endregion

        #region Mappings

        /// <summary>
        /// Get the value mapped to a symbol.
        /// </summary>
        /// <param name="name">The symbol to look up.</param>
        /// <returns>The mapped value.</returns>
        public object GetMapping(Symbol name)
        {
            return Mappings.valAt(name);
        }

        /// <summary>
        /// Find the <see cref="Var">Var</see> mapped to a <see cref="Symbol">Symbol</see>.
        /// </summary>
        /// <param name="sym">The symbol to look up.</param>
        /// <returns>The mapped var.</returns>
        public Var FindInternedVar(Symbol sym)
        {
            return (Mappings.valAt(sym) is Var v && v.Namespace == this) ? v : null;
        }

        #endregion

        #region Aliases

        /// <summary>
        /// Find the <see cref="Namespace">Namespace</see> aliased by a <see cref="Symbol">Symbol</see>.
        /// </summary>
        /// <param name="alias">The symbol alias.</param>
        /// <returns>The aliased namespace</returns>
        public Namespace LookupAlias(Symbol alias)
        {
            return (Namespace)Aliases.valAt(alias);
        }

        /// <summary>
        /// Add an alias for a namespace.
        /// </summary>
        /// <param name="alias">The alias for the namespace.</param>
        /// <param name="ns">The namespace being aliased.</param>
        /// <remarks>Lowercase name for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public void addAlias(Symbol alias, Namespace ns)
        {
            if (alias == null)
                throw new ArgumentNullException(nameof(alias),"Expecting Symbol + Namespace");
            if ( ns == null )
                throw new ArgumentNullException(nameof(ns), "Expecting Symbol + Namespace");

            IPersistentMap map = Aliases;

            // race condition
            while (!map.containsKey(alias))
            {
                IPersistentMap newMap = map.assoc(alias, ns);
                _aliases.CompareAndSet(map, newMap);
                map = Aliases;
            }
            // you can rebind an alias, but only to the initially-aliased namespace
            if (!map.valAt(alias).Equals(ns))
                throw new InvalidOperationException(String.Format("Alias {0} already exists in namespace {1}, aliasing {2}",
                    alias, _name, map.valAt(alias)));
        }

        /// <summary>
        /// Remove an alias.
        /// </summary>
        /// <param name="alias">The alias name</param>
        /// <remarks>Lowercase name for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public void removeAlias(Symbol alias)
        {
            IPersistentMap map = Aliases;
            while (map.containsKey(alias))
            {
                IPersistentMap newMap = map.without(alias);
                _aliases.CompareAndSet(map, newMap);
                map = Aliases;
            }
        }


        #endregion

        #region core.clj compatibility

        /// <summary>
        /// Get the namespace name.
        /// </summary>
        /// <returns>The <see cref="Symbol">Symbol</see> naming the namespace.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public Symbol getName()
        {
            return Name;
        }

        /// <summary>
        /// Get the mappings of the namespace.
        /// </summary>
        /// <returns>The mappings.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IPersistentMap getMappings()
        {
            return Mappings;
        }

        /// <summary>
        /// Get the aliases.
        /// </summary>
        /// <returns>A map of aliases.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IPersistentMap getAliases()
        {
            return Aliases;
        }


        #endregion

        #region ISerializable Members

        [System.Security.SecurityCritical]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(NamespaceSerializationHelper));
            info.AddValue("_name",_name);
        }

        [Serializable]
        class NamespaceSerializationHelper : IObjectReference
        {
#pragma warning disable 649
            readonly Symbol _name;
#pragma warning restore 649

            #region IObjectReference Members

            public object GetRealObject(StreamingContext context)
            {
               return Namespace.findOrCreate(_name);
            }

            #endregion
        }

        #endregion
    }
}
