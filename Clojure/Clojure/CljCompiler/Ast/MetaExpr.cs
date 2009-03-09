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
    class MetaExpr : Expr
    {
        #region Data

        readonly Expr _expr;
        readonly MapExpr _meta;

        #endregion

        #region Ctors

        public MetaExpr(Expr expr, MapExpr meta)
        {
            _expr = expr;
            _meta = meta;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _expr.HasClrType; }
        }

        public override Type ClrType
        {
            get { return _expr.ClrType; }
        }

        #endregion
    }
}
