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
    class TryExpr : Expr
    {
       #region Data

        readonly Expr _tryExpr;
        readonly Expr _finallyExpr;
        readonly PersistentVector _catchExprs;
        readonly int _retLocal;
        readonly int _finallyLocal;

        #endregion

        #region Ctors

        public TryExpr(Expr tryExpr, PersistentVector catchExprs, Expr finallyExpr, int retLocal, int finallyLocal)
        {
            _tryExpr = tryExpr;
            _catchExprs = catchExprs;
            _finallyExpr = finallyExpr;
            _retLocal = retLocal;
            _finallyLocal = finallyLocal;

        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tryExpr.HasClrType; }
        }

        public override Type ClrType
        {
            get { return _tryExpr.ClrType; }
        }

        #endregion

        public sealed class Parser : IParser
        {
            public Expr Parse(object form)
            {
                throw new NotImplementedException();
            }
        }
    }
}
