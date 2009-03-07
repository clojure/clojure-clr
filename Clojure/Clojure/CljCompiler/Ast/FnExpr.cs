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
    class FnExpr : Expr
    {
        #region Data

        IPersistentCollection _methods;
        FnMethod _variadicMethod = null;
        string _name;
        string _simpleName;
        string _internalName;
        string _thisName;
        Type _fnType;
        readonly object _tag;
        IPersistentMap _closes = PersistentHashMap.EMPTY;          // localbinding -> itself
        IPersistentMap _keywords = PersistentHashMap.EMPTY;         // Keyword -> KeywordExpr
        IPersistentMap _vars = PersistentHashMap.EMPTY;
        PersistentVector _constants;
        bool _onceOnly = false;
        string _superName = null;
        
        #endregion

        #region Ctors

        public FnExpr(object tag)
        {
            _tag = tag;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return _tag != null ? Compiler.TagToType(_tag) : typeof(IFn); }
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
