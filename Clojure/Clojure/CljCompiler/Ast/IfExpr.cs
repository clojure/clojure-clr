/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang.CljCompiler.Ast
{
    class IfExpr : Expr
    {
        #region Data

        readonly Expr _testExpr;
        readonly Expr _thenExpr;
        readonly Expr _elseExpr;

        #endregion

        #region Ctors

        public IfExpr(Expr testExpr, Expr thenExpr, Expr elseExpr)
        {
            _testExpr = testExpr;
            _thenExpr = thenExpr;
            _elseExpr = elseExpr;
        }

        #endregion

        #region Type mangling
        
        public override bool HasClrType
        {
            get {
                return _thenExpr.HasClrType
                    && _elseExpr.HasClrType
                    && (_thenExpr.HasClrType == _elseExpr.HasClrType
                        || _thenExpr.ClrType == null
                        || _elseExpr.ClrType == null);
            }
        }

        public override Type ClrType
        {
            get {
                Type thenType = _thenExpr.ClrType;
                return thenType ?? _elseExpr.ClrType;
            }
        }

        #endregion

        public sealed class Parser : IParser
        {
            public Expr Parse(object frm)
            {
                ISeq form = (ISeq)frm;

                // (if test then) or (if test then else)

                if (form.count() > 4)
                    throw new Exception("Too many arguments to if");

                if (form.count() < 3)
                    throw new Exception("Too few arguments to if");


                Expr testExpr = Compiler.GenerateAST(RT.second(form));
                Expr thenExpr = Compiler.GenerateAST(RT.third(form));
                Expr elseExpr = Compiler.GenerateAST(RT.fourth(form));

                return new IfExpr(testExpr, thenExpr, elseExpr);
            }
        }

    }
}
