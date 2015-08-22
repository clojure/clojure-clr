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
    public abstract class StaticFieldOrPropertyExpr<TInfo> : FieldOrPropertyExpr
    {
        #region Data

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        protected readonly string _memberName;
        public string MemberName { get { return _memberName; } }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        protected readonly Type _type;
        public Type MemberType { get { return _type; } }

        protected readonly TInfo _tinfo;
        public TInfo MemberInfo { get { return _tinfo; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        protected readonly string _source;
        public string Source { get { return _source; } }

        protected readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }

        protected readonly Symbol _tag;
        public Symbol Tag { get { return _tag; } }

        #endregion

        #region Ctors

        protected StaticFieldOrPropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string fieldName, TInfo tinfo)
        {
            _source = source;
            _spanMap = spanMap;
            _memberName = fieldName;
            _type = type;
            _tinfo = tinfo;
            _tag = tag;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        #endregion

        #region Code generation

        public override void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            EmitUnboxed(RHC.Expression, objx, ilg);
            HostExpr.EmitBoxReturn(objx, ilg, FieldType);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion
    }

    public sealed class StaticFieldExpr : StaticFieldOrPropertyExpr<FieldInfo>
    {
        #region C-tors

        public StaticFieldExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string fieldName, FieldInfo finfo)
            : base(source, spanMap, tag, type, fieldName, finfo)
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
            return _tinfo.GetValue(null);
        }

        #endregion

        #region Code generation

        public override bool CanEmitPrimitive
        {
            get { return Util.IsPrimitive(_tinfo.FieldType); }
        }

        protected override Type FieldType
        {
            get { return _tinfo.FieldType; }
        }

        public override void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);
            if (_tinfo.IsLiteral)
            {
                // literal fields need to be inlined directly in here... We use GetRawConstant
                // which will work even in partial trust if the constant is protected.
                object value = _tinfo.GetRawConstantValue();
                switch (Type.GetTypeCode(_tinfo.FieldType))
                {
                    case TypeCode.Boolean:
                        if ((bool)value)
                        {
                            ilg.Emit(OpCodes.Ldc_I4_1);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Ldc_I4_0);
                        }
                        break;
                    case TypeCode.Byte: ilg.EmitInt((int)(byte)value); break;
                    case TypeCode.Char: ilg.EmitInt((int)(char)value); break;
                    case TypeCode.Double: ilg.EmitDouble((double)value); break;
                    case TypeCode.Int16: ilg.EmitInt((int)(short)value); break;
                    case TypeCode.Int32: ilg.EmitInt((int)value); break;
                    case TypeCode.Int64: ilg.EmitLong((long)value); break;
                    case TypeCode.SByte: ilg.EmitInt((int)(sbyte)value); break;
                    case TypeCode.Single: ilg.EmitSingle((float)value); break;
                    case TypeCode.String: ilg.EmitString((string)value); break;
                    case TypeCode.UInt16: ilg.EmitInt((int)(ushort)value); break;
                    case TypeCode.UInt32: ilg.Emit(OpCodes.Ldc_I4, (uint)value); break;
                    case TypeCode.UInt64: ilg.Emit(OpCodes.Ldc_I8, (ulong)value); break;
                }
            }
            else
            {
                ilg.MaybeEmitVolatileOp(_tinfo);
                ilg.EmitFieldGet(_tinfo);
            }
        }

        #endregion

        #region AssignableExpr members

        public override object EvalAssign(Expr val)
        {
            if (_tinfo.IsInitOnly)
            {
                throw new InvalidOperationException(String.Format("Attempt to set readonly static field {0} in class {1}", _tinfo.Name, _tinfo.DeclaringType));
            }

            object e = val.Eval();
            _tinfo.SetValue(null, e);
            return e;
        }

        public override void EmitAssign(RHC rhc, ObjExpr objx, CljILGen ilg, Expr val)
        {
            if (_tinfo.IsInitOnly)
            {
                throw new InvalidOperationException(String.Format("Attempt to set readonly static field {0} in class {1}", _tinfo.Name, _tinfo.DeclaringType));
            }

            GenContext.EmitDebugInfo(ilg, _spanMap);

            val.Emit(RHC.Expression, objx, ilg);
            ilg.Emit(OpCodes.Dup);
            HostExpr.EmitUnboxArg(objx, ilg, FieldType);
            ilg.MaybeEmitVolatileOp(_tinfo);
            ilg.EmitFieldSet(_tinfo);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }
        #endregion
    }

    public sealed class StaticPropertyExpr : StaticFieldOrPropertyExpr<PropertyInfo>
    {
        #region C-tors

        public StaticPropertyExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string propertyName, PropertyInfo pinfo)
            : base(source, spanMap, tag, type, propertyName, pinfo)
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
            return _tinfo.GetValue(null,new object[0]);
        }

        #endregion

        #region Code generation

        public override bool CanEmitPrimitive
        {
            get { return Util.IsPrimitive(_tinfo.PropertyType); }
        }

        protected override Type FieldType
        {
            get { return _tinfo.PropertyType; }
        }

        public override void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);
            ilg.EmitPropertyGet(_tinfo);
        }

        #endregion

        #region AssignableExpr members

        public override object EvalAssign(Expr val)
        {
            object e = val.Eval();
            _tinfo.SetValue(null, e, new object[0]);
            return e;
        }

        public override void EmitAssign(RHC rhc, ObjExpr objx, CljILGen ilg, Expr val)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);

            val.Emit(RHC.Expression, objx, ilg);
            ilg.Emit(OpCodes.Dup);
            HostExpr.EmitUnboxArg(objx, ilg, FieldType);
            ilg.EmitPropertySet(_tinfo);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion
    }
}
