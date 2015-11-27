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
using System.Reflection;
using System.Reflection.Emit;


namespace clojure.lang.CljCompiler.Ast
{
    public class StaticMethodExpr : MethodExpr
    {
        #region Data

        readonly Type _type;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public Type Type { get { return _type; } }

        static readonly Keyword warnOnBoxedKeyword = Keyword.intern("warn-on-boxed");

        #endregion

        #region Ctors

        public StaticMethodExpr(string source, IPersistentMap spanMap, Symbol tag, Type type, string methodName, IList<Type> typeArgs, IList<HostArg> args, bool tailPosition)
            : base(source,spanMap,tag,methodName,typeArgs, args, tailPosition)
        {
            _type = type;
            _method  = Reflector.GetMatchingMethod(spanMap, _type, _args, _methodName, typeArgs);
            if ( _method != null && warnOnBoxedKeyword.Equals(RT.UncheckedMathVar.deref()) && IsBoxedMath(_method))
            {
                RT.errPrintWriter().WriteLine("Boxed math warning, {0}:{1}:{2} - call {3}.",
                    Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(spanMap), Compiler.GetColumnFromSpanMap(spanMap), _method.ToString());
            }
        }

        public static bool IsBoxedMath(MethodBase m)
        {
            Type t = m.DeclaringType;
            if ( t == typeof(Numbers))
            {
                object[] boxedMaths = m.GetCustomAttributes(typeof(WarnBoxedMathAttribute), true);
                if (boxedMaths.Length > 0)
                    return ((WarnBoxedMathAttribute)boxedMaths[0]).Value;

                ParameterInfo[] pis = ((MethodBase)m).GetParameters();
                foreach (ParameterInfo param in pis)
                    if (param.ParameterType.Equals(typeof(object)) )
                        return true;
            }

            return false;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _method != null || _tag != null; }
        }

        public override Type ClrType
        {
            get { return Compiler.RetType((_tag != null) ? HostExpr.TagToType(_tag) : null, (_method != null) ? _method.ReturnType : null); }
        }

        #endregion

        #region eval

        public override object Eval()
        {
            try
            {
                object[] argvals = new object[_args.Count];
                for (int i = 0; i < _args.Count; i++)
                    argvals[i] = _args[i].ArgExpr.Eval();
                if (_method != null)
                    return Reflector.InvokeMethod(_method, null, argvals);
                return Reflector.CallStaticMethod(_methodName, _typeArgs, _type, argvals);
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

        #region Code generation

        protected override bool IsStaticCall
        {
            get { return true; }
        }

        protected override void EmitTargetExpression(ObjExpr objx, CljILGen ilg)
        {
            ilg.Emit(OpCodes.Ldtoken, _type);
            ilg.Emit(OpCodes.Call, Compiler.Method_Type_GetTypeFromHandle);
        }

        protected override Type GetTargetType()
        {
            return typeof(Type);
        }

        internal bool CanEmitIntrinsicPredicate()
        {
            return _method != null && Intrinsics.HasPred(_method);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "rhc")]
        internal void EmitIntrinsicPredicate(RHC rhc, ObjExpr objx, CljILGen ilg, Label falseLabel)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (_method != null)
            {
                MethodExpr.EmitTypedArgs(objx, ilg, _method.GetParameters(), _args);
                // JVM: clear locals
                Intrinsics.EmitPred(_method, ilg, falseLabel);
            }
            else
                throw new InvalidOperationException("Unboxed emit of unknown member");
        }

        #endregion
    }
}
