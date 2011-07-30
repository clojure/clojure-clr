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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using clojure.lang.CljCompiler.Ast;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using System.Text.RegularExpressions;

namespace clojure.lang
{
    public static class Compiler
    {
        #region other constants

        internal const int MaxPositionalArity = 20;
        internal const string CompileStubPrefix = "compile__stub";

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

        internal static readonly Symbol InvokeStaticSym = Symbol.intern("invokeStatic");

        #endregion

        #region Keywords

        static readonly Keyword InlineKeyword = Keyword.intern(null, "inline");
        static readonly Keyword InlineAritiesKeyword = Keyword.intern(null, "inline-arities");
        internal static readonly Keyword StaticKeyword = Keyword.intern(null, "static");
        internal static readonly Keyword ArglistsKeyword = Keyword.intern(null, "arglists");

        static readonly Keyword VolatileKeyword = Keyword.intern(null,"volatile");
        internal static readonly Keyword ImplementsKeyword = Keyword.intern(null,"implements");
        internal static readonly Keyword ProtocolKeyword = Keyword.intern(null,"protocol");
        static readonly Keyword OnKeyword = Keyword.intern(null, "on");
        internal static readonly Keyword DynamicKeyword = Keyword.intern("dynamic");


        #endregion

        #region Vars

        //boolean
        internal static readonly Var CompileFilesVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                 //Symbol.intern("*compile-files*"), RT.F);
                                                         Symbol.intern("*compile-files*"), false).setDynamic();  
        //JAVA: Boolean.FALSE -- changed from RT.F in rev 1108, not sure why


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
        internal static readonly Var LineVar = Var.create(0).setDynamic();          // From the JVM version
        //internal static readonly Var LINE_BEFORE = Var.create(0).setDynamic();   // From the JVM version
        //internal static readonly Var LINE_AFTER = Var.create(0).setDynamic();    // From the JVM version
        internal static readonly Var DocumentInfoVar = Var.create(null).setDynamic();  // Mine
        internal static readonly Var SourceSpanVar = Var.create(null).setDynamic();    // Mine

        internal static readonly Var MethodVar = Var.create(null).setDynamic();
        public static readonly Var LocalEnvVar = Var.create(PersistentHashMap.EMPTY).setDynamic();
        //Integer
        internal static readonly Var NextLocalNumVar = Var.create(0).setDynamic();
        internal static readonly Var LoopLocalsVar = Var.create(null).setDynamic();
        // Label
        internal static readonly Var LoopLabelVar = Var.create().setDynamic();


        internal static readonly Var InCatchFinallyVar = Var.create(null).setDynamic();          //null or not

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

        internal static readonly Var CompilerContextVar = Var.create(null).setDynamic();

        #endregion

