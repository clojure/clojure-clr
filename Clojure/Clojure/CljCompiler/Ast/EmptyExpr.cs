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
using System.Reflection;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class EmptyExpr : Expr
    {
        #region Data

        readonly object _coll;
        public object Coll => _coll;

        #endregion

        #region Ctors

        public EmptyExpr(object coll)
        {
            _coll = coll;
        }

        #endregion

        #region Type mangling

        public bool HasClrType => true;

        public Type ClrType
        {
            get
            {
                return _coll switch
                {
                    IPersistentList => typeof(IPersistentList),
                    IPersistentVector => typeof(IPersistentVector),
                    IPersistentMap => typeof(IPersistentMap),
                    IPersistentSet => typeof(IPersistentSet),
                    _ => throw new InvalidOperationException("Unknown Collection type.")
                };
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
            switch (_coll)
            {
                case IPersistentList:
                case LazySeq:
                    ilg.EmitFieldGet(ListEmptyFI);
                    break;
                case IPersistentVector:
                    ilg.EmitFieldGet(VectorEmptyFI);
                    break;
                case IPersistentMap:
                    ilg.EmitFieldGet(HashMapEmptyFI);
                    break;
                case IPersistentSet:
                    ilg.EmitFieldGet(HashSetEmptyFI);
                    break;
                default:
                    throw new InvalidOperationException("Unknown collection type.");
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() => true;

        #endregion
    }
}
