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
    class NewExpr : Expr
    {
        #region Data

        readonly IPersistentVector _args;
        readonly ConstructorInfo _ctor;
        readonly Type _type;

        #endregion

        #region Ctors

        public NewExpr(Type type, IPersistentVector args)
        {
            _args = args;
            _type = type;
            
            ComputeCtor();
        }

        private void ComputeCtor()
        {
 	        throw new NotImplementedException();
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return _type; }
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
