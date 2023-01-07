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
using System.Linq.Expressions;
using System.Dynamic;
using clojure.lang.Runtime.Binding;
using clojure.lang.Runtime;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class NewExpr : Expr
    {
        #region Data

        readonly IList<HostArg> _args;
        public IList<HostArg> Args { get { return _args; } }
        
        readonly ConstructorInfo _ctor;
        public ConstructorInfo Ctor { get { return _ctor; } }
        
        readonly Type _type;
        public Type Type { get { return _type; } }
        
        bool _isNoArgValueTypeCtor = false;
        public bool IsNoArgValueTypeCtor { get { return _isNoArgValueTypeCtor; } }
        
        readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }

        #endregion

        #region Ctors

        public NewExpr(Type type, IList<HostArg> args, IPersistentMap spanMap)
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

            ConstructorInfo ctor = Reflector.GetMatchingConstructor(_spanMap, _type, _args, out int numCtors);

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
            {
                RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1}:{2} - call to {3} ctor can't be resolved.",
                    Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), _type.FullName);
                RT.errPrintWriter().Flush();
            }

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        private void EmitForNoArgValueTypeCtor(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            LocalBuilder loc = ilg.DeclareLocal(_type);
            ilg.Emit(OpCodes.Ldloca, loc);
            ilg.Emit(OpCodes.Initobj, _type);
            ilg.Emit(OpCodes.Ldloc, loc);
            ilg.Emit(OpCodes.Box, _type);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
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
            DynamicExpression dyn = Expression.Dynamic(binder, typeof(object), paramExprs);

            MethodExpr.EmitDynamicCallPreamble(dyn, _spanMap, "__interop_ctor_" + RT.nextID(), returnType, paramExprs, paramTypes.ToArray(), ilg, out LambdaExpression lambda, out Type delType, out MethodBuilder mbLambda);

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
                        if (argType.IsPrimitive && ha.ArgExpr is MaybePrimitiveExpr expr)
                        {
                            expr.EmitUnboxed(RHC.Expression, objx, ilg);
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
            if (Compiler.CompileStubOrigClassVar.isBound && Compiler.CompileStubOrigClassVar.deref() != null && objx.TypeBuilder != null)
                ilg.Emit(OpCodes.Ldtoken, objx.TypeBuilder);
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
