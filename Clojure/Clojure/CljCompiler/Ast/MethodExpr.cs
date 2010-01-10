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
using System.Dynamic;
using System.Reflection;
#else
using System.Linq.Expressions;
#endif


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
            {
                int argCount = _args.Count;
                Expression[] argExprs = new Expression[argCount + 1];

                Expression target = GenTargetExpression(context);
                argExprs[0] = target;

                List<ParameterExpression> sbParams = new List<ParameterExpression>();
                List<Expression> sbInits = new List<Expression>();
                List<Expression> sbTransfers = new List<Expression>();

                BindingFlags cflags = BindingFlags.Public | BindingFlags.Instance;

                for (int i = 0; i < argCount; i++)
                {
                    HostArg ha = _args[i];

                    Expr e = ha.ArgExpr;
                    Type argType = e.HasClrType ? (e.ClrType ?? typeof(Object)) : typeof(Object);

                    switch (ha.ParamType)
                    {
                        case ParameterType.Ref:
                        case ParameterType.Out:
                            {
                                Type sbType = typeof(MSC::System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
                                ParameterExpression sbParam = Expression.Parameter(sbType, String.Format("__sb_{0}", i));
                                ConstructorInfo[] cinfos = sbType.GetConstructors();
                                Expression sbInit1 =
                                    Expression.Assign(
                                        sbParam,
                                        Expression.New(
                                            sbType.GetConstructor(cflags, null, new Type[] { argType }, null),
                                            ha.LocalBinding.ParamExpression));
                                Expression sbXfer = Expression.Assign(ha.LocalBinding.ParamExpression, Expression.Field(sbParam, "Value"));
                                sbParams.Add(sbParam);
                                sbInits.Add(sbInit1);
                                sbTransfers.Add(sbXfer);
                                argExprs[i + 1] = sbParam;
                            }
                            break;
                        case ParameterType.Standard:
                            argExprs[i + 1] = e.GenDlr(context);
                            break;

                        default:
                            throw Util.UnreachableCode();
                    }
                }

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
            }
            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;

        }

        protected abstract bool IsStaticCall { get; }
        protected abstract Expression GenTargetExpression(GenContext context);
        protected abstract Expression GenDlrForMethod(GenContext context);

        #endregion
    }
}
