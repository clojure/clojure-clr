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

using System.Collections.Generic;

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Dynamic;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace clojure.lang.Runtime.Binding
{
    static class DynUtils
    {
        #region Argument names

        static Dictionary<int, string[]> _argNamesCache = new Dictionary<int, string[]>();

        public static string[] GetArgNames(int argCount)
        {
            string[] names;
            if (!_argNamesCache.TryGetValue(argCount, out names))
            {
                names = CreateArgNames(argCount);
                _argNamesCache[argCount] = names;
            }
            return names;
        }

        private static string[] CreateArgNames(int argCount)
        {
            string[] names = new string[argCount];
            for (int i = 0; i < argCount; i++)
                names[i] = "arg" + i.ToString();
            return names;
        }

        #endregion

        #region  Boxing support

        public static DynamicMetaObject MaybeBoxReturnValue(DynamicMetaObject res)
        {
            if (res.Expression.Type.IsValueType)
            {
                res = AddBoxing(res);
            }
            else if (res.Expression.Type == typeof(void))
            {
                res = new DynamicMetaObject(
                    Expression.Block(
                        res.Expression,
                        Expression.Constant(null)
                    ),
                    res.Restrictions
                );
            }

            return res;
        }

        public static DynamicMetaObject AddBoxing(DynamicMetaObject res)
        {
            if (res.Expression.Type.IsValueType)
            {
                res = new DynamicMetaObject(
                    AddBoxing(res.Expression),
                    res.Restrictions
                );
            }
            return res;
        }

        public static Expression AddBoxing(Expression res)
        {
            return AstUtils.Convert(res, typeof(object));
        }

        #endregion
    }
}
