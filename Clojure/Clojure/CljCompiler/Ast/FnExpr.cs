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


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private Dictionary<int, DynamicMethod> _dynMethodMap;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private object[] _compiledConstants;

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

            // Uncomment -if- to enable light compilation  (and see below)
            //bool hasPrimDecls = HasPrimDecls((ISeq)RT.next(form));
            //if (Compiler.IsCompiling || hasPrimDecls || fn.IsStatic)
            //{
                GenContext context = Compiler.CompilerContextVar.deref() as GenContext ?? Compiler.EvalContext;
                newContext = context.WithNewDynInitHelper(fn.InternalName + "__dynInitHelper_" + RT.nextID().ToString());
                Var.pushThreadBindings(RT.map(Compiler.CompilerContextVar, newContext));
            //}

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


                // Uncomment if/else to enable light compilation (and see above)
                //if (Compiler.IsCompiling || prims.Count > 0|| fn.IsStatic)
                //{

                    IPersistentVector primTypes = PersistentVector.EMPTY;
                    foreach (string typename in prims)
                        primTypes = primTypes.cons(Type.GetType(typename));

                        fn.Compile(
                            fn.IsVariadic ? typeof(RestFn) : typeof(AFunction),
                            null,
                            primTypes,
                            fn._onceOnly,
                            newContext);
                //}
                //else
                //{
                //    fn.FnMode = FnMode.Light;
                //    fn.LightCompile(fn.GetPrecompiledType(), Compiler.EvalContext);
                //}

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

        void LightCompile(Type compiledType, GenContext context)
        {
            if (_compiledType != null)
                return;

            _compiledType = compiledType;

            LightCompileMethods();
            LightCompileConstants();
        }

        void LightCompileMethods()
        {
            Dictionary<int, DynamicMethod> dict = new Dictionary<int, DynamicMethod>();

            // Create a dynamic method that takes an array of closed-over values
            // and returns an instance of AFnImpl.

            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.LightEmit(this, CompiledType);
                int key = GetMethodKey(method);

                //dict[key] = new WeakReference(method.DynMethod);
                dict[key] = method.DynMethod;
            }

            DynMethodMap[DynMethodMapKey] = dict;
            _dynMethodMap = dict;
        }

        void LightCompileConstants()
        {
            object[] cs = new object[Constants.count()];

            for (int i = 0; i < Constants.count(); i++)
            {
                cs[i] = Constants.nth(i);
            }

            //ConstantsMap[DynMethodMapKey] = new WeakReference(cs);
            ConstantsMap[DynMethodMapKey] = cs;
            _compiledConstants = cs;
        }


        static readonly MethodInfo Method_DynamicMethod_CreateDelegate = typeof(DynamicMethod).GetMethod("CreateDelegate", new Type[] { typeof(Type), typeof(object) });
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


        void LightEmit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {

            //emitting a Fn means constructing an instance, feeding closed-overs from enclosing scope, if any
            //objx arg is enclosing objx, not this

            
            // Create the function instance
            LocalBuilder fnLocal = ilg.DeclareLocal(CompiledType);
            
            if (CompiledType == typeof(RestFnImpl))
            {
                ilg.EmitInt(_variadicMethod.RequiredArity);
                ilg.EmitNew(Compiler.Ctor_RestFnImpl_1);
            }
            else
            {
                ilg.EmitNew(Compiler.Ctor_AFnImpl);
            }

            ilg.Emit(OpCodes.Stloc, fnLocal);

            //ilg.EmitString(String.Format("Creating fn {0}", Name));
            //ilg.Emit(OpCodes.Call, typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(string) }));

            // Set up the methods

            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                int key = GetMethodKey(method);
 
                string fieldName = IsVariadic && method.IsVariadic
                    ? "_fnDo" + (key - 1)  // because key is arity+1 for variadic
                    : "_fn" + key;

                FieldInfo fi = CompiledType.GetField(fieldName);

                ilg.Emit(OpCodes.Ldloc, fnLocal);


                EmitGetDynMethod(key, ilg);
                ilg.EmitType(fi.FieldType);
                ilg.Emit(OpCodes.Ldloc, fnLocal);
                ilg.Emit(OpCodes.Callvirt, Method_DynamicMethod_CreateDelegate);
                ilg.Emit(OpCodes.Castclass, fi.FieldType);

                ilg.MaybeEmitVolatileOp(fi);
                ilg.EmitFieldSet(fi);
            }



            // setup the constants and locals
            ilg.Emit(OpCodes.Ldloc, fnLocal);

            if (Constants.count() > 0)
            {
                EmitGetCompiledConstants(ilg);
            }
            else
            {
                ilg.EmitInt(0);
                ilg.EmitArray(typeof(Object[]));
            }

            if (Closes.count() > 0)
            {

                int maxIndex = Closes.Max(c => ((LocalBinding)c.key()).Index);

                ilg.EmitInt(maxIndex + 1);
                ilg.Emit(OpCodes.Newarr, typeof(object));

                for (ISeq s = RT.keys(Closes); s != null; s = s.next())
                {
                    LocalBinding lb = (LocalBinding)s.first();
                    ilg.Emit(OpCodes.Dup);
                    ilg.EmitInt(lb.Index);
                    objx.EmitLocal(ilg, lb);
                    ilg.EmitStoreElement(typeof(object));
                }
            }
            else
            {
                ilg.EmitInt(0);
                ilg.EmitArray(typeof(Object[]));
            }

            // Create the closure
            ilg.EmitNew(Compiler.Ctor_Closure_2);

            // Assign the clojure
            ilg.EmitCall(Compiler.Method_IFnClosure_SetClosure);

            // Leave the instance on the stack.
            ilg.Emit(OpCodes.Ldloc, fnLocal);
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
            if (FnMode == FnMode.Full)
                return base.Eval();
            else
            {
                DynamicMethod dyn = new DynamicMethod("__fnEval_" + RT.nextID(), typeof(object), Type.EmptyTypes,true);
                CljILGen ilg = new CljILGen(dyn.GetILGenerator());

                LightEmit(RHC.Expression, this /* WRONG!!! */, ilg);
                ilg.Emit(OpCodes.Ret);
                Delegate dlg = dyn.CreateDelegate(typeof(Compiler.ReplDelegate));
                return dlg.DynamicInvoke();
            }
        }

        #endregion

        #region Immediate mode compilation

        protected Type GetPrecompiledType()
        {
            return IsVariadic ? typeof(RestFnImpl) : typeof(AFnImpl);
        }

        //Expression GenImmediateCode(RHC rhc, ObjExpr objx, GenContext context)
        //{
        //    ParameterExpression p1 = Expression.Parameter(CompiledType, "__x__");
        //    _thisParam = p1;

        //    List<Expression> exprs = new List<Expression>();

        //    if (CompiledType == typeof(RestFnImpl))
        //        exprs.Add(Expression.Assign(p1,
        //                  Expression.New(Compiler.Ctor_RestFnImpl_1, Expression.Constant(_variadicMethod.RequiredArity))));
        //    else
        //        exprs.Add(Expression.Assign(p1, Expression.New(p1.Type)));

        //    for (ISeq s = RT.seq(_methods); s != null; s = s.next())
        //    {
        //        FnMethod method = (FnMethod)s.first();
        //        LambdaExpression lambda = method.GenerateImmediateLambda(rhc,this,context);
        //        string fieldName = IsVariadic && method.IsVariadic
        //            ? "_fnDo" + method.RequiredArity
        //            : "_fn" + method.NumParams;
        //        exprs.Add(Expression.Assign(Expression.Field(p1, fieldName), lambda));
        //    }

        //    exprs.Add(p1);

        //    Expression expr = Expression.Block(new ParameterExpression[] { p1 }, exprs);
        //    return expr;
        //}

        #endregion

        #region Code generation

        internal void EmitForDefn(ObjExpr objx, CljILGen ilg)
        {
            Emit(RHC.Expression, objx, ilg);
        }

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            if (FnMode == FnMode.Full)
                base.Emit(rhc, objx, ilg);
            else
                LightEmit(rhc, objx, ilg);
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
