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
using AstUtils = Microsoft.Scripting.Ast.Utils;
using Microsoft.Scripting;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    class InvokeExpr : Expr
    {
        #region Data

        readonly Expr _fexpr;
        readonly Object _tag;
        readonly IPersistentVector _args;
        readonly string _source;
        readonly IPersistentMap _spanMap;
        bool _isProtocol = false;
        bool _isDirect = false;
        int _siteIndex = -1;
        Type _protocolOn;
        MethodInfo _onMethod;

        static readonly Keyword _onKey = Keyword.intern("on");
        static readonly Keyword _methodMapKey = Keyword.intern("method-map");
        static readonly Keyword _dynamicKey = Keyword.intern("dynamic");

        #endregion

        #region Ctors

        public InvokeExpr(string source, IPersistentMap spanMap, Symbol tag, Expr fexpr, IPersistentVector args)
        {
            _source = source;
            _spanMap = spanMap;
            _fexpr = fexpr;
            _args = args;

            if (fexpr is VarExpr)
            {
                Var fvar = ((VarExpr)fexpr).Var;
                Var pvar = (Var)RT.get(fvar.meta(), Compiler.PROTOCOL_KEY);
                if (pvar != null && Compiler.PROTOCOL_CALLSITES.IsBound)
                {
                    _isProtocol = true;
                    _siteIndex = Compiler.RegisterProtocolCallsite(fvar);
                    Object pon = RT.get(pvar.get(), _onKey);
                    _protocolOn = HostExpr.MaybeType(pon, false);
                    if (_protocolOn != null)
                    {
                        IPersistentMap mmap = (IPersistentMap)RT.get(pvar.get(), _methodMapKey);
                        string mname = Compiler.Munge(((Keyword)mmap.valAt(Keyword.intern(fvar.Symbol))).Symbol.ToString());
                        List<MethodInfo> methods = Reflector.GetMethods(_protocolOn, mname, args.count() - 1,  false);
                        if (methods.Count != 1)
                            throw new ArgumentException(String.Format("No single method: {0} of interface: {1} found for function: {2} of protocol: {3}",
                                mname, _protocolOn.FullName, fvar.Symbol, pvar.Symbol));
                        _onMethod = methods[0];
                    }
                }
                else if (pvar == null && Compiler.VAR_CALLSITES.IsBound
                    && fvar.Namespace.Name.Name.StartsWith("clojure")
                    && !RT.booleanCast(RT.get(RT.meta(fvar), _dynamicKey)))
                {
                    // Java TODO: more specific criteria for binding these
                    _isDirect = true;
                    _siteIndex = Compiler.RegisterVarCallsite(fvar);
                }
            }

            _tag = tag ?? (fexpr is VarExpr ? ((VarExpr)fexpr).Tag : null);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tag != null; }
        }

        public override Type ClrType
        {
            get { return HostExpr.TagToType(_tag); }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ISeq form)
        {
            // TODO: DO we need the recur context here and below?
            Expr fexpr = Compiler.GenerateAST(form.first(),false);

            if ( fexpr is VarExpr && ((VarExpr)fexpr).Var.Equals(Compiler.INSTANCE))
            {
                if ( RT.second(form) is Symbol )
                {
                    Type t = HostExpr.MaybeType(RT.second(form),false);
                    if ( t != null )
                        return new InstanceOfExpr((string)Compiler.SOURCE.deref(), (IPersistentMap)Compiler.SOURCE_SPAN.deref(), t, Compiler.GenerateAST(RT.third(form), false));
                }
            }

            //if ( fexpr is KeywordExpr && RT.count(form) == 2 && Compiler.KEYWORD_CALLSITES.IsBound )
            //{
            //    Expr target = Compiler.GenerateAST(RT.second(form), false);
            //    return new KeywordInvokeExpr((string)Compiler.SOURCE.deref(), (IPersistentMap)Compiler.SOURCE_SPAN.deref(), Compiler.TagOf(form), (KeywordExpr)fexpr, target);
            //}

            IPersistentVector args = PersistentVector.EMPTY;
            for ( ISeq s = RT.seq(form.next()); s != null; s = s.next())
                args = args.cons(Compiler.GenerateAST(s.first(),false));
            return new InvokeExpr((string)Compiler.SOURCE.deref(),
                (IPersistentMap)Compiler.SOURCE_SPAN.deref(), //Compiler.GetSourceSpanMap(form),
                Compiler.TagOf(form),
                fexpr,
                args);
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression basicFn = _fexpr.GenDlr(context);
            basicFn = Expression.Convert(basicFn, typeof(IFn));

            Expression fn;

            // TODO: Determine if this optimization is valid for Immediate mode
            if (_isDirect && context.Mode == CompilerMode.File)
            {
                ParameterExpression v = Expression.Parameter(typeof(IFn));
                Expression initV = Expression.Assign(v, Expression.Field(null, context.ObjExpr.BaseType, context.ObjExpr.VarCallsiteName(_siteIndex)));
                Expression test = Expression.Condition(Expression.Equal(v, Expression.Constant(null,typeof(IFn))), basicFn, v);
                Expression block = Expression.Block(typeof(IFn), new ParameterExpression[] { v }, initV, test);
                fn = block;
            }
            else
                fn = basicFn;

            int argCount = _args.count();

            Expression[] args = new Expression[argCount];

            for (int i = 0; i < argCount; i++ )
                args[i] = Compiler.MaybeBox(((Expr)_args.nth(i)).GenDlr(context));

            Expression call = GenerateInvocation(fn, args);
            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }

        private static Expression GenerateInvocation(Expression fn, Expression[] args)
        {
            MethodInfo mi;
            Expression[] actualArgs;

            if (args.Length <= Compiler.MAX_POSITIONAL_ARITY)
            {
                mi = Compiler.Methods_IFn_invoke[args.Length];
                actualArgs = args;
            }
            else
            {
                // pick up the extended version.
                mi = Compiler.Methods_IFn_invoke[Compiler.MAX_POSITIONAL_ARITY + 1];
                Expression[] leftoverArgs = new Expression[args.Length - Compiler.MAX_POSITIONAL_ARITY];
                Array.Copy(args, Compiler.MAX_POSITIONAL_ARITY, leftoverArgs, 0, args.Length - Compiler.MAX_POSITIONAL_ARITY);

                Expression restArg = Expression.NewArrayInit(typeof(object), leftoverArgs);

                actualArgs = new Expression[Compiler.MAX_POSITIONAL_ARITY + 1];
                Array.Copy(args, 0, actualArgs, 0, Compiler.MAX_POSITIONAL_ARITY);
                actualArgs[Compiler.MAX_POSITIONAL_ARITY] = restArg;
            }

            Expression call = Expression.Call(fn, mi, actualArgs);

            return call;
        }

        #endregion
    }
}
