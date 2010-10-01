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
    class AssignExpr : Expr
    {
        #region Data

        readonly AssignableExpr _target;
        readonly Expr _val;

        #endregion

        #region Ctors

        public AssignExpr(AssignableExpr target, Expr val)
        {
            _target = target;
            _val = val;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _val.HasClrType; }
        }

        public Type ClrType
        {
            get { return _val.ClrType; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq)frm;
                if (RT.Length(form) != 3)
                    throw new ArgumentException("Malformed assignment, expecting (set! target val)");
                Expr target = Compiler.Analyze(new ParserContext(RHC.Expression, true), RT.second(form));
                if (!(target is AssignableExpr))
                    throw new ArgumentException("Invalid assignment target");
                return new AssignExpr((AssignableExpr)target,
                    Compiler.Analyze(pcon.SetRhc(RHC.Expression),RT.third(form)));
            }
        }

        #endregion

        #region Eval

        public object Eval()
        {
            return _target.EvalAssign(_val);
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return _target.GenAssign(rhc,objx,context,_val);
        }

        #endregion
    }
}
