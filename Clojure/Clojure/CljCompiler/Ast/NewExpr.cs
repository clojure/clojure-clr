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
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
using Microsoft.Scripting.Ast; //for Utils in GenDlrForMethod
#endif
using System.Dynamic;
using clojure.lang.Runtime.Binding;
using clojure.lang.Runtime;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    class NewExpr : Expr
    {
        #region Data

        readonly List<HostArg> _args;
        readonly ConstructorInfo _ctor;
        readonly Type _type;
        bool _isNoArgValueTypeCtor = false;
        readonly IPersistentMap _spanMap;

        #endregion

        #region Ctors

        public NewExpr(Type type, List<HostArg> args, IPersistentMap spanMap)
        {
            _args = args;
            _type = type;
            _spanMap = spanMap;
            _ctor = ComputeCtor();
        }

        private ConstructorInfo ComputeCtor()
        {
            if (Compiler.CompileStubClassVar.isBound && _type == (Type)Compiler.CompileStubClassVar.deref())
                return null;

            int numArgs = _args.Count;

            int numCtors;
            ConstructorInfo ctor = Reflector.GetMatchingConstructor(_spanMap, _type, _args, out numCtors);

            if (numCtors == 0)
            {
                if (_type.IsValueType && numArgs == 0)
                {
                    // Value types have a default no-arg c-tor that is not picked up in the regular c-tors.
                    _isNoArgValueTypeCtor = true;
                    return null;
                }
                throw new InvalidOperationException(string.Format("No constructor in type: {0} with {1} arguments", _type.Name, numArgs));
            }

            if (ctor == null && RT.booleanCast(RT.WarnOnReflectionVar.deref()))
                RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1} - call to {2} ctor can't be resolved.",
                    Compiler.SourcePathVar.deref(), _spanMap != null ? (int)_spanMap.valAt(RT.StartLineKey, 0) : 0, _type.FullName);

            return ctor;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return _type; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                //int line = (int)Compiler.LINE.deref();

                ISeq form = (ISeq)frm;

                // form => (new Typename args ... )

                if (form.count() < 2)
                    throw new ParseException("wrong number of arguments, expecting: (new Typename args ...)");

                Type t = HostExpr.MaybeType(RT.second(form), false);
                if (t == null)
                    throw new ParseException("Unable to resolve classname: " + RT.second(form));

                List<HostArg> args = HostExpr.ParseArgs(pcon, RT.next(RT.next(form)));

                return new NewExpr(t, args, (IPersistentMap)Compiler.SourceSpanVar.deref());
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            Object[] argvals = new Object[_args.Count];
            for (int i = 0; i < _args.Count; i++)
                argvals[i] = _args[i].ArgExpr.Eval();
            if ( _ctor != null )
                return _ctor.Invoke(Reflector.BoxArgs(_ctor.GetParameters(),argvals));  // TODO: Deal with ByRef parameters
            return Reflector.InvokeConstructor(_type,argvals);
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression call;

            if (_ctor != null)
                call = GenDlrForMethod(rhc, objx, context);
            else if (_isNoArgValueTypeCtor)
                call = Expression.Default(_type);
            else
                call = GenerateComplexCall(rhc, objx, context);

            call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);
            return call;
        }

        // TODO: See if it is worth removing the code duplication with MethodExp.GenDlr.

        private Expression GenerateComplexCall(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression call;

            Expression target = GenTargetExpression(objx,context);

            List<Expression> exprs = new List<Expression>(_args.Count);
            List<ParameterExpression> sbParams = new List<ParameterExpression>();
            List<Expression> sbInits = new List<Expression>();
            List<Expression> sbTransfers = new List<Expression>();
            MethodExpr.GenerateComplexArgList(objx, context, _args, out exprs, out sbParams, out sbInits, out sbTransfers);

            Expression[] argExprs = ClrExtensions.ArrayInsert<Expression>(target, exprs);


            Type returnType = this.ClrType;
            Type stubType = Compiler.CompileStubOrigClassVar.isBound ? (Type)Compiler.CompileStubOrigClassVar.deref() : null;

            if (returnType == stubType)
                returnType = objx.BaseType;

            // TODO: get rid of Default
            CreateInstanceBinder binder = new ClojureCreateInstanceBinder(ClojureContext.Default,_args.Count);
            DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), argExprs);
            // I'd like to use returnType in place of typeof(object) in the previous, 
            // But I can't override ReturnType in DefaultCreateInstanceBinder and this causes an error.
            // Look for the conversion below.

            //if (context.Mode == CompilerMode.File)
            if (context.DynInitHelper != null)
                call = context.DynInitHelper.ReduceDyn(dyn);
            else
                call = dyn;

            call = Expression.Convert(call, returnType);

            if (sbParams.Count > 0)
            {

                // We have ref/out params.  Construct the complicated call;

                ParameterExpression callValParam = Expression.Parameter(returnType, "__callVal");
                ParameterExpression[] allParams = ClrExtensions.ArrayInsert<ParameterExpression>(callValParam, sbParams);

                call = Expression.Block(
                    returnType,
                    allParams,
                    Expression.Block(sbInits),
                    Expression.Assign(callValParam, call),
                    Expression.Block(sbTransfers),
                    callValParam);
            }

            return call;    
        }

        private Expression GenTargetExpression(ObjExpr objx, GenContext context)
        {
            if (Compiler.CompileStubOrigClassVar.isBound && Compiler.CompileStubOrigClassVar.deref() != null && objx.TypeBlder != null)
                return Expression.Constant(objx.TypeBlder, typeof(Type));

            if (_type != null)
                return Expression.Constant(_type, typeof(Type));

            throw new ArgumentException("Cannot generate type for NewExpr. Serious!");

            //string name = Compiler.DestubClassName(_type.FullName);
            //return Expression.Call(null, Compiler.Method_RT_classForName, Expression.Constant(name));
        }

        Expression GenDlrForMethod(RHC rhc, ObjExpr objx, GenContext context)
        {
            // The ctor is uniquely determined.

            Expression[] args = HostExpr.GenTypedArgs(objx, context, _ctor.GetParameters(), _args);
            return Utils.SimpleNewHelper(_ctor, args);

            // JAVA: emitClearLocals
        }

        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            if (_ctor != null)
                EmitForMethod(rhc, objx, context);
            else if (_isNoArgValueTypeCtor)
                EmitForNoArgValueTypeCtor(rhc, objx, context);
            else
                EmitComplexCall(rhc, objx, context);

            if (rhc == RHC.Statement)
                context.GetILGenerator().Emit(OpCodes.Pop);
        }

        private void EmitForMethod(RHC rhc, ObjExpr objx, GenContext context)
        {
            EmitParamsForMethod(objx,context);
            context.GetILGenerator().Emit(OpCodes.Newobj,_ctor);
        }

        private void EmitParamsForMethod(ObjExpr objx, GenContext context)
        {
            ParameterInfo[] pis = _ctor.GetParameters();

            for( int i =0; i< pis.Length; i++ ) 
            {
                HostArg arg = _args[i];
                ParameterInfo pi = pis[i];
                Type argType = arg.ArgExpr.HasClrType ? arg.ArgExpr.ClrType : typeof(object);
                Type paramType = pi.ParameterType;

                arg.ArgExpr.Emit(RHC.Expression,objx,context);
            
                if (!CompatibleParameterTypes(paramType,argType)) 
                {
                    if ( paramType != argType )
                        if ( paramType.IsByRef)
                            paramType = paramType.GetElementType();
                    if ( paramType != argType )
                        context.GetILGenerator().Emit(OpCodes.Castclass,paramType);
                }
            }
        }

        // Straight from the  DLR code.
        private static bool CompatibleParameterTypes(Type parameter, Type argument)
        {
            if (parameter == argument ||
                (!parameter.IsValueType && !argument.IsValueType && parameter.IsAssignableFrom(argument)))
                return true;

            if (parameter.IsByRef && parameter.GetElementType() == argument)
                return true;

            return false;
        }
        

        private void EmitForNoArgValueTypeCtor(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGenerator ilg = context.GetILGenerator();
            LocalBuilder loc = ilg.DeclareLocal(_type);
            ilg.Emit(OpCodes.Ldloca, loc);
            ilg.Emit(OpCodes.Initobj, _type);
            ilg.Emit(OpCodes.Box, _type);
        }

        // TODO: See if it is worth removing the code duplication with MethodExp.GenDlr.


        private void EmitComplexCall(RHC rhc, ObjExpr objx, GenContext context)
        {
            List<ParameterExpression> paramExprs = new List<ParameterExpression>(_args.Count+1);
            paramExprs.Add(Expression.Parameter(_type));
            EmitTargetExpression(objx,context);

            foreach ( HostArg ha in _args )
            {
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType ? (e.ClrType ?? typeof(Object)) : typeof(Object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        paramExprs.Add(Expression.Parameter(argType.MakeByRefType(),ha.LocalBinding.Name));
                        context.GetILGenerator().Emit(OpCodes.Ldloca,ha.LocalBinding.LocalVar);
                        break;

                    case HostArg.ParameterType.Standard:
                        paramExprs.Add(Expression.Parameter(argType,ha.LocalBinding.Name));
                        ha.ArgExpr.Emit(RHC.Expression,objx,context);
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
               
                Type returnType = HasClrType ? ClrType : typeof(object);
                CreateInstanceBinder binder = new ClojureCreateInstanceBinder(ClojureContext.Default,_args.Count);
                DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramExprs);
                Expression call = dyn;
                if ( context.DynInitHelper != null )
                    call = context.DynInitHelper.ReduceDyn(dyn);
                
                if ( returnType == typeof(void) )
                {
                    call = Expression.Block(call,Expression.Default(typeof(object)));
                    returnType = typeof(object);
                }
                call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);

                Type[] paramTypes = paramExprs.Map((x) => x.Type);
                MethodBuilder mbLambda = context.TB.DefineMethod("__interop_ctor_"+RT.nextID(),MethodAttributes.Static|MethodAttributes.Public,CallingConventions.Standard,returnType,paramTypes);
                LambdaExpression lambda = Expression.Lambda(call,paramExprs);
                lambda.CompileToMethod(mbLambda);

                context.GetILGenerator().Emit(OpCodes.Call,mbLambda);
            }       
        }

        static readonly MethodInfo Method_Type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

        private void EmitTargetExpression(ObjExpr objx, GenContext context)
        {
            ILGenerator ilg = context.GetILGenerator();

            if (Compiler.CompileStubOrigClassVar.isBound && Compiler.CompileStubOrigClassVar.deref() != null && objx.TypeBlder != null)
                ilg.Emit(OpCodes.Ldtoken, objx.TypeBlder);
            else if (_type != null)
                ilg.Emit(OpCodes.Ldtoken, typeof(Object));
            else
                throw new ArgumentException("Cannot generate type for NewExpr. Serious!");

            ilg.Emit(OpCodes.Call,Compiler.Method_Type_GetTypeFromHandle);
        }

        #endregion
    }
}
