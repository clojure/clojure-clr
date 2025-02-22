﻿/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using clojure.lang.CljCompiler.Ast;
using clojure.lang.CljCompiler.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace clojure.lang
{
    public static class Compiler
    {
        #region other constants

        internal const int MaxPositionalArity = 20;
        //internal const string CompileStubPrefix = "compile__stub";
        internal const string DeftypeBaseClassNamePrefix = "DeftypeBase";

        #endregion

        #region Duplicate types

        static readonly Dictionary<String, Type> _duplicateTypeMap = new Dictionary<string, Type>();

        internal static void RegisterDuplicateType(Type type)
        {
            _duplicateTypeMap[type.FullName] = type;
        }

        internal static Type FindDuplicateType(string typename)
        {
            _duplicateTypeMap.TryGetValue(typename, out Type type);
            return type;
        }

        #endregion
        
        #region Symbols

        public static readonly Symbol DefSym = Symbol.intern("def");
        public static readonly Symbol LoopSym = Symbol.intern("loop*");
        public static readonly Symbol RecurSym = Symbol.intern("recur");
        public static readonly Symbol IfSym = Symbol.intern("if");
        public static readonly Symbol LetSym = Symbol.intern("let*");
        public static readonly Symbol LetfnSym = Symbol.intern("letfn*");
        public static readonly Symbol DoSym = Symbol.intern("do");
    
        public static readonly Symbol FnSym = Symbol.intern("fn*");
    
        public static readonly Symbol FnOnceSym = (Symbol) Symbol.intern("fn*").withMeta(RT.map(Keyword.intern(null, "once"), true));
        public static readonly Symbol QuoteSym = Symbol.intern("quote");
        public static readonly Symbol TheVarSym = Symbol.intern("var");
        public static readonly Symbol DotSym = Symbol.intern(".");
        public static readonly Symbol AssignSym = Symbol.intern("set!");
        public static readonly Symbol TrySym = Symbol.intern("try");
        public static readonly Symbol CatchSym = Symbol.intern("catch");
        public static readonly Symbol FinallySym = Symbol.intern("finally");
        public static readonly Symbol ThrowSym = Symbol.intern("throw");
        public static readonly Symbol MonitorEnterSym = Symbol.intern("monitor-enter");
        public static readonly Symbol MonitorExitSym = Symbol.intern("monitor-exit");
        public static readonly Symbol ImportSym = Symbol.intern("clojure.core","import*");
        public static readonly Symbol DeftypeSym = Symbol.intern("deftype*");
        public static readonly Symbol CaseSym = Symbol.intern("case*");
        public static readonly Symbol NewSym = Symbol.intern("new");
        public static readonly Symbol ThisSym = Symbol.intern("this");
        public static readonly Symbol ReifySym = Symbol.intern("reify*");
        public static readonly Symbol AmpersandSym = Symbol.intern("&");

        public static readonly Symbol IdentitySym = Symbol.intern("clojure.core", "identity");

        static readonly Symbol NsSym = Symbol.intern("ns");
        static readonly Symbol InNsSym = Symbol.intern("in-ns");

        internal static readonly Symbol ISeqSym = Symbol.intern("clojure.lang.ISeq");

        internal static readonly Symbol ClassSym = Symbol.intern("System.Type");

        //internal static readonly Symbol InvokeStaticSym = Symbol.intern("invokeStatic");

        #endregion

        #region Keywords

        internal static readonly Keyword LoadNsKeyword = Keyword.intern(null, "load-ns");
        static readonly Keyword InlineKeyword = Keyword.intern(null, "inline");
        static readonly Keyword InlineAritiesKeyword = Keyword.intern(null, "inline-arities");
        //internal static readonly Keyword StaticKeyword = Keyword.intern(null, "static");
        internal static readonly Keyword ArglistsKeyword = Keyword.intern(null, "arglists");

        //static readonly Keyword VolatileKeyword = Keyword.intern(null,"volatile");
        internal static readonly Keyword ImplementsKeyword = Keyword.intern(null,"implements");
        internal static readonly Keyword ProtocolKeyword = Keyword.intern(null,"protocol");
        //static readonly Keyword OnKeyword = Keyword.intern(null, "on");
        internal static readonly Keyword DynamicKeyword = Keyword.intern("dynamic");
        internal static readonly Keyword RedefKeyword = Keyword.intern("redef");


        internal static readonly Keyword DisableLocalsClearingKeyword = Keyword.intern("disable-locals-clearing");

        internal static readonly Keyword DirectLinkingKeyword = Keyword.intern("direct-linking");
        internal static readonly Keyword ElideMetaKeyword = Keyword.intern("elide-meta");
 

        #endregion

        #region Vars

        //boolean
        internal static readonly Var CompileFilesVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                         Symbol.intern("*compile-files*"), false).setDynamic();  


        internal static readonly Var InstanceVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                         Symbol.intern("instance?"), false).setDynamic();  


        //String
        public static readonly Var CompilePathVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                 Symbol.intern("*compile-path*"), null).setDynamic();

        public static readonly Var CompileVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                Symbol.intern("compile"));

        // String
        internal static readonly Var SourceVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                        Symbol.intern("*source-path*"), "NO_SOURCE_FILE").setDynamic();
        // String
        internal static readonly Var SourcePathVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
            Symbol.intern("*file*"), "NO_SOURCE_PATH").setDynamic();

        //Integer
        public static readonly Var LineVar = Var.create(0).setDynamic();          // From the JVM version
        public static readonly Var ColumnVar = Var.create(0).setDynamic();          // From the JVM version
        //internal static readonly Var LINE_BEFORE = Var.create(0).setDynamic();   // From the JVM version
        //internal static readonly Var COLUMN_BEFORE = Var.create(0).setDynamic();   // From the JVM version
        //internal static readonly Var LINE_AFTER = Var.create(0).setDynamic();    // From the JVM version
        //internal static readonly Var COLUMN_AFTER = Var.create(0).setDynamic();    // From the JVM version
        public static readonly Var SourceSpanVar = Var.create(null).setDynamic();    // Mine

        internal static int LineVarDeref()
        {
            return Util.ConvertToInt(LineVar.deref());
        }

        internal static int ColumnVarDeref()
        {
            return Util.ConvertToInt(ColumnVar.deref());
        }

        internal static readonly Var MethodVar = Var.create(null).setDynamic();
        public static readonly Var LocalEnvVar = Var.create(PersistentHashMap.EMPTY).setDynamic();
        //Integer
        internal static readonly Var NextLocalNumVar = Var.create(0).setDynamic();
        internal static readonly Var LoopLocalsVar = Var.create(null).setDynamic();
        // Label
        internal static readonly Var LoopLabelVar = Var.create().setDynamic();

        internal static readonly Var InTryBlockVar = Var.create(null).setDynamic();          //null or not
        internal static readonly Var InCatchFinallyVar = Var.create(null).setDynamic();          //null or not
        internal static readonly Var MethodReturnContextVar = Var.create(null).setDynamic();    // null or not

        internal static readonly Var NoRecurVar = Var.create(null).setDynamic();

        internal static readonly Var VarsVar = Var.create().setDynamic();           //var->constid
        internal static readonly Var ConstantsVar = Var.create().setDynamic();      //vector<object>
        internal static readonly Var ConstantIdsVar = Var.create().setDynamic();   // IdentityHashMap
        internal static readonly Var KeywordsVar = Var.create().setDynamic();       //keyword->constid

        internal static readonly Var KeywordCallsitesVar = Var.create().setDynamic();  // vector<keyword>
        internal static readonly Var ProtocolCallsitesVar = Var.create().setDynamic(); // vector<var>
        internal static readonly Var VarCallsitesVar = Var.create().setDynamic();      // set<var>

        internal static readonly Var CompileStubSymVar = Var.create(null).setDynamic();
        internal static readonly Var CompileStubClassVar = Var.create(null).setDynamic();
        internal static readonly Var CompileStubOrigClassVar = Var.create(null).setDynamic();
        internal static readonly Var CompilingDefTypeVar = Var.create(null).setDynamic();

        internal static readonly Var CompilerContextVar = Var.create(null).setDynamic();
        internal static readonly Var CompilerActiveVar = Var.create(false).setDynamic();
        
        public static Var CompilerOptionsVar;

        public static object GetCompilerOption(Keyword k)
        {
            return RT.get(CompilerOptionsVar.deref(), k);
        }

        internal static void InitializeCompilerOptions()
        {
            Object compilerOptions = null;

			string nixPrefix = "CLOJURE_COMPILER_";
			string winPrefix = "clojure.compiler.";

            IDictionary envVars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry de in envVars)
            {
                string name = (string)de.Key;
                string v = (string)de.Value;
				if (name.StartsWith(nixPrefix))
                {
					// compiler options on *nix need to be of the form
					// CLOJURE_COMPILER_DIRECT_LINKING because most shells do not
					// support hyphens in variable names
					string optionName = name.Substring(nixPrefix.Length).Replace("_", "-").ToLower();
                    compilerOptions = RT.assoc(compilerOptions,
                        RT.keyword(null, optionName),
                        RT.readString(v));
                }
				if ( name.StartsWith(winPrefix))
                {
                    compilerOptions = RT.assoc(compilerOptions,
                        RT.keyword(null, name.Substring(winPrefix.Length)),
                        RT.readString(v));
                }

            }

            CompilerOptionsVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                Symbol.intern("*compiler-options*"), compilerOptions).setDynamic();
        }

        public static object ElideMeta(object m)
        {
            ICollection<Object> elides = (ICollection<Object>)GetCompilerOption(ElideMetaKeyword);
            if (elides != null)
            {
                foreach (object k in elides)
                    m = RT.dissoc(m, k);
            }
            return m;
        }

        #endregion

        #region Special forms

        public static readonly IPersistentMap specials = PersistentHashMap.create(
            DefSym, new DefExpr.Parser(),
            LoopSym, new LetExpr.Parser(),
            RecurSym, new RecurExpr.Parser(),
            IfSym, new IfExpr.Parser(),
            CaseSym, new CaseExpr.Parser(),
            LetSym, new LetExpr.Parser(),
            LetfnSym, new LetFnExpr.Parser(),
            DoSym, new BodyExpr.Parser(),
            FnSym, null,
            QuoteSym, new ConstantExpr.Parser(),
            TheVarSym, new TheVarExpr.Parser(),
            ImportSym, new ImportExpr.Parser(),
            DotSym, new HostExpr.Parser(),
            AssignSym, new AssignExpr.Parser(),
            DeftypeSym, new NewInstanceExpr.DefTypeParser(),
            ReifySym, new NewInstanceExpr.ReifyParser(),
            TrySym, new TryExpr.Parser(),
            ThrowSym, new ThrowExpr.Parser(),
            MonitorEnterSym, new MonitorEnterExpr.Parser(),
            MonitorExitSym, new MonitorExitExpr.Parser(),
            CatchSym, null,
            FinallySym, null,
            NewSym, new NewExpr.Parser(),
            AmpersandSym, null
        );

        public static bool IsSpecial(Object sym)
        {
            return specials.containsKey(sym);
        }

        static IParser GetSpecialFormParser(object op)
        {
            return (IParser)specials.valAt(op);
        }

        #endregion

        #region MethodInfos, etc.

        internal static readonly MethodInfo Method_ArraySeq_create = typeof(ArraySeq).GetMethod("create", new Type[] { typeof(Object[]) });

        internal static readonly PropertyInfo Method_Compiler_CurrentNamespace = typeof(Compiler).GetProperty("CurrentNamespace");
        internal static readonly MethodInfo Method_Compiler_PushNS = typeof(Compiler).GetMethod("PushNS");

        internal static readonly MethodInfo Method_ILookupSite_fault = typeof(ILookupSite).GetMethod("fault");
        internal static readonly MethodInfo Method_ILookupThunk_get = typeof(ILookupThunk).GetMethod("get");

        internal static readonly MethodInfo Method_IPersistentMap_valAt2 = typeof(ILookup).GetMethod("valAt", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_IPersistentMap_without = typeof(IPersistentMap).GetMethod("without");

        internal static readonly MethodInfo Method_IObj_withMeta = typeof(IObj).GetMethod("withMeta");

        internal static readonly MethodInfo Method_Keyword_intern_string = typeof(Keyword).GetMethod("intern", new Type[] { typeof(String) });
        
        internal static readonly MethodInfo Method_Monitor_Enter = typeof(Monitor).GetMethod("Enter", new Type[] { typeof(Object) });
        internal static readonly MethodInfo Method_Monitor_Exit = typeof(Monitor).GetMethod("Exit", new Type[] { typeof(Object) });

        internal static readonly MethodInfo Method_Namespace_importClass1 = typeof(Namespace).GetMethod("importClass", new Type[] { typeof(Type) });
        internal static readonly MethodInfo Method_Namespace_importClass2 = typeof(Namespace).GetMethod("importClass", new Type[] { typeof(Symbol), typeof(Type) });

        internal static readonly MethodInfo Method_PersistentList_create = typeof(PersistentList).GetMethod("create", new Type[] { typeof(System.Collections.IList) });
        internal static readonly MethodInfo Method_PersistentHashSet_create = typeof(PersistentHashSet).GetMethod("create", new Type[] { typeof(Object[]) });
        internal static readonly FieldInfo Method_PersistentHashSet_EMPTY = typeof(PersistentHashSet).GetField("EMPTY");

        internal static readonly MethodInfo Method_Reflector_GetInstanceFieldOrProperty = typeof(Reflector).GetMethod("GetInstanceFieldOrProperty");
        internal static readonly MethodInfo Method_Reflector_SetInstanceFieldOrProperty = typeof(Reflector).GetMethod("SetInstanceFieldOrProperty");

        internal static readonly MethodInfo Method_RT_classForName = typeof(RT).GetMethod("classForName");
        internal static readonly MethodInfo Method_RT_intCast_long = typeof(RT).GetMethod("intCast", new Type[] { typeof(long) });
        internal static readonly MethodInfo Method_RT_uncheckedIntCast_long = typeof(RT).GetMethod("uncheckedIntCast", new Type[] { typeof(long) });
        internal static readonly MethodInfo Method_RT_keyword = typeof(RT).GetMethod("keyword");
        internal static readonly MethodInfo Method_RT_map = typeof(RT).GetMethod("map");
        internal static readonly MethodInfo Method_RT_mapUniqueKeys = typeof(RT).GetMethod("mapUniqueKeys"); 
        internal static readonly MethodInfo Method_RT_seq = typeof(RT).GetMethod("seq"); 
        internal static readonly MethodInfo Method_RT_seqOrElse = typeof(RT).GetMethod("seqOrElse");
        internal static readonly MethodInfo Method_RT_set = typeof(RT).GetMethod("set");
        internal static readonly MethodInfo Method_RT_vector = typeof(RT).GetMethod("vector");
        internal static readonly MethodInfo Method_RT_readString = typeof(RT).GetMethod("readString", new Type[]{ typeof(String) });
        internal static readonly MethodInfo Method_RT_var2 = typeof(RT).GetMethod("var", new Type[] { typeof(string), typeof(string) });

        internal static readonly MethodInfo Method_Symbol_intern2 = typeof(Symbol).GetMethod("intern", new Type[] { typeof(string), typeof(string) });

        internal static readonly MethodInfo Method_Type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

        internal static readonly MethodInfo Method_Util_classOf = typeof(Util).GetMethod("classOf");
        internal static readonly MethodInfo Method_Util_ConvertToInt = typeof(Util).GetMethod("ConvertToInt");

        internal static readonly MethodInfo Method_Util_equiv = typeof(Util).GetMethod("equiv", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_Util_hash = typeof(Util).GetMethod("hash");
        internal static readonly MethodInfo Method_Util_IsNonCharNumeric = typeof(Util).GetMethod("IsNonCharNumeric");
        
        internal static readonly MethodInfo Method_Var_bindRoot = typeof(Var).GetMethod("bindRoot");
        internal static readonly MethodInfo Method_Var_get = typeof(Var).GetMethod("deref");
        internal static readonly MethodInfo Method_Var_set = typeof(Var).GetMethod("set");
        internal static readonly MethodInfo Method_Var_setMeta = typeof(Var).GetMethod("setMeta");
        internal static readonly MethodInfo Method_Var_popThreadBindings = typeof(Var).GetMethod("popThreadBindings");
        internal static readonly MethodInfo Method_Var_getRawRoot = typeof(Var).GetMethod("getRawRoot");
        internal static readonly MethodInfo Method_Var_setDynamic0 = typeof(Var).GetMethod("setDynamic", Type.EmptyTypes);

        internal static readonly ConstructorInfo Ctor_KeywordLookupSite_1 = typeof(KeywordLookupSite).GetConstructor(new Type[] { typeof(Keyword) });
        internal static readonly ConstructorInfo Ctor_Regex_1 = typeof(Regex).GetConstructor(new Type[] { typeof(String) });

        internal static readonly ConstructorInfo Ctor_Serializable = typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes);

        internal static readonly MethodInfo[] Methods_IFn_invoke = new MethodInfo[MaxPositionalArity + 2];
        internal static readonly MethodInfo[] Methods_CreateTuple = new MethodInfo[] { 
            typeof(Tuple).GetMethod("create",CreateObjectTypeArray(0)),
            typeof(Tuple).GetMethod("create",CreateObjectTypeArray(1)),
            typeof(Tuple).GetMethod("create",CreateObjectTypeArray(2)),
            typeof(Tuple).GetMethod("create",CreateObjectTypeArray(3)),
            typeof(Tuple).GetMethod("create",CreateObjectTypeArray(4)),
            typeof(Tuple).GetMethod("create",CreateObjectTypeArray(5)),
            typeof(Tuple).GetMethod("create",CreateObjectTypeArray(6)),
        };

        internal static Type[] CreateObjectTypeArray(int size)
        {
            Type[] typeArray = new Type[size];
            for (int i = 0; i < size; i++)
                typeArray[i] = typeof(Object);
            return typeArray;
        }

        #endregion

        #region QME, param-tag suport

        internal readonly static Symbol ParamTagAny = Symbol.intern(null, "_");

        internal static IPersistentVector ParamTagsOf(Symbol sym)
        {
            var paramTags = RT.get(RT.meta(sym),RT.ParamTagsKey);

            if (paramTags != null && !(paramTags is IPersistentVector))
                throw new ArgumentException($"param-tags of symbol {sym} should be a vector.");

            return (IPersistentVector)paramTags;
        }

        // calls TagToType on every element, unless it encounters _ which becomes null
        internal static List<Type> TagsToClasses(ISeq paramTags)
        {
            if (paramTags == null)
                return null;

            var sig = new List<Type>();

            for (ISeq s = paramTags; s != null; s = s.next())
            {
                var t = s.first();
                if (t.Equals(ParamTagAny))
                    sig.Add(null);
                else
                    sig.Add(HostExpr.TagToType(t));
            }
    
            return sig;
        }

        internal static bool SignatureMatches(List<Type> sig, MethodBase method)
        {
            ParameterInfo[] methodSig = method.GetParameters();

            for ( int i=0; i<methodSig.Length; i++ )
            {
                if (sig[i] != null && !sig[i].Equals(methodSig[i].ParameterType))
                    return false;
            }

            return true;
        }

        static bool IsStaticMethod(MethodBase method) => method is MethodInfo mi && method.IsStatic;
        static bool IsInstanceMethod(MethodBase method) => !(method is MethodInfo) || method.IsStatic;
        static bool IsConstructor(MethodBase method) => method is ConstructorInfo;

        public static void CheckMethodArity(MethodBase method, int argCount)
        {
            ParameterInfo[] methodSig = method.GetParameters();
            if (methodSig.Length != argCount)
            {
                string name = method is ConstructorInfo ? "new" : method.Name;
                string description = MethodDescription(method.DeclaringType, name);
                throw new ArgumentException($"Invocation of {description} expected {methodSig.Length} arguments, but received {argCount}.");
            }
        }

        public static string MethodDescription(Type t, string name)
        {
            bool isCtor = t != null && name.Equals("new");
            string type = isCtor ? "constructor" : "method";
            return $"{type} {(isCtor ? "" : name)} in class {t.Name}";
        }
      


        #endregion


        #region C-tors & factory methods

        static Compiler()
        {
            for (int i = 0; i <= Compiler.MaxPositionalArity; i++)
                Methods_IFn_invoke[i] = typeof(IFn).GetMethod("invoke", CreateObjectTypeArray(i));

            Type[] types = new Type[Compiler.MaxPositionalArity + 1];
            CreateObjectTypeArray(Compiler.MaxPositionalArity).CopyTo(types, 0);
            types[Compiler.MaxPositionalArity] = typeof(object[]);
            Methods_IFn_invoke[Compiler.MaxPositionalArity + 1]
                = typeof(IFn).GetMethod("invoke", types);

            // Moved this to clojure.lang.RT's static constructor because we need to bind *compiler-options* there.
            // InitializeCompilerOptions();
        }

        #endregion

        #region Symbol/namespace resolving

        // TODO: we have duplicate code below.

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Symbol resolveSymbol(Symbol sym)
        {
            //already qualified or classname?
            if (sym.Name.IndexOf('.') > 0)
                return sym;

            if (sym.Namespace != null)
            {
                Namespace ns = namespaceFor(sym);
                if (ns == null || (ns.Name.Name == null ? sym.Namespace == null : ns.Name.Name.Equals(sym.Namespace)))
                {
                    Type at = HostExpr.MaybeArrayType(sym);
                    if (at != null)
                        return Util.arrayTypeToSymbol(at);
                    return sym;
                }
                return Symbol.intern(ns.Name.Name, sym.Name);
            }

            Object o = CurrentNamespace.GetMapping(sym);
            if (o == null)
                return Symbol.intern(CurrentNamespace.Name.Name, sym.Name);

            Type ot = o as Type;
             if (ot != null)
                return Symbol.intern(null, ot.FullName);

            if (o is Var ov)
                return Symbol.intern(ov.Namespace.Name.Name, ov.Symbol.Name);

            return null;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Namespace namespaceFor(Symbol sym)
        {
            return namespaceFor(CurrentNamespace, sym);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Namespace namespaceFor(Namespace inns, Symbol sym)
        {
            //note, presumes non-nil sym.ns
            // first check against currentNS' aliases...
            Symbol nsSym = Symbol.intern(sym.Namespace);
            Namespace ns = inns.LookupAlias(nsSym);
            if (ns == null)
            {
                // ...otherwise check the Namespaces map.
                ns = Namespace.find(nsSym);
            }
            return ns;
        }

        public static Namespace CurrentNamespace
        {
            get { return (Namespace)RT.CurrentNSVar.deref(); }
        }

        public static object Resolve(Symbol symbol, bool allowPrivate)
        {
            return ResolveIn(CurrentNamespace, symbol, allowPrivate);
        }

        public static object Resolve(Symbol symbol)
        {
            return ResolveIn(CurrentNamespace, symbol, false);
        }

        private static object ResolveIn(Namespace n, Symbol symbol, bool allowPrivate)
        {
            // note: ns-qualified vars must already exist
            if (symbol.Namespace != null)
            {
                Namespace ns = namespaceFor(n, symbol);
                if (ns == null)
                {
                    Type at = HostExpr.MaybeArrayType(symbol);
                    if ( at != null)
                        return at;
                    throw new InvalidOperationException("No such namespace: " + symbol.Namespace);
                }

                Var v = ns.FindInternedVar(Symbol.intern(symbol.Name));
                if (v == null)
                    throw new InvalidOperationException("No such var: " + symbol);
                else if (v.Namespace != CurrentNamespace && !v.isPublic && !allowPrivate)
                    throw new InvalidOperationException(string.Format("var: {0} is not public", symbol));
                return v;
            }
            else if (symbol.Name.IndexOf('.') > 0 || symbol.Name[symbol.Name.Length - 1] == ']')
                return RT.classForNameE(symbol.Name);
            else if (symbol.Equals(NsSym))
                return RT.NSVar;
            else if (symbol.Equals(InNsSym))
                return RT.InNSVar;
            else
            {
                if (Util.equals(symbol, CompileStubSymVar.get()))
                    return CompileStubClassVar.get();

                object o = n.GetMapping(symbol);
                if (o == null)
                {
                    if (RT.booleanCast(RT.AllowUnresolvedVarsVar.deref()))
                        return symbol;
                    else
                        throw new InvalidOperationException(string.Format("Unable to resolve symbol: {0} in this context", symbol));
                }
                return o;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static object maybeResolveIn(Namespace n, Symbol symbol)
        {
            // note: ns-qualified vars must already exist
            if (symbol.Namespace != null)
            {
                Namespace ns = namespaceFor(n, symbol);
                if (ns == null)
                    return HostExpr.MaybeArrayType(symbol);

                Var v = ns.FindInternedVar(Symbol.intern(symbol.Name));
                if (v == null)
                    return null;
                return v;
            }
            else if (symbol.Name.IndexOf('.') > 0 && !symbol.Name.EndsWith(".")
                || (symbol.Name.Length > 0 && symbol.Name[symbol.Name.Length - 1] == ']'))              /// JAVA: symbol.charAt[0] == '[')
                return RT.classForName(symbol.Name);
            else if (symbol.Equals(NsSym))
                return RT.NSVar;
            else if (symbol.Equals(InNsSym))
                return RT.InNSVar;
            else
            {
                object o = n.GetMapping(symbol);
                return o;
            }
        }

        #endregion

        #region Bindings, registration
        
        public static void RegisterVar(Var v)
        {
            if (!VarsVar.isBound)
                return;
            IPersistentMap varsMap = (IPersistentMap)VarsVar.deref();
            Object id = RT.get(varsMap, v);
            if (id == null)
            {
                VarsVar.set(RT.assoc(varsMap, v, RegisterConstant(v)));
            }
        }

        internal static Var LookupVar(Symbol sym, bool internNew, Boolean registerMacro)
        {
            Var var = null;

            // Note: ns-qualified vars in other namespaces must exist already
            if (sym.Namespace != null)
            {
                Namespace ns = Compiler.namespaceFor(sym);
                if (ns == null)
                    return null;
                Symbol name = Symbol.intern(sym.Name);
                if (internNew && ns == CurrentNamespace)
                    var = CurrentNamespace.intern(name);
                else
                    var = ns.FindInternedVar(name);
            }
            else if (sym.Equals(NsSym))
                var = RT.NSVar;
            else if (sym.Equals(InNsSym))
                var = RT.InNSVar;
            else
            {
                // is it mapped?
                Object o = CurrentNamespace.GetMapping(sym);
                if (o == null)
                {
                    // introduce a new var in the current ns
                    if (internNew)
                        var = CurrentNamespace.intern(Symbol.intern(sym.Name));
                }
                else
                {
                    var = o as Var;
                    if (var == null)
                        throw new InvalidOperationException(string.Format("Expecting var, but {0} is mapped to {1}", sym, o));
                }
            }
            if (var != null && (!var.IsMacro || registerMacro))
                RegisterVar(var);
            return var;
        }

        internal static Var LookupVar(Symbol sym, bool internNew)
        {
            return LookupVar(sym, internNew, true);
        }


        internal static int RegisterConstant(Object o)
        {
            if (!ConstantsVar.isBound)
                return -1;
            PersistentVector v = (PersistentVector)ConstantsVar.deref();
            IdentityHashMap ids = (IdentityHashMap)ConstantIdsVar.deref();
            if (ids.TryGetValue(o, out int i))
                return i;
            ConstantsVar.set(RT.conj(v, o));
            ids[o] = v.count();
            return v.count();
        }

        internal static KeywordExpr RegisterKeyword(Keyword keyword)
        {
            if (!KeywordsVar.isBound)
                return new KeywordExpr(keyword);

            IPersistentMap keywordsMap = (IPersistentMap)KeywordsVar.deref();
            object id = RT.get(keywordsMap, keyword);
            if (id == null)
                KeywordsVar.set(RT.assoc(keywordsMap, keyword, RegisterConstant(keyword)));
            return new KeywordExpr(keyword);
        }

        internal static int RegisterKeywordCallsite(Keyword keyword)
        {
            if (!KeywordCallsitesVar.isBound)
                throw new InvalidOperationException("KEYWORD_CALLSITES is not bound");

            IPersistentVector keywordCallsites = (IPersistentVector)KeywordCallsitesVar.deref();
            keywordCallsites = keywordCallsites.cons(keyword);
            KeywordCallsitesVar.set(keywordCallsites);
            return keywordCallsites.count() - 1;
        }

        internal static int RegisterProtocolCallsite(Var v)
        {
            if (!ProtocolCallsitesVar.isBound)
                throw new InvalidOperationException("PROTOCOL_CALLSITES is not bound");

            IPersistentVector protocolCallsites = (IPersistentVector)ProtocolCallsitesVar.deref();
            protocolCallsites = protocolCallsites.cons(v);
            ProtocolCallsitesVar.set(protocolCallsites);
            return protocolCallsites.count() - 1;
        }

        internal static void RegisterVarCallsite(Var v)
        {
            if (!VarCallsitesVar.isBound)
                throw new InvalidOperationException("VAR_CALLSITES is not bound");

            IPersistentCollection varCallsites = (IPersistentCollection)VarCallsitesVar.deref();
            varCallsites = varCallsites.cons(v);
            VarCallsitesVar.set(varCallsites);
            //return varCallsites.count() - 1;
        }

         internal static IPersistentCollection EmptyVarCallSites()
         {
             return PersistentHashSet.EMPTY;
         }


         internal static LocalBinding RegisterLocalThis(Symbol sym, Symbol tag, Expr init)
         {
             return RegisterLocalInternal(sym, tag, init, typeof(Object),  true, false, false);
         }

         internal static LocalBinding RegisterLocal(Symbol sym, Symbol tag, Expr init, Type declaredType, bool isArg)
        {
             return RegisterLocalInternal(sym, tag, init, declaredType, false, isArg, false);
        }


        internal static LocalBinding RegisterLocal(Symbol sym, Symbol tag, Expr init, Type declaredType, bool isArg, bool isByRef)
        {
            return RegisterLocalInternal(sym, tag, init, declaredType, false, isArg, isByRef);
        }

        private static LocalBinding RegisterLocalInternal(Symbol sym, Symbol tag, Expr init, Type declaredType, bool isThis, bool isArg, bool isByRef)
        {
            int num = GetAndIncLocalNum();
            LocalBinding b = new LocalBinding(num, sym, tag, init, declaredType, isThis, isArg, isByRef);
            IPersistentMap localsMap = (IPersistentMap)LocalEnvVar.deref();
            LocalEnvVar.set(RT.assoc(localsMap, b.Symbol, b));
            ObjMethod method = (ObjMethod)MethodVar.deref();
            method.AddLocal(num, b);
            return b;
        }

        internal static int GetAndIncLocalNum()
        {
            int num = (int)NextLocalNumVar.deref();
            ObjMethod m = (ObjMethod)MethodVar.deref();
            if (num > m.MaxLocal)
                m.MaxLocal = num;
            NextLocalNumVar.set(num + 1);
            return num;
        }

        internal static LocalBinding ReferenceLocal(Symbol symbol)
        {
            if (!LocalEnvVar.isBound)
                return null;

            LocalBinding b = (LocalBinding)RT.get(LocalEnvVar.deref(), symbol);
            if (b != null)
            {
                ObjMethod method = (ObjMethod)MethodVar.deref();
                if (b.Index == 0)
                    method.UsesThis = true;
                CloseOver(b, method);
            }

            return b;
        }

        static void CloseOver(LocalBinding b, ObjMethod method)
        {
            if (b != null && method != null)
            {
                LocalBinding lb = (LocalBinding)RT.get(method.Locals, b);
                if (lb == null)
                {
                    method.Objx.Closes = (IPersistentMap)RT.assoc(method.Objx.Closes, b, b);
                    CloseOver(b, method.Parent);
                }
                else
                {
                    if (lb.Index == 0)
                        method.UsesThis = true;
                    if (InCatchFinallyVar.deref() != null)
                    {
                        method.LocalsUsedInCatchFinally = (PersistentHashSet)method.LocalsUsedInCatchFinally.cons(b.Index);
                    }
                }
            }
        }

        #endregion

        #region other type hacking

        internal static Type MaybePrimitiveType(Expr e)
        {
            if (e is MaybePrimitiveExpr mpe && mpe.HasClrType && mpe.CanEmitPrimitive)
            {
                Type t = e.ClrType;
                if (Util.IsPrimitive(t))
                    return t;
            }
            return null;
        }

        internal static Type MaybeClrType(ICollection<Expr> exprs)
        {
            Type match = null;
            try
            {
                foreach (Expr e in exprs)
                {
                    if (e is ThrowExpr)
                        continue;
                    if (!e.HasClrType)
                        return null;
                    Type t = e.ClrType;
                    if (t == null)
                        return null;
                    if (match == null)
                        match = t;
                    else if (match != t)
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return match;
        }

        public static bool Inty(Type t)
        {
            return t == typeof(int)
                || t == typeof(uint)
                || t == typeof(short)
                || t == typeof(ushort)
                || t == typeof(byte)
                || t == typeof(sbyte)
                || t == typeof(char)
                || t == typeof(ulong);  // not sure abou this one.
        }

        public static Type RetType(Type tc, Type ret)
        {
            if (tc == null)
                return ret;
            if (ret == null)
                return tc;
            if (ret.IsPrimitive && tc.IsPrimitive)
            {
                if ((Inty(ret) && Inty(tc)) || (ret == tc))
                    return tc;
                throw new InvalidOperationException(String.Format("Cannot coerce {0} to {1}, use a cast instead", ret, tc));
            }
            return tc;
        }

        private static Dictionary<Type,string> primTypeNamesMap = new Dictionary<Type,string>
        {
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(char), "char" },
            { typeof(short), "short" },
            { typeof(byte), "byte" },
            { typeof(bool), "bool" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" },
            { typeof(sbyte), "sbyte" },
            { typeof(void), "void" }
        };

        public static bool TryPrimTypeToName(Type type, out string name)
        {
            return primTypeNamesMap.TryGetValue(type, out name);
        }

        public static Type PrimType(Symbol sym)
        {
            if (sym == null)
                return null;
            Type t = null;
            switch (sym.Name)
            {
                case "int":
                case "Int32":
                case "System.Int32":
                    t = typeof(int); break;
                case "long":
                case "Int64":
                case "System.Int64":
                    t = typeof(long); break;
                case "float": 
                case "Single":
                case "System.Single":
                    t = typeof(float); break;
                case "double": 
                case "Double":
                case "System.Double":
                    t = typeof(double); break;
                case "char": 
                case "Char":
                case "System.Char":
                    t = typeof(char); break;
                case "short": 
                case "Int16":
                case "System.Int16":
                    t = typeof(short); break;
                case "byte":
                case "Byte":
                case "System.Byte":
                    t = typeof(byte); break;
                case "bool":
                case "boolean":
                case "Boolean":
                case "System.Boolean":
                    t = typeof(bool); break;
                case "void": t = typeof(void); break;
                case "uint":
                case "UInt32":
                case "System.UInt32":
                    t = typeof(uint); break;
                case "ulong":
                case "UInt64":
                case "System.UInt64":
                    t = typeof(ulong); break;
                case "ushort":
                case "UInt16":
                case "System.UInt16":
                    t = typeof(ushort); break;
                case "sbyte":
                case "SByte":
                case "System.SByte":
                    t = typeof(sbyte); break;
            }
            return t;
        }

        public static Type PrimType(Type t)
        {
            return t.IsPrimitive ? t : typeof(object);
        }

        internal static Type TagType(Object tag)
        {
            if (tag == null)
                return typeof(object);

            Type t = null;

            {
                Symbol tagAsSym = tag as Symbol;
                if (tagAsSym != null)
                    t = PrimType(tagAsSym);
            }

            if (t == null)
                t = HostExpr.TagToType(tag);

            return t;
        }

        #endregion

        #region Name munging

        static readonly IPersistentMap _charMap = PersistentHashMap.create('-', "_",
            //		                         '.', "_DOT_",
             ':', "_COLON_",
             '+', "_PLUS_",
             '>', "_GT_",
             '<', "_LT_",
             '=', "_EQ_",
             '~', "_TILDE_",
             '!', "_BANG_",
             '@', "_CIRCA_",
             '#', "_SHARP_",
             '\'',"_SINGLEQUOTE_",
             '"', "_DOUBLEQUOTE_",
             '%', "_PERCENT_",
             '^', "_CARET_",
             '&', "_AMPERSAND_",
             '*', "_STAR_",
             '|', "_BAR_",
             '{', "_LBRACE_",
             '}', "_RBRACE_",
             '[', "_LBRACK_",
             ']', "_RBRACK_",
             '/', "_SLASH_",
             '\\', "_BSLASH_",
             '?', "_QMARK_"
             );

        public static IPersistentMap CHAR_MAP { get { return _charMap; } }

        static readonly public IPersistentMap DEMUNGE_MAP = CreateDemungeMap();

        private static IPersistentMap CreateDemungeMap()
        {
            // DEMUNGE_MAP maps strings to characters in the opposite
            // direction that CHAR_MAP does, plus it maps "$" to '/'

            IPersistentMap m = RT.map("$", '/');
            for (ISeq s = RT.seq(CHAR_MAP); s != null; s = s.next())
            {
                IMapEntry e = (IMapEntry)s.first();
                Char origch = (Char)e.key();
                String escapeStr = (String)e.val();
                m = m.assoc(escapeStr, origch);
            }
            return m;
        }


        private class LengthCmp : IComparer
        {
            public int Compare(object x, object y)
            {
                return ((String)y).Length - ((String)x).Length;
            }
        }
    
        static readonly public Regex DEMUNGE_PATTERN = CreateDemungePattern();

        private static Regex CreateDemungePattern()
        {
            // DEMUNGE_PATTERN searches for the first of any occurrence of
            // the strings that are keys of DEMUNGE_MAP.
            // Note: Regex matching rules mean that #"_|_COLON_" "_COLON_"
            // returns "_", but #"_COLON_|_" "_COLON_" returns "_COLON_"
            // as desired.  Sorting string keys of DEMUNGE_MAP from longest to
            // shortest ensures correct matching behavior, even if some strings are
            // prefixes of others.

            object[] mungeStrs = RT.toArray(RT.keys(DEMUNGE_MAP));
            Array.Sort(mungeStrs, new LengthCmp());
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (Object s in mungeStrs) 
            {
                String escapeStr = (String) s;
                if ( ! first )
                    sb.Append("|");
                first = false;
                sb.Append(Regex.Escape(escapeStr));
            }

            return new Regex(sb.ToString());
        }



        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static string munge(string name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                string sub = (string)_charMap.valAt(c);
                if (sub == null)
                    sb.Append(c);
                else
                    sb.Append(sub);
            }
            return sb.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static String demunge(string mungedNamed)
        {
            StringBuilder sb = new StringBuilder();
            int lastMatchEnd = 0;
            for (Match m = DEMUNGE_PATTERN.Match(mungedNamed); m.Success; m = m.NextMatch() )
            {
                int start = m.Index;

                // Keep everything before the match
                sb.Append(mungedNamed.Substring(lastMatchEnd, start-lastMatchEnd));
                lastMatchEnd = start + m.Length;
                // Replace the match with DEMUNGE_MAP result
                Char origCh = (Char)DEMUNGE_MAP.valAt(m.Groups[0].Value);
                sb.Append(origCh);
            }
            // Keep everything after the last match
            sb.Append(mungedNamed.Substring(lastMatchEnd));
            return sb.ToString();
        }

        #endregion

        #region eval

        /// <summary>
        ///  
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        /// <remarks>Initial lowercase for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static object eval(object form)
        {
            IPersistentMap meta = RT.meta(form);
            object line = (meta != null ? meta.valAt(RT.LineKey,LineVarDeref()) : LineVarDeref());
            object column = (meta != null ? meta.valAt(RT.ColumnKey, ColumnVarDeref()) : ColumnVarDeref());
            object sourceSpan = (meta != null ? meta.valAt(RT.SourceSpanKey, SourceSpanVar.deref()) : SourceSpanVar.deref());

            IPersistentMap bindings = RT.mapUniqueKeys(LineVar, line, ColumnVar, column, SourceSpanVar, sourceSpan, CompilerContextVar, null);
            if ( meta != null )
            {
                object eval_file = meta.valAt(RT.EvalFileKey);
                if ( eval_file != null )
                {
                    bindings = bindings.assoc(SourcePathVar, eval_file);
                    try
                    {
                        bindings  = bindings.assoc(SourceVar,new FileInfo((string)eval_file).Name);
                    }
                    catch (Exception)
                    {
                    }
                }

            }

            ParserContext pconExpr = new ParserContext(RHC.Expression);
            ParserContext pconEval = new ParserContext(RHC.Eval);

            Var.pushThreadBindings(bindings);
            try
            {
                form = Macroexpand(form);
              

                if (form is ISeq && Util.equals(RT.first(form), DoSym))
                {
                    ISeq s = RT.next(form);
                    for (; RT.next(s) != null; s = RT.next(s))
                        eval(RT.first(s));
                    return eval(RT.first(s));
                }
                else if ( (form is IType) ||
                    (form is IPersistentCollection && !(RT.first(form) is Symbol symbol && symbol.Name.StartsWith("def"))))
                {
                    ObjExpr objx = (ObjExpr)Analyze(pconExpr, RT.list(FnSym, PersistentVector.EMPTY, form), "eval" + RT.nextID());
                    IFn fn = (IFn)objx.Eval();
                    return fn.invoke();
                }
                else
                {
                    Expr expr = Analyze(pconEval, form);
                    return expr.Eval();

                }
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        #endregion

        #region Macroexpansion

        private static volatile Var MacroCheckVar = null;
        private static volatile bool MacroCheckLoading = false;
        private static readonly Object MacroCheckLock = new object();

        public static Var EnsureMacroCheck()
        {
            if ( MacroCheckVar == null)
            {
                lock(MacroCheckLock)
                {
                    if (MacroCheckVar == null)
                    {
                        MacroCheckLoading = true;
                        RT.LoadSpecCode();
                        MacroCheckVar = Var.find(Symbol.intern("clojure.spec.alpha", "macroexpand-check"));
                        MacroCheckLoading = false;
                    }
                }
            }

            return MacroCheckVar;
        }

        public static void CheckSpecs(Var v, ISeq form)
        {
            if ( RT.CHECK_SPECS && !MacroCheckLoading)
            {
                try
                {
                    EnsureMacroCheck().applyTo(RT.cons(v, RT.list(form.next())));
                }
                catch ( Exception e)
                {
                    throw new CompilerException((string)SourcePathVar.deref(), LineVarDeref(), ColumnVarDeref(), v.ToSymbol(), CompilerException.PhaseMacroSyntaxCheckKeyword, e);
                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        /// <remarks>Initial lowercase for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static object macroexpand1(object form)
        {
            return form is ISeq s
                ? MacroexpandSeq1(s)
                : form;
        }

        static object Macroexpand(object form)
        {
            object exf = macroexpand1(form);
            if (exf != form)
                return Macroexpand(exf);
            return form;
        }

        //public static Regex UnpackFnNameRE = new Regex("^(.+)/$([^_]+)(__[0-9]+)*$");
    
        public readonly static Regex FnNameSuffixRE = new Regex("__[0-9]+$");
        static String RemoveFnSuffix(string s)
        {
            while (true)
            {
                Match m = FnNameSuffixRE.Match(s);
                if (m.Success)
                    s = s.Substring(0, s.Length - m.Groups[0].Length);
                else return s;
            }
        }


        private static object MacroexpandSeq1(ISeq form)
        {
            object op = RT.first(form);

            if (IsSpecial(op))
                return form;

            // macro expansion
            Var v = IsMacro(op);
            if (v != null)
            {
                CheckSpecs(v, form);
                try
                {
                    ISeq args = RT.cons(form, RT.cons(Compiler.LocalEnvVar.get(), form.next()));
                    return v.applyTo(args);
                }
                catch (ArityException e)
                {
                    // hide the 2 extra params for a macro
                    // This simple test is used in the JVM:   if (e.Name.Equals(munge(v.ns.Name.Name) + "$" + munge(v.sym.Name)))
                    // Does not work for us because have to append a __1234 to the type name for functions in order to avoid name collisiions in the eval assembly.
                    // So we have to see if the name is of the form   namespace$name__xxxx  where the __xxxx can be repeated.
                    String reducedName = RemoveFnSuffix(e.Name);
                    if (reducedName.Equals(munge(v.ns.Name.Name) + "$" + munge(v.sym.Name)))
                    {
                        throw new ArityException(e.Actual - 2, e.Name);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (CompilerException)
                {
                    throw;
                }
                // in C# 6, could use ...  catch ( Exception ex )   when (e is ArgumentException || e is InvalidOperationException || e is ExceptionInfo e)
                catch (Exception e)
                {

                    if (e is ArgumentException || e is InvalidOperationException || e is ExceptionInfo)
                    {
                        throw new CompilerException((String)SourcePathVar.deref(), LineVarDeref(), ColumnVarDeref(),
                            op is Symbol symbol ? symbol : null,
                            CompilerException.PhaseMacroSyntaxCheckKeyword,
                            e);
                    }
                    else
                    {

                        throw new CompilerException((String)SourcePathVar.deref(), LineVarDeref(), ColumnVarDeref(),
                            op is Symbol symbol ? symbol : null,
                            (e.GetType().Equals(typeof(Exception)) ? CompilerException.PhaseMacroSyntaxCheckKeyword : CompilerException.PhaseMacroExpandKeyword),
                            e);
                    }
                }
            }
            else
            {
                Symbol sym = op as Symbol;
                if (sym != null)
                {
                    string sname = sym.Name;
                    // (.substring s 2 5) => (. x substring 2 5)
                    // ns == null ensures that Class/.instanceMethod isn't expanded to . form
                    if (sname[0] == '.' && sym.Namespace == null)
                    {
                        if (form.count() < 2)
                            throw new ArgumentException("Malformed member expression, expecting (.member target ...)");
                        Symbol method = Symbol.intern(sname.Substring(1));
                        object target = RT.second(form);
                        if (HostExpr.MaybeType(target, false) != null)
                            target = ((IObj)RT.list(IdentitySym, target)).withMeta(RT.map(RT.TagKey, ClassSym));
                        // We need to make sure source information gets transferred
                        return MaybeTransferSourceInfo(PreserveTag(form, RT.listStar(DotSym, target, method, form.next().next())), form);
                    }
                    //else if (NamesStaticMember(sym))
                    //{
                    //    Symbol target = Symbol.intern(sym.Namespace);
                    //    Type t = HostExpr.MaybeType(target, false);
                    //    if (t != null)
                    //    {
                    //        Symbol method = Symbol.intern(sym.Name);
                    //        // We need to make sure source information gets transferred
                    //        return MaybeTransferSourceInfo(PreserveTag(form, RT.listStar(Compiler.DotSym, target, method, form.next())), form);
                    //    }
                    //}
                    else
                    {
                        // (x.substring 2 5) =>  (. x substring 2 5)
                        // also (package.class.name ... ) (. package.class name ... )
                        int index = sname.LastIndexOf('.');
                        if (index == sname.Length - 1)
                            // We need to make sure source information gets transferred
                            return MaybeTransferSourceInfo(RT.listStar(Compiler.NewSym, Symbol.intern(sname.Substring(0, index)), form.next()), form);
                    }
                }

            }
            return form;
        }

        private static Var IsMacro(Object op)
        {
            Symbol opAsSym = op as Symbol;

            if (opAsSym != null && ReferenceLocal(opAsSym) != null)
                return null;

            Var opAsVar = op as Var;

            if (opAsSym != null || opAsVar != null)
            {
                Var v = opAsVar ??  LookupVar(opAsSym, false,false);
                if (v != null && v.IsMacro)
                {
                    if (v.Namespace != CurrentNamespace && !v.isPublic)
                        throw new InvalidOperationException(string.Format("Var: {0} is not public", v));
                    return v;
                }
            }
            return null;
        }

        private static IFn IsInline(object op, int arity)
        {
            // Java:  	//no local inlines for now

            Symbol opAsSymbol = op as Symbol;
            if (opAsSymbol != null && ReferenceLocal(opAsSymbol) != null)
                return null;

            Var opAsVar = op as Var;
            if (opAsSymbol != null || opAsVar != null)
            {
                Var v = opAsVar ?? LookupVar(opAsSymbol, false);
                if (v != null)
                {
                    if (v.Namespace != CurrentNamespace && !v.isPublic)
                        throw new InvalidOperationException("var: " + v + " is not public");
                    IFn ret = (IFn)RT.get(v.meta(), InlineKeyword);
                    if (ret != null)
                    {
                        IFn arityPred = (IFn)RT.get(v.meta(), InlineAritiesKeyword);
                        if (arityPred == null || RT.booleanCast(arityPred.invoke(arity)))
                            return ret;
                    }
                }
            }
            return null;
        }

        static object MaybeTransferSourceInfo(object newForm, object oldForm)
        {
            if (!(newForm is IObj newObj))
                return newForm;

            if (!(oldForm is IObj oldObj))
                return newForm;

            IPersistentMap oldMeta = oldObj.meta();
            if (oldMeta == null)
                return newForm;

            IPersistentMap spanMap = (IPersistentMap)oldMeta.valAt(RT.SourceSpanKey);
            if (spanMap != null)
            {
                IPersistentMap newMeta = newObj.meta();
                if (newMeta == null)
                    newMeta = RT.map();

                newMeta = newMeta.assoc(RT.SourceSpanKey, spanMap);

                return newObj.withMeta(newMeta);
            }

            return newForm;
        }

        static object PreserveTag(ISeq src, object dst)
        {
            Symbol tag = TagOf(src);
            if (tag != null )
            {
                if (dst is IObj iobj)
                {
                    IPersistentMap meta = iobj.meta();
                    return iobj.withMeta((IPersistentMap)RT.assoc(meta, RT.TagKey, tag));
                }
            }
            return dst;
        }

        internal static Type[] GetTypes(ParameterInfo[] ps)
        {
            Type[] ts = new Type[ps.Length];
            for (int i = 0; i < ps.Length; i++)
                ts[i] = ps[i].ParameterType;
            return ts;
        }

        static readonly Dictionary<Type, Symbol> TypeToTagDict = new Dictionary<Type, Symbol>()
        {
            { typeof(bool), Symbol.create(null,"bool") },
            { typeof(char), Symbol.create(null,"char") },
            { typeof(byte), Symbol.create(null,"byte") },
            { typeof(sbyte), Symbol.create(null,"sbyte") },
            { typeof(short), Symbol.create(null,"short") },
            { typeof(ushort), Symbol.create(null,"ushort") },
            { typeof(int), Symbol.create(null,"int") },
            { typeof(uint), Symbol.create(null,"uint") },
            { typeof(long), Symbol.create(null,"long") },
            { typeof(ulong), Symbol.create(null,"ulong") },
            { typeof(float), Symbol.create(null,"float") },
            { typeof(double), Symbol.create(null,"double") },
        };

        internal static Symbol TagOf(object o)
        {
            object tag = RT.get(RT.meta(o), RT.TagKey);

            {
                Symbol sym = tag as Symbol;
                if (sym != null)
                    return sym;
            }

            {
                if (tag is String str)
                    return Symbol.intern(null, str);
            }

            {
                Type t = tag as Type;
                if (t != null && TypeToTagDict.TryGetValue(t, out Symbol sym))
                {
                    return sym;
                }
            }

            return null;
        }

        internal static bool NamesStaticMember(Symbol sym)
        {
            return sym.Namespace != null && namespaceFor(sym) == null;
        }

        #endregion

        #region Compilation

        public static int GetLineFromSpanMap(IPersistentMap spanMap)
        {
            if (spanMap == null )
                return 0;

            if (GetLocation(spanMap, RT.StartLineKey, out int line))
                return line;

            return 0;
        }


        public static int GetColumnFromSpanMap(IPersistentMap spanMap)
        {
            if (spanMap == null)
                return 0;

            if (GetLocation(spanMap, RT.StartColumnKey, out int line))
                return line;

            return 0;
        }

        static bool GetLocation(IPersistentMap spanMap, Keyword key, out int val)
        {
            object oval = spanMap.valAt(key);
            if (oval != null && oval is int ioval)
            {
                val = ioval;
                return true;
            }
            val = -1;
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "not actually redundant -- rule checker fail")]
        public static bool GetLocations(IPersistentMap spanMap, out int startLine, out int startCol, out int finishLine, out int finishCol)
        {
            startLine = -1;
            startCol = -1;
            finishLine = -1;
            finishCol = -1;

            return GetLocation(spanMap, RT.StartLineKey, out startLine)
                && GetLocation(spanMap, RT.StartColumnKey, out startCol)
                && GetLocation(spanMap, RT.EndLineKey, out finishLine)
                && GetLocation(spanMap, RT.EndColumnKey, out finishCol);
        }

        static GenContext CreateEvalContext(string name, bool createDynInitHelper)
        {
            GenContext c = GenContext.CreateWithInternalAssembly(name, createDynInitHelper);
            //GenContext c = GenContext.CreateWithExternalAssembly(name, ".dll", true);  // for debugging use with SaveEvalContext
            return c;
        }

        static GenContext _evalContext = CreateEvalContext("eval", false);
        static public GenContext EvalContext { get { return _evalContext; } }

        static int _saveId = 0;
        public static void SaveEvalContext()
        {
            _evalContext.SaveAssembly();
            _evalContext = CreateEvalContext("eval" + (_saveId++).ToString(), false);
        }

        public static bool IsCompiling
        {
            get { return RT.booleanCast(CompilerActiveVar.deref()); }
        }

        public static bool IsCompilingDefType
        {
            get { return RT.booleanCast(CompilingDefTypeVar.deref()); }
        }

        public static string IsCompilingSuffix()
        {
            GenContext context = (GenContext)CompilerContextVar.deref();
            return context == null ? "_INTERP" : "_COMP_" + (new AssemblyName(context.AssemblyBuilder.FullName)).Name;
        }

        internal static string InitClassName(string sourcePath)
        {
            return "__Init__$" + sourcePath.Replace(".", "/").Replace("/", "$");
        }
        
        public static void PushNS()
        {
            Var.pushThreadBindings(PersistentHashMap.create(Var.intern(Symbol.intern("clojure.core"),
                                                                       Symbol.intern("*ns*")).setDynamic(), null,
                                                                       RT.ReadEvalVar, true /* RT.T */));
        }

        public static object Compile(TextReader rdr, string sourceDirectory, string sourceName, string relativePath)
        {
            if (CompilePathVar.deref() == null)
                throw new InvalidOperationException("*compile-path* not set");

             string sourcePath = relativePath;
            GenContext context = GenContext.CreateWithExternalAssembly(sourceName, sourcePath, ".dll", true);

            Compile(context, rdr, sourceDirectory, sourceName, relativePath);

            context.SaveAssembly();

            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        public static object Compile(GenContext context,TextReader rdr, string sourceDirectory, string sourceName, string relativePath)
        {
            object eofVal = new object();
            object form;

            string sourcePath = relativePath;

            // generate loader class
            ObjExpr objx = new ObjExpr(null);

            var internalName = sourcePath.Replace(Path.PathSeparator, '/');
                
            {
                int lastDotIndex = sourcePath.LastIndexOf('.');
                if ( lastDotIndex > -1 )
                    internalName = internalName.Substring(0, lastDotIndex); 
            }
                
            objx.InternalName = internalName + "__init";

            TypeBuilder initTB = context.AssemblyGen.DefinePublicType(InitClassName(internalName), typeof(object), true);
            context = context.WithTypeBuilder(initTB);

            // static load method
            MethodBuilder initMB = initTB.DefineMethod("Initialize", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            CljILGen ilg = new CljILGen(initMB.GetILGenerator());

            LineNumberingTextReader lntr = rdr as LineNumberingTextReader ?? new LineNumberingTextReader(rdr);

            Var.pushThreadBindings(RT.mapUniqueKeys(
                SourcePathVar, relativePath,
                SourceVar, sourceName,
                MethodVar, null,
                LocalEnvVar, null,
                LoopLocalsVar, null,
                NextLocalNumVar, 0,
                RT.ReadEvalVar, true /* RT.T */,
                RT.CurrentNSVar, RT.CurrentNSVar.deref(),
                ConstantsVar, PersistentVector.EMPTY,
                ConstantIdsVar, new IdentityHashMap(),
                KeywordsVar, PersistentHashMap.EMPTY,
                VarsVar, PersistentHashMap.EMPTY,
                RT.UncheckedMathVar, RT.UncheckedMathVar.deref(),
                RT.WarnOnReflectionVar, RT.WarnOnReflectionVar.deref(),
                RT.DataReadersVar, RT.DataReadersVar.deref(),
                CompilerContextVar, context,
                CompilerActiveVar, true
                ));

            try
            {
                Object readerOpts = ReaderOpts(sourceName);

                while ((form = LispReader.read(lntr, false, eofVal, false, readerOpts)) != eofVal)
                {
                    Compile1(initTB, ilg, objx, form);
                }

                initMB.GetILGenerator().Emit(OpCodes.Ret);

                // static fields for constants
                objx.EmitConstantFieldDefs(initTB);
                MethodBuilder constInitsMB = objx.EmitConstants(initTB);

                // Static init for constants, keywords, vars
                ConstructorBuilder cb = initTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
                ILGenerator cbGen = cb.GetILGenerator();

                cbGen.BeginExceptionBlock();

                cbGen.Emit(OpCodes.Call,Method_Compiler_PushNS);
                cbGen.Emit(OpCodes.Call, constInitsMB);

                cbGen.BeginFinallyBlock();
                cbGen.Emit(OpCodes.Call, Method_Var_popThreadBindings);

                cbGen.EndExceptionBlock();
                cbGen.Emit(OpCodes.Ret);

                var descAttrBuilder =
                 new CustomAttributeBuilder(typeof (DescriptionAttribute).GetConstructor(new[] {typeof (String)}),
                                           new [] {String.Format("{{:clojure-namespace {0}}}", CurrentNamespace)});
                initTB.SetCustomAttribute(descAttrBuilder);

                initTB.CreateType();
            }
            catch (LispReader.ReaderException e)
            {
                throw new CompilerException(sourcePath, e.Line,  e.Column, e.InnerException);
            }
            finally
            {
                Var.popThreadBindings();
            }
            return null;
        }


        private static void Compile1(TypeBuilder tb, CljILGen ilg,  ObjExpr objx, object form)
        {
            object line = LineVarDeref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LineKey))
                line = RT.meta(form).valAt(RT.LineKey);
            object column = ColumnVarDeref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.ColumnKey))
                column = RT.meta(form).valAt(RT.ColumnKey);
            IPersistentMap sourceSpan = (IPersistentMap)SourceSpanVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SourceSpanKey))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SourceSpanKey);

            ParserContext evPC = new ParserContext(RHC.Eval);
 
            Var.pushThreadBindings(RT.map(LineVar, line, ColumnVar, column, SourceSpanVar, sourceSpan));

            try
            {
                form = Macroexpand(form);
                if (form is ISeq && Util.Equals(RT.first(form), DoSym))
                {
                    for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                        Compile1(tb, ilg, objx, RT.first(s));
                }
                else
                {
                    Expr expr = Analyze(evPC, form);
                    objx.Keywords = (IPersistentMap)KeywordsVar.deref();
                    objx.Vars = (IPersistentMap)VarsVar.deref();
                    objx.Constants = (PersistentVector)ConstantsVar.deref();
                    objx.EmitConstantFieldDefs(tb);
                    expr.Emit(RHC.Expression,objx,ilg);
                    ilg.Emit(OpCodes.Pop);
                    expr.Eval();
                }
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        #endregion
        
        #region Loading

        internal static void LoadAssembly(FileInfo assyInfo, string relativePath)
        {
            Assembly assy;

            try
            {
                assy = Assembly.LoadFrom(assyInfo.FullName);
            }
            catch (IOException e)
            {
                throw new AssemblyNotFoundException(e.Message,e);
            }
            catch (ArgumentException e)
            {
                throw new AssemblyNotFoundException(e.Message,e);
            }
            catch (BadImageFormatException e)
            {
                throw new AssemblyNotFoundException(e.Message, e);
            }
            catch (System.Security.SecurityException e)
            {
                throw new AssemblyNotFoundException(e.Message, e);
            }

            InitAssembly(assy, relativePath);
        }

        internal static void LoadAssembly(byte[] assyData, string relativePath)
        {
            Assembly assy;
            try
            {
                assy = Assembly.Load(assyData);
            }
            catch (ArgumentException e)
            {
                throw new AssemblyNotFoundException(e.Message, e);
            }
            catch (BadImageFormatException e)
            {
                throw new AssemblyNotFoundException(e.Message, e);
            }

            InitAssembly(assy, relativePath);
        }


        private static Type GetTypeFromAssy(Assembly assy, string typeName)
        {
            if (RT.IsRunningOnMono)
            {
                // I have no idea why Mono can't find our initializer types using Assembly.GetType(string).
                // This is roll-your-own.
                Type[] types = assy.GetExportedTypes();
                foreach (Type t in types)
                {
                    if (t.Name.Equals(typeName))
                        return t;
                }
                return null;
            }
            else
                return assy.GetType(typeName);
        }

        private static void InitAssembly(Assembly assy, string relativePath)
        {
            Type initType = GetTypeFromAssy(assy,InitClassName(relativePath));
            if (initType == null)
            {
                initType = GetTypeFromAssy(assy, "__Init__"); // old init class name
                if (initType == null)
                {
                    throw new AssemblyInitializationException(String.Format("Cannot find initializer for {0}.{1}",assy.FullName,relativePath));
                }
            }
            InvokeInitType(assy, initType);
        }

        private static void InvokeInitType(Assembly assy, Type initType)
        {
            try
            {
                initType.InvokeMember("Initialize", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, Type.DefaultBinder, null, new object[0]);
            }
            catch (Exception e)
            {
                throw new AssemblyInitializationException(String.Format("Error initializing {0}: {1}", assy.FullName, e.Message),e);
            }
        }

        internal static bool TryLoadInitType(string relativePath)
        {
            var initClassName = InitClassName(relativePath);
            Type initType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic)
                    continue;
                initType = asm.GetType(initClassName);
                if (initType != null)
                    break;
            }
            if (initType == null)
                return false;

            InvokeInitType(initType.Assembly, initType);
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static object loadFile(string fileName)
        {
            FileInfo finfo = new FileInfo(fileName);
            if (!finfo.Exists)
                throw new FileNotFoundException($"Cannot find file to load: {fileName}", fileName);

            using (TextReader rdr = finfo.OpenText())
                return load(rdr, finfo.FullName, finfo.Name, fileName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static object load(TextReader rdr)
        {
            return load(rdr, null, "NO_SOURCE_FILE", null);  // ?
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static object load(TextReader rdr, string relativePath)
        {
            return load(rdr, null, "NO_SOURCE_FILE", relativePath);  // ?
        }

        public delegate object ReplDelegate();

        static void ConsumeWhitespaces(LineNumberingTextReader lnReader)
        {
            int ch = lnReader.Read();
            while (LispReader.isWhitespace(ch))
                ch = lnReader.Read();
            LispReader.Unread(lnReader, ch);
        }

        static readonly Object OPTS_COND_ALLOWED = RT.mapUniqueKeys(LispReader.OPT_READ_COND, LispReader.COND_ALLOW);

        static Object ReaderOpts(string sourceName)
        {
            if (sourceName != null && sourceName.EndsWith(".cljc"))
                return OPTS_COND_ALLOWED;
            else
                return null;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        public static object load(TextReader rdr, string sourcePath, string sourceName, string relativePath)
        {
            object ret = null;
            object eofVal = new object();
            object form;

            LineNumberingTextReader lntr = rdr as LineNumberingTextReader ?? new LineNumberingTextReader(rdr);

            ConsumeWhitespaces(lntr);

            Var.pushThreadBindings(RT.mapUniqueKeys(

                //LOADER, RT.makeClassLoader(),
                SourcePathVar, relativePath,
                SourceVar, sourceName,
                MethodVar, null,
                LocalEnvVar, null,
                LoopLocalsVar, null,
                NextLocalNumVar, 0,
                RT.ReadEvalVar, true /* RT.T */,
                RT.CurrentNSVar, RT.CurrentNSVar.deref(),
                RT.UncheckedMathVar, RT.UncheckedMathVar.deref(),
                RT.WarnOnReflectionVar, RT.WarnOnReflectionVar.deref(),
                RT.DataReadersVar, RT.DataReadersVar.deref(),
                CompilerContextVar, EvalContext,
                CompilerActiveVar, false
                //LINE_BEFORE, lntr.LineNumber,
                //LINE_AFTER, lntr.LineNumber,
                //COLUMN_BEFORE, lntr.ColumnNumber,
                //COLUMN_AFTER, lntr.ColumnNumber
                ));

            Object readerOpts = ReaderOpts(sourceName);

            int lineBefore = lntr.LineNumber;
            int columnBefore = lntr.ColumnNumber;
            //int lineAfter = lntr.LineNumber;
            //int columnAfter = lntr.ColumnNumber;
            try
            {
                while ((form = LispReader.read(lntr, false, eofVal, false, readerOpts)) != eofVal)
                {
                    ConsumeWhitespaces(lntr);
                    //LINE_AFTER.set(lntr.LineNumber);
                    //COLUMN_AFTER.set(lntr.ColumnNumber);
                    //lineAfter = lntr.LineNumber;
                    //columnAfter = lntr.ColumnNumber;
                    ret = eval(form);
                    //LINE_BEFORE.set(lntr.LineNumber);
                    //COLUMN_BEFORE.set(lntr.ColumnNumber);
                    lineBefore = lntr.LineNumber;
                    columnBefore = lntr.ColumnNumber;
                }
            }
            catch (LispReader.ReaderException e)
            {
                throw new CompilerException(sourcePath, e.Line, e.Column, null, CompilerException.PhaseReadKeyword, e.InnerException);
            }
            catch (CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CompilerException(sourcePath, lineBefore, columnBefore, null, CompilerException.PhaseExecutionKeyword, e);
            }
            finally
            {
                Var.popThreadBindings();
            }

            return ret;
        }

        #endregion

        #region Form analysis

        internal static LiteralExpr NilExprInstance = new NilExpr();
        internal static LiteralExpr TrueExprInstance = new BooleanExpr(true);
        internal static LiteralExpr FalseExprInstance = new BooleanExpr(false);

        public static Expr Analyze(ParserContext pcontext, object form)
        {
            return Analyze(pcontext, form, null);
        }

        public static Expr Analyze(ParserContext pcontext, object form, string name)
        {
            try
            {
                if (form is LazySeq)
                {
                    object mform = form;
                    form = RT.seq(form);
                    if (form == null)
                        form = PersistentList.EMPTY;
                    form = ((IObj)form).withMeta(RT.meta(mform));
                }
                if (form == null)
                    return NilExprInstance;
                else if (form is Boolean)
                    return ((bool)form) ? TrueExprInstance : FalseExprInstance;

                Type type = form.GetType();

                if (type == typeof(Symbol))
                    return AnalyzeSymbol((Symbol)form);
                else if (type == typeof(Keyword))
                    return RegisterKeyword((Keyword)form);
                else if (Util.IsNumeric(form))
                    return NumberExpr.Parse(form);
                else if (type == typeof(String))
                    return new StringExpr(String.Intern((String)form));
                else if (form is IPersistentCollection collection
                    && ! (form is IRecord)
                    && ! (form is IType)
                    && collection.count() == 0)
                    return OptionallyGenerateMetaInit(pcontext, form, new EmptyExpr(form));
                else if (form is ISeq seq)
                    return AnalyzeSeq(pcontext, seq, name);
                else if (form is IPersistentVector vector)
                    return VectorExpr.Parse(pcontext, vector);
                else if (form is IRecord)
                    return new ConstantExpr(form);
                else if (form is IType)
                    return new ConstantExpr(form);
                else if (form is IPersistentMap map)
                    return MapExpr.Parse(pcontext, map);
                else if (form is IPersistentSet set)
                    return SetExpr.Parse(pcontext, set);
                else
                    return new ConstantExpr(form);
            }
            catch (CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CompilerException((String)SourcePathVar.deref(), LineVarDeref(), ColumnVarDeref(), e);
            }
        }

        internal static Expr OptionallyGenerateMetaInit(ParserContext pcon, object form, Expr expr)
        {
            Expr ret = expr;

            if ( RT.meta(form) != null )
                ret = new MetaExpr(ret, (MapExpr)MapExpr.Parse(pcon.EvalOrExpr(),((IObj)form).meta()));

            return ret;
        }

        private static Expr AnalyzeSymbol(Symbol symbol)
        {
            Symbol tag = TagOf(symbol);

            if (symbol.Namespace == null) // ns-qualified syms are always Vars
            {
                LocalBinding b = ReferenceLocal(symbol);
                if (b != null)
                    return new LocalBindingExpr(b, tag);
            }
            else
            {
                if (namespaceFor(symbol) == null && !Util.IsPosDigit(symbol.Name))
                {
                    Symbol nsSym = Symbol.intern(symbol.Namespace);
                    Type t = HostExpr.MaybeType(nsSym, false);
                    if (t != null)
                    {
                        FieldInfo finfo;
                        PropertyInfo pinfo;

                        if ((finfo = Reflector.GetField(t, symbol.Name, true)) != null)
                            return new StaticFieldExpr((string)SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), tag, t, symbol.Name, finfo);
                        else if ((pinfo = Reflector.GetProperty(t, symbol.Name, true)) != null)
                            return new StaticPropertyExpr((string)SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), tag, t, symbol.Name, pinfo);
                        else return new QualifiedMethodExpr(t, symbol);
                    }
                    //throw new InvalidOperationException(string.Format("Unable to find static field: {0} in {1}", symbol.Name, t));
                }
            }

            object o = Compiler.Resolve(symbol);

           Symbol oAsSymbol;

            if (o is Var oAsVar)
            {
                if (IsMacro(oAsVar) != null)
                    throw new InvalidOperationException("Can't take the value of a macro: " + oAsVar);
                if (RT.booleanCast(RT.get(oAsVar.meta(), RT.ConstKey)))
                    return Analyze(new ParserContext(RHC.Expression), RT.list(QuoteSym, oAsVar.get()));
                RegisterVar(oAsVar);
                return new VarExpr(oAsVar, tag);
            }
            else if (o is Type)
                return new ConstantExpr(o);
            else if ((oAsSymbol = o as Symbol) != null)
                return new UnresolvedVarExpr(oAsSymbol);

            throw new InvalidOperationException(string.Format("Unable to resolve symbol: {0} in this context", symbol));
        }

        internal static Expr AnalyzeSeq(ParserContext pcon, ISeq form, string name )
        {
            object line = LineVarDeref();
            object column = ColumnVarDeref();
            IPersistentMap sourceSpan = (IPersistentMap)SourceSpanVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LineKey))
                line = RT.meta(form).valAt(RT.LineKey);
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.ColumnKey))
                column = RT.meta(form).valAt(RT.ColumnKey);
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SourceSpanKey))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SourceSpanKey);

            Var.pushThreadBindings(RT.map(LineVar, line, ColumnVar, column, SourceSpanVar, sourceSpan));
            Object op = null;
            try
            {

                object me = MacroexpandSeq1(form);
                if (me != form)
                    return Analyze(pcon, me, name);

                op = RT.first(form);
                if (op == null)
                    throw new ArgumentNullException("form", "Can't call nil");

                IFn inline = IsInline(op, RT.count(RT.next(form)));

                if (inline != null)
                    return Analyze(pcon, MaybeTransferSourceInfo(PreserveTag(form, inline.applyTo(RT.next(form))), form));

                IParser p;
                if (op.Equals(FnSym))
                    return FnExpr.Parse(pcon, form, name);
                if ((p = GetSpecialFormParser(op)) != null)
                    return p.Parse(pcon, form);
                else
                    return InvokeExpr.Parse(pcon, form);
            }
            catch (CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                Symbol s = (op != null && op is Symbol symbol) ? symbol : null;
                throw new CompilerException((String)SourcePathVar.deref(), LineVarDeref(), ColumnVarDeref(), s, e);
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        internal static bool InTailCall(RHC context)
        {
            return (context == RHC.Return) && (MethodReturnContextVar.deref() != null) &&  (InTryBlockVar.deref() == null);
        }

        #endregion

        #region CompilerException

        [Serializable]
        public sealed class CompilerException : Exception, IExceptionInfo
        {
            #region static constants

            // Error keys
            public static readonly String ErrorNamespaceStr = "clojure.error";
            public static readonly Keyword ErrorSourceKeyword = Keyword.intern(ErrorNamespaceStr, "source");
            public static readonly Keyword ErrorLineKeyword = Keyword.intern(ErrorNamespaceStr, "line");
            public static readonly Keyword ErrorColumnKeyword = Keyword.intern(ErrorNamespaceStr, "column");
            public static readonly Keyword ErrorPhaseKeyword = Keyword.intern(ErrorNamespaceStr, "phase");
            public static readonly Keyword ErrorSymbolKeyword = Keyword.intern(ErrorNamespaceStr, "symbol");

           // Compile error phases
            public static readonly Keyword PhaseReadKeyword = Keyword.intern(null, "read-source");
            public static readonly Keyword PhaseMacroSyntaxCheckKeyword = Keyword.intern(null, "macro-syntax-check");
            public static readonly Keyword PhaseMacroExpandKeyword = Keyword.intern(null, "macroexpand");
            public static readonly Keyword PhaseCompileSyntaxCheckKeyword = Keyword.intern(null, "compile-syntax-check"); 
            public static readonly Keyword PhaseCompilationKeyword = Keyword.intern(null, "compilation");
            public static readonly Keyword PhaseExecutionKeyword = Keyword.intern(null, "execution");

            public static readonly Keyword SpecProblemsKeyword = Keyword.intern("clojure.spec.alpha", "problems");

            #endregion

            #region data

            public string FileSource { get; private set; }
            public int Line { get; private set; }
            public IPersistentMap MyData { get; private set; }
            
            #endregion

            #region C-tors

            public CompilerException()
            {
                FileSource = "<unknown>";
            }

            public CompilerException(string message)
                : base(message)
            {
                FileSource = "<unknown>";
            }

            public CompilerException(string message, Exception innerException)
                :base(message,innerException)
            {
                FileSource = "<unknown>";
            }

            public CompilerException(string source, int line, int column, Exception cause)
                :this(source,line,column, null, cause)
            {
            }

            public CompilerException(string source, int line, int column, Symbol sym, Exception cause)
                :this(source,line,column,sym,PhaseCompileSyntaxCheckKeyword,cause)
            {
            }

            public CompilerException(String source, int line, int column, Symbol sym, Keyword phase, Exception cause)
                : base(MakeMsg(source, line, column, sym, phase, cause), cause)
            {
                FileSource = source;
                Line = line;
                Associative m = RT.map(ErrorPhaseKeyword, phase, ErrorLineKeyword, line, ErrorColumnKeyword, column);
                if (source != null) m = RT.assoc(m, ErrorSourceKeyword, source);
                if (sym != null) m = RT.assoc(m, ErrorSymbolKeyword, sym);
                MyData = (IPersistentMap)m;
            }

            private CompilerException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");

                FileSource = info.GetString("FileSource");
                Line = info.GetInt32("Line");
                MyData = (IPersistentMap)info.GetValue("MyData",typeof(IPersistentMap));
            }

            #endregion

            #region Support

            private static String Verb(Keyword phase) 
            {   
                if (PhaseReadKeyword.Equals(phase))
			        return "reading source";
                else if (PhaseCompileSyntaxCheckKeyword.Equals(phase))   
			        return "compiling";
                else 
			        return "macroexpanding";
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
            public static String MakeMsg(String source, int line, int column, Symbol sym, Keyword phase, Exception cause)
            {
                return (PhaseMacroExpandKeyword.Equals(phase) ? "Unexpected error " : "Syntax error ") +
                        Verb(phase) + " " + (sym != null ? sym + " " : "") +
                        "at (" + (source != null && !source.Equals("NO_SOURCE_PATH") ? (source + ":") : "") +
                        line + ":" + column + ").";
            }

            public override string ToString()
            {
                Exception cause = InnerException;
                if (cause != null)
                {
                    if (cause is IExceptionInfo eInfo)
                    {
                        IPersistentMap data = (IPersistentMap)eInfo.getData();
                        if (PhaseMacroSyntaxCheckKeyword.Equals(data.valAt(ErrorPhaseKeyword)) && data.valAt(SpecProblemsKeyword) != null)
                            return String.Format("{0}", Message);
                    }
                    else
                        return String.Format("{0}\n{1}", Message, cause.Message);
                }
                return Message;
            }
            
            // JVM has this deprecated
            //static string ErrorMsg(string source, int line, int column, string s)
            //{
            //    return string.Format("{0}, compiling: ({1}:{2}:{3})", s, source, line,column);
            //}

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new System.ArgumentNullException("info");
                base.GetObjectData(info, context);
                info.AddValue("FileSource", FileSource);
                info.AddValue("Line", Line);
                info.AddValue("Data", Data);
            }

            #endregion

            public IPersistentMap getData()
            {
                return MyData;
            }
        }

        #endregion

        #region AssemblyLoadException

        [Serializable]
        public class AssemblyLoadException : Exception
        {
            #region C-tors

            public AssemblyLoadException()
            {
            }

            public AssemblyLoadException(string msg)
                : base(msg)
            {
            }

            public AssemblyLoadException(string msg, Exception innerException)
                : base(msg, innerException)
            {
            }

            protected AssemblyLoadException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            #endregion
        }

        [Serializable]
        public sealed class AssemblyNotFoundException : AssemblyLoadException
        {
            #region C-tors

            public AssemblyNotFoundException()
            {
            }

            public AssemblyNotFoundException(string msg)
                : base(msg)
            {
            }

            public AssemblyNotFoundException(string msg, Exception innerException)
                : base(msg, innerException)
            {
            }

            private AssemblyNotFoundException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            #endregion
        }

        [Serializable]
        public sealed class AssemblyInitializationException : AssemblyLoadException
        {
            #region C-tors

            public AssemblyInitializationException()
            {
            }

            public AssemblyInitializationException(string msg)
                : base(msg)
            {
            }

            public AssemblyInitializationException(string msg, Exception innerException)
                : base(msg, innerException)
            {
            }

            private AssemblyInitializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            #endregion
        }

        #endregion
    }
}
