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
using System.Linq;
using System.Text;
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

using System.IO;
using System.Dynamic;

namespace clojure.lang.CljCompiler.Ast
{
    class NewExpr : Expr
    {
        #region Data

        readonly List<HostArg> _args;
        readonly ConstructorInfo _ctor;
        readonly Type _type;
        bool _isNoArgValueTypeCtor = false;
        readonly IPersistentMap _spanMap;

        #endregion

        #region Ctors

        public NewExpr(Type type, List<HostArg> args, IPersistentMap spanMap)
        {
            _args = args;
            _type = type;
            _spanMap = spanMap;
            _ctor = ComputeCtor();
        }

        private ConstructorInfo ComputeCtor()
        {
            int numArgs = _args.Count;

            int numCtors;
            ConstructorInfo ctor = HostExpr.GetMatchingConstructor(_spanMap, _type, _args, out numCtors);

            if (numCtors == 0)
            {
                if (_type.IsValueType && numArgs == 0)
                {
                    // Value types have a default no-arg c-tor that is not picked up in the regular c-tors.
                    _isNoArgValueTypeCtor = true;
                    return null;
                }
                throw new InvalidOperationException(string.Format("No constructor in type: {0} with {1} arguments", _type.Name, numArgs));
            }

            if (ctor == null && RT.booleanCast(RT.WARN_ON_REFLECTION.deref()))
                ((TextWriter)RT.ERR.deref()).WriteLine("Reflection warning, line: {0}:{1} - call to {2} ctor can't be resolved.",
                    Compiler.SOURCE_PATH.deref(), _spanMap != null ? (int)_spanMap.valAt(RT.START_LINE_KEY, 0) : 0, _type.FullName);

            return ctor;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return _type; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext)
            {
                //int line = (int)Compiler.LINE.deref();

                ISeq form = (ISeq)frm;

                // form => (new Typename args ... )

                if (form.count() < 2)
                    throw new Exception("wrong number of arguments, expecting: (new Typename args ...)");

                Type t = Compiler.MaybeType(RT.second(form), false);
                if (t == null)
                    throw new ArgumentException("Unable to resolve classname: " + RT.second(form));

                List<HostArg> args = HostExpr.ParseArgs(RT.next(RT.next(form)));

                return new NewExpr(t, args, (IPersistentMap)Compiler.SOURCE_SPAN.deref());
            }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression call;

            if (_ctor != null)
                call = GenDlrForMethod(context);
            else if (_isNoArgValueTypeCtor)
            {
                call = Expression.Default(_type);
            }
            else
            {
                call = GenerateComplexCall(context);
                //Expression typeExpr = Expression.Call(Compiler.Method_RT_classForName, Expression.Constant(_type.FullName));
                //Expression args = Compiler.GenArgArray(context, _args);
                //// Java: emitClearLocals

                //call = Expression.Call(Compiler.Method_Reflector_InvokeConstructor, typeExpr, args);
            }

            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }

        // TODO: See if it is worth removing the code duplication with MethodExp.GenDlr.

        private Expression GenerateComplexCall(GenContext context)
        {
            Expression call;

            Expression target = GenTargetExpression(context);

            List<Expression> exprs = new List<Expression>(_args.Count);
            List<ParameterExpression> sbParams = new List<ParameterExpression>();
            List<Expression> sbInits = new List<Expression>();
            List<Expression> sbTransfers = new List<Expression>();
            MethodExpr.GenerateComplexArgList(context, _args, out exprs, out sbParams, out sbInits, out sbTransfers);

            Expression[] argExprs = DynUtils.ArrayInsert<Expression>(target, exprs);


            Type returnType = ClrType;

            CreateInstanceBinder binder = new DefaultCreateInstanceBinder(_args.Count);
            DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), argExprs);
            // I'd like to use returnType in place of typeof(object) in the previous, 
            // But I can't override ReturnType in DefaultCreateInstanceBinder and this causes an error.
            // Look for the conversion below.

          
            if (context.Mode == CompilerMode.File)
                call = context.DynInitHelper.ReduceDyn(dyn);
            else
                call = dyn;

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

        private Expression GenTargetExpression(GenContext context)
        {
            return Expression.Constant(_type, typeof(Type));
        }

        Expression GenDlrForMethod(GenContext context)
        {
            // The ctor is uniquely determined.

            Expression[] args = HostExpr.GenTypedArgs(context, _ctor.GetParameters(), _args);
            //return Expression.New(_ctor, args);

            return Utils.SimpleNewHelper(_ctor, args);

            // JAVA: emitClearLocals
        }


        #endregion
    }
}
