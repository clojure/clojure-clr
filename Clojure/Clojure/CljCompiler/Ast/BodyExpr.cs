/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang.CljCompiler.Ast
{
    class BodyExpr : Expr
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

        public override bool HasClrType
        {
            get { return LastExpr.HasClrType; }
        }

        public override Type ClrType
        {
            get { return LastExpr.ClrType; }
        }

        #endregion

        public sealed class Parser : IParser
        {
            public Expr Parse(object frms)
            {
                ISeq forms = (ISeq)frms;

                if (Util.equals(RT.first(forms), Compiler.DO))
                    forms = RT.next(forms);

                IPersistentVector exprs = PersistentVector.EMPTY;

                for (ISeq s = forms; s != null; s = s.next())
                {
                    if (s.next() == null)
                    {
                        // in tail recurive position
                        try
                        {
                            Var.pushThreadBindings(PersistentHashMap.create(Compiler.IN_TAIL_POSITION, RT.T));
                            exprs = exprs.cons(Compiler.GenerateAST(s.first()));
                        }
                        finally
                        {
                            Var.popThreadBindings();
                        }
                    }
                    else
                        exprs = exprs.cons(Compiler.GenerateAST(s.first()));
                }
                if (exprs.count() == 0)
                    exprs = exprs.cons(Compiler.NIL_EXPR);

                return new BodyExpr(exprs);

            }
        }

    }
}
