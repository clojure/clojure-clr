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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif


namespace clojure.lang.CljCompiler.Ast
{
    class MapExpr : Expr
    {
        #region Data

        readonly IPersistentVector _keyvals;

        public IPersistentVector KeyVals
        {
            get { return _keyvals; }
        } 


        #endregion

        #region Ctors

        public MapExpr(IPersistentVector keyvals)
        {
            _keyvals = keyvals;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return typeof(IPersistentMap); }
        }

        #endregion

        #region Parsing

        public static Expr Parse(IPersistentMap form)
        {
            ParserContext pcon = new ParserContext(false, false);

            IPersistentVector keyvals = PersistentVector.EMPTY;
            for (ISeq s = RT.seq(form); s != null; s = s.next())
            {
                IMapEntry e = (IMapEntry)s.first();
                keyvals = (IPersistentVector)keyvals.cons(Compiler.GenerateAST(e.key(),pcon));
                keyvals = (IPersistentVector)keyvals.cons(Compiler.GenerateAST(e.val(),pcon));
            }
            Expr ret = new MapExpr(keyvals);
            return Compiler.OptionallyGenerateMetaInit(form, ret);
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression argArray = Compiler.GenArgArray(context, _keyvals);
            Expression ret = Expression.Call(Compiler.Method_RT_map, argArray);
            return ret;
        }

        #endregion
    }
}
