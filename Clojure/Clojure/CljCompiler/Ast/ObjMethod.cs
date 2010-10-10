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
using System.Reflection.Emit;
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Generation;

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

        protected LocalBinding _thisBinding;
        protected Type _explicitInterface = null;
        protected MethodInfo _explicitMethodInfo = null;

        protected IPersistentVector _parms;

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

        #endregion

        #region abstract methods

        internal abstract bool IsVariadic { get; }
        internal abstract int NumParams { get; }
        internal abstract int RequiredArity { get; }
        internal abstract string MethodName { get; }
        protected abstract string StaticMethodName { get; }
        protected abstract Type ReturnType { get; }
        protected abstract Type[] ArgTypes { get; }

        #endregion

        #region Ctors

        public ObjMethod(ObjExpr fn, ObjMethod parent)
        {
            _parent = parent;
            _objx = fn;
        }

        #endregion

        #region Code generation

        internal void GenerateCode(ObjExpr objx, GenContext context)
        {
            MethodBuilder mb = GenerateStaticMethod(objx, context);
            GenerateMethod(mb, objx, context);
        }


        MethodBuilder GenerateStaticMethod(ObjExpr objx, GenContext context)
        {
            string methodName = StaticMethodName;
            TypeBuilder tb = objx.TypeBuilder;

            List<ParameterExpression> parms = new List<ParameterExpression>(_argLocals.count() + 1);

            ParameterExpression thisParm = Expression.Parameter(objx.BaseType, "this");
            if (_thisBinding != null)
            {
                _thisBinding.ParamExpression = thisParm;
                _thisBinding.Tag = Symbol.intern(null, objx.BaseType.FullName);
            }
            objx.ThisParam = thisParm;
            parms.Add(thisParm);

            try
            {
                LabelTarget loopLabel = Expression.Label("top");

                Var.pushThreadBindings(RT.map(Compiler.LOOP_LABEL, loopLabel, Compiler.METHOD, this));

                Type[] argTypes = ArgTypes;

                for (int i = 0; i < _argLocals.count(); i++)
                {
                    LocalBinding lb = (LocalBinding)_argLocals.nth(i);
                    ParameterExpression parm = Expression.Parameter(argTypes[i], lb.Name);
                    lb.ParamExpression = parm;
                    parms.Add(parm);
                }

                Expression body =
                    Expression.Block(
                        Expression.Label(loopLabel),
                        _body.GenCode(RHC.Return,objx,context));

                Expression convBody = Compiler.MaybeConvert(body, ReturnType);

                LambdaExpression lambda = Expression.Lambda(convBody, parms);
                // JVM: Clears locals here.


                // TODO: Cache all the CreateObjectTypeArray values
                MethodBuilder mb = tb.DefineMethod(methodName, MethodAttributes.Static, ReturnType, argTypes);

                lambda.CompileToMethod(mb, context.IsDebuggable);
                return mb;
            }
            finally
            {
                Var.popThreadBindings();
            }

        }


        void GenerateMethod(MethodInfo staticMethodInfo, ObjExpr objx, GenContext context)
        {

            TypeBuilder tb = objx.TypeBuilder;

            MethodBuilder mb = tb.DefineMethod(MethodName, MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, ReturnType, ArgTypes);

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

            ILGen gen = new ILGen(mb.GetILGenerator());
            gen.EmitLoadArg(0);                             
            for (int i = 1; i <= _argLocals.count(); i++)
                gen.EmitLoadArg(i);                         
            gen.EmitCall(staticMethodInfo);                 
            gen.Emit(OpCodes.Ret);

            if ( IsExplicit )
                tb.DefineMethodOverride(mb, _explicitMethodInfo);            

        }

        #endregion
    }
}
