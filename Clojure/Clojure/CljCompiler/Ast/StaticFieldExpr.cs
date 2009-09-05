/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Linq.Expressions;
using System.IO;

namespace clojure.lang.CljCompiler.Ast
{
    abstract class StaticFieldOrPropertyExpr<TInfo> : FieldOrPropertyExpr
    {
        #region Data

        readonly string _fieldName;
        readonly Type _type;
        protected readonly TInfo _tinfo;
        readonly int _line;

        #endregion

        #region Ctors

        protected StaticFieldOrPropertyExpr(int line, Type type, string fieldName, TInfo tinfo)
        {
            _line = line;
            _fieldName = fieldName;
            _type = type;
            _tinfo = tinfo;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            return Compiler.MaybeBox(GenDlrUnboxed(context));
        }

        #endregion

        #region AssignableExpr Members

        public override Expression GenAssignDlr(GenContext context, Expr val)
        {
            Expression access = GenDlrUnboxed(context);
            Expression valExpr = val.GenDlr(context);
            return Expression.Assign(access, valExpr);
        }

        #endregion
    }

    sealed class StaticFieldExpr : StaticFieldOrPropertyExpr<FieldInfo>
    {
        #region C-tors

        public StaticFieldExpr(int line, Type type, string fieldName, FieldInfo finfo)
            : base(line, type, fieldName, finfo)
        {
        }

        #endregion


        #region Type mangling

        public override Type ClrType
        {
            get { return _tinfo.FieldType; }
        }

        #endregion

        #region Code generation

        public override Expression GenDlrUnboxed(GenContext context)
        {
            return Expression.Field(null, _tinfo);
        }

        #endregion
    }

    sealed class StaticPropertyExpr : StaticFieldOrPropertyExpr<PropertyInfo>
    {
        #region C-tors

        public StaticPropertyExpr(int line, Type type, string fieldName, PropertyInfo pinfo)
            : base(line, type, fieldName, pinfo)
        {
        }

        #endregion


        #region Type mangling

        public override Type ClrType
        {
            get { return _tinfo.PropertyType; }
        }

        #endregion

        #region Code generation

        public override Expression GenDlrUnboxed(GenContext context)
        {
            return Expression.Property(null, _tinfo);
        }

        #endregion
    }

}
