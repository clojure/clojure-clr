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

        bool _onceOnly = false;

        #endregion

        #region not yet
        /*
         * 
         * 
        static readonly Keyword KW_SUPER_NAME = Keyword.intern(null, "super-name");

        string _simpleName;



        //int _line;
         
         */

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
                : Compiler.Munge(Compiler.CurrentNamespace.Name.Name) + "$";

            if (RT.second(form) is Symbol)
                name = ((Symbol)RT.second(form)).Name;

            string simpleName = (name == null ? "fn" : Compiler.Munge(name).Replace(".", "_DOT_")) + "__" + RT.nextID();
            
            Name = baseName + simpleName;
            InternalName = Name.Replace('.', '/');

            _objType = RT.classForName(InternalName);
            // fn.fntype = Type.getObjectType(fn.internalName) -- JAVA            
        }

        #endregion

        #region Parsing

        public static Expr Parse(object frm, string name, bool isRecurContext)
        {
            ISeq form = (ISeq)frm;

            FnExpr fn = new FnExpr(Compiler.TagOf(form));

            if (((IMeta)form.first()).meta() != null)
            {
                fn._onceOnly = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ONCE));
            }


            fn.ComputeNames(form, name);

            try
            {
                Var.pushThreadBindings(RT.map(
                    Compiler.CONSTANTS, PersistentVector.EMPTY,
                    Compiler.KEYWORDS, PersistentHashMap.EMPTY,
                    Compiler.VARS, PersistentHashMap.EMPTY,
                    Compiler.KEYWORD_CALLSITES,PersistentVector.EMPTY,
                    Compiler.PROTOCOL_CALLSITES,PersistentVector.EMPTY,
                    Compiler.VAR_CALLSITES,PersistentVector.EMPTY));

                //arglist might be preceded by symbol naming this fn
                if (RT.second(form) is Symbol)
                {
                    fn._thisName = ((Symbol)RT.second(form)).Name;
                    form = RT.cons(Compiler.FN, RT.next(RT.next(form)));
                }

                //  Added to improve stack trace messages.
                //  This seriously hoses compilation of core.clj.  Needs investigation.
                //  Backing this out.
                //else if (name != null)
                //{
                //    fn._thisName = name;
                //}

                // Normalize body
                // If it is (fn [arg...] body ...), turn it into
                //          (fn ([arg...] body...))
                // so that we can treat uniformly as (fn ([arg...] body...) ([arg...] body...) ... )
                if (RT.second(form) is IPersistentVector)
                    form = RT.list(Compiler.FN, RT.next(form));

                SortedDictionary<int, FnMethod> methods = new SortedDictionary<int, FnMethod>();
                FnMethod variadicMethod = null;

                for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                {
                    FnMethod f = FnMethod.Parse(fn, (ISeq)RT.first(s));
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

                IPersistentCollection allMethods = null;
                foreach (FnMethod method in methods.Values)
                    allMethods = RT.conj(allMethods, method);
                if (variadicMethod != null)
                    allMethods = RT.conj(allMethods, variadicMethod);

                fn._methods = allMethods;
                fn._variadicMethod = variadicMethod;
                fn._keywords = (IPersistentMap)Compiler.KEYWORDS.deref();
                fn._vars = (IPersistentMap)Compiler.VARS.deref();
                fn._constants = (PersistentVector)Compiler.CONSTANTS.deref();
                fn._keywordCallsites = (IPersistentVector)Compiler.KEYWORD_CALLSITES.deref();
                fn._protocolCallsites = (IPersistentVector)Compiler.PROTOCOL_CALLSITES.deref();
                fn._varCallsites = (IPersistentVector)Compiler.VAR_CALLSITES.deref();

                fn._constantsID = RT.nextID();
            }
            finally
            {
                Var.popThreadBindings();
            }
            
            // JAVA: fn.compile();
            fn._superType = fn.GetSuperType();
            fn.GenerateClass();
            return fn;
        }

        #endregion

        #region Class generation

        protected override Type GenerateClassForImmediate(GenContext context)
        {
            _baseType = GetBaseClass(context, _superType);
            return _baseType;
        }


        protected override Type GenerateClassForFile(GenContext context)
        {
            // Needs its own GenContext so it has its own DynInitHelper
            GenContext genC = context.WithNewDynInitHelper(InternalName + "__dynInitHelper_" + RT.nextID().ToString() );
            return EnsureTypeBuilt(genC);

        }

        #endregion 

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            return base.GenDlr(context);
        }


        protected override void GenerateMethods(GenContext context)
        {
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.GenerateCode(context);
            }

            if (IsVariadic)
            {
                TypeBuilder tb = context.ObjExpr.TypeBuilder;
                GenerateGetRequiredArityMethod(tb, _variadicMethod.RequiredArity);
            }
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


        protected override Expression GenDlrImmediate(GenContext context)
        {
            //_baseType = GetBaseClass(context, _superType);
            return GenerateImmediateLambda(context, _baseType);
        }

        protected Type GetSuperType()
        {
            //return _superName != null
            //    ? Type.GetType(_superName)
            //    : IsVariadic
            //    ? typeof(RestFn)
            //    : typeof(AFunction);
            return IsVariadic
                ? typeof(RestFn)
                : typeof(AFunction);
        }

        private Expression GenerateImmediateLambda(GenContext context, Type baseClass)
        {
            ParameterExpression p1 = Expression.Parameter(baseClass, "__x__");
            _thisParam = p1;

            List<Expression> exprs = new List<Expression>();

            if (baseClass == typeof(RestFnImpl))
                exprs.Add(Expression.Assign(p1,
                          Expression.New(Compiler.Ctor_RestFnImpl_1, Expression.Constant(_variadicMethod.RequiredArity))));
            else
                exprs.Add(Expression.Assign(p1, Expression.New(p1.Type)));

            GenContext newContext = CreateContext(context, null, baseClass);

            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                LambdaExpression lambda = method.GenerateImmediateLambda(newContext);
                string fieldName = IsVariadic && method.IsVariadic
                    ? "_fnDo" + method.RequiredArity
                    : "_fn" + method.NumParams;
                exprs.Add(Expression.Assign(Expression.Field(p1, fieldName), lambda));
            }

            exprs.Add(p1);

            Expression expr = Expression.Block(new ParameterExpression[] { p1 }, exprs);
            return expr;
        }

        private static Type GetBaseClass(GenContext context, Type superType)
        {
            Type baseClass = LookupBaseClass(superType);
            if (baseClass != null)
                return baseClass;

            baseClass = GenerateBaseClass(context, superType);
            baseClass = RegisterBaseClass(superType, baseClass);
            return baseClass;
        }

        static AtomicReference<IPersistentMap> _baseClassMapRef = new AtomicReference<IPersistentMap>(PersistentHashMap.EMPTY);

        static FnExpr()
        {
            _baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(RestFn), typeof(RestFnImpl)));
            //_baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(AFn),typeof(AFnImpl)));
        }


        private static Type LookupBaseClass(Type superType)
        {
            return (Type)_baseClassMapRef.Get().valAt(superType);
        }

        private static Type RegisterBaseClass(Type superType, Type baseType)
        {
            IPersistentMap map = _baseClassMapRef.Get();

            while (!map.containsKey(superType))
            {
                IPersistentMap newMap = map.assoc(superType, baseType);
                _baseClassMapRef.CompareAndSet(map, newMap);
                map = _baseClassMapRef.Get();
            }

            return LookupBaseClass(superType);  // may not be the one we defined -- race condition
        }


        private static Type GenerateBaseClass(GenContext context, Type superType)
        {
            return AFnImplGenerator.Create(context, superType);
        }



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

    }
}
