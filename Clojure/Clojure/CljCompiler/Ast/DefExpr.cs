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
    class DefExpr : Expr
    {
        #region Data

        readonly Var _var;
        readonly Expr _init;
        readonly Expr _meta;
        readonly bool _initProvided;
        readonly bool _isDynamic;
        readonly string _source;
        readonly int _line;

        #endregion

        #region Ctors

        public DefExpr(string source, int line, Var var, Expr init, Expr meta, bool initProvided, bool isDyanamic)
        {
            _source = source;
            _line = line;
            _var = var;
            _init = init;
            _meta = meta;
            _isDynamic = isDyanamic;
            _initProvided = initProvided;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return typeof(Var); }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object form)
            {
                // (def x) or (def x initexpr) or (def x "docstring" initexpr)
                string docstring = null;
                if (RT.count(form) == 4 && (RT.third(form) is String))
                {
                    docstring = (String)RT.third(form);
                    form = RT.list(RT.first(form), RT.second(form), RT.fourth(form));
                }

                if (RT.count(form) > 3)
                    throw new Exception("Too many arguments to def");

                if (RT.count(form) < 2)
                    throw new Exception("Too few arguments to def");

                Symbol sym = RT.second(form) as Symbol;

                if (sym == null)
                    throw new Exception("First argument to def must be a Symbol.");

                //Console.WriteLine("Def {0}", sym.Name);
                
                Var v = Compiler.LookupVar(sym, true);

                if (v == null)
                    throw new Exception("Can't refer to qualified var that doesn't exist");

                if (!v.Namespace.Equals(Compiler.CurrentNamespace))
                {
                    if (sym.Namespace == null)
                        v = Compiler.CurrentNamespace.intern(sym);

                    //throw new Exception(string.Format("Name conflict, can't def {0} because namespace: {1} refers to: {2}",
                    //            sym, Compiler.CurrentNamespace.Name, v));
                    else
                        throw new Exception("Can't create defs outside of current namespace");
                }

                IPersistentMap mm = sym.meta();
                bool isDynamic = RT.booleanCast(RT.get(mm, Compiler.DYNAMIC_KEY));
                if (!isDynamic && sym.Name.StartsWith("*") && sym.Name.EndsWith("*") && sym.Name.Length > 1)
                {
                    RT.errPrintWriter().WriteLine("Var {0} is not marked :dynamic true, setting to :dynamic. You should fix this before next release!", sym);
                    isDynamic = true;
                    mm = (IPersistentMap)RT.assoc(mm, Compiler.DYNAMIC_KEY, true);
                }

                if (RT.booleanCast(RT.get(mm, Compiler.ARGLISTS_KEY)))
                {
                    IPersistentMap vm = v.meta();
                    //vm = (IPersistentMap)RT.assoc(vm, Compiler.STATIC_KEY, true);
                    // drop quote
                    vm = (IPersistentMap)RT.assoc(vm, Compiler.ARGLISTS_KEY, RT.second(mm.valAt(Compiler.ARGLISTS_KEY)));
                    v.setMeta(vm);
                }

                Object source_path = Compiler.SOURCE_PATH.get();
                source_path = source_path ?? "NO_SOURCE_FILE";
                mm = (IPersistentMap)RT.assoc(mm,RT.LINE_KEY, Compiler.LINE.get())
                    .assoc(RT.FILE_KEY, source_path);
                    //.assoc(RT.SOURCE_SPAN_KEY,Compiler.SOURCE_SPAN.deref());
                if (docstring != null)
                    mm = (IPersistentMap)RT.assoc(mm, RT.DOC_KEY, docstring);

                //mm = mm.without(RT.DOC_KEY)
                //            .without(Keyword.intern(null, "arglists"))
                //            .without(RT.FILE_KEY)
                //            .without(RT.LINE_KEY)
                //            .without(Keyword.intern(null, "ns"))
                //            .without(Keyword.intern(null, "name"))
                //            .without(Keyword.intern(null, "added"))
                //            .without(Keyword.intern(null, "static"));

                Expr meta =  mm == null || mm.count() == 0 ? null : Compiler.Analyze(pcon.EvEx(),mm);
                Expr init = Compiler.Analyze(pcon.EvEx(),RT.third(form), v.Symbol.Name);
                bool initProvided = RT.count(form) == 3;

                return new DefExpr(
                    (string)Compiler.SOURCE.deref(),
                    (int) Compiler.LINE.deref(),
                    v, init, meta, initProvided,isDynamic);
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            try
            {
                if (_initProvided)
                    _var.bindRoot(_init.Eval());
                if (_meta != null)
                {
                    IPersistentMap metaMap = (IPersistentMap)_meta.Eval();
                    if (_initProvided || true) // includesExplicitMetadata((MapExpr)_meta))
                        _var.setMeta((IPersistentMap)_meta.Eval());
                }
                return _var.setDynamic(_isDynamic);
            }
            catch (Exception e)
            {
                if (!(e is Compiler.CompilerException))
                    throw new Compiler.CompilerException(_source, _line, e);
                else
                    throw e;
            }
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            List<Expression> exprs = new List<Expression>();

            ParameterExpression parm = Expression.Parameter(typeof(Var), "v");

            Expression varExpr = objx.GenVar(context,_var);

            if (_isDynamic)
                varExpr = Expression.Call(varExpr, Compiler.Method_Var_setDynamic0);

            exprs.Add(Expression.Assign(parm, varExpr));

            if (_meta != null)
            {
                if (_initProvided || true) //IncludesExplicitMetadata((MapExpr)_meta))
                {
                    exprs.Add(Expression.Call(parm, Compiler.Method_Var_setMeta, Expression.Convert(_meta.GenCode(RHC.Expression, objx, context), typeof(IPersistentMap))));
                }
            }

            if (_initProvided )
                // RETYPE: get rid of Box? 
                // Java doesn't Box here, but we have to deal with unboxed bool values
                exprs.Add(Expression.Call(parm, Compiler.Method_Var_bindRoot, Compiler.MaybeBox(_init.GenCode(RHC.Expression,objx,context))));

            exprs.Add(parm);

            return Expression.Block(new ParameterExpression[] { parm }, exprs);
        }

        #endregion

        #region Misc

        private bool IncludesExplicitMetadata(MapExpr expr) {
        for(int i=0; i < expr.KeyVals.count(); i += 2)
            {
                Keyword k = ((KeywordExpr)expr.KeyVals.nth(i)).Kw;
                if ((k != RT.FILE_KEY) &&
                    (k != RT.DECLARED_KEY) &&
                    (k != RT.SOURCE_SPAN_KEY ) &&
                    (k != RT.LINE_KEY))
                    return true;
            }
        return false;
    }

        #endregion
    }
}
