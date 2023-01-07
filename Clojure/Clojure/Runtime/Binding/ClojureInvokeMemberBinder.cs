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
using System.Linq.Expressions;
using System.Dynamic;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;
using System.Reflection;
using System.Reflection.Emit;
using clojure.lang.CljCompiler.Ast;

namespace clojure.lang.Runtime.Binding
{

    public class ClojureInvokeMemberBinder : InvokeMemberBinder, IExpressionSerializable, IClojureBinder
    {
        #region Data

        readonly ClojureContext _context;
        readonly bool _isStatic;

        static readonly MethodInfo MI_CreateMe = typeof(ClojureInvokeMemberBinder).GetMethod("CreateMe");

        #endregion

        #region C-tors

        public ClojureInvokeMemberBinder(ClojureContext context, string name, int argCount, bool isStatic)
            : base(name, false, new CallInfo(argCount, DynUtils.GetArgNames(argCount)))
        {
            _context = context;
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



            Type typeToUse = _isStatic && target.Value is Type type ? type : target.LimitType;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(args.Length + (_isStatic ? 0 : 1));
            if (!_isStatic)
                argsPlus.Add(target);
            foreach (DynamicMetaObject arg in args)
                argsPlus.Add(arg);

            OverloadResolverFactory factory = _context.SharedOverloadResolverFactory;
            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(args.Length), _isStatic ? CallTypes.None : CallTypes.ImplicitInstance);

            BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | (_isStatic ? BindingFlags.Static : BindingFlags.Instance);
            IList<MethodBase> methods = new List<MethodBase>(typeToUse.GetMethods(flags).Where<MethodBase>(x => x.Name == Name && x.GetParameters().Length == args.Length));

            if (methods.Count > 0)
            {
                DynamicMetaObject dmo = _context.Binder.CallMethod(
                    res,
                    methods,
                    target.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(target).Merge(BindingRestrictions.Combine(args))),
                    Name,
                    NarrowingLevel.None,
                    NarrowingLevel.All,
                    out BindingTarget bt);
                dmo = DynUtils.MaybeBoxReturnValue(dmo);

                //; Console.WriteLine(dmo.Expression.DebugView);
                return dmo;
            }

            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant(String.Format("No matching member {0} taking {1} args for {2}", this.Name, args.Length, typeToUse.Name))),
                        typeof(object)),
                    target.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(target).Merge(BindingRestrictions.Combine(args))));
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IExpressionSerializable Members

        public Expression CreateExpression()
        {
            return Expression.Call(MI_CreateMe,
                BindingHelpers.CreateBinderStateExpression(),
                Expression.Constant(this.Name),
                Expression.Constant(this.CallInfo.ArgumentCount),
                Expression.Constant(this._isStatic));
        }

        public static ClojureInvokeMemberBinder CreateMe(ClojureContext context, string name, int argCount, bool isStatic)
        {
            return new ClojureInvokeMemberBinder(context, name, argCount, isStatic);
        }

        #endregion

        #region IClojureBinder

        public ClojureContext Context
        {
            get { return _context; }
        }

        // Should match CreateExpression
        public void GenerateCreationIL(ILGenerator ilg)
        {
            CljILGen ilg2 = new CljILGen(ilg);
            ilg2.EmitCall(BindingHelpers.Method_ClojureContext_GetDefault);
            ilg2.EmitString(Name);
            ilg2.EmitInt(this.CallInfo.ArgumentCount);
            ilg2.EmitBoolean(_isStatic);
            ilg2.EmitCall(MI_CreateMe);
        }

        #endregion
    }
}
