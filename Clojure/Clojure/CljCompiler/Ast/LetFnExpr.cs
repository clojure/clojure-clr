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

    public class LetFnExpr : Expr
    {
        #region Data

        readonly IPersistentVector _bindingInits;
        public IPersistentVector BindingInits { get { return _bindingInits; } }

        readonly Expr _body;
        public Expr Body { get { return _body; } }

        #endregion

        #region Ctors

        public LetFnExpr(IPersistentVector bindingInits, Expr body)
        {
            _bindingInits = bindingInits;
            _body = body;
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

                // form => (letfn*  [var1 (fn [args] body) ... ] body ... )

                if (!(RT.second(form) is IPersistentVector bindings))
                    throw new ParseException("Bad binding form, expected vector");

                if ((bindings.count() % 2) != 0)
                    throw new ParseException("Bad binding form, expected matched symbol/expression pairs.");

                ISeq body = RT.next(RT.next(form));

                if (pcon.Rhc == RHC.Eval)
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FnOnceSym, PersistentVector.EMPTY, form)), "letfn__" + RT.nextID());

                IPersistentMap dynamicBindings = RT.map(
                    Compiler.LocalEnvVar, Compiler.LocalEnvVar.deref(),
                    Compiler.NextLocalNumVar, Compiler.NextLocalNumVar.deref());

                try
                {
                    Var.pushThreadBindings(dynamicBindings);

                    // pre-seed env (like Lisp labels)
                    IPersistentVector lbs = PersistentVector.EMPTY;
                    for (int i = 0; i < bindings.count(); i += 2)
                    {
                        if (!(bindings.nth(i) is Symbol))
                            throw new ParseException("Bad binding form, expected symbol, got " + bindings.nth(i));

                        Symbol sym = (Symbol)bindings.nth(i);
                        if (sym.Namespace != null)
                            throw new ParseException("Can't let qualified name: " + sym);

                        LocalBinding b = Compiler.RegisterLocal(sym, Compiler.TagOf(sym), null, typeof(Object), false);
                        // b.CanBeCleared = false;
                        lbs = lbs.cons(b);
                    }

                    IPersistentVector bindingInits = PersistentVector.EMPTY;
                    for (int i = 0; i < bindings.count(); i += 2)
                    {
                        Symbol sym = (Symbol)bindings.nth(i);
                        Expr init = Compiler.Analyze(pcon.SetRhc(RHC.Expression),bindings.nth(i + 1),sym.Name);
                        LocalBinding b = (LocalBinding)lbs.nth(i / 2);
                        b.Init = init;
                        BindingInit bi = new BindingInit(b, init);
                        bindingInits = bindingInits.cons(bi);
                    }

                    return new LetFnExpr(bindingInits,new BodyExpr.Parser().Parse(pcon, body));
                }
                finally
                {
                    Var.popThreadBindings();
                }
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval letfns");
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            int n = _bindingInits.count();

            // Define our locals
            for (int i = 0; i < n; i++)
            {
                BindingInit bi = (BindingInit)_bindingInits.nth(i);
                LocalBuilder local = ilg.DeclareLocal(typeof(IFn));
                bi.Binding.LocalVar = local;
                ilg.Emit(OpCodes.Ldnull);
                ilg.Emit(OpCodes.Stloc, local);
            }

            // Then initialize

            IPersistentSet lbset = PersistentHashSet.EMPTY;

            for (int i = 0; i < n; i++)
            {
                BindingInit bi = (BindingInit)_bindingInits.nth(i);
                lbset = (IPersistentSet)lbset.cons(bi.Binding);

                bi.Init.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Stloc,bi.Binding.LocalVar);
            }

            for (int i = 0; i < n; i++)
            {
                BindingInit bi = (BindingInit)_bindingInits.nth(i);
                ObjExpr fe = (ObjExpr)bi.Init;

                ilg.Emit(OpCodes.Ldloc, bi.Binding.LocalVar);
                fe.EmitLetFnInits(ilg, bi.Binding.LocalVar, objx, lbset);
            }

            _body.Emit(rhc, objx, ilg);
        }

        public bool HasNormalExit() { return _body.HasNormalExit(); }

        #endregion
    }
}
