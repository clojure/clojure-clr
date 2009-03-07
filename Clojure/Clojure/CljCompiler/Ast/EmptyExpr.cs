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
    class EmptyExpr : Expr
    {
        #region Data

        readonly object _coll;

        #endregion

        #region Ctors

        public EmptyExpr(object coll)
        {
            _coll = coll;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get {
                if (_coll is IPersistentList)
                    return typeof(IPersistentList);
                else if (_coll is IPersistentVector)
                    return typeof(IPersistentVector);
                else if (_coll is IPersistentMap)
                    return typeof(IPersistentMap);
                else if (_coll is IPersistentSet)
                    return typeof(IPersistentSet);
                else
                    throw new InvalidOperationException("Unknown Collection type.");
            }
        }

        #endregion
    }
}
