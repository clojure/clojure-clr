/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;

namespace clojure.lang.CljCompiler.Ast
{
    public class NumberExpr : LiteralExpr, MaybePrimitiveExpr
    {
        #region Data

        readonly object _n;
        public object N => _n;

        readonly int _id;
        public int Id => _id;

        #endregion

        #region Ctors

        public NumberExpr(object n)
        {
            _n = n;
            _id = Compiler.RegisterConstant(n);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType => true;

        public override Type ClrType
        {
            get
            {
                return _n switch
                {
                    int => typeof(long),
                    double => typeof(double),
                    long => typeof(long),
                    _ => throw new InvalidOperationException("Unsupported Number type: " + _n.GetType().Name)
                };
            }
        }

        #endregion

        #region Parsing

        public static Expr Parse(object form)
        {
            if (form is int || form is double || form is long)
                return new NumberExpr(form);
            else
                return new ConstantExpr(form);
        }

        #endregion

        #region LiteralExpr members

        public override object Val
        {
            get { return _n; }
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            if (rhc != RHC.Statement)
                objx.EmitConstant(ilg, _id, _n);
        }

        public bool CanEmitPrimitive => true;

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Type t = _n.GetType();

            if (t == typeof(int))
                ilg.EmitLong((long)(int)_n);
            else if (t == typeof(double))
                ilg.EmitDouble((double)_n);
            else if (t == typeof(long))
                ilg.EmitLong((long)_n);
            else
                throw new ArgumentException("Unsupported Number type: " + _n.GetType().Name);
        }

        #endregion
    }
}
