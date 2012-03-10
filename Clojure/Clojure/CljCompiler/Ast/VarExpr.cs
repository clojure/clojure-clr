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
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    class VarExpr : Expr, AssignableExpr
    {
        #region Data

        readonly Var _var;
        public Var Var
        {
            get { return _var; }
        } 

        readonly object _tag;

        public object Tag
        {
            get { return _tag; }
        } 

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
            get { return HostExpr.TagToType(_tag); }
        }

        #endregion

        #region eval

        public object Eval()
        {
            return _var.deref();
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return objx.GenVarValue(context, _var);
        }

        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            objx.EmitVarValue(context, _var);
            if (rhc == RHC.Statement)
                context.GetILGenerator().Emit(OpCodes.Pop);
        }

        public bool HasThrowLast() { return false; }


        #endregion

        #region AssignableExpr Members

        public Expression GenAssign(RHC rhc, ObjExpr objx, GenContext context, Expr val)
        {
            // RETYPE: Get rid of Box?
            Expression varExpr = objx.GenVar(context, _var);
            Expression valExpr = val.GenCode(RHC.Expression,objx,context);
            return Expression.Call(varExpr, Compiler.Method_Var_set, Compiler.MaybeBox(valExpr));
        }

        public object EvalAssign(Expr val)
        {
            return _var.set(val.Eval());
        }


        public void EmitAssign(RHC rhc, ObjExpr objx, GenContext context, Expr val)
        {
            ILGenerator ilg = context.GetILGenerator();

            objx.EmitVar(context, _var);
            val.Emit(RHC.Expression, objx, context);
            ilg.Emit(OpCodes.Call, Compiler.Method_Var_set);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion
    }
}
