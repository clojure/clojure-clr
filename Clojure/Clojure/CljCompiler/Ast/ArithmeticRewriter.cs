using System;
using System.Collections.Generic;
using System.Linq;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
using System.Reflection;
#endif


namespace clojure.lang.CljCompiler.Ast
{
    class ArithmeticRewriter : ExpressionVisitor    
    {

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Expression rewrite = null;
            if (TryUncheckedCast(node,ref rewrite))
                return rewrite;


            return base.VisitMethodCall(node);
        }

        static bool TryUncheckedCast(MethodCallExpression node, ref Expression rewrite)
        {
            MethodInfo info = node.Method;
            Type decType = info.DeclaringType;
            string name = info.Name;

            if (decType == typeof(RT))
            {
                if (name.StartsWith("unchecked") && name.EndsWith("Cast"))
                {
                    ParameterInfo[] pinfos = info.GetParameters();
                    if (pinfos.Length == 1 && pinfos[0].ParameterType != typeof(object))
                    {
                        rewrite = Expression.Convert(node.Arguments[0], info.ReturnType);
                        return true;
                    }
                }
            }
            else if (decType == typeof(Util))
            {
                if (name == "identical")
                {
                    ParameterExpression param1 = Expression.Parameter(typeof(Object), "temp1");
                    ParameterExpression param2 = Expression.Parameter(typeof(Object), "temp2");

                    rewrite = Expression.Block(typeof(bool), new ParameterExpression[] { param1, param2 },
                        Expression.Assign(param1, node.Arguments[0]),
                        Expression.Assign(param2, node.Arguments[1]),
                        Expression.Condition(
                            Expression.TypeIs(param1, typeof(ValueType)),
                            Expression.Call(param1, typeof(object).GetMethod("Equals", new Type[] { typeof(object) }), param2),
                            Expression.ReferenceEqual(param1, param2)));
                    return true;
                }
            }
            else if (decType == typeof(Numbers))
            {
                ParameterInfo[] pinfos = info.GetParameters();

                bool argsOk = (pinfos.Length == 1 && (pinfos[0].ParameterType == typeof(double) || pinfos[0].ParameterType == typeof(long)))
                    || (pinfos.Length == 2 && (pinfos[0].ParameterType == typeof(double) || pinfos[0].ParameterType == typeof(long)) && pinfos[0].ParameterType == pinfos[1].ParameterType);
                if (argsOk)
                {
                    switch (name)
                    {
                        case "lt":
                            rewrite = Expression.LessThan(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "lte":
                            rewrite = Expression.LessThanOrEqual(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "gt":
                            rewrite = Expression.GreaterThan(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "gte":
                            rewrite = Expression.GreaterThanOrEqual(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "isPos":
                            rewrite = Expression.GreaterThan(node.Arguments[0], Expression.Constant(pinfos[0].ParameterType == typeof(double) ? (object)0.0 : (object)0L));
                            return true;
                        case "isNeg":
                            rewrite = Expression.LessThan(node.Arguments[0], Expression.Constant(pinfos[0].ParameterType == typeof(double) ? (object)0.0 : (object)0L));
                            return true;
                        case "isZero":
                            rewrite = Expression.Equal(node.Arguments[0], Expression.Constant(pinfos[0].ParameterType == typeof(double) ? (object)0.0 : (object)0L));
                            return true;
                        case "equiv":
                            rewrite = Expression.Equal(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "unchecked_add":
                            rewrite = Expression.Add(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "unchecked_minus":
                            if (pinfos.Length == 1)
                                rewrite = Expression.Negate(node.Arguments[0]);
                            else
                                rewrite = Expression.Subtract(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "unchecked_multiply":
                            rewrite = Expression.Multiply(node.Arguments[0], node.Arguments[1]);
                            return true;
                        case "unchecked_inc":
                            rewrite = Expression.Increment(node.Arguments[0]);
                            return true;
                        case "unchecked_dec":
                            rewrite = Expression.Decrement(node.Arguments[0]);
                            return true;
                    }

                    if (pinfos[0].ParameterType == typeof(double))
                    {
                        switch (name)
                        {
                            case "add":
                            case "addP":
                                rewrite = Expression.Add(node.Arguments[0], node.Arguments[1]);
                                return true;
                            case "multiply":
                            case "multiplyP":
                                rewrite = Expression.Multiply(node.Arguments[0], node.Arguments[1]);
                                return true;
                            case "divide":
                                rewrite = Expression.Divide(node.Arguments[0], node.Arguments[1]);
                                return true;
                            case "minus":
                            case "minusP":
                                if (pinfos.Length == 1)
                                    rewrite = Expression.Negate(node.Arguments[0]);
                                else
                                    rewrite = Expression.Subtract(node.Arguments[0], node.Arguments[1]);
                                return true;
                            case "inc":
                            case "incP":
                                rewrite = Expression.Increment(node.Arguments[0]);
                                return true;
                            case "dec":
                            case "decP":
                                rewrite = Expression.Decrement(node.Arguments[0]);
                                return true;
                        }
                    }
                    else // long
                    {
                        switch (name)
                        {
                            case "quotient":
                                rewrite = Expression.Divide(node.Arguments[0], node.Arguments[1]);
                                return true;
                            case "remainder":
                                rewrite = Expression.Modulo(node.Arguments[0], node.Arguments[1]);
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    
    }
}
