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
            if (_method.DeclaringType == (Type)Compiler.COMPILE_STUB_ORIG_CLASS.deref())
                _method = FindEquivalentMethod(_method, objx.BaseType);            
            
            int argCount = _args.Count;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(argCount + (IsStaticCall ? 0 : 1));
            if (!IsStaticCall)
                argsPlus.Add(new DynamicMetaObject(GenTargetExpression(objx, context), BindingRestrictions.Empty));

            List<int> refPositions = new List<int>();

            ParameterInfo[] methodParms = _method.GetParameters();

            for (int i=0; i< argCount; i++ )
            {
                HostArg ha = _args[i];

                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType ? (e.ClrType ?? typeof(object)) : typeof(Object);

                Type t;

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
#if CLR2
                        t = typeof(MSC::System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
#else
                        t = typeof(System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
#endif
                        refPositions.Add(i);
                        argsPlus.Add(new DynamicMetaObject(GenConvertMaybePrim(GenTypedArg(objx, context, argType, e), methodParms[i].ParameterType.GetElementType()), BindingRestrictions.Empty));
                        break;
                    case HostArg.ParameterType.Standard:
                        t = argType;
                        Expression typedArg = GenTypedArg(objx, context, argType, e);
                        Type ptype = methodParms[i].ParameterType;
                        if (!ptype.IsGenericParameter)
                            typedArg = GenConvertMaybePrim(typedArg, methodParms[i].ParameterType);
                        argsPlus.Add(new DynamicMetaObject(typedArg, BindingRestrictions.Empty));

                        break;
                    default:
                        throw Util.UnreachableCode();
                }
                // TODO: Rethink how we are getting typing done.
                //argsPlus.Add(new DynamicMetaObject(Expression.Convert(GenTypedArg(context, argType, e), argType), BindingRestrictions.Empty));
                //argsPlus.Add(new DynamicMetaObject(GenTypedArg(context, argType, e), BindingRestrictions.Empty));
            }

            //OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
            OverloadResolverFactory factory = NumericConvertOverloadResolverFactory.Instance;
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

            return call;
        }


        // RETYPE: TODO: Mostly same as HostExpr.GenMaybeUnboxedArg
        static MethodInfo MI_Util_ConvertToByte = typeof(Util).GetMethod("ConvertToByte");
        static MethodInfo MI_Util_ConvertToSByte = typeof(Util).GetMethod("ConvertToSByte");
        static MethodInfo MI_Util_ConvertToChar = typeof(Util).GetMethod("ConvertToChar");
        static MethodInfo MI_Util_ConvertToDecimal = typeof(Util).GetMethod("ConvertToDecimal");
        static MethodInfo MI_Util_ConvertToShort = typeof(Util).GetMethod("ConvertToShort");
        static MethodInfo MI_Util_ConvertToUShort = typeof(Util).GetMethod("ConvertToUShort");
        static MethodInfo MI_Util_ConvertToInt = typeof(Util).GetMethod("ConvertToInt");
        static MethodInfo MI_Util_ConvertToUInt = typeof(Util).GetMethod("ConvertToUInt");
        static MethodInfo MI_Util_ConvertToLong = typeof(Util).GetMethod("ConvertToLong");
        static MethodInfo MI_Util_ConvertToULong = typeof(Util).GetMethod("ConvertToULong");
        static MethodInfo MI_Util_ConvertToFloat = typeof(Util).GetMethod("ConvertToFloat");
        static MethodInfo MI_Util_ConvertToDouble = typeof(Util).GetMethod("ConvertToDouble");
        static MethodInfo MI_RT_booleanCast = typeof(RT).GetMethod("booleanCast",BindingFlags.Static| BindingFlags.Public,null,new Type[] {typeof(Object)},null);

        

        public static Expression GenConvertMaybePrim(Expression expr, Type toType)
        {
            if ( expr.Type == toType )
                return expr;

            if (toType == typeof(void))
                return Expression.Block(expr, Expression.Empty());

            if (expr.Type == typeof(void))
                return Expression.Block(expr, Expression.Default(toType));

            if ( expr.Type.IsPrimitive && toType.IsPrimitive)
                return Expression.Convert(expr,toType);

            if ( toType.IsPrimitive )
            {
                MethodInfo converter;
                switch ( Type.GetTypeCode(toType) )
                {
                    case TypeCode.Boolean:
                        converter = MI_RT_booleanCast;
                        break;
                    case TypeCode.Byte:
                        converter = MI_Util_ConvertToByte;
                        break;
                    case TypeCode.Decimal:
                        converter = MI_Util_ConvertToDecimal;
                        break;
                    case TypeCode.Char:
                        converter = MI_Util_ConvertToChar;
                        break;
                    case TypeCode.Double:
                        converter = MI_Util_ConvertToDouble;
                        break;
                    case TypeCode.Int16:
                        converter = MI_Util_ConvertToShort;
                        break;
                    case TypeCode.Int32:
                        converter = MI_Util_ConvertToInt;
                        break;
                    case TypeCode.Int64:
                        converter = MI_Util_ConvertToLong;
                        break;
                    case TypeCode.SByte:
                        converter = MI_Util_ConvertToSByte;
                        break;
                    case TypeCode.UInt16:
                        converter = MI_Util_ConvertToUShort;
                        break;
                    case TypeCode.UInt32:
                        converter = MI_Util_ConvertToUInt;
                        break;
                    case TypeCode.UInt64:
                        converter = MI_Util_ConvertToULong;
                        break;
                    case TypeCode.Single:
                        converter = MI_Util_ConvertToFloat;
                        break;
                    default:
                        throw Util.UnreachableCode();
                }
                return Expression.Call(converter,expr);
            }

            return Expression.Convert(expr,toType);
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

            Expression[] argExprs = DynUtils.ArrayInsert<Expression>(target, exprs);

            Type returnType = HasClrType ? ClrType : typeof(object);

            InvokeMemberBinder binder = new DefaultInvokeMemberBinder(_methodName, argExprs.Length, IsStaticCall);
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
                ParameterExpression[] allParams = DynUtils.ArrayInsert<ParameterExpression>(callValParam, sbParams);

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
                            ConstructorInfo[] cinfos = sbType.GetConstructors();
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

        #endregion
    }
}
