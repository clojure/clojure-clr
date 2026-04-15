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
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace clojure.lang.CljCompiler.Ast
{
    public class FnMethod : ObjMethod
    {
        #region Data

        protected IPersistentVector _reqParms = PersistentVector.EMPTY;  // localbinding => localbinding
        public IPersistentVector ReqParms => _reqParms;

        protected LocalBinding _restParm = null;
        public LocalBinding RestParm => _restParm;

        Type[] _argTypes;
        // Accessor for _argTypes: see below.

        Type _retType;
        // accessor for _retType: see below.

        string _prim;
        public override string Prim => _prim;


        #endregion

        #region C-tors

        public FnMethod(FnExpr fn, ObjMethod parent)
            : base(fn, parent)
        {
        }

        // For top-level compilation only
        // TODO: Can we get rid of this when the DLR-based compile goes away?
        public FnMethod(FnExpr fn, ObjMethod parent, BodyExpr body)
            : base(fn, parent)
        {
            Body = body;
            ArgLocals = PersistentVector.EMPTY;
            //_thisBinding = Compiler.RegisterLocal(Symbol.intern(fn.ThisName ?? "fn__" + RT.nextID()), null, null, false);
        }

        #endregion

        #region ObjMethod methods

        public override bool IsVariadic => _restParm is not null;

        public override int NumParams => _reqParms.count() + (IsVariadic ? 1 : 0);

        public override int RequiredArity => _reqParms.count();

        public override string MethodName => IsVariadic ? "doInvoke" : "invoke";

        public override Type[] ArgTypes
        {
            get
            {
                if (IsVariadic && _reqParms.count() == Compiler.MaxPositionalArity)
                {
                    Type[] ret = new Type[Compiler.MaxPositionalArity + 1];
                    for (int i = 0; i < Compiler.MaxPositionalArity + 1; i++)
                        ret[i] = typeof(Object);
                    return ret;
                }

                return Compiler.CreateObjectTypeArray(NumParams);
            }
        }

        public override Type ReturnType
        {
            get
            {
                if (_prim is not null) // Objx.IsStatic)
                    return _retType;

                return typeof(object);
            }
        }

        #endregion

        #region Parsing

        enum ParamParseState { Required, Rest, Done };

        internal static FnMethod Parse(FnExpr fn, ISeq form, object retTag)
        {
            // ([args] body ... )

            IPersistentVector parms = (IPersistentVector)RT.first(form);
            ISeq body = RT.next(form);

            try
            {
                FnMethod method = new(fn, (ObjMethod)Compiler.MethodVar.deref())
                {
                    SpanMap = (IPersistentMap)Compiler.SourceSpanVar.deref()
                };

                Var.pushThreadBindings(RT.mapUniqueKeys(
                    Compiler.MethodVar, method,
                    Compiler.LocalEnvVar, Compiler.LocalEnvVar.deref(),
                    Compiler.LoopLocalsVar, null,
                    Compiler.NextLocalNumVar, 0,
                    Compiler.MethodReturnContextVar, true));

                // In the JVM code, the call to PrimInterface is here.
                // But we need to know if rettag is used and the computed return type, so it should come later.

                if (retTag is String)
                    retTag = Symbol.intern(null, (string)retTag);
                if (retTag is not Symbol)
                    retTag = null;
                if (retTag is not null)
                {
                    string retStr = ((Symbol)retTag).Name;
                    if (!(retStr.Equals("long") || retStr.Equals("double")))
                        retTag = null;
                }
                method._retType = Compiler.TagType(Compiler.TagOf(parms) ?? retTag);

                if (method._retType.IsPrimitive)
                {
                    if (!(method._retType == typeof(double) || method._retType == typeof(long)))
                        throw new ParseException("Only long and double primitives are supported");
                }
                else
                    method._retType = typeof(object);

                method._prim = PrimInterface(parms, method._retType);
                //if (method._prim != null)
                //    method._prim = method._prim.Replace('.', '/');

                if (method.IsAsync && method._prim != null)
                    throw new ParseException("^:async cannot be combined with primitive type hints");

                // register 'this' as local 0  
                Compiler.RegisterLocalThis(Symbol.intern(fn.ThisName ?? "fn__" + RT.nextID()), null, null);

                ParamParseState paramState = ParamParseState.Required;
                IPersistentVector argLocals = PersistentVector.EMPTY;
                List<Type> argTypes = [];

                int parmsCount = parms.count();

                for (int i = 0; i < parmsCount; i++)
                {
                    if (parms.nth(i) is not Symbol)
                        throw new ParseException("fn params must be Symbols");
                    Symbol p = (Symbol)parms.nth(i);
                    if (p.Namespace is not null)
                        throw new ParseException("Can't use qualified name as parameter: " + p);
                    if (p.Equals(Compiler.AmpersandSym))
                    {
                        if (paramState == ParamParseState.Required)
                            paramState = ParamParseState.Rest;
                        else
                            throw new ParseException("Invalid parameter list");
                    }
                    else
                    {
                        Type pt = Compiler.PrimType(Compiler.TagType(Compiler.TagOf(p)));

                        if (pt.IsPrimitive && !(pt == typeof(double) || pt == typeof(long)))
                            throw new ParseException("Only long and double primitives are supported: " + p);

                        if (paramState == ParamParseState.Rest && Compiler.TagOf(p) != null)
                            throw new ParseException("& arg cannot have type hint");
                        if (paramState == ParamParseState.Rest && method.Prim != null)
                            throw new ParseException("fns taking primitives cannot be variadic");
                        if (paramState == ParamParseState.Rest)
                            pt = typeof(ISeq);
                        argTypes.Add(pt);
                        LocalBinding b = pt.IsPrimitive
                            ? Compiler.RegisterLocal(p, null, new MethodParamExpr(pt), pt, true)
                            : Compiler.RegisterLocal(p,
                            paramState == ParamParseState.Rest ? Compiler.ISeqSym : Compiler.TagOf(p),
                            null, pt, true);

                        argLocals = argLocals.cons(b);
                        switch (paramState)
                        {
                            case ParamParseState.Required:
                                method._reqParms = method._reqParms.cons(b);
                                break;
                            case ParamParseState.Rest:
                                method._restParm = b;
                                paramState = ParamParseState.Done;
                                break;
                            default:
                                throw new ParseException("Unexpected parameter");
                        }
                    }
                }

                if (method.RequiredArity > Compiler.MaxPositionalArity)
                    throw new ParseException(string.Format("Can't specify more than {0} parameters", Compiler.MaxPositionalArity));
                Compiler.LoopLocalsVar.set(argLocals);
                method.ArgLocals = argLocals;
                method._argTypes = [.. argTypes];
                method.Body = (new BodyExpr.Parser()).Parse(new ParserContext(RHC.Return), body);
                return method;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        #endregion

        #region primitive interfaces support

        public static char TypeChar(object x)
        {
            Type t = x as Type ?? Compiler.PrimType(x as Symbol);

            if (t is null || !t.IsPrimitive)
                return 'O';
            if (t == typeof(long))
                return 'L';
            if (t == typeof(double))
                return 'D';
            throw new ArgumentException("Only long and double primitives are supported");
        }

        public static bool IsPrimType(object x)
        {
            Type t = x as Type ?? Compiler.PrimType(x as Symbol);

            if (t == typeof(long) || t == typeof(double))
                return true;

            return false;
        }

        // Use this if we supply the return type rather than getting it from the meta of the arglist vector
        public static string PrimInterface(IPersistentVector arglist, Type retType)
        {
            StringBuilder sb = new();
            for (int i = 0; i < arglist.count(); i++)
                sb.Append(TypeChar(Compiler.TagOf(arglist.nth(i))));
            sb.Append(TypeChar(retType));
            string ret = sb.ToString();
            bool prim = ret.Contains("L") || ret.Contains("D");
            if (prim && arglist.count() > 4)
                throw new ArgumentException("fns taking primitives support only 4 or fewer args");
            if (prim)
                return "clojure.lang.primifs." + ret;
            return null;
        }


        // Use this if we get the return type from the meta of the arglist vector

        public static string PrimInterface(IPersistentVector arglist)
        {
            StringBuilder sb = new();
            for (int i = 0; i < arglist.count(); i++)
                sb.Append(TypeChar(Compiler.TagOf(arglist.nth(i))));
            sb.Append(TypeChar(Compiler.TagOf(arglist)));
            string ret = sb.ToString();
            bool prim = ret.Contains("L") || ret.Contains("D");
            if (prim && arglist.count() > 4)
                throw new ArgumentException("fns taking primitives support only 4 or fewer args");
            if (prim)
                return "clojure.lang.primifs." + ret;
            return null;
        }

        public static bool IsPrimInterface(IPersistentVector arglist)
        {
            if (arglist.count() > 4)
                return false;

            for (int i = 0; i < arglist.count(); i++)
                if (IsPrimType(Compiler.TagOf(arglist.nth(i))))
                    return true;

            if (IsPrimType(Compiler.TagOf(arglist)))
                return true;

            return false;
        }

        public static bool HasPrimInterface(ISeq form)
        {
            return RT.first(form) is IPersistentVector parms && IsPrimInterface(parms);
        }

        #endregion

        #region Code generation


        public override void Emit(ObjExpr fn, TypeBuilder tb)
        {
            if (fn.CanBeDirect)
            {
                //Console.WriteLine("emit static: {0}", fn.Name);
                DoEmitStatic(fn, tb);
            }
            else if (Prim != null)
            {
                //Console.WriteLine("emit prim: {0}", fn.Name);
                DoEmitPrim(fn, tb);
            }
            else
            {
                //Console.WriteLine("emit normal: {0}", fn.Name);
                DoEmit(fn, tb);
            }
        }

        private void DoEmitStatic(ObjExpr fn, TypeBuilder tb)
        {
            MethodAttributes attribs = MethodAttributes.Static | MethodAttributes.Public;

            string methodName = "invokeStatic";

            Type returnType = IsAsync 
                ? typeof(System.Threading.Tasks.Task<object>)
                : ReturnType;

            //Type returnType = ReturnType;

            MethodBuilder baseMB = tb.DefineMethod(methodName, attribs, returnType, _argTypes);

#if NET11_0_OR_GREATER
            if (IsAsync)
            {
                baseMB.SetImplementationFlags(
                    baseMB.GetMethodImplementationFlags() | (MethodImplAttributes)MethodImplOptions.Async);
            }
#else
            // Should we emit a warning?
#endif

            CljILGen baseIlg = new(baseMB.GetILGenerator());

            try
            {
                Label loopLabel = baseIlg.DefineLabel();
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar, loopLabel, Compiler.MethodVar, this));

                GenContext.EmitDebugInfo(baseIlg, SpanMap);

                baseIlg.MarkLabel(loopLabel);
                EmitBody(Objx, baseIlg, _retType, Body);
                if (Body.HasNormalExit())
                    baseIlg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }

            // Generate the regular invoke, calling the static method
            {
                MethodBuilder regularMB = tb.DefineMethod(MethodName, MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, typeof(Object), ArgTypes);
                SetCustomAttributes(regularMB);

                CljILGen regIlg = new(regularMB.GetILGenerator());

                for (int i = 0; i < _argTypes.Length; i++)
                {
                    regIlg.EmitLoadArg(i + 1);
                    HostExpr.EmitUnboxArg(fn, regIlg, _argTypes[i]);
                }

                GenContext.EmitDebugInfo(baseIlg, SpanMap);

                regIlg.Emit(OpCodes.Call, baseMB);
                if (ReturnType.IsValueType)
                    regIlg.Emit(OpCodes.Box, ReturnType);
                regIlg.Emit(OpCodes.Ret);
            }

            // Generate primInvoke if prim
            if (Prim != null)
            {
                MethodAttributes primAttribs = MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual;

                string primMethodName = "invokePrim";

                Type primReturnType;
                if (_retType == typeof(double) || _retType == typeof(long))
                    primReturnType = ReturnType;
                else
                    primReturnType = typeof(object);

                MethodBuilder primMB = tb.DefineMethod(primMethodName, primAttribs, primReturnType, _argTypes);
                SetCustomAttributes(primMB);

                CljILGen primIlg = new(primMB.GetILGenerator());
                for (int i = 0; i < _argTypes.Length; i++)
                {
                    primIlg.EmitLoadArg(i + 1);
                    //HostExpr.EmitUnboxArg(fn, primIlg, _argTypes[i]);
                }
                primIlg.Emit(OpCodes.Call, baseMB);
                if (Body.HasNormalExit())
                    primIlg.Emit(OpCodes.Ret);
            }
        }

        private void DoEmitPrim(ObjExpr fn, TypeBuilder tb)
        {
            MethodAttributes attribs = MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual;

            string methodName = "invokePrim";

            Type returnType;
            if (_retType == typeof(double) || _retType == typeof(long))
                returnType = ReturnType;
            else
                returnType = typeof(object);

            MethodBuilder baseMB = tb.DefineMethod(methodName, attribs, returnType, _argTypes);

            SetCustomAttributes(baseMB);

            CljILGen baseIlg = new(baseMB.GetILGenerator());

            try
            {
                Label loopLabel = baseIlg.DefineLabel();
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar, loopLabel, Compiler.MethodVar, this));

                GenContext.EmitDebugInfo(baseIlg, SpanMap);

                baseIlg.MarkLabel(loopLabel);
                EmitBody(Objx, baseIlg, _retType, Body);
                if (Body.HasNormalExit())
                    baseIlg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }

            // Generate the regular invoke, calling the prim method

            MethodBuilder regularMB = tb.DefineMethod(MethodName, MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, typeof(Object), ArgTypes);
            SetCustomAttributes(regularMB);

            CljILGen regIlg = new(regularMB.GetILGenerator());

            regIlg.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < _argTypes.Length; i++)
            {
                regIlg.EmitLoadArg(i + 1);
                HostExpr.EmitUnboxArg(fn, regIlg, _argTypes[i]);
            }
            regIlg.Emit(OpCodes.Call, baseMB);
            if (ReturnType.IsValueType)
                regIlg.Emit(OpCodes.Box, ReturnType);
            regIlg.Emit(OpCodes.Ret);
        }

        private void DoEmit(ObjExpr fn, TypeBuilder tb)
        {
#if NET11_0_OR_GREATER
            if (IsAsync)
            {
                DoEmitAsync(fn, tb);
                return;
            }
#endif
            MethodAttributes attribs = MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual;
            MethodBuilder mb = tb.DefineMethod(MethodName, attribs, ReturnType, ArgTypes);
            SetCustomAttributes(mb);

            CljILGen baseIlg = new(mb.GetILGenerator());

            try
            {
                Label loopLabel = baseIlg.DefineLabel();
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar, loopLabel, Compiler.MethodVar, this));

                GenContext.EmitDebugInfo(baseIlg, SpanMap);

                baseIlg.MarkLabel(loopLabel);
                Body.Emit(RHC.Return, fn, baseIlg);
                if (Body.HasNormalExit())
                    baseIlg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }

            if (IsExplicit)
                tb.DefineMethodOverride(mb, ExplicitMethodInfo);
        }


