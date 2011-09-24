using System;
using System.Collections.Generic;
using System.Linq;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    class IntrinsicsRewriter : ExpressionVisitor    
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
                switch (name)
                {
                    case "doubleCast":
                    case "uncheckedDoubleCast":
                    case "longCast":
                    case "uncheckedIntCast":
                    case "uncheckedLongCast":
                        {
                            ParameterInfo[] pinfos = info.GetParameters();
                            if (pinfos.Length == 1 && pinfos[0].ParameterType != typeof(object))
                            {
                                rewrite = Expression.Convert(node.Arguments[0], info.ReturnType);
                                return true;
                            }
                        }
                        break;
                    case "aget":
                        {
                            ParameterInfo[] pinfos = info.GetParameters();
                            if (pinfos.Length == 2 && pinfos[0].ParameterType.IsArray)
                            {
                                rewrite = Expression.ArrayIndex(node.Arguments[0], node.Arguments[1]);
                                return true;
                            }
                        }
                        break;

                    case "alength":
                        {
                            ParameterInfo[] pinfos = info.GetParameters();
                            if (pinfos.Length == 1 && pinfos[0].ParameterType.IsArray)
                            {
                                rewrite = Expression.ArrayLength(node.Arguments[0]);
                                return true;
                            }
                        }
                        break;
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
                else if (name == "equiv")
                {
                    ParameterInfo[] pinfos = info.GetParameters();
                    if (pinfos.Length == 2
                        && pinfos[0].ParameterType == pinfos[1].ParameterType
                        && (pinfos[0].ParameterType == typeof(long) || pinfos[0].ParameterType == typeof(double) || pinfos[0].ParameterType == typeof(bool)))
                    {
                        rewrite = Expression.Equal(node.Arguments[0], node.Arguments[1]);
                        return true;
                    }
                }
            }
            else if (decType == typeof(Numbers))
            {
                ParameterInfo[] pinfos = info.GetParameters();

                if (pinfos.Length == 1)
                {
                    Type t0 = pinfos[0].ParameterType;
                    Expression arg0 = node.Arguments[0];
                    if (t0 == typeof(double))
                    {
                        switch (name)
                        {
                            case "minus":
                            case "unchecked_minus":
                                rewrite = Expression.Negate(arg0);
                                return true;
                            case "inc":
                            case "unchecked_inc":
                                rewrite = Expression.Increment(arg0);
                                return true;
                            case "dec":
                            case "unchecked_dec":
                                rewrite = Expression.Decrement(arg0);
                                return true;
                            case "isZero":
                                rewrite = Expression.Equal(arg0, Expression.Constant(0.0));
                                return true;
                            case "isPos":
                                rewrite = Expression.GreaterThan(arg0, Expression.Constant(0.0));
                                return true;
                            case "isNeg":
                                rewrite = Expression.LessThan(arg0, Expression.Constant(0.0));
                                return true;
                        }
                    }
                    else if (t0 == typeof(long))
                    {
                        switch (name)
                        {
                            case "unchecked_minus":
                                rewrite = Expression.Negate(arg0);
                                return true;
                            case "unchecked_inc":
                                rewrite = Expression.Increment(arg0);
                                return true;
                            case "unchecked_dec":
                                rewrite = Expression.Decrement(arg0);
                                return true;
                            case "isZero":
                                rewrite = Expression.Equal(arg0, Expression.Constant(0L));
                                return true;
                            case "isPos":
                                rewrite = Expression.GreaterThan(arg0, Expression.Constant(0L));
                                return true;
                            case "isNeg":
                                rewrite = Expression.LessThan(arg0, Expression.Constant(0L));
                                return true;
                        }
                    }
                    else if (t0 == typeof(int))
                    {
                        switch (name)
                        {
                            case "unchecked_int_negate":
                                rewrite = Expression.Negate(arg0);
                                return true;
                            case "unchecked_int_inc":
                                rewrite = Expression.Increment(arg0);
                                return true;
                            case "unchecked_int_dec":
                                rewrite = Expression.Decrement(arg0);
                                return true;
                        }
                    }
                }
                else if (pinfos.Length == 2 && pinfos[0].ParameterType == pinfos[1].ParameterType)
                {
                    Type t0 = pinfos[0].ParameterType;
                    Expression arg0 = node.Arguments[0];
                    Expression arg1 = node.Arguments[1];
                    if (t0 == typeof(double))
                    {
                        switch (name)
                        {
                            case "add":
                            case "unchecked_add":
                                rewrite = Expression.Add(arg0, arg1);
                                return true;
                            case "multiply":
                            case "unchecked_multiply":
                                rewrite = Expression.Multiply(arg0, arg1);
                                return true;
                            case "divide":
                                rewrite = Expression.Divide(arg0, arg1);
                                return true;
                            case "minus":
                            case "unchecked_minus":
                                rewrite = Expression.Subtract(arg0, arg1);
                                return true;
                            case "lt":
                                rewrite = Expression.LessThan(arg0, arg1);
                                return true;
                            case "lte":
                                rewrite = Expression.LessThanOrEqual(arg0, arg1);
                                return true;
                            case "gt":
                                rewrite = Expression.GreaterThan(arg0, arg1);
                                return true;
                            case "gte":
                                rewrite = Expression.GreaterThanOrEqual(arg0, arg1);
                                return true;
                            case "equiv":
                                rewrite = Expression.Equal(arg0, arg1);
                                return true;
                        }
                    }
                    else if (t0 == typeof(long))
                    {
                        switch (name)
                        {
                            case "and":
                                rewrite = Expression.And(arg0, arg1);
                                return true;
                            case "or":
                                rewrite = Expression.Or(arg0, arg1);
                                return true;
                            case "xor":
                                rewrite = Expression.ExclusiveOr(arg0, arg1);
                                return true;

                            case "remainder":
                                rewrite = Expression.Modulo(arg0, arg1);
                                return true;
                            case "shiftLeft":
                                rewrite = Expression.LeftShift(arg0, Expression.Convert(arg1,typeof(int)));
                                return true;
                            case "shiftRight":
                                rewrite = Expression.RightShift(arg0, Expression.Convert(arg1,typeof(int)));
                                return true;
                            case "quotient":
                                rewrite = Expression.Divide(arg0, arg1);
                                return true;
                            case "unchecked_add":
                                rewrite = Expression.Add(arg0, arg1);
                                return true;
                            case "unchecked_minus":
                                rewrite = Expression.Subtract(arg0, arg1);
                                return true;
                            case "unchecked_multiply":
                                rewrite = Expression.Multiply(arg0, arg1);
                                return true;
                            case "lt":
                                rewrite = Expression.LessThan(arg0, arg1);
                                return true;
                            case "lte":
                                rewrite = Expression.LessThanOrEqual(arg0, arg1);
                                return true;
                            case "gt":
                                rewrite = Expression.GreaterThan(arg0, arg1);
                                return true;
                            case "gte":
                                rewrite = Expression.GreaterThanOrEqual(arg0, arg1);
                                return true;
                            case "equiv":
                                rewrite = Expression.Equal(arg0, arg1);
                                return true;
                        }
                    }
                    else if (t0 == typeof(int))
                    {
                        switch (name)
                        {
                            case "shiftLeftInt":
                                rewrite = Expression.LeftShift(arg0, arg1);
                                return true;
                            case "shiftRightInt":
                                rewrite = Expression.RightShift(arg0, arg1);
                                return true;
                            case "unchecked_int_add":
                                rewrite = Expression.Add(arg0, arg1);
                                return true;
                            case "unchecked_int_subtract":
                                rewrite = Expression.Subtract(arg0, arg1);
                                return true;
                            case "unchecked_int_multiply":
                                rewrite = Expression.Multiply(arg0, arg1);
                                return true;
                            case "unchecked_int_divide":
                                rewrite = Expression.Divide(arg0, arg1);
                                return true;
                            case "unchecked_int_remainder":
                                rewrite = Expression.Modulo(arg0, arg1);
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    
    }
}
