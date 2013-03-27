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

#if CLR2
extern alias MSC;
#endif

using System;
using System.Collections.Generic;

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Dynamic;
using System.Reflection;
using clojure.lang.Runtime.Binding;
using clojure.lang.Runtime;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    abstract class MethodExpr : HostExpr
    {
        #region Data

        protected readonly string _methodName;
        protected readonly List<HostArg> _args;
        protected readonly List<Type> _typeArgs;
        protected MethodInfo _method;
        protected readonly string _source;
        protected readonly IPersistentMap _spanMap;
        protected readonly Symbol _tag;

        #endregion

        #region C-tors

        protected MethodExpr(string source, IPersistentMap spanMap, Symbol tag, string methodName, List<Type> typeArgs, List<HostArg> args)
        {
            _source = source;
            _spanMap = spanMap;
            _methodName = methodName;
            _typeArgs = typeArgs;
            _args = args;
            _tag = tag;
        }

        #endregion

        #region Code generation

        protected abstract bool IsStaticCall { get; }

        public override bool CanEmitPrimitive
        {
            get { return _method != null && Util.IsPrimitive(_method.ReturnType); }
        }

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);

            Type retType;

            if (_method != null)
            {
                EmitForMethod(objx, ilg);
                retType = _method.ReturnType;
            }
            else
            {
                EmitComplexCall(objx, ilg);
                retType = typeof(object);
            }
            HostExpr.EmitBoxReturn(objx, ilg, retType);

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public override void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (_method != null)
            {
                EmitForMethod(objx, ilg);
            }
            else
            {
                throw new InvalidOperationException("Unboxed emit of unknown member.");
            }

            if (rhc == RHC.Statement)
               ilg.Emit(OpCodes.Pop);
        }

        private void EmitForMethod(ObjExpr objx, CljILGen ilg)
        {
            if ( _method.IsGenericMethodDefinition )
            {
                EmitComplexCall(objx, ilg);
                return;
            }

            if (!IsStaticCall)
            {
                EmitTargetExpression(objx, ilg);
                EmitPrepForCall(ilg,typeof(object),_method.DeclaringType);
            }

            EmitTypedArgs(objx, ilg, _method.GetParameters(), _args);
            if (IsStaticCall)
            {
                if (Intrinsics.HasOp(_method))
                    Intrinsics.EmitOp(_method,ilg);
                else
                    ilg.Emit(OpCodes.Call, _method);
            }
            else
                ilg.Emit(OpCodes.Callvirt, _method); 
        }

        public static readonly MethodInfo Method_MethodExpr_GetDelegate = typeof(MethodExpr).GetMethod("GetDelegate");
 
        public static readonly Dictionary<int, Delegate> DelegatesMap = new Dictionary<int, Delegate>();

        public static Delegate GetDelegate(int key)
        {
            Delegate d = DelegatesMap[key];
            return d;
        }

        public static void CacheDelegate(int key, Delegate d)
        {
            DelegatesMap[key] = d;
        }

        protected abstract void EmitTargetExpression(ObjExpr objx, CljILGen ilg);
        protected abstract Type GetTargetType();

        private void EmitComplexCall(ObjExpr objx, CljILGen ilg)
        {
            // TOD: We have gotten rid of light-compile. Simplify this.
            // This is made more complex than I'd like by light-compiling.
            // Without light-compile, we could just:
            //   Emit the target expression
            //   Emit the arguments (and build up the parameter list for the lambda)
            //   Create the lambda, compile to a methodbuilder, and call it.
            // Light-compile forces us to 
            //     create a lambda at the beginning because we must 
            //     compile it to a delegate, to get the type
            //     write code to grab the delegate from a cache
            //     Then emit the target expression
            //          emit the arguments (but note we need already to have built the parameter list)
            //          Call the delegate
            //  Combined, this becomes
            //      Build the parameter list
            //      Build the dynamic call and lambda  (slightly different for light-compile vs full)
            //      if light-compile
            //          build the delegate
            //          cache it
            //          emit code to retrieve and cast it
            //       emit the target expression
            //       emit the args
            //       emit the call (slightly different for light compile vs full)
            //

            //  Build the parameter list

            List<ParameterExpression> paramExprs = new List<ParameterExpression>(_args.Count + 1);
            List<Type> paramTypes = new List<Type>(_args.Count + 1);

            Type targetType = GetTargetType();
            if (!targetType.IsPrimitive)
                targetType = typeof(object);

            paramExprs.Add(Expression.Parameter(targetType));
            paramTypes.Add(targetType);
            int i = 0;
            foreach (HostArg ha in _args)
            {
                i++;
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType && e.ClrType != null && e.ClrType.IsPrimitive ? e.ClrType : typeof(object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        {
                            Type byRefType = argType.MakeByRefType();
                            paramExprs.Add(Expression.Parameter(byRefType, ha.LocalBinding.Name));
                            paramTypes.Add(byRefType);
                            break;
                        }

                    case HostArg.ParameterType.Standard:
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr)
                        {
                            paramExprs.Add(Expression.Parameter(argType, ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            paramTypes.Add(argType);
                        }
                        else
                        {
                            paramExprs.Add(Expression.Parameter(typeof(object), ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            paramTypes.Add(typeof(object));
                        }
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }

            // Build dynamic call and lambda
            Type returnType = HasClrType ? ClrType : typeof(object);
            InvokeMemberBinder binder = new ClojureInvokeMemberBinder(ClojureContext.Default, _methodName, paramExprs.Count, IsStaticCall);

            // This is what I want to do.
            //DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramExprs);
            // Unfortunately, the Expression.Dynamic method does not respect byRef parameters.
            // The workaround appears to be to roll your delegate type and then use Expression.MakeDynamic, as below.

            List<Type> callsiteParamTypes = new List<Type>(paramTypes.Count + 1);
            callsiteParamTypes.Add(typeof(System.Runtime.CompilerServices.CallSite));
            callsiteParamTypes.AddRange(paramTypes);
            Type dynType = Microsoft.Scripting.Generation.Snippets.Shared.DefineDelegate("__interop__", returnType, callsiteParamTypes.ToArray());
 
#if CLR2
            // Not covariant. Sigh.
            List<Expression> paramsAsExprs = new List<Expression>(paramExprs.Count);
            paramsAsExprs.AddRange(paramExprs.ToArray());
            DynamicExpression dyn = Expression.MakeDynamic(dynType, binder, paramsAsExprs);
#else
            DynamicExpression dyn = Expression.MakeDynamic(dynType, binder, paramExprs);
#endif
            LambdaExpression lambda;
            Type delType;
            MethodBuilder mbLambda;

            EmitDynamicCallPreamble(dyn, _spanMap, "__interop_" + _methodName + RT.nextID(), returnType, paramExprs, paramTypes.ToArray(), ilg, out lambda, out delType, out mbLambda);

            //  Emit target + args
            
            EmitTargetExpression(objx, ilg);

            i = 0;
            foreach (HostArg ha in _args)
            {
                i++;
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType && e.ClrType != null && e.ClrType.IsPrimitive ? e.ClrType : typeof(object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        EmitByRefArg(ha,objx,ilg);
                        break;

                    case HostArg.ParameterType.Standard:
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr)
                        {
                            ((MaybePrimitiveExpr)ha.ArgExpr).EmitUnboxed(RHC.Expression, objx, ilg);
                        }
                        else
                        {
                            ha.ArgExpr.Emit(RHC.Expression, objx, ilg);
                        }
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }

            EmitDynamicCallPostlude(lambda, delType, mbLambda, ilg);
        }

        public static void EmitByRefArg(HostArg ha, ObjExpr objx, CljILGen ilg)
        {
            if (ha.LocalBinding.IsArg)
                ilg.Emit(OpCodes.Ldarga, ha.LocalBinding.Index);
            else if (ha.LocalBinding.IsThis)
                ilg.Emit(OpCodes.Ldarga, 0);
            else
                ilg.Emit(OpCodes.Ldloca, ha.LocalBinding.LocalVar);
        }

        static public void EmitDynamicCallPreamble(DynamicExpression dyn, IPersistentMap spanMap, string methodName, Type returnType, List<ParameterExpression> paramExprs, Type[] paramTypes, CljILGen ilg, out LambdaExpression lambda, out Type delType, out MethodBuilder mbLambda)
        {
            Expression call = dyn;

            GenContext context = Compiler.CompilerContextVar.deref() as GenContext;
            if (context != null && context.DynInitHelper != null)
                call = context.DynInitHelper.ReduceDyn(dyn);

            if (returnType == typeof(void))
            {
                call = Expression.Block(call, Expression.Default(typeof(object)));
                returnType = typeof(object);
            }
            else if (returnType != call.Type)
            {
                call = Expression.Convert(call, returnType);
            }

            call = GenContext.AddDebugInfo(call, spanMap);


            delType = Microsoft.Scripting.Generation.Snippets.Shared.DefineDelegate("__interop__", returnType, paramTypes);
            lambda = Expression.Lambda(delType, call, paramExprs);
            mbLambda = null;
            
            if (context == null)
            {
                // light compile

                Delegate d = lambda.Compile();
                int key = RT.nextID();
                CacheDelegate(key, d);
 
                ilg.EmitInt(key);
                ilg.Emit(OpCodes.Call, Method_MethodExpr_GetDelegate);
                ilg.Emit(OpCodes.Castclass, delType);
            }
            else
            {
                mbLambda = context.TB.DefineMethod(methodName, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, returnType, paramTypes);
                lambda.CompileToMethod(mbLambda);
            }
        }


        static public void EmitDynamicCallPostlude(LambdaExpression lambda, Type delType, MethodBuilder mbLambda, CljILGen ilg)
        {
               GenContext context = Compiler.CompilerContextVar.deref() as GenContext;
 
            if ( context == null )
            {
                // light compile

                MethodInfo mi = delType.GetMethod("Invoke");
                ilg.Emit(OpCodes.Callvirt, mi);
            }
            else
            {
                ilg.Emit(OpCodes.Call, mbLambda);
            }
        }

        internal static void EmitArgsAsArray(IPersistentVector args, ObjExpr objx, CljILGen ilg)
        {
            ilg.EmitInt(args.count());
            ilg.Emit(OpCodes.Newarr, typeof(Object));

            for (int i = 0; i < args.count(); i++)
            {
                ilg.Emit(OpCodes.Dup);
                ilg.EmitInt(i);
                ((Expr)args.nth(i)).Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Stelem_Ref);
            }
        }

        public static void EmitTypedArgs(ObjExpr objx, CljILGen ilg, ParameterInfo[] parms, List<HostArg> args)
        {
            for (int i = 0; i < parms.Length; i++)
            {
                HostArg ha = args[i];
                ParameterInfo pi = parms[i];
                bool argIsByRef = ha.ParamType == HostArg.ParameterType.ByRef;
                bool paramIsByRef = pi.ParameterType.IsByRef;

                if (!paramIsByRef)
                    EmitTypedArg(objx, ilg, pi.ParameterType, ha.ArgExpr);
                else // paramIsByRef
                {
                    if (argIsByRef)
                    {
                        EmitByRefArg(ha, objx, ilg);
                    }
                    else
                    {
                        EmitTypedArg(objx, ilg, parms[i].ParameterType, args[i].ArgExpr);
                        LocalBuilder loc = ilg.DeclareLocal(pi.ParameterType);
                        loc.SetLocalSymInfo("_byRef_temp" + i);
                        ilg.Emit(OpCodes.Stloc, loc);
                        ilg.Emit(OpCodes.Ldloca, loc);
                    }
                }
            }
        }

        public static void EmitTypedArgs(ObjExpr objx, CljILGen ilg, ParameterInfo[] parms, IPersistentVector args)
        {
            for (int i = 0; i < parms.Length; i++)
                EmitTypedArg(objx, ilg, parms[i].ParameterType, (Expr)args.nth(i));
        }

        public static void EmitTypedArg(ObjExpr objx, CljILGen ilg, Type paramType, Expr arg)
        {
            Type primt = Compiler.MaybePrimitiveType(arg);
            MaybePrimitiveExpr mpe = arg as MaybePrimitiveExpr;

            if (primt == paramType)
            {
                mpe.EmitUnboxed(RHC.Expression, objx, ilg);
            }
            else if (primt == typeof(int) && paramType == typeof(long))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Conv_I8);
             }
            else if (primt == typeof(long) && paramType == typeof(int))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, ilg);
                if (RT.booleanCast(RT.UncheckedMathVar.deref()))
                    ilg.Emit(OpCodes.Call,Compiler.Method_RT_uncheckedIntCast_long);
                else
                    ilg.Emit(OpCodes.Call,Compiler.Method_RT_intCast_long);
            }
            else if (primt == typeof(float) && paramType == typeof(double))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Conv_R8);
            }
            else if (primt == typeof(double) && paramType == typeof(float))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Conv_R4);
            }
            else
            {
                arg.Emit(RHC.Expression, objx, ilg);
                HostExpr.EmitUnboxArg(objx, ilg, paramType);
            }
        }


        public static void EmitPrepForCall(CljILGen ilg, Type targetType, Type declaringType)
        {
             EmitConvertToType(ilg, targetType, declaringType, false);
            if (declaringType.IsValueType)
            {
                LocalBuilder vtTemp = ilg.DeclareLocal(declaringType);
                GenContext.SetLocalName(vtTemp, "valueTemp");
                ilg.Emit(OpCodes.Stloc, vtTemp);
                ilg.Emit(OpCodes.Ldloca, vtTemp);
            }
        }

        static MethodInfo MI_EmitConvertToType = typeof(Microsoft.Scripting.Generation.ILGen).GetMethod("EmitConvertToType",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
        internal static void EmitConvertToType(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            //  If the DLR folks had made this method public (instead of internal), I could call it directly.
            //  Didn't feel like copying their code due to license/copyright.
 
            MI_EmitConvertToType.Invoke(ilg, new Object[] {typeFrom, typeTo, isChecked});
        }

        #endregion
    }
}
