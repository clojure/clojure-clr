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
using System.Reflection.Emit;

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

        public bool HasClrType
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

        public Type ClrType
        {
            get { return _thenExpr.ClrType ?? _elseExpr.ClrType; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq)frm;

                // (if test then) or (if test then else)

                if (form.count() > 4)
                    throw new ParseException("Too many arguments to if");

                if (form.count() < 3)
                    throw new ParseException("Too few arguments to if");


                Expr testExpr = Compiler.Analyze(pcon.EvalOrExpr().SetAssign(false),RT.second(form));
                Expr thenExpr = Compiler.Analyze(pcon.SetAssign(false), RT.third(form));
                Expr elseExpr = Compiler.Analyze(pcon.SetAssign(false), RT.fourth(form));

                return new IfExpr((IPersistentMap)Compiler.SourceSpanVar.deref(), testExpr, thenExpr, elseExpr);
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            Object t = _testExpr.Eval();
            if (RT.booleanCast(t))
                return _thenExpr.Eval();
            return _elseExpr.Eval();
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return GenCode(rhc, objx, context, false);
        }

        private Expression GenCode(RHC rhc, ObjExpr objx, GenContext context, bool genUnboxed)
        {
            bool testIsBool = Compiler.MaybePrimitiveType(_testExpr) == typeof(bool);

            Expression testCode;

            if (testIsBool)
                testCode = ((MaybePrimitiveExpr)_testExpr).GenCodeUnboxed(RHC.Expression,objx,context);
            else
            {
                ParameterExpression testVar = Expression.Parameter(typeof(object), "__test");
                Expression assign = Expression.Assign(testVar, Compiler.MaybeBox(_testExpr.GenCode(RHC.Expression,objx, context)));
                Expression boolExpr =
                    Expression.Not(
                        Expression.OrElse(
                            Expression.Equal(testVar, Expression.Constant(null)),
                            Expression.AndAlso(Expression.TypeIs(testVar, typeof(bool)), Expression.IsFalse(Expression.Unbox(testVar, typeof(bool))))));
                testCode = Expression.Block(typeof(bool), new ParameterExpression[] { testVar }, assign, boolExpr);
            }

            Expression thenCode = genUnboxed ? ((MaybePrimitiveExpr)_thenExpr).GenCodeUnboxed(rhc, objx, context) : _thenExpr.GenCode(rhc, objx, context);

            Expression elseCode = genUnboxed ? ((MaybePrimitiveExpr)_elseExpr).GenCodeUnboxed(rhc, objx, context) : _elseExpr.GenCode(rhc, objx, context);

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
                    // TODO: Can we find a common ancestor?  probably not.
                    thenCode = Expression.Convert(thenCode, typeof(object));
                    elseCode = Expression.Convert(elseCode, typeof(object));
                    targetType = typeof(object);
                }
            }

            Expression cond = Expression.Condition(testCode, thenCode, elseCode, targetType);
            cond = Compiler.MaybeAddDebugInfo(cond, _sourceSpan, context.IsDebuggable);
            return cond;
        }

        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            DoEmit(rhc, objx, context, false);
        }

        void DoEmit(RHC rhc, ObjExpr objx, GenContext context, bool emitUnboxed)
        {
            ILGenerator ilg = context.GetILGenerator();
            Label nullLabel = ilg.DefineLabel();
            Label falseLabel = ilg.DefineLabel();
            Label endLabel = ilg.DefineLabel();
            Label trueLabel = ilg.DefineLabel();

            Compiler.MaybeEmitDebugInfo(context, ilg, _sourceSpan);

            StaticMethodExpr sme = _testExpr as StaticMethodExpr;
            if (sme != null && sme.CanEmitIntrinsicPredicate())
                sme.EmitIntrinsicPredicate(RHC.Expression, objx, context, falseLabel);
            else if (Compiler.MaybePrimitiveType(_testExpr) == typeof(bool))
            {
                ((MaybePrimitiveExpr)_testExpr).EmitUnboxed(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Brfalse, falseLabel);
            }
            else
            {
                LocalBuilder tempLoc = ilg.DeclareLocal(typeof(Object));
                Compiler.MaybeSetLocalSymName(context, tempLoc, "test");

                _testExpr.Emit(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Stloc, tempLoc);

                ilg.Emit(OpCodes.Brfalse, nullLabel);

                ilg.Emit(OpCodes.Ldloc, tempLoc);
                ilg.Emit(OpCodes.Isinst, typeof(bool));
                ilg.Emit(OpCodes.Ldnull);
                ilg.Emit(OpCodes.Cgt_Un);
                ilg.Emit(OpCodes.Brfalse, trueLabel);

                ilg.Emit(OpCodes.Ldloc, tempLoc);
                ilg.Emit(OpCodes.Unbox_Any, typeof(bool));
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Ceq);
                ilg.Emit(OpCodes.Brtrue, falseLabel);
            }

            ilg.MarkLabel(trueLabel);

            if (emitUnboxed)
                ((MaybePrimitiveExpr)_thenExpr).EmitUnboxed(rhc, objx, context);
            else
                _thenExpr.Emit(rhc, objx, context);


            if ( _thenExpr.HasNormalExit() )
                ilg.Emit(OpCodes.Br, endLabel);

            ilg.MarkLabel(nullLabel);
            ilg.MarkLabel(falseLabel);

            if (emitUnboxed)
                ((MaybePrimitiveExpr)_elseExpr).EmitUnboxed(rhc, objx, context);
            else
                _elseExpr.Emit(rhc, objx, context);
            ilg.MarkLabel(endLabel);
        }

        public bool HasNormalExit() { return _thenExpr.HasNormalExit() || _elseExpr.HasNormalExit(); }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
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

        public Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            return GenCode(rhc, objx, context, true);
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            DoEmit(rhc, objx, context, true);
        }


        #endregion
    }
}
