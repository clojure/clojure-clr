/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using clojure.lang.CljCompiler.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class FnExpr(object tag) : ObjExpr(tag)
    {
        #region Data

        static readonly Keyword KW_ONCE = Keyword.intern(null, "once");

#if NET11_0_OR_GREATER
        static readonly Keyword KW_ASYNC = Keyword.intern(null, "async");
        public bool IsAsync { get; private set; }
#endif

        FnMethod _variadicMethod = null;
        public FnMethod VariadicMethod => _variadicMethod;
        bool IsVariadic => _variadicMethod is not null;

        bool _hasMeta;
        protected override bool SupportsMeta => _hasMeta;

        bool _hasEnclosingMethod;

        private readonly int _dynMethodMapKey = RT.nextID();
        public int DynMethodMapKey => _dynMethodMapKey;

        Type _cachedType;

        #endregion

        #region Misc

        // This naming convention drawn from the Java code.
        internal void ComputeNames(ISeq form, string name)
        {
            ObjMethod enclosingMethod = (ObjMethod)Compiler.MethodVar.deref();

            string baseName = enclosingMethod is not null
                ? enclosingMethod.Objx.Name
                : Compiler.munge(Compiler.CurrentNamespace.Name.Name) + "$";

            Symbol nm = RT.second(form) as Symbol;

            if (nm != null)
            {
                name = nm.Name + "__" + RT.nextID();
            }
            else
            {
                if (name is null)
                    name = "fn__" + RT.nextID();
                else if (enclosingMethod != null)
                    name += "__" + RT.nextID();
            }

            string simpleName = Compiler.munge(name).Replace(".", "_DOT_");

            Name = baseName + simpleName;
            InternalName = Name;  //  Name.Replace('.', '/');
        }

        #endregion

        #region Type munging

        public override bool HasClrType => true;

        public override Type ClrType
        {
            get
            {
                _cachedType ??= _tag is not null ? HostExpr.TagToType(_tag) : typeof(AFunction);
                return _cachedType;
            }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ParserContext pcon, ISeq form, string name)
        {
            ISeq origForm = form;

            FnExpr fn = new(Compiler.TagOf(form))
            {
                Src = form
            };

            Keyword retKey = Keyword.intern(null, "rettag");  // TODO: make static
            object retTag = RT.get(RT.meta(form), retKey);
            ObjMethod enclosingMethod = (ObjMethod)Compiler.MethodVar.deref();
            fn._hasEnclosingMethod = enclosingMethod is not null;


            if (((IMeta)form.first()).meta() is not null)
            {
                fn.OnceOnly = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ONCE));
#if NET11_0_OR_GREATER
                fn.IsAsync = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ASYNC));
#endif
            }

#if NET11_0_OR_GREATER
            // Also check metadata on the form itself (propagated by defn -> fn macro)
            if (!fn.IsAsync && RT.meta(form) is IPersistentMap formMeta)
            {
                fn.IsAsync = RT.booleanCast(RT.get(formMeta, KW_ASYNC));
            }
