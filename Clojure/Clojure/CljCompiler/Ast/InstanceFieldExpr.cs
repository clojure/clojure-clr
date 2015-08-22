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
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public abstract class InstanceFieldOrPropertyExpr<TInfo> : FieldOrPropertyExpr
    {
        #region Data

        protected readonly Expr _target;
        public Expr Target { get { return _target; } }

        protected readonly Type _targetType;
        public Type TargetType { get { return _targetType; } }

        protected readonly TInfo _tinfo;
        public TInfo MemberInfo { get { return _tinfo; } }

        protected readonly string _memberName;
        public string MemberName { get { return _memberName; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        protected readonly string _source;
        public string Source { get { return _source; } }
        
        protected readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }

        protected readonly Symbol _tag;
        public Symbol Tag { get { return _tag; } }

        #endregion

        #region Ctors

        protected InstanceFieldOrPropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string memberName, TInfo tinfo)
        {
            _source = source;
            _spanMap = spanMap;
            _target = target;
            _memberName = memberName;
            _tinfo = tinfo;
            _tag = tag;

            _targetType = target.HasClrType ? target.ClrType : null;

            // Java version does not include check on _targetType
            // However, this seems consistent with the checks in the generation code.
            if ((_targetType == null || _tinfo == null) && RT.booleanCast(RT.WarnOnReflectionVar.deref()))
            {
                if (_targetType == null)
                {
                    RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1}:{2} - reference to field/property {3} can't be resolved.",
                        Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), _memberName);
                }
                else
                {
                    RT.errPrintWriter().WriteLine("Reflection warning, {0}:{1}:{2} - reference to field/property {3} on {4} can't be resolved.",
                       Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), _memberName, _targetType.FullName);
                }
            }
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tinfo != null || _tag != null; }
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Type targetType = _targetType;

            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (targetType != null && _tinfo != null)
            {
                _target.Emit(RHC.Expression, objx, ilg);
                MethodExpr.EmitPrepForCall(ilg, typeof(object), FieldDeclaringType);
                EmitGet(ilg);
                HostExpr.EmitBoxReturn(objx, ilg, FieldType);
            }
            else
            {
                // We could convert this to a dynamic call-site
                _target.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Ldstr, _memberName);
                ilg.Emit(OpCodes.Call, Compiler.Method_Reflector_GetInstanceFieldOrProperty);
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }


        public override void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Type targetType = _targetType;

            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (targetType != null && _tinfo != null)
            {
                _target.Emit(RHC.Expression, objx, ilg);
                MethodExpr.EmitPrepForCall(ilg, typeof(object), FieldDeclaringType);
                EmitGet(ilg);
            }
            else
            {
                throw new InvalidOperationException("Unboxed emit of unknown member.");
            }
        }

        protected abstract void EmitGet(CljILGen ilg);
        protected abstract void EmitSet(CljILGen ilg);
        protected abstract Type FieldDeclaringType { get; }

        #endregion

        #region AssignableExpr Members

        public override void EmitAssign(RHC rhc, ObjExpr objx, CljILGen ilg, Expr val)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (_targetType != null && _tinfo != null)
            {
                _target.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Castclass, _targetType);
                val.Emit(RHC.Expression, objx, ilg);
                LocalBuilder tmp = ilg.DeclareLocal(typeof(object));
                GenContext.SetLocalName(tmp, "valTemp");
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Stloc, tmp);
                if (FieldType.IsValueType)
                    HostExpr.EmitUnboxArg(objx, ilg, FieldType);
                else
                    ilg.Emit(OpCodes.Castclass, FieldType);
                EmitSet(ilg);
                ilg.Emit(OpCodes.Ldloc, tmp);
            }
            else
            {
                _target.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Ldstr, _memberName);
                val.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Call, Compiler.Method_Reflector_SetInstanceFieldOrProperty); 
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);

        }

        #endregion
    }

    public sealed class InstanceFieldExpr : InstanceFieldOrPropertyExpr<FieldInfo>
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

        public override object Eval()
        {
            return _tinfo.GetValue(_target.Eval());
        }

        #endregion

        #region Code generation

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

        protected override void EmitGet(CljILGen ilg)
        {
            ilg.MaybeEmitVolatileOp(_tinfo);
            ilg.EmitFieldGet(_tinfo);
        }

        protected override void EmitSet(CljILGen ilg)
        {
            if ( _tinfo.IsInitOnly )
            {
                throw new InvalidOperationException(String.Format("Attempt to set readonly field {0} in class {1}", _tinfo.Name, _tinfo.DeclaringType));
            }
            ilg.MaybeEmitVolatileOp(_tinfo);
            ilg.EmitFieldSet(_tinfo);
        }

        #endregion

        #region AssignableExpr members

        public override object EvalAssign(Expr val)
        {
            object target = _target.Eval();
            object e = val.Eval();

            if (_tinfo.IsInitOnly)
            {
                throw new InvalidOperationException(String.Format("Attempt to set readonly field {0} in class {1}", _tinfo.Name, _targetType));
            }

            _tinfo.SetValue(target, e);
            return e;
        }
        
        #endregion
    }

    public sealed class InstancePropertyExpr : InstanceFieldOrPropertyExpr<PropertyInfo>
    {
        #region C-tors

        public InstancePropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string propertyName, PropertyInfo pinfo)
            :base(source,spanMap,tag, target,propertyName,pinfo)  
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

        public override object Eval()
        {
            return _tinfo.GetValue(_target.Eval(), new object[0]);
        }

        #endregion

        #region Code generation

        public override bool CanEmitPrimitive
        {
            get { return _targetType != null && _tinfo != null && Util.IsPrimitive(_tinfo.PropertyType); }
        }

        protected override Type FieldType
        {
            get { return _tinfo.PropertyType; }
        }

        protected override void EmitGet(CljILGen ilg)
        {
            ilg.EmitPropertyGet(_tinfo);
        }

        protected override void EmitSet(CljILGen ilg)
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
