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
    sealed class InstanceOfExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly Expr _expr;
        readonly Type _t;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        readonly string _source;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        readonly IPersistentMap _spanMap;

        #endregion

        #region C-tors

        public InstanceOfExpr(string source, IPersistentMap spanMap, Type t, Expr expr)
        {
            _source = source;
            _spanMap = spanMap;
            _t = t;
            _expr = expr;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return typeof(bool); }
        }

        #endregion

        #region eval

        public object Eval()
        {
            return _t.IsInstanceOfType(_expr.Eval());
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return HostExpr.GenBoxReturn(GenCodeUnboxed(RHC.Expression, objx, context), typeof(bool), objx, context);
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return true; }
        }

        public Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            return Expression.TypeIs(_expr.GenCode(RHC.Expression, objx, context), _t); ;
        }

        #endregion
    }
}
