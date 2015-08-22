using System;
using System.Reflection.Emit;


namespace clojure.lang.CljCompiler.Ast
{
    public class NumberExpr : LiteralExpr, MaybePrimitiveExpr
    {
        #region Data

        readonly object _n;
        public object N { get { return _n; } }

        readonly int _id;
        public int Id { get { return _id; } }

        #endregion

        #region Ctors

        public NumberExpr(object n)
        {
            _n = n;
            _id = Compiler.RegisterConstant(n);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get
            {
                if (_n is int)
                    return typeof(long);
                else if (_n is double)
                    return typeof(double);
                else if (_n is long)
                    return typeof(long);
                else
                    throw new InvalidOperationException("Unsupported Number type: " + _n.GetType().Name);
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

        public bool CanEmitPrimitive
        {
            get { return true; }
        }

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
