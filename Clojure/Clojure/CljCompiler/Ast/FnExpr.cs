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

namespace clojure.lang.CljCompiler.Ast
{
    class FnExpr : Expr
    {
        #region Data

        static readonly Keyword KW_ONCE = Keyword.intern(null, "once");
        static readonly Keyword KW_SUPER_NAME = Keyword.intern(null, "super-name");

        IPersistentCollection _methods;
        FnMethod _variadicMethod = null;
        string _name;
        string _simpleName;
        string _internalName;

        string _thisName;
        public string ThisName
        {
            get { return _thisName; }
            set { _thisName = value; }
        }

        Type _fnType;
        readonly object _tag;
        IPersistentMap _closes = PersistentHashMap.EMPTY;          // localbinding -> itself
        IPersistentMap _keywords = PersistentHashMap.EMPTY;         // Keyword -> KeywordExpr
        IPersistentMap _vars = PersistentHashMap.EMPTY;
        PersistentVector _constants;
        int _constantsID;
        bool _onceOnly = false;
        string _superName = null;
        
        #endregion

        #region Ctors

        public FnExpr(object tag)
        {
            _tag = tag;
        }

        #endregion

        
        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return _tag != null ? Compiler.TagToType(_tag) : typeof(IFn); }
        }

        #endregion

        // This naming convention drawn from the Java code.
        internal void ComputeNames(ISeq form)
        {
            FnMethod enclosingMethod = (FnMethod)Compiler.METHODS.deref();

            string baseName = enclosingMethod != null
                ? (enclosingMethod.Fn._name + "$")
                : (Compiler.Munge(Compiler.CurrentNamespace.Name.Name) + "$");

            if (RT.second(form) is Symbol)
                _thisName = ((Symbol)RT.second(form)).Name;

            _simpleName = (_name == null ? "fn" : Compiler.Munge(_name).Replace(".", "_DOT_")) + "__" + RT.nextID();
            _name = baseName + _simpleName;
            _internalName = _name.Replace('.', '/');
            _fnType = RT.classForName(_internalName);
            // fn.fntype = Type.getObjectType(fn.internalName) -- JAVA            
        }


        public sealed class Parser : IParser
        {
            public Expr Parse(object frm)
            {
                ISeq form = (ISeq)frm;

                FnExpr fn = new FnExpr(Compiler.TagOf(form));

                if (((IMeta)form.first()).meta() != null)
                {
                    fn._onceOnly = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ONCE));
                    fn._superName = (string)RT.get(RT.meta(form.first()), KW_SUPER_NAME);
                }


                fn.ComputeNames(form);

                try
                {
                    Var.pushThreadBindings(RT.map(
                        Compiler.CONSTANTS, PersistentVector.EMPTY,
                        Compiler.KEYWORDS, PersistentHashMap.EMPTY,
                        Compiler.VARS, PersistentHashMap.EMPTY));

                    //arglist might be preceded by symbol naming this fn
                    if (RT.second(form) is Symbol)
                        form = RT.cons(Compiler.FN, RT.next(RT.next(form)));

                    // Normalize body
                    // If it is (fn [arg...] body ...), turn it into
                    //          (fn ([arg...] body...))
                    // so that we can treat uniformly as (fn ([arg...] body...) ([arg...] body...) ... )
                    if (RT.second(form) is IPersistentVector)
                        form = RT.list(Compiler.FN, RT.next(form));


                    FnMethod variadicMethod = null;
                    SortedDictionary<int, FnMethod> methods = new SortedDictionary<int, FnMethod>();

                    for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                    {
                        FnMethod f = FnMethod.Parse(fn, (ISeq)RT.first(s));
                        if (f.IsVariadic)
                        {
                            if (variadicMethod == null)
                                variadicMethod = f;
                            else
                                throw new Exception("Can't have more than 1 variadic overload");
                        }
                        else if (!methods.ContainsKey(f.RequiredArity))
                            methods[f.RequiredArity] = f;
                        else
                            throw new Exception("Can't have 2 overloads with the same arity.");
                    }

                    if (variadicMethod != null && methods.Count > 0 && methods.Keys.Max() >= variadicMethod.NumParams)
                        throw new Exception("Can't have fixed arity methods with more params than the variadic method.");

                    IPersistentCollection allMethods = null;
                    foreach (FnMethod method in methods.Values)
                        allMethods = RT.conj(allMethods, method);
                    if (variadicMethod != null)
                        allMethods = RT.conj(allMethods, variadicMethod);

                    fn._methods = allMethods;
                    fn._variadicMethod = variadicMethod;
                    fn._keywords = (IPersistentMap)Compiler.KEYWORDS.deref();
                    fn._vars = (IPersistentMap)Compiler.VARS.deref();
                    fn._constants = (PersistentVector)Compiler.CONSTANTS.deref();
                    fn._constantsID = RT.nextID();
                }
                finally
                {
                    Var.popThreadBindings();
                }
                // JAVA: fn.compile();
                return fn;                
            }

        }

    }
}
