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
    public class LocalBindingExpr : Expr, MaybePrimitiveExpr, AssignableExpr
    {
        #region Data

        readonly LocalBinding _b;
        public LocalBinding Binding { get { return _b; } }

        readonly Symbol _tag;
        public Symbol Tag { get { return _tag; } }

        Type _cachedType;

        #endregion

        #region Ctors

        public LocalBindingExpr(LocalBinding b, Symbol tag)
        {
            if (b.PrimitiveType != null && tag != null)
                if (!b.PrimitiveType.Equals(Compiler.TagType(tag)))
                    throw new InvalidOperationException("Can't type hint a primitive local with a diffent type");
                else _tag = null;
            else _tag = tag;
            _b = b;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _tag != null || _b.HasClrType; }
        }

        public Type ClrType
        {
            get
            {
                if (_cachedType == null)
                    _cachedType = _tag != null ? HostExpr.TagToType(_tag) : _b.ClrType;
                return _cachedType;
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval locals");
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            if (rhc != RHC.Statement)
                objx.EmitLocal(ilg, _b);
        }

        public bool HasNormalExit() { return true; }

        public bool CanEmitPrimitive
        {
            get { return _b.PrimitiveType != null; }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            objx.EmitUnboxedLocal(ilg, _b);
        }

        #endregion

        #region AssignableExpr Members

        public Object EvalAssign(Expr val)
        {
            throw new InvalidOperationException("Can't eval locals");
        }

        public void EmitAssign(RHC rhc, ObjExpr objx, CljILGen ilg, Expr val)
        {
            objx.EmitAssignLocal(ilg, _b, val);
            if (rhc != RHC.Statement)
                objx.EmitLocal(ilg, _b);
        }

        #endregion
    }
}
