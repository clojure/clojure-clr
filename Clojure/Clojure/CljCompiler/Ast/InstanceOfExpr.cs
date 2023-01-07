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
    public sealed class InstanceOfExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly Expr _expr;
        public Expr Expr { get { return _expr; } }

        readonly Type _t;
        public Type Type { get { return _t; } }

        readonly string _source;
        public string Source { get { return _source; } }

        readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }

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

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            EmitUnboxed(rhc, objx, ilg);
            HostExpr.EmitBoxReturn(objx, ilg, typeof(bool));
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        public bool CanEmitPrimitive
        {
            get { return true; }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            _expr.Emit(RHC.Expression, objx, ilg);

            ilg.Emit(OpCodes.Isinst, _t);
            ilg.Emit(OpCodes.Ldnull);
            ilg.Emit(OpCodes.Cgt_Un);
        }

        #endregion
    }
}
