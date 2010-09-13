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
using System.Text;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    class FnExpr : ObjExpr
    {
        #region Data

        static readonly Keyword KW_ONCE = Keyword.intern(null, "once");

        FnMethod _variadicMethod = null;

        bool IsVariadic { get { return _variadicMethod != null; } }

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
            ObjMethod enclosingMethod = (ObjMethod)Compiler.METHOD.deref();

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

        public bool HasClrType
        {
            get
            {
                return true;
            }
        }

        public Type ClrType
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

            try
            {
                Var.pushThreadBindings(RT.map(
                    Compiler.CONSTANTS, PersistentVector.EMPTY,
                    Compiler.CONSTANT_IDS, new IdentityHashMap(),
                    Compiler.KEYWORDS, PersistentHashMap.EMPTY,
                    Compiler.VARS, PersistentHashMap.EMPTY,
                    Compiler.KEYWORD_CALLSITES,PersistentVector.EMPTY,
                    Compiler.PROTOCOL_CALLSITES,PersistentVector.EMPTY,
                    Compiler.VAR_CALLSITES,PersistentVector.EMPTY));

                //arglist might be preceded by symbol naming this fn
                if (RT.second(form) is Symbol)
                {
                    Symbol nm = (Symbol)RT.second(form);
                    fn._thisName = nm.Name;
                    fn.IsStatic = RT.booleanCast(RT.get(nm.meta(), Compiler.STATIC_KEY));
                    form = RT.cons(Compiler.FN, RT.next(RT.next(form)));
                }

                // Normalize body
			    //now (fn [args] body...) or (fn ([args] body...) ([args2] body2...) ...)
			    //turn former into latter
                if (RT.second(form) is IPersistentVector)
                    form = RT.list(Compiler.FN, RT.next(form));

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
                fn.Keywords = (IPersistentMap)Compiler.KEYWORDS.deref();
                fn.Vars = (IPersistentMap)Compiler.VARS.deref();
                fn.Constants = (PersistentVector)Compiler.CONSTANTS.deref();
                fn.KeywordCallsites = (IPersistentVector)Compiler.KEYWORD_CALLSITES.deref();
                fn.ProtocolCallsites = (IPersistentVector)Compiler.PROTOCOL_CALLSITES.deref();
                fn.VarCallsites = (IPersistentVector)Compiler.VAR_CALLSITES.deref();

                fn._constantsID = RT.nextID();
            }
            finally
            {
                Var.popThreadBindings();
            }

            if (Compiler.IsCompiling)
            {
                GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? Compiler.EvalContext;
                GenContext genC = context.WithNewDynInitHelper(fn.InternalName + "__dynInitHelper_" + RT.nextID().ToString());

                fn.Compile(fn.IsVariadic ? typeof(RestFn) : typeof(AFunction), PersistentVector.EMPTY, fn.OnceOnly, genC);
            }
            else
            {
                fn.CompiledType = fn.GetPrecompiledType();
                fn._fnMode = FnMode.Light;
            }

            if (origForm is IObj && ((IObj)origForm).meta() != null)
                return new MetaExpr(fn, (MapExpr)MapExpr.Parse(pcon.EvEx(),((IObj)origForm).meta()));
            else
                return fn;
        }

        //internal Type Compile()
        //{
        //    // Needs its own GenContext so it has its own DynInitHelper
        //    GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? Compiler.EvalContext;
        //    GenContext genC = context.WithNewDynInitHelper(InternalName + "__dynInitHelper_" + RT.nextID().ToString());

        //    _superType = GetSuperType();
        //    return GenerateClass(genC);
        //}

        internal void AddMethod(FnMethod method)
        {
            _methods = RT.conj(_methods,method);
        }

        #endregion

        #region eval

        public override object Eval()
        {
            if (_fnMode == FnMode.Full)
                return base.Eval();

            Expression fn = GenImmediateCode(RHC.Expression, this, Compiler.EvalContext);
            Expression<Compiler.ReplDelegate> lambdaForCompile = Expression.Lambda<Compiler.ReplDelegate>(Expression.Convert(fn, typeof(Object)), "ReplCall", null);
            return lambdaForCompile.Compile().Invoke();

        }

        #endregion

        #region Code generation

        //public override Expression GenDlr(GenContext context)
        //{
        //    return base.GenDlr(context);
        //}


        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            if (_fnMode == FnMode.Full)
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


        //private static Type GetBaseClass(GenContext context, Type superType)
        //{
        //    Type baseClass = LookupBaseClass(superType);
        //    if (baseClass != null)
        //        return baseClass;

        //    baseClass = GenerateBaseClass(context, superType);
        //    baseClass = RegisterBaseClass(superType, baseClass);
        //    return baseClass;
        //}

        //static AtomicReference<IPersistentMap> _baseClassMapRef = new AtomicReference<IPersistentMap>(PersistentHashMap.EMPTY);

        //static FnExpr()
        //{
        //    _baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(RestFn), typeof(RestFnImpl)));
        //    //_baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(AFn),typeof(AFnImpl)));
        //}


        //private static Type LookupBaseClass(Type superType)
        //{
        //    return (Type)_baseClassMapRef.Get().valAt(superType);
        //}

        //private static Type RegisterBaseClass(Type superType, Type baseType)
        //{
        //    IPersistentMap map = _baseClassMapRef.Get();

        //    while (!map.containsKey(superType))
        //    {
        //        IPersistentMap newMap = map.assoc(superType, baseType);
        //        _baseClassMapRef.CompareAndSet(map, newMap);
        //        map = _baseClassMapRef.Get();
        //    }

        //    return LookupBaseClass(superType);  // may not be the one we defined -- race condition
        //}


        //private static Type GenerateBaseClass(GenContext context, Type superType)
        //{
        //    return AFnImplGenerator.Create(context, superType);
        //}



        //protected override Expression GenDlrImmediate(GenContext context)
        //{
        //    //_baseType = GetBaseClass(context, _superType);
        //    return GenerateImmediateLambda(context, _baseType);
        //}

        //protected Type GetSuperType()
        //{
        //    //return _superName != null
        //    //    ? Type.GetType(_superName)
        //    //    : IsVariadic
        //    //    ? typeof(RestFn)
        //    //    : typeof(AFunction);
        //    return IsVariadic
        //        ? typeof(RestFn)
        //        : typeof(AFunction);
        //}

        //private Expression GenerateImmediateLambda(GenContext context, Type baseClass)
        //{
        //    ParameterExpression p1 = Expression.Parameter(baseClass, "__x__");
        //    _thisParam = p1;

        //    List<Expression> exprs = new List<Expression>();

        //    if (baseClass == typeof(RestFnImpl))
        //        exprs.Add(Expression.Assign(p1,
        //                  Expression.New(Compiler.Ctor_RestFnImpl_1, Expression.Constant(_variadicMethod.RequiredArity))));
        //    else
        //        exprs.Add(Expression.Assign(p1, Expression.New(p1.Type)));

        //    GenContext newContext = CreateContext(context, null, baseClass);

        //    for (ISeq s = RT.seq(_methods); s != null; s = s.next())
        //    {
        //        FnMethod method = (FnMethod)s.first();
        //        LambdaExpression lambda = method.GenerateImmediateLambda(newContext);
        //        string fieldName = IsVariadic && method.IsVariadic
        //            ? "_fnDo" + method.RequiredArity
        //            : "_fn" + method.NumParams;
        //        exprs.Add(Expression.Assign(Expression.Field(p1, fieldName), lambda));
        //    }

        //    exprs.Add(p1);

        //    Expression expr = Expression.Block(new ParameterExpression[] { p1 }, exprs);
        //    return expr;
        //}

        //private static Type GetBaseClass(GenContext context, Type superType)
        //{
        //    Type baseClass = LookupBaseClass(superType);
        //    if (baseClass != null)
        //        return baseClass;

        //    baseClass = GenerateBaseClass(context, superType);
        //    baseClass = RegisterBaseClass(superType, baseClass);
        //    return baseClass;
        //}

        //static AtomicReference<IPersistentMap> _baseClassMapRef = new AtomicReference<IPersistentMap>(PersistentHashMap.EMPTY);

        //static FnExpr()
        //{
        //    _baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(RestFn), typeof(RestFnImpl)));
        //    //_baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(AFn),typeof(AFnImpl)));
        //}


        //private static Type LookupBaseClass(Type superType)
        //{
        //    return (Type)_baseClassMapRef.Get().valAt(superType);
        //}

        //private static Type RegisterBaseClass(Type superType, Type baseType)
        //{
        //    IPersistentMap map = _baseClassMapRef.Get();

        //    while (!map.containsKey(superType))
        //    {
        //        IPersistentMap newMap = map.assoc(superType, baseType);
        //        _baseClassMapRef.CompareAndSet(map, newMap);
        //        map = _baseClassMapRef.Get();
        //    }

        //    return LookupBaseClass(superType);  // may not be the one we defined -- race condition
        //}


        //private static Type GenerateBaseClass(GenContext context, Type superType)
        //{
        //    return AFnImplGenerator.Create(context, superType);
        //}



        #endregion

        #region not yet
        /* 




        protected override Type GetBaseClass(GenContext context, Type superType)
        {
            if (superType == typeof(RestFn))
            {
                int reqArity = _variadicMethod.RequiredArity;
                Type baseClass = LookupRestFnBaseClass(reqArity);
                if (baseClass != null)
                    return baseClass;

                baseClass = GenerateRestFnBaseClass(context, reqArity);
                baseClass = RegisterRestFnBaseClass(reqArity, baseClass);
                return baseClass;

            }

            return base.GetBaseClass(context, superType);
        }


        static AtomicReference<IPersistentMap> _restFnClassMapRef = new AtomicReference<IPersistentMap>(PersistentHashMap.EMPTY);

        //static ObjExpr()
        //{
        //    _baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(RestFn),typeof(RestFnImpl)));
        //    //_baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(AFn),typeof(AFnImpl)));
        //}


        private static Type LookupRestFnBaseClass(int reqArity)
        {
            return (Type)_restFnClassMapRef.Get().valAt(reqArity);
        }

        private static Type RegisterRestFnBaseClass( int reqArity, Type baseType)
        {
            IPersistentMap map = _restFnClassMapRef.Get();

            while (!map.containsKey(reqArity))
            {
                IPersistentMap newMap = map.assoc(reqArity, baseType);
                _restFnClassMapRef.CompareAndSet(map, newMap);
                map = _restFnClassMapRef.Get();
            }

            return LookupRestFnBaseClass(reqArity);  // may not be the one we defined -- race condition
        }


        private static Type GenerateRestFnBaseClass(GenContext context, int reqArity)
        {
            string name = "RestFnImpl__" + reqArity.ToString();
            TypeBuilder baseTB = context.AssemblyGen.DefinePublicType(name, typeof(RestFnImpl), true);

            GenerateGetRequiredArityMethod(baseTB, reqArity);

            return baseTB.CreateType();
        }





        
        */
        #endregion

        #region Class generation

        //public override FnMode CompileMode()
        //{
        //    return FnMode.Light;
        //}

        //protected override Type GenerateClassForImmediate(GenContext context)
        //{
        //    //if (_protocolCallsites.count() > 0)
        //    //{
        //    //    context = context.ChangeMode(CompilerMode.File);
        //    //    return GenerateClassForFile(context);
        //    //}

        //    ObjType = _baseType = GetBaseClass(context, _superType);
        //    return _baseType;
        //}

        //protected override Type GenerateClassForFile(GenContext context)
        //{
        //    return EnsureTypeBuilt(context);
        //}

        #endregion 

    }
}
