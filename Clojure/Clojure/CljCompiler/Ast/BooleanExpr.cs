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
    class BooleanExpr : LiteralExpr  // , MaybePrimitiveExpr  TODO: No reason this shouldn't be, but it messes up the RecurExpr emit code.
    {
        #region Data

        readonly bool _val;

        public override object Val
        {
            get { return _val; }
        }

        #endregion

        #region C-tors

        public BooleanExpr(bool val)
        {
            _val = val;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return typeof(Boolean); }
        }

        #endregion

        #region Code generation

        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return Expression.Constant(_val);
        }

        public override void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGen ilg = context.GetILGen();
            ilg.EmitBoolean(_val);
            ilg.Emit(OpCodes.Box,typeof(bool));
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion

        #region MaybePrimitiveExpr members  - deleted (for now)

        //public bool CanEmitPrimitive
        //{
        //    get { return true; }
        //}

        //public void EmitUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        //{
        //    ILGenerator ilg = context.GetILGenerator();
        //    if (_val)
        //        ilg.Emit(OpCodes.Ldc_I4_1);
        //    else
        //        ilg.Emit(OpCodes.Ldc_I4_0);
        //}

        #endregion
    }
}
