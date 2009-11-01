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
using System.IO;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif


namespace clojure.lang.CljCompiler.Ast
{
    abstract class InstanceFieldOrProprtyExpr<TInfo> : FieldOrPropertyExpr
    {
        #region Data

        readonly Expr _target;
        readonly Type _targetType;
        protected readonly TInfo _tinfo;
        readonly string _fieldName;
        readonly int _line;

        #endregion

        #region Ctors

        public InstanceFieldOrProprtyExpr(int line, Expr target, string fieldName, TInfo tinfo)
        {
            _line = line;
            _target = target;
            _fieldName = fieldName;
            _tinfo = tinfo;

            _targetType = target.HasClrType ? target.ClrType : null;

            // Java version does not include check on _targetType
            // However, this seems consistent with the checks in the generation code.
            if ( (_targetType == null || _tinfo == null) && RT.booleanCast(RT.WARN_ON_REFLECTION.deref()))
                ((TextWriter)RT.ERR.deref()).WriteLine("Reflection warning {0}:{1} - reference to field/property {2} can't be resolved.", 
                    Compiler.SOURCE_PATH.deref(), line,_fieldName);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tinfo != null; }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression target = _target.GenDlr(context);
            if (_targetType != null && _tinfo != null)
            {
                Expression convTarget = Expression.Convert(target, _targetType);
                Expression access = GenAccess(convTarget);
                return Compiler.MaybeBox(access);
            }
            else
            {
                Expression call = Expression.Call(Compiler.Method_Reflector_GetInstanceFieldOrProperty, target, Expression.Constant(_fieldName));
                return Compiler.MaybeBox(call);
            }
        }

        protected abstract Expression GenAccess(Expression target);

        public override Expression GenDlrUnboxed(GenContext context)
        {
            Expression target = _target.GenDlr(context);
            if (_targetType != null && _tinfo != null)
            {
                Expression convTarget = Expression.Convert(target, _targetType);
                Expression access = GenAccess(convTarget);
                return access;
            }
            else
                throw new InvalidOperationException("Unboxed emit of unknown member.");
        }

        #endregion

        #region AssignableExpr Members

        public override Expression GenAssignDlr(GenContext context, Expr val)
        {
            Expression target = _target.GenDlr(context);
            Expression valExpr = val.GenDlr(context);
            if (_targetType != null && _tinfo != null)
            {
                Expression convTarget = Expression.Convert(target, _targetType);
                Expression access = GenAccess(convTarget);
                return Expression.Assign(access, valExpr);
            }
            else
            {
                Expression call = Expression.Call(
                    Compiler.Method_Reflector_SetInstanceFieldOrProperty,
                    target,
                    Expression.Constant(_fieldName),
                    Compiler.MaybeBox(valExpr));
                return call;
            }
        }

        #endregion
    }

    sealed class InstanceFieldExpr : InstanceFieldOrProprtyExpr<FieldInfo>
    {
        #region C-tors

        public InstanceFieldExpr(int line, Expr target, string fieldName, FieldInfo finfo)
            :base(line,target,fieldName,finfo)  
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

        protected override Expression GenAccess(Expression target)
        {
            return Expression.Field(target, _tinfo);
        }

        #endregion
    }


    sealed class InstancePropertyExpr : InstanceFieldOrProprtyExpr<PropertyInfo>
    {
        #region C-tors

        public InstancePropertyExpr(int line, Expr target, string fieldName, PropertyInfo pinfo)
            :base(line,target,fieldName,pinfo)  
        {
        }

        #endregion

        #region Type mangling

        public override Type ClrType
        {
            get { return  _tinfo.PropertyType; }
        }

        #endregion

        #region Code generation

        protected override Expression GenAccess(Expression target)
        {
            return Expression.Property(target, _tinfo);
        }

        #endregion
    }
}
