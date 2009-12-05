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

using clojure.lang.CljCompiler.Ast;


namespace clojure.lang
{
    public static class GenDelegate
    {

        #region Data

        static GenContext _context = new GenContext("delegates", CompilerMode.Immediate);

        #endregion

        #region A little debugging aid

        static int _saveId = 0;
        public static void SaveProxyContext()
        {
            _context.AssyBldr.Save("delegates" + _saveId++ + ".dll");
            _context = new GenContext("delegates", CompilerMode.Immediate);
        }

        #endregion

        #region Factory method

        //public static Delegate Create(Type delegateType, IPersistentVector parms, ISeq body)
        //{
        //    return Create(delegateType,null,parms,body);
        //}
            

        //public static Delegate Create(Type delegateType, string optName, IPersistentVector parmVec, ISeq body)
        //{
        //    MethodInfo invokeMI = delegateType.GetMethod("Invoke");
        //    Type returnType = invokeMI.ReturnType;

        //    ParameterInfo[] delParams = invokeMI.GetParameters();

        //    bool delVariadic = (invokeMI.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs;
        //    bool handlerVariadic = false;

        //    if (delVariadic != handlerVariadic)
        //        throw new ArgumentException("Handler and delegate must both be variadic or both be non-variadic.");

        //    int delParamCount = delParams.Length;
        //    int handlerParamCount = handlerVariadic ? parmVec.length() - 1 : parmVec.length();

        //    if ( delParamCount != handlerParamCount )
        //        throw new ArgumentException("Wrong number of parameters to generate typed delegate");


        //    List<ParameterExpression> parms = new List<ParameterExpression>();
        //    List<Expression> callArgs = new List<Expression>();

        //    foreach (ParameterInfo pi in delParams) 
        //    {
        //        ParameterExpression pe = Expression.Parameter(pi.ParameterType, pi.Name);
        //        parms.Add(pe);
        //        callArgs.Add(Compiler.MaybeBox(pe));
        //    }

        //    ISeq form = RT.listStar(Compiler.FN, parmVec, body);           
        //    Expr formAst = Compiler.GenerateAST(form, false);
        //    Expression formExpr = Compiler.GenerateDlrExpression(_context, formAst);
        //    Expression finalExpr = Expression.Call(
        //        formExpr, 
        //        formExpr.Type.GetMethod("invoke", Compiler.CreateObjectTypeArray(parms.Count)),
        //        callArgs);

        //    LambdaExpression lambda = Expression.Lambda(delegateType, finalExpr, true, parms);

        //    return lambda.Compile();
        //}

        public static Delegate Create(Type delegateType, IFn fn)
        {
            MethodInfo invokeMI = delegateType.GetMethod("Invoke");
            Type returnType = invokeMI.ReturnType;

            ParameterInfo[] delParams = invokeMI.GetParameters();

            List<ParameterExpression> parms = new List<ParameterExpression>();
            List<Expression> callArgs = new List<Expression>();

            foreach (ParameterInfo pi in delParams)
            {
                ParameterExpression pe = Expression.Parameter(pi.ParameterType, pi.Name);
                parms.Add(pe);
                callArgs.Add(Compiler.MaybeBox(pe));
            }
            Expression body = Expression.Call(Expression.Constant(fn),Compiler.Methods_IFn_invoke[parms.Count], callArgs);

            LambdaExpression lambda = Expression.Lambda(delegateType, body, true, parms);

            return lambda.Compile();
        }


        #endregion

    }
}
