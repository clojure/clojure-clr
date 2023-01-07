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
using System.Reflection;
using System.Reflection.Emit;


namespace clojure.lang.CljCompiler.Ast
{
    public class DefExpr : Expr
    {
        #region Data

        readonly Var _var;
        public Var Var { get { return _var; } }

        readonly Expr _init;
        public Expr Init { get { return _init; } }

        readonly Expr _meta;
        public Expr Meta { get { return _meta; } }

        readonly bool _initProvided;
        public bool InitProvided { get { return _initProvided; } }
        
        readonly bool _isDynamic;
        public bool IsDynamic { get { return _isDynamic; } }
        
        readonly bool _shadowsCoreMapping;
        public bool ShadowsCoreMapping { get { return _shadowsCoreMapping; } }
        
        readonly string _source;
        public string Source { get { return _source; } }
        
        readonly int _line;
        public int Line { get { return _line; } }
        
        readonly int _column;
        public int Column { get { return _column; } }

        #endregion

        #region Ctors

        public DefExpr(string source, int line, int column, Var var, Expr init, Expr meta, bool initProvided, bool isDyanamic, bool shadowsCoreMapping)
        {
            _source = source;
            _line = line;
            _column = column;
            _var = var;
            _init = init;
            _meta = meta;
            _isDynamic = isDyanamic;
            _shadowsCoreMapping = shadowsCoreMapping;
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
                if (RT.count(form) == 4 && (RT.third(form) is String str))
                {
                    docstring = str;
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

                bool shadowsCoreMapping = false;

                if (!v.Namespace.Equals(Compiler.CurrentNamespace))
                {
                    if (sym.Namespace == null)
                    {
                        v = Compiler.CurrentNamespace.intern(sym);
                        shadowsCoreMapping = true;
                        Compiler.RegisterVar(v);
                    }

                    //throw new Exception(string.Format("Name conflict, can't def {0} because namespace: {1} refers to: {2}",
                    //            sym, Compiler.CurrentNamespace.Name, v));
                    else
                        throw new ParseException("Can't create defs outside of current namespace");
                }

                IPersistentMap mm = sym.meta();
                bool isDynamic = RT.booleanCast(RT.get(mm, Compiler.DynamicKeyword));
                if (isDynamic)
                    v.setDynamic();
                if (!isDynamic && sym.Name.StartsWith("*") && sym.Name.EndsWith("*") && sym.Name.Length > 2)
                {
                    RT.errPrintWriter().WriteLine("Warning: {0} not declared dynamic and thus is not dynamically rebindable, "
                                          + "but its name suggests otherwise. Please either indicate ^:dynamic {0} or change the name. ({1}:{2}\n",
                                           sym,Compiler.SourcePathVar.get(),Compiler.LineVar.get());
                    RT.errPrintWriter().Flush();
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

                //  Following comment in JVM version
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
                    Compiler.LineVarDeref(),
                    Compiler.ColumnVarDeref(),
                    v, init, meta, initProvided,isDynamic,shadowsCoreMapping);
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
                throw new Compiler.CompilerException(_source, _line, _column, Compiler.DefSym, Compiler.CompilerException.PhaseExecutionKeyword, e);
            }
        }

        #endregion

        #region Code generation

        static readonly FieldInfo VarNsFI = typeof(Var).GetField("_ns", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        static readonly FieldInfo VarSymFI = typeof(Var).GetField("_sym", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        static readonly MethodInfo NamespaceReferMI = typeof(Namespace).GetMethod("refer");

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            objx.EmitVar(ilg, _var);

            if ( _shadowsCoreMapping )
            {
                LocalBuilder locNs = ilg.DeclareLocal(typeof(Namespace));
                GenContext.SetLocalName(locNs, "ns");

                ilg.Emit(OpCodes.Dup);
                ilg.EmitFieldGet(VarNsFI);
                ilg.Emit(OpCodes.Stloc,locNs);

                LocalBuilder locSym = ilg.DeclareLocal(typeof(Symbol));
                GenContext.SetLocalName(locSym, "sym");

                ilg.Emit(OpCodes.Dup);
                ilg.EmitFieldGet(VarSymFI);
                ilg.Emit(OpCodes.Stloc, locSym);

                ilg.Emit(OpCodes.Ldloc, locNs);
                ilg.Emit(OpCodes.Ldloc, locSym);
                ilg.Emit(OpCodes.Call, NamespaceReferMI);
            }

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
                if (_init is FnExpr expr)
                    expr.EmitForDefn(objx, ilg);
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

        //private static bool IncludesExplicitMetadata(MapExpr expr)
        //{
        //    for (int i = 0; i < expr.KeyVals.count(); i += 2)
        //    {
        //        Keyword k = ((KeywordExpr)expr.KeyVals.nth(i)).Kw;
        //        if ((k != RT.FileKey) &&
        //            (k != RT.DeclaredKey) &&
        //            (k != RT.SourceSpanKey) &&
        //            (k != RT.LineKey) &&
        //            (k != RT.ColumnKey))
        //            return true;
        //    }
        //    return false;
        //}

        #endregion
    }
}
