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
    public class StringExpr : LiteralExpr
    {
        #region Data

        readonly string _str;
        public override object Val { get { return _str; } }

        #endregion

        #region Ctors

        public StringExpr(string str)
        {
            _str = str;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return typeof(string); }
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            if (rhc != RHC.Statement)
                ilg.EmitString(_str);
        }

        #endregion
    }
}
