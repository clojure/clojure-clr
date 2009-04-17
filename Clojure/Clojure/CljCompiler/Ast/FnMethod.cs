/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Linq.Expressions;

namespace clojure.lang.CljCompiler.Ast
{
    class FnMethod
    {

        #region Data

        // Java: when closures are defined inside other closures,
        // the closed over locals need to be propagated to the enclosing fn
        readonly FnMethod _parent;
        internal FnMethod Parent
        {
            get { return _parent; }
        } 


        IPersistentMap _locals = null;       // localbinding => localbinding
        public IPersistentMap Locals
        {
            get { return _locals; }
            set { _locals = value; }
        }

        IPersistentMap _indexLocals = null;  // num -> localbinding
        public IPersistentMap IndexLocals
        {
            get { return _indexLocals; }
            set { _indexLocals = value; }
        }

        IPersistentVector _reqParms = PersistentVector.EMPTY;  // localbinding => localbinding

        LocalBinding _restParm = null;

        Expr _body = null;

        FnExpr _fn;
        internal FnExpr Fn
        {
            get { return _fn; }
            set { _fn = value; }
        }

        IPersistentVector _argLocals;
        
        int _maxLocal = 0;
        public int MaxLocal
        {
            get { return _maxLocal; }
            set { _maxLocal = value; }
        }

        LocalBinding _thisBinding;

        // int line;

        IPersistentSet _localsUsedInCatchFinally = PersistentHashSet.EMPTY;
        public IPersistentSet LocalsUsedInCatchFinally
        {
            get { return _localsUsedInCatchFinally; }
            set { _localsUsedInCatchFinally = value; }
        }

        internal bool IsVariadic
        {
            get { return _restParm != null; }
        }


        internal int NumParams
        {
            get { return _reqParms.count() + (IsVariadic ? 1 : 0); }
        }

        internal int RequiredArity
        {
            get { return _reqParms.count(); }
        }
    
        #endregion

        #region C-tors

        public FnMethod(FnExpr fn, FnMethod parent)
        {
            _parent = parent;
            _fn = fn;
        }

        #endregion

        #region Parsing

        enum ParamParseState { Required, Rest, Done };

        internal static FnMethod Parse(FnExpr fn, ISeq form)
        {
            // ([args] body ... )

            IPersistentVector parms = (IPersistentVector)RT.first(form);
            ISeq body = RT.next(form);

            try
            {
                FnMethod method = new FnMethod(fn, (FnMethod)Compiler.METHODS.deref());
                // TODO: method.line = (Integer) LINE.deref();


                Var.pushThreadBindings(RT.map(
                    Compiler.METHODS, method,
                    Compiler.LOCAL_ENV, Compiler.LOCAL_ENV.deref(),
                    Compiler.LOOP_LOCALS, null,
                    Compiler.NEXT_LOCAL_NUM, 0));

                // register 'this' as local 0  
                method._thisBinding = Compiler.RegisterLocal(Symbol.intern(fn.ThisName ?? "fn__" + RT.nextID()), null, null);

                ParamParseState paramState = ParamParseState.Required;
                IPersistentVector argLocals = PersistentVector.EMPTY;
                int parmsCount = parms.count();

                for (int i = 0; i < parmsCount; i++)
                {
                    if (!(parms.nth(i) is Symbol))
                        throw new ArgumentException("fn params must be Symbols");
                    Symbol p = (Symbol)parms.nth(i);
                    if (p.Namespace != null)
                        throw new Exception("Can't use qualified name as parameter: " + p);
                    if (p.Equals(Compiler._AMP_))
                    {
                        if (paramState == ParamParseState.Required)
                            paramState = ParamParseState.Rest;
                        else
                            throw new Exception("Invalid parameter list");
                    }
                    else
                    {
                        LocalBinding b = Compiler.RegisterLocal(p,
                            paramState == ParamParseState.Rest ? Compiler.ISEQ : Compiler.TagOf(p),
                            null); // asdf-tag

                        argLocals = argLocals.cons(b);
                        switch (paramState)
                        {
                            case ParamParseState.Required:
                                method._reqParms = method._reqParms.cons(b);
                                break;
                            case ParamParseState.Rest:
                                method._restParm = b;
                                paramState = ParamParseState.Done;
                                break;
                            default:
                                throw new Exception("Unexpected parameter");
                        }
                    }
                }

                if (method.NumParams > Compiler.MAX_POSITIONAL_ARITY)
                    throw new Exception(string.Format("Can't specify more than {0} parameters", Compiler.MAX_POSITIONAL_ARITY));
                Compiler.LOOP_LOCALS.set(argLocals);
                method._argLocals = argLocals;
                method._body = (new BodyExpr.Parser()).Parse(body);
                return method;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        #endregion

        #region Code generation

        internal void GenerateCode(GenContext context)
        {
            MethodBuilder mb = GenerateStaticMethod(context);
            GenerateMethod(mb,context);
        }

        void GenerateMethod(MethodInfo staticMethodInfo, GenContext context)
        {
            string methodName = IsVariadic ? "doInvoke" : "invoke";

            TypeBuilder tb = context.FnExpr.TypeBuilder;

            // TODO: Cache all the CreateObjectTypeArray values
            MethodBuilder mb = tb.DefineMethod(methodName, MethodAttributes.ReuseSlot|MethodAttributes.Public|MethodAttributes.Virtual, typeof(object), Compiler.CreateObjectTypeArray(NumParams));
            ILGenerator gen = mb.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            for (int i = 1; i <= _argLocals.count(); i++)
                gen.Emit(OpCodes.Ldarg, i);
            gen.Emit(OpCodes.Call, staticMethodInfo);
            gen.Emit(OpCodes.Ret);            
        }

        MethodBuilder GenerateStaticMethod(GenContext context)
        {
            string methodName = GetStaticMethodName();
            FnExpr fn = context.FnExpr;
            TypeBuilder tb = fn.TypeBuilder;

            List<ParameterExpression> parms = new List<ParameterExpression>(_argLocals.count() + 1);

            ParameterExpression thisParm = Expression.Parameter(fn.BaseType, "this");
            _thisBinding.ParamExpression = thisParm;
            fn.ThisParam = thisParm;
            parms.Add(thisParm);

            try
            {
                LabelTarget loopLabel = Expression.Label("top");

                Var.pushThreadBindings(RT.map(Compiler.LOOP_LABEL, loopLabel, Compiler.METHODS, this));



                for (int i = 0; i < _argLocals.count(); i++)
                {
                    LocalBinding lb = (LocalBinding)_argLocals.nth(i);
                    ParameterExpression parm = Expression.Parameter(typeof(object), lb.Name);
                    lb.ParamExpression = parm;
                    parms.Add(parm);
                }

                Expression body =
                    Expression.Block(
                        Expression.Label(loopLabel),
                        Compiler.MaybeBox(_body.GenDlr(context)));
                LambdaExpression lambda = Expression.Lambda(body, parms);
                // TODO: Figure out why the Java code nulls all the local variables here.


                // TODO: Cache all the CreateObjectTypeArray values
                MethodBuilder mb = tb.DefineMethod(methodName, MethodAttributes.Static, typeof(object), Compiler.CreateObjectTypeArray(NumParams));

                lambda.CompileToMethod(mb);
                return mb;
            }
            finally
            {
                Var.popThreadBindings();
            }

        }

        private string GetStaticMethodName()
        {
            return String.Format("__invokeHelper_{0}{1}", RequiredArity, IsVariadic ? "v" : string.Empty);
        }

        #endregion
    }
}
