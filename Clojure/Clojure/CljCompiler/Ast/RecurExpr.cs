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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection.Emit;
using Microsoft.Scripting.Generation;


namespace clojure.lang.CljCompiler.Ast
{
    class RecurExpr : Expr
    {
        #region Data

        readonly IPersistentVector _args;
        readonly IPersistentVector _loopLocals;
        readonly string _source;
        readonly IPersistentMap _spanMap;

        #endregion

        #region Ctors

        public RecurExpr(string source, IPersistentMap spanMap, IPersistentVector loopLocals, IPersistentVector args)
        {
            _loopLocals = loopLocals;
            _args = args;
            _source = source;
            _spanMap = spanMap;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return null; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                string source = (string)Compiler.SourceVar.deref();
                IPersistentMap spanMap = (IPersistentMap)Compiler.SourceSpanVar.deref();  // Compiler.GetSourceSpanMap(form);
                
                ISeq form = (ISeq)frm;

                IPersistentVector loopLocals = (IPersistentVector)Compiler.LoopLocalsVar.deref();

                if (pcon.Rhc != RHC.Return || loopLocals == null)
                    throw new ParseException("Can only recur from tail position");

                if (Compiler.InCatchFinallyVar.deref() != null)
                    throw new ParseException("Cannot recur from catch/finally.");

                if (Compiler.NoRecurVar.deref() != null)
                    throw new ParseException("Cannot recur across try");

                IPersistentVector args = PersistentVector.EMPTY;

                for (ISeq s = RT.seq(form.next()); s != null; s = s.next())
                    args = args.cons(Compiler.Analyze(pcon.SetRhc(RHC.Expression).SetAssign(false), s.first()));
                if (args.count() != loopLocals.count())
                    throw new ParseException(string.Format("Mismatched argument count to recur, expected: {0} args, got {1}",
                        loopLocals.count(), args.count()));

                for (int i = 0; i < loopLocals.count(); i++)
                {
                    LocalBinding lb = (LocalBinding)loopLocals.nth(i);
                    Type primt = lb.PrimitiveType;
                    if (primt != null)
                    {
                        bool mismatch = false;
                        Type pt = Compiler.MaybePrimitiveType((Expr)args.nth(i));
                        if (primt == typeof(long))
                        {
                            if (!(pt == typeof(long) || pt == typeof(int) || pt == typeof(short) || pt == typeof(uint) || pt == typeof(ushort) || pt == typeof(ulong)
                                || pt == typeof(char) || pt == typeof(byte) || pt == typeof(sbyte)))
                                mismatch = true;
                        }
                        else if (primt == typeof(double))
                        {
                            if (!(pt == typeof(double) || pt == typeof(float)))
                                mismatch = true;
                        }

                        if (mismatch)
                        {
                            lb.RecurMismatch = true;
                            if (RT.booleanCast(RT.WarnOnReflectionVar.deref()))
                                RT.errPrintWriter().WriteLine("{0}:{1} recur arg for primitive local: {2} is not matching primitive, had: {3}, needed {4}",
                                    source, spanMap != null ? (int)spanMap.valAt(RT.StartLineKey, 0) : 0,
                                    lb.Name, pt != null ? pt.Name : "Object", primt.Name);
                        }
                    }
                }

 
                return new RecurExpr(source, spanMap, loopLocals, args);
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval recur");
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            LabelTarget loopLabel = (LabelTarget)Compiler.LoopLabelVar.deref();
            if (loopLabel == null)
                throw new InvalidOperationException("Recur not in proper context.");

            int argCount = _args.count();

            List<ParameterExpression> tempVars = new List<ParameterExpression>(argCount);
            List<Expression> tempAssigns = new List<Expression>(argCount);
            List<Expression> finalAssigns = new List<Expression>(argCount);

            // Evaluate all the init forms into local variables.
            for (int i = 0; i < _loopLocals.count(); i++)
            {
                LocalBinding lb = (LocalBinding)_loopLocals.nth(i);
                Expr arg = (Expr)_args.nth(i);

                ParameterExpression tempVar;
                Expression valExpr;

                Type primt = lb.PrimitiveType;
                if (primt != null)
                {
                    tempVar = Expression.Parameter(primt, "__local__" + i);

                    MaybePrimitiveExpr mpeArg = arg as MaybePrimitiveExpr;
                    Type pt = Compiler.MaybePrimitiveType(arg);
                    if (pt == primt)
                    {
                        valExpr = mpeArg.GenCodeUnboxed(RHC.Expression, objx, context);
                        // do nothing
                    }
                    else if (primt == typeof(long) && pt == typeof(int))
                    {
                        valExpr = mpeArg.GenCodeUnboxed(RHC.Expression, objx, context);
                        valExpr = Expression.Convert(valExpr, primt);
                    }
                    else if (primt == typeof(double) && pt == typeof(float))
                    {
                        valExpr = mpeArg.GenCodeUnboxed(RHC.Expression, objx, context);
                        valExpr = Expression.Convert(valExpr, primt);
                    }
                    else if (primt == typeof(int) && pt == typeof(long))
                    {
                        valExpr = mpeArg.GenCodeUnboxed(RHC.Expression, objx, context);
                        valExpr = Expression.Convert(valExpr, primt);
                    }
                    else if (primt == typeof(float) && pt == typeof(double))
                    {
                        valExpr = mpeArg.GenCodeUnboxed(RHC.Expression, objx, context);
                        valExpr = Expression.Convert(valExpr, primt);
                    }
                    else
                    {
                        //if (true) //RT.booleanCast(RT.WARN_ON_REFLECTION.deref()))
                            //RT.errPrintWriter().WriteLine
                        throw new ArgumentException(String.Format(
                                "{0}:{1} recur arg for primitive local: {2} is not matching primitive, had: {3}, needed {4}",
                                _source, _spanMap != null ? (int)_spanMap.valAt(RT.StartLineKey, 0) : 0, 
                                lb.Name, (arg.HasClrType ? arg.ClrType.Name : "Object"), primt.Name));
                        //valExpr = arg.GenCode(RHC.Expression, objx, context);
                       // valExpr = Expression.Convert(valExpr, primt);
                    }

                }
                else
                {
                    tempVar = Expression.Parameter(lb.ParamExpression.Type, "__local__" + i);
                    valExpr = arg.GenCode(RHC.Expression, objx, context);
                }
                    

                //ParameterExpression tempVar = Expression.Parameter(lb.ParamExpression.Type, "__local__" + i);
                //Expression valExpr = ((Expr)_args.nth(i)).GenCode(RHC.Expression, objx, context);
                tempVars.Add(tempVar);

                //if (tempVar.Type == typeof(Object))
                //    tempAssigns.Add(Expression.Assign(tempVar, Compiler.MaybeBox(valExpr)));
                //else
                //    tempAssigns.Add(Expression.Assign(tempVar, Expression.Convert(valExpr, tempVar.Type)));
                if (valExpr.Type.IsPrimitive && !tempVar.Type.IsPrimitive)
                    tempAssigns.Add(Expression.Assign(tempVar, Compiler.MaybeBox(valExpr)));
                else if (!valExpr.Type.IsPrimitive && tempVar.Type.IsPrimitive)
                    tempAssigns.Add(Expression.Assign(tempVar, HostExpr.GenUnboxArg(valExpr, tempVar.Type)));
                else
                    tempAssigns.Add(Expression.Assign(tempVar, valExpr));

                finalAssigns.Add(Expression.Assign(lb.ParamExpression, tempVar));
            }

            List<Expression> exprs = tempAssigns;
            exprs.AddRange(finalAssigns);
            exprs.Add(Expression.Goto(loopLabel));
            // need to do this to get a return value in the type inferencing -- else can't use this in a then or else clause.
            exprs.Add(Expression.Constant(null));
            return Expression.Block(tempVars, exprs);
        }

        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGen ilg = context.GetILGen();

