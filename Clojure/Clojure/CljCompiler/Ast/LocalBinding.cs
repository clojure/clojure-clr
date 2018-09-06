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

        bool _hasTypeCached = false;
        bool _cachedHasType = false;
        Type _cachedType;

        #endregion

        #region C-tors

        public LocalBinding(int index, Symbol sym, Symbol tag, Expr init, Type declaredType, bool isThis, bool isArg, bool isByRef)
        {
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
                if (!_hasTypeCached)
                {
                    if (Init != null
                    && Init.HasClrType
                    && Util.IsPrimitive(Init.ClrType)
                    && !(Init is MaybePrimitiveExpr))
                        _cachedHasType = false;
                    else
                        _cachedHasType = Tag != null || (Init != null && Init.HasClrType);
                    _hasTypeCached = true;
                }
                return _cachedHasType;
            }
        }

        public Type ClrType
        {
            get
            {
                if (_cachedType == null)
                    _cachedType = Tag != null ? HostExpr.TagToType(Tag) : Init.ClrType;
                return _cachedType;
            }
        }

        public Type PrimitiveType
        {
            get { return Compiler.MaybePrimitiveType(Init); }
        }

        #endregion
    }
}
