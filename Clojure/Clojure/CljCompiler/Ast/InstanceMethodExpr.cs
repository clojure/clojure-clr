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


namespace clojure.lang.CljCompiler.Ast
{
    public class InstanceMethodExpr : MethodExpr
    {
        #region Data

        readonly Expr _target;
        public Expr Target { get { return _target; } }

        #endregion

        #region Ctors

        public InstanceMethodExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string methodName, IList<Type> typeArgs, IList<HostArg> args, bool tailPosition)
            : base(source,spanMap,tag,methodName,typeArgs,args,tailPosition)
        {
            _target = target;

            if (target.HasClrType && target.ClrType == null)
                throw new ArgumentException(String.Format("Attempt to call instance method {0} on nil", methodName));

            _method = Reflector.GetMatchingMethod(spanMap, target, _args, _methodName, _typeArgs);
        }

        #endregion

        #region eval

        public override object Eval()
        {
            try
            {
                object targetVal = _target.Eval();
                object[] argvals = new object[_args.Count];
                for (int i = 0; i < _args.Count; i++)
                    argvals[i] = _args[i].ArgExpr.Eval();
                if (_method != null)
                    return Reflector.InvokeMethod(_method,targetVal, argvals);
                return Reflector.CallInstanceMethod(_methodName, _typeArgs, targetVal, argvals);
            }
            catch (Compiler.CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Compiler.CompilerException(_source, Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), e);
            }                    
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _method != null || _tag != null; }
        }

        public override Type ClrType
        {
            get { return Compiler.RetType((_tag != null ? HostExpr.TagToType(_tag) : null), (_method != null) ? _method.ReturnType : null); }
        }

        #endregion

        #region Code generation

        protected override bool IsStaticCall
        {
            get { return false; }
        }

        protected override void EmitTargetExpression(ObjExpr objx, CljILGen ilg)
        {
            _target.Emit(RHC.Expression, objx, ilg);
        }

        protected override Type GetTargetType()
        {
            return _target.HasClrType ? _target.ClrType : typeof(object);
        }

        #endregion
    }
}
