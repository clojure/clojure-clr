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

namespace clojure.lang.CljCompiler.Ast
{
    class InstanceMethodExpr : MethodExpr
    {
        #region Data

        readonly Expr _target;
        readonly string _methodName;
        readonly IPersistentVector _args;
        readonly MethodInfo _method;
        readonly string _source;
        readonly IPersistentMap _spanMap;
        readonly Symbol _tag;

        #endregion

        #region Ctors

        public InstanceMethodExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string methodName, IPersistentVector args)
        {
            _source = source;
            _spanMap = spanMap;
            _target = target;
            _methodName = methodName;
            _args = args;
            _tag = tag;

            if (target.HasClrType && target.ClrType == null)
                throw new ArgumentException(String.Format("Attempt to call instance method {0} on nil", methodName));

            _method = GetMatchingMethod(spanMap, target, _args, _methodName);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _method != null || _tag != null; }
        }

        public override Type ClrType
        {
            get { return _tag != null ? Compiler.TagToType(_tag) : _method.ReturnType; }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression[] exprs = new Expression[_args.count() + 1];
            exprs[0] = _target.GenDlr(context);

            for (int i = 0; i < _args.count(); i++)
                exprs[i+1] = ((Expr)_args.nth(i)).GenDlr(context);

            Type returnType = HasClrType ? ClrType : typeof(object);

            InvokeMemberBinder binder = new DefaultInvokeMemberBinder(_methodName, exprs.Length);
            DynamicExpression dyn = Expression.Dynamic(binder, returnType, exprs);

            Expression call = dyn;

            if (context.Mode == CompilerMode.File)
                call = context.DynInitHelper.ReduceDyn(dyn);

            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }

        //public override Expression GenDlr(GenContext context)
        //{
        //    if (_method != null)
        //        return Compiler.MaybeBox(GenDlrForMethod(context));
        //    else
        //        return GenDlrViaReflection(context);
        //}

        public override Expression GenDlrUnboxed(GenContext context)
        {
            if (_method != null)
                return GenDlrForMethod(context);
            else
                throw new InvalidOperationException("Unboxed emit of unknown member.");
        }

        private Expression GenDlrForMethod(GenContext context)
        {
            Expression target = _target.GenDlr(context);
            Expression[] args = GenTypedArgs(context, _method.GetParameters(), _args);

            Expression call = AstUtils.SimpleCallHelper(target,_method, args);
            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }

        private Expression GenDlrViaReflection(GenContext context)
        {
            Expression[] parms = new Expression[_args.count()];
            for (int i = 0; i < _args.count(); i++)
                parms[i] = Compiler.MaybeBox(((Expr)_args.nth(i)).GenDlr(context));

            Expression[] moreArgs = new Expression[3];
            moreArgs[0] = Expression.Constant(_methodName);
            moreArgs[1] = _target.GenDlr(context);
            moreArgs[2] = Expression.NewArrayInit(typeof(object), parms);

            Expression call = Expression.Call(Compiler.Method_Reflector_CallInstanceMethod, moreArgs);
            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }

        #endregion
    }
}
