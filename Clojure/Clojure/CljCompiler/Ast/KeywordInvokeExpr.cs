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
            if (context.Mode == CompilerMode.Immediate)
            {
                // This will emit a plain Keyword reference, rather than a callsite.
                InvokeExpr ie = new InvokeExpr(_source, _spanMap, (Symbol)_tag, _kw, RT.vector(_target));
                return ie.GenDlr(context);
            }
            else
            {

                ParameterExpression thunkParam = Expression.Parameter(typeof(ILookupThunk), "thunk");
                Expression assignThunk = Expression.Assign(thunkParam, Expression.Field(null, context.ObjExpr.ThunkField(_siteIndex)));

                ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
                Expression assignTarget = Expression.Assign(targetParam,_target.GenDlr(context));

                ParameterExpression valParam = Expression.Parameter(typeof(Object), "val");
                Expression assignVal = Expression.Assign(valParam, Expression.Call(thunkParam, Compiler.Method_ILookupThunk_get,targetParam));

                ParameterExpression siteParam = Expression.Parameter(typeof(KeywordLookupSite), "site");
                Expression assignSite = Expression.Assign(siteParam, Expression.Field(null, context.ObjExpr.KeywordLookupSiteField(_siteIndex)));

                Expression block =
                    Expression.Block(typeof(Object), new ParameterExpression[] { thunkParam, valParam, targetParam },
                        assignThunk,
                        assignTarget,
                        assignVal,
                        Expression.Condition(
                            Expression.NotEqual(valParam, thunkParam),
                            valParam,
                            Expression.Block(typeof(Object), new ParameterExpression[] { siteParam },
                                assignSite,
                                Expression.Call(siteParam, Compiler.Method_ILookupSite_fault, targetParam, context.ObjExpr.ThisParam)),
                            typeof(object)));


                block = Compiler.MaybeAddDebugInfo(block, _spanMap);
                return block;
            }
        } 

        #endregion

    }
}
