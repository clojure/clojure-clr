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
    public sealed class LocalBinding
    {
        #region Data

        private readonly Symbol _sym;
        public Symbol Symbol { get { return _sym; } }

        public Symbol Tag { get; set; }

        public Expr Init { get; set; }

        private readonly String _name;
        public String Name { get { return _name; } }

        public int Index { get; set;}
    
        public LocalBuilder LocalVar { get; set; }

        readonly bool _isArg;
        public bool IsArg { get { return _isArg; } }

        readonly bool _isByRef;
        public bool IsByRef { get { return _isByRef; } }

        readonly bool _isThis;
        public bool IsThis { get { return _isThis; } }

        public bool RecurMismatch { get; set; }

        readonly Type _declaredType;
        public Type DeclaredType { get { return _declaredType; } }

        #endregion

        #region C-tors

        public LocalBinding(int index, Symbol sym, Symbol tag, Expr init, Type declaredType, bool isThis, bool isArg, bool isByRef)
        {
            if (Compiler.MaybePrimitiveType(init) != null && tag != null)
                throw new InvalidOperationException("Can't type hint a local with a primitive initializer");

            Index = index;
            _sym = sym;
            Tag = tag;
            Init = init;
            _name = Compiler.munge(sym.Name);
            _isThis = isThis;
            _isArg = isArg;
            _isByRef = isByRef;
            _declaredType = declaredType;
            RecurMismatch = false;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get
            {
                if (Init != null
                    && Init.HasClrType
                    && Util.IsPrimitive(Init.ClrType)
                    && !(Init is MaybePrimitiveExpr))
                    return false;

                return Tag != null || (Init != null && Init.HasClrType);
            }
        }

        public Type ClrType
        {
            get
            {
                return Tag != null
                    ? HostExpr.TagToType(Tag)
                    : Init.ClrType;
            }
        }

        public Type PrimitiveType
        {
            get { return Compiler.MaybePrimitiveType(Init); }
        }

        #endregion
    }
}
