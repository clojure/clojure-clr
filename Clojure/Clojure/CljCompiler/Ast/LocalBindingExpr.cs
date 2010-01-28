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
    class LocalBindingExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly LocalBinding _b;

        internal LocalBinding Binding
        {
            get { return _b; }
        } 

        readonly Symbol _tag;

        #endregion

        #region Ctors

        public LocalBindingExpr(LocalBinding b, Symbol tag)
        {
            if (b.PrimitiveType != null && _tag != null)
                throw new InvalidOperationException("Can't type hint a primitive local");
            _b = b;
            _tag = tag;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tag != null || _b.HasClrType; }
        }

        public override Type ClrType
        {
            get 
            {
                if (_tag != null)
                    return HostExpr.TagToType(_tag);
                return _b.ClrType;
            }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            return context.ObjExpr.GenLocal(context,_b);
        }


        public Expression GenDlrUnboxed(GenContext context)
        {
            return context.ObjExpr.GenUnboxedLocal(context,_b);
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return _b.PrimitiveType != null; }
        }

        #endregion
    }
}
