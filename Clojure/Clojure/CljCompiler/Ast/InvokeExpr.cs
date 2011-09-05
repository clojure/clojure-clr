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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection;
using clojure.lang;

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
        int _siteIndex = -1;
        Type _protocolOn;
        MethodInfo _onMethod;

        static readonly Keyword _onKey = Keyword.intern("on");
        static readonly Keyword _methodMapKey = Keyword.intern("method-map");

        #endregion

        #region Ctors

        public InvokeExpr(string source, IPersistentMap spanMap, Symbol tag, Expr fexpr, IPersistentVector args)
        {
            _source = source;
            _spanMap = spanMap;
            _fexpr = fexpr;
            _args = args;

            VarExpr varFexpr = fexpr as VarExpr;

            if (varFexpr != null)
            {
                Var fvar = varFexpr.Var;
                Var pvar = (Var)RT.get(fvar.meta(), Compiler.ProtocolKeyword);
                if (pvar != null && Compiler.ProtocolCallsitesVar.isBound)
                {
                    _isProtocol = true;
                    _siteIndex = Compiler.RegisterProtocolCallsite(fvar);
                    Object pon = RT.get(pvar.get(), _onKey);
                    _protocolOn = HostExpr.MaybeType(pon, false);
                    if (_protocolOn != null)
                    {
                        IPersistentMap mmap = (IPersistentMap)RT.get(pvar.get(), _methodMapKey);
                        Keyword mmapVal = (Keyword)mmap.valAt(Keyword.intern(fvar.sym));
                        if (mmapVal == null)
                        {
                            throw new ArgumentException(String.Format("No method of interface: {0} found for function: {1} of protocol: {2} (The protocol method may have been defined before and removed.)",
                                _protocolOn.FullName, fvar.Symbol, pvar.Symbol));
                        }
                        String mname = Compiler.munge(mmapVal.Symbol.ToString());
                       
                        IList<MethodBase> methods = Reflector.GetMethods(_protocolOn, mname, null, args.count() - 1,  false);
                        if (methods.Count != 1)
                            throw new ArgumentException(String.Format("No single method: {0} of interface: {1} found for function: {2} of protocol: {3}",
                                mname, _protocolOn.FullName, fvar.Symbol, pvar.Symbol));
                        _onMethod = (MethodInfo) methods[0];
                    }
                }
            }

            //_tag = tag ?? (varFexpr != null ? varFexpr.Tag : null);
            if (tag != null)
                _tag = tag;
            else if (varFexpr != null)
            {
                object arglists = RT.get(RT.meta(varFexpr.Var), Compiler.ArglistsKeyword);
                object sigTag = null;
                for (ISeq s = RT.seq(arglists); s != null; s = s.next())
                {
                    APersistentVector sig = (APersistentVector)s.first();
                    int restOffset = sig.IndexOf(Compiler.AmpersandSym);
                    if (args.count() == sig.count() || (restOffset > -1 && args.count() >= restOffset))
                    {
                        sigTag = Compiler.TagOf(sig);
                        break;
                    }
                }
                _tag = sigTag ?? varFexpr.Tag;
            }
            else
                _tag = null;

        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _tag != null; }
        }

        public Type ClrType
        {
            get { return HostExpr.TagToType(_tag); }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ParserContext pcon, ISeq form)
        {
            pcon = pcon.EvalOrExpr();

            // TODO: DO we need the recur context here and below?
            Expr fexpr = Compiler.Analyze(pcon,form.first());
            VarExpr varFexpr = fexpr as VarExpr;

            if (varFexpr != null && varFexpr.Var.Equals(Compiler.InstanceVar))
            {
                if ( RT.second(form) is Symbol )
                {
                    Type t = HostExpr.MaybeType(RT.second(form),false);
                    if ( t != null )
                        return new InstanceOfExpr((string)Compiler.SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), t, Compiler.Analyze(pcon,RT.third(form)));
                }
            }

            if (varFexpr != null && pcon.Rhc != RHC.Eval)
            {
                Var v = varFexpr.Var;
                object arglists = RT.get(RT.meta(v), Compiler.ArglistsKeyword);
                int arity = RT.count(form.next());
                for (ISeq s = RT.seq(arglists); s != null; s = s.next())
                {
                    IPersistentVector sargs = (IPersistentVector)s.first();
                    if (sargs.count() == arity)
                    {
                        string primc = FnMethod.PrimInterface(sargs);
                        if (primc != null)
                            return Compiler.Analyze(pcon,
                                RT.listStar(Symbol.intern(".invokePrim"),
                                            ((Symbol)form.first()).withMeta(RT.map(RT.TagKey, Symbol.intern(primc))),
                                            form.next()));
                        break;
                    }
                }
            }

            KeywordExpr kwFexpr = fexpr as KeywordExpr;

            if (kwFexpr != null && RT.count(form) == 2 && Compiler.KeywordCallsitesVar.isBound)
            {
                Expr target = Compiler.Analyze(pcon, RT.second(form));
                return new KeywordInvokeExpr((string)Compiler.SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), Compiler.TagOf(form), kwFexpr, target);
            }

            IPersistentVector args = PersistentVector.EMPTY;
            for ( ISeq s = RT.seq(form.next()); s != null; s = s.next())
                args = args.cons(Compiler.Analyze(pcon,s.first()));

            //if (args.count() > Compiler.MAX_POSITIONAL_ARITY)
            //    throw new ArgumentException(String.Format("No more than {0} args supported", Compiler.MAX_POSITIONAL_ARITY));

            return new InvokeExpr((string)Compiler.SourceVar.deref(),
                (IPersistentMap)Compiler.SourceSpanVar.deref(), //Compiler.GetSourceSpanMap(form),
                Compiler.TagOf(form),
                fexpr,
                args);
        }

        #endregion

        #region eval

        public object Eval()
        {
            try
            {
                IFn fn = (IFn)_fexpr.Eval();
                IPersistentVector argvs = PersistentVector.EMPTY;
                for (int i = 0; i < _args.count(); i++)
                    argvs = (IPersistentVector)argvs.cons(((Expr)_args.nth(i)).Eval());
                return fn.applyTo(RT.seq(Util.Ret1(argvs, argvs = null)));
            }
            catch (Compiler.CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Compiler.CompilerException(_source, Compiler.GetLineFromSpanMap(_spanMap), e);
            }
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression basicFn = _fexpr.GenCode(RHC.Expression, objx, context);
            basicFn = Expression.Convert(basicFn, typeof(IFn));

            if (_isProtocol)
                return GenProto(rhc,objx,context,basicFn);

            return GenNonProto(rhc, objx, context, basicFn);
        }

        private Expression GenNonProto(RHC rhc, ObjExpr objx, GenContext context, Expression basicFn)
        {
            return GenerateArgsAndCall(rhc, objx, context, basicFn);
        }


        private Expression GenerateArgsAndCall(RHC rhc, ObjExpr objx, GenContext context, Expression fn, Expression arg0)
        {
            Expression[] args = new Expression[_args.count()];
            args[0] = arg0;
            GenerateArgs(rhc, objx, context, args, 1);
            return GenerateCall(fn,args,context.IsDebuggable);
        }

        private Expression GenerateArgsAndCall(RHC rhc, ObjExpr objx, GenContext context, Expression fn)
        {
            Expression[] args = new Expression[_args.count()];
            GenerateArgs(rhc, objx, context, args, 0);
            return GenerateCall(fn, args, context.IsDebuggable);
        }

        private void GenerateArgs(RHC rhc, ObjExpr objx, GenContext context, Expression[] args, int firstIndex)
        {
            int argCount = _args.count();

            for (int i = firstIndex; i < argCount; i++)
            {
                Expression bare = ((Expr)_args.nth(i)).GenCode(RHC.Expression,objx,context);
                args[i] = Compiler.MaybeBox(bare);
            }

        }

        private Expression GenerateCall(Expression fn, Expression[] args, bool isDebuggable)
        {
            Expression call = GenerateInvocation(fn, args);
            call = Compiler.MaybeAddDebugInfo(call, _spanMap, isDebuggable);
            return call;
        }

        private static Expression GenerateInvocation(Expression fn, Expression[] args)
        {
            MethodInfo mi;
            Expression[] actualArgs;

            if (args.Length <= Compiler.MaxPositionalArity)
            {
                mi = Compiler.Methods_IFn_invoke[args.Length];
                actualArgs = args;
            }
            else
            {
                // pick up the extended version.
                mi = Compiler.Methods_IFn_invoke[Compiler.MaxPositionalArity + 1];
                Expression[] leftoverArgs = new Expression[args.Length - Compiler.MaxPositionalArity];
                Array.Copy(args, Compiler.MaxPositionalArity, leftoverArgs, 0, args.Length - Compiler.MaxPositionalArity);

                Expression restArg = Expression.NewArrayInit(typeof(object), leftoverArgs);

                actualArgs = new Expression[Compiler.MaxPositionalArity + 1];
                Array.Copy(args, 0, actualArgs, 0, Compiler.MaxPositionalArity);
                actualArgs[Compiler.MaxPositionalArity] = restArg;
            }

            if (fn.Type != typeof(IFn))
                fn = Expression.Convert(fn, typeof(IFn));

            Expression call = Expression.Call(fn, mi, actualArgs);

            return call;
        }

        // TODO: PRIORITY: IMPLEMENT protocolOn

        private Expression GenProto(RHC rhc, ObjExpr objx, GenContext context, Expression fn)
        {
            switch (objx.FnMode)
            {
                case FnMode.Light:
                    return GenProtoLight(rhc,objx,context, fn);
                case FnMode.Full:
                    return GenProtoFull(rhc, objx, context, fn);
                default:
                    throw Util.UnreachableCode();
            }
        }

        // TODO: remove duplicate code between GenProtoLight and GenProtoFull

        private Expression GenProtoLight(RHC rhc, ObjExpr objx, GenContext context, Expression fn)
        {
            Var v = ((VarExpr)_fexpr).Var;
            Expr e = (Expr)_args.nth(0);

            ParameterExpression targetParam = Expression.Parameter(typeof(Object), "target");
            ParameterExpression targetTypeParam = Expression.Parameter(typeof(Type), "targetType");
            ParameterExpression vpfnParam = Expression.Parameter(typeof(AFunction), "vpfn");
            //ParameterExpression thisParam = objx.ThisParam;

            Expression targetParamAssign = Expression.Assign(targetParam, Compiler.MaybeBox(e.GenCode(RHC.Expression,objx,context)));
            Expression targetTypeParamAssign =
                Expression.Assign(
                    targetTypeParam,
                    Expression.Call(null, Compiler.Method_Util_classOf, targetParam));

            Expression vpfnParamAssign =
                Expression.Assign(
                    vpfnParam,
                    Expression.Convert(Expression.Call(objx.GenVar(context, v), Compiler.Method_Var_getRawRoot), typeof(AFunction)));

            if (_protocolOn == null)
            {
                return Expression.Block(
                    new ParameterExpression[] { targetParam, targetTypeParam, vpfnParam },
                    targetParamAssign,
                    targetTypeParamAssign,
                    vpfnParamAssign,
                    GenerateArgsAndCall(rhc, objx, context, vpfnParam, targetParam));
            }
            else
            {
                Expression[] args = new Expression[_args.count() - 1];
                for (int i = 1; i < _args.count(); i++)
                {
                    Expression bare = ((Expr)_args.nth(i)).GenCode(RHC.Expression,objx,context);
                    args[i - 1] = Compiler.MaybeBox(bare);
                }

                return Expression.Block(
                     new ParameterExpression[] { targetParam, targetTypeParam, vpfnParam },
                     targetParamAssign,
                     targetTypeParamAssign,
                     Expression.Condition(
                        Expression.Not(Expression.TypeIs(targetParam, _protocolOn)),
                        Expression.Block(
                            vpfnParamAssign,
                            GenerateArgsAndCall(rhc, objx, context, vpfnParam, targetParam)),
                         Compiler.MaybeBox(Expression.Call(Expression.Convert(targetParam, _protocolOn), _onMethod, args))));
            }
        }


        private Expression GenProtoFull(RHC rhc, ObjExpr objx, GenContext context, Expression fn)
        {
            Var v = ((VarExpr)_fexpr).Var;
            Expr e = (Expr)_args.nth(0);

            ParameterExpression targetParam = Expression.Parameter(typeof(Object), "target");
            ParameterExpression targetTypeParam = Expression.Parameter(typeof(Type), "targetType");
            ParameterExpression vpfnParam = Expression.Parameter(typeof(AFunction), "vpfn");
            ParameterExpression thisParam = objx.ThisParam;

            Expression targetParamAssign = Expression.Assign(targetParam, Expression.Convert(e.GenCode(RHC.Expression, objx, context),targetParam.Type));
            Expression targetTypeParamAssign =
                Expression.Assign(
                    targetTypeParam,
                    Expression.Call(null, Compiler.Method_Util_classOf, targetParam));

            Expression cachedTypeField = Expression.Field(thisParam, objx.CachedTypeField(_siteIndex));

            Expression setCachedClass =
                Expression.Assign(
                    cachedTypeField,
                    targetTypeParam);

            Expression vpfnParamAssign =
                Expression.Assign(
                    vpfnParam,
                    Expression.Convert(Expression.Call(objx.GenVar(context, v), Compiler.Method_Var_getRawRoot), typeof(AFunction)));

            if (_protocolOn == null)
            {
                return Expression.Block(
                    new ParameterExpression[] { targetParam, targetTypeParam, vpfnParam },
                    targetParamAssign,
                    targetTypeParamAssign,
                    Expression.IfThen(
                        Expression.NotEqual(targetTypeParam,cachedTypeField),
                        setCachedClass),
                    vpfnParamAssign,
                    GenerateArgsAndCall(rhc, objx, context, vpfnParam, targetParam));
            }
            else
            {
                Expression[] args = new Expression[_args.count()-1];
                for (int i = 1; i < _args.count(); i++)
                {
                    Expression bare = ((Expr)_args.nth(i)).GenCode(RHC.Expression, objx, context);
                    args[i - 1] = Compiler.MaybeBox(bare);
                }

                return Expression.Block(
                     new ParameterExpression[] { targetParam, targetTypeParam, vpfnParam },
                     targetParamAssign,
                     targetTypeParamAssign,
                     Expression.Condition(
                        Expression.And(
                            Expression.NotEqual(targetTypeParam, cachedTypeField),
                            Expression.TypeIs(targetParam, _protocolOn)),
                        Compiler.MaybeBox(Expression.Call(Expression.Convert(targetParam, _protocolOn), _onMethod, args)),
                        Expression.Block(
                            Expression.IfThen(
                                Expression.NotEqual(targetTypeParam, cachedTypeField),
                                setCachedClass),
                            vpfnParamAssign,
                            GenerateArgsAndCall(rhc, objx, context, vpfnParam, targetParam))));
            }
        }

        #endregion
    }
}
