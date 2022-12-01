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
using System.Dynamic;
using System.Linq.Expressions;
using clojure.lang.Runtime.Binding;
using clojure.lang.Runtime;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace clojure.lang.CljCompiler.Ast
{
    public class InstanceZeroArityCallExpr : HostExpr
    {
        #region Data

        readonly Expr _target;
        public Expr Target { get { return _target; } }

        readonly string _memberName;
        public string MemberName { get { return _memberName; } }

        protected readonly string _source;
        public string Source { get { return _source; } }

        protected readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }

        protected readonly Symbol _tag;
        public Symbol Tag { get { return _tag; } }

        #endregion

        #region Ctors

        internal InstanceZeroArityCallExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string memberName)
        {
            _source = source;
            _spanMap = spanMap;
            _memberName = memberName;
            _target = target;
            _tag = tag;

            if ( RT.booleanCast(RT.WarnOnReflectionVar.deref()))
                if (target.HasClrType)
                {
                    RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1}:{2} - reference to field/property {3} on {4} can't be resolved.",
                        Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), memberName, target.ClrType.FullName);
                    RT.errPrintWriter().Flush();
                }
                else
                {
                    RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1}:{2} - reference to field/property {3} can't be resolved (target class is unknown).",
                       Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), memberName);
                    RT.errPrintWriter().Flush();
                }
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

        #region eval

        // TODO: Handle by-ref
        public override object Eval()
        {
            object target = _target.Eval();
            return Reflector.CallInstanceMethod(_memberName, null, target, new object[0]);
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            EmitUnboxed(rhc, objx, ilg);
            HostExpr.EmitBoxReturn(objx, ilg, typeof(Object));

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public override bool CanEmitPrimitive
        {
            get { return HasClrType && Util.IsPrimitive(ClrType); }
        }

        public override void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            // See MethodExpr.EmitComplexCall to see why this is so complicated

            //  Build the parameter list

            List<ParameterExpression> paramExprs = new List<ParameterExpression>();
            List<Type> paramTypes = new List<Type>();

            Type paramType = _target.HasClrType && _target.ClrType != null && _target.ClrType.IsPrimitive ? _target.ClrType : typeof(object);
            ParameterExpression param = Expression.Parameter(paramType);
            paramExprs.Add(param);
            paramTypes.Add(paramType);


            // Build dynamic call and lambda
            Type returnType = HasClrType ? ClrType : typeof(object);

            GetMemberBinder binder = new ClojureGetZeroArityMemberBinder(ClojureContext.Default, _memberName, false);
            DynamicExpression dyn = Expression.Dynamic(binder, returnType, paramExprs);


            MethodExpr.EmitDynamicCallPreamble(dyn, _spanMap, "__interop_" + _memberName + RT.nextID(), returnType, paramExprs, paramTypes.ToArray(), ilg, out LambdaExpression lambda, out Type delType, out MethodBuilder mbLambda);

            //  Emit target + args (no args, actually)

            _target.Emit(RHC.Expression, objx, ilg);

            MethodExpr.EmitDynamicCallPostlude(lambda, delType, mbLambda, ilg);
 
        }

        #endregion
    }
}
