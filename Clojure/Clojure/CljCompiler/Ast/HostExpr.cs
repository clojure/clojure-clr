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
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    abstract class HostExpr : Expr, MaybePrimitiveExpr
    {
        public sealed class Parser : IParser
        {
            public Expr Parse(object frm)
            {
                ISeq form = (ISeq)frm;

                // form is one of:
                //  (. x fieldname-sym)
                //  (. x 0-ary-method)
                //  (. x propertyname-sym)
                //  (. x methodname-sym args+)
                //  (. x (methodname-sym args?))

                if (RT.Length(form) < 3)
                    throw new ArgumentException("Malformed member expression, expecting (. target member ... )");

                // determine static or instance
                // static target must be symbol, either fully.qualified.Typename or Typename that has been imported

                Type t = Compiler.MaybeType(RT.second(form), false);
                // at this point, t will be non-null if static

                Expr instance = null;
                if (t == null)
                    instance = Compiler.GenerateAST(RT.second(form));

                bool isFieldOrProperty = false;

                if (RT.Length(form) == 3 && RT.third(form) is Symbol)
                {
                    Symbol sym = (Symbol)RT.third(form);
                    if (t != null)
                        isFieldOrProperty =
                            t.GetField(sym.Name, BindingFlags.Static | BindingFlags.Public) != null
                            || t.GetProperty(sym.Name, BindingFlags.Static | BindingFlags.Public) != null;
                    else if (instance != null && instance.HasClrType && instance.ClrType != null)
                        isFieldOrProperty =
                            t.GetField(sym.Name, BindingFlags.Instance | BindingFlags.Public) != null
                            || t.GetProperty(sym.Name, BindingFlags.Instance | BindingFlags.Public) != null;
                }

                if (isFieldOrProperty)
                {
                    Symbol sym = (Symbol)RT.third(form);
                    if (t != null)
                        return new StaticFieldExpr(t, sym.Name);
                    else
                        return new InstanceFieldExpr(instance, sym.Name);
                }


                ISeq call = RT.third(form) is ISeq ? (ISeq)RT.third(form) : RT.next(RT.next(form));

                if (!(RT.first(call) is Symbol))
                    throw new ArgumentException("Malformed member exception");

                string methodName = ((Symbol)RT.first(call)).Name;
                IPersistentVector args = PersistentVector.EMPTY;

                for (ISeq s = RT.next(call); s != null; s = s.next())
                    args = args.cons(Compiler.GenerateAST(s.first()));

                return t != null
                    ? (MethodExpr)(new StaticMethodExpr(t, methodName, args))
                    : (MethodExpr)(new InstanceMethodExpr(instance, methodName, args));
            }
        }
    }
}
