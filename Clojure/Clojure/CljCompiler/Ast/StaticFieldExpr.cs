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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.IO;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using Microsoft.Scripting;


namespace clojure.lang.CljCompiler.Ast
{
    abstract class StaticFieldOrPropertyExpr<TInfo> : FieldOrPropertyExpr
    {
        #region Data

        readonly string _fieldName;
        readonly Type _type;
        protected readonly TInfo _tinfo;
        readonly protected string _source;
        readonly protected IPersistentMap _spanMap;

        #endregion

        #region Ctors

        protected StaticFieldOrPropertyExpr(string source, IPersistentMap spanMap, Type type, string fieldName, TInfo tinfo)
        {
            _source = source;
            _spanMap = spanMap;
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
            Expression assign = Expression.Assign(access, valExpr);
            assign = Compiler.MaybeAddDebugInfo(assign, _spanMap);
            return assign;
        }

        #endregion
    }

    sealed class StaticFieldExpr : StaticFieldOrPropertyExpr<FieldInfo>
    {
        #region C-tors

        public StaticFieldExpr(string source, IPersistentMap spanMap, Type type, string fieldName, FieldInfo finfo)
            : base(source, spanMap, type, fieldName, finfo)
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
            Expression field = Expression.Field(null, _tinfo);
            field = Compiler.MaybeAddDebugInfo(field, _spanMap);
            return field;
        }

        #endregion
    }

    sealed class StaticPropertyExpr : StaticFieldOrPropertyExpr<PropertyInfo>
    {
        #region C-tors

        public StaticPropertyExpr(string source, IPersistentMap spanMap, Type type, string fieldName, PropertyInfo pinfo)
            : base(source, spanMap, type, fieldName, pinfo)
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
            Expression prop = Expression.Property(null, _tinfo);
            prop = Compiler.MaybeAddDebugInfo(prop, _spanMap);
            return prop;
        }

        #endregion
    }

}
