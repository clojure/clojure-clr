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
    public sealed class MethodParamExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly Type _t;
        public Type Type { get { return _t; } }

        #endregion

        #region C-tors

        public MethodParamExpr(Type t)
        {
            _t = t;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _t != null; }
        }

        public Type ClrType
        {
            get { return _t; }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval");
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            throw new InvalidOperationException("Can't emit");
        }

        public bool HasNormalExit() { return true; }

        public bool CanEmitPrimitive
        {
            get { return Util.IsPrimitive(_t); }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            throw new InvalidOperationException("Can't emit");
        }

        #endregion
    }
}
