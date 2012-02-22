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
using Microsoft.Scripting.Generation;
using System.Reflection.Emit;


namespace clojure.lang.CljCompiler.Ast
{
    class MetaExpr : Expr
    {
        #region Data

        readonly Expr _expr;
        readonly Expr _meta;

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

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression objExpr = _expr.GenCode(RHC.Expression, objx, context);
            Expression iobjExpr = Expression.Convert(objExpr, typeof(IObj));

            Expression metaExpr = _meta.GenCode(RHC.Expression, objx, context);
            metaExpr = Expression.Convert(metaExpr, typeof(IPersistentMap));
    
            Expression ret = Expression.Call(iobjExpr, Compiler.Method_IObj_withMeta, metaExpr);

            return ret;
        }

        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGen ilg = context.GetILGen();

            _expr.Emit(RHC.Expression, objx, context);
            ilg.Emit(OpCodes.Castclass, typeof(IObj));
            _meta.Emit(RHC.Expression, objx, context);
            ilg.Emit(OpCodes.Castclass, typeof(IPersistentMap));
            ilg.EmitCall(Compiler.Method_IObj_withMeta);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion
    }
}
