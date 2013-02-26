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
    class ThrowExpr : UntypedExpr
    {
        #region Data

        readonly Expr _excExpr;

        #endregion

        #region Ctors

        public ThrowExpr(Expr excExpr)
        {
            _excExpr = excExpr;
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object form)
            {
                if (pcon.Rhc == RHC.Eval)
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FnOnceSym, PersistentVector.EMPTY, form)), "throw__" + RT.nextID());

                return new ThrowExpr(Compiler.Analyze(pcon.SetRhc(RHC.Expression).SetAssign(false), RT.second(form)));
            }
        }

        #endregion

        #region eval

        public override object Eval()
        {
            throw new InvalidOperationException("Can't eval throw");
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            _excExpr.Emit(RHC.Expression, objx, ilg);
            ilg.Emit(OpCodes.Castclass, typeof(Exception));
            ilg.Emit(OpCodes.Throw);
        }

        public override bool HasNormalExit() { return false; }

        #endregion
    }
}
