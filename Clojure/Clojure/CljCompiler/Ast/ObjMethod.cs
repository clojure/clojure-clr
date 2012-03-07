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
using System.Reflection.Emit;
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    abstract class ObjMethod
    {
        #region Data

        // Java: when closures are defined inside other closures,
        // the closed over locals need to be propagated to the enclosing objx
        readonly ObjMethod _parent;
        IPersistentMap _locals = null;       // localbinding => localbinding
        IPersistentMap _indexLocals = null;  // num -> localbinding
        protected Expr _body = null;
        ObjExpr _objx;
        protected IPersistentVector _argLocals;
        int _maxLocal = 0;
        IPersistentSet _localsUsedInCatchFinally = PersistentHashSet.EMPTY;
        protected IPersistentMap _methodMeta;

        protected LocalBinding _thisBinding;
        protected Type _explicitInterface = null;
        protected MethodInfo _explicitMethodInfo = null;

        protected IPersistentVector _parms;

        protected MethodBuilder _staticMethodBuilder;

        protected IPersistentMap SpanMap { get; set; }

        #endregion

        #region Data accessors

        internal ObjMethod Parent
        {
            get { return _parent; }
        }

        public IPersistentMap Locals
        {
            get { return _locals; }
            set { _locals = value; }
        }

        public IPersistentMap IndexLocals
        {
            get { return _indexLocals; }
            set { _indexLocals = value; }
        }

        internal ObjExpr Objx
        {
            get { return _objx; }
            //set { _objx = value; }
        }

        public int MaxLocal
        {
            get { return _maxLocal; }
            set { _maxLocal = value; }
        }

        public IPersistentSet LocalsUsedInCatchFinally
        {
            get { return _localsUsedInCatchFinally; }
            set { _localsUsedInCatchFinally = value; }
        }

        protected bool IsExplicit { get { return _explicitInterface != null; } }

        public virtual string Prim { get { return null; } }

        #endregion

        #region abstract methods

        internal abstract bool IsVariadic { get; }
        internal abstract int NumParams { get; }
        internal abstract int RequiredArity { get; }
        internal abstract string MethodName { get; }
        protected abstract string StaticMethodName { get; }
        protected abstract Type ReturnType { get; }
        protected abstract Type StaticReturnType { get; }
        protected abstract Type[] ArgTypes { get; }
        protected abstract Type[] StaticMethodArgTypes { get; }

        #endregion

        #region Ctors

        public ObjMethod(ObjExpr fn, ObjMethod parent)
        {
            _parent = parent;
            _objx = fn;
        }

        #endregion

        #region Code generation

        internal void GenerateCode(ObjExpr objx, GenContext context)
        {
            GenerateStaticMethod(objx, context);
            GenerateMethod(objx, context);
        }


        MethodBuilder GenerateStaticMethod(ObjExpr objx, GenContext context)
        {
            string methodName = StaticMethodName;
            TypeBuilder tb = objx.TypeBlder;

            List<ParameterExpression> parms = new List<ParameterExpression>(_argLocals.count() + 1);
            List<Type> parmTypes = new List<Type>(_argLocals.count() + 1);

            ParameterExpression thisParm = Expression.Parameter(objx.BaseType, "this");
            if (_thisBinding != null)
            {
                _thisBinding.ParamExpression = thisParm;
                _thisBinding.Tag = Symbol.intern(null, objx.BaseType.FullName);
            }
            objx.ThisParam = thisParm;
            parms.Add(thisParm);
            parmTypes.Add(objx.BaseType);

            try
            {
                LabelTarget loopLabel = Expression.Label("top");

                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar, loopLabel, Compiler.MethodVar, this));

                Type[] argTypes = StaticMethodArgTypes;

                for (int i = 0; i < _argLocals.count(); i++)
                {
                    LocalBinding lb = (LocalBinding)_argLocals.nth(i);
                    ParameterExpression parm = Expression.Parameter(argTypes[i], lb.Name);
                    lb.ParamExpression = parm;
                    parms.Add(parm);
                    parmTypes.Add(argTypes[i]);
                }

                Expression body =
                    Expression.Block(
                        //maybeLoadVarsExpr,
                        Expression.Label(loopLabel),
                        GenBodyCode(StaticReturnType,objx,context));

                //Expression convBody = Compiler.MaybeConvert(body, ReturnType);
                Expression convBody = HostExpr.GenUnboxArg(body, StaticReturnType);

                LambdaExpression lambda = Expression.Lambda(convBody, parms);
                // JVM: Clears locals here.


                // TODO: Cache all the CreateObjectTypeArray values
                MethodBuilder mb = tb.DefineMethod(methodName, MethodAttributes.Static|MethodAttributes.Public, StaticReturnType, parmTypes.ToArray());

                //Console.Write("StMd: {0} {1}(", ReturnType.Name, methodName);
                //foreach (Type t in parmTypes)
                //    Console.Write("{0}, ", t.Name);
                //Console.WriteLine(")");

                lambda.CompileToMethod(mb, context.IsDebuggable);

                _staticMethodBuilder = mb;
                return mb;
            }
            finally
            {
                Var.popThreadBindings();
            }

        }

        private Expression GenBodyCode(Type retType, ObjExpr objx, GenContext context)
        {
            MaybePrimitiveExpr be = (MaybePrimitiveExpr)_body;
            if (Util.IsPrimitive(retType) && be.CanEmitPrimitive)
            {
                Type bt = Compiler.MaybePrimitiveType(be);
                if (bt == retType)
                    return be.GenCodeUnboxed(RHC.Return, objx, context);
                else if (retType == typeof(long) && bt == typeof(int))
                    return Expression.Convert(be.GenCodeUnboxed(RHC.Return, objx, context), typeof(long));
                else if (retType == typeof(double) && bt == typeof(float))
                    return Expression.Convert(be.GenCodeUnboxed(RHC.Return, objx, context), typeof(double));
                else if (retType == typeof(int) && bt == typeof(long))
                    return Expression.Call(null, Compiler.Method_RT_intCast_long, be.GenCodeUnboxed(RHC.Return, objx, context));
                else if (retType == typeof(float) && bt == typeof(double))
                    return Expression.Convert(be.GenCodeUnboxed(RHC.Return, objx, context), typeof(float));
                else
                    throw new ArgumentException(String.Format("Mismatched primitive return, expected: {0}, had: {1}", retType, be.ClrType));

            }
            else
            {
                return _body.GenCode(RHC.Return, objx, context);
                // Java code does: gen.unbox(Type.getType(retClass) here.
                // I don't know how to do the equivalent.
            }
        }


        void GenerateMethod(ObjExpr objx, GenContext context)
        {
            if (Prim != null)
                GeneratePrimMethod(objx, context);

            TypeBuilder tb = objx.TypeBlder;

            MethodBuilder mb = tb.DefineMethod(MethodName, MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, ReturnType, ArgTypes);


            //Console.Write("InMd: {0} {1}(", ReturnType.Name, MethodName);
            //foreach (Type t in ArgTypes)
            //    Console.Write("{0}", t.Name);
            //Console.WriteLine(")");

            GenInterface.SetCustomAttributes(mb, _methodMeta);
            if (_parms != null)
            {
                for (int i = 0; i < _parms.count(); i++)
                {
                    IPersistentMap meta = GenInterface.ExtractAttributes(RT.meta(_parms.nth(i)));
                    if (meta != null && meta.count() > 0)
                    {
                        ParameterBuilder pb = mb.DefineParameter(i + 1, ParameterAttributes.None, ((Symbol)_parms.nth(i)).Name);
                        GenInterface.SetCustomAttributes(pb, meta);
                    }
                }
            }

            ILGenerator gener = mb.GetILGenerator();
            ILGen gen = new ILGen(gener);

            gen.EmitLoadArg(0);
            for (int i = 1; i <= _argLocals.count(); i++)
            {
                gen.EmitLoadArg(i);
                EmitUnboxArg(gener, ArgTypes[i-1], StaticMethodArgTypes[i - 1]);
            }        
            gen.EmitCall(_staticMethodBuilder);
            if (ReturnType != StaticReturnType)
                gen.Emit(OpCodes.Castclass, ReturnType);
            gen.Emit(OpCodes.Ret);

            if ( IsExplicit )
                tb.DefineMethodOverride(mb, _explicitMethodInfo);            

        }

        static void EmitUnboxArg(ILGenerator gen, Type argType, Type paramType)
        {
             if (argType == paramType)
                return;
             HostExpr.EmitUnboxArg(gen, paramType); 
        }


        void GeneratePrimMethod(ObjExpr objx, GenContext context)
        {
            TypeBuilder tb = objx.TypeBlder;
            MethodBuilder mb = tb.DefineMethod("invokePrim", MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, StaticReturnType, StaticMethodArgTypes);


            //Console.Write("InMd: {0} {1}(", ReturnType.Name, "invokePrim");
            //foreach (Type t in ArgTypes)
            //    Console.Write("{0}", t.Name);
            //Console.WriteLine(")");

            GenInterface.SetCustomAttributes(mb, _methodMeta);
            if (_parms != null)
            {
                for (int i = 0; i < _parms.count(); i++)
                {
                    IPersistentMap meta = GenInterface.ExtractAttributes(RT.meta(_parms.nth(i)));
                    if (meta != null && meta.count() > 0)
                    {
                        ParameterBuilder pb = mb.DefineParameter(i + 1, ParameterAttributes.None, ((Symbol)_parms.nth(i)).Name);
                        GenInterface.SetCustomAttributes(pb, meta);
                    }
                }
            }

            ILGen gen = new ILGen(mb.GetILGenerator());
            gen.EmitLoadArg(0);
            for (int i = 1; i <= _argLocals.count(); i++)
                gen.EmitLoadArg(i);
            gen.EmitCall(_staticMethodBuilder);
            gen.Emit(OpCodes.Ret);

            if (IsExplicit)
                tb.DefineMethodOverride(mb, _explicitMethodInfo);

        }

        #endregion

        #region No-DLR code generation

        
        protected abstract String GetMethodName();
        protected abstract Type GetReturnType();
        protected abstract Type[] GetArgTypes();


        public virtual void Emit(ObjExpr fn, GenContext context)
        {
            MethodBuilder mb = context.TB.DefineMethod(GetMethodName(), MethodAttributes.Public, GetReturnType(), GetArgTypes());

            GenContext newContext = context.WithBuilders(context.TB, mb);
            ILGenerator ilg = newContext.GetILGenerator();
            Label loopLabel = ilg.DefineLabel();

            Compiler.MaybeEmitDebugInfo(context, ilg, SpanMap);

            try 
            {
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar,loopLabel,Compiler.MethodVar,this));
                ilg.MarkLabel(loopLabel);
                _body.Emit(RHC.Return,fn,newContext);
                ilg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


        protected static void EmitBody(ObjExpr objx, GenContext context, Type retType, Expr body)
        {
            ILGenerator ilg = context.GetILGenerator();

            MaybePrimitiveExpr be = (MaybePrimitiveExpr)body;
            if (Util.IsPrimitive(retType) && be.CanEmitPrimitive)
            {
                Type bt = Compiler.MaybePrimitiveType(be);
                if (bt == retType)
                    be.EmitUnboxed(RHC.Return, objx, context);
                else if (retType == typeof(long) && bt == typeof(int))
                {
                    be.EmitUnboxed(RHC.Return, objx, context);
                    ilg.Emit(OpCodes.Conv_I8);
                }
                else if (retType == typeof(double) && bt == typeof(float))
                {
                    be.EmitUnboxed(RHC.Return, objx, context);
                    ilg.Emit(OpCodes.Conv_R8);
                }
                else if (retType == typeof(int) && bt == typeof(long))
                {
                    be.EmitUnboxed(RHC.Return, objx, context);
                    ilg.Emit(OpCodes.Call,Compiler.Method_RT_intCast_long);
                }
                else if (retType == typeof(float) && bt == typeof(double))
                {
                    be.EmitUnboxed(RHC.Return, objx, context);
                    ilg.Emit(OpCodes.Conv_R4);
                }
                else
                {
                    throw new ArgumentException(String.Format("Mismatched primitive return, expected: {0}, had: {1}", retType, be.ClrType));
                }
            }
            else
            {
                body.Emit(RHC.Return, objx, context);
                if (retType == typeof(void))
                    ilg.Emit(OpCodes.Pop);
                else
                    EmitUnboxArg(ilg, typeof(object), retType);
            }
        }


        protected void SetCustomAttributes(MethodBuilder mb)
        {
            GenInterface.SetCustomAttributes(mb, _methodMeta);
            if (_parms != null)
            {
                for (int i = 0; i < _parms.count(); i++)
                {
                    IPersistentMap meta = GenInterface.ExtractAttributes(RT.meta(_parms.nth(i)));
                    if (meta != null && meta.count() > 0)
                    {
                        ParameterBuilder pb = mb.DefineParameter(i + 1, ParameterAttributes.None, ((Symbol)_parms.nth(i)).Name);
                        GenInterface.SetCustomAttributes(pb, meta);
                    }
                }
            }
        }

        #endregion
    }
}
