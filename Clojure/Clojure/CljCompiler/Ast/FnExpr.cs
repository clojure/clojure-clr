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
using System.Linq;

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Scripting.Generation;

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

            Name = baseName + simpleName;
            InternalName = Name.Replace('.', '/');
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

            // Java: fn.objtype = Type.getObjectType(fn.internalName) -- makes no sense for us, this is ASM only.
            
            List<string> prims = new List<string>();

            try
            {
                Var.pushThreadBindings(RT.map(
                    Compiler.ConstantsVar, PersistentVector.EMPTY,
                    Compiler.ConstantIdsVar, new IdentityHashMap(),
                    Compiler.KeywordsVar, PersistentHashMap.EMPTY,
                    Compiler.VarsVar, PersistentHashMap.EMPTY,
                    Compiler.KeywordCallsitesVar,PersistentVector.EMPTY,
                    Compiler.ProtocolCallsitesVar,PersistentVector.EMPTY,
                    Compiler.VarCallsitesVar,Compiler.EmptyVarCallSites(),
                    Compiler.NoRecurVar,null));

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

                SortedDictionary<int, FnMethod> methods = new SortedDictionary<int, FnMethod>();
                FnMethod variadicMethod = null;

                for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                {
                    FnMethod f = FnMethod.Parse(fn, (ISeq)RT.first(s),fn.IsStatic);
                    if (f.IsVariadic)
                    {
                        if (variadicMethod == null)
                            variadicMethod = f;
                        else
                            throw new Exception("Can't have more than 1 variadic overload");
                    }
                    else if (!methods.ContainsKey(f.RequiredArity))
                        methods[f.RequiredArity] = f;
                    else
                        throw new Exception("Can't have 2 overloads with the same arity.");
                    if (f.Prim != null)
                        prims.Add(f.Prim);
                }

                if (variadicMethod != null && methods.Count > 0 && methods.Keys.Max() >= variadicMethod.NumParams)
                    throw new Exception("Can't have fixed arity methods with more params than the variadic method.");

                if ( fn.IsStatic && fn.Closes.count() > 0 )
                    throw new ArgumentException("static fns can't be closures");

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
                fmeta = fmeta.without(RT.LINE_KEY).without(RT.FILE_KEY);
            fn._hasMeta = RT.count(fmeta) > 0;

            if (Compiler.IsCompiling || prims.Count > 0)
            {
                GenContext context = Compiler.CompilerContextVar.get() as GenContext ?? Compiler.EvalContext;
                GenContext genC = context.WithNewDynInitHelper(fn.InternalName + "__dynInitHelper_" + RT.nextID().ToString());

                IPersistentVector primTypes = PersistentVector.EMPTY;
                foreach (string typename in prims)
                    primTypes = primTypes.cons(Type.GetType(typename));

                fn.Compile(
                    fn.IsVariadic ? typeof(RestFn) : typeof(AFunction), 
                    null,
                    primTypes,
                    fn.OnceOnly, 
                    genC);
            }
            else
            {
                fn.CompiledType = fn.GetPrecompiledType();
                fn.FnMode = FnMode.Light;
            }

            if (fn.SupportsMeta)
                return new MetaExpr(fn, MapExpr.Parse(pcon.EvEx(),fmeta));
            else
                return fn;
        }

        internal void AddMethod(FnMethod method)
        {
            _methods = RT.conj(_methods,method);
        }

        #endregion

        #region eval

        public override object Eval()
        {
            if (FnMode == FnMode.Full)
                return base.Eval();

            Expression fn = GenImmediateCode(RHC.Expression, this, Compiler.EvalContext);
            Expression<Compiler.ReplDelegate> lambdaForCompile = Expression.Lambda<Compiler.ReplDelegate>(Expression.Convert(fn, typeof(Object)), "ReplCall", null);
            return lambdaForCompile.Compile().Invoke();

        }

        #endregion

        #region Code generation

        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            if (FnMode == FnMode.Full)
                return base.GenCode(rhc, objx, context);

            return GenImmediateCode(rhc, objx, context);
        }

        protected override void GenerateMethods(GenContext context)
        {
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.GenerateCode(this,context);
            }

            if (IsVariadic)
                GenerateGetRequiredArityMethod(TypeBuilder, _variadicMethod.RequiredArity);
        }

        static MethodBuilder GenerateGetRequiredArityMethod(TypeBuilder tb, int requiredArity)
        {
            MethodBuilder mb = tb.DefineMethod(
                "getRequiredArity",
                MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(int),
                Type.EmptyTypes);

            ILGen gen = new ILGen(mb.GetILGenerator());
            gen.EmitInt(requiredArity);
            gen.Emit(OpCodes.Ret);

            return mb;
        }
        
        #endregion

        #region Immediate mode compilation

        protected Type GetPrecompiledType()
        {
            return IsVariadic ? typeof(RestFnImpl) : typeof(AFnImpl);
        }

        Expression GenImmediateCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            ParameterExpression p1 = Expression.Parameter(CompiledType, "__x__");
            _thisParam = p1;

            List<Expression> exprs = new List<Expression>();

            if (CompiledType == typeof(RestFnImpl))
                exprs.Add(Expression.Assign(p1,
                          Expression.New(Compiler.Ctor_RestFnImpl_1, Expression.Constant(_variadicMethod.RequiredArity))));
            else
                exprs.Add(Expression.Assign(p1, Expression.New(p1.Type)));

            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                LambdaExpression lambda = method.GenerateImmediateLambda(rhc,this,context);
                string fieldName = IsVariadic && method.IsVariadic
                    ? "_fnDo" + method.RequiredArity
                    : "_fn" + method.NumParams;
                exprs.Add(Expression.Assign(Expression.Field(p1, fieldName), lambda));
            }

            exprs.Add(p1);

            Expression expr = Expression.Block(new ParameterExpression[] { p1 }, exprs);
            return expr;
        }


        #endregion
    }
}
