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
using System.Reflection.Emit;


namespace clojure.lang.CljCompiler.Ast
{
    public class MetaExpr : Expr
    {
        #region Data

        readonly Expr _expr;
        public Expr Expr { get { return _expr; } }

        readonly Expr _meta;
        public Expr Meta { get { return _meta; } }

        #endregion

        #region Ctors

        public MetaExpr(Expr expr, Expr meta)
        {
            _expr = expr;
            _meta = meta;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _expr.HasClrType; }
        }

        public Type ClrType
        {
            get { return _expr.ClrType; }
        }

        #endregion

        #region eval

        public object Eval()
        {
            return ((IObj)_expr.Eval()).withMeta((IPersistentMap)_meta.Eval());
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            _expr.Emit(RHC.Expression, objx, ilg);
            ilg.Emit(OpCodes.Castclass, typeof(IObj));
            _meta.Emit(RHC.Expression, objx, ilg);
            ilg.Emit(OpCodes.Castclass, typeof(IPersistentMap));
            ilg.EmitCall(Compiler.Method_IObj_withMeta);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
