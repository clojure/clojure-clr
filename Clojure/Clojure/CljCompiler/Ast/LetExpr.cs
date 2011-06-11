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

namespace clojure.lang.CljCompiler.Ast
{
    class LetExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly IPersistentVector _bindingInits;
        readonly Expr _body;
        readonly bool _isLoop;

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

                bool isLoop = RT.first(form).Equals(Compiler.LOOP);

                IPersistentVector bindings = RT.second(form) as IPersistentVector;

                if (bindings == null)
                    throw new ArgumentException("Bad binding form, expected vector");

                if ((bindings.count() % 2) != 0)
                    throw new ArgumentException("Bad binding form, expected matched symbol/value pairs.");

                ISeq body = RT.next(RT.next(form));

                if (pcon.Rhc == RHC.Eval
                    || (pcon.Rhc == RHC.Expression && isLoop))
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FN, PersistentVector.EMPTY, form)), "let__" + RT.nextID());

                ObjMethod method = (ObjMethod)Compiler.METHOD.deref();
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
                        Compiler.LOCAL_ENV, Compiler.LOCAL_ENV.deref(),
                        Compiler.NEXT_LOCAL_NUM, Compiler.NEXT_LOCAL_NUM.deref());
                    method.Locals = backupMethodLocals;
                    method.IndexLocals = backupMethodIndexLocals;


                    if (isLoop)
                        dynamicBindings = dynamicBindings.assoc(Compiler.LOOP_LOCALS, null);

                    try
                    {
                        Var.pushThreadBindings(dynamicBindings);

                        IPersistentVector bindingInits = PersistentVector.EMPTY;
                        IPersistentVector loopLocals = PersistentVector.EMPTY;

                        for (int i = 0; i < bindings.count(); i += 2)
                        {
                            if (!(bindings.nth(i) is Symbol))
                                throw new ArgumentException("Bad binding form, expected symbol, got " + bindings.nth(i));

                            Symbol sym = (Symbol)bindings.nth(i);
                            if (sym.Namespace != null)
                                throw new Exception("Can't let qualified name: " + sym);

                            Expr init = Compiler.Analyze(pcon.SetRhc(RHC.Expression).SetAssign(false), bindings.nth(i + 1), sym.Name);
                            if (isLoop)
                            {
                                if (recurMismatches != null && RT.booleanCast(recurMismatches.nth(i / 2)) )
                                {
                                    HostArg ha = new HostArg(HostArg.ParameterType.Standard, init, null);
                                    List<HostArg> has = new List<HostArg>(1);
                                    has.Add(ha);
                                    init = new StaticMethodExpr("", PersistentArrayMap.EMPTY, null, typeof(RT), "box", null, has);
                                    if (RT.booleanCast(RT.WARN_ON_REFLECTION.deref()))
                                        RT.errPrintWriter().WriteLine("Auto-boxing loop arg: " + sym);
                                }
                                else if (Compiler.MaybePrimitiveType(init) == typeof(int))
                                {
                                    List<HostArg> args = new List<HostArg>();
                                    args.Add(new HostArg(HostArg.ParameterType.Standard, init, null));
                                    init = new StaticMethodExpr("", null, null, typeof(RT), "longCast", null, args);
                                }
                                else if (Compiler.MaybePrimitiveType(init) == typeof(float))
                                {
                                    List<HostArg> args = new List<HostArg>();
                                    args.Add(new HostArg(HostArg.ParameterType.Standard, init, null));
                                    init = new StaticMethodExpr("", null, null, typeof(RT), "doubleCast", null, args);
                                }
                            }

                            // Sequential enhancement of env (like Lisp let*)
                            LocalBinding b = Compiler.RegisterLocal(sym, Compiler.TagOf(sym), init, false);
                            BindingInit bi = new BindingInit(b, init);
                            bindingInits = bindingInits.cons(bi);

                            if (isLoop)
                                loopLocals = loopLocals.cons(b);
                        }
                        if (isLoop)
                            Compiler.LOOP_LOCALS.set(loopLocals);

                        Expr bodyExpr;
                        bool moreMismatches = false;
                        try
                        {
                            if (isLoop)
                            {
                                // stuff with clear paths,
                                Var.pushThreadBindings(RT.map(Compiler.NO_RECUR, null));
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

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return GenCode(rhc, objx, context, false);
        }

        private Expression GenCode(RHC rhc, ObjExpr objx, GenContext context, bool genUnboxed)
        {
            LabelTarget loopLabel = Expression.Label();

            List<ParameterExpression> parms = new List<ParameterExpression>();
            List<Expression> forms = new List<Expression>();

            for (int i = 0; i < _bindingInits.count(); i++)
            {
                BindingInit bi = (BindingInit)_bindingInits.nth(i);
                Type primType = Compiler.MaybePrimitiveType(bi.Init);
                ParameterExpression parmExpr;
                Expression initExpr;
                if ( primType != null ) 
                {
                    parmExpr = Expression.Parameter(primType,bi.Binding.Name);
                    initExpr =  ((MaybePrimitiveExpr)bi.Init).GenCodeUnboxed(RHC.Expression,objx,context);
                }
                else 
                {
                    parmExpr =  Expression.Parameter(typeof(object),bi.Binding.Name);
                    initExpr = Compiler.MaybeBox(bi.Init.GenCode(RHC.Expression,objx,context));
                }
                bi.Binding.ParamExpression = parmExpr;
                parms.Add(parmExpr);
                forms.Add(Expression.Assign(parmExpr, initExpr));
            }


            forms.Add(Expression.Label(loopLabel));

            try
            {
                if (_isLoop)
                    Var.pushThreadBindings(PersistentHashMap.create(Compiler.LOOP_LABEL, loopLabel));

                Expression form = genUnboxed 
                    ? ((MaybePrimitiveExpr)_body).GenCodeUnboxed(rhc,objx,context) 
                    : _body.GenCode(rhc,objx,context);

                forms.Add(form);
            }
            finally
            {
                if (_isLoop)
                    Var.popThreadBindings();
            }

            Expression block = Expression.Block(parms, forms);
            return block;
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return _body is MaybePrimitiveExpr && ((MaybePrimitiveExpr)_body).CanEmitPrimitive; }
        }

        public Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            return GenCode(rhc, objx, context, true);
        }

        #endregion
    }
}
