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
using System.Linq;
using System.Text;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    class FnExpr : ObjExpr
    {
        #region Data

        static readonly Keyword KW_ONCE = Keyword.intern(null, "once");
        static readonly Keyword KW_SUPER_NAME = Keyword.intern(null, "super-name");

        string _simpleName;

        bool _onceOnly = false;

        //int _line;

        #endregion

        #region Ctors

        public FnExpr(object tag)
            :base(tag)
        {
        }

        #endregion
        
        #region Type mangling



        #endregion

        #region Misc

        // This naming convention drawn from the Java code.
        internal void ComputeNames(ISeq form, string name)
        {
            FnMethod enclosingMethod = (FnMethod)Compiler.METHODS.deref();

            string baseName = enclosingMethod != null
                ? (enclosingMethod.Fn._name + "$")
                : (Compiler.Munge(Compiler.CurrentNamespace.Name.Name) + "$");

            if (RT.second(form) is Symbol)
                name = ((Symbol)RT.second(form)).Name;

            _simpleName = (name == null ? "fn" : Compiler.Munge(name).Replace(".", "_DOT_")) + "__" + RT.nextID();
            _name = baseName + _simpleName;
            _internalName = _name.Replace('.', '/');

            _objType = RT.classForName(_internalName);
            // fn.fntype = Type.getObjectType(fn.internalName) -- JAVA            
        }

        bool IsVariadic { get { return _variadicMethod != null; } }

        #endregion

        #region Parsing

        public static Expr Parse(object frm, string name, bool isRecurContext)
        {
            ISeq form = (ISeq)frm;

            FnExpr fn = new FnExpr(Compiler.TagOf(form));

            if (((IMeta)form.first()).meta() != null)
            {
                fn._onceOnly = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ONCE));
                fn._superName = (string)RT.get(RT.meta(form.first()), KW_SUPER_NAME);
            }


            fn.ComputeNames(form, name);

            try
            {
                Var.pushThreadBindings(RT.map(
                    Compiler.CONSTANTS, PersistentVector.EMPTY,
                    Compiler.KEYWORDS, PersistentHashMap.EMPTY,
                    Compiler.VARS, PersistentHashMap.EMPTY));

                //arglist might be preceded by symbol naming this fn
                if (RT.second(form) is Symbol)
                {
                    fn._thisName = ((Symbol)RT.second(form)).Name;
                    form = RT.cons(Compiler.FN, RT.next(RT.next(form)));
                }
                //  Added to improve stack trace messages.
                //  This seriously hoses compilation of core.clj.  Needs investigation.
                //  Backing this out.
                //else if (name != null)
                //{
                //    fn._thisName = name;
                //}

                // Normalize body
                // If it is (fn [arg...] body ...), turn it into
                //          (fn ([arg...] body...))
                // so that we can treat uniformly as (fn ([arg...] body...) ([arg...] body...) ... )
                if (RT.second(form) is IPersistentVector)
                    form = RT.list(Compiler.FN, RT.next(form));

                //fn._line = (int)Compiler.LINE.deref();

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

        #endregion

        #region Code generation

        protected override void GenerateMethods(GenContext context)
        {
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.GenerateCode(context);
            }
        }

        #endregion
        
 
    }
}
