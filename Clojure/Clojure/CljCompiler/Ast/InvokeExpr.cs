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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using AstUtils = Microsoft.Scripting.Ast.Utils;
using Microsoft.Scripting;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    class InvokeExpr : Expr
    {
        #region Data

        readonly Expr _fexpr;
        readonly Object _tag;
        readonly IPersistentVector _args;
        readonly string _source;
        readonly IPersistentMap _spanMap;

        #endregion

        #region Ctors

        public InvokeExpr(string source, IPersistentMap spanMap, Symbol tag, Expr fexpr, IPersistentVector args)
        {
            _source = source;
            _spanMap = spanMap;
            _fexpr = fexpr;
            _args = args;
            _tag = tag ?? (fexpr is VarExpr ? ((VarExpr)fexpr).Tag : null);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tag != null; }
        }

        public override Type ClrType
        {
            get { return Compiler.TagToType(_tag); }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ISeq form)
        {
            Expr fexpr = Compiler.GenerateAST(form.first(),false);
            IPersistentVector args = PersistentVector.EMPTY;
            for ( ISeq s = RT.seq(form.next()); s != null; s = s.next())
                args = args.cons(Compiler.GenerateAST(s.first(),false));
            return new InvokeExpr((string)Compiler.SOURCE.deref(),
                Compiler.GetSourceSpanMap(form),
                Compiler.TagOf(form),
                fexpr,
                args);
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression fn = _fexpr.GenDlr(context);
            fn = Expression.Convert(fn, typeof(IFn));

            int argCount = _args.count();

            Expression[] args = new Expression[argCount];

            for (int i = 0; i < argCount; i++ )
                args[i] = Compiler.MaybeBox(((Expr)_args.nth(i)).GenDlr(context));

            Expression call = GenerateInvocation(fn, args);
            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }

        private static Expression GenerateInvocation(Expression fn, Expression[] args)
        {
            MethodInfo mi;
            Expression[] actualArgs;

            if (args.Length <= Compiler.MAX_POSITIONAL_ARITY)
            {
                mi = Compiler.Methods_IFn_invoke[args.Length];
                actualArgs = args;
            }
            else
            {
                // pick up the extended version.
                mi = Compiler.Methods_IFn_invoke[Compiler.MAX_POSITIONAL_ARITY + 1];
                Expression[] leftoverArgs = new Expression[args.Length - Compiler.MAX_POSITIONAL_ARITY];
                Array.Copy(args, Compiler.MAX_POSITIONAL_ARITY, leftoverArgs, 0, args.Length - Compiler.MAX_POSITIONAL_ARITY);

                Expression restArg = Expression.NewArrayInit(typeof(object), leftoverArgs);

                actualArgs = new Expression[Compiler.MAX_POSITIONAL_ARITY + 1];
                Array.Copy(args, 0, actualArgs, 0, Compiler.MAX_POSITIONAL_ARITY);
                actualArgs[Compiler.MAX_POSITIONAL_ARITY] = restArg;
            }

            Expression call = Expression.Call(fn, mi, actualArgs);

            return call;
        }

        #endregion
    }
}