        #region Special forms

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "specials")]
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

        //internal static readonly PropertyInfo Method_AFunction_MethodImplCache = typeof(AFunction).GetProperty("MethodImplCache");

        //internal static readonly MethodInfo Method_ArraySeq_create = typeof(ArraySeq).GetMethod("create", BindingFlags.Static | BindingFlags.Public,null, new Type[] { typeof(object[]) }, null);

        internal static readonly PropertyInfo Method_Compiler_CurrentNamespace = typeof(Compiler).GetProperty("CurrentNamespace");
        internal static readonly MethodInfo Method_Compiler_PushNS = typeof(Compiler).GetMethod("PushNS");

        //internal static readonly MethodInfo Method_Delegate_CreateDelegate = typeof(Delegate).GetMethod("CreateDelegate", BindingFlags.Static | BindingFlags.Public,null,new Type[] {typeof(Type), typeof(Object), typeof(string)},null);

        internal static readonly MethodInfo Method_ILookupSite_fault = typeof(ILookupSite).GetMethod("fault");
        internal static readonly MethodInfo Method_ILookupThunk_get = typeof(ILookupThunk).GetMethod("get");

        internal static readonly MethodInfo Method_IPersistentMap_valAt2 = typeof(ILookup).GetMethod("valAt", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_IPersistentMap_without = typeof(IPersistentMap).GetMethod("without");

        internal static readonly MethodInfo Method_IObj_withMeta = typeof(IObj).GetMethod("withMeta");

        //internal static readonly MethodInfo Method_Keyword_intern_symbol = typeof(Keyword).GetMethod("intern", new Type[] { typeof(Symbol) });
        internal static readonly MethodInfo Method_Keyword_intern_string = typeof(Keyword).GetMethod("intern", new Type[] { typeof(String) });
        
        //internal static readonly MethodInfo Method_KeywordLookupSite_Get = typeof(KeywordLookupSite).GetMethod("Get");

        //internal static readonly MethodInfo Method_MethodImplCache_fnFor = typeof(MethodImplCache).GetMethod("fnFor");

        internal static readonly MethodInfo Method_Monitor_Enter = typeof(Monitor).GetMethod("Enter", new Type[] { typeof(Object) });
        internal static readonly MethodInfo Method_Monitor_Exit = typeof(Monitor).GetMethod("Exit", new Type[] { typeof(Object) });

        //internal static readonly MethodInfo Method_Object_ReferenceEquals = typeof(Object).GetMethod("ReferenceEquals");
        //internal static readonly MethodInfo Method_Object_MemberwiseClone = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);

        internal static readonly MethodInfo Method_Namespace_importClass1 = typeof(Namespace).GetMethod("importClass", new Type[] { typeof(Type) });

        //internal static readonly MethodInfo Method_Numbers_num_long = typeof(Numbers).GetMethod("num", new Type[] { typeof(long) });

        internal static readonly MethodInfo Method_PersistentList_create = typeof(PersistentList).GetMethod("create", new Type[] { typeof(System.Collections.IList) });
        internal static readonly MethodInfo Method_PersistentHashSet_create = typeof(PersistentHashSet).GetMethod("create", new Type[] { typeof(Object[]) });
        internal static readonly FieldInfo Method_PersistentHashSet_EMPTY = typeof(PersistentHashSet).GetField("EMPTY");

        //internal static readonly MethodInfo Method_Reflector_CallInstanceMethod = typeof(Reflector).GetMethod("CallInstanceMethod");
        //internal static readonly MethodInfo Method_Reflector_CallStaticMethod = typeof(Reflector).GetMethod("CallStaticMethod");
        //internal static readonly MethodInfo Method_Reflector_InvokeConstructor = typeof(Reflector).GetMethod("InvokeConstructor");
        internal static readonly MethodInfo Method_Reflector_GetInstanceFieldOrProperty = typeof(Reflector).GetMethod("GetInstanceFieldOrProperty");
        internal static readonly MethodInfo Method_Reflector_SetInstanceFieldOrProperty = typeof(Reflector).GetMethod("SetInstanceFieldOrProperty");

        //internal static readonly MethodInfo Method_RT_arrayToList = typeof(RT).GetMethod("arrayToList");
        internal static readonly MethodInfo Method_RT_classForName = typeof(RT).GetMethod("classForName");
        internal static readonly MethodInfo Method_RT_intCast_long = typeof(RT).GetMethod("intCast", new Type[] { typeof(long) });
        internal static readonly MethodInfo Method_RT_uncheckedIntCast_long = typeof(RT).GetMethod("uncheckedIntCast", new Type[] { typeof(long) });
        //internal static readonly MethodInfo Method_RT_IsTrue = typeof(RT).GetMethod("IsTrue");
        internal static readonly MethodInfo Method_RT_keyword = typeof(RT).GetMethod("keyword");
        internal static readonly MethodInfo Method_RT_map = typeof(RT).GetMethod("map");
        internal static readonly MethodInfo Method_RT_seqOrElse = typeof(RT).GetMethod("seqOrElse");
        internal static readonly MethodInfo Method_RT_set = typeof(RT).GetMethod("set");
        internal static readonly MethodInfo Method_RT_vector = typeof(RT).GetMethod("vector");
        internal static readonly MethodInfo Method_RT_readString = typeof(RT).GetMethod("readString");
        internal static readonly MethodInfo Method_RT_var2 = typeof(RT).GetMethod("var", new Type[] { typeof(string), typeof(string) });

        internal static readonly MethodInfo Method_Symbol_intern2 = typeof(Symbol).GetMethod("intern", new Type[] { typeof(string), typeof(string) });

        internal static readonly MethodInfo Method_Util_classOf = typeof(Util).GetMethod("classOf");
        internal static readonly MethodInfo Method_Util_ConvertToInt = typeof(Util).GetMethod("ConvertToInt");

        //internal static readonly MethodInfo Method_Util_equals = typeof(Util).GetMethod("equals", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_Util_equiv = typeof(Util).GetMethod("equiv", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_Util_hash = typeof(Util).GetMethod("hash");
        internal static readonly MethodInfo Method_Util_IsNonCharNumeric = typeof(Util).GetMethod("IsNonCharNumeric");
        
        internal static readonly MethodInfo Method_Var_bindRoot = typeof(Var).GetMethod("bindRoot");
        internal static readonly MethodInfo Method_Var_get = typeof(Var).GetMethod("deref");
        internal static readonly MethodInfo Method_Var_set = typeof(Var).GetMethod("set");
        internal static readonly MethodInfo Method_Var_setMeta = typeof(Var).GetMethod("setMeta");
        internal static readonly MethodInfo Method_Var_popThreadBindings = typeof(Var).GetMethod("popThreadBindings");
        //internal static readonly MethodInfo Method_Var_hasRoot = typeof(Var).GetMethod("hasRoot");
        internal static readonly MethodInfo Method_Var_getRawRoot = typeof(Var).GetMethod("getRawRoot");
        //internal static readonly MethodInfo Method_Var_getRoot = typeof(Var).GetMethod("getRoot");
        internal static readonly MethodInfo Method_Var_setDynamic0 = typeof(Var).GetMethod("setDynamic", Type.EmptyTypes);
        //internal static readonly PropertyInfo Method_Var_Rev = typeof(Var).GetProperty("Rev");

        internal static readonly ConstructorInfo Ctor_KeywordLookupSite_1 = typeof(KeywordLookupSite).GetConstructor(new Type[] { typeof(Keyword) });
        internal static readonly ConstructorInfo Ctor_Regex_1 = typeof(Regex).GetConstructor(new Type[] { typeof(String) });
        internal static readonly ConstructorInfo Ctor_RestFnImpl_1 = typeof(RestFnImpl).GetConstructor(new Type[] { typeof(int) });

        internal static readonly ConstructorInfo Ctor_Serializable = typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes);

        internal static readonly MethodInfo[] Methods_IFn_invoke = new MethodInfo[MaxPositionalArity + 2];

        internal static Type[] CreateObjectTypeArray(int size)
        {
            Type[] typeArray = new Type[size];
            for (int i = 0; i < size; i++)
                typeArray[i] = typeof(Object);
            return typeArray;
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
        }

        #endregion

        #region Symbol/namespace resolving

        // TODO: we have duplicate code below.

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resolve")]
        public static Symbol resolveSymbol(Symbol sym)
        {
            //already qualified or classname?
            if (sym.Name.IndexOf('.') > 0)
                return sym;

            if (sym.Namespace != null)
            {
                Namespace ns = namespaceFor(sym);
                if (ns == null || ns.Name.Name == sym.Namespace)
                    return sym;
                return Symbol.intern(ns.Name.Name, sym.Name);
            }

            Object o = CurrentNamespace.GetMapping(sym);
            if (o == null)
                return Symbol.intern(CurrentNamespace.Name.Name, sym.Name);

            Type ot = o as Type;
             if (ot != null)
                return Symbol.intern(null, Util.NameForType(ot));

             Var ov = o as Var;
             if (ov != null)

                return Symbol.intern(ov.Namespace.Name.Name, ov.Symbol.Name);

            return null;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "namespace")]
        public static Namespace namespaceFor(Symbol sym)
        {
            return namespaceFor(CurrentNamespace, sym);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "namespace")]
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

        public static string DestubClassName(String className)
        {
            //skip over prefix + '.' or '/'
            if (className.StartsWith(CompileStubPrefix))
                return className.Substring(CompileStubPrefix.Length + 1);
            return className;
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
                Namespace ns = NamespaceFor(n, symbol);
                if (ns == null)
                    throw new Exception("No such namespace: " + symbol.Namespace);

                Var v = ns.FindInternedVar(Symbol.intern(symbol.Name));
                if (v == null)
                    throw new Exception("No such var: " + symbol);
                else if (v.Namespace != CurrentNamespace && !v.IsPublic && !allowPrivate)
                    throw new InvalidOperationException(string.Format("var: {0} is not public", symbol));
                return v;
            }
            else if (symbol.Name.IndexOf('.') > 0 || symbol.Name[symbol.Name.Length - 1] == ']')
                return RT.classForName(symbol.Name);
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
                        throw new Exception(string.Format("Unable to resolve symbol: {0} in this context", symbol));
                }
                return o;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "maybe")]
        public static object maybeResolveIn(Namespace n, Symbol symbol)
        {
            // note: ns-qualified vars must already exist
            if (symbol.Namespace != null)
            {
                Namespace ns = NamespaceFor(n, symbol);
                if (ns == null)
                    return null;

                Var v = ns.FindInternedVar(Symbol.intern(symbol.Name));
                if (v == null)
                    return null;
                return v;
            }
            else if (symbol.Name.IndexOf('.') > 0 && !symbol.Name.EndsWith(".")
                || symbol.Name[symbol.Name.Length - 1] == ']')              /// JAVA: symbol.Name[0] == '[')
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

        public static Namespace NamespaceFor(Symbol symbol)
        {
            return NamespaceFor(CurrentNamespace, symbol);
        }

        public static Namespace NamespaceFor(Namespace n, Symbol symbol)
        {
            // Note: presumes non-nil sym.ns
            // first check against CurrentNamespace's aliases
            Symbol nsSym = Symbol.intern(symbol.Namespace);
            Namespace ns = n.LookupAlias(nsSym);
            if (ns == null)
                // otherwise, check the namespaces map
                ns = Namespace.find(nsSym);
            return ns;
        }

        #endregion

        #region Bindings, registration
        
        private static void RegisterVar(Var v)
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

        internal static Var LookupVar(Symbol sym, bool internNew)
        {
            Var var = null;

            // Note: ns-qualified vars in other namespaces must exist already
            if (sym.Namespace != null)
            {
                Namespace ns = Compiler.NamespaceFor(sym);
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
                        throw new Exception(string.Format("Expecting var, but {0} is mapped to {1}", sym, o));
                }
            }
            if (var != null)
                RegisterVar(var);
            return var;
        }



        internal static int RegisterConstant(Object o)
        {
            if (!ConstantsVar.isBound)
                return -1;
            PersistentVector v = (PersistentVector)ConstantsVar.deref();
            IdentityHashMap ids = (IdentityHashMap)ConstantIdsVar.deref();
            int i;
            if (ids.TryGetValue(o, out i))
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

        internal static LocalBinding RegisterLocal(Symbol sym, Symbol tag, Expr init, bool isArg)
        {
            return RegisterLocal(sym, tag, init, isArg, false);
        }

        internal static LocalBinding RegisterLocal(Symbol sym, Symbol tag, Expr init, bool isArg, bool isByRef)
        {
            int num = GetAndIncLocalNum();
            LocalBinding b = new LocalBinding(num, sym, tag, init, isArg, isByRef);
            IPersistentMap localsMap = (IPersistentMap)LocalEnvVar.deref();
            LocalEnvVar.set(RT.assoc(localsMap, b.Symbol, b));
            ObjMethod method = (ObjMethod)MethodVar.deref();
            method.Locals = (IPersistentMap)RT.assoc(method.Locals, b, b);
            method.IndexLocals = (IPersistentMap)RT.assoc(method.IndexLocals, num, b);
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
                CloseOver(b, method);
            }

            return b;
        }

        static void CloseOver(LocalBinding b, ObjMethod method)
        {
            if (b != null && method != null)
            {
                if (RT.get(method.Locals, b) == null)
                {
                    method.Objx.Closes = (IPersistentMap)RT.assoc(method.Objx.Closes, b, b);
                    CloseOver(b, method.Parent);
                }
                else if (InCatchFinallyVar.deref() != null)
                {
                    method.LocalsUsedInCatchFinally = (PersistentHashSet)method.LocalsUsedInCatchFinally.cons(b.Index);
                }
            }
        }

        #endregion

        #region Boxing arguments

        internal static Expression MaybeBox(Expression expr)
        {
            if (expr.Type == typeof(void))
                // I guess we'll pass a void.  This happens when we have a throw, for example.
                return Expression.Block(expr, Expression.Default(typeof(object)));

            return expr.Type.IsValueType
                ? Expression.Convert(expr, typeof(object))
                : expr;
        }

        #endregion

        #region other type hacking

        internal static Type MaybePrimitiveType(Expr e)
        {
            MaybePrimitiveExpr mpe = e as MaybePrimitiveExpr;

            if (mpe != null && mpe.HasClrType && mpe.CanEmitPrimitive)
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


        internal static Expression GenArgArray(RHC rhc, ObjExpr objx, GenContext context, IPersistentVector args)
        {
            Expression[] exprs = new Expression[args.count()];

            for (int i = 0; i < args.count(); i++)
            {
                Expr arg = (Expr)args.nth(i);
                exprs[i] = Compiler.MaybeBox(arg.GenCode(RHC.Expression,objx,context));
            }

            Expression argArray = Expression.NewArrayInit(typeof(object), exprs);
            return argArray;
        }

        internal static Expression MaybeConvert(Expression expr, Type type)
        {
            if (type == typeof(void))
                type = typeof(object);

            if (expr.Type == typeof(void))
                // I guess we'll pass a void.  This happens when we have a throw, for example.
                return Expression.Block(expr, Expression.Default(type));

            if (expr.Type == type)
                return expr;

            return Expression.Convert(expr, type);
        }


        public static Type PrimType(Symbol sym)
        {
            if (sym == null)
                return null;
            Type t = null;
            switch (sym.Name)
            {
                case "int": t = typeof(int); break;
                case "long": t = typeof(long); break;
                case "float": t = typeof(float); break;
                case "double": t = typeof(double); break;
                case "char": t = typeof(char); break;
                case "short": t = typeof(short); break;
                case "byte": t = typeof(byte); break;
                case "bool":
                case "boolean": t = typeof(bool); break;
                case "void": t = typeof(void); break;
                case "uint": t = typeof(uint); break;
                case "ulong": t = typeof(ulong); break;
                case "ushort": t = typeof(ushort); break;
                case "sbyte": t = typeof(sbyte); break;
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

        public static IPersistentMap CHAR_MAP = PersistentHashMap.create('-', "_",
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


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "munge")]
        public static string munge(string name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                string sub = (string)CHAR_MAP.valAt(c);
                if (sub == null)
                    sb.Append(c);
                else
                    sb.Append(sub);
            }
            return sb.ToString();
        }

        #endregion

        #region Duplicate types

        static Dictionary<String, Type> _duplicateTypeMap = new Dictionary<string, Type>();

        internal static void RegisterDuplicateType(Type type)
        {
            _duplicateTypeMap[type.FullName] = type;
        }

        internal static Type FindDuplicateType(string typename)
        {
            Type type;
            _duplicateTypeMap.TryGetValue(typename, out type);
            return type;
        }

        #endregion

        #region eval

        /// <summary>
        ///  
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        /// <remarks>Initial lowercase for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "eval")]
        public static object eval(object form)
        {
            int line = (int)LineVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LineKey))
                line = (int)RT.meta(form).valAt(RT.LineKey);
            IPersistentMap sourceSpan = (IPersistentMap)SourceSpanVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SourceSpanKey))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SourceSpanKey);

            ParserContext pconExpr = new ParserContext(RHC.Expression);
            ParserContext pconEval = new ParserContext(RHC.Eval);

            Var.pushThreadBindings(RT.map(LineVar, line, SourceSpanVar, sourceSpan, CompilerContextVar, null));
            try
            {
                form = Macroexpand(form);
                bool formIsIpc = (form as IPersistentCollection) != null;

                if (formIsIpc && Util.equals(RT.first(form), DoSym))
                {
                    ISeq s = RT.next(form);
                    for (; RT.next(s) != null; s = RT.next(s))
                        eval(RT.first(s));
                    return eval(RT.first(s));
                }
                else if ( (form is IType) ||
                    (formIsIpc && !(RT.first(form) is Symbol && ((Symbol)RT.first(form)).Name.StartsWith("def"))))
                {
                    ObjExpr objx = (ObjExpr)Analyze(pconExpr, RT.list(FnSym, PersistentVector.EMPTY, form), "eval__" + RT.nextID());
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        /// <remarks>Initial lowercase for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "macroexpand")]
        public static object macroexpand1(object form)
        {
            ISeq s = form as ISeq;
            return s != null 
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

        private static object MacroexpandSeq1(ISeq form)
        {
            object op = RT.first(form);

            if (IsSpecial(op))
                return form;

            // macro expansion
            Var v = IsMacro(op);
            if (v != null)
            {
                try
                {
                    return v.applyTo(RT.cons(form, RT.cons(LocalEnvVar.get(), form.next())));
                }
                catch (ArityException e)
                {
                    // hide the 2 extra params for a macro
                    throw new ArityException(e.Actual - 2, e.Name);
                }
            }
            else
            {
                Symbol sym = op as Symbol;
                if (sym != null)
                {
                    string sname = sym.Name;
                    // (.substring s 2 5) => (. x substring 2 5)
                    if (sname[0] == '.')
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
                    else if (NamesStaticMember(sym))
                    {
                        Symbol target = Symbol.intern(sym.Namespace);
                        Type t = HostExpr.MaybeType(target, false);
                        if (t != null)
                        {
                            Symbol method = Symbol.intern(sym.Name);
                            // We need to make sure source information gets transferred
                            return MaybeTransferSourceInfo(PreserveTag(form, RT.listStar(Compiler.DotSym, target, method, form.next())), form);
                        }
                    }
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
                Var v = opAsVar ??  LookupVar(opAsSym, false);
                if (v != null && v.IsMacro)
                {
                    if (v.Namespace != CurrentNamespace && !v.IsPublic)
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
            IObj newObj = newForm as IObj;
            if (newObj == null)
                return newForm;

            IObj oldObj = oldForm as IObj;
            if (oldObj == null)
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
                IObj iobj = dst as IObj;
                if (iobj != null)
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

        internal static Symbol TagOf(object o)
        {
            object tag = RT.get(RT.meta(o), RT.TagKey);

            {
                Symbol sym = tag as Symbol;
                if (sym != null)
                    return sym;
            }

            {
                string str = tag as String;
                if (str != null)
                    return Symbol.intern(null, str);
            }

            return null;
        }

        internal static bool NamesStaticMember(Symbol sym)
        {
            return sym.Namespace != null && NamespaceFor(sym) == null;
        }

        #endregion

        #region Compilation

        internal static SymbolDocumentInfo DocInfo()
        {
            return (SymbolDocumentInfo)DocumentInfoVar.deref();
        }

        public static int GetLineFromSpanMap(IPersistentMap spanMap)
        {
            if (spanMap == null )
                return 0;

            int line;
            if (GetLocation(spanMap,RT.StartLineKey,out line) )
                return line;

            return 0;
        }

        static bool GetLocation(IPersistentMap spanMap, Keyword key, out int val)
        {
            object oval = spanMap.valAt(key);
            if (oval != null && oval is int)
            {
                val = (int)oval;
                return true;
            }
            val = -1;
            return false;
        }

        static bool GetLocations(IPersistentMap spanMap, out int startLine, out int startCol, out int finishLine, out int finishCol)
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

        internal static Expression MaybeAddDebugInfo(Expression expr, IPersistentMap spanMap, bool isDebuggable)
        {
            if ( isDebuggable && spanMap != null & Compiler.DocInfo() != null)
            {
                int startLine;
                int startCol;
                int finishLine;
                int finishCol;
                if (GetLocations(spanMap, out startLine, out startCol, out finishLine, out finishCol))
                    return AstUtils.AddDebugInfo(expr,
                        Compiler.DocInfo(),
                        new Microsoft.Scripting.SourceLocation(0, (int)spanMap.valAt(RT.StartLineKey), (int)spanMap.valAt(RT.StartColumnKey)),
                        new Microsoft.Scripting.SourceLocation(0, (int)spanMap.valAt(RT.EndLineKey), (int)spanMap.valAt(RT.EndColumnKey)));
            }
            return expr;
        }

        static GenContext _evalContext = GenContext.CreateWithInternalAssembly("eval", false);
        static public GenContext EvalContext { get { return _evalContext; } }

        static int _saveId = 0;
        public static void SaveEvalContext()
        {
            _evalContext.SaveAssembly();
            _evalContext = GenContext.CreateWithInternalAssembly("eval" + (_saveId++).ToString(), false);
        }

        public static bool IsCompiling
        {
            get { return CompilerContextVar.deref() != null; }
        }

        public static string IsCompilingSuffix()
        {
            GenContext context = (GenContext)CompilerContextVar.deref();
            return context == null ? "_INTERP" : "_COMP_" + (new AssemblyName(context.AssemblyBuilder.FullName)).Name;
        }

        internal static object Compile(TextReader rdr, string sourceDirectory, string sourceName, string relativePath)
        {
            if (CompilePathVar.deref() == null)
                throw new Exception("*compile-path* not set");

            object eofVal = new object();
            object form;

            //string sourcePath = sourceDirectory == null ? sourceName : sourceDirectory + "\\" + sourceName;
            string sourcePath = relativePath;

            LineNumberingTextReader lntr = rdr as LineNumberingTextReader ?? new LineNumberingTextReader(rdr);

            GenContext context = GenContext.CreateWithExternalAssembly(relativePath, ".dll", true);
            GenContext evalContext = GenContext.CreateWithInternalAssembly("EvalForCompile", false);

            Var.pushThreadBindings(RT.map(
                SourcePathVar, sourcePath,
                SourceVar, sourceName,
                MethodVar, null,
                LocalEnvVar, null,
                LoopLocalsVar, null,
                NextLocalNumVar, 0,
                RT.CurrentNSVar, RT.CurrentNSVar.deref(),
                    //LINE_BEFORE, lntr.LineNumber,
                    //LINE_AFTER, lntr.LineNumber,
                DocumentInfoVar, Expression.SymbolDocument(sourceName),  // I hope this is enough
                ConstantsVar, PersistentVector.EMPTY,
                ConstantIdsVar, new IdentityHashMap(),
                KeywordsVar, PersistentHashMap.EMPTY,
                VarsVar, PersistentHashMap.EMPTY,
                RT.UncheckedMathVar, RT.UncheckedMathVar.deref(),
                RT.WarnOnReflectionVar, RT.WarnOnReflectionVar.deref(),

                //KEYWORD_CALLSITES, PersistentVector.EMPTY,  // jvm doesn't do this, don't know why
                //VAR_CALLSITES, EmptyVarCallSites(),      // jvm doesn't do this, don't know why
                //PROTOCOL_CALLSITES, PersistentVector.EMPTY, // jvm doesn't do this, don't know why
                CompilerContextVar, context
                ));


            try
            {
                FnExpr objx = new FnExpr(null);
                objx.InternalName = sourcePath.Replace(Path.PathSeparator, '/').Substring(0, sourcePath.LastIndexOf('.')) + "__init";

                TypeBuilder exprTB = context.AssemblyGen.DefinePublicType("__REPL__", typeof(object), true);

                //List<string> names = new List<string>();
                List<Expr> exprs = new List<Expr>();

                int i = 0;
                while ((form = LispReader.read(lntr, false, eofVal, false)) != eofVal)
                {
                    //Java version: LINE_AFTER.set(lntr.LineNumber);

                    Compile1(context, evalContext, exprTB, form, exprs, ref i);


                    //Java version: LINE_BEFORE.set(lntr.LineNumber);
                }

                Type exprType = exprTB.CreateType();

                // Need to put the loader init in its own type because we can't generate calls on the MethodBuilders
                //  until after their types have been closed.

                TypeBuilder initTB = context.AssemblyGen.DefinePublicType("__Init__", typeof(object), true);

                Expression pushNSExpr = Expression.Call(null, Method_Compiler_PushNS);
                Expression popExpr = Expression.Call(null, Method_Var_popThreadBindings);

                BodyExpr bodyExpr = new BodyExpr(PersistentVector.create1(exprs));
                FnMethod method = new FnMethod(objx, null, bodyExpr);
                objx.AddMethod(method);

                objx.Keywords = (IPersistentMap)KeywordsVar.deref();
                objx.Vars = (IPersistentMap)VarsVar.deref();
                objx.Constants = (PersistentVector)ConstantsVar.deref();
                //objx.KeywordCallsites = (IPersistentVector)KEYWORD_CALLSITES.deref();
                //objx.ProtocolCallsites = (IPersistentVector)PROTOCOL_CALLSITES.deref();
                //objx.VarCallsites = (IPersistentSet)VAR_CALLSITES.deref();

                objx.KeywordCallsites = PersistentVector.EMPTY;
                objx.ProtocolCallsites = PersistentVector.EMPTY;
                objx.VarCallsites = (IPersistentSet)EmptyVarCallSites();

                objx.Compile(typeof(AFunction), null, PersistentVector.EMPTY, false, context);

                Expression fnNew = objx.GenCode(RHC.Expression,objx,context);
                Expression fnInvoke = Expression.Call(fnNew, fnNew.Type.GetMethod("invoke", System.Type.EmptyTypes));

                Expression tryCatch = Expression.TryCatchFinally(fnInvoke, popExpr);

                Expression body = Expression.Block(pushNSExpr, tryCatch);

                // create initializer call
                MethodBuilder mbInit = initTB.DefineMethod("Initialize", MethodAttributes.Public | MethodAttributes.Static);
                LambdaExpression initFn = Expression.Lambda(body);
                //initFn.CompileToMethod(mbInit, DebugInfoGenerator.CreatePdbGenerator());
                initFn.CompileToMethod(mbInit, context.IsDebuggable);

                initTB.CreateType();

                context.SaveAssembly();
            }
            catch (LispReader.ReaderException e)
            {
                throw new CompilerException(sourcePath, e.Line, e.InnerException);
            }
            finally
            {
                Var.popThreadBindings();
            }
            return null;
        }

        private static void Compile1(GenContext compileContext, GenContext evalContext, TypeBuilder exprTB, object form, List<Expr> exprs, ref int i)
        {

            int line = (int)LineVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LineKey))
                line = (int)RT.meta(form).valAt(RT.LineKey);
            IPersistentMap sourceSpan = (IPersistentMap)SourceSpanVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SourceSpanKey))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SourceSpanKey);

            Var.pushThreadBindings(RT.map(LineVar, line, SourceSpanVar, sourceSpan));

            ParserContext pcontext = new ParserContext(RHC.Eval);

            try
            {

                form = Macroexpand(form);
                if (form is IPersistentCollection && Util.Equals(RT.first(form), DoSym))
                {
                    for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                        Compile1(compileContext, evalContext, exprTB, RT.first(s), exprs, ref i);
                }
                else
                {
                    Expr expr = Analyze(pcontext, form);
                    exprs.Add(expr);     // should pick up the keywords/vars/constants here
                    expr.Eval();
                }
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        public static void PushNS()
        {
            Var.pushThreadBindings(PersistentHashMap.create(Var.intern(Symbol.intern("clojure.core"),
                                                                       Symbol.intern("*ns*")).setDynamic(), null));
        }


        internal static bool LoadAssembly(FileInfo assyInfo)
        {
            Assembly assy = Assembly.LoadFrom(assyInfo.FullName);
            Type initType = assy.GetType("__Init__");
            if (initType == null)
            {
                Console.WriteLine("Bad assembly");
                return false;
            }
            try
            {
                initType.InvokeMember("Initialize", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, Type.DefaultBinder, null, new object[0]);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error initializing {0}: {1}", assyInfo.FullName, e.Message);
                return false;
            }
        }

        #endregion

        #region Loading

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "load")]
        public static object loadFile(string fileName)
        {
            FileInfo finfo = new FileInfo(fileName);
            if (!finfo.Exists)
                throw new FileNotFoundException("Cannot find file to load", fileName);

            using (TextReader rdr = finfo.OpenText())
                return load(rdr, finfo.FullName, finfo.Name, fileName);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "load")]
        public static object load(TextReader rdr, string relativePath)
        {
            return load(rdr, null, "NO_SOURCE_FILE", relativePath);  // ?
        }

        public delegate object ReplDelegate();


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "load")]
        public static object load(TextReader rdr, string sourcePath, string sourceName, string relativePath)
        {
            object ret = null;
            object eofVal = new object();
            object form;

            LineNumberingTextReader lntr = rdr as LineNumberingTextReader ?? new LineNumberingTextReader(rdr);
 
            Var.pushThreadBindings(RT.map(
                //LOADER, RT.makeClassLoader(),
                SourcePathVar, sourcePath,
                SourceVar, sourceName,
                DocumentInfoVar, Expression.SymbolDocument(sourceName),  // I hope this is enough

                RT.CurrentNSVar, RT.CurrentNSVar.deref(),
                RT.UncheckedMathVar, RT.UncheckedMathVar.deref(),
                RT.WarnOnReflectionVar, RT.WarnOnReflectionVar.deref()
                //LINE_BEFORE, lntr.LineNumber,
                //LINE_AFTER, lntr.LineNumber
                ));

            try
            {
                while ((form = LispReader.read(lntr, false, eofVal, false)) != eofVal)
                {
                    //LINE_AFTER.set(lntr.LineNumber);
                    //Expression<ReplDelegate> ast = Compiler.GenerateLambda(form, false);
                    //ret = ast.Compile().Invoke();
                    ret = eval(form);
                    //LINE_BEFORE.set(lntr.LineNumber);
                }
            }
            catch (LispReader.ReaderException e)
            {
                throw new CompilerException(sourcePath, e.Line, e.InnerException);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public static Expr Analyze(ParserContext pcontext, object form, string name)
        {
            try
            {
                if (form is LazySeq)
                {
                    form = RT.seq(form);
                    if (form == null)
                        form = PersistentList.EMPTY;
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
                else if (form is IPersistentCollection && ((IPersistentCollection)form).count() == 0)
                    return OptionallyGenerateMetaInit(pcontext, form, new EmptyExpr(form));
                else if (form is ISeq)
                    return AnalyzeSeq(pcontext, (ISeq)form, name);
                else if (form is IPersistentVector)
                    return VectorExpr.Parse(pcontext, (IPersistentVector)form);
                else if (form is IRecord)
                    return new ConstantExpr(form);
                else if (form is IType)
                    return new ConstantExpr(form);
                else if (form is IPersistentMap)
                    return MapExpr.Parse(pcontext, (IPersistentMap)form);
                else if (form is IPersistentSet)
                    return SetExpr.Parse(pcontext, (IPersistentSet)form);
                else
                    return new ConstantExpr(form);
            }
            catch (CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CompilerException((String)SourcePathVar.deref(), (int)LineVar.deref(), e);
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
                if (namespaceFor(symbol) == null)
                {
                    Symbol nsSym = Symbol.intern(symbol.Namespace);
                    Type t = HostExpr.MaybeType(nsSym, false);
                    if (t != null)
                    {
                        FieldInfo finfo;
                        PropertyInfo pinfo;

                        if ((finfo = Reflector.GetField(t, symbol.Name, true)) != null)
                            return new StaticFieldExpr((string)SourceVar.deref(),(IPersistentMap)Compiler.SourceSpanVar.deref(), tag, t, symbol.Name, finfo);
                        else if ((pinfo = Reflector.GetProperty(t, symbol.Name, true)) != null)
                            return new StaticPropertyExpr((string)SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), tag, t, symbol.Name, pinfo);
                    }
                    throw new Exception(string.Format("Unable to find static field: {0} in {1}", symbol.Name, t));
                }
            }

            object o = Compiler.Resolve(symbol);

           Symbol oAsSymbol;
           Var oAsVar = o as Var;
 
            if (oAsVar != null)
            {
                if (IsMacro(oAsVar) != null)
                    throw new Exception("Can't take the value of a macro: " + oAsVar);
                if (RT.booleanCast(RT.get(oAsVar.meta(), RT.ConstKey)))
                    return Analyze(new ParserContext(RHC.Expression), RT.list(QuoteSym, oAsVar.get()));
                RegisterVar(oAsVar);
                return new VarExpr(oAsVar, tag);
            }
            else if (o is Type)
                return new ConstantExpr(o);
            else if ( (oAsSymbol = o as Symbol) != null)
                return new UnresolvedVarExpr(oAsSymbol);

            throw new Exception(string.Format("Unable to resolve symbol: {0} in this context", symbol));
        }

        private static Expr AnalyzeSeq(ParserContext pcon, ISeq form, string name )
        {
            int line = (int)LineVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LineKey))
                line = (int)RT.meta(form).valAt(RT.LineKey);
            IPersistentMap sourceSpan = (IPersistentMap)SourceSpanVar.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SourceSpanKey))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SourceSpanKey);

            Var.pushThreadBindings(RT.map(LineVar, line, SourceSpanVar, sourceSpan));

            try
            {

                object me = MacroexpandSeq1(form);
                if (me != form)
                    return Analyze(pcon, me, name);

                object op = RT.first(form);
                if (op == null)
                    throw new ArgumentNullException("Can't call nil");

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
                throw new CompilerException((String)SourcePathVar.deref(), (int)LineVar.deref(), e);
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


        #endregion

        #region CompilerException

        public sealed class CompilerException : Exception
        {

            public CompilerException(string source, int line, Exception cause)
                : base(ErrorMsg(source, line, cause.ToString()), cause)
            {
                Source = source;
            }

            public override string ToString()
            {
                return Message;
            }

            static string ErrorMsg(string source, int line, string s)
            {
                return string.Format("{0}, compiling: ({1}:{2})", s, source, line);
            }

        }

        #endregion
    }
}
