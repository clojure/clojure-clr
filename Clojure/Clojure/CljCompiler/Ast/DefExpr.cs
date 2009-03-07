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
    class DefExpr : Expr
    {
        #region Data

        readonly Var _var;
        readonly Expr _init;
        readonly Expr _meta;
        readonly bool _initProvided;

        #endregion

        #region Ctors

        public DefExpr(Var var, Expr init, Expr meta, bool initProvided)
        {
            _var = var;
            _init = init;
            _meta = meta;
            _initProvided = initProvided;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return typeof(Var); }
        }

        #endregion

        public sealed class Parser : IParser
        {
            public Expr Parse(object form)
            {
                throw new NotImplementedException();
            }
        }


    }
}
