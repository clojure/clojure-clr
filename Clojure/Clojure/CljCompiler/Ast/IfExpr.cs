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

using clojure.runtime;

namespace clojure.lang.CljCompiler.Ast
{
    class IfExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly IPersistentMap _sourceSpan;
        readonly Expr _testExpr;
        readonly Expr _thenExpr;
        readonly Expr _elseExpr;

        #endregion

        #region Ctors

        public IfExpr( IPersistentMap sourceSpan, Expr testExpr, Expr thenExpr, Expr elseExpr)
        {
            _sourceSpan = sourceSpan;
            _testExpr = testExpr;
            _thenExpr = thenExpr;
            _elseExpr = elseExpr;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get
            {
                return _thenExpr.HasClrType
                && _elseExpr.HasClrType
                && (_thenExpr.ClrType == _elseExpr.ClrType
                    || (_thenExpr.ClrType == null && !_elseExpr.ClrType.IsValueType)
                    || (_elseExpr.ClrType == null && !_thenExpr.ClrType.IsValueType));
            }
        }

        public override Type ClrType
        {
            get { return _thenExpr.ClrType ?? _elseExpr.ClrType; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(object frm, ParserContext pcon)
            {
                ISeq form = (ISeq)frm;

                // (if test then) or (if test then else)

                if (form.count() > 4)
                    throw new Exception("Too many arguments to if");

                if (form.count() < 3)
                    throw new Exception("Too few arguments to if");


                Expr testExpr = Compiler.GenerateAST(RT.second(form), pcon.SetRecur(false).SetAssign(false));
                Expr thenExpr = Compiler.GenerateAST(RT.third(form), pcon.SetAssign(false));
                //Expr elseExpr = form.count() == 4 ? Compiler.GenerateAST(RT.fourth(form), pcon.SetAssign(false)) : null;
                Expr elseExpr = Compiler.GenerateAST(RT.fourth(form), pcon.SetAssign(false));

                return new IfExpr((IPersistentMap)Compiler.SOURCE_SPAN.deref(), testExpr, thenExpr, elseExpr);
            }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            return GenDlr(context, false);
        }

        private Expression GenDlr(GenContext context, bool genUnboxed)
        {
            bool testIsBool = Compiler.MaybePrimitiveType(_testExpr) == typeof(bool);

            Expression testCode;

            if (testIsBool)
                testCode = ((MaybePrimitiveExpr)_testExpr).GenDlrUnboxed(context);
            else
            {
                ParameterExpression testVar = Expression.Parameter(typeof(object), "__test");
                Expression assign = Expression.Assign(testVar, Compiler.MaybeBox(_testExpr.GenDlr(context)));
                Expression boolExpr =
                    Expression.Not(
                        Expression.OrElse(
                            Expression.Equal(testVar, Expression.Constant(null)),
                            Expression.AndAlso(Expression.TypeIs(testVar, typeof(bool)), Expression.IsFalse(Expression.Unbox(testVar, typeof(bool))))));
                testCode = Expression.Block(typeof(bool), new ParameterExpression[] { testVar }, assign, boolExpr);
            }

            Expression thenCode = genUnboxed ? ((MaybePrimitiveExpr)_thenExpr).GenDlrUnboxed(context) : _thenExpr.GenDlr(context);

            Expression elseCode = genUnboxed ? ((MaybePrimitiveExpr)_elseExpr).GenDlrUnboxed(context) : _elseExpr.GenDlr(context);

            Type targetType = typeof(object);
            if (this.HasClrType && this.ClrType != null)
                // In this case, both _thenExpr and _elseExpr have types, and they are the same, or one is null.
                // TODO: Not sure if this works if one has a null value.
                targetType = this.ClrType;

            if (thenCode.Type == typeof(void) && elseCode.Type != typeof(void))
                thenCode = Expression.Block(thenCode, Expression.Default(elseCode.Type));
            else if (elseCode.Type == typeof(void) && thenCode.Type != typeof(void))
                elseCode = Expression.Block(elseCode, Expression.Default(thenCode.Type));
            else if (!Reflector.AreReferenceAssignable(targetType, thenCode.Type) || !Reflector.AreReferenceAssignable(targetType, elseCode.Type))
            // Above: this is the test that Expression.Condition does.
            {
                // Try to reconcile
                if (thenCode.Type.IsAssignableFrom(elseCode.Type) && elseCode.Type != typeof(void))
                {
                    elseCode = Expression.Convert(elseCode, thenCode.Type);
                    targetType = thenCode.Type;
                }
                else if (elseCode.Type.IsAssignableFrom(thenCode.Type) && thenCode.Type != typeof(void))
                {
                    thenCode = Expression.Convert(thenCode, elseCode.Type);
                    targetType = elseCode.Type;
                }
                else
                {
                    //if (thenCode.Type == typeof(void))
                    //{
                    //    thenCode = Expression.Block(thenCode, Expression.Default(elseCode.Type));
                    //    targetType = elseCode.Type;
                    //}
                    //else if (elseCode.Type == typeof(void))
                    //{
                    //    elseCode = Expression.Block(elseCode, Expression.Default(thenCode.Type));
                    //    targetType = thenCode.Type;
                    //}
                    //else
                    //{
                    // TODO: Can we find a common ancestor?  probably not.
                    thenCode = Expression.Convert(thenCode, typeof(object));
                    elseCode = Expression.Convert(elseCode, typeof(object));
                    targetType = typeof(object);
                    //}
                }
            }

            Expression cond = Expression.Condition(testCode, thenCode, elseCode, targetType);
            cond = Compiler.MaybeAddDebugInfo(cond, _sourceSpan);
            return cond;
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get 
            {
                try
                {
                    return _thenExpr is MaybePrimitiveExpr
                        && _elseExpr is MaybePrimitiveExpr
                        && _thenExpr.ClrType == _elseExpr.ClrType
                        && ((MaybePrimitiveExpr)_thenExpr).CanEmitPrimitive
                        && ((MaybePrimitiveExpr)_elseExpr).CanEmitPrimitive;
                }
                catch ( Exception )
                {
                    return false;
                }
            }
        }

        public Expression GenDlrUnboxed(GenContext context)
        {
            return GenDlr(context, true);
        }

        #endregion
    }
}
