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

namespace clojure.lang.CljCompiler.Ast
{
    public class BodyExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly IPersistentVector _exprs;
        public IPersistentVector Exprs { get { return _exprs; } }

        public Expr LastExpr
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

                if (Util.equals(RT.first(forms), Compiler.DoSym))
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
                    exprs = exprs.cons(Compiler.NilExprInstance);

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

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            for (int i = 0; i < _exprs.count() - 1; i++)
            {
                Expr e = (Expr)_exprs.nth(i);
                e.Emit(RHC.Statement, objx, ilg);
            }
            LastExpr.Emit(rhc, objx, ilg);
        }

        public bool CanEmitPrimitive
        {
            get { return LastExpr is MaybePrimitiveExpr expr && expr.CanEmitPrimitive; }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            for (int i = 0; i < _exprs.count() - 1; i++)
            {
                Expr e = (Expr)_exprs.nth(i);
                e.Emit(RHC.Statement, objx, ilg);
            }
            MaybePrimitiveExpr mbe = (MaybePrimitiveExpr)LastExpr;
            mbe.EmitUnboxed(rhc, objx, ilg);
        }

        public bool HasNormalExit() { return LastExpr.HasNormalExit(); }

        #endregion
    }
}
