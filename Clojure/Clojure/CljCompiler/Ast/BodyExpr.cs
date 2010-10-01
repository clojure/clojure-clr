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
    class BodyExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly IPersistentVector _exprs;

        Expr LastExpr
        {
            get
            {
                return (Expr)_exprs.nth(_exprs.count() - 1);
            }
        }

        #endregion

        #region Ctors

        public BodyExpr(IPersistentVector exprs)
        {
            _exprs = exprs;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return LastExpr.HasClrType; }
        }

        public Type ClrType
        {
            get { return LastExpr.ClrType; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frms)
            {
                ISeq forms = (ISeq)frms;

                if (Util.equals(RT.first(forms), Compiler.DO))
                    forms = RT.next(forms);

                IPersistentVector exprs = PersistentVector.EMPTY;

                for (; forms != null; forms = forms.next())
                {
                    Expr e = (pcon.Rhc != RHC.Eval && (pcon.Rhc == RHC.Statement || forms.next() != null))
                        ? Compiler.Analyze(pcon.SetRhc(RHC.Statement), forms.first())
                        : Compiler.Analyze(pcon, forms.first());
                    exprs = exprs.cons(e);
                }
                if (exprs.count() == 0)
                    exprs = exprs.cons(Compiler.NIL_EXPR);

                return new BodyExpr(exprs);
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            object ret = null;
            for ( int i=0; i<_exprs.count(); i++ )
                ret = ((Expr)_exprs.nth(i)).Eval();

            return ret;
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            List<Expression> exprs = new List<Expression>(_exprs.count());

            for (int i = 0; i < _exprs.count()-1; i++)
            {
                Expr e = (Expr)_exprs.nth(i);
                exprs.Add(e.GenCode(RHC.Statement,objx,context));
            }
            exprs.Add(LastExpr.GenCode(rhc, objx, context));

            return Expression.Block(exprs);
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return LastExpr is MaybePrimitiveExpr && ((MaybePrimitiveExpr)LastExpr).CanEmitPrimitive; }
        }

        public Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            List<Expression> exprs = new List<Expression>(_exprs.count());

            for (int i = 0; i < _exprs.count()-1; i++)
            {
                Expr e = (Expr)_exprs.nth(i);
                exprs.Add(e.GenCode(RHC.Statement,objx,context));
            }

            MaybePrimitiveExpr last = (MaybePrimitiveExpr)LastExpr;
            exprs.Add(last.GenCodeUnboxed(rhc,objx,context));

            return Expression.Block(exprs);
        }

        #endregion
    }
}
