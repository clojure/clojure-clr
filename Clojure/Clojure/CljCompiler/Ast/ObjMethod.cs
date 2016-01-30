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
    public abstract class ObjMethod
    {
        #region Data

        // Java: when closures are defined inside other closures,
        // the closed over locals need to be propagated to the enclosing objx
        readonly ObjMethod _parent;
        public ObjMethod Parent { get { return _parent; } }

        public IPersistentMap Locals { get; protected set; } // localbinding => localbinding
        public IPersistentMap IndexLocals { get; protected set; }  // num -> localbinding

        readonly ObjExpr _objx;
        public ObjExpr Objx { get { return _objx; } }

        public Expr Body { get; protected set; }
        public IPersistentVector ArgLocals { get; protected set; }
        public int MaxLocal { get; set; }
        public IPersistentSet LocalsUsedInCatchFinally { get; set; }

        public IPersistentMap MethodMeta { get; protected set; }

        public Type ExplicitInterface { get; protected set; }
        public MethodInfo ExplicitMethodInfo { get; protected set; }

        public IPersistentVector Parms { get; protected set; }

        public IPersistentMap SpanMap { get; protected set; }
        public bool UsesThis { get; set; }

        #endregion

        #region Data accessors

        public void AddLocal(int index, LocalBinding lb)
        {
            Locals = (IPersistentMap)RT.assoc(Locals, lb, lb);
            IndexLocals = (IPersistentMap)RT.assoc(IndexLocals, index, lb);
        }

        public void SetLocals(IPersistentMap locals, IPersistentMap indexLocals)
        {
            Locals = locals;
            IndexLocals = indexLocals;
        }

        protected bool IsExplicit { get { return ExplicitInterface != null; } }

        public virtual string Prim { get { return null; } }

        #endregion

        #region abstract methods

        public abstract bool IsVariadic { get; }
        public abstract int NumParams { get; }
        public abstract int RequiredArity { get; }
        public abstract string MethodName { get; }
        public abstract Type ReturnType { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public abstract Type[] ArgTypes { get; }

        #endregion

        #region Ctors

        protected ObjMethod(ObjExpr fn, ObjMethod parent)
        {
            _parent = parent;
            _objx = fn;
            LocalsUsedInCatchFinally = PersistentHashSet.EMPTY;
        }

        #endregion

        #region Code generation

        public virtual void Emit(ObjExpr fn, TypeBuilder tb)
        {
            MethodBuilder mb = tb.DefineMethod(MethodName, MethodAttributes.Public, ReturnType, ArgTypes);

            CljILGen ilg = new CljILGen(mb.GetILGenerator());
            Label loopLabel = ilg.DefineLabel();

            GenContext.EmitDebugInfo(ilg, SpanMap);

            try 
            {
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar,loopLabel,Compiler.MethodVar,this));
                ilg.MarkLabel(loopLabel);
                Body.Emit(RHC.Return,fn,ilg);
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
            GenInterface.SetCustomAttributes(mb, MethodMeta);
            if (Parms != null)
            {
                for (int i = 0; i < Parms.count(); i++)
                {
                    IPersistentMap meta = GenInterface.ExtractAttributes(RT.meta(Parms.nth(i)));
                    if (meta != null && meta.count() > 0)
                    {
                        ParameterBuilder pb = mb.DefineParameter(i + 1, ParameterAttributes.None, ((Symbol)Parms.nth(i)).Name);
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

        //static void EmitClearThis(CljILGen ilg) 
        //{
        //    ilg.EmitNull();
        //    ilg.EmitStoreArg(0);
        //}

        #endregion
    }
}
