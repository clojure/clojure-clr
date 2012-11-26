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
        readonly int _column;

        #endregion

        #region Ctors

        public DefExpr(string source, int line, int column, Var var, Expr init, Expr meta, bool initProvided, bool isDyanamic)
        {
            _source = source;
            _line = line;
            _column = column;
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
                    throw new ParseException("Too many arguments to def");

                if (RT.count(form) < 2)
                    throw new ParseException("Too few arguments to def");

                Symbol sym = RT.second(form) as Symbol;

                if (sym == null)
                    throw new ParseException("First argument to def must be a Symbol.");

                //Console.WriteLine("Def {0}", sym.Name);
                
                Var v = Compiler.LookupVar(sym, true);

                if (v == null)
                    throw new ParseException("Can't refer to qualified var that doesn't exist");

                if (!v.Namespace.Equals(Compiler.CurrentNamespace))
                {
                    if (sym.Namespace == null)
                        v = Compiler.CurrentNamespace.intern(sym);

                    //throw new Exception(string.Format("Name conflict, can't def {0} because namespace: {1} refers to: {2}",
                    //            sym, Compiler.CurrentNamespace.Name, v));
                    else
                        throw new ParseException("Can't create defs outside of current namespace");
                }

                IPersistentMap mm = sym.meta();
                bool isDynamic = RT.booleanCast(RT.get(mm, Compiler.DynamicKeyword));
                if (isDynamic)
                    v.setDynamic();
                if (!isDynamic && sym.Name.StartsWith("*") && sym.Name.EndsWith("*") && sym.Name.Length > 1)
                {
                    RT.errPrintWriter().WriteLine("Warning: {0} not declared dynamic and thus is not dynamically rebindable, "
                                          + "but its name suggests otherwise. Please either indicate ^:dynamic {0} or change the name. ({1}:{2}\n",
                                           sym,Compiler.SourcePathVar.get(),Compiler.LineVar.get());
                }

                if (RT.booleanCast(RT.get(mm, Compiler.ArglistsKeyword)))
                {
                    IPersistentMap vm = v.meta();
                    //vm = (IPersistentMap)RT.assoc(vm, Compiler.STATIC_KEY, true);
                    // drop quote
                    vm = (IPersistentMap)RT.assoc(vm, Compiler.ArglistsKeyword, RT.second(mm.valAt(Compiler.ArglistsKeyword)));
                    v.setMeta(vm);
                }

                Object source_path = Compiler.SourcePathVar.get();
                source_path = source_path ?? "NO_SOURCE_FILE";
                mm = (IPersistentMap)RT.assoc(mm,RT.LineKey, Compiler.LineVar.get())
                    .assoc(RT.ColumnKey,Compiler.ColumnVar.get())
                    .assoc(RT.FileKey, source_path);
                    //.assoc(RT.SOURCE_SPAN_KEY,Compiler.SOURCE_SPAN.deref());
                if (docstring != null)
                    mm = (IPersistentMap)RT.assoc(mm, RT.DocKey, docstring);

                //mm = mm.without(RT.DOC_KEY)
                //            .without(Keyword.intern(null, "arglists"))
                //            .without(RT.FILE_KEY)
                //            .without(RT.LINE_KEY)
                //            .without(RT.COLUMN_KEY)
                //            .without(Keyword.intern(null, "ns"))
                //            .without(Keyword.intern(null, "name"))
                //            .without(Keyword.intern(null, "added"))
                //            .without(Keyword.intern(null, "static"));

                mm = (IPersistentMap)Compiler.ElideMeta(mm);

                Expr meta =  mm == null || mm.count() == 0 ? null : Compiler.Analyze(pcon.EvalOrExpr(),mm);
                Expr init = Compiler.Analyze(pcon.EvalOrExpr(),RT.third(form), v.Symbol.Name);
                bool initProvided = RT.count(form) == 3;

                return new DefExpr(
                    (string)Compiler.SourceVar.deref(),
                    (int) Compiler.LineVar.deref(),
                    (int) Compiler.ColumnVar.deref(),
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
                    if (_initProvided || true) // includesExplicitMetadata((MapExpr)_meta))
                        _var.setMeta((IPersistentMap)_meta.Eval());
                }
                return _var.setDynamic(_isDynamic);
            }
            catch (Compiler.CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Compiler.CompilerException(_source, _line, _column, e);
            }
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            objx.EmitVar(ilg, _var);
            if (_isDynamic)
            {
                ilg.Emit(OpCodes.Call, Compiler.Method_Var_setDynamic0);
            }
            if (_meta != null)
            {
                if (_initProvided || true) //IncludesExplicitMetadata((MapExpr)_meta))
                {
                    ilg.Emit(OpCodes.Dup);
                    _meta.Emit(RHC.Expression, objx, ilg);
                    ilg.Emit(OpCodes.Castclass, typeof(IPersistentMap));
                    ilg.Emit(OpCodes.Call, Compiler.Method_Var_setMeta);
                }
            }
            if (_initProvided)
            {
                ilg.Emit(OpCodes.Dup);
                if (_init is FnExpr)
                    ((FnExpr)_init).EmitForDefn(objx, ilg);
                else
                    _init.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Call,Compiler.Method_Var_bindRoot);
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion

        #region Misc

        private static bool IncludesExplicitMetadata(MapExpr expr)
        {
            for (int i = 0; i < expr.KeyVals.count(); i += 2)
            {
                Keyword k = ((KeywordExpr)expr.KeyVals.nth(i)).Kw;
                if ((k != RT.FileKey) &&
                    (k != RT.DeclaredKey) &&
                    (k != RT.SourceSpanKey) &&
                    (k != RT.LineKey) &&
                    (k != RT.ColumnKey))
                    return true;
            }
            return false;
        }

        #endregion
    }
}
