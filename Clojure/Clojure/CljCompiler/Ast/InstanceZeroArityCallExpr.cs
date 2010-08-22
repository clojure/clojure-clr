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
using System.Dynamic;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace clojure.lang.CljCompiler.Ast
{
    class InstanceZeroArityCallExpr : HostExpr
    {
        #region Data

        readonly Expr _target;
        readonly Type _targetType; 
        readonly string _memberName;
        protected readonly string _source;
        protected readonly IPersistentMap _spanMap;
        protected readonly Symbol _tag;

        #endregion

        #region Ctors

        internal InstanceZeroArityCallExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string memberName)
        {
            _source = source;
            _spanMap = spanMap;
            _memberName = memberName;
            _target = target;
            _tag = tag;

            _targetType = target.HasClrType ? target.ClrType : null;
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
            return Compiler.MaybeBox(GenDlrUnboxed(context));
        }

        public override Expression GenDlrUnboxed(GenContext context)
        {
            Expression target = _target.GenDlr(context);

            Type returnType = HasClrType ? ClrType : typeof(object);

            GetMemberBinder binder = new DefaultGetZeroArityMemberBinder(_memberName, false);
            DynamicExpression dyn = Expression.Dynamic(binder, returnType, new Expression[] { target });

            Expression call = dyn;

            //if ( context.Mode == CompilerMode.File )
            if ( context.DynInitHelper != null )
                call = context.DynInitHelper.ReduceDyn(dyn);

            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }

        public override bool CanEmitPrimitive
        {
            get { return HasClrType && Util.IsPrimitive(ClrType); }
        }

        #endregion
    }
}
