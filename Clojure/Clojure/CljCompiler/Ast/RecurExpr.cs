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
using System.CodeDom;
using System.Reflection.Emit;


namespace clojure.lang.CljCompiler.Ast
{
    public class RecurExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly IPersistentVector _args;
        public IPersistentVector Args { get { return _args; } }

        readonly IPersistentVector _loopLocals;
        public IPersistentVector LoopLocals { get { return _args; } }

        readonly string _source;
        public string Source { get { return _source; } }

        readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }

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
            get { return Recur.RecurType; }
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
                            {
                                RT.errPrintWriter().WriteLine("{0}:{1} recur arg for primitive local: {2} is not matching primitive, had: {3}, needed {4}",
                                    source, spanMap != null ? (int)spanMap.valAt(RT.StartLineKey, 0) : 0,
                                    lb.Name, pt != null ? pt.Name : "Object", primt.Name);
                                RT.errPrintWriter().Flush();
                            }
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

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Label? loopLabel = (Label)Compiler.LoopLabelVar.deref();
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
                            mpeArg.EmitUnboxed(RHC.Expression, objx, ilg);
                        }
                        else if (primt == typeof(long) && pt == typeof(int))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, ilg);
                            ilg.Emit(OpCodes.Conv_I8);
                        }
                        else if (primt == typeof(double) && pt == typeof(float))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, ilg);
                            ilg.Emit(OpCodes.Conv_R8);
                        }
                        else if (primt == typeof(int) && pt == typeof(long))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, ilg);
                            //ilg.EmitCall(Compiler.Method_RT_intCast_long);
                            ilg.Emit(OpCodes.Conv_I4);

                        }
                        else if (primt == typeof(float) && pt == typeof(double))
                        {
                            mpeArg.EmitUnboxed(RHC.Expression, objx, ilg);
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
                        arg.Emit(RHC.Expression, objx, ilg);
                    }

                }
            }
            for (int i = _loopLocals.count() - 1; i >= 0; i--)
            {
                LocalBinding lb = (LocalBinding)_loopLocals.nth(i);
                //Type primt = lb.PrimitiveType;
                if (lb.IsArg)
                {

                    if (lb.DeclaredType == typeof(ISeq) && i == _loopLocals.count() - 1)
                    {
                        // This special case is in response to an odd situation that the JVM accepts and the CLR does not.
                        // See https://clojure.atlassian.net/browse/CLJCLR-113
                        ilg.EmitCall(Compiler.Method_RT_seq);
                    }
                    else if (lb.DeclaredType != typeof(Object) && !lb.DeclaredType.IsPrimitive)
                        ilg.Emit(OpCodes.Castclass, lb.DeclaredType);
                    ilg.EmitStoreArg(lb.Index);
                }
                else
                {
                    ilg.Emit(OpCodes.Stloc, lb.LocalVar);
                }
            }

            ilg.Emit(OpCodes.Br, loopLabel.Value);   
        }

        public bool HasNormalExit() { return false; }

        public bool CanEmitPrimitive
        {
            get { return true; }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Emit(rhc, objx, ilg);
        }

        #endregion
    }

    public static class Recur
    {
        public static readonly Type RecurType = typeof(Recur);
    }
}
