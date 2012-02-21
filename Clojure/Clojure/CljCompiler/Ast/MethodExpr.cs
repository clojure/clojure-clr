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

        public override void Emit(RHC rhc, ObjExpr2 objx, GenContext context)
        {
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
                context.GetILGenerator().Emit(OpCodes.Pop);
        }

        public override void EmitUnboxed(RHC rhc, ObjExpr2 objx, GenContext context)
        {
            if (_method != null)
            {
                EmitForMethod(objx, context);
            }
            else
            {
                throw new InvalidOperationException("Unboxed emit of unknown member.");
            }

            if (rhc == RHC.Statement)
                context.GetILGenerator().Emit(OpCodes.Pop);
        }

        private void EmitForMethod(ObjExpr2 objx, GenContext context)
        {
            // TODO: Do we still need this?
            //if (_method.DeclaringType == (Type)Compiler.CompileStubOrigClassVar.deref())
            //    _method = FindEquivalentMethod(_method, objx.BaseType);

            if (_args.Exists((x) => x.ParamType == HostArg.ParameterType.ByRef)
                || Array.Exists(_method.GetParameters(),(x)=> x.IsOut)
                || _method.IsGenericMethodDefinition)
            {
                EmitComplexCall(objx, context);
                return;
            }

            // No by-ref args, not a generic method, so we can generate straight arg converts and call
            if (!IsStaticCall)
            {
                EmitTargetExpression(objx, context);
                context.GetILGenerator().Emit(OpCodes.Castclass, _method.DeclaringType);
            }

            MethodExpr.EmitTypedArgs(objx, context, _method.GetParameters(), _args);
            if (IsStaticCall)
            {
                if (IsIntrinsic(_method))
                    EmitIntrinsicCall(objx, context);
                else
                    context.GetILGenerator().Emit(OpCodes.Call, _method);
            }
            else
                context.GetILGenerator().Emit(OpCodes.Callvirt, _method); 
        }

        private void EmitIntrinsicCall(ObjExpr2 objx, GenContext context)
        {
            throw new NotImplementedException();
        }

        private bool IsIntrinsic(MethodInfo _method)
        {
            throw new NotImplementedException();
        }

        public static void EmitTypedArgs(ObjExpr2 objx, GenContext context, ParameterInfo[] parameterInfo, List<HostArg> _args)
        {
            throw new NotImplementedException();
        }

        protected abstract void EmitTargetExpression(ObjExpr2 objx, GenContext context);
        protected abstract Type GetTargetType();


        private void EmitComplexCall(ObjExpr2 objx, GenContext context)
        {
            List<ParameterExpression> paramExprs = new List<ParameterExpression>(_args.Count+1);
            paramExprs.Add(Expression.Parameter(GetTargetType()));
            EmitTargetExpression(objx,context);

            foreach ( HostArg ha in _args )
            {
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType ? (e.ClrType ?? typeof(Object)) : typeof(Object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        paramExprs.Add(Expression.Parameter(argType.MakeByRefType(),ha.LocalBinding.Name));
                        context.GetILGenerator().Emit(OpCodes.Ldloca,ha.LocalBinding.LocalVar);
                        break;

                    case HostArg.ParameterType.Standard:
                        paramExprs.Add(Expression.Parameter(argType,ha.LocalBinding.Name));
                        ha.ArgExpr.Emit(RHC.Expression,objx,context);
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
               
                Type returnType = HasClrType ? ClrType : typeof(object);
                InvokeMemberBinder binder = new ClojureInvokeMemberBinder(ClojureContext.Default,_methodName, paramExprs.Count, IsStaticCall);
                DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramExprs);
                Expression call = dyn;
                if ( context.DynInitHelper != null )
                    call = context.DynInitHelper.ReduceDyn(dyn);
                
                if ( returnType == typeof(void) )
                {
                    call = Expression.Block(call,Expression.Default(typeof(object)));
                    returnType = typeof(object);
                }
                call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);

                Type[] paramTypes = paramExprs.Map((x) => x.Type);
                MethodBuilder mbLambda = context.TB.DefineMethod("__interop_"+_methodName+RT.nextID(),MethodAttributes.Static|MethodAttributes.Public,CallingConventions.Standard,returnType,paramTypes);
                LambdaExpression lambda = Expression.Lambda(call,paramExprs);
                lambda.CompileToMethod(mbLambda);

                context.GetILGenerator().Emit(OpCodes.Call,mbLambda);
            }
        }
        #endregion

        internal static void EmitArgsAsArray(IPersistentVector restArgs, ObjExpr2 objx, GenContext context)
        {
            throw new NotImplementedException();
        }


        public static void EmitTypedArgs(ObjExpr2 objx, GenContext context, ParameterInfo[] parameterInfo, IPersistentVector iPersistentVector)
        {
            throw new NotImplementedException();
        }
    }
}
