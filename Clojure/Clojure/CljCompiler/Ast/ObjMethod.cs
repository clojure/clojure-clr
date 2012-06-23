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
using System.Reflection.Emit;
using System.Reflection;


namespace clojure.lang.CljCompiler.Ast
{
    abstract class ObjMethod
    {
        #region Data

        // Java: when closures are defined inside other closures,
        // the closed over locals need to be propagated to the enclosing objx
        readonly ObjMethod _parent;
        IPersistentMap _locals = null;       // localbinding => localbinding
        IPersistentMap _indexLocals = null;  // num -> localbinding
        protected Expr _body = null;
        ObjExpr _objx;
        protected IPersistentVector _argLocals;
        int _maxLocal = 0;
        IPersistentSet _localsUsedInCatchFinally = PersistentHashSet.EMPTY;
        protected IPersistentMap _methodMeta;

        //protected LocalBinding _thisBinding;
        protected Type _explicitInterface = null;
        protected MethodInfo _explicitMethodInfo = null;

        protected IPersistentVector _parms;

        protected IPersistentMap SpanMap { get; set; }

        #endregion

        #region Data accessors

        internal ObjMethod Parent
        {
            get { return _parent; }
        }

        public IPersistentMap Locals
        {
            get { return _locals; }
            set { _locals = value; }
        }

        public IPersistentMap IndexLocals
        {
            get { return _indexLocals; }
            set { _indexLocals = value; }
        }

        internal ObjExpr Objx
        {
            get { return _objx; }
            //set { _objx = value; }
        }

        public int MaxLocal
        {
            get { return _maxLocal; }
            set { _maxLocal = value; }
        }

        public IPersistentSet LocalsUsedInCatchFinally
        {
            get { return _localsUsedInCatchFinally; }
            set { _localsUsedInCatchFinally = value; }
        }

        protected bool IsExplicit { get { return _explicitInterface != null; } }

        public virtual string Prim { get { return null; } }

        #endregion

        #region abstract methods

        internal abstract bool IsVariadic { get; }
        internal abstract int NumParams { get; }
        internal abstract int RequiredArity { get; }
        internal abstract string MethodName { get; }
        protected abstract string StaticMethodName { get; }
        protected abstract Type ReturnType { get; }
        protected abstract Type StaticReturnType { get; }
        protected abstract Type[] ArgTypes { get; }
        protected abstract Type[] StaticMethodArgTypes { get; }

        #endregion

        #region Ctors

        public ObjMethod(ObjExpr fn, ObjMethod parent)
        {
            _parent = parent;
            _objx = fn;
        }

        #endregion

        #region Code generation

        protected abstract String GetMethodName();
        protected abstract Type GetReturnType();
        protected abstract Type[] GetArgTypes();

        public virtual void Emit(ObjExpr fn, TypeBuilder tb)
        {
            MethodBuilder mb = tb.DefineMethod(GetMethodName(), MethodAttributes.Public, GetReturnType(), GetArgTypes());

            CljILGen ilg = new CljILGen(mb.GetILGenerator());
            Label loopLabel = ilg.DefineLabel();

            GenContext.EmitDebugInfo(ilg, SpanMap);

            try 
            {
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar,loopLabel,Compiler.MethodVar,this));
                ilg.MarkLabel(loopLabel);
                _body.Emit(RHC.Return,fn,ilg);
                ilg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        protected static void EmitBody(ObjExpr objx, CljILGen ilg, Type retType, Expr body)
        {
            MaybePrimitiveExpr be = (MaybePrimitiveExpr)body;
            if (Util.IsPrimitive(retType) && be.CanEmitPrimitive)
            {
                Type bt = Compiler.MaybePrimitiveType(be);
                if (bt == retType)
                    be.EmitUnboxed(RHC.Return, objx, ilg);
                else if (retType == typeof(long) && bt == typeof(int))
                {
                    be.EmitUnboxed(RHC.Return, objx, ilg);
                    ilg.Emit(OpCodes.Conv_I8);
                }
                else if (retType == typeof(double) && bt == typeof(float))
                {
                    be.EmitUnboxed(RHC.Return, objx, ilg);
                    ilg.Emit(OpCodes.Conv_R8);
                }
                else if (retType == typeof(int) && bt == typeof(long))
                {
                    be.EmitUnboxed(RHC.Return, objx, ilg);
                    ilg.Emit(OpCodes.Call,Compiler.Method_RT_intCast_long);
                }
                else if (retType == typeof(float) && bt == typeof(double))
                {
                    be.EmitUnboxed(RHC.Return, objx, ilg);
                    ilg.Emit(OpCodes.Conv_R4);
                }
                else
                {
                    throw new ArgumentException(String.Format("Mismatched primitive return, expected: {0}, had: {1}", retType, be.ClrType));
                }
            }
            else
            {
                body.Emit(RHC.Return, objx, ilg);
                if (body.HasNormalExit())
                {
                    if (retType == typeof(void))
                        ilg.Emit(OpCodes.Pop);
                    else
                        EmitUnboxArg(ilg, typeof(object), retType);
                }
            }
        }

        protected void SetCustomAttributes(MethodBuilder mb)
        {
            GenInterface.SetCustomAttributes(mb, _methodMeta);
            if (_parms != null)
            {
                for (int i = 0; i < _parms.count(); i++)
                {
                    IPersistentMap meta = GenInterface.ExtractAttributes(RT.meta(_parms.nth(i)));
                    if (meta != null && meta.count() > 0)
                    {
                        ParameterBuilder pb = mb.DefineParameter(i + 1, ParameterAttributes.None, ((Symbol)_parms.nth(i)).Name);
                        GenInterface.SetCustomAttributes(pb, meta);
                    }
                }
            }
        }

        static void EmitUnboxArg(CljILGen ilg, Type argType, Type paramType)
        {
            if (argType == paramType)
                return;
            HostExpr.EmitUnboxArg(ilg, paramType);
        }

        #endregion
    }
}
