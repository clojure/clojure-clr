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
    public class EmptyExpr : Expr
    {
        #region Data

        readonly object _coll;
        public object Coll { get { return _coll; } }

        #endregion

        #region Ctors

        public EmptyExpr(object coll)
        {
            _coll = coll;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get {
                if (_coll is IPersistentList)
                    return typeof(IPersistentList);
                else if (_coll is IPersistentVector)
                    return typeof(IPersistentVector);
                else if (_coll is IPersistentMap)
                    return typeof(IPersistentMap);
                else if (_coll is IPersistentSet)
                    return typeof(IPersistentSet);
                else
                    throw new InvalidOperationException("Unknown Collection type.");
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            return _coll;
        }

        #endregion

        #region Code generation

        static readonly FieldInfo HashMapEmptyFI = typeof(PersistentArrayMap).GetField("EMPTY");
        static readonly FieldInfo HashSetEmptyFI = typeof(PersistentHashSet).GetField("EMPTY");
        static readonly FieldInfo ListEmptyFI = typeof(PersistentList).GetField("EMPTY");
        static readonly FieldInfo VectorEmptyFI = typeof(PersistentVector).GetField("EMPTY");

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            if (_coll is IPersistentList || _coll is LazySeq) // JVM does not include LazySeq test.  I'm getting it in some places.  LazySeq of 0 size got us here, we'll treat as an empty list
                ilg.EmitFieldGet(ListEmptyFI);
            else if (_coll is IPersistentVector)
                ilg.EmitFieldGet(VectorEmptyFI);
            else if (_coll is IPersistentMap)
                ilg.EmitFieldGet(HashMapEmptyFI);
            else if (_coll is IPersistentSet)
                ilg.EmitFieldGet(HashSetEmptyFI);
            else
                throw new InvalidOperationException("Unknown collection type.");
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
