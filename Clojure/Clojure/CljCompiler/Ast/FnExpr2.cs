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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System;
using System.Collections.Generic;

namespace clojure.lang.CljCompiler.Ast
{
    public class FnExpr2 : ObjExpr2
    {

        public FnExpr2(object tag) 
            :base(tag)
        {
        }

        #region Parsing

        public static Expr Parse(ParserContext pcon, ISeq form, string name)
        {
            ISeq origForm = form;

            FnExpr fn = new FnExpr(Compiler.TagOf(form));
            fn.Src = form;

            if (((IMeta)form.first()).meta() != null)
            {
                fn.OnceOnly = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ONCE));
            }

            fn.ComputeNames(form, name);

            // Java: fn.objtype = Type.getObjectType(fn.internalName) -- makes no sense for us, this is ASM only.

            List<string> prims = new List<string>();



            //arglist might be preceded by symbol naming this fn
            if (RT.second(form) is Symbol)
            {
                Symbol nm = (Symbol)RT.second(form);
                fn._thisName = nm.Name;
                fn.IsStatic = false; // RT.booleanCast(RT.get(nm.meta(), Compiler.STATIC_KEY));
                form = RT.cons(Compiler.FnSym, RT.next(RT.next(form)));
            }

            // Normalize body
            //now (fn [args] body...) or (fn ([args] body...) ([args2] body2...) ...)
            //turn former into latter
            if (RT.second(form) is IPersistentVector)
                form = RT.list(Compiler.FnSym, RT.next(form));

            bool hasPrimDecls = HasPrimDecls((ISeq)RT.next(form));


            GenContext newContext = null;

            if (Compiler.IsCompiling || hasPrimDecls)
            {
                GenContext context = Compiler.CompilerContextVar.get() as GenContext ?? Compiler.EvalContext;
                newContext = context.WithNewDynInitHelper(fn.InternalName + "__dynInitHelper_" + RT.nextID().ToString());
                Var.pushThreadBindings(RT.map(Compiler.CompilerContextVar, newContext));
            }

            try
            {
                try
                {
                    Var.pushThreadBindings(RT.map(
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
                        FnMethod f = FnMethod.Parse(fn, (ISeq)RT.first(s), fn.IsStatic);
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

                    if (fn.IsStatic && fn.Closes.count() > 0)
                        throw new ParseException("static fns can't be closures");

                    IPersistentCollection allMethods = null;
                    foreach (FnMethod method in methods.Values)
                        allMethods = RT.conj(allMethods, method);
                    if (variadicMethod != null)
                        allMethods = RT.conj(allMethods, variadicMethod);

                    fn.Methods = allMethods;
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
                    fmeta = fmeta.without(RT.LineKey).without(RT.FileKey);
                fn._hasMeta = RT.count(fmeta) > 0;


                //if (Compiler.IsCompiling || prims.Count > 0)
                if (Compiler.IsCompiling)
                {
                    //GenContext context = Compiler.CompilerContextVar.get() as GenContext ?? Compiler.EvalContext;
                    //GenContext genC = context.WithNewDynInitHelper(fn.InternalName + "__dynInitHelper_" + RT.nextID().ToString());

                    IPersistentVector primTypes = PersistentVector.EMPTY;
                    foreach (string typename in prims)
                        primTypes = primTypes.cons(Type.GetType(typename));

                    fn.Compile(
                        fn.IsVariadic ? typeof(RestFn) : typeof(AFunction),
                        null,
                        primTypes,
                        fn.OnceOnly,
                        newContext);
                }
                else
                {
                    fn.CompiledType = fn.GetPrecompiledType();
                    fn.FnMode = FnMode.Light;
                }

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
            Methods = RT.conj(Methods, method);
        }

        internal void EmitForDefn(ObjExpr2 objx, GenContext context)
        {
            throw new NotImplementedException();
        }
    }
}
