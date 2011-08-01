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
    class TheVarExpr : Expr
    {
        #region Data

        readonly Var _var;

        #endregion

        #region Ctors

        public TheVarExpr(Var var)
        {
            _var = var;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return typeof(Var); }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object form)
            {
                Symbol sym = (Symbol)RT.second(form);
                Var v = Compiler.LookupVar(sym, false);
                if (v != null)
                    return new TheVarExpr(v);
                throw new ParseException(string.Format("Unable to resolve var: {0} in this context", sym));
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

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return objx.GenVar(context,_var);
        }

        #endregion
    }
}
