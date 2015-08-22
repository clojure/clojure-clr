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
    public class MonitorExitExpr : UntypedExpr
    {
        #region Data

        readonly Expr _target;
        public Expr Target { get { return _target; } }

        #endregion

        #region Ctors

        public MonitorExitExpr(Expr target)
        {
            _target = target;
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object form)
            {
                return new MonitorExitExpr(Compiler.Analyze(pcon.SetRhc(RHC.Expression),RT.second(form)));
            }
        }

        #endregion

        #region eval

        public override object Eval()
        {
            throw new InvalidOperationException("Can't eval monitor-exit");
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            _target.Emit(RHC.Expression, objx, ilg);
            ilg.EmitCall(Compiler.Method_Monitor_Exit);
            Compiler.NilExprInstance.Emit(rhc, objx, ilg);
        }

        #endregion
    }
}
