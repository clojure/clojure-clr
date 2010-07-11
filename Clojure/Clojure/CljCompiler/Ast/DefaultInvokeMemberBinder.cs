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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Dynamic;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    public class DefaultInvokeMemberBinder : InvokeMemberBinder, IExpressionSerializable
    {
        #region Data

        readonly DefaultBinder _binder = new DefaultBinder();
        readonly bool _isStatic;

        #endregion

        #region C-tors

        public DefaultInvokeMemberBinder(string name, int argCount, bool isStatic)
            : base(name, false, new CallInfo(argCount, DynUtils.GetArgNames(argCount)))
        {
            _isStatic = isStatic;
        }

        #endregion

        #region InvokeMemberBinder methods

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (target.HasValue && target.Value == null)
                return errorSuggestion ??
                    new DynamicMetaObject(
                        Expression.Throw(
                            Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                                Expression.Constant(String.Format("Cannot call {0} method named {1} on nil", _isStatic ? "static" : "instance", this.Name))),
                            typeof(object)),
                            BindingRestrictions.GetInstanceRestriction(target.Expression, null));


            OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
            Type typeToUse = _isStatic && target.Value is Type ? (Type)target.Value : target.LimitType;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(args.Length + (_isStatic ? 0 : 1));
            if (!_isStatic)
                argsPlus.Add(target);
            foreach (DynamicMetaObject arg in args)
                argsPlus.Add(arg);

            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(args.Length), _isStatic ? CallTypes.None : CallTypes.ImplicitInstance);

            BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | (_isStatic ? BindingFlags.Static : BindingFlags.Instance);
            IList<MethodBase> methods = new List<MethodBase>(typeToUse.GetMethods(flags).Where<MethodBase>(x => x.Name == Name && x.GetParameters().Length == args.Length));

            if (methods.Count > 0)
            {

                DynamicMetaObject dmo = _binder.CallMethod(res, methods);
                dmo = DynUtils.MaybeBoxReturnValue(dmo);

                //; Console.WriteLine(dmo.Expression.DebugView);
                return dmo;
            }

            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant(String.Format("Cannot find member {0} matching args", this.Name))),
                        typeof(object)),
                    target.Restrictions.Merge(BindingRestrictions.Combine(args)));
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IExpressionSerializable Members

        public Expression CreateExpression()
        {
            return Expression.Call(typeof(DefaultInvokeMemberBinder).GetMethod("CreateMe"),
                Expression.Constant(this.Name),
                Expression.Constant(this.CallInfo.ArgumentCount),
                Expression.Constant(this._isStatic));
        }

        public static DefaultInvokeMemberBinder CreateMe(string name, int argCount, bool isStatic)
        {
            return new DefaultInvokeMemberBinder(name, argCount, isStatic);
        }

        #endregion
    }
}