#if NET11_0_OR_GREATER
        private void DoEmitAsync(ObjExpr fn, TypeBuilder tb)
        {
            // Generate the async implementation method: invokeAsync() -> Task<object>
            // This method gets the 0x2000 async flag and contains the real body with await* calls.

            Type asyncReturnType = typeof(System.Threading.Tasks.Task<object>);
            MethodBuilder asyncMB = tb.DefineMethod(
                MethodName + "Async",
                MethodAttributes.Private,
                asyncReturnType,
                ArgTypes);

            asyncMB.SetImplementationFlags(
                asyncMB.GetMethodImplementationFlags() | (MethodImplAttributes)MethodImplOptions.Async);

            CljILGen asyncIlg = new(asyncMB.GetILGenerator());

            try
            {
                Label loopLabel = asyncIlg.DefineLabel();
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar, loopLabel, Compiler.MethodVar, this));

                GenContext.EmitDebugInfo(asyncIlg, SpanMap);

                asyncIlg.MarkLabel(loopLabel);
                Body.Emit(RHC.Return, fn, asyncIlg);
                if (Body.HasNormalExit())
                    asyncIlg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }

            // Generate the IFn.invoke() override: invoke() -> object
            // This wrapper calls invokeAsync() and returns the Task<object> as object.
            MethodAttributes attribs = MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual;
            MethodBuilder invokeMB = tb.DefineMethod(MethodName, attribs, typeof(object), ArgTypes);
            SetCustomAttributes(invokeMB);

            CljILGen invokeIlg = new(invokeMB.GetILGenerator());

            // Load 'this' and all arguments, then call invokeAsync
            invokeIlg.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < ArgTypes.Length; i++)
                invokeIlg.EmitLoadArg(i + 1);
            invokeIlg.Emit(OpCodes.Call, asyncMB);
            // Task<object> is already an object reference, no boxing needed
            invokeIlg.Emit(OpCodes.Ret);

            if (IsExplicit)
                tb.DefineMethodOverride(invokeMB, ExplicitMethodInfo);
        }
#endif

        #endregion

    }

}

