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
using System.Dynamic;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Runtime;
using System.Reflection;


namespace clojure.lang.Runtime.Binding
{
    internal sealed class ClojureOverloadResolverFactory : OverloadResolverFactory
    {
        #region Instance variables

        private readonly ClojureBinder _binder;

        #endregion

        #region C-tors and factories

        public ClojureOverloadResolverFactory(ClojureBinder binder)
        {
            Assert.NotNull(binder);
            _binder = binder;
        }

        public override DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType)
        {
            return new ClojureOverloadResolver(_binder, args, signature, callType);
        }

        #endregion
    }


    public sealed class ClojureOverloadResolver : DefaultOverloadResolver
    {
        #region Properties

        //private new ClojureBinder Binder { get { return (ClojureBinder)base.Binder; } }

        #endregion

        #region C-tors

        // instance method call
        public ClojureOverloadResolver(ClojureBinder binder, DynamicMetaObject instance, IList<DynamicMetaObject> args, CallSignature signature)
            : base(binder, instance, args, signature)
        {
        }

        // method call
        public ClojureOverloadResolver(ClojureBinder binder, IList<DynamicMetaObject> args, CallSignature signature)
            : this(binder, args, signature, CallTypes.None)
        {
        }

        // method call
        public ClojureOverloadResolver(ClojureBinder binder, IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType)
            : base(binder, args, signature, callType)
        {
        }

        #endregion
    }
}
