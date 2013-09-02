﻿/**
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
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    class FnExpr : ObjExpr
    {
        #region Data

        static readonly Keyword KW_ONCE = Keyword.intern(null, "once");

        FnMethod _variadicMethod = null;

        bool IsVariadic { get { return _variadicMethod != null; } }

        bool _hasMeta;

        protected override bool SupportsMeta
        {
            get { return _hasMeta; }
        }

        private int _dynMethodMapKey = RT.nextID();
        public int DynMethodMapKey { get { return _dynMethodMapKey; } }

        #endregion

        #region Ctors

        public FnExpr(object tag)
            : base(tag)
        {
        }

        #endregion
       
        #region Misc

        // This naming convention drawn from the Java code.
        internal void ComputeNames(ISeq form, string name)
        {
            ObjMethod enclosingMethod = (ObjMethod)Compiler.MethodVar.deref();

            string baseName = enclosingMethod != null
                ? (enclosingMethod.Objx.Name + "$")
                : Compiler.munge(Compiler.CurrentNamespace.Name.Name) + "$";

            if (RT.second(form) is Symbol)
                name = ((Symbol)RT.second(form)).Name;

            string simpleName = name != null ?
                        (Compiler.munge(name).Replace(".", "_DOT_")
                        + (enclosingMethod != null ? "__" + RT.nextID() : ""))
                        : ("fn"
                          + "__" + RT.nextID());            

            _name = baseName + simpleName;
            InternalName = _name.Replace('.', '/');
        }

        #endregion

        #region Type munging

        public override bool HasClrType
        {
            get
            {
                return true;
            }
        }

        public override Type ClrType
        {
            get
            {
                return typeof(AFunction);
            }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ParserContext pcon, ISeq form, string name)
        {
            ISeq origForm = form;

            FnExpr fn = new FnExpr(Compiler.TagOf(form));
            fn._src = form;

            if (((IMeta)form.first()).meta() != null)
            {
                fn._onceOnly = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ONCE));
            }

            fn.ComputeNames(form, name);

            List<string> prims = new List<string>();

            //arglist might be preceded by symbol naming this fn
            if (RT.second(form) is Symbol)
            {
                Symbol nm = (Symbol)RT.second(form);
                fn._thisName = nm.Name;
                fn._isStatic = false; // RT.booleanCast(RT.get(nm.meta(), Compiler.STATIC_KEY));
                form = RT.cons(Compiler.FnSym, RT.next(RT.next(form)));
            }

            // Normalize body
            //now (fn [args] body...) or (fn ([args] body...) ([args2] body2...) ...)
            //turn former into latter
            if (RT.second(form) is IPersistentVector)
                form = RT.list(Compiler.FnSym, RT.next(form));

            fn.SpanMap = (IPersistentMap)Compiler.SourceSpanVar.deref();

            GenContext newContext = null;

            GenContext context = Compiler.CompilerContextVar.deref() as GenContext ?? Compiler.EvalContext;
            newContext = context.WithNewDynInitHelper(fn.InternalName + "__dynInitHelper_" + RT.nextID().ToString());
            Var.pushThreadBindings(RT.map(Compiler.CompilerContextVar, newContext));


            try
            {
                try
                {
                    Var.pushThreadBindings(RT.mapUniqueKeys(
                        Compiler.ConstantsVar, PersistentVector.EMPTY,
                        Compiler.ConstantIdsVar, new IdentityHashMap(),
                        Compiler.KeywordsVar, PersistentHashMap.EMPTY,
                        Compiler.VarsVar, PersistentHashMap.EMPTY,
                        Compiler.KeywordCallsitesVar, PersistentVector.EMPTY,
                        Compiler.ProtocolCallsitesVar, PersistentVector.EMPTY,
                        Compiler.VarCallsitesVar, Compiler.EmptyVarCallSites(),
                        Compiler.NoRecurVar, null));
                    SortedDictionary<int, FnMethod> methods = new SortedDictionary<int, FnMethod>();
                    FnMethod variadicMethod = null;

                    for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                    {
                        FnMethod f = FnMethod.Parse(fn, (ISeq)RT.first(s), fn._isStatic);
                        if (f.IsVariadic)
                        {
                            if (variadicMethod == null)
                                variadicMethod = f;
                            else
                                throw new ParseException("Can't have more than 1 variadic overload");
                        }
                        else if (!methods.ContainsKey(f.RequiredArity))
                            methods[f.RequiredArity] = f;
                        else
                            throw new ParseException("Can't have 2 overloads with the same arity.");
                        if (f.Prim != null)
                            prims.Add(f.Prim);
                    }

                    if (variadicMethod != null && methods.Count > 0 && methods.Keys.Max() >= variadicMethod.NumParams)
                        throw new ParseException("Can't have fixed arity methods with more params than the variadic method.");

                    if (fn._isStatic && fn.Closes.count() > 0)
                        throw new ParseException("static fns can't be closures");

                    IPersistentCollection allMethods = null;
                    foreach (FnMethod method in methods.Values)
                        allMethods = RT.conj(allMethods, method);
                    if (variadicMethod != null)
                        allMethods = RT.conj(allMethods, variadicMethod);

                    fn._methods = allMethods;
                    fn._variadicMethod = variadicMethod;
                    fn.Keywords = (IPersistentMap)Compiler.KeywordsVar.deref();
                    fn.Vars = (IPersistentMap)Compiler.VarsVar.deref();
                    fn.Constants = (PersistentVector)Compiler.ConstantsVar.deref();
                    fn.KeywordCallsites = (IPersistentVector)Compiler.KeywordCallsitesVar.deref();
                    fn.ProtocolCallsites = (IPersistentVector)Compiler.ProtocolCallsitesVar.deref();
                    fn.VarCallsites = (IPersistentSet)Compiler.VarCallsitesVar.deref();

                    fn._constantsID = RT.nextID();
                }
                finally
                {
                    Var.popThreadBindings();
                }


                IPersistentMap fmeta = RT.meta(origForm);
                if (fmeta != null)
                    fmeta = fmeta.without(RT.LineKey).without(RT.ColumnKey).without(RT.SourceSpanKey).without(RT.FileKey);
                fn._hasMeta = RT.count(fmeta) > 0;


                IPersistentVector primTypes = PersistentVector.EMPTY;
                foreach (string typename in prims)
                    primTypes = primTypes.cons(Type.GetType(typename));

                fn.Compile(
                    fn.IsVariadic ? typeof(RestFn) : typeof(AFunction),
                    null,
                    primTypes,
                    fn._onceOnly,
                    newContext);

                if (fn.SupportsMeta)
                    return new MetaExpr(fn, MapExpr.Parse(pcon.EvalOrExpr(), fmeta));
                else
                    return fn;

            }
            finally
            {
                if (newContext != null)
                    Var.popThreadBindings();
            }
        }

        internal void AddMethod(FnMethod method)
        {
            _methods = RT.conj(_methods,method);
        }


        static bool HasPrimDecls(ISeq forms)
        {
            for (ISeq s = forms; s != null; s = RT.next(s))
                if (FnMethod.HasPrimInterface((ISeq)RT.first(s)))
                    return true;

            return false;
        }

        static readonly MethodInfo Method_FnExpr_GetDynMethod = typeof(FnExpr).GetMethod("GetDynMethod");
        static readonly MethodInfo Method_FnExpr_GetCompiledConstants = typeof(FnExpr).GetMethod("GetCompiledConstants");

        static readonly Dictionary<int, Dictionary<int, DynamicMethod > > DynMethodMap = new Dictionary<int,Dictionary<int,DynamicMethod>>();
        static readonly Dictionary<int, object[]> ConstantsMap = new Dictionary<int, object[]>();

        public static DynamicMethod GetDynMethod(int key, int arity)
        {
            DynamicMethod dm = DynMethodMap[key][arity];
            if (dm == null)
                Console.WriteLine("Bad dynmeth retrieval");
            return dm;
        //    Dictionary<int, WeakReference > dict = DynMethodMap[key];
        //    WeakReference wr = dict[arity];
        //    return (DynamicMethod)wr.Target;
        }
         
        public static object[] GetCompiledConstants(int key)
        {
            return ConstantsMap[key];
            //WeakReference wr = ConstantsMap[key];
            //return (object[])wr.Target;
        }

        private static int GetMethodKey(FnMethod method)
        {
            int arity = method.IsVariadic 
                ? method.RequiredArity + 1  // to avoid the non-variadics, the last of which may have NumParams == this method RequireArity
                : method.NumParams;

            return arity;
        }

        private void EmitGetDynMethod(int arity, CljILGen ilg)
        {            
            ilg.EmitInt(DynMethodMapKey);
            ilg.EmitInt(arity);
            ilg.Emit(OpCodes.Call,Method_FnExpr_GetDynMethod);
        }

        private void EmitGetCompiledConstants(CljILGen ilg)
        {
            ilg.EmitInt(DynMethodMapKey);
            ilg.Emit(OpCodes.Call, Method_FnExpr_GetCompiledConstants);
        }

        #endregion

        #region eval

        public override object Eval()
        {
            return base.Eval();
        }

        #endregion

        #region Code generation

        internal void EmitForDefn(ObjExpr objx, CljILGen ilg)
        {
            Emit(RHC.Expression, objx, ilg);
        }

        protected override void EmitMethods(TypeBuilder tb)
        {
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.Emit(this, tb);
            }

            if (IsVariadic)
                EmitGetRequiredArityMethod(_typeBuilder, _variadicMethod.RequiredArity);

            List<int> supportedArities = new List<int>();
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                supportedArities.Add(method.NumParams);
            }

            EmitHasArityMethod(_typeBuilder, supportedArities, IsVariadic, IsVariadic ? _variadicMethod.RequiredArity : 0);
        }

        static void EmitGetRequiredArityMethod(TypeBuilder tb, int requiredArity)
        {
            MethodBuilder mb = tb.DefineMethod(
                "getRequiredArity",
                MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(int),
                Type.EmptyTypes);

            CljILGen gen = new CljILGen(mb.GetILGenerator());
            gen.EmitInt(requiredArity);
            gen.Emit(OpCodes.Ret);
        }

        #endregion
    }
}
