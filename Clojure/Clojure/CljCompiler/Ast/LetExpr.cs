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
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class LetExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly IPersistentVector _bindingInits;
        public IPersistentVector BindingInits { get { return _bindingInits; } }

        readonly Expr _body;
        public Expr Body { get { return _body; } }
        
        readonly bool _isLoop;
        public bool IsLoop { get { return _isLoop; } }

        #endregion

        #region Ctors

        public LetExpr(IPersistentVector bindingInits, Expr body, bool isLoop)
        {
            _bindingInits = bindingInits;
            _body = body;
            _isLoop = isLoop;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _body.HasClrType; }
        }

        public Type ClrType
        {
            get { return _body.ClrType; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq)frm;

                // form => (let  [var1 val1 var2 val2 ... ] body ... )
                //      or (loop [var1 val1 var2 val2 ... ] body ... )

                bool isLoop = RT.first(form).Equals(Compiler.LoopSym);

                if (!(RT.second(form) is IPersistentVector bindings))
                    throw new ParseException("Bad binding form, expected vector");

                if ((bindings.count() % 2) != 0)
                    throw new ParseException("Bad binding form, expected matched symbol/value pairs.");

                ISeq body = RT.next(RT.next(form));

                if (pcon.Rhc == RHC.Eval
                    || (pcon.Rhc == RHC.Expression && isLoop))
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FnOnceSym, PersistentVector.EMPTY, form)), "let__" + RT.nextID());

                ObjMethod method = (ObjMethod)Compiler.MethodVar.deref();
                IPersistentMap backupMethodLocals = method.Locals;
                IPersistentMap backupMethodIndexLocals = method.IndexLocals;

                IPersistentVector recurMismatches = PersistentVector.EMPTY;
                for (int i = 0; i < bindings.count() / 2; i++)
                {
                    recurMismatches = recurMismatches.cons(false);
                }

                // may repeat once for each binding with a mismatch, return breaks
                while (true)
                {

                    IPersistentMap dynamicBindings = RT.map(
                        Compiler.LocalEnvVar, Compiler.LocalEnvVar.deref(),
                        Compiler.NextLocalNumVar, Compiler.NextLocalNumVar.deref());
                    method.SetLocals(backupMethodLocals, backupMethodIndexLocals);

                    if (isLoop)
                        dynamicBindings = dynamicBindings.assoc(Compiler.LoopLocalsVar, null);

                    try
                    {
                        Var.pushThreadBindings(dynamicBindings);

                        IPersistentVector bindingInits = PersistentVector.EMPTY;
                        IPersistentVector loopLocals = PersistentVector.EMPTY;

                        for (int i = 0; i < bindings.count(); i += 2)
                        {
                            if (!(bindings.nth(i) is Symbol))
                                throw new ParseException("Bad binding form, expected symbol, got " + bindings.nth(i));

                            Symbol sym = (Symbol)bindings.nth(i);
                            if (sym.Namespace != null)
                                throw new ParseException("Can't let qualified name: " + sym);

                            Expr init = Compiler.Analyze(pcon.SetRhc(RHC.Expression).SetAssign(false), bindings.nth(i + 1), sym.Name);
                            if (isLoop)
                            {
                                if (recurMismatches != null && RT.booleanCast(recurMismatches.nth(i / 2)) )
                                {
                                    HostArg ha = new HostArg(HostArg.ParameterType.Standard, init, null);
                                    List<HostArg> has = new List<HostArg>(1)
                                    {
                                        ha
                                    };
                                    init = new StaticMethodExpr("", PersistentArrayMap.EMPTY, null, typeof(RT), "box", null, has, false);
                                    if (RT.booleanCast(RT.WarnOnReflectionVar.deref()))
                                    {
                                        RT.errPrintWriter().WriteLine("Auto-boxing loop arg: " + sym);
                                        RT.errPrintWriter().Flush();
                                    }
                                }
                                else if (Compiler.MaybePrimitiveType(init) == typeof(int))
                                {
                                    List<HostArg> args = new List<HostArg>
                                    {
                                        new HostArg(HostArg.ParameterType.Standard, init, null)
                                    };
                                    init = new StaticMethodExpr("", null, null, typeof(RT), "longCast", null, args, false);
                                }
                                else if (Compiler.MaybePrimitiveType(init) == typeof(float))
                                {
                                    List<HostArg> args = new List<HostArg>
                                    {
                                        new HostArg(HostArg.ParameterType.Standard, init, null)
                                    };
                                    init = new StaticMethodExpr("", null, null, typeof(RT), "doubleCast", null, args, false);
                                }
                            }

                            // Sequential enhancement of env (like Lisp let*)
                            LocalBinding b = Compiler.RegisterLocal(sym, Compiler.TagOf(sym), init, typeof(Object), false);
                            BindingInit bi = new BindingInit(b, init);
                            bindingInits = bindingInits.cons(bi);

                            if (isLoop)
                                loopLocals = loopLocals.cons(b);
                        }
                        if (isLoop)
                            Compiler.LoopLocalsVar.set(loopLocals);

                        Expr bodyExpr;
                        bool moreMismatches = false;
                        try
                        {
                            if (isLoop)
                            {
                                object methodReturnContext = pcon.Rhc == RHC.Return ? Compiler.MethodReturnContextVar.deref() : null; 
                                // stuff with clear paths,
                                Var.pushThreadBindings(RT.map(Compiler.NoRecurVar, null,
                                                              Compiler.MethodReturnContextVar, methodReturnContext));
                            }
                            bodyExpr = new BodyExpr.Parser().Parse(isLoop ? pcon.SetRhc(RHC.Return) : pcon, body);
                        }
                        finally
                        {
                            if (isLoop)
                            {
                                Var.popThreadBindings();

                                for (int i = 0; i < loopLocals.count(); i++)
                                {
                                    LocalBinding lb = (LocalBinding)loopLocals.nth(i);
                                    if (lb.RecurMismatch)
                                    {
                                        recurMismatches = (IPersistentVector)recurMismatches.assoc(i, true);
                                        moreMismatches = true;
                                    }
                                }
                            }
                        }

                        if (!moreMismatches)

                            return new LetExpr(bindingInits, bodyExpr, isLoop);

                    }
                    finally
                    {
                        Var.popThreadBindings();
                    }
                }
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval let/loop");
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            DoEmit(rhc, objx, ilg, false);
        }

        void DoEmit(RHC rhc, ObjExpr objx, CljILGen ilg, bool emitUnboxed)
        {
            List<LocalBuilder> locals = new List<LocalBuilder>();
            
            for (int i = 0; i < _bindingInits.count(); i++)
            {
                BindingInit bi = (BindingInit)_bindingInits.nth(i);
                Type primType = Compiler.MaybePrimitiveType(bi.Init);
                if (primType != null)
                {
                    LocalBuilder local = ilg.DeclareLocal(primType);
                    locals.Add(local);
                    GenContext.SetLocalName(local, bi.Binding.Name);
                    bi.Binding.LocalVar = local;
                    
                    ((MaybePrimitiveExpr)bi.Init).EmitUnboxed(RHC.Expression, objx, ilg);
                    ilg.Emit(OpCodes.Stloc, local);
                }
                else
                {
                    LocalBuilder local = ilg.DeclareLocal(typeof(Object));
                    locals.Add(local);
                    GenContext.SetLocalName(local, bi.Binding.Name);
                    bi.Binding.LocalVar = local;

                    bi.Init.Emit(RHC.Expression, objx, ilg);
                    ilg.Emit(OpCodes.Stloc, local);
                }
             }

            Label loopLabel = ilg.DefineLabel();
            ilg.MarkLabel(loopLabel);

            try
            {
                if (_isLoop)
                    Var.pushThreadBindings(PersistentHashMap.create(Compiler.LoopLabelVar, loopLabel));

                if (emitUnboxed)
                    ((MaybePrimitiveExpr)_body).EmitUnboxed(rhc, objx, ilg);
                else
                    _body.Emit(rhc, objx, ilg);
            }
            finally
            {
                if (_isLoop)
                    Var.popThreadBindings();
            }
        }

        public bool HasNormalExit() { return _body.HasNormalExit(); }


        public bool CanEmitPrimitive
        {
            get { return _body is MaybePrimitiveExpr expr && expr.CanEmitPrimitive; }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            DoEmit(rhc, objx, ilg, true);
        }

        #endregion
    }
}
