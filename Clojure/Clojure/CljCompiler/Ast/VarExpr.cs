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
    public class VarExpr : Expr, AssignableExpr
    {
        #region Data

        readonly Var _var;
        public Var Var { get { return _var; } } 

        readonly object _tag;
        public object Tag { get { return _tag; } }

        Type _cachedType;  

        #endregion

        #region Ctors

        public VarExpr(Var var, Symbol tag)
        {
            _var = var;
            _tag = tag ?? var.Tag;
        }


        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _tag != null; }
        }

        public Type ClrType
        {
            get
            {
                if (_cachedType == null)
                    _cachedType = HostExpr.TagToType(_tag);
                return _cachedType;
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            return _var.deref();
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            objx.EmitVarValue(ilg, _var);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion

        #region AssignableExpr Members

        public object EvalAssign(Expr val)
        {
            return _var.set(val.Eval());
        }

        public void EmitAssign(RHC rhc, ObjExpr objx, CljILGen ilg, Expr val)
        {
            objx.EmitVar(ilg, _var);
            val.Emit(RHC.Expression, objx, ilg);
            ilg.Emit(OpCodes.Call, Compiler.Method_Var_set);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion
    }
}
