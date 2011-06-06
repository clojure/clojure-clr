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

        internal const int MAX_POSITIONAL_ARITY = 20;

        #endregion

        #region Symbols

        public static readonly Symbol DEF = Symbol.intern("def");
        public static readonly Symbol LOOP = Symbol.intern("loop*");
        public static readonly Symbol RECUR = Symbol.intern("recur");
        public static readonly Symbol IF = Symbol.intern("if");
        public static readonly Symbol LET = Symbol.intern("let*");
        public static readonly Symbol LETFN = Symbol.intern("letfn*");
        public static readonly Symbol DO = Symbol.intern("do");
        public static readonly Symbol FN = Symbol.intern("fn*");
        public static readonly Symbol QUOTE = Symbol.intern("quote");
        public static readonly Symbol THE_VAR = Symbol.intern("var");
        public static readonly Symbol DOT = Symbol.intern(".");
        public static readonly Symbol ASSIGN = Symbol.intern("set!");
        public static readonly Symbol TRY = Symbol.intern("try");
        public static readonly Symbol CATCH = Symbol.intern("catch");
        public static readonly Symbol FINALLY = Symbol.intern("finally");
        public static readonly Symbol THROW = Symbol.intern("throw");
        public static readonly Symbol MONITOR_ENTER = Symbol.intern("monitor-enter");
        public static readonly Symbol MONITOR_EXIT = Symbol.intern("monitor-exit");
        public static readonly Symbol IMPORT = Symbol.intern("clojure.core","import*");
        public static readonly Symbol DEFTYPE = Symbol.intern("deftype*");
        public static readonly Symbol CASE = Symbol.intern("case*");
        public static readonly Symbol NEW = Symbol.intern("new");
        public static readonly Symbol THIS = Symbol.intern("this");
        public static readonly Symbol REIFY = Symbol.intern("reify*");
        public static readonly Symbol _AMP_ = Symbol.intern("&");

        public static readonly Symbol IDENTITY = Symbol.intern("clojure.core", "identity");

        static readonly Symbol NS = Symbol.intern("ns");
        static readonly Symbol IN_NS = Symbol.intern("in-ns");

        internal static readonly Symbol ISEQ = Symbol.intern("clojure.lang.ISeq");

        internal static readonly Symbol CLASS = Symbol.intern("System.Type");

        internal static readonly Symbol INVOKE_STATIC = Symbol.intern("invokeStatic");

        #endregion

        #region Keywords

        static readonly Keyword INLINE_KEY = Keyword.intern(null, "inline");
        static readonly Keyword INLINE_ARITIES_KEY = Keyword.intern(null, "inline-arities");
        internal static readonly Keyword STATIC_KEY = Keyword.intern(null, "static");
        internal static readonly Keyword ARGLISTS_KEY = Keyword.intern(null, "arglists");

        static readonly Keyword VOLATILE_KEY = Keyword.intern(null,"volatile");
        internal static readonly Keyword IMPLEMENTS_KEY = Keyword.intern(null,"implements");
        internal static readonly Keyword PROTOCOL_KEY = Keyword.intern(null,"protocol");
        static readonly Keyword ON_KEY = Keyword.intern(null, "on");
        internal static readonly Keyword DYNAMIC_KEY = Keyword.intern("dynamic");

        internal const string COMPILE_STUB_PREFIX = "compile__stub";

        #endregion

        #region Vars

        //boolean
        internal static readonly Var COMPILE_FILES = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                 //Symbol.intern("*compile-files*"), RT.F);
                                                         Symbol.intern("*compile-files*"), false).setDynamic();  
        //JAVA: Boolean.FALSE -- changed from RT.F in rev 1108, not sure why


        internal static readonly Var INSTANCE = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                         Symbol.intern("instance?"), false).setDynamic();  


        //String
        public static readonly Var COMPILE_PATH = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                 Symbol.intern("*compile-path*"), null).setDynamic();

        public static readonly Var COMPILE = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                                Symbol.intern("compile"));

        // String
        internal static readonly Var SOURCE = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
                                        Symbol.intern("*source-path*"), "NO_SOURCE_FILE").setDynamic();
        // String
        internal static readonly Var SOURCE_PATH = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),
            Symbol.intern("*file*"), "NO_SOURCE_PATH").setDynamic();

        //Integer
        internal static readonly Var LINE = Var.create(0).setDynamic();          // From the JVM version
        //internal static readonly Var LINE_BEFORE = Var.create(0).setDynamic();   // From the JVM version
        //internal static readonly Var LINE_AFTER = Var.create(0).setDynamic();    // From the JVM version
        internal static readonly Var DOCUMENT_INFO = Var.create(null).setDynamic();  // Mine
        internal static readonly Var SOURCE_SPAN = Var.create(null).setDynamic();    // Mine

        internal static readonly Var METHOD = Var.create(null).setDynamic();
        public static readonly Var LOCAL_ENV = Var.create(PersistentHashMap.EMPTY).setDynamic();
        //Integer
        internal static readonly Var NEXT_LOCAL_NUM = Var.create(0).setDynamic();
        internal static readonly Var LOOP_LOCALS = Var.create(null).setDynamic();
        // Label
        internal static readonly Var LOOP_LABEL = Var.create().setDynamic();


        internal static readonly Var IN_CATCH_FINALLY = Var.create(null).setDynamic();          //null or not

        internal static readonly Var NO_RECUR = Var.create(null).setDynamic();

        internal static readonly Var VARS = Var.create().setDynamic();           //var->constid
        internal static readonly Var CONSTANTS = Var.create().setDynamic();      //vector<object>
        internal static readonly Var CONSTANT_IDS = Var.create().setDynamic();   // IdentityHashMap
        internal static readonly Var KEYWORDS = Var.create().setDynamic();       //keyword->constid

        internal static readonly Var KEYWORD_CALLSITES = Var.create().setDynamic();  // vector<keyword>
        internal static readonly Var PROTOCOL_CALLSITES = Var.create().setDynamic(); // vector<var>
        internal static readonly Var VAR_CALLSITES = Var.create().setDynamic();      // set<var>

        internal static readonly Var COMPILE_STUB_SYM = Var.create(null).setDynamic();
        internal static readonly Var COMPILE_STUB_CLASS = Var.create(null).setDynamic();
        internal static readonly Var COMPILE_STUB_ORIG_CLASS = Var.create(null).setDynamic();

        internal static readonly Var COMPILER_CONTEXT = Var.create(null).setDynamic();

        #endregion

        #region Special forms

        public static readonly IPersistentMap _specials = PersistentHashMap.create(
            DEF, new DefExpr.Parser(),
            LOOP, new LetExpr.Parser(),
            RECUR, new RecurExpr.Parser(),
            IF, new IfExpr.Parser(),
            CASE, new CaseExpr.Parser(),
            LET, new LetExpr.Parser(),
            LETFN, new LetFnExpr.Parser(),
            DO, new BodyExpr.Parser(),
            FN, null,
            QUOTE, new ConstantExpr.Parser(),
            THE_VAR, new TheVarExpr.Parser(),
            IMPORT, new ImportExpr.Parser(),
            DOT, new HostExpr.Parser(),
            ASSIGN, new AssignExpr.Parser(),
            DEFTYPE, new NewInstanceExpr.DefTypeParser(),
            REIFY, new NewInstanceExpr.ReifyParser(),
            TRY, new TryExpr.Parser(),
            THROW, new ThrowExpr.Parser(),
            MONITOR_ENTER, new MonitorEnterExpr.Parser(),
            MONITOR_EXIT, new MonitorExitExpr.Parser(),
            CATCH, null,
            FINALLY, null,
            NEW, new NewExpr.Parser(),
            _AMP_, null
        );

        public static bool IsSpecial(Object sym)
        {
            return _specials.containsKey(sym);
        }

        static IParser GetSpecialFormParser(object op)
        {
            return (IParser)_specials.valAt(op);
        }

        #endregion

        #region MethodInfos, etc.

        internal static readonly PropertyInfo Method_AFunction_MethodImplCache = typeof(AFunction).GetProperty("MethodImplCache");

        internal static readonly MethodInfo Method_ArraySeq_create = typeof(ArraySeq).GetMethod("create", BindingFlags.Static | BindingFlags.Public,null, new Type[] { typeof(object[]) }, null);

        internal static readonly PropertyInfo Method_Compiler_CurrentNamespace = typeof(Compiler).GetProperty("CurrentNamespace");
        internal static readonly MethodInfo Method_Compiler_PushNS = typeof(Compiler).GetMethod("PushNS");

        internal static readonly MethodInfo Method_Delegate_CreateDelegate = typeof(Delegate).GetMethod("CreateDelegate", BindingFlags.Static | BindingFlags.Public,null,new Type[] {typeof(Type), typeof(Object), typeof(string)},null);

        internal static readonly MethodInfo Method_ILookupSite_fault = typeof(ILookupSite).GetMethod("fault");
        internal static readonly MethodInfo Method_ILookupThunk_get = typeof(ILookupThunk).GetMethod("get");

        internal static readonly MethodInfo Method_IPersistentMap_valAt2 = typeof(ILookup).GetMethod("valAt", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_IPersistentMap_without = typeof(IPersistentMap).GetMethod("without");

        internal static readonly MethodInfo Method_IObj_withMeta = typeof(IObj).GetMethod("withMeta");

        internal static readonly MethodInfo Method_Keyword_intern_symbol = typeof(Keyword).GetMethod("intern", new Type[] { typeof(Symbol) });
        internal static readonly MethodInfo Method_Keyword_intern_string = typeof(Keyword).GetMethod("intern", new Type[] { typeof(String) });
        
        internal static readonly MethodInfo Method_KeywordLookupSite_Get = typeof(KeywordLookupSite).GetMethod("Get");

        internal static readonly MethodInfo Method_MethodImplCache_fnFor = typeof(MethodImplCache).GetMethod("fnFor");

        internal static readonly MethodInfo Method_Monitor_Enter = typeof(Monitor).GetMethod("Enter", new Type[] { typeof(Object) });
        internal static readonly MethodInfo Method_Monitor_Exit = typeof(Monitor).GetMethod("Exit", new Type[] { typeof(Object) });

        internal static readonly MethodInfo Method_Object_ReferenceEquals = typeof(Object).GetMethod("ReferenceEquals");
        internal static readonly MethodInfo Method_Object_MemberwiseClone = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);

        internal static readonly MethodInfo Method_Namespace_importClass1 = typeof(Namespace).GetMethod("importClass", new Type[] { typeof(Type) });

        internal static readonly MethodInfo Method_Numbers_num_long = typeof(Numbers).GetMethod("num", new Type[] { typeof(long) });

        internal static readonly MethodInfo Method_PersistentList_create = typeof(PersistentList).GetMethod("create", new Type[] { typeof(System.Collections.IList) });
        internal static readonly MethodInfo Method_PersistentHashSet_create = typeof(PersistentHashSet).GetMethod("create", new Type[] { typeof(Object[]) });
        internal static readonly FieldInfo Method_PersistentHashSet_EMPTY = typeof(PersistentHashSet).GetField("EMPTY");

        //internal static readonly MethodInfo Method_Reflector_CallInstanceMethod = typeof(Reflector).GetMethod("CallInstanceMethod");
        //internal static readonly MethodInfo Method_Reflector_CallStaticMethod = typeof(Reflector).GetMethod("CallStaticMethod");
        //internal static readonly MethodInfo Method_Reflector_InvokeConstructor = typeof(Reflector).GetMethod("InvokeConstructor");
        internal static readonly MethodInfo Method_Reflector_GetInstanceFieldOrProperty = typeof(Reflector).GetMethod("GetInstanceFieldOrProperty");
        internal static readonly MethodInfo Method_Reflector_SetInstanceFieldOrProperty = typeof(Reflector).GetMethod("SetInstanceFieldOrProperty");

        internal static readonly MethodInfo Method_RT_arrayToList = typeof(RT).GetMethod("arrayToList");
        internal static readonly MethodInfo Method_RT_classForName = typeof(RT).GetMethod("classForName");
        internal static readonly MethodInfo Method_RT_intCast_long = typeof(RT).GetMethod("intCast", new Type[] { typeof(long) });
        internal static readonly MethodInfo Method_RT_uncheckedIntCast_long = typeof(RT).GetMethod("uncheckedIntCast", new Type[] { typeof(long) });
        internal static readonly MethodInfo Method_RT_IsTrue = typeof(RT).GetMethod("IsTrue");
        internal static readonly MethodInfo Method_RT_keyword = typeof(RT).GetMethod("keyword");
        internal static readonly MethodInfo Method_RT_map = typeof(RT).GetMethod("map");
        internal static readonly MethodInfo Method_RT_printToConsole = typeof(RT).GetMethod("printToConsole");
        internal static readonly MethodInfo Method_RT_seqOrElse = typeof(RT).GetMethod("seqOrElse");
        internal static readonly MethodInfo Method_RT_set = typeof(RT).GetMethod("set");
        internal static readonly MethodInfo Method_RT_vector = typeof(RT).GetMethod("vector");
        internal static readonly MethodInfo Method_RT_readString = typeof(RT).GetMethod("readString");
        internal static readonly MethodInfo Method_RT_var2 = typeof(RT).GetMethod("var", new Type[] { typeof(string), typeof(string) });

        internal static readonly MethodInfo Method_Symbol_intern2 = typeof(Symbol).GetMethod("intern", new Type[] { typeof(string), typeof(string) });

        internal static readonly MethodInfo Method_Util_classOf = typeof(Util).GetMethod("classOf");
        internal static readonly MethodInfo Method_Util_ConvertToInt = typeof(Util).GetMethod("ConvertToInt");

        internal static readonly MethodInfo Method_Util_equals = typeof(Util).GetMethod("equals", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_Util_equiv = typeof(Util).GetMethod("equiv", new Type[] { typeof(object), typeof(object) });
        internal static readonly MethodInfo Method_Util_Hash = typeof(Util).GetMethod("Hash");
        internal static readonly MethodInfo Method_Util_IsNonCharNumeric = typeof(Util).GetMethod("IsNonCharNumeric");
        
        internal static readonly MethodInfo Method_Var_bindRoot = typeof(Var).GetMethod("bindRoot");
        internal static readonly MethodInfo Method_Var_get = typeof(Var).GetMethod("deref");
        internal static readonly MethodInfo Method_Var_set = typeof(Var).GetMethod("set");
        internal static readonly MethodInfo Method_Var_setMeta = typeof(Var).GetMethod("setMeta");
        internal static readonly MethodInfo Method_Var_popThreadBindings = typeof(Var).GetMethod("popThreadBindings");
        internal static readonly MethodInfo Method_Var_hasRoot = typeof(Var).GetMethod("hasRoot");
        internal static readonly MethodInfo Method_Var_getRawRoot = typeof(Var).GetMethod("getRawRoot");
        internal static readonly MethodInfo Method_Var_getRoot = typeof(Var).GetMethod("getRoot");
        internal static readonly MethodInfo Method_Var_setDynamic0 = typeof(Var).GetMethod("setDynamic", Type.EmptyTypes);
        //internal static readonly PropertyInfo Method_Var_Rev = typeof(Var).GetProperty("Rev");

        internal static readonly ConstructorInfo Ctor_KeywordLookupSite_1 = typeof(KeywordLookupSite).GetConstructor(new Type[] { typeof(Keyword) });
        internal static readonly ConstructorInfo Ctor_Regex_1 = typeof(Regex).GetConstructor(new Type[] { typeof(String) });
        internal static readonly ConstructorInfo Ctor_RestFnImpl_1 = typeof(RestFnImpl).GetConstructor(new Type[] { typeof(int) });

        internal static readonly ConstructorInfo Ctor_Serializable = typeof(SerializableAttribute).GetConstructor(Type.EmptyTypes);

        internal static readonly MethodInfo[] Methods_IFn_invoke = new MethodInfo[MAX_POSITIONAL_ARITY + 2];

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
            for (int i = 0; i <= Compiler.MAX_POSITIONAL_ARITY; i++)
                Methods_IFn_invoke[i] = typeof(IFn).GetMethod("invoke", CreateObjectTypeArray(i));

            Type[] types = new Type[Compiler.MAX_POSITIONAL_ARITY + 1];
            CreateObjectTypeArray(Compiler.MAX_POSITIONAL_ARITY).CopyTo(types, 0);
            types[Compiler.MAX_POSITIONAL_ARITY] = typeof(object[]);
            Methods_IFn_invoke[Compiler.MAX_POSITIONAL_ARITY + 1]
                = typeof(IFn).GetMethod("invoke", types);
        }

        #endregion

        #region Symbol/namespace resolving

        // TODO: we have duplicate code below.

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
            else if (o is Type)
                return Symbol.intern(null, Util.NameForType((Type)o));
            else if (o is Var)
            {
                Var v = (Var)o;
                return Symbol.intern(v.Namespace.Name.Name, v.Symbol.Name);
            }
            return null;

        }


        public static Namespace namespaceFor(Symbol sym)
        {
            return namespaceFor(CurrentNamespace, sym);
        }

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
            get { return (Namespace)RT.CURRENT_NS.deref(); }
        }

        public static string DestubClassName(String className)
        {
            //skip over prefix + '.' or '/'
            if (className.StartsWith(COMPILE_STUB_PREFIX))
                return className.Substring(COMPILE_STUB_PREFIX.Length + 1);
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
            else if (symbol.Equals(NS))
                return RT.NS_VAR;
            else if (symbol.Equals(IN_NS))
                return RT.IN_NS_VAR;
            else
            {
                if (Util.equals(symbol, COMPILE_STUB_SYM.get()))
                    return COMPILE_STUB_CLASS.get();

                object o = n.GetMapping(symbol);
                if (o == null)
                {
                    if (RT.booleanCast(RT.ALLOW_UNRESOLVED_VARS.deref()))
                        return symbol;
                    else
                        throw new Exception(string.Format("Unable to resolve symbol: {0} in this context", symbol));
                }
                return o;
            }
        }

        // core.clj compatibility
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
            else if (symbol.Equals(NS))
                return RT.NS_VAR;
            else if (symbol.Equals(IN_NS))
                return RT.IN_NS_VAR;
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
            if (!VARS.isBound)
                return;
            IPersistentMap varsMap = (IPersistentMap)VARS.deref();
            Object id = RT.get(varsMap, v);
            if (id == null)
            {
                VARS.set(RT.assoc(varsMap, v, RegisterConstant(v)));
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
            else if (sym.Equals(NS))
                var = RT.NS_VAR;
            else if (sym.Equals(IN_NS))
                var = RT.IN_NS_VAR;
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
                else if (o is Var)
                    var = (Var)o;
                else
                    throw new Exception(string.Format("Expecting var, but {0} is mapped to {1}", sym, o));
            }
            if (var != null)
                RegisterVar(var);
            return var;
        }



        internal static int RegisterConstant(Object o)
        {
            if (!CONSTANTS.isBound)
                return -1;
            PersistentVector v = (PersistentVector)CONSTANTS.deref();
            IdentityHashMap ids = (IdentityHashMap)CONSTANT_IDS.deref();
            int i;
            if (ids.TryGetValue(o, out i))
                return i;
            CONSTANTS.set(RT.conj(v, o));
            ids[o] = v.count();
            return v.count();
        }

        internal static KeywordExpr RegisterKeyword(Keyword keyword)
        {
            if (!KEYWORDS.isBound)
                return new KeywordExpr(keyword);

            IPersistentMap keywordsMap = (IPersistentMap)KEYWORDS.deref();
            object id = RT.get(keywordsMap, keyword);
            if (id == null)
                KEYWORDS.set(RT.assoc(keywordsMap, keyword, RegisterConstant(keyword)));
            return new KeywordExpr(keyword);
        }

        internal static int RegisterKeywordCallsite(Keyword keyword)
        {
            if (!KEYWORD_CALLSITES.isBound)
                throw new InvalidOperationException("KEYWORD_CALLSITES is not bound");

            IPersistentVector keywordCallsites = (IPersistentVector)KEYWORD_CALLSITES.deref();
            keywordCallsites = keywordCallsites.cons(keyword);
            KEYWORD_CALLSITES.set(keywordCallsites);
            return keywordCallsites.count() - 1;
        }

        internal static int RegisterProtocolCallsite(Var v)
        {
            if (!PROTOCOL_CALLSITES.isBound)
                throw new InvalidOperationException("PROTOCOL_CALLSITES is not bound");

            IPersistentVector protocolCallsites = (IPersistentVector)PROTOCOL_CALLSITES.deref();
            protocolCallsites = protocolCallsites.cons(v);
            PROTOCOL_CALLSITES.set(protocolCallsites);
            return protocolCallsites.count() - 1;
        }

        internal static void RegisterVarCallsite(Var v)
        {
            if (!VAR_CALLSITES.isBound)
                throw new InvalidOperationException("VAR_CALLSITES is not bound");

            IPersistentCollection varCallsites = (IPersistentCollection)VAR_CALLSITES.deref();
            varCallsites = varCallsites.cons(v);
            VAR_CALLSITES.set(varCallsites);
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
            IPersistentMap localsMap = (IPersistentMap)LOCAL_ENV.deref();
            LOCAL_ENV.set(RT.assoc(localsMap, b.Symbol, b));
            ObjMethod method = (ObjMethod)METHOD.deref();
            method.Locals = (IPersistentMap)RT.assoc(method.Locals, b, b);
            method.IndexLocals = (IPersistentMap)RT.assoc(method.IndexLocals, num, b);
            return b;
        }

        internal static int GetAndIncLocalNum()
        {
            int num = (int)NEXT_LOCAL_NUM.deref();
            ObjMethod m = (ObjMethod)METHOD.deref();
            if (num > m.MaxLocal)
                m.MaxLocal = num;
            NEXT_LOCAL_NUM.set(num + 1);
            return num;
        }

        internal static LocalBinding ReferenceLocal(Symbol symbol)
        {
            if (!LOCAL_ENV.isBound)
                return null;

            LocalBinding b = (LocalBinding)RT.get(LOCAL_ENV.deref(), symbol);
            if (b != null)
            {
                ObjMethod method = (ObjMethod)METHOD.deref();
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
                else if (IN_CATCH_FINALLY.deref() != null)
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
            if (e is MaybePrimitiveExpr && e.HasClrType && ((MaybePrimitiveExpr)e).CanEmitPrimitive)
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
            if (tag is Symbol)
                t = PrimType((Symbol)tag);
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


        // Used in core_deftype, so initial lowercase required for compatibility
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
        public static object eval(object form)
        {
            int line = (int)LINE.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LINE_KEY))
                line = (int)RT.meta(form).valAt(RT.LINE_KEY);
            IPersistentMap sourceSpan = (IPersistentMap)SOURCE_SPAN.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SOURCE_SPAN_KEY))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SOURCE_SPAN_KEY);

            ParserContext pconExpr = new ParserContext(RHC.Expression);
            ParserContext pconEval = new ParserContext(RHC.Eval);

            Var.pushThreadBindings(RT.map(LINE, line, SOURCE_SPAN, sourceSpan, COMPILER_CONTEXT, null));
            try
            {
                form = Macroexpand(form);
                if (form is IPersistentCollection && Util.equals(RT.first(form), DO))
                {
                    ISeq s = RT.next(form);
                    for (; RT.next(s) != null; s = RT.next(s))
                        eval(RT.first(s));
                    return eval(RT.first(s));
                }
                else if (form is IPersistentCollection && !(RT.first(form) is Symbol && ((Symbol)RT.first(form)).Name.StartsWith("def")))
                {
                    ObjExpr objx = (ObjExpr)Analyze(pconExpr, RT.list(FN, PersistentVector.EMPTY, form), "eval__" + RT.nextID());
                    IFn fn = (IFn)objx.Eval();
                    return fn.invoke();
                    //Expression<ReplDelegate> ast = Compiler.GenerateLambda(form, "eval" + RT.nextID().ToString(), false);
                    //return ast.Compile().Invoke();
                }
                else
                {
                    Expr expr = Analyze(pconEval, form);
                    return expr.Eval();
                    // In the Java version, one would actually eval.
                    //Expression<ReplDelegate> ast = Compiler.GenerateLambda(form, false);
                    //return ast.Compile().Invoke();
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
        public static object macroexpand1(object form)
        {
            return (form is ISeq)
                ? MacroexpandSeq1((ISeq)form)
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
                    return v.applyTo(RT.cons(form, RT.cons(LOCAL_ENV.get(), form.next())));
                }
                catch (ArityException e)
                {
                    // hide the 2 extra params for a macro
                    throw new ArityException(e.Actual - 2, e.Name);
                }
            }
            else
            {
                if (op is Symbol)
                {
                    Symbol sym = (Symbol)op;
                    string sname = sym.Name;
                    // (.substring s 2 5) => (. x substring 2 5)
                    if (sname[0] == '.')
                    {
                        if (form.count() < 2)
                            throw new ArgumentException("Malformed member expression, expecting (.member target ...)");
                        Symbol method = Symbol.intern(sname.Substring(1));
                        object target = RT.second(form);
                        if (HostExpr.MaybeType(target, false) != null)
                            target = ((IObj)RT.list(IDENTITY, target)).withMeta(RT.map(RT.TAG_KEY, CLASS));
                        // We need to make sure source information gets transferred
                        return MaybeTransferSourceInfo(PreserveTag(form, RT.listStar(DOT, target, method, form.next().next())), form);
                    }
                    else if (NamesStaticMember(sym))
                    {
                        Symbol target = Symbol.intern(sym.Namespace);
                        Type t = HostExpr.MaybeType(target, false);
                        if (t != null)
                        {
                            Symbol method = Symbol.intern(sym.Name);
                            // We need to make sure source information gets transferred
                            return MaybeTransferSourceInfo(PreserveTag(form, RT.listStar(Compiler.DOT, target, method, form.next())), form);
                        }
                    }
                    else
                    {
                        // (x.substring 2 5) =>  (. x substring 2 5)
                        // also (package.class.name ... ) (. package.class name ... )
                        int index = sname.LastIndexOf('.');
                        if (index == sname.Length - 1)
                            // We need to make sure source information gets transferred
                            return MaybeTransferSourceInfo(RT.listStar(Compiler.NEW, Symbol.intern(sname.Substring(0, index)), form.next()), form);
                    }
                }

            }
            return form;
        }

        private static Var IsMacro(Object op)
        {
            if (op is Symbol && ReferenceLocal((Symbol)op) != null)
                return null;
            if (op is Symbol || op is Var)
            {
                Var v = (op is Var) ? (Var)op : LookupVar((Symbol)op, false);
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
            if (op is Symbol && ReferenceLocal((Symbol)op) != null)
                return null;

            if (op is Symbol || op is Var)
            {
                Var v = (op is Var) ? (Var)op : LookupVar((Symbol)op, false);
                if (v != null)
                {
                    if (v.Namespace != CurrentNamespace && !v.isPublic)
                        throw new InvalidOperationException("var: " + v + " is not public");
                    IFn ret = (IFn)RT.get(v.meta(), INLINE_KEY);
                    if (ret != null)
                    {
                        IFn arityPred = (IFn)RT.get(v.meta(), INLINE_ARITIES_KEY);
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

            IPersistentMap spanMap = (IPersistentMap)oldMeta.valAt(RT.SOURCE_SPAN_KEY);
            if (spanMap != null)
            {
                IPersistentMap newMeta = newObj.meta();
                if (newMeta == null)
                    newMeta = RT.map();

                newMeta = newMeta.assoc(RT.SOURCE_SPAN_KEY, spanMap);

                return newObj.withMeta(newMeta);
            }

            return newForm;
        }

        static object PreserveTag(ISeq src, object dst)
        {
            Symbol tag = TagOf(src);
            if (tag != null && dst is IObj)
            {
                IPersistentMap meta = RT.meta(dst);
                return ((IObj)dst).withMeta((IPersistentMap)RT.assoc(meta, RT.TAG_KEY, tag));
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
            object tag = RT.get(RT.meta(o), RT.TAG_KEY);
            if (tag is Symbol)
                return (Symbol)tag;
            else if (tag is string)
                return Symbol.intern(null, (String)tag);
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
            return (SymbolDocumentInfo)DOCUMENT_INFO.deref();
        }

        public static int GetLineFromSpanMap(IPersistentMap spanMap)
        {
            if (spanMap == null )
                return 0;

            int line;
            if (GetLocation(spanMap,RT.START_LINE_KEY,out line) )
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

            return GetLocation(spanMap, RT.START_LINE_KEY, out startLine)
                && GetLocation(spanMap, RT.START_COLUMN_KEY, out startCol)
                && GetLocation(spanMap, RT.END_LINE_KEY, out finishLine)
                && GetLocation(spanMap, RT.END_COLUMN_KEY, out finishCol);
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
                        new Microsoft.Scripting.SourceLocation(0, (int)spanMap.valAt(RT.START_LINE_KEY), (int)spanMap.valAt(RT.START_COLUMN_KEY)),
                        new Microsoft.Scripting.SourceLocation(0, (int)spanMap.valAt(RT.END_LINE_KEY), (int)spanMap.valAt(RT.END_COLUMN_KEY)));
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
            get { return COMPILER_CONTEXT.deref() != null; }
        }

        public static string IsCompilingSuffix()
        {
            GenContext context = (GenContext)COMPILER_CONTEXT.deref();
            return context == null ? "_INTERP" : "_COMP_" + (new AssemblyName(context.AssemblyBuilder.FullName)).Name;
        }

        internal static object Compile(TextReader rdr, string sourceDirectory, string sourceName, string relativePath)
        {
            if (COMPILE_PATH.deref() == null)
                throw new Exception("*compile-path* not set");

            object eofVal = new object();
            object form;

            //string sourcePath = sourceDirectory == null ? sourceName : sourceDirectory + "\\" + sourceName;
            string sourcePath = relativePath;

            LineNumberingTextReader lntr =
                (rdr is LineNumberingTextReader) ? (LineNumberingTextReader)rdr : new LineNumberingTextReader(rdr);

            GenContext context = GenContext.CreateWithExternalAssembly(relativePath, ".dll", true);
            GenContext evalContext = GenContext.CreateWithInternalAssembly("EvalForCompile", false);

            Var.pushThreadBindings(RT.map(
                SOURCE_PATH, sourcePath,
                SOURCE, sourceName,
                METHOD, null,
                LOCAL_ENV, null,
                LOOP_LOCALS, null,
                NEXT_LOCAL_NUM, 0,
                RT.CURRENT_NS, RT.CURRENT_NS.deref(),
                    //LINE_BEFORE, lntr.LineNumber,
                    //LINE_AFTER, lntr.LineNumber,
                DOCUMENT_INFO, Expression.SymbolDocument(sourceName),  // I hope this is enough
                CONSTANTS, PersistentVector.EMPTY,
                CONSTANT_IDS, new IdentityHashMap(),
                KEYWORDS, PersistentHashMap.EMPTY,
                VARS, PersistentHashMap.EMPTY,
                RT.UNCHECKED_MATH, RT.UNCHECKED_MATH.deref(),
                RT.WARN_ON_REFLECTION, RT.WARN_ON_REFLECTION.deref(),

                //KEYWORD_CALLSITES, PersistentVector.EMPTY,  // jvm doesn't do this, don't know why
                //VAR_CALLSITES, EmptyVarCallSites(),      // jvm doesn't do this, don't know why
                //PROTOCOL_CALLSITES, PersistentVector.EMPTY, // jvm doesn't do this, don't know why
                COMPILER_CONTEXT, context
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

                objx.Keywords = (IPersistentMap)KEYWORDS.deref();
                objx.Vars = (IPersistentMap)VARS.deref();
                objx.Constants = (PersistentVector)CONSTANTS.deref();
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

            int line = (int)LINE.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LINE_KEY))
                line = (int)RT.meta(form).valAt(RT.LINE_KEY);
            IPersistentMap sourceSpan = (IPersistentMap)SOURCE_SPAN.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SOURCE_SPAN_KEY))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SOURCE_SPAN_KEY);

            Var.pushThreadBindings(RT.map(LINE, line, SOURCE_SPAN, sourceSpan));

            ParserContext pcontext = new ParserContext(RHC.Eval);

            try
            {

                form = Macroexpand(form);
                if (form is IPersistentCollection && Util.Equals(RT.first(form), DO))
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

        public static object loadFile(string filename)
        {
            FileInfo finfo = new FileInfo(filename);
            if (!finfo.Exists)
                throw new FileNotFoundException("Cannot find file to load", filename);

            using (TextReader rdr = finfo.OpenText())
                return load(rdr, finfo.FullName, finfo.Name, filename);
        }


        public static object load(TextReader rdr, string relativePath)
        {
            return load(rdr, null, "NO_SOURCE_FILE", relativePath);  // ?
        }

        public delegate object ReplDelegate();


        public static object load(TextReader rdr, string sourcePath, string sourceName, string relativePath)
        {
            object ret = null;
            object eofVal = new object();
            object form;

            LineNumberingTextReader lntr =
                (rdr is LineNumberingTextReader) ? (LineNumberingTextReader)rdr : new LineNumberingTextReader(rdr);

            Var.pushThreadBindings(RT.map(
                //LOADER, RT.makeClassLoader(),
                SOURCE_PATH, sourcePath,
                SOURCE, sourceName,
                DOCUMENT_INFO, Expression.SymbolDocument(sourceName),  // I hope this is enough

                RT.CURRENT_NS, RT.CURRENT_NS.deref(),
                RT.UNCHECKED_MATH, RT.UNCHECKED_MATH.deref(),
                RT.WARN_ON_REFLECTION, RT.WARN_ON_REFLECTION.deref()
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

        internal static LiteralExpr NIL_EXPR = new NilExpr();
        internal static LiteralExpr TRUE_EXPR = new BooleanExpr(true);
        internal static LiteralExpr FALSE_EXPR = new BooleanExpr(false);

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
                    form = RT.seq(form);
                    if (form == null)
                        form = PersistentList.EMPTY;
                }
                if (form == null)
                    return NIL_EXPR;
                else if (form is Boolean)
                    return ((bool)form) ? TRUE_EXPR : FALSE_EXPR;

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
                throw new CompilerException((String)SOURCE_PATH.deref(), (int)LINE.deref(), e);
            }
        }

        internal static Expr OptionallyGenerateMetaInit(ParserContext pcon, object form, Expr expr)
        {
            Expr ret = expr;

            if ( RT.meta(form) != null )
                ret = new MetaExpr(ret, (MapExpr)MapExpr.Parse(pcon.EvEx(),((IObj)form).meta()));

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
                            return new StaticFieldExpr((string)SOURCE.deref(),(IPersistentMap)Compiler.SOURCE_SPAN.deref(), tag, t, symbol.Name, finfo);
                        else if ((pinfo = Reflector.GetProperty(t, symbol.Name, true)) != null)
                            return new StaticPropertyExpr((string)SOURCE.deref(), (IPersistentMap)Compiler.SOURCE_SPAN.deref(), tag, t, symbol.Name, pinfo);
                    }
                    throw new Exception(string.Format("Unable to find static field: {0} in {1}", symbol.Name, t));
                }
            }

            object o = Compiler.Resolve(symbol);
            if (o is Var)
            {
                Var v = (Var)o;
                if (IsMacro(v) != null)
                    throw new Exception("Can't take the value of a macro: " + v);
                if (RT.booleanCast(RT.get(v.meta(), RT.CONST_KEY)))
                    return Analyze(new ParserContext(RHC.Expression), RT.list(QUOTE, v.get()));
                RegisterVar(v);
                return new VarExpr(v, tag);
            }
            else if (o is Type)
                return new ConstantExpr(o);
            else if (o is Symbol)
                return new UnresolvedVarExpr((Symbol)o);

            throw new Exception(string.Format("Unable to resolve symbol: {0} in this context", symbol));
        }

        private static Expr AnalyzeSeq(ParserContext pcon, ISeq form, string name )
        {
            int line = (int)LINE.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.LINE_KEY))
                line = (int)RT.meta(form).valAt(RT.LINE_KEY);
            IPersistentMap sourceSpan = (IPersistentMap)SOURCE_SPAN.deref();
            if (RT.meta(form) != null && RT.meta(form).containsKey(RT.SOURCE_SPAN_KEY))
                sourceSpan = (IPersistentMap)RT.meta(form).valAt(RT.SOURCE_SPAN_KEY);

            Var.pushThreadBindings(RT.map(LINE, line, SOURCE_SPAN, sourceSpan));

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
                if (op.Equals(FN))
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
                throw new CompilerException((String)SOURCE_PATH.deref(), (int)LINE.deref(), e);
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
