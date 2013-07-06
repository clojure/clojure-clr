﻿/**
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
                RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1}:{2} - call to {3} ctor can't be resolved.",
                    Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), _type.FullName);

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

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        { 
            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (_ctor != null)
                EmitForMethod(rhc, objx, ilg);
            else if (_isNoArgValueTypeCtor)
                EmitForNoArgValueTypeCtor(rhc, objx, ilg);
            else
            {
                EmitComplexCall(rhc, objx, ilg);
                if (_type.IsValueType)
                    HostExpr.EmitBoxReturn(objx, ilg, _type);
            }

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        private void EmitForMethod(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            EmitParamsForMethod(objx,ilg);
            ilg.Emit(OpCodes.Newobj,_ctor);
            if ( _type.IsValueType )
                ilg.Emit(OpCodes.Box, _type);
        }

        private void EmitParamsForMethod(ObjExpr objx, CljILGen ilg)
        {
            ParameterInfo[] pis = _ctor.GetParameters();
            MethodExpr.EmitTypedArgs(objx, ilg, pis, _args);
        }

       private void EmitForNoArgValueTypeCtor(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            LocalBuilder loc = ilg.DeclareLocal(_type);
            ilg.Emit(OpCodes.Ldloca, loc);
            ilg.Emit(OpCodes.Initobj, _type);
            ilg.Emit(OpCodes.Ldloc, loc);
            ilg.Emit(OpCodes.Box, _type);
        }

        private void EmitComplexCall(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            // See the notes on MethodExpr.EmitComplexCall on why this is so complicated

            List<ParameterExpression> paramExprs = new List<ParameterExpression>(_args.Count + 1);
            List<Type> paramTypes = new List<Type>(_args.Count + 1);

            paramExprs.Add(Expression.Parameter(typeof(Type)));
            paramTypes.Add(typeof(Type));

            int i = 0;
            foreach (HostArg ha in _args)
            {
                i++;
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType && e.ClrType != null && e.ClrType.IsPrimitive ? e.ClrType : typeof(object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        {
                            Type byRefType = argType.MakeByRefType();
                            paramExprs.Add(Expression.Parameter(byRefType, ha.LocalBinding.Name));
                            paramTypes.Add(byRefType);
                            break;
                        }

                    case HostArg.ParameterType.Standard:
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr)
                        {
                            paramExprs.Add(Expression.Parameter(argType, ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            paramTypes.Add(argType);
                        }
                        else
                        {
                            paramExprs.Add(Expression.Parameter(typeof(object), ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            paramTypes.Add(typeof(object));
                        }
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }

            // Build dynamic call and lambda
            Type returnType = HasClrType ? ClrType : typeof(object);
            CreateInstanceBinder binder = new ClojureCreateInstanceBinder(ClojureContext.Default, _args.Count);

#if CLR2
            // Not covariant. Sigh.
            List<Expression> paramsAsExprs = new List<Expression>(paramExprs.Count);
            paramsAsExprs.AddRange(paramExprs.ToArray());
            DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramsAsExprs);
#else
           DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramExprs);

#endif

            LambdaExpression lambda;
            Type delType;
            MethodBuilder mbLambda;

            MethodExpr.EmitDynamicCallPreamble(dyn, _spanMap, "__interop_ctor_" + RT.nextID(), returnType, paramExprs, paramTypes.ToArray(), ilg, out lambda, out delType, out mbLambda);

            //  Emit target + args
  
            EmitTargetExpression(objx, ilg);

            i = 0;
            foreach (HostArg ha in _args)
            {
                i++;
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType && e.ClrType != null && e.ClrType.IsPrimitive ? e.ClrType : typeof(object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        MethodExpr.EmitByRefArg(ha, objx, ilg);
                        break;

                    case HostArg.ParameterType.Standard:
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr)
                        {
                            ((MaybePrimitiveExpr)ha.ArgExpr).EmitUnboxed(RHC.Expression, objx, ilg);
                        }
                        else
                        {
                            ha.ArgExpr.Emit(RHC.Expression, objx, ilg);
                        }
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }
            
            MethodExpr.EmitDynamicCallPostlude(lambda, delType, mbLambda, ilg); 
        }

        private void EmitTargetExpression(ObjExpr objx, CljILGen ilg)
        {
            if (Compiler.CompileStubOrigClassVar.isBound && Compiler.CompileStubOrigClassVar.deref() != null && objx.TypeBlder != null)
                ilg.Emit(OpCodes.Ldtoken, objx.TypeBlder);
            else if (_type != null)
                ilg.Emit(OpCodes.Ldtoken, _type);
            else
                throw new ArgumentException("Cannot generate type for NewExpr. Serious!");

            ilg.Emit(OpCodes.Call,Compiler.Method_Type_GetTypeFromHandle);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
