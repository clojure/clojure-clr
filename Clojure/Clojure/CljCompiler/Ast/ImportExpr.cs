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
    class ImportExpr : Expr
    {
        #region Data

        readonly string _c;

        #endregion

        #region Ctors

        public ImportExpr(string c)
        {
            _c = c;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return false; }
        }

        public Type ClrType
        {
            get { throw new ArgumentException("ImportExpr has no Java class"); }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                return new ImportExpr((string)RT.second(frm));
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            Namespace ns = (Namespace)RT.CURRENT_NS.deref();
            ns.importClass(RT.classForName(_c));
            return null;
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression getTypeExpr = Expression.Call(null, Compiler.Method_RT_classForName, Expression.Constant(_c));
            Expression getNsExpr = Expression.Property(null, Compiler.Method_Compiler_CurrentNamespace);
            return Expression.Call(getNsExpr, Compiler.Method_Namespace_importClass1, getTypeExpr);   
        }

        #endregion
    }
}
