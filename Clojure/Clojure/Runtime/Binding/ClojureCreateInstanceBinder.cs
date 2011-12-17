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

namespace clojure.lang.Runtime.Binding
{


    public class ClojureCreateInstanceBinder : CreateInstanceBinder, IExpressionSerializable, IClojureSite
    {
        #region Data

        readonly ClojureContext _context;

        #endregion

        #region C-tors

        public ClojureCreateInstanceBinder(ClojureContext context, int argCount)
            : base(new CallInfo(argCount, DynUtils.GetArgNames(argCount)))
        {
            _context = context;
        }

        #endregion

        #region CreateInstanceBinder methods

        public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            Type typeToUse = target.Value is Type ? (Type)target.Value : target.LimitType;

            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(args.Length);

            foreach (DynamicMetaObject arg in args)
                argsPlus.Add(arg);

            OverloadResolverFactory factory = _context.SharedOverloadResolverFactory;
            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(args.Length), CallTypes.None);

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            IList<MethodBase> methods = new List<MethodBase>(typeToUse.GetConstructors(flags).Where<MethodBase>(x => x.GetParameters().Length == args.Length));

            if (methods.Count > 0)
            {
                BindingTarget bt;
                DynamicMetaObject dmo = _context.Binder.CallMethod(res, methods, BindingRestrictions.Empty, "_ctor", NarrowingLevel.None, NarrowingLevel.All, out bt);
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
            return Expression.Call(typeof(ClojureCreateInstanceBinder).GetMethod("CreateMe"),
                BindingHelpers.CreateBinderStateExpression(),
                Expression.Constant(this.CallInfo.ArgumentCount));
        }

        public static ClojureCreateInstanceBinder CreateMe(ClojureContext context, int argCount)
        {
            return new ClojureCreateInstanceBinder(context, argCount);
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
