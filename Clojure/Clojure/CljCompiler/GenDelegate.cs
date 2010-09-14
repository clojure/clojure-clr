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

        //static GenContext _context = new GenContext("delegates", CompilerMode.Immediate);
        static GenContext _context = new GenContext("delegates", false);

        #endregion

        #region A little debugging aid

        //static int _saveId = 0;
        public static void SaveProxyContext()
        {
            _context.SaveAssembly();
            //_context = new GenContext("delegates", CompilerMode.Immediate);
            _context = new GenContext("delegates", false);
        }

        #endregion

        #region Factory method

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

            Expression call =                    
                Expression.Call(
                    Expression.Constant(fn),
                    Compiler.Methods_IFn_invoke[parms.Count], 
                    callArgs);

            Expression body =  returnType == typeof(void)
                ? (Expression)Expression.Block(call,Expression.Default(typeof(void)))
                : (Expression)Expression.Convert(call, returnType);

            LambdaExpression lambda = Expression.Lambda(delegateType, body, true, parms);

            return lambda.Compile();
        }


        #endregion
    }
}
