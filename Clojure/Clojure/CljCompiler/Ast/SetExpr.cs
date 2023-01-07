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
    public class SetExpr : Expr
    {
        #region Data

        readonly IPersistentVector _keys;
        public IPersistentVector Keys { get { return _keys; } }

        #endregion

        #region Ctors

        public SetExpr(IPersistentVector keys)
        {
            _keys = keys;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return typeof(IPersistentSet); }
        }

        #endregion
        
        #region Parsing

        public static Expr Parse(ParserContext pcon, IPersistentSet form)
        {
            ParserContext pconToUse = pcon.EvalOrExpr();
            bool constant = true;

            IPersistentVector keys = PersistentVector.EMPTY;
            for (ISeq s = RT.seq(form); s != null; s = s.next())
            {
                object e = s.first();
                Expr expr = Compiler.Analyze(pconToUse, e);
                keys = (IPersistentVector)keys.cons(expr);
                if (!(expr is LiteralExpr))
                    constant = false;
            }
            Expr ret = new SetExpr(keys);

            if (form is IObj iobjForm && iobjForm.meta() != null)
                return Compiler.OptionallyGenerateMetaInit(pcon, form, ret);
            else if (constant)
            {
                IPersistentSet set = PersistentHashSet.EMPTY;
                for (int i = 0; i < keys.count(); i++)
                {
                    LiteralExpr ve = (LiteralExpr)keys.nth(i);
                    set = (IPersistentSet)set.cons(ve.Val);
                }
                return new ConstantExpr(set);
            }
            else
                return ret;
        }

        #endregion

        #region eval

        public object Eval()
        {
            Object[] ret = new Object[_keys.count()];
            for (int i = 0; i < _keys.count(); i++)
                ret[i] = ((Expr)_keys.nth(i)).Eval();
            return RT.set(ret);
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            MethodExpr.EmitArgsAsArray(_keys, objx, ilg);
            ilg.Emit(OpCodes.Call, Compiler.Method_RT_set);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