#endif

            fn.ComputeNames(form, name);

            List<string> prims = [];

            //arglist might be preceded by symbol naming this fn
            Symbol nm = RT.second(form) as Symbol;
            if (nm is not null)
            {
                fn.ThisName = nm.Name;
                form = RT.cons(Compiler.FnSym, RT.next(RT.next(form)));
            }

            // Normalize body
            //now (fn [args] body...) or (fn ([args] body...) ([args2] body2...) ...)
            //turn former into latter
            if (RT.second(form) is IPersistentVector)
                form = RT.list(Compiler.FnSym, RT.next(form));

            fn.SpanMap = (IPersistentMap)Compiler.SourceSpanVar.deref();
            GenContext context = Compiler.CompilerContextVar.deref() as GenContext ?? Compiler.EvalContext;
            GenContext newContext = context.WithNewDynInitHelper(fn.InternalName + "__dynInitHelper_" + RT.nextID().ToString());
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
                        //Compiler.VarCallsitesVar, Compiler.EmptyVarCallSites(),
                        Compiler.NoRecurVar, null));
                    SortedDictionary<int, FnMethod> methods = [];
                    FnMethod variadicMethod = null;
                    bool usesThis = false;

                    for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                    {
                        FnMethod f = FnMethod.Parse(fn, (ISeq)RT.first(s), retTag);
                        if (f.UsesThis)
                        {
                            //Console.WriteLine("{0} uses this",fn.Name);
                            usesThis = true;
                        }
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

                    fn.CanBeDirect = !fn._hasEnclosingMethod && fn.Closes.count() == 0 && !usesThis;

                    IPersistentCollection allMethods = null;
                    foreach (FnMethod method in methods.Values)
                        allMethods = RT.conj(allMethods, method);
                    if (variadicMethod != null)
                        allMethods = RT.conj(allMethods, variadicMethod);

                    if (fn.CanBeDirect)
                    {
                        for (ISeq s = RT.seq(allMethods); s != null; s = s.next())
                        {
                            FnMethod fm = s.first() as FnMethod;
                            if (fm.Locals is not null)
                            {
                                for (ISeq sl = RT.seq(RT.keys(fm.Locals)); sl != null; sl = sl.next())
                                {
                                    LocalBinding lb = sl.first() as LocalBinding;
                                    if (lb.IsArg)
                                        lb.Index -= 1;
                                }
                            }
                        }
                    }

                    fn.Methods = allMethods;
                    fn._variadicMethod = variadicMethod;
                    fn.Keywords = (IPersistentMap)Compiler.KeywordsVar.deref();
                    fn.Vars = (IPersistentMap)Compiler.VarsVar.deref();
                    fn.Constants = (PersistentVector)Compiler.ConstantsVar.deref();
                    fn.KeywordCallsites = (IPersistentVector)Compiler.KeywordCallsitesVar.deref();
                    fn.ProtocolCallsites = (IPersistentVector)Compiler.ProtocolCallsitesVar.deref();
                    //fn.VarCallsites = (IPersistentSet)Compiler.VarCallsitesVar.deref();

                    fn.ConstantsID = RT.nextID();
                }
                finally
                {
                    Var.popThreadBindings();
                }


                IPersistentMap fmeta = RT.meta(origForm);
                if (fmeta is not null)
                    fmeta = fmeta.without(RT.LineKey).without(RT.ColumnKey).without(RT.SourceSpanKey).without(RT.FileKey).without(retKey);
                fn._hasMeta = RT.count(fmeta) > 0;


                IPersistentVector primTypes = PersistentVector.EMPTY;
                foreach (string typename in prims)
                    primTypes = primTypes.cons(Type.GetType(typename));

                fn.Compile(
                    fn.IsVariadic ? typeof(RestFn) : typeof(AFunction),
                    null,
                    primTypes,
                    fn.OnceOnly,
                    newContext);

                if (fn.SupportsMeta)
                    return new MetaExpr(fn, MapExpr.Parse(pcon.EvalOrExpr(), fmeta));
                else
                    return fn;

            }
            finally
            {
                if (newContext is not null)
                    Var.popThreadBindings();
            }
        }

        internal void AddMethod(FnMethod method)
        {
            Methods = RT.conj(Methods, method);
        }

        static readonly Dictionary<int, Dictionary<int, DynamicMethod>> DynMethodMap = [];
        static readonly Dictionary<int, object[]> ConstantsMap = [];

        public static DynamicMethod GetDynMethod(int key, int arity)
        {
            DynamicMethod dm = DynMethodMap[key][arity];
            if (dm is null)
                Console.WriteLine("Bad dynmeth retrieval");
            return dm;
            //    Dictionary<int, WeakReference > dict = DynMethodMap[key];
            //    WeakReference wr = dict[arity];
            //    return (DynamicMethod)wr.Target;
        }

        public static object[] GetCompiledConstants(int key)
        {
            return ConstantsMap[key];
        }

        #endregion

        #region eval

        public override object Eval() => base.Eval();

        #endregion

        #region Code generation

        internal void EmitForDefn(ObjExpr objx, CljILGen ilg) => Emit(RHC.Expression, objx, ilg);

        protected override void EmitMethods(TypeBuilder tb)
        {
            for (ISeq s = RT.seq(Methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.Emit(this, tb);
            }

            if (IsVariadic)
                EmitGetRequiredArityMethod(TypeBuilder, _variadicMethod.RequiredArity);

            List<int> supportedArities = [];
            for (ISeq s = RT.seq(Methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                supportedArities.Add(method.NumParams);
            }

            EmitHasArityMethod(TypeBuilder, supportedArities, IsVariadic, IsVariadic ? _variadicMethod.RequiredArity : 0);
        }

        static void EmitGetRequiredArityMethod(TypeBuilder tb, int requiredArity)
        {
            MethodBuilder mb = tb.DefineMethod(
                "getRequiredArity",
                MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(int),
                Type.EmptyTypes);

            CljILGen gen = new(mb.GetILGenerator());
            gen.EmitInt(requiredArity);
            gen.Emit(OpCodes.Ret);
        }

        #endregion
    }
}
