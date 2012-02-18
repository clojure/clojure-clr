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
    class LocalBindingExpr : Expr, MaybePrimitiveExpr, AssignableExpr
    {
        #region Data

        readonly LocalBinding _b;

        internal LocalBinding Binding
        {
            get { return _b; }
        } 

        readonly Symbol _tag;

        #endregion

        #region Ctors

        public LocalBindingExpr(LocalBinding b, Symbol tag)
        {
            if (b.PrimitiveType != null && _tag != null)
                throw new InvalidOperationException("Can't type hint a primitive local");
            _b = b;
            _tag = tag;
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
                if (_tag != null)
                    return HostExpr.TagToType(_tag);
                return _b.ClrType;
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

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return objx.GenLocal(context,_b);
        }


        public void Emit(RHC rhc, ObjExpr2 objx, GenContext context)
        {
            if (rhc != RHC.Statement)
                objx.EmitLocal(context, _b);
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return _b.PrimitiveType != null; }
        }

        public Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            return objx.GenUnboxedLocal(context, _b);
        }

        public void EmitUnboxed(RHC rhc, ObjExpr2 objx, GenContext context)
        {
            objx.EmitUnboxedLocal(context, _b);
        }


        #endregion

        #region AssignableExpr Members

        public Object EvalAssign(Expr val)
        {
            throw new InvalidOperationException("Can't eval locals");
        }

        public Expression GenAssign(RHC rhc, ObjExpr objx, GenContext context, Expr val)
        {
            return Expression.Block(
                objx.GenAssignLocal(context,_b,val),
                objx.GenLocal(context,_b));
        }

        public void EmitAssign(RHC rhc, ObjExpr2 objx, GenContext context, Expr val)
        {
            objx.EmitAssignLocal(context, _b, val);
            if (rhc != RHC.Statement)
                objx.EmitLocal(context, _b);
        }


        #endregion
    }
}
