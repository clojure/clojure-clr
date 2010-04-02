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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace clojure.lang.CljCompiler.Ast
{
    sealed class KeywordInvokeExpr : Expr
    {
        #region Data

        readonly KeywordExpr _kw;
        readonly Object _tag;
        readonly Expr _target;
        readonly string _source;
        readonly IPersistentMap _spanMap;
        readonly int _siteIndex;

        #endregion

        #region C-tors

        public KeywordInvokeExpr(string source, IPersistentMap spanMap, Symbol tag, KeywordExpr kw, Expr target)
        {
            _source = source;
            _spanMap = spanMap;
            _kw = kw;
            _target = target;
            _tag = tag;
            _siteIndex = Compiler.RegisterKeywordCallsite(kw.Kw);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tag != null; }
        }

        public override Type ClrType
        {
            get { return HostExpr.TagToType(_tag); }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            // This will emit a plain Keyword reference, rather than a callsite.
            InvokeExpr ie = new InvokeExpr(_source, _spanMap, (Symbol)_tag, _kw, RT.vector(_target));
            return ie.GenDlr(context);

           

        }

        #endregion

    }
}
