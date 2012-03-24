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

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        { 
            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (_ctor != null)
                EmitForMethod(rhc, objx, ilg);
            else if (_isNoArgValueTypeCtor)
                EmitForNoArgValueTypeCtor(rhc, objx, ilg);
            else
                EmitComplexCall(rhc, objx, ilg);

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

            for( int i =0; i< pis.Length; i++ ) 
            {
                // TODO: if hostarg is by ref, we should deal with this?
                HostArg arg = _args[i];
                ParameterInfo pi = pis[i];
                Type argType = arg.ArgExpr.HasClrType ? arg.ArgExpr.ClrType : typeof(object);
                Type paramType = pi.ParameterType;

                //arg.ArgExpr.Emit(RHC.Expression,objx,context);
                MethodExpr.EmitTypedArg(objx, ilg, paramType, arg.ArgExpr);

                //if (!CompatibleParameterTypes(paramType,argType)) 
                //{
                //    if ( paramType != argType )
                //        if ( paramType.IsByRef)
                //            paramType = paramType.GetElementType();
                //    if ( paramType != argType )
                //        context.GetILGenerator().Emit(OpCodes.Castclass,paramType);
                //}
            }
        }

        // Straight from the  DLR code.
        private static bool CompatibleParameterTypes(Type parameter, Type argument)
        {
            if (parameter == argument ||
                argument == null ||
                (!parameter.IsValueType && !argument.IsValueType && parameter.IsAssignableFrom(argument)))
                return true;

            if (parameter.IsByRef && parameter.GetElementType() == argument)
                return true;

            return false;
        }


        private void EmitForNoArgValueTypeCtor(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            LocalBuilder loc = ilg.DeclareLocal(_type);
            ilg.Emit(OpCodes.Ldloca, loc);
            ilg.Emit(OpCodes.Initobj, _type);
            ilg.Emit(OpCodes.Box, _type);
        }

        // TODO: See if it is worth removing the code duplication with MethodExp.GenDlr.


        private void EmitComplexCall(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            List<ParameterExpression> paramExprs = new List<ParameterExpression>(_args.Count + 1);
            paramExprs.Add(Expression.Parameter(typeof(Type)));
            EmitTargetExpression(objx, ilg);

            int i = 0;
            foreach (HostArg ha in _args)
            {
                i++;
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType && e.ClrType != null && e.ClrType.IsPrimitive ? e.ClrType : typeof(object);

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
                        paramExprs.Add(Expression.Parameter(argType.MakeByRefType(), ha.LocalBinding.Name));
                        ilg.Emit(OpCodes.Ldloca, ha.LocalBinding.LocalVar);
                        break;

                    case HostArg.ParameterType.Standard:
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr)
                        {
                            paramExprs.Add(Expression.Parameter(argType, ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            ((MaybePrimitiveExpr)ha.ArgExpr).EmitUnboxed(RHC.Expression, objx, ilg);
                        }
                        else
                        {
                            paramExprs.Add(Expression.Parameter(typeof(object), ha.LocalBinding != null ? ha.LocalBinding.Name : "__temp_" + i));
                            ha.ArgExpr.Emit(RHC.Expression, objx, ilg);
                        }
                        break;

                    default:
                        throw Util.UnreachableCode();
                }
            }

            Type returnType = HasClrType ? ClrType : typeof(object);
            CreateInstanceBinder binder = new ClojureCreateInstanceBinder(ClojureContext.Default, _args.Count);
            DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramExprs);
            Expression call = dyn;

            GenContext context = Compiler.CompilerContextVar.deref() as GenContext;
            if (context.DynInitHelper != null)
                call = context.DynInitHelper.ReduceDyn(dyn);

            if (returnType == typeof(void))
            {
                call = Expression.Block(call, Expression.Default(typeof(object)));
                returnType = typeof(object);
            }
            call = GenContext.AddDebugInfo(call, _spanMap);

            Type[] paramTypes = paramExprs.Map((x) => x.Type);
            MethodBuilder mbLambda = context.TB.DefineMethod("__interop_ctor_" + RT.nextID(), MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, returnType, paramTypes);
            LambdaExpression lambda = Expression.Lambda(call, paramExprs);
            lambda.CompileToMethod(mbLambda);

            ilg.Emit(OpCodes.Call, mbLambda);

        }

        static readonly MethodInfo Method_Type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

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
