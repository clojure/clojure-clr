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
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    class InstanceMethodExpr : MethodExpr
    {
        #region Data

        readonly Expr _target;
        readonly string _methodName;
        readonly IPersistentVector _args;
        readonly MethodInfo _method;

        #endregion

        #region Ctors

        public InstanceMethodExpr(Expr target, string methodName, IPersistentVector args)
        {
            _target = target;
            _methodName = methodName;
            _args = args;

            _method = ComputeMethod();
        }

        // TODO: ComputeMethod
        private MethodInfo ComputeMethod()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _method != null; }
        }

        public override Type ClrType
        {
            get { return _method.ReturnType; }
        }

        #endregion

    }
}
