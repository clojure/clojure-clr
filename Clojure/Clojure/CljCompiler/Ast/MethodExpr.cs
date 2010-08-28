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
        protected MethodInfo _method;
        protected readonly string _source;
        protected readonly IPersistentMap _spanMap;
        protected readonly Symbol _tag;

        #endregion

        #region C-tors

        protected MethodExpr(string source, IPersistentMap spanMap, Symbol tag, string methodName, List<HostArg> args)
        {
            _source = source;
            _spanMap = spanMap;
            _methodName = methodName;
            _args = args;
            _tag = tag;
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression call;

            if (_method != null)
                call = GenDlrForMethod(context);
            else
                call = GenerateComplexCall(context);
            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;

        }

        public override Expression GenDlrUnboxed(GenContext context)
        {
            if (_method != null)
            {
                Expression call = GenDlrForMethod(context);
                call = Compiler.MaybeAddDebugInfo(call, _spanMap);
                return call;
            }
            else
                throw new InvalidOperationException("Unboxed emit of unknown member.");
        }


        protected Expression GenDlrForMethod(GenContext context)
        {
            if (_method.DeclaringType == (Type)Compiler.COMPILE_STUB_ORIG_CLASS.deref())
                _method = FindEquivalentMethod(_method, context.ObjExpr.BaseType);            
            
            int argCount = _args.Count;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(argCount + (IsStaticCall ? 0 : 1));
            if (!IsStaticCall)
                argsPlus.Add(new DynamicMetaObject(GenTargetExpression(context), BindingRestrictions.Empty));

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
                        argsPlus.Add(new DynamicMetaObject(Expression.Convert(GenTypedArg(context, argType, e), methodParms[i].ParameterType.GetElementType()), BindingRestrictions.Empty));
                        break;
                    case HostArg.ParameterType.Standard:
                        t = argType;
                        argsPlus.Add(new DynamicMetaObject(Expression.Convert(GenTypedArg(context, argType, e), methodParms[i].ParameterType), BindingRestrictions.Empty));

                        break;
                    default:
                        throw Util.UnreachableCode();
                }
                // TODO: Rethink how we are getting typing done.
                //argsPlus.Add(new DynamicMetaObject(Expression.Convert(GenTypedArg(context, argType, e), argType), BindingRestrictions.Empty));
                //argsPlus.Add(new DynamicMetaObject(GenTypedArg(context, argType, e), BindingRestrictions.Empty));
            }

            OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
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

        private MethodInfo FindEquivalentMethod(MethodInfo _method, Type baseType)
        {
            BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic;

            if (IsStaticCall)
                flags |= BindingFlags.Static;
            else
                flags |= BindingFlags.Instance;
          
            return baseType.GetMethod(_method.Name,flags, null, Compiler.GetTypes(_method.GetParameters()), null);
        }


        private Expression GenerateComplexCall(GenContext context)
        {
            Expression call;

            Expression target = GenTargetExpression(context);

            List<Expression> exprs = new List<Expression>(_args.Count);
            List<ParameterExpression> sbParams = new List<ParameterExpression>();
            List<Expression> sbInits = new List<Expression>();
            List<Expression> sbTransfers = new List<Expression>();
            GenerateComplexArgList(context, _args, out exprs, out sbParams, out sbInits, out sbTransfers);

            Expression[] argExprs = DynUtils.ArrayInsert<Expression>(target, exprs);

            Type returnType = HasClrType ? ClrType : typeof(object);

            InvokeMemberBinder binder = new DefaultInvokeMemberBinder(_methodName, argExprs.Length, IsStaticCall);
            DynamicExpression dyn = Expression.Dynamic(binder, returnType, argExprs);

            //if (context.Mode == CompilerMode.File)
            if ( context.DynInitHelper != null )
                call = context.DynInitHelper.ReduceDyn(dyn);
            else
                call = dyn;

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
                        argExprs.Add(e.GenDlr(context));
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }
        }

        protected abstract bool IsStaticCall { get; }
        protected abstract Expression GenTargetExpression(GenContext context);
        //protected abstract Expression GenDlrForMethod(GenContext context);

        public override bool CanEmitPrimitive
        {
            get { return _method != null && Util.IsPrimitive(_method.ReturnType); }
        }

        #endregion
    }
}
