/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class TheVarExpr : Expr
    {
        #region Data

        readonly Var _var;
        public Var Var => _var;

        #endregion

        #region Ctors

        public TheVarExpr(Var var)
        {
            _var = var;
        }

        #endregion

        #region Type mangling

        public bool HasClrType => true;

        public Type ClrType => typeof(Var);

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object form)
            {
                Symbol sym = (Symbol)RT.second(form);
                Var v = Compiler.LookupVar(sym, false);
                if (v is not null)
                    return new TheVarExpr(v);
                throw new ParseException($"Unable to resolve var: {sym} in this context");
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            return _var;
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            objx.EmitVar(ilg, _var);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
