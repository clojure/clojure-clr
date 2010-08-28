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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Actions;
using System.Dynamic;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Actions.Calls;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{

    public class DefaultCreateInstanceBinder : CreateInstanceBinder, IExpressionSerializable
    {
        #region Data

        readonly DefaultBinder _binder = new DefaultBinder();

        #endregion

        #region C-tors

        public DefaultCreateInstanceBinder(int argCount)
            : base(new CallInfo(argCount, DynUtils.GetArgNames(argCount)))
        {
        }

        #endregion

        #region CreateInstanceBinder methods

        public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
            Type typeToUse = target.Value is Type ? (Type)target.Value : target.LimitType;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(args.Length);

            foreach (DynamicMetaObject arg in args)
                argsPlus.Add(arg);

            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(args.Length), CallTypes.None);

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            IList<MethodBase> methods = new List<MethodBase>(typeToUse.GetConstructors(flags).Where<MethodBase>(x => x.GetParameters().Length == args.Length));

            if (methods.Count > 0)
            {
                DynamicMetaObject dmo = _binder.CallMethod(res, methods);
                dmo = DynUtils.MaybeBoxReturnValue(dmo);
                return dmo;
            }

            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant("Cannot find constructor matching args")),
                        typeof(object)),
                    target.Restrictions.Merge(BindingRestrictions.Combine(args)));
        }

        #endregion

        #region IExpressionSerializable Members

        public Expression CreateExpression()
        {
            return Expression.Call(typeof(DefaultCreateInstanceBinder).GetMethod("CreateMe"),
                Expression.Constant(this.CallInfo.ArgumentCount));
        }

        public static DefaultCreateInstanceBinder CreateMe(int argCount)
        {
            return new DefaultCreateInstanceBinder(argCount);
        }

        #endregion
    }
}
