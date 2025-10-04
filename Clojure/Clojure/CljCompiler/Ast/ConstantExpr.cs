﻿/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class ConstantExpr : LiteralExpr
    {
        #region Data

        readonly object _v;
        public override object Val => _v;

        readonly int _id;
        public int Id => _id;

        #endregion

        #region Ctors

        public ConstantExpr(object v)
        {
            _v = v;
            _id = Compiler.RegisterConstant(v);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get
            {
                return _v.GetType().IsPublic
                    || _v.GetType().IsNestedPublic
                    || typeof(Type).IsInstanceOfType(_v);   // This bit of hackery is due to the fact that RuntimeType is not public.  
                                                            // Without this, System.Int64 would be seen as only type System.Object, not System.RuntimeType.
            }
        }

        public override Type ClrType
        {
            get
            {
                if (_v is APersistentMap)
                    return typeof(APersistentMap);
                else if (_v is APersistentSet)
                    return typeof(APersistentSet);
                else if (_v is APersistentVector)
                    return typeof(APersistentVector);
                else if (_v is Type)
                    return typeof(Type);
                else
                    return _v.GetType();
            }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            static readonly Keyword FormKey = Keyword.intern("form");

            public Expr Parse(ParserContext pcon, object form)
            {
                int argCount = RT.count(form) - 1;
                if (argCount != 1)
                {
                    IPersistentMap exData = new PersistentArrayMap(new Object[] { FormKey, form });
                    throw new ExceptionInfo("Wrong number of args (" +
                                            argCount +
                                            ") passed to quote",
                                            exData);
                }

                object v = RT.second(form);

                if (v is null)
                    return Compiler.NilExprInstance;
                else if (v is Boolean)
                {
                    if ((bool)v)
                        return Compiler.TrueExprInstance;
                    else
                        return Compiler.FalseExprInstance;
                }
                else if (Util.IsNumeric(v))
                    return NumberExpr.Parse(v);
                else if (v is string)
                    return new StringExpr((String)v);
                else if (v is IPersistentCollection collection
                    && collection.count() == 0
                    && (!(v is IObj ov) || (ov.meta() == null)))
                    return new EmptyExpr(v);
                else
                    return new ConstantExpr(v);
            }
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            objx.EmitConstant(ilg, _id);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion
    }
}
