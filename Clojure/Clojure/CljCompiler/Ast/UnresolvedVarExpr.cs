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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif


namespace clojure.lang.CljCompiler.Ast
{
    class UnresolvedVarExpr : Expr
    {
        #region Data

        readonly Symbol _symbol;

        #endregion

        #region Ctors

        public UnresolvedVarExpr(Symbol symbol)
        {
            _symbol = symbol;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return false; }
        }

        public Type ClrType
        {
            get { throw new InvalidOperationException("UnresolvedVarExpr has no CLR type");  }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new ArgumentException("UnresolvedVarExpr cannot be evalled");
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return Expression.Empty();
        }

        #endregion
    }
}
