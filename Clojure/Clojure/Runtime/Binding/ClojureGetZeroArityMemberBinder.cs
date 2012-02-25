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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Dynamic;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Actions;
using System.Reflection;

namespace clojure.lang.Runtime.Binding
{

    public class ClojureGetZeroArityMemberBinder : GetMemberBinder, IExpressionSerializable, IClojureSite
    {
        #region Data

        readonly bool _isStatic;
        readonly ClojureContext _context;

        #endregion

        #region Properties

        public bool IsStatic
        {
            get { return _isStatic; }
        }

        #endregion

        #region C-tor

        public ClojureGetZeroArityMemberBinder(ClojureContext context, string name, bool isStatic)
            : base(name, false)
        {
            _context = context;
            _isStatic = isStatic;
        }

        #endregion

        #region GetMemberBinder Members

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (target.HasValue && target.Value == null)
                return errorSuggestion ??
                    new DynamicMetaObject(
                        Expression.Throw(
                            Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                                Expression.Constant(String.Format("Cannot call {0} field/property/member name {1} on nil", _isStatic ? "static" : "instance", this.Name))),
                            typeof(object)),
                            BindingRestrictions.GetInstanceRestriction(target.Expression, null));

            Expression instanceExpr = _isStatic ? null : Expression.Convert(target.Expression, target.LimitType);
            Type typeToUse = _isStatic && target.Value is Type ? (Type)target.Value : target.LimitType;

            BindingRestrictions restrictions = target.Restrictions.Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            BindingFlags flags = BindingFlags.Public;
            if (_isStatic)
                flags |= BindingFlags.Static;
            else
                flags |= BindingFlags.Instance;

            FieldInfo finfo = typeToUse.GetField(Name, flags);
            if (finfo != null)
                return DynUtils.MaybeBoxReturnValue(new DynamicMetaObject(Expression.Field(instanceExpr, finfo), restrictions));

            PropertyInfo pinfo = typeToUse.GetProperty(Name, flags);
            if (pinfo != null)
                return DynUtils.MaybeBoxReturnValue(new DynamicMetaObject(Expression.Property(instanceExpr, pinfo), restrictions));

            MethodInfo minfo = typeToUse.GetMethod(Name, flags, Type.DefaultBinder, Type.EmptyTypes, new ParameterModifier[0]);
            if (minfo != null)
                return DynUtils.MaybeBoxReturnValue(new DynamicMetaObject(Expression.Call(instanceExpr, minfo), restrictions));

            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant(String.Format("Cannot find {0} field/property/member name {1}", _isStatic ? "static" : "instance", this.Name))),
                        typeof(object)),
                    target.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(target)));
        }

        #endregion

        #region IExpressionSerializable Members

        public Expression CreateExpression()
        {
            return Expression.Call(typeof(ClojureGetZeroArityMemberBinder).GetMethod("CreateMe"),
                BindingHelpers.CreateBinderStateExpression(),
                Expression.Constant(this.Name),
                Expression.Constant(this._isStatic));
        }

        public static ClojureGetZeroArityMemberBinder CreateMe(ClojureContext context, string name, bool isStatic)
        {
            return new ClojureGetZeroArityMemberBinder(context, name, isStatic);
        }

        #endregion

        #region IClojureSite members

        public ClojureContext Context
        {
            get { return _context; }
        }

        #endregion
    }
}
