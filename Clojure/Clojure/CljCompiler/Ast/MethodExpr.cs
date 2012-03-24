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
            //if (_method.DeclaringType == (Type)Compiler.CompileStubOrigClassVar.deref())
            //    _method = FindEquivalentMethod(_method, objx.BaseType);
            if (_args.Exists((x) => x.ParamType == HostArg.ParameterType.ByRef)
                || Array.Exists(_method.GetParameters(),(x)=> x.ParameterType.IsByRef)
                || _method.IsGenericMethodDefinition)
            {
                EmitComplexCall(objx, ilg);
                return;
            }

            // No by-ref args, not a generic method, so we can generate straight arg converts and call
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


        protected abstract void EmitTargetExpression(ObjExpr objx, CljILGen ilg);
        protected abstract Type GetTargetType();

        private void EmitComplexCall(ObjExpr objx, CljILGen ilg)
        {
            List<ParameterExpression> paramExprs = new List<ParameterExpression>(_args.Count + 1);

            Type targetType = GetTargetType();
            if (!targetType.IsPrimitive)
                targetType = typeof(object);

            //if (targetType == (Type)Compiler.CompileStubOrigClassVar.deref())
            //    targetType = objx.TypeBlder;

            paramExprs.Add(Expression.Parameter(targetType));
            EmitTargetExpression(objx, ilg);

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
                        ilg.Emit(OpCodes.Ldloca, ha.LocalBinding.LocalVar);
                        break;

                    case HostArg.ParameterType.Standard:
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr)
                        {
                            paramExprs.Add(Expression.Parameter(argType, ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            ((MaybePrimitiveExpr)ha.ArgExpr).EmitUnboxed(RHC.Expression, objx, ilg);
                        }
                        else
                        {
                            paramExprs.Add(Expression.Parameter(typeof(object), ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            ha.ArgExpr.Emit(RHC.Expression, objx, ilg);
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

            GenContext context = Compiler.CompilerContextVar.deref() as GenContext;
            if (context.DynInitHelper != null)
                call = context.DynInitHelper.ReduceDyn(dyn);

            if (returnType == typeof(void))
            {
                call = Expression.Block(call, Expression.Default(typeof(object)));
                returnType = typeof(object);
            }
            call = GenContext.AddDebugInfo(call, _spanMap);

            Type[] paramTypes = paramExprs.Map((x) => x.Type);
            MethodBuilder mbLambda = context.TB.DefineMethod("__interop_" + _methodName + RT.nextID(), MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, returnType, paramTypes);
            LambdaExpression lambda = Expression.Lambda(call, paramExprs);
            lambda.CompileToMethod(mbLambda);

            ilg.Emit(OpCodes.Call, mbLambda);
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
                EmitTypedArg(objx, ilg, parms[i].ParameterType, args[i].ArgExpr);

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


        // Adopted from DLR code (ILGen)
        internal static void EmitConvertToType(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
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
        static void EmitCastToType(CljILGen ilg, Type typeFrom, Type typeTo)
        {
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
        private static void EmitNumericConversion(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            bool isFromUnsigned = IsUnsignedType(typeFrom);
            bool isFromFloatingPoint = IsFloatingPointType(typeFrom);
            if (typeTo == typeof(Single))
            {
                if (isFromUnsigned)
                    ilg.Emit(OpCodes.Conv_R_Un);
                ilg.Emit(OpCodes.Conv_R4);
            }
            else if (typeTo == typeof(Double))
            {
                if (isFromUnsigned)
                    ilg.Emit(OpCodes.Conv_R_Un);
                ilg.Emit(OpCodes.Conv_R8);
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
                                ilg.Emit(OpCodes.Conv_Ovf_I1_Un);
                                break;
                            case TypeCode.Int16:
                                ilg.Emit(OpCodes.Conv_Ovf_I2_Un);
                                break;
                            case TypeCode.Int32:
                                ilg.Emit(OpCodes.Conv_Ovf_I4_Un);
                                break;
                            case TypeCode.Int64:
                                ilg.Emit(OpCodes.Conv_Ovf_I8_Un);
                                break;
                            case TypeCode.Byte:
                                ilg.Emit(OpCodes.Conv_Ovf_U1_Un);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                ilg.Emit(OpCodes.Conv_Ovf_U2_Un);
                                break;
                            case TypeCode.UInt32:
                                ilg.Emit(OpCodes.Conv_Ovf_U4_Un);
                                break;
                            case TypeCode.UInt64:
                                ilg.Emit(OpCodes.Conv_Ovf_U8_Un);
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
                                ilg.Emit(OpCodes.Conv_Ovf_I1);
                                break;
                            case TypeCode.Int16:
                                ilg.Emit(OpCodes.Conv_Ovf_I2);
                                break;
                            case TypeCode.Int32:
                                ilg.Emit(OpCodes.Conv_Ovf_I4);
                                break;
                            case TypeCode.Int64:
                                ilg.Emit(OpCodes.Conv_Ovf_I8);
                                break;
                            case TypeCode.Byte:
                                ilg.Emit(OpCodes.Conv_Ovf_U1);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                ilg.Emit(OpCodes.Conv_Ovf_U2);
                                break;
                            case TypeCode.UInt32:
                                ilg.Emit(OpCodes.Conv_Ovf_U4);
                                break;
                            case TypeCode.UInt64:
                                ilg.Emit(OpCodes.Conv_Ovf_U8);
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
                            ilg.Emit(OpCodes.Conv_I1);
                            break;
                        case TypeCode.Byte:
                            ilg.Emit(OpCodes.Conv_U1);
                            break;
                        case TypeCode.Int16:
                            ilg.Emit(OpCodes.Conv_I2);
                            break;
                        case TypeCode.UInt16:
                        case TypeCode.Char:
                            ilg.Emit(OpCodes.Conv_U2);
                            break;
                        case TypeCode.Int32:
                            ilg.Emit(OpCodes.Conv_I4);
                            break;
                        case TypeCode.UInt32:
                            ilg.Emit(OpCodes.Conv_U4);
                            break;
                        case TypeCode.Int64:
                            if (isFromUnsigned)
                            {
                                ilg.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        case TypeCode.UInt64:
                            if (isFromUnsigned || isFromFloatingPoint)
                            {
                                ilg.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Cannot convert to " + typeTo);
                    }
                }
            }
        }

        // Taken from DLR code (ILGen)
        private static void EmitNullableConversion(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            bool isTypeFromNullable = IsNullableType(typeFrom);
            bool isTypeToNullable = IsNullableType(typeTo);
              if (isTypeFromNullable && isTypeToNullable)
                EmitNullableToNullableConversion(ilg,typeFrom, typeTo, isChecked);
            else if (isTypeFromNullable)
                EmitNullableToNonNullableConversion(ilg, typeFrom, typeTo, isChecked);
            else
                EmitNonNullableToNullableConversion(ilg, typeFrom, typeTo, isChecked);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNonNullableToNullableConversion(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            //Debug.Assert(!TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(TypeUtils.IsNullableType(typeTo));
            LocalBuilder locTo = null;
            locTo = ilg.DeclareLocal(typeTo);
            Type nnTypeTo = GetNonNullableType(typeTo);
            EmitConvertToType(ilg, typeFrom, nnTypeTo, isChecked);
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            ilg.Emit(OpCodes.Newobj, ci);
            ilg.Emit(OpCodes.Stloc, locTo);
            ilg.Emit(OpCodes.Ldloc, locTo);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToNonNullableConversion(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(!TypeUtils.IsNullableType(typeTo));
            if (typeTo.IsValueType)
                EmitNullableToNonNullableStructConversion(ilg,typeFrom, typeTo, isChecked);
            else
                EmitNullableToReferenceConversion(ilg,typeFrom);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToNonNullableStructConversion(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
        {

            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(!TypeUtils.IsNullableType(typeTo));
            //Debug.Assert(typeTo.IsValueType);
            LocalBuilder locFrom = null;
            locFrom = ilg.DeclareLocal(typeFrom);
            ilg.Emit(OpCodes.Stloc, locFrom);
            ilg.Emit(OpCodes.Ldloca, locFrom);
            EmitGetValue(ilg,typeFrom);
            Type nnTypeFrom = GetNonNullableType(typeFrom);
            EmitConvertToType(ilg, nnTypeFrom, typeTo, isChecked);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToReferenceConversion(CljILGen ilg, Type typeFrom)
        {
            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            // We've got a conversion from nullable to Object, ValueType, Enum, etc.  Just box it so that
            // we get the nullable semantics.  
            ilg.Emit(OpCodes.Box, typeFrom);
        }


        // Taken from DLR code (ILGen)
        private static void EmitNullableToNullableConversion(CljILGen ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            //Debug.Assert(TypeUtils.IsNullableType(typeFrom));
            //Debug.Assert(TypeUtils.IsNullableType(typeTo));
            Label labIfNull = default(Label);
            Label labEnd = default(Label);
            LocalBuilder locFrom = null;
            LocalBuilder locTo = null;
            locFrom = ilg.DeclareLocal(typeFrom);
            ilg.Emit(OpCodes.Stloc, locFrom);
            locTo = ilg.DeclareLocal(typeTo);
            // test for null
            ilg.Emit(OpCodes.Ldloca, locFrom);
            EmitHasValue(ilg,typeFrom);
            labIfNull = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brfalse_S, labIfNull);
            ilg.Emit(OpCodes.Ldloca, locFrom);
            EmitGetValueOrDefault(ilg,typeFrom);
            Type nnTypeFrom = GetNonNullableType(typeFrom);
            Type nnTypeTo = GetNonNullableType(typeTo);
            EmitConvertToType(ilg,nnTypeFrom, nnTypeTo, isChecked);
            // construct result type
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            ilg.Emit(OpCodes.Newobj, ci);
            ilg.Emit(OpCodes.Stloc, locTo);
            labEnd = ilg.DefineLabel();
            ilg.Emit(OpCodes.Br_S, labEnd);
            // if null then create a default one
            ilg.MarkLabel(labIfNull);
            ilg.Emit(OpCodes.Ldloca, locTo);
            ilg.Emit(OpCodes.Initobj, typeTo);
            ilg.MarkLabel(labEnd);
            ilg.Emit(OpCodes.Ldloc, locTo);
        }


        // Taken from DLR code (ILGen)
        internal static void EmitHasValue(CljILGen ilg, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
            //Debug.Assert(nullableType.IsValueType);
            ilg.Emit(OpCodes.Call, mi);
        }


        // Taken from DLR code (ILGen)
        internal static void EmitGetValue(CljILGen ilg, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            //Debug.Assert(nullableType.IsValueType);
            ilg.Emit(OpCodes.Call, mi);
        }


        // Taken from DLR code (ILGen)
        internal static void EmitGetValueOrDefault(CljILGen ilg, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("GetValueOrDefault", System.Type.EmptyTypes);
            //Debug.Assert(nullableType.IsValueType);
            ilg.Emit(OpCodes.Call, mi);
        }

        #endregion
    }
}
