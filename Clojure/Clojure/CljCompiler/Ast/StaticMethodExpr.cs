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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif


namespace clojure.lang.CljCompiler.Ast
{
    class StaticMethodExpr : MethodExpr
    {
        #region Data

        readonly Type _type;

        #endregion

        #region Ctors

        public StaticMethodExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string methodName, List<HostArg> args)
            : base(source,spanMap,tag,methodName,args)
        {
            _type = type;
            _method  = GetMatchingMethod(spanMap, _type, _args, _methodName);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _method != null || _tag != null; }
        }

        public override Type ClrType
        {
            get { return _tag != null ? HostExpr.TagToType(_tag) : _method.ReturnType; }
        }

        #endregion

        #region Code generation

        protected override bool IsStaticCall
        {
            get { return true; }
        }

        protected override Expression GenTargetExpression(GenContext context)
        {
            return Expression.Constant(_type, typeof(Type));
        }

        //protected override Expression GenDlrForMethod(GenContext context)
        //{
        //    Expression target = _target.GenDlr(context);
        //    Expression[] args = GenTypedArgs(context, _method.GetParameters(), _args);

        //    return AstUtils.ComplexCallHelper(target,_method, args);
        //}

        #endregion
    }
}
