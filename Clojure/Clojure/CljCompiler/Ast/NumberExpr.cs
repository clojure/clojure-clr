using System;
using System.Collections.Generic;
using System.Linq;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Text;


namespace clojure.lang.CljCompiler.Ast
{
    class NumberExpr : LiteralExpr, MaybePrimitiveExpr
    {
        #region Data

        readonly object _n;
        readonly int _id;

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
                    throw new ArgumentException("Unsupported Number type: " + _n.GetType().Name);
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

        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            return objx.GenConstant(context, _id, _n);
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return true; }
        }

        public Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            Type t = _n.GetType();

            if (t == typeof(int))
                return Expression.Constant((long)_n, typeof(long));
            else if (t == typeof(double))
                return Expression.Constant((double)_n, typeof(double));
            else if ( t == typeof(long) )
                return Expression.Constant((long)_n,typeof(long));

            throw new ArgumentException("Unsupported Number type: " + _n.GetType().Name);
        }

        #endregion
    }
}
