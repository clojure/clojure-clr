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
                if (pvar != null && Compiler.PROTOCOL_CALLSITES.isBound)
                {
                    _isProtocol = true;
                    _siteIndex = Compiler.RegisterProtocolCallsite(fvar);
                    Object pon = RT.get(pvar.get(), _onKey);
                    _protocolOn = HostExpr.MaybeType(pon, false);
                    if (_protocolOn != null)
                    {
                        IPersistentMap mmap = (IPersistentMap)RT.get(pvar.get(), _methodMapKey);
                        string mname = Compiler.munge(((Keyword)mmap.valAt(Keyword.intern(fvar.Symbol))).Symbol.ToString());
                        List<MethodInfo> methods = Reflector.GetMethods(_protocolOn, mname, args.count() - 1,  false);
                        if (methods.Count != 1)
                            throw new ArgumentException(String.Format("No single method: {0} of interface: {1} found for function: {2} of protocol: {3}",
                                mname, _protocolOn.FullName, fvar.Symbol, pvar.Symbol));
                        _onMethod = methods[0];
                    }
                }
                else if (pvar == null && Compiler.VAR_CALLSITES.isBound
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
            ParserContext pcon = new ParserContext(false, false);

            // TODO: DO we need the recur context here and below?
            Expr fexpr = Compiler.GenerateAST(form.first(),pcon);

            if ( fexpr is VarExpr && ((VarExpr)fexpr).Var.Equals(Compiler.INSTANCE))
            {
                if ( RT.second(form) is Symbol )
                {
                    Type t = HostExpr.MaybeType(RT.second(form),false);
                    if ( t != null )
                        return new InstanceOfExpr((string)Compiler.SOURCE.deref(), (IPersistentMap)Compiler.SOURCE_SPAN.deref(), t, Compiler.GenerateAST(RT.third(form), pcon));
                }
            }

            if (fexpr is KeywordExpr && RT.count(form) == 2 && Compiler.KEYWORD_CALLSITES.isBound)
            {
                Expr target = Compiler.GenerateAST(RT.second(form), pcon);
                return new KeywordInvokeExpr((string)Compiler.SOURCE.deref(), (IPersistentMap)Compiler.SOURCE_SPAN.deref(), Compiler.TagOf(form), (KeywordExpr)fexpr, target);
            }

            IPersistentVector args = PersistentVector.EMPTY;
            for ( ISeq s = RT.seq(form.next()); s != null; s = s.next())
                args = args.cons(Compiler.GenerateAST(s.first(),pcon));
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

            if (_isProtocol)
                return GenProto(context,basicFn);

            return GenNonProto(context,basicFn);
        }

        private Expression GenNonProto(GenContext context, Expression basicFn)
        {
            Expression fn;

            //if (_isDirect && context.Mode == CompilerMode.File)
            if (_isDirect && context.FnCompileMode == FnMode.Full)
                {
                // TODO: Determine if this optimization is valid for Immediate mode
                ParameterExpression v = Expression.Parameter(typeof(IFn));
                Expression initV = Expression.Assign(v, Expression.Field(null, context.ObjExpr.BaseType, context.ObjExpr.VarCallsiteName(_siteIndex)));
                Expression test = Expression.Condition(Expression.Equal(v, Expression.Constant(null, typeof(IFn))), basicFn, v);
                Expression block = Expression.Block(typeof(IFn), new ParameterExpression[] { v }, initV, test);
                fn = block;
            }
            else
                fn = basicFn;

            return GenerateArgsAndCall(context, fn);
        }


        private Expression GenerateArgsAndCall(GenContext context, Expression fn, Expression arg0)
        {
            Expression[] args = new Expression[_args.count()];
            args[0] = arg0;
            GenerateArgs(context,args,1);
            return GenerateCall(fn,args);
        }

        private Expression GenerateArgsAndCall(GenContext context, Expression fn)
        {
            Expression[] args = new Expression[_args.count()];
            GenerateArgs(context,args,0);
            return GenerateCall(fn, args);
        }

        private void GenerateArgs(GenContext context, Expression[] args, int firstIndex)
        {
            int argCount = _args.count();

            for (int i = firstIndex; i < argCount; i++)
            {
                Expression bare = ((Expr)_args.nth(i)).GenDlr(context);
                args[i] = Compiler.MaybeBox(bare);
            }

        }

        private Expression GenerateCall(Expression fn, Expression[] args)
        {
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

            if (fn.Type != typeof(IFn))
                fn = Expression.Convert(fn, typeof(IFn));

            Expression call = Expression.Call(fn, mi, actualArgs);

            return call;
        }

        // TODO: PRIORITY: IMPLEMENT protocolOn

        private Expression GenProto(GenContext context, Expression fn)
        {
            switch (context.FnCompileMode)
            {
                case FnMode.Light:
                    return GenProtoLight(context, fn);
                case FnMode.Full:
                    return GenProtoFull(context, fn);
                default:
                    throw Util.UnreachableCode();
            }
        }

        // TODO: remove duplicate code between GenProtoLight and GenProtoFull

        private Expression GenProtoLight(GenContext context, Expression fn)
        {
            Var v = ((VarExpr)_fexpr).Var;
            Expr e = (Expr)_args.nth(0);

            ParameterExpression targetParam = Expression.Parameter(typeof(Object), "target");
            ParameterExpression targetTypeParam = Expression.Parameter(typeof(Type), "targetType");
            ParameterExpression vpfnParam = Expression.Parameter(typeof(AFunction), "vpfn");
            ParameterExpression thisParam = context.ObjExpr.ThisParam;

            Expression targetParamAssign = Expression.Assign(targetParam, Compiler.MaybeBox(e.GenDlr(context)));
            Expression targetTypeParamAssign =
                Expression.Assign(
                    targetTypeParam,
                    Expression.Call(null, Compiler.Method_Util_classOf, targetParam));

            Expression vpfnParamAssign =
                Expression.Assign(
                    vpfnParam,
                    Expression.Convert(Expression.Call(context.ObjExpr.GenVar(context, v), Compiler.Method_Var_getRawRoot), typeof(AFunction)));

            if (_protocolOn == null)
            {
                return Expression.Block(
                    new ParameterExpression[] { targetParam, targetTypeParam, vpfnParam },
                    targetParamAssign,
                    targetTypeParamAssign,
                    vpfnParamAssign,
                    GenerateArgsAndCall(context, vpfnParam, targetParam));
            }
            else
            {
                Expression[] args = new Expression[_args.count() - 1];
                for (int i = 1; i < _args.count(); i++)
                {
                    Expression bare = ((Expr)_args.nth(i)).GenDlr(context);
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
                            GenerateArgsAndCall(context, vpfnParam, targetParam)),
                         Compiler.MaybeBox(Expression.Call(Expression.Convert(targetParam, _protocolOn), _onMethod, args))));

            }

            //Var v = ((VarExpr)_fexpr).Var;

            //Expr e = (Expr)_args.nth(0);

            //ParameterExpression fnParam = Expression.Parameter(typeof(IFn), "fn");
            //ParameterExpression targetParam = Expression.Parameter(typeof(Object), "target");
            //ParameterExpression targetTypeParam = Expression.Parameter(typeof(Type), "targetType");
            //ParameterExpression vpfnParam = Expression.Parameter(typeof(AFunction), "vpfn");
            //ParameterExpression implParam = Expression.Parameter(typeof(IFn), "implFn");
            //ParameterExpression thisParam = context.ObjExpr.ThisParam;


            //Expression fnParamAssign = Expression.Assign(fnParam, Expression.Convert(fn, typeof(IFn)));
            //Expression targetParamAssign = Expression.Assign(targetParam, Compiler.MaybeBox(e.GenDlr(context)));
            //Expression targetTypeParamAssign =
            //    Expression.Assign(
            //        targetTypeParam,
            //        Expression.Call(null, Compiler.Method_Util_classOf, targetParam));
            //Expression vpfnParamAssign =
            //    Expression.Assign(
            //        vpfnParam,
            //        Expression.Convert(Expression.Call(context.ObjExpr.GenVar(context, v), Compiler.Method_Var_getRawRoot), typeof(AFunction)));

            //Expression implParamAssign =
            //    Expression.Block(
            //        Expression.Assign(
            //            implParam,
            //            Expression.Call(
            //                Expression.Property(vpfnParam, Compiler.Method_AFunction_MethodImplCache),
            //                Compiler.Method_MethodImplCache_fnFor,
            //                targetTypeParam)),
            //        Expression.IfThen(
            //            Expression.Equal(implParam, Expression.Constant(null)),
            //            Expression.Assign(implParam, vpfnParam)));



            //LabelTarget clear1Label = Expression.Label("clear1");
            //LabelTarget clear2Label = Expression.Label("clear2");
            //LabelTarget callLabel = Expression.Label("call");

            //Expression block1 = Expression.Block(fnParamAssign, targetParamAssign);
            //Expression block2 = Expression.Block(
            //    targetTypeParamAssign,
            //    vpfnParamAssign,
            //    implParamAssign,
            //    GenerateArgsAndCall(context, fnParam, targetParam));

            //Expression block;

            //if (_protocolOn != null)
            //{
            //    Expression[] args = new Expression[_args.count() - 1];
            //    for (int i = 1; i < _args.count(); i++)
            //    {
            //        Expression bare = ((Expr)_args.nth(i)).GenDlr(context);
            //        args[i - 1] = Compiler.MaybeBox(bare);
            //    }

            //    block = Expression.Block(
            //        new ParameterExpression[] { fnParam, targetParam, targetTypeParam, vpfnParam, implParam },
            //        block1,
            //        Expression.Condition(
            //            Expression.TypeIs(targetParam, _protocolOn),
            //            Compiler.MaybeBox(Expression.Call(Expression.Convert(targetParam, _protocolOn), _onMethod, args)),
            //            block2));
            //}
            //else
            //    block = Expression.Block(
            //            new ParameterExpression[] { fnParam, targetParam, targetTypeParam, vpfnParam, implParam },
            //            block1,
            //            block2);

            //return block;
        }


        private Expression GenProtoFull(GenContext context, Expression fn)
        {
            Var v = ((VarExpr)_fexpr).Var;
            Expr e = (Expr)_args.nth(0);

            ParameterExpression targetParam = Expression.Parameter(typeof(Object), "target");
            ParameterExpression targetTypeParam = Expression.Parameter(typeof(Type), "targetType");
            ParameterExpression vpfnParam = Expression.Parameter(typeof(AFunction), "vpfn");
            ParameterExpression thisParam = context.ObjExpr.ThisParam;

            Expression targetParamAssign = Expression.Assign(targetParam, e.GenDlr(context));
            Expression targetTypeParamAssign =
                Expression.Assign(
                    targetTypeParam,
                    Expression.Call(null, Compiler.Method_Util_classOf, targetParam));

            Expression cachedTypeField = Expression.Field(thisParam, context.ObjExpr.CachedTypeField(_siteIndex));

            Expression setCachedClass =
                Expression.Assign(
                    cachedTypeField,
                    targetTypeParam);

            Expression vpfnParamAssign =
                Expression.Assign(
                    vpfnParam,
                    Expression.Convert(Expression.Call(context.ObjExpr.GenVar(context, v), Compiler.Method_Var_getRawRoot), typeof(AFunction)));

            if (_protocolOn == null)
            {
                return Expression.Block(
                    new ParameterExpression[] { targetParam, targetTypeParam, vpfnParam },
                    targetParamAssign,
                    targetTypeParamAssign,
                    setCachedClass,
                    vpfnParamAssign,
                    GenerateArgsAndCall(context, vpfnParam, targetParam));
            }
            else
            {
                Expression[] args = new Expression[_args.count()-1];
                for (int i = 1; i < _args.count(); i++)
                {
                    Expression bare = ((Expr)_args.nth(i)).GenDlr(context);
                    args[i - 1] = Compiler.MaybeBox(bare);
                }

                return Expression.Block(
                     new ParameterExpression[] { targetParam, targetTypeParam, vpfnParam },
                     targetParamAssign,
                     targetTypeParamAssign,
                     Expression.Condition(
                        Expression.Or(
                            Expression.Equal(targetTypeParam, cachedTypeField),
                            Expression.Not(Expression.TypeIs(targetParam, _protocolOn))),
                        Expression.Block(
                            setCachedClass,
                            vpfnParamAssign,
                            GenerateArgsAndCall(context, vpfnParam, targetParam)),
                         Compiler.MaybeBox(Expression.Call(Expression.Convert(targetParam, _protocolOn), _onMethod, args))));                           

            }


            //Var v = ((VarExpr)_fexpr).Var;
            //Expr e = (Expr)_args.nth(0);

            //ParameterExpression fnParam = Expression.Parameter(typeof(IFn), "fn"); 
            //ParameterExpression targetParam = Expression.Parameter(typeof(Object), "target");
            //ParameterExpression targetTypeParam = Expression.Parameter(typeof(Type), "targetType");
            //ParameterExpression vpfnParam = Expression.Parameter(typeof(AFunction), "vpfn");
            //ParameterExpression implParam = Expression.Parameter(typeof(IFn), "implFn");
            //ParameterExpression thisParam = context.ObjExpr.ThisParam;


            //Expression fnParamAssign = Expression.Assign(fnParam, Expression.Convert(fn, typeof(IFn)));
            //Expression targetParamAssign = Expression.Assign(targetParam, e.GenDlr(context));
            //Expression targetTypeParamAssign =
            //    Expression.Assign(
            //        targetTypeParam,
            //        Expression.Call(null, Compiler.Method_Util_classOf, targetParam));
            //Expression vpfnParamAssign =
            //    Expression.Assign(
            //        vpfnParam,
            //        Expression.Convert(Expression.Call(context.ObjExpr.GenVar(context, v), Compiler.Method_Var_getRawRoot), typeof(AFunction)));

            //Expression cachedTypeField = Expression.Field(thisParam, context.ObjExpr.CachedTypeField(_siteIndex));
            //Expression cachedProtoFnField = Expression.Field(thisParam, context.ObjExpr.CachedProtoFnField(_siteIndex));
            //Expression cachedProtoImplField = Expression.Field(thisParam, context.ObjExpr.CachedProtoImplField(_siteIndex));

            //Expression setCachedClass =
            //    Expression.Assign(
            //        cachedTypeField,
            //        targetTypeParam);

            //Expression setCachedProtoFn =
            //    Expression.Block(
            //        Expression.Assign(cachedProtoFnField, vpfnParam),
            //        Expression.Assign(
            //            implParam,
            //            Expression.Call(
            //                Expression.Property(vpfnParam, Compiler.Method_AFunction_MethodImplCache),
            //                Compiler.Method_MethodImplCache_fnFor,
            //                targetTypeParam)),
            //        Expression.IfThenElse(
            //            Expression.Equal(implParam, Expression.Constant(null)),
            //            Expression.Block(
            //                Expression.Assign(cachedProtoFnField, Expression.Constant(null,typeof(AFunction))),
            //                Expression.Assign(implParam, vpfnParam)),
            //            Expression.Assign(
            //                cachedProtoImplField,
            //                implParam)));

            //Expression standardImplParamAssign = Expression.Assign(implParam, cachedProtoImplField);


            //LabelTarget clear1Label = Expression.Label("clear1");
            //LabelTarget clear2Label = Expression.Label("clear2");
            //LabelTarget callLabel = Expression.Label("call");

            //Expression block1 = Expression.Block(fnParamAssign, targetParamAssign);
            //Expression block2 = Expression.Block(
            //    targetTypeParamAssign,
            //    vpfnParamAssign,
            //    Expression.IfThen(
            //            Expression.NotEqual(targetTypeParam, cachedTypeField),
            //            Expression.Goto(clear1Label)),
            //        Expression.IfThen(
            //            Expression.NotEqual(vpfnParam, cachedProtoFnField),
            //            Expression.Goto(clear2Label)),
            //        standardImplParamAssign,
            //        Expression.Goto(callLabel),
            //        Expression.Label(clear1Label),
            //        setCachedClass,
            //        Expression.Label(clear2Label),
            //        setCachedProtoFn,
            //        Expression.Label(callLabel),
            //        GenerateArgsAndCall(context, fnParam, targetParam));

            //Expression block;

            //if (_protocolOn != null)
            //{
            //    Expression[] args = new Expression[_args.count()-1];
            //    for (int i = 1; i < _args.count(); i++)
            //    {
            //        Expression bare = ((Expr)_args.nth(i)).GenDlr(context);
            //        args[i - 1] = Compiler.MaybeBox(bare);
            //    }

            //    block = Expression.Block(
            //        new ParameterExpression[] { fnParam, targetParam, targetTypeParam, vpfnParam, implParam },
            //        block1,
            //        Expression.Condition(
            //            Expression.TypeIs(targetParam, _protocolOn),
            //            Compiler.MaybeBox(Expression.Call(Expression.Convert(targetParam,_protocolOn), _onMethod,args)),
            //            block2));
            //}
            //else
            //    block = Expression.Block(
            //            new ParameterExpression[] { fnParam, targetParam, targetTypeParam, vpfnParam, implParam },
            //            block1,
            //            block2);

            //return block;
        }

        #endregion
    }
}
