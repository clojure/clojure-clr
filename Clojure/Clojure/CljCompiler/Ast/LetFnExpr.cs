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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif


namespace clojure.lang.CljCompiler.Ast
{
    class LetFnExpr : Expr
    {
        #region Data

        readonly IPersistentVector _bindingInits;
        readonly Expr _body;

        #endregion

        #region Ctors

        public LetFnExpr(IPersistentVector bindingInits, Expr body)
        {
            _bindingInits = bindingInits;
            _body = body;

        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _body.HasClrType; }
        }

        public Type ClrType
        {
            get { return _body.ClrType; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq)frm;

                // form => (letfn*  [var1 (fn [args] body) ... ] body ... )

                IPersistentVector bindings = RT.second(form) as IPersistentVector;

                if (bindings == null)
                    throw new ArgumentException("Bad binding form, expected vector");

                if ((bindings.count() % 2) != 0)
                    throw new ArgumentException("Bad binding form, expected matched symbol/expression pairs.");

                ISeq body = RT.next(RT.next(form));

                if (pcon.Rhc == RHC.Eval)
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FnSym, PersistentVector.EMPTY, form)), "letfn__" + RT.nextID());

                IPersistentMap dynamicBindings = RT.map(
                    Compiler.LocalEnvVar, Compiler.LocalEnvVar.deref(),
                    Compiler.NextLocalNumVar, Compiler.NextLocalNumVar.deref());

                try
                {
                    Var.pushThreadBindings(dynamicBindings);

                    // pre-seed env (like Lisp labels)
                    IPersistentVector lbs = PersistentVector.EMPTY;
                    for (int i = 0; i < bindings.count(); i += 2)
                    {
                        if (!(bindings.nth(i) is Symbol))
                            throw new ArgumentException("Bad binding form, expected symbol, got " + bindings.nth(i));

                        Symbol sym = (Symbol)bindings.nth(i);
                        if (sym.Namespace != null)
                            throw new Exception("Can't let qualified name: " + sym);

                        LocalBinding b = Compiler.RegisterLocal(sym, Compiler.TagOf(sym), null,false);
                        // b.CanBeCleared = false;
                        lbs = lbs.cons(b);
                    }

                    IPersistentVector bindingInits = PersistentVector.EMPTY;
                    for (int i = 0; i < bindings.count(); i += 2)
                    {
                        Symbol sym = (Symbol)bindings.nth(i);
                        Expr init = Compiler.Analyze(pcon.SetRhc(RHC.Expression),bindings.nth(i + 1));
                        LocalBinding b = (LocalBinding)lbs.nth(i / 2);
                        b.Init = init;
                        BindingInit bi = new BindingInit(b, init);
                        bindingInits = bindingInits.cons(bi);
                    }

                    return new LetFnExpr(bindingInits,new BodyExpr.Parser().Parse(pcon, body));
                }
                finally
                {
                    Var.popThreadBindings();
                }
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval letfns");
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            int n = _bindingInits.count();
            List<ParameterExpression> parms = new List<ParameterExpression>(n);
            List<Expression> forms = new List<Expression>(n);

            /// First, set up the environment.
            for (int i = 0; i < n; i++)
            {
                BindingInit bi = (BindingInit)_bindingInits.nth(i);
                ParameterExpression parmExpr = Expression.Parameter(typeof(IFn), bi.Binding.Name);
                bi.Binding.ParamExpression = parmExpr;
                parms.Add(parmExpr);
            }

            // Then initialize
            for (int i = 0; i < n; i++)
            {
                BindingInit bi = (BindingInit)_bindingInits.nth(i);
                ParameterExpression parmExpr = (ParameterExpression)bi.Binding.ParamExpression;
                forms.Add(Expression.Assign(parmExpr, bi.Init.GenCode(RHC.Expression,objx,context)));
            }

            // The work
            forms.Add(_body.GenCode(rhc,objx,context));
            return Expression.Block(parms,forms);
        }

        #endregion
    }
}
