/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting;


namespace clojure.lang.CljCompiler.Ast
{
    class DefExpr : Expr
    {
        #region Data

        readonly Var _var;
        readonly Expr _init;
        readonly Expr _meta;
        readonly bool _initProvided;

        #endregion

        #region Ctors

        public DefExpr(Var var, Expr init, Expr meta, bool initProvided)
        {
            _var = var;
            _init = init;
            _meta = meta;
            _initProvided = initProvided;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return typeof(Var); }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(object form, bool isRecurContext)
            {
                // (def x) or (def x initexpr)
                if (RT.count(form) > 3)
                    throw new Exception("Too many arguments to def");

                if (RT.count(form) < 2)
                    throw new Exception("Too few arguments to def");

                Symbol sym = RT.second(form) as Symbol;

                if (sym == null)
                    throw new Exception("Second argument to def must be a Symbol.");

                Var v = Compiler.LookupVar(sym, true);

                if (v == null)
                    throw new Exception("Can't refer to qualified var that doesn't exist");

                if (!v.Namespace.Equals(Compiler.CurrentNamespace))
                {
                    if (sym.Namespace == null)
                        throw new Exception(string.Format("Name conflict, can't def {0} because namespace: {1} refers to: {2}",
                                    sym, Compiler.CurrentNamespace.Name, v));
                    else
                        throw new Exception("Can't create defs outside of current namespace");
                }

                IPersistentMap mm = sym.meta();
                Object source_path = Compiler.SOURCE_PATH.deref();
                source_path = source_path ?? "NO_SOURCE_FILE";
                mm = (IPersistentMap)RT.assoc(mm, RT.LINE_KEY, Compiler.LINE.deref()).assoc(RT.FILE_KEY, source_path);

                //SourceSpan? span = (SourceSpan?)Compiler.SOURCE_SPAN.deref();
                IPersistentMap spanMap = Compiler.GetSourceSpanMap(form);
                if (spanMap != null)
                    mm = mm.assoc(RT.SOURCE_SPAN_KEY, spanMap);

                Expr meta =  mm == null ? null : Compiler.GenerateAST(mm,false);
                Expr init = Compiler.GenerateAST(RT.third(form),v.Symbol.Name,false);
                bool initProvided = RT.count(form) == 3;

                return new DefExpr(v, init, meta, initProvided);
            }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            List<Expression> exprs = new List<Expression>();

            ParameterExpression parm = Expression.Parameter(typeof(Var), "v");

            Expression varExpr = context.FnExpr.GenVar(context,_var);

            exprs.Add(Expression.Assign(parm, varExpr));

            if (_initProvided)
                // Java doesn't Box here, but we have to deal with unboxed bool values
                exprs.Add(Expression.Call(parm, Compiler.Method_Var_BindRoot, Compiler.MaybeBox(_init.GenDlr(context))));

            if (_meta != null)
                // Java casts to IPersistentMap on the _meta, but Expression.Call can handle that for us.
                exprs.Add(Expression.Call(parm, Compiler.Method_Var_setMeta, _meta.GenDlr(context)));

            exprs.Add(parm);

            return Expression.Block(new ParameterExpression[] { parm }, exprs);
        }

        #endregion
    }
}
