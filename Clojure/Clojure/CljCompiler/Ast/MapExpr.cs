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
    public class MapExpr : Expr
    {
        #region Data

        readonly IPersistentVector _keyvals;
        public IPersistentVector KeyVals { get { return _keyvals; } }

        #endregion

        #region Ctors

        public MapExpr(IPersistentVector keyvals)
        {
            _keyvals = keyvals;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return typeof(IPersistentMap); }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ParserContext pcon, IPersistentMap form)
        {
            ParserContext pconToUse = pcon.EvalOrExpr();

            bool keysConstant = true;
            bool valsConstant = true;
            bool allConstantKeysUnique = true;
            IPersistentSet constantKeys = PersistentHashSet.EMPTY;

            IPersistentVector keyvals = PersistentVector.EMPTY;

            for (ISeq s = RT.seq(form); s != null; s = s.next())
            {
                IMapEntry e = (IMapEntry)s.first();
                Expr k = Compiler.Analyze(pconToUse, e.key());
                Expr v = Compiler.Analyze(pconToUse, e.val());
                keyvals = (IPersistentVector)keyvals.cons(k);
                keyvals = (IPersistentVector)keyvals.cons(v);
                if (k is LiteralExpr)
                {
                    object kval = k.Eval();
                    if (constantKeys.contains(kval))
                        allConstantKeysUnique = false;
                    else
                        constantKeys = (IPersistentSet)constantKeys.cons(kval);
                }
                else
                    keysConstant = false;
                if (!(v is LiteralExpr))
                    valsConstant = false;
            }

            Expr ret = new MapExpr(keyvals);

            if (form is IObj iobjForm && iobjForm.meta() != null)
                return Compiler.OptionallyGenerateMetaInit(pcon, form, ret);
            //else if (constant)
            //{
            // This 'optimzation' works, mostly, unless you have nested map values.
            // The nested map values do not participate in the constants map, so you end up with the code to create the keys.
            // Result: huge duplication of keyword creation.  3X increase in init time to the REPL.
            //    //IPersistentMap m = PersistentHashMap.EMPTY;
            //    //for (int i = 0; i < keyvals.length(); i += 2)
            //    //    m = m.assoc(((LiteralExpr)keyvals.nth(i)).Val, ((LiteralExpr)keyvals.nth(i + 1)).Val);
            //    //return new ConstantExpr(m);
            //    return ret;
            //}
            else if (keysConstant)
            {
                // TBD: Add more detail to exception thrown below.
                if (!allConstantKeysUnique)
                    throw new ArgumentException("Duplicate constant keys in map");
                if (valsConstant)
                {
                    // This 'optimzation' works, mostly, unless you have nested map values.
                    // The nested map values do not participate in the constants map, so you end up with the code to create the keys.
                    // Result: huge duplication of keyword creation.  3X increase in init time to the REPL.
                    //IPersistentMap m = PersistentArrayMap.EMPTY;
                    //for (int i = 0; i < keyvals.length(); i += 2)
                    //    m = m.assoc(((LiteralExpr)keyvals.nth(i)).Val, ((LiteralExpr)keyvals.nth(i + 1)).Val);
                    //return new ConstantExpr(m);
                    return ret;
                }
                else
                    return ret;
            }
            else
                return ret;
        }

        #endregion

        #region eval

        public object Eval()
        {
            Object[] ret = new Object[_keyvals.count()];
            for (int i = 0; i < _keyvals.count(); i++)
                ret[i] = ((Expr)_keyvals.nth(i)).Eval();
            return RT.map(ret);
        }


        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            bool allKeysConstant = true;
            bool allConstantKeysUnique = true;
            IPersistentSet constantKeys = PersistentHashSet.EMPTY;

            for (int i = 0; i < _keyvals.count(); i += 2)
            {
                Expr k = (Expr)_keyvals.nth(i);
                if (k is LiteralExpr)
                {
                    object kval = k.Eval();
                    if (constantKeys.contains(kval))
                        allConstantKeysUnique = false;
                    else
                        constantKeys = (IPersistentSet)constantKeys.cons(kval);
                }
                else
                {
                    allKeysConstant = false;
                }
            }

            MethodExpr.EmitArgsAsArray(_keyvals, objx, ilg);

            if ((allKeysConstant && allConstantKeysUnique) || (_keyvals.count() <= 2))
                ilg.EmitCall(Compiler.Method_RT_mapUniqueKeys);
            else
                ilg.EmitCall(Compiler.Method_RT_map);

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);            
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
