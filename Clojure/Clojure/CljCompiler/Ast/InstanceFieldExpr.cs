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
using System.Reflection;

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Generation;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    abstract class InstanceFieldOrPropertyExpr<TInfo> : FieldOrPropertyExpr
    {
        #region Data

        protected readonly Expr _target;
        protected readonly Type _targetType;
        protected readonly TInfo _tinfo;
        protected readonly string _fieldName;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        protected readonly string _source;
        
        protected readonly IPersistentMap _spanMap;
        protected readonly Symbol _tag;

        #endregion

        #region Ctors

        public InstanceFieldOrPropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string fieldName, TInfo tinfo)
        {
            _source = source;
            _spanMap = spanMap;
            _target = target;
            _fieldName = fieldName;
            _tinfo = tinfo;
            _tag = tag;

            _targetType = target.HasClrType ? target.ClrType : null;

            // Java version does not include check on _targetType
            // However, this seems consistent with the checks in the generation code.
            if ( (_targetType == null || _tinfo == null) && RT.booleanCast(RT.WarnOnReflectionVar.deref()))
                RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1} - reference to field/property {2} can't be resolved.", 
                    Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(_spanMap),_fieldName);
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tinfo != null || _tag != null; }
        }

        #endregion

        #region Code generation

        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            Type targetType = _targetType;

            Type stubType = Compiler.CompileStubOrigClassVar.isBound ? (Type)Compiler.CompileStubOrigClassVar.deref() : null;

            if ( _targetType == stubType )
                targetType = objx.BaseType;

            Expression target = _target.GenCode(RHC.Expression, objx, context);
            Expression call;
            if (targetType != null && _tinfo != null)
            {
                Expression convTarget = Expression.Convert(target, targetType);
                Expression access = GenAccess(rhc, objx, convTarget);
                call = HostExpr.GenBoxReturn(access,FieldType,objx,context);
            }
            else
            {
                // TODO: Convert to Dynamic call site
                call = Expression.Call(Compiler.Method_Reflector_GetInstanceFieldOrProperty, target, Expression.Constant(_fieldName));
            }
            call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);
            return call;
        }

        protected abstract Expression GenAccess(RHC rhc, ObjExpr objx, Expression target);

        public override Expression GenCodeUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            Expression target = _target.GenCode(RHC.Expression, objx, context);
            if (_targetType != null && _tinfo != null)
            {
                Expression convTarget = Expression.Convert(target, _targetType);
                Expression access = GenAccess(rhc,objx, convTarget);
                access = Compiler.MaybeAddDebugInfo(access, _spanMap, context.IsDebuggable);
                return access;
            }
            else
                throw new InvalidOperationException("Unboxed emit of unknown member.");
        }

        public override void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGen ilg = context.GetILGen();

            Type targetType = _targetType;

            //Type stubType = Compiler.CompileStubOrigClassVar.isBound ? (Type)Compiler.CompileStubOrigClassVar.deref() : null;

            //if (_targetType == stubType)
            //    targetType = objx.BaseType;

            Compiler.MaybeEmitDebugInfo(context, ilg, _spanMap);

            if (targetType != null && _tinfo != null)
            {
                _target.Emit(RHC.Expression, objx, context);
                // TODO: Transfer this mechanism for calls on value types to MethodExpr, InstanceZeroArityCallExpr, StaticFieldExpr
                if (FieldDeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Unbox_Any, FieldDeclaringType);
                    LocalBuilder vtTemp = ilg.DeclareLocal(FieldDeclaringType);
                    vtTemp.SetLocalSymInfo("valueTemp");
                    ilg.Emit(OpCodes.Stloc, vtTemp);
                    ilg.Emit(OpCodes.Ldloca, vtTemp);
                }
                else
                    ilg.Emit(OpCodes.Castclass, FieldDeclaringType);
                EmitGet(ilg);
                HostExpr.EmitBoxReturn(objx, context, FieldType);
            }
            else
            {
                // TODO: convert to dynamic?
                _target.Emit(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Ldstr, _fieldName);
                ilg.Emit(OpCodes.Call, Compiler.Method_Reflector_GetInstanceFieldOrProperty);
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }


        public override void EmitUnboxed(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGen ilg = context.GetILGen();

            Type targetType = _targetType;

            //Type stubType = Compiler.CompileStubOrigClassVar.isBound ? (Type)Compiler.CompileStubOrigClassVar.deref() : null;

            //if (_targetType == stubType)
            //    targetType = objx.BaseType;

            Compiler.MaybeEmitDebugInfo(context, ilg, _spanMap);

            if (targetType != null && _tinfo != null)
            {
                _target.Emit(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Isinst, targetType);
                EmitGet(ilg);
            }
            else
            {
                throw new InvalidOperationException("Unboxed emit of unknown member.");
            }
        }

        protected abstract void EmitGet(ILGen ilg);
        protected abstract void EmitSet(ILGen ilg);
        protected abstract Type FieldDeclaringType { get; }

        #endregion

        #region AssignableExpr Members

        public override Expression GenAssign(RHC rhc, ObjExpr objx, GenContext context, Expr val)
        {
            Expression target = _target.GenCode(RHC.Expression, objx, context);
            Expression valExpr = val.GenCode(RHC.Expression, objx, context);
            Expression call;
            if (_targetType != null && _tinfo != null)
            {
                Expression convTarget = Expression.Convert(target, _targetType);
                Expression access = GenAccess(rhc, objx, convTarget);
                Expression unboxValExpr = HostExpr.GenUnboxArg(valExpr, FieldType);
                //call = Expression.Assign(access, Expression.Convert(valExpr, access.Type));
                call = Expression.Assign(access, unboxValExpr);  
            }
            else
            {
                // TODO: Convert to a dynamic call site
                call = Expression.Call(
                    Compiler.Method_Reflector_SetInstanceFieldOrProperty,
                    target,
                    Expression.Constant(_fieldName),
                    Compiler.MaybeBox(valExpr));
            }

            call = Compiler.MaybeAddDebugInfo(call, _spanMap, context.IsDebuggable);
            return call;
        }

        public override void EmitAssign(RHC rhc, ObjExpr objx, GenContext context, Expr val)
        {
            ILGen ilg = context.GetILGen();

            Compiler.MaybeEmitDebugInfo(context, ilg, _spanMap);

            if (_targetType != null && _tinfo != null)
            {
                _target.Emit(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Castclass, _targetType);
                val.Emit(RHC.Expression, objx, context);
                LocalBuilder tmp = ilg.DeclareLocal(typeof(object));
                tmp.SetLocalSymInfo("valTemp");
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Stloc, tmp);
                if (FieldType.IsPrimitive)
                    HostExpr.EmitUnboxArg(objx, context, FieldType);
                else
                    ilg.Emit(OpCodes.Castclass, FieldType);
                EmitSet(ilg);
                ilg.Emit(OpCodes.Ldloc, tmp);
            }
            else
            {
                _target.Emit(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Ldstr, _fieldName);
                val.Emit(RHC.Expression, objx, context);
                ilg.Emit(OpCodes.Call, Compiler.Method_Reflector_SetInstanceFieldOrProperty); 
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);

        }

        #endregion
    }

    sealed class InstanceFieldExpr : InstanceFieldOrPropertyExpr<FieldInfo>
    {
        #region C-tors

        public InstanceFieldExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string fieldName, FieldInfo finfo)
            :base(source,spanMap,tag,target,fieldName,finfo)  
        {
        }

        #endregion

        #region Type mangling

        public override Type ClrType
        {
            get { return _tag != null ? HostExpr.TagToType(_tag) : _tinfo.FieldType; }
        }

        #endregion

        #region eval

        // TODO: Handle by-ref
        public override object Eval()
        {
            return _tinfo.GetValue(_target.Eval());
        }

        #endregion

        #region Code generation

        protected override Expression GenAccess(RHC rhc, ObjExpr objx, Expression target)
        {
            return Expression.Field(target, _tinfo);
        }

        public override bool CanEmitPrimitive
        {
            get { return _targetType != null && _tinfo != null && Util.IsPrimitive(_tinfo.FieldType); }
        }

        protected override Type FieldType
        {
            get { return _tinfo.FieldType; }
        }

        protected override Type FieldDeclaringType
        {
            get { return  _tinfo.DeclaringType; }
        }

        protected override void EmitGet(ILGen ilg)
        {
            ilg.EmitFieldGet(_tinfo);
        }

        protected override void EmitSet(ILGen ilg)
        {
            ilg.EmitFieldSet(_tinfo);
        }

        #endregion

        #region AssignableExpr members

        public override object EvalAssign(Expr val)
        {
            object target = _target.Eval();
            object e = val.Eval();
            _tinfo.SetValue(target, e);
            return e;
        }
        
        #endregion
    }

    sealed class InstancePropertyExpr : InstanceFieldOrPropertyExpr<PropertyInfo>
    {
        #region C-tors

        public InstancePropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string fieldName, PropertyInfo pinfo)
            :base(source,spanMap,tag, target,fieldName,pinfo)  
        {
        }

        #endregion

        #region Type mangling

        public override Type ClrType
        {
            get { return _tag != null ? HostExpr.TagToType(_tag) : _tinfo.PropertyType; }
        }

        #endregion

        #region eval

        // TODO: Handle by-ref
        public override object Eval()
        {
            return _tinfo.GetValue(_target.Eval(), new object[0]);
        }

        #endregion

        #region Code generation

        protected override Expression GenAccess(RHC rhc, ObjExpr objx, Expression target)
        {
            return Expression.Property(target, _tinfo);
        }

        public override bool CanEmitPrimitive
        {
            get { return _targetType != null && _tinfo != null && Util.IsPrimitive(_tinfo.PropertyType); }
        }

        protected override Type FieldType
        {
            get { return _tinfo.PropertyType; }
        }

        protected override void EmitGet(ILGen ilg)
        {
            ilg.EmitPropertyGet(_tinfo);
        }

        protected override void EmitSet(ILGen ilg)
        {
            ilg.EmitPropertySet(_tinfo);
        }

        protected override Type FieldDeclaringType
        {
            get { return _tinfo.DeclaringType; }
        }

        #endregion

        #region AssignableExpr members

        public override object EvalAssign(Expr val)
        {
            object target = _target.Eval();
            object e = val.Eval();
            _tinfo.SetValue(target, e,new object[0]);
            return e;
        }

        #endregion
    }
}
