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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Text;


namespace clojure.lang.CljCompiler.Ast
{
    class StaticInvokeExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly Type _target;
        readonly Type _retType;
        readonly Type[] _paramTypes;
        readonly IPersistentVector _args;
        readonly bool _variadic;
        readonly Symbol _tag;

        #endregion

        #region Ctors

        public StaticInvokeExpr(Type target, Type retType, Type[] paramTypes, bool variadic, IPersistentVector args, Symbol tag)
        {
            _target = target;
            _retType = retType;
            _paramTypes = paramTypes;
            _variadic = variadic;
            _args = args;
            _tag = tag;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return _tag != null ? HostExpr.TagToType(_tag) : _retType; }
        }

        #endregion

        #region parsing

        public static Expr Parse(Var v, ISeq args, Symbol tag)
        {
            IPersistentCollection paramlists = (IPersistentCollection)RT.get(v.meta(), Compiler.ARGLISTS_KEY);
            if (paramlists == null)
                throw new InvalidOperationException("Can't call static fn with no arglists " + v);

            IPersistentVector paramlist = null;
            int argcount = RT.count(args);
            bool variadic = false;
            for (ISeq aseq = RT.seq(paramlists); aseq != null; aseq = aseq.next())
            {
                if (!(aseq.first() is IPersistentVector))
                    throw new InvalidOperationException("Expected vector arglist, had: " + aseq.first());
                IPersistentVector alist = (IPersistentVector)aseq.first();
                if (alist.count() > 1 && alist.nth(alist.count() - 2).Equals(Compiler._AMP_))
                {
                    if (argcount >= alist.count() - 2)
                    {
                        paramlist = alist;
                        variadic = true;
                    }
                }
                else if (alist.count() == argcount)
                {
                    paramlist = alist;
                    variadic = false;
                    break;
                }
            }
            if (paramlist == null)
                throw new ArgumentException(String.Format("Invalid arity - can't call: {0} with {1} args", v, argcount));

            Type retClass = Compiler.TagType(Compiler.TagOf(paramlist));

            List<Type> paramTypes = new List<Type>();

            if (variadic)
            {
                for (int i = 0; i < paramlist.count() - 2; i++)
                {
                    Type pt = Compiler.TagType(Compiler.TagOf(paramlist.nth(i)));
                    paramTypes.Add(pt);
                }
                paramTypes.Add(typeof(ISeq));
            }
            else
            {
                for (int i = 0; i < paramlist.count(); i++)
                {
                    Type pt = Compiler.TagType(Compiler.TagOf(paramlist.nth(i)));
                    paramTypes.Add(pt);
                }
            }

            string cname = v.Namespace.Name.Name.Replace('.', '/').Replace('-', '_') + "$" + Compiler.munge(v.sym.Name);
            Type target = RT.classForName(cname);  // not sure this will work.

            IPersistentVector argv = PersistentVector.EMPTY;
            for (ISeq s = RT.seq(args); s != null; s = s.next())
                argv = argv.cons(Compiler.Analyze(new ParserContext(RHC.Expression), s.first()));

            return new StaticInvokeExpr(target, retClass, paramTypes.ToArray(), variadic, argv, tag);
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval StaticInvokeExpr");
        }

        #endregion

        #region Generating code

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression unboxed = GenCodeUnboxed(rhc, objx, context);
            Expression boxed = Compiler.MaybeBox(unboxed);
            return boxed;
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return _retType.IsPrimitive; }
        }

        public Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            throw new NotImplementedException("TODO: Implement StaticInvodeExpr.GenCodeUnboxed");
        }

        #endregion
    }
}
