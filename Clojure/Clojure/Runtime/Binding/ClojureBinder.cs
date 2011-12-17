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
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Generation;
using System.Dynamic;
using Microsoft.Scripting.Runtime;

namespace clojure.lang.Runtime.Binding
{
    using Ast = Expression;
    using AstUtils = Microsoft.Scripting.Ast.Utils;


    public class ClojureBinder : DefaultBinder
    {
        #region Instance variables

        readonly ClojureContext _context;

        #endregion

        #region Properties

        public ClojureContext Context { get { return _context; } }

        public override bool PrivateBinding { get { return _context.DomainManager.Configuration.PrivateBinding; } }

        #endregion

        #region Constructors

        public ClojureBinder(ClojureContext context)
        {
            _context = context;
        }

        #endregion

        #region conversions

        public override bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level)
        {
            return Converter.CanConvertFrom(fromType, toType, level);
        }

        public override Ast ConvertExpression(Ast expr, Type toType, ConversionResultKind kind, OverloadResolverFactory resolverFactory)
        {
            Type exprType = expr.Type;
            Type visType = CompilerHelpers.GetVisibleType(toType);

            if (typeof(IFn).IsAssignableFrom(exprType) && typeof(Delegate).IsAssignableFrom(visType))
                return Ast.Call(typeof(Converter).GetMethod("ConvertToDelegate"), Ast.Convert(expr, typeof(IFn)), Expression.Constant(visType));

            return base.ConvertExpression(expr, toType, kind, resolverFactory);
        }

        #endregion
    }
}