            Label loopLabel = (Label)Compiler.LoopLabelVar.deref();
            if (loopLabel == null)
                throw new InvalidOperationException("Recur not in proper context.");

            {
                for (int i = 0; i < _loopLocals.count(); i++)
                {
                    LocalBinding lb = (LocalBinding)_loopLocals.nth(i);
                    Expr arg = (Expr)_args.nth(i);

                    Type primt = lb.PrimitiveType;
                    if (primt != null)
                    {
                        MaybePrimitiveExpr mpeArg = arg as MaybePrimitiveExpr;
                        Type pt = Compiler.MaybePrimitiveType(arg);
                        if (pt == primt)
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, context);
                        }
                        else if (primt == typeof(long) && pt == typeof(int))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, context);
                            ilg.Emit(OpCodes.Conv_I8);
                        }
                        else if (primt == typeof(double) && pt == typeof(float))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, context);
                            ilg.Emit(OpCodes.Conv_R8);
                        }
                        else if (primt == typeof(int) && pt == typeof(long))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, context);
                            ilg.EmitCall(Compiler.Method_RT_intCast_long);

                        }
                        else if (primt == typeof(float) && pt == typeof(double))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, context);
                            ilg.Emit(OpCodes.Conv_R4);
                        }
                        else
                        {
                            throw new ArgumentException(String.Format(
                                    "{0}:{1} recur arg for primitive local: {2} is not matching primitive, had: {3}, needed {4}",
                                    _source, _spanMap != null ? (int)_spanMap.valAt(RT.StartLineKey, 0) : 0,
                                    lb.Name, (arg.HasClrType ? arg.ClrType.Name : "Object"), primt.Name));
                        }

                    }
                    else
                    {
                        arg.Emit(RHC.Expression, objx, context);
                    }

                }
            }
            for (int i = _loopLocals.count() - 1; i >= 0; i--)
            {
                LocalBinding lb = (LocalBinding)_loopLocals.nth(i);
                Type primt = lb.PrimitiveType;
                if (lb.IsArg)
                    //ilg.Emit(OpCodes.Starg, lb.Index - (objx.IsStatic ? 0 : 1));
                    ilg.Emit(OpCodes.Starg, lb.Index);
                else
                {
                    ilg.Emit(OpCodes.Stloc, lb.LocalVar);
                }
            }

            ilg.Emit(OpCodes.Br, loopLabel);
            if ( rhc !=  RHC.Statement )
                ilg.Emit(OpCodes.Ldnull);       
        }

        public bool HasThrowLast() { return false; }


        #endregion
    }
}
