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
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif


namespace clojure.lang.CljCompiler.Ast
{
    abstract class StaticFieldOrPropertyExpr<TInfo> : FieldOrPropertyExpr
    {
        #region Data

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        readonly string _fieldName;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]   
        readonly Type _type;
        
        protected readonly TInfo _tinfo;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]   
        protected readonly string _source;

        protected readonly IPersistentMap _spanMap;
        protected readonly Symbol _tag;

        #endregion

        #region Ctors

        protected StaticFieldOrPropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string fieldName, TInfo tinfo)
        {
            _source = source;
            _spanMap = spanMap;
            _fieldName = fieldName;
            _type = type;
            _tinfo = tinfo;
            _tag = tag;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        #endregion

        #region Code generation

        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return HostExpr.GenBoxReturn(GenCodeUnboxed(rhc, objx, context),FieldType,objx,context);
        }

        #endregion

        #region AssignableExpr Members

        public override Expression GenAssign(RHC rhc, ObjExpr objx, GenContext context, Expr val)
        {
            Expression access = GenCodeUnboxed(RHC.Expression, objx, context);
            Expression valExpr = val.GenCode(RHC.Expression, objx, context);
            Expression unboxValExpr = HostExpr.GenUnboxArg(valExpr, FieldType);
            Expression assign = Expression.Assign(access, unboxValExpr);
            assign = Compiler.MaybeAddDebugInfo(assign, _spanMap, context.IsDebuggable);
            return assign;
        }

        #endregion
    }

    sealed class StaticFieldExpr : StaticFieldOrPropertyExpr<FieldInfo>
    {
        #region C-tors

        public StaticFieldExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string fieldName, FieldInfo finfo)
            : base(source, spanMap, tag, type, fieldName, finfo)
        {
        }

        #endregion

        #region Type mangling

        public override Type ClrType
        {
            get { return _tag != null ? HostExpr.TagToType(_tag) : _tinfo.FieldType; }
        }

        #endregion

        #region eval

        // TODO: Handle by-ref
        public override object Eval()
        {
            return _tinfo.GetValue(null);
        }

        #endregion

        #region Code generation

        public override Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression field = Expression.Field(null, _tinfo);
            field = Compiler.MaybeAddDebugInfo(field, _spanMap, context.IsDebuggable);
            return field;
        }

        public override bool CanEmitPrimitive
        {
            get { return Util.IsPrimitive(_tinfo.FieldType); }
        }

        protected override Type FieldType
        {
            get { return _tinfo.FieldType; }
        }

        #endregion

        #region AssignableExpr members

        public override object EvalAssign(Expr val)
        {
            object e = val.Eval();
            _tinfo.SetValue(null, e);
            return e;
        }

        #endregion
    }

    sealed class StaticPropertyExpr : StaticFieldOrPropertyExpr<PropertyInfo>
    {
        #region C-tors

        public StaticPropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string fieldName, PropertyInfo pinfo)
            : base(source, spanMap, tag, type, fieldName, pinfo)
        {
        }

        #endregion

        #region Type mangling

        public override Type ClrType
        {
            get { return _tag != null ? HostExpr.TagToType(_tag) : _tinfo.PropertyType; }
        }

        #endregion

        #region eval

        // TODO: Handle by-ref
        public override object Eval()
        {
            return _tinfo.GetValue(null,new object[0]);
        }

        #endregion

        #region Code generation

        public override Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression prop = Expression.Property(null, _tinfo);
            prop = Compiler.MaybeAddDebugInfo(prop, _spanMap, context.IsDebuggable);
            return prop;
        }

        public override bool CanEmitPrimitive
        {
            get { return Util.IsPrimitive(_tinfo.PropertyType); }
        }

        protected override Type FieldType
        {
            get { return _tinfo.PropertyType; }
        }

        #endregion

        #region AssignableExpr members

        public override object EvalAssign(Expr val)
        {
            object e = val.Eval();
            _tinfo.SetValue(null, e, new object[0]);
            return e;
        }

        #endregion
    }

}
