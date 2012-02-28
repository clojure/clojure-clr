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
using Microsoft.Scripting.Generation;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    class TryExpr : Expr
    {
        #region Nested classes

        public sealed class CatchClause
        {
            readonly Type _type;
            public Type Type
            {
              get { return _type; }  
            } 

            readonly LocalBinding _lb;
            internal LocalBinding Lb
            {
              get { return _lb; }  
            } 

            readonly Expr _handler;
            internal Expr Handler
            {
              get { return _handler; }  
            } 


            public CatchClause(Type type, LocalBinding lb, Expr handler)
            {
                _type = type;
                _lb = lb;
                _handler = handler;
            }
        }

        #endregion

        #region Data

        readonly Expr _tryExpr;
        readonly Expr _finallyExpr;
        readonly IPersistentVector _catchExprs;
        //readonly int _retLocal;
        //readonly int _finallyLocal;

        #endregion

        #region Ctors

        public TryExpr(Expr tryExpr, IPersistentVector catchExprs, Expr finallyExpr, int retLocal, int finallyLocal)
        {
            _tryExpr = tryExpr;
            _catchExprs = catchExprs;
            _finallyExpr = finallyExpr;
            //_retLocal = retLocal;
            //_finallyLocal = finallyLocal;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _tryExpr.HasClrType; }
        }

        public Type ClrType
        {
            get { return _tryExpr.ClrType; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq)frm;

                if (pcon.Rhc != RHC.Return)
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FnSym, PersistentVector.EMPTY, form)), "try__" + RT.nextID());

                // (try try-expr* catch-expr* finally-expr?)
                // catch-expr: (catch class sym expr*)
                // finally-expr: (finally expr*)

                IPersistentVector body = PersistentVector.EMPTY;
                IPersistentVector catches = PersistentVector.EMPTY;
                Expr bodyExpr = null;
                Expr finallyExpr = null;
                bool caught = false;

                int retLocal = Compiler.GetAndIncLocalNum();
                int finallyLocal = Compiler.GetAndIncLocalNum();

                for (ISeq fs = form.next(); fs != null; fs = fs.next())
                {
                    object f = fs.first();
                    ISeq fSeq = f as ISeq;
                    object op = fSeq != null ? fSeq.first() : null;
                    if (!Util.equals(op, Compiler.CatchSym) && !Util.equals(op, Compiler.FinallySym))
                    {
                        if (caught)
                            throw new ParseException("Only catch or finally clause can follow catch in try expression");
                        body = body.cons(f);
                    }
                    else
                    {
                        if (bodyExpr == null)
                        {
                            try
                            {
                                Var.pushThreadBindings(RT.map(Compiler.NoRecurVar, true));
                                bodyExpr = new BodyExpr.Parser().Parse(pcon.SetAssign(false), RT.seq(body));
                            }
                            finally
                            {
                                Var.popThreadBindings();
                            }
                        }
                        if (Util.equals(op, Compiler.CatchSym))
                        {
                            Type t = HostExpr.MaybeType(RT.second(f), false);
                            if (t == null)
                                throw new ParseException("Unable to resolve classname: " + RT.second(f));
                            if (!(RT.third(f) is Symbol))
                                throw new ParseException("Bad binding form, expected symbol, got: " + RT.third(f));
                            Symbol sym = (Symbol)RT.third(f);
                            if (sym.Namespace != null)
                                throw new ParseException("Can't bind qualified name: " + sym);

                            IPersistentMap dynamicBindings = RT.map(
                                Compiler.LocalEnvVar, Compiler.LocalEnvVar.deref(),
                                Compiler.NextLocalNumVar, Compiler.NextLocalNumVar.deref(),
                                Compiler.InCatchFinallyVar, true);

                            try
                            {
                                Var.pushThreadBindings(dynamicBindings);
                                LocalBinding lb = Compiler.RegisterLocal(sym,
                                    (Symbol)(RT.second(f) is Symbol ? RT.second(f) : null),
                                    null,false);
                                Expr handler = (new BodyExpr.Parser()).Parse(pcon.SetAssign(false), RT.next(RT.next(RT.next(f))));
                                catches = catches.cons(new CatchClause(t, lb, handler)); ;
                            }
                            finally
                            {
                                Var.popThreadBindings();
                            }
                            caught = true;
                        }
                        else // finally
                        {
                            if (fs.next() != null)
                                throw new InvalidOperationException("finally clause must be last in try expression");
                            try
                            {
                                //Var.pushThreadBindings(RT.map(Compiler.IN_CATCH_FINALLY, RT.T));
                                Var.pushThreadBindings(RT.map(Compiler.InCatchFinallyVar, true));
                                finallyExpr = (new BodyExpr.Parser()).Parse(pcon.SetRhc(RHC.Statement).SetAssign(false), RT.next(f));
                            }
                            finally
                            {
                                Var.popThreadBindings();
                            }
                        }
                    }
                }

                if (bodyExpr == null)
                {
                    try
                    {
                        Var.pushThreadBindings(RT.map(Compiler.NoRecurVar, true));
                        bodyExpr = (new BodyExpr.Parser()).Parse(pcon, RT.seq(body));
                    }
                    finally
                    {
                        Var.popThreadBindings();
                    }
                }
                return new TryExpr(bodyExpr, catches, finallyExpr, retLocal, finallyLocal);
              }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval try");
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression basicBody = _tryExpr.GenCode(rhc, objx, context);
            if (basicBody.Type == typeof(void))
                basicBody = Expression.Block(basicBody, Expression.Default(typeof(object)));
            Expression tryBody = Expression.Convert(basicBody,typeof(object));

            CatchBlock[] catches = new CatchBlock[_catchExprs.count()];
            for ( int i=0; i<_catchExprs.count(); i++ )
            {
                CatchClause clause = (CatchClause) _catchExprs.nth(i);
                ParameterExpression parmExpr = Expression.Parameter(clause.Type, clause.Lb.Name);
                clause.Lb.ParamExpression = parmExpr;
                catches[i] = Expression.Catch(parmExpr, Expression.Convert(clause.Handler.GenCode(rhc, objx, context), typeof(object)));
            }                

            Expression tryStmt = _finallyExpr == null
                ? catches.Length == 0 ? tryBody : Expression.TryCatch(tryBody, catches)
                : Expression.TryCatchFinally(tryBody, _finallyExpr.GenCode(RHC.Statement, objx, context), catches);

            return tryStmt;
        }


        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            if (_catchExprs.count() == 0 && _finallyExpr == null)
            {
                // degenerate case
                _tryExpr.Emit(rhc, objx, context);
                return;
            }

            ILGenerator ilg = context.GetILGenerator();

            LocalBuilder retLocal = ilg.DeclareLocal(typeof(Object));

            Label endLabel = ilg.BeginExceptionBlock();
            _tryExpr.Emit(rhc, objx, context);
            if (rhc != RHC.Statement)
                ilg.Emit(OpCodes.Stloc, retLocal);
            //ilg.Emit(OpCodes.Leave, endLabel);

            for (int i = 0; i < _catchExprs.count(); i++)
            {
                CatchClause clause = (CatchClause)_catchExprs.nth(i);
                ilg.BeginCatchBlock(clause.Type);
                // Exception should be on the stack.  Put in clause local
                clause.Lb.LocalVar = ilg.DeclareLocal(clause.Type);
                ilg.Emit(OpCodes.Stloc, clause.Lb.LocalVar);
                clause.Handler.Emit(rhc, objx, context);
                if (rhc != RHC.Statement)
                    ilg.Emit(OpCodes.Stloc, retLocal);
                //ilg.Emit(OpCodes.Leave, endLabel);
            }

            if (_finallyExpr != null)
            {
                ilg.BeginFinallyBlock();
                _finallyExpr.Emit(RHC.Statement, objx, context);
            }
            ilg.EndExceptionBlock();
            if (rhc != RHC.Statement)
                ilg.Emit(OpCodes.Ldloc, retLocal);
        }

        #endregion
    }
}
