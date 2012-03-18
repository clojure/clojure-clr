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
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;
using clojure.lang.Runtime.Binding;
using clojure.lang.Runtime;
using System.Reflection.Emit;
using Microsoft.Scripting.Generation;

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

        static readonly IntrinsicsRewriter _arithmeticRewriter = new IntrinsicsRewriter();

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

        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression call;
            Type retType;

            if (_method != null)
            {
                call = GenDlrForMethod(objx, context);
                retType = _method.ReturnType;
            }
            else
            {
                call = GenerateComplexCall(objx, context);
                retType = typeof(object);
            }

            call = HostExpr.GenBoxReturn(call, retType, objx, context);         
            call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);
            return call;
        }

        public override Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            if (_method != null)
            {
                Expression call = GenDlrForMethod(objx,context);
                call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);
                return call;
            }
            else
                throw new InvalidOperationException("Unboxed emit of unknown member.");
        }


        protected Expression GenDlrForMethod(ObjExpr objx, GenContext context)
        {
            if (_method.DeclaringType == (Type)Compiler.CompileStubOrigClassVar.deref())
                _method = FindEquivalentMethod(_method, objx.BaseType);            
            
            int argCount = _args.Count;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(argCount + (IsStaticCall ? 0 : 1));
            if (!IsStaticCall)
                argsPlus.Add(new DynamicMetaObject(Expression.Convert(GenTargetExpression(objx, context),_method.DeclaringType), BindingRestrictions.Empty));

            List<int> refPositions = new List<int>();

            ParameterInfo[] methodParms = _method.GetParameters();

            for (int i=0; i< argCount; i++ )
            {
                HostArg ha = _args[i];

                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType ? (e.ClrType ?? typeof(object)) : typeof(Object);

                //Type t;

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        refPositions.Add(i);
                        argsPlus.Add(new DynamicMetaObject(HostExpr.GenUnboxArg(GenTypedArg(objx, context, argType, e), methodParms[i].ParameterType.GetElementType()), BindingRestrictions.Empty));
                        break;
                    case HostArg.ParameterType.Standard:
                        Type ptype = methodParms[i].ParameterType;
                        if (ptype.IsGenericParameter)
                            ptype = argType;

                        Expression typedArg = GenTypedArg(objx, context, ptype, e);

                        argsPlus.Add(new DynamicMetaObject(typedArg, BindingRestrictions.Empty));

                        break;
                    default:
                        throw Util.UnreachableCode();
                }
            }

            // TODO: get rid of use of Default
            OverloadResolverFactory factory = ClojureContext.Default.SharedOverloadResolverFactory;
            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(argCount), IsStaticCall ? CallTypes.None : CallTypes.ImplicitInstance);

            List<MethodBase> methods = new List<MethodBase>();
            methods.Add(_method);

            BindingTarget bt = res.ResolveOverload(_methodName, methods, NarrowingLevel.None, NarrowingLevel.All);
            if (!bt.Success)
                throw new ArgumentException("Conflict in argument matching. -- Internal error.");

            Expression call = bt.MakeExpression();

            if (refPositions.Count > 0)
            {
                ParameterExpression resultParm = Expression.Parameter(typeof(Object[]));

                List<Expression> stmts = new List<Expression>(refPositions.Count + 2);
                stmts.Add(Expression.Assign(resultParm, call));

                // TODO: Fold this into the loop above
                foreach (int i in refPositions)
                {
                    HostArg ha = _args[i];
                    Expr e = ha.ArgExpr;
                    Type argType = e.HasClrType ? (e.ClrType ?? typeof(object)) : typeof(Object);
                    stmts.Add(Expression.Assign(_args[i].LocalBinding.ParamExpression, Expression.Convert(Expression.ArrayIndex(resultParm, Expression.Constant(i + 1)), argType)));
                }

                Type returnType = HasClrType ? ClrType : typeof(object);
                stmts.Add(Expression.Convert(Expression.ArrayIndex(resultParm, Expression.Constant(0)), returnType));
                call = Expression.Block(new ParameterExpression[] { resultParm }, stmts);
            }

            call = _arithmeticRewriter.Visit(call);

            return call;
        }


        private MethodInfo FindEquivalentMethod(MethodInfo _method, Type baseType)
        {
            BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic;

            if (IsStaticCall)
                flags |= BindingFlags.Static;
            else
                flags |= BindingFlags.Instance;
          
            return baseType.GetMethod(_method.Name,flags, null, Compiler.GetTypes(_method.GetParameters()), null);
        }


        private Expression GenerateComplexCall(ObjExpr objx, GenContext context)
        {
            Expression call;

            Expression target = GenTargetExpression(objx, context);

            List<Expression> exprs = new List<Expression>(_args.Count);
            List<ParameterExpression> sbParams = new List<ParameterExpression>();
            List<Expression> sbInits = new List<Expression>();
            List<Expression> sbTransfers = new List<Expression>();
            GenerateComplexArgList(objx, context, _args, out exprs, out sbParams, out sbInits, out sbTransfers);

            Expression[] argExprs = ClrExtensions.ArrayInsert<Expression>(target, exprs);

            Type returnType = HasClrType ? ClrType : typeof(object);

            // TODO: Get rid of Default
            InvokeMemberBinder binder = new ClojureInvokeMemberBinder(ClojureContext.Default,_methodName, argExprs.Length, IsStaticCall);
            //DynamicExpression dyn = Expression.Dynamic(binder, returnType, argExprs);
            DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), argExprs);

            //if (context.Mode == CompilerMode.File)
            if ( context.DynInitHelper != null )
                call = context.DynInitHelper.ReduceDyn(dyn);
            else
                call = dyn;

            if (returnType == typeof(void))
                call = Expression.Block(call, Expression.Default(typeof(object)));
            else
                call = Expression.Convert(call, returnType);

            if (sbParams.Count > 0)
            {

                // We have ref/out params.  Construct the complicated call;

                ParameterExpression callValParam = Expression.Parameter(returnType, "__callVal");
                ParameterExpression[] allParams = ClrExtensions.ArrayInsert<ParameterExpression>(callValParam, sbParams);

                call = Expression.Block(
                    returnType,
                    allParams,
                    Expression.Block(sbInits),
                    Expression.Assign(callValParam, call),
                    Expression.Block(sbTransfers),
                    callValParam);
            }

            return call;
        }

       internal static void GenerateComplexArgList(
           ObjExpr objx,
            GenContext context,
            List<HostArg> args,
            out List<Expression> argExprs,
            out List<ParameterExpression> sbParams,
            out  List<Expression> sbInits,
            out List<Expression> sbTransfers)
        {
            argExprs = new List<Expression>(args.Count);
            sbParams = new List<ParameterExpression>();
            sbInits = new List<Expression>();
            sbTransfers = new List<Expression>();

            BindingFlags cflags = BindingFlags.Public | BindingFlags.Instance;

            foreach (HostArg ha in args)
            {
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType ? (e.ClrType ?? typeof(Object)) : typeof(Object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        {
#if CLR2
                            Type sbType = typeof(MSC::System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
#else
                            Type sbType = typeof(System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
#endif

                            ParameterExpression sbParam = Expression.Parameter(sbType, String.Format("__sb_{0}", sbParams.Count));
                            //ConstructorInfo[] cinfos = sbType.GetConstructors();
                            Expression sbInit1 =
                                Expression.Assign(
                                    sbParam,
                                    Expression.New(
                                        sbType.GetConstructor(cflags, null, new Type[] { argType }, null),
                                        Expression.Convert(ha.LocalBinding.ParamExpression,argType)));
                            Expression sbXfer = Expression.Assign(ha.LocalBinding.ParamExpression, Expression.Field(sbParam, "Value"));
                            sbParams.Add(sbParam);
                            sbInits.Add(sbInit1);
                            sbTransfers.Add(sbXfer);
                            argExprs.Add(sbParam);
                        }
                        break;
                    case HostArg.ParameterType.Standard:
                        argExprs.Add(e.GenCode(RHC.Expression, objx, context));
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }
        }

        protected abstract bool IsStaticCall { get; }
        protected abstract Expression GenTargetExpression(ObjExpr objx, GenContext context);

        public override bool CanEmitPrimitive
        {
            get { return _method != null && Util.IsPrimitive(_method.ReturnType); }
        }

        public override void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGenerator ilg = context.GetILGenerator();

            Compiler.MaybeEmitDebugInfo(context, ilg, _spanMap);

            Type retType;

            if (_method != null)
            {
                EmitForMethod(objx, context);
                retType = _method.ReturnType;
            }
            else
            {
                EmitComplexCall(objx, context);
                retType = typeof(object);
            }
            HostExpr.EmitBoxReturn(objx, context, retType);

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public override void EmitUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGenerator ilg = context.GetILGenerator();

            Compiler.MaybeEmitDebugInfo(context, ilg, _spanMap);

            if (_method != null)
            {
                EmitForMethod(objx, context);
            }
            else
            {
                throw new InvalidOperationException("Unboxed emit of unknown member.");
            }

            if (rhc == RHC.Statement)
               ilg.Emit(OpCodes.Pop);
        }

        private void EmitForMethod(ObjExpr objx, GenContext context)
        {
            //if (_method.DeclaringType == (Type)Compiler.CompileStubOrigClassVar.deref())
            //    _method = FindEquivalentMethod(_method, objx.BaseType);
            ILGenerator ilg = context.GetILGenerator();

            if (_args.Exists((x) => x.ParamType == HostArg.ParameterType.ByRef)
                || Array.Exists(_method.GetParameters(),(x)=> x.ParameterType.IsByRef)
                || _method.IsGenericMethodDefinition)
            {
                EmitComplexCall(objx, context);
                return;
            }

            // No by-ref args, not a generic method, so we can generate straight arg converts and call
            if (!IsStaticCall)
            {
                EmitTargetExpression(objx, context);
                EmitPrepForCall(context,typeof(object),_method.DeclaringType);
            }

            EmitTypedArgs(objx, context, _method.GetParameters(), _args);
            if (IsStaticCall)
            {
                if (Intrinsics.HasOp(_method))
                    Intrinsics.EmitOp(_method,context.GetILGenerator());
                else
                    ilg.Emit(OpCodes.Call, _method);
            }
            else
                ilg.Emit(OpCodes.Callvirt, _method); 
        }





        protected abstract void EmitTargetExpression(ObjExpr objx, GenContext context);
        protected abstract Type GetTargetType();


        private void EmitComplexCall(ObjExpr objx, GenContext context)
        {
            List<ParameterExpression> paramExprs = new List<ParameterExpression>(_args.Count + 1);

            Type targetType = GetTargetType();
            if (!targetType.IsPrimitive)
                targetType = typeof(object);

            //if (targetType == (Type)Compiler.CompileStubOrigClassVar.deref())
            //    targetType = objx.TypeBlder;

            paramExprs.Add(Expression.Parameter(targetType));
            EmitTargetExpression(objx, context);

            int i = 0;
            foreach (HostArg ha in _args)
            {
                i++;
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType && e.ClrType != null && e.ClrType.IsPrimitive ? e.ClrType : typeof(object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        paramExprs.Add(Expression.Parameter(argType.MakeByRefType(), ha.LocalBinding.Name));
                        context.GetILGenerator().Emit(OpCodes.Ldloca, ha.LocalBinding.LocalVar);
                        break;

                    case HostArg.ParameterType.Standard:
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr)
                        {
                            paramExprs.Add(Expression.Parameter(argType, ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            ((MaybePrimitiveExpr)ha.ArgExpr).EmitUnboxed(RHC.Expression, objx, context);
                        }
                        else
                        {
                            paramExprs.Add(Expression.Parameter(typeof(object), ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            ha.ArgExpr.Emit(RHC.Expression, objx, context);
                        }
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }

            Type returnType = HasClrType ? ClrType : typeof(object);
            InvokeMemberBinder binder = new ClojureInvokeMemberBinder(ClojureContext.Default, _methodName, paramExprs.Count, IsStaticCall);
            DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramExprs);
            Expression call = dyn;
            if (context.DynInitHelper != null)
                call = context.DynInitHelper.ReduceDyn(dyn);

            if (returnType == typeof(void))
            {
                call = Expression.Block(call, Expression.Default(typeof(object)));
                returnType = typeof(object);
            }
            call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);

            Type[] paramTypes = paramExprs.Map((x) => x.Type);
            MethodBuilder mbLambda = context.TB.DefineMethod("__interop_" + _methodName + RT.nextID(), MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, returnType, paramTypes);
            LambdaExpression lambda = Expression.Lambda(call, paramExprs);
            lambda.CompileToMethod(mbLambda);

            context.GetILGenerator().Emit(OpCodes.Call, mbLambda);
        }
        
        #endregion

        internal static void EmitArgsAsArray(IPersistentVector args, ObjExpr objx, GenContext context)
        {
            ILGen ilg2 = context.GetILGen();
            ilg2.EmitInt(args.count());
            ilg2.Emit(OpCodes.Newarr, typeof(Object));

            for (int i = 0; i < args.count(); i++)
            {
                ilg2.Emit(OpCodes.Dup);
                ilg2.EmitInt(i);
                ((Expr)args.nth(i)).Emit(RHC.Expression, objx, context);
                ilg2.Emit(OpCodes.Stelem_Ref);
            }
        }

        public static void EmitTypedArgs(ObjExpr objx, GenContext context, ParameterInfo[] parms, List<HostArg> args)
        {
            for (int i = 0; i < parms.Length; i++)
                EmitTypedArg(objx, context, parms[i].ParameterType, args[i].ArgExpr);

        }

        public static void EmitTypedArgs(ObjExpr objx, GenContext context, ParameterInfo[] parms, IPersistentVector args)
        {
            for (int i = 0; i < parms.Length; i++)
                EmitTypedArg(objx, context, parms[i].ParameterType, (Expr)args.nth(i));
        }

        public static void EmitTypedArg(ObjExpr objx, GenContext context, Type paramType, Expr arg)
        {
            Type primt = Compiler.MaybePrimitiveType(arg);
            MaybePrimitiveExpr mpe = arg as MaybePrimitiveExpr;
            ILGen ilg = context.GetILGen();

            if (primt == paramType)
            {
                mpe.EmitUnboxed(RHC.Expression, objx, context);
            }
            else if (primt == typeof(int) && paramType == typeof(long))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Conv_I8);
             }
            else if (primt == typeof(long) && paramType == typeof(int))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, context);
                if (RT.booleanCast(RT.UncheckedMathVar.deref()))
                    ilg.Emit(OpCodes.Call,Compiler.Method_RT_uncheckedIntCast_long);
                else
                    ilg.Emit(OpCodes.Call,Compiler.Method_RT_intCast_long);
            }
            else if (primt == typeof(float) && paramType == typeof(double))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Conv_R8);
            }
            else if (primt == typeof(double) && paramType == typeof(float))
            {
                mpe.EmitUnboxed(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Conv_R4);
            }
            else
            {
                arg.Emit(RHC.Expression, objx, context);
                HostExpr.EmitUnboxArg(objx, context, paramType);
            }
        }


        public static void EmitPrepForCall(GenContext context, Type targetType, Type declaringType)
        {
            ILGenerator ilg = context.GetILGenerator();

            EmitConvertToType(ilg, targetType, declaringType, false);
            if (declaringType.IsValueType)
            {
                LocalBuilder vtTemp = ilg.DeclareLocal(declaringType);
                Compiler.MaybeSetLocalSymName(context, vtTemp, "valueTemp");
                ilg.Emit(OpCodes.Stloc, vtTemp);
                ilg.Emit(OpCodes.Ldloca, vtTemp);
            }
        }


        // Adopted from DLR code (ILGen)
        internal static void EmitConvertToType(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            if (TypesAreEquivalent(typeFrom, typeTo))
                return;

            if (typeFrom == typeof(void) || typeTo == typeof(void))
                return;

            bool isTypeFromNullable = IsNullableType(typeFrom);
            bool isTypeToNullable = IsNullableType(typeTo);

            Type nnExprType = GetNonNullableType(typeFrom);
            Type nnType = GetNonNullableType(typeTo);

            // DLR also tests here: TypeUtils.IsLegalExplicitVariantDelegateConversion(typeFrom, typeTo))

            if (typeFrom.IsInterface || // interface cast
               typeTo.IsInterface ||
               typeFrom == typeof(object) || // boxing cast
               typeTo == typeof(object))
            {
                EmitCastToType(ilg, typeFrom, typeTo);
            }
            else if (isTypeFromNullable || isTypeToNullable)
            {
                EmitNullableConversion(ilg, typeFrom, typeTo, isChecked);
            }
            else if (!(IsConvertible(typeFrom) && IsConvertible(typeTo)) // primitive runtime conversion
                     &&
                     (nnExprType.IsAssignableFrom(nnType) || // down cast
                     nnType.IsAssignableFrom(nnExprType))) // up cast
            {
                EmitCastToType(ilg, typeFrom, typeTo);
            }
            else if (typeFrom.IsArray && typeTo.IsArray)
            {
                // See DevDiv Bugs #94657.
                EmitCastToType(ilg, typeFrom, typeTo);
            }
            else
            {
                EmitNumericConversion(ilg, typeFrom, typeTo, isChecked);
            }
        }        
        
        // Stolen from DLR code
        static void EmitCastToType(ILGenerator ilg, Type typeFrom, Type typeTo) {
            if (!typeFrom.IsValueType && typeTo.IsValueType) {
                ilg.Emit(OpCodes.Unbox_Any, typeTo);
            } else if (typeFrom.IsValueType && !typeTo.IsValueType) {
                ilg.Emit(OpCodes.Box, typeFrom);
                if (typeTo != typeof(object)) {
                    ilg.Emit(OpCodes.Castclass, typeTo);
                }
            } else if (!typeFrom.IsValueType && !typeTo.IsValueType) {
                ilg.Emit(OpCodes.Castclass, typeTo);
            } else {
                throw new InvalidCastException(String.Format("Cannot cast from {0} to {1}", typeFrom, typeTo));
            }
        }


        // Taken from DLR code (TypeUtils)
        static bool TypesAreEquivalent(Type t1, Type t2)
        {
            return t1 == t2 || t1.IsEquivalentTo(t2);
        }

        // Taken from DLR code (TypeUtils) 
        internal static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        // Taken from DLR code (TypeUtils) 
        internal static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        // Taken from DLR code (TypeUtils) 
        internal static bool IsConvertible(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum)
            {
                return true;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                    return true;
                default:
                    return false;
            }
        }

        // Taken from DLR code (TypeUtils) 
        internal static bool IsFloatingPointType(Type type)
        {
            type = GetNonNullableType(type);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        // Taken from DLR code (TypeUtils) 
        internal static bool IsUnsignedType(Type type)
        {
            type = GetNonNullableType(type);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }




        // Taken from DLR code (ILGen)
        private static void EmitNumericConversion(ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
        {
            bool isFromUnsigned = IsUnsignedType(typeFrom);
            bool isFromFloatingPoint = IsFloatingPointType(typeFrom);
            if (typeTo == typeof(Single))
            {
                if (isFromUnsigned)
                    il.Emit(OpCodes.Conv_R_Un);
                il.Emit(OpCodes.Conv_R4);
            }
            else if (typeTo == typeof(Double))
            {
                if (isFromUnsigned)
                    il.Emit(OpCodes.Conv_R_Un);
                il.Emit(OpCodes.Conv_R8);
            }
            else
            {
                TypeCode tc = Type.GetTypeCode(typeTo);
                if (isChecked)
                {
                    // Overflow checking needs to know if the source value on the IL stack is unsigned or not.
                    if (isFromUnsigned)
                    {
                        switch (tc)
                        {
                            case TypeCode.SByte:
                                il.Emit(OpCodes.Conv_Ovf_I1_Un);
                                break;
                            case TypeCode.Int16:
                                il.Emit(OpCodes.Conv_Ovf_I2_Un);
                                break;
                            case TypeCode.Int32:
                                il.Emit(OpCodes.Conv_Ovf_I4_Un);
                                break;
                            case TypeCode.Int64:
                                il.Emit(OpCodes.Conv_Ovf_I8_Un);
                                break;
                            case TypeCode.Byte:
                                il.Emit(OpCodes.Conv_Ovf_U1_Un);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                il.Emit(OpCodes.Conv_Ovf_U2_Un);
                                break;
                            case TypeCode.UInt32:
                                il.Emit(OpCodes.Conv_Ovf_U4_Un);
                                break;
                            case TypeCode.UInt64:
                                il.Emit(OpCodes.Conv_Ovf_U8_Un);
                                break;
                            default:
                                throw new InvalidOperationException("Cannot convert to " + typeTo);
                        }
                    }
                    else
                    {
                        switch (tc)
                        {
                            case TypeCode.SByte:
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                break;
                            case TypeCode.Int16:
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                break;
                            case TypeCode.Int32:
                                il.Emit(OpCodes.Conv_Ovf_I4);
                                break;
                            case TypeCode.Int64:
                                il.Emit(OpCodes.Conv_Ovf_I8);
                                break;
                            case TypeCode.Byte:
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                break;
                            case TypeCode.UInt32:
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                break;
                            case TypeCode.UInt64:
                                il.Emit(OpCodes.Conv_Ovf_U8);
                                break;
                            default:
                                throw new InvalidOperationException("Cannot convert to " + typeTo);
                        }
                    }
                }
                else
                {
                    switch (tc)
                    {
                        case TypeCode.SByte:
                            il.Emit(OpCodes.Conv_I1);
                            break;
                        case TypeCode.Byte:
                            il.Emit(OpCodes.Conv_U1);
                            break;
                        case TypeCode.Int16:
                            il.Emit(OpCodes.Conv_I2);
                            break;
                        case TypeCode.UInt16:
                        case TypeCode.Char:
                            il.Emit(OpCodes.Conv_U2);
                            break;
                        case TypeCode.Int32:
                            il.Emit(OpCodes.Conv_I4);
                            break;
                        case TypeCode.UInt32:
                            il.Emit(OpCodes.Conv_U4);
                            break;
                        case TypeCode.Int64:
                            if (isFromUnsigned)
                            {
                                il.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                il.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        case TypeCode.UInt64:
                            if (isFromUnsigned || isFromFloatingPoint)
                            {
                                il.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                il.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Cannot convert to " + typeTo);
                    }
                }
            }
        }

        // Taken from DLR code (ILGen)
        private static void EmitNullableConversion(ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
        {
            bool isTypeFromNullable = IsNullableType(typeFrom);
            bool isTypeToNullable = IsNullableType(typeTo);
              if (isTypeFromNullable && isTypeToNullable)
                EmitNullableToNullableConversion(il,typeFrom, typeTo, isChecked);
            else if (isTypeFromNullable)
                EmitNullableToNonNullableConversion(il, typeFrom, typeTo, isChecked);
            else
                EmitNonNullableToNullableConversion(il, typeFrom, typeTo, isChecked);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNonNullableToNullableConversion(ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
        {
            //Debug.Assert(!TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(TypeUtils.IsNullableType(typeTo));
            LocalBuilder locTo = null;
            locTo = il.DeclareLocal(typeTo);
            Type nnTypeTo = GetNonNullableType(typeTo);
            EmitConvertToType(il, typeFrom, nnTypeTo, isChecked);
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            il.Emit(OpCodes.Newobj, ci);
            il.Emit(OpCodes.Stloc, locTo);
            il.Emit(OpCodes.Ldloc, locTo);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToNonNullableConversion(ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
        {
            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(!TypeUtils.IsNullableType(typeTo));
            if (typeTo.IsValueType)
                EmitNullableToNonNullableStructConversion(il,typeFrom, typeTo, isChecked);
            else
                EmitNullableToReferenceConversion(il,typeFrom);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToNonNullableStructConversion(ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
        {

            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(!TypeUtils.IsNullableType(typeTo));
            //Debug.Assert(typeTo.IsValueType);
            LocalBuilder locFrom = null;
            locFrom = il.DeclareLocal(typeFrom);
            il.Emit(OpCodes.Stloc, locFrom);
            il.Emit(OpCodes.Ldloca, locFrom);
            EmitGetValue(il,typeFrom);
            Type nnTypeFrom = GetNonNullableType(typeFrom);
            EmitConvertToType(il, nnTypeFrom, typeTo, isChecked);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToReferenceConversion(ILGenerator il, Type typeFrom)
        {
            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            // We've got a conversion from nullable to Object, ValueType, Enum, etc.  Just box it so that
            // we get the nullable semantics.  
            il.Emit(OpCodes.Box, typeFrom);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToNullableConversion(ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
        {
            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(TypeUtils.IsNullableType(typeTo));
            Label labIfNull = default(Label);
            Label labEnd = default(Label);
            LocalBuilder locFrom = null;
            LocalBuilder locTo = null;
            locFrom = il.DeclareLocal(typeFrom);
            il.Emit(OpCodes.Stloc, locFrom);
            locTo = il.DeclareLocal(typeTo);
            // test for null
            il.Emit(OpCodes.Ldloca, locFrom);
            EmitHasValue(il,typeFrom);
            labIfNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, labIfNull);
            il.Emit(OpCodes.Ldloca, locFrom);
            EmitGetValueOrDefault(il,typeFrom);
            Type nnTypeFrom = GetNonNullableType(typeFrom);
            Type nnTypeTo = GetNonNullableType(typeTo);
            EmitConvertToType(il,nnTypeFrom, nnTypeTo, isChecked);
            // construct result type
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            il.Emit(OpCodes.Newobj, ci);
            il.Emit(OpCodes.Stloc, locTo);
            labEnd = il.DefineLabel();
            il.Emit(OpCodes.Br_S, labEnd);
            // if null then create a default one
            il.MarkLabel(labIfNull);
            il.Emit(OpCodes.Ldloca, locTo);
            il.Emit(OpCodes.Initobj, typeTo);
            il.MarkLabel(labEnd);
            il.Emit(OpCodes.Ldloc, locTo);
        }


        // Taken from DLR code (ILGen)
        internal static void EmitHasValue(ILGenerator il, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
            //Debug.Assert(nullableType.IsValueType);
            il.Emit(OpCodes.Call, mi);
        }


        // Taken from DLR code (ILGen)
        internal static void EmitGetValue(ILGenerator il, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            //Debug.Assert(nullableType.IsValueType);
            il.Emit(OpCodes.Call, mi);
        }


        // Taken from DLR code (ILGen)
        internal static void EmitGetValueOrDefault(ILGenerator il, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("GetValueOrDefault", System.Type.EmptyTypes);
            //Debug.Assert(nullableType.IsValueType);
            il.Emit(OpCodes.Call, mi);
        }

    }
}
