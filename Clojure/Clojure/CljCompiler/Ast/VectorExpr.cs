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


namespace clojure.lang.CljCompiler.Ast
{
    class VectorExpr : Expr
    {
        #region Data

        readonly IPersistentVector _args;

        #endregion

        #region Ctors

        public VectorExpr(IPersistentVector args)
        {
            _args = args;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return typeof(IPersistentVector); }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ParserContext pcon, IPersistentVector form)
        {
            ParserContext pconToUse = pcon.EvalOrExpr();
            bool constant = true;

            IPersistentVector args = PersistentVector.EMPTY;
            for (int i = 0; i < form.count(); i++ )
            {
                Expr v = Compiler.Analyze(pconToUse, form.nth(i));
                args = (IPersistentVector)args.cons(v);
                if ( !(v is LiteralExpr) )
                    constant = false;
            }

            Expr ret = new VectorExpr(args);
            IObj iobjForm = form as IObj;
            if (iobjForm != null && iobjForm.meta() != null)
                return Compiler.OptionallyGenerateMetaInit(pcon,form, ret);
            else if ( constant )
            {
                IPersistentVector rv = PersistentVector.EMPTY;
                for ( int i=0; i<args.count(); i++ )
                {
                    LiteralExpr ve = (LiteralExpr)args.nth(i);
                    rv = (IPersistentVector)rv.cons(ve.Val);
                }
                return new ConstantExpr(rv);
            }
            else
                return ret;
        }

        #endregion

        #region eval

        public object Eval()
        {
            IPersistentVector ret = PersistentVector.EMPTY;
            for (int i = 0; i < _args.count(); i++)
                ret = (IPersistentVector)ret.cons(((Expr)_args.nth(i)).Eval());
            return ret;
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression argArray = Compiler.GenArgArray(rhc, objx, context, _args);
            Expression ret = Expression.Call(Compiler.Method_RT_vector, argArray);
            return ret;
        }

        #endregion
    }
}
