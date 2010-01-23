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

extern alias MSC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Dynamic;
using System.Reflection;

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

            if (context.Mode == CompilerMode.File)
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
                    case HostArg.ParameterType.Ref:
                    case HostArg.ParameterType.Out:
                        {
                            Type sbType = typeof(MSC::System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
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
        protected abstract Expression GenDlrForMethod(GenContext context);

        public override bool CanEmitPrimitive
        {
            get { return _method != null && Util.IsPrimitive(_method.ReturnType); }
        }

        #endregion
    }
}
