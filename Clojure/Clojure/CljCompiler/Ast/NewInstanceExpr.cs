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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang.CljCompiler.Ast
{
    class NewInstanceExpr : ObjExpr
    {

        #region Data


        #endregion

        #region C-tors

        public NewInstanceExpr(object tag)
            : base(tag)
        {
        }

        #endregion

        #region Type mangling


        #endregion

        #region Parsing

        public sealed class ReifyParser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext)
            {
                return null;
            }
        }

        public sealed class DefTypeParser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext)
            {
                return null;
            }
        }

        #endregion

        #region Code generation


        #endregion
    }
}
