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
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using AstUtils = Microsoft.Scripting.Ast.Utils;
using Microsoft.Scripting;
using System.Dynamic;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;

namespace clojure.lang.CljCompiler.Ast
{
    class InstanceMethodExpr : MethodExpr
    {
        #region Data

        readonly Expr _target;

        #endregion

        #region Ctors

        public InstanceMethodExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string methodName, List<HostArg> args)
            : base(source,spanMap,tag,methodName,args)
        {
            _target = target;

            if (target.HasClrType && target.ClrType == null)
                throw new ArgumentException(String.Format("Attempt to call instance method {0} on nil", methodName));

            _method = GetMatchingMethod(spanMap, target, _args, _methodName);
        }

        #endregion

        #region eval

        // TODO: handle by-ref
        public override object Eval()
        {
            object targetVal = _target.Eval();
            object[] argvals = new object[_args.Count];
            for (int i = 0; i < _args.Count; i++)
                argvals[i] = _args[i].ArgExpr.Eval();
            if (_method != null)
                return _method.Invoke(targetVal, argvals);
            return Reflector.CallInstanceMethod(_methodName, targetVal, argvals);
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
            get { return false; }
        }

        protected override Expression GenTargetExpression(ObjExpr objx, GenContext context)
        {
            Expression expr = _target.GenCode(RHC.Expression, objx, context);
            if ( _target.HasClrType )
                expr =  Expression.Convert(expr,_target.ClrType);

            return expr;
        }

        #endregion
    }
}
