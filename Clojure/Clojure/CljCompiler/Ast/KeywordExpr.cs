﻿/**
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
    public class KeywordExpr : LiteralExpr
    {
        #region Data

        readonly Keyword _kw;

        public Keyword Kw { get { return _kw; } }

        #endregion

        #region Ctors

        public KeywordExpr(Keyword kw)
        {
            _kw = kw;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return typeof(Keyword); }
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            objx.EmitKeyword(ilg, _kw);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion

        #region LiteralExpr methods

        public override object Val
        {
            get { return _kw; }
        }

        #endregion
    }
}
