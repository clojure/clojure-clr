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
using System.Linq;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
using Microsoft.Scripting.Ast;
#endif
using System.Text;
using System.Reflection;
using System.Reflection.Emit;


namespace clojure.lang.CljCompiler.Ast
{
    class StaticInvokeExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        //readonly Type _target;
        readonly MethodInfo _method;
        readonly Type _retType;
        readonly IPersistentVector _args;

        readonly bool _variadic;
        readonly object _tag;

        public MethodInfo Method { get { return _method; } }

        #endregion

        #region Ctors

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "target")]
        public StaticInvokeExpr(Type target, MethodInfo method, bool variadic, IPersistentVector args, object tag)
        {
            //_target = target;
            _method = method;
            _retType = method.ReturnType;
            _variadic = variadic;
            _args = args;
            _tag = tag;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return Compiler.RetType((_tag != null ? HostExpr.TagToType(_tag) : _retType), _retType); }
        }

        #endregion

        #region parsing

        public static Expr Parse(Var v, ISeq args, object tag)
        {
            if (!v.isBound || v.get() == null)
            {
                //Console.WriteLine("Not bound: {0}", v);
                return null;
            }

            Type target = v.get().GetType();

            MethodInfo[] allMethods = target.GetMethods();
            bool variadic = false;
            int argcount = RT.count(args);
            MethodInfo method = null;
            ParameterInfo[] pInfos = null;

            foreach (MethodInfo m in allMethods)
            {
                //Console.WriteLine("Method {0}", m.Name);
                if (m.IsStatic && m.Name.Equals("invokeStatic"))
                {
                    pInfos = m.GetParameters();
                    if (argcount == pInfos.Length)
                    {
                        method = m;
                        variadic = argcount > 0 && pInfos[pInfos.Length - 1].ParameterType == typeof(ISeq);
                        break;
                    }
                    else if (argcount > pInfos.Length
                        && pInfos.Length > 0
                        && pInfos[pInfos.Length - 1].ParameterType == typeof(ISeq))
                    {
                        method = m;
                        variadic = true;
                        break;
                    }
                }
            }

            if (method == null)
                return null;

            IPersistentVector argv = PersistentVector.EMPTY;
            for (ISeq s = RT.seq(args); s != null; s = s.next())
                argv = argv.cons(Compiler.Analyze(new ParserContext(RHC.Expression), s.first()));

            return new StaticInvokeExpr(target, method, variadic, argv, tag);
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval StaticInvokeExpr");
        }

        #endregion

        #region Generating code


        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            EmitUnboxed(rhc, objx, ilg);
            if (rhc != RHC.Statement)
                HostExpr.EmitBoxReturn(objx, ilg, _retType);
            if (rhc == RHC.Statement )
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit()
        {
            return true;
        }


        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            if (_variadic)
            {
                
                ParameterInfo[] pinfos = _method.GetParameters();
                for (int i =0; i< pinfos.Length-1; i++ )
                {
                    Expr e = (Expr)_args.nth(i);
                    if (Compiler.MaybePrimitiveType(e) == pinfos[i].ParameterType)
                        ((MaybePrimitiveExpr)e).EmitUnboxed(RHC.Expression, objx, ilg);
                    else
                    {
                        e.Emit(RHC.Expression, objx, ilg);
                        HostExpr.EmitUnboxArg(objx, ilg, pinfos[i].ParameterType);
                    }
                }
                IPersistentVector restArgs = RT.subvec(_args, pinfos.Length - 1, _args.count());
                MethodExpr.EmitArgsAsArray(restArgs, objx, ilg);
                ilg.EmitCall(Compiler.Method_ArraySeq_create);               
            }
            else
                MethodExpr.EmitTypedArgs(objx, ilg, _method.GetParameters(), _args);

            ilg.EmitCall(_method);
        }

        #endregion

        #region MaybePrimitiveExpr Members

        public bool CanEmitPrimitive
        {
            get { return _retType.IsPrimitive; }
        }

        #endregion
    }
}
