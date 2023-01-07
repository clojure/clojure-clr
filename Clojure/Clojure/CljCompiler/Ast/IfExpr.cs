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
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class IfExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly IPersistentMap _sourceSpan;
        public IPersistentMap SourceSpan { get { return _sourceSpan; } } 

        readonly Expr _testExpr;
        public Expr TestExpr { get { return _testExpr; } }

        readonly Expr _thenExpr;
        public Expr ThenExpr { get { return _thenExpr; } }
        
        readonly Expr _elseExpr;
        public Expr ElseExpr { get { return _elseExpr; } }

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
                    || _thenExpr.ClrType == Recur.RecurType
                    || _elseExpr.ClrType == Recur.RecurType
                    || (_thenExpr.ClrType == null && !_elseExpr.ClrType.IsValueType)
                    || (_elseExpr.ClrType == null && !_thenExpr.ClrType.IsValueType));
            }
        }

        public Type ClrType
        {
            get
            {
                Type thenType = _thenExpr.ClrType;
                if (thenType != null && thenType != Recur.RecurType)
                    return thenType;
                return _elseExpr.ClrType;
            }
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

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            DoEmit(rhc, objx, ilg, false);
        }

        void DoEmit(RHC rhc, ObjExpr objx, CljILGen ilg, bool emitUnboxed)
        {
            Label nullLabel = ilg.DefineLabel();
            Label falseLabel = ilg.DefineLabel();
            Label endLabel = ilg.DefineLabel();
            Label trueLabel = ilg.DefineLabel();

            GenContext.EmitDebugInfo(ilg, _sourceSpan);

            if (_testExpr is StaticMethodExpr sme && sme.CanEmitIntrinsicPredicate())
                sme.EmitIntrinsicPredicate(RHC.Expression, objx, ilg, falseLabel);
            else if (Compiler.MaybePrimitiveType(_testExpr) == typeof(bool))
            {
                ((MaybePrimitiveExpr)_testExpr).EmitUnboxed(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Brfalse, falseLabel);
            }
            else
            {
                LocalBuilder tempLoc = ilg.DeclareLocal(typeof(Object));
                GenContext.SetLocalName(tempLoc, "test");

                _testExpr.Emit(RHC.Expression, objx, ilg);
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
                ((MaybePrimitiveExpr)_thenExpr).EmitUnboxed(rhc, objx, ilg);
            else
                _thenExpr.Emit(rhc, objx, ilg);


            if ( _thenExpr.HasNormalExit() )
                ilg.Emit(OpCodes.Br, endLabel);

            ilg.MarkLabel(nullLabel);
            ilg.MarkLabel(falseLabel);

            if (emitUnboxed)
                ((MaybePrimitiveExpr)_elseExpr).EmitUnboxed(rhc, objx, ilg);
            else
                _elseExpr.Emit(rhc, objx, ilg);
            ilg.MarkLabel(endLabel);
        }

        public bool HasNormalExit() { return _thenExpr.HasNormalExit() || _elseExpr.HasNormalExit(); }

        public bool CanEmitPrimitive
        {
            get 
            {
                try
                {
                    return _thenExpr is MaybePrimitiveExpr tExpr
                        && _elseExpr is MaybePrimitiveExpr eExpr
                        && _thenExpr.ClrType == _elseExpr.ClrType
                        && tExpr.CanEmitPrimitive
                        && eExpr.CanEmitPrimitive;
                }
                catch ( Exception )
                {
                    return false;
                }
            }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            DoEmit(rhc, objx, ilg, true);
        }

        #endregion
    }
}
