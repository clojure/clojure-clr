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

namespace clojure.lang.CljCompiler.Ast
{
    class InstanceFieldExpr : FieldExpr, AssignableExpr
    {
        #region Data

        readonly Expr _target;
        readonly Type _targetType;
        readonly FieldInfo _fieldInfo;
        readonly PropertyInfo _propertyInfo;
        readonly string _fieldName;

        #endregion

        #region Ctors

        public InstanceFieldExpr(Expr target, string fieldName)
        {
            _target = target;
            _fieldName = fieldName;

            _targetType = target.HasClrType ? target.ClrType : null;
            _fieldInfo = _targetType != null ? _targetType.GetField(_fieldName, BindingFlags.Instance | BindingFlags.Public) : null;
            _propertyInfo = _targetType != null ? _targetType.GetProperty(_fieldName, BindingFlags.Instance | BindingFlags.Public) : null;

            if ( _fieldInfo == null && _propertyInfo == null  && RT.booleanCast(RT.WARN_ON_REFLECTION.deref()))
                ((TextWriter)RT.ERR.deref()).WriteLine("Reflection warning -- reference to field/property {0} can't be resolved.", _fieldName);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _fieldInfo != null || _propertyInfo != null; }
        }

        public override Type ClrType
        {
            get {

                return _fieldInfo != null
                    ? _fieldInfo.FieldType
                    : _propertyInfo.PropertyType;
            }
        }

        #endregion
    }
}
