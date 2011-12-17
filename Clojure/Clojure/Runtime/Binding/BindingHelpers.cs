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
using System.Reflection;
using Microsoft.Scripting.Actions;
using System.Dynamic;

namespace clojure.lang.Runtime.Binding
{
    static class BindingHelpers
    {
        static readonly PropertyInfo Property_ClojureContext_Default = typeof(ClojureContext).GetProperty("Default");
        static readonly Expression _contextExpr = Expression.Property(null,Property_ClojureContext_Default);

        internal static Expression CreateBinderStateExpression()
        {
            return _contextExpr;
        }
    }
}
