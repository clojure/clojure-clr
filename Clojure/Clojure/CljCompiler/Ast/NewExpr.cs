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
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

using System.IO;

namespace clojure.lang.CljCompiler.Ast
{
    class NewExpr : Expr
    {
        #region Data

        readonly IPersistentVector _args;
        readonly ConstructorInfo _ctor;
        readonly Type _type;
        bool _isNoArgValueTypeCtor = false;
        readonly IPersistentMap _spanMap;

        #endregion

        #region Ctors

        public NewExpr(Type type, IPersistentVector args, IPersistentMap spanMap)
        {
            _args = args;
            _type = type;
            _spanMap = spanMap;
            _ctor = ComputeCtor();
        }

        private ConstructorInfo ComputeCtor()
        {
            int numArgs = _args.count();

            List<ConstructorInfo> cinfos 
                = new List<ConstructorInfo>(_type.GetConstructors()
                    .Where(x => x.GetParameters().Length == numArgs && x.IsPublic));

            if (cinfos.Count == 0)
            {
                if (_type.IsValueType && numArgs == 0)
                {
                    // Value types have a default no-arg c-tor that is not picked up in the regular c-tors.
                    _isNoArgValueTypeCtor = true;
                    return null;
                }
                throw new InvalidOperationException(string.Format("No constructor in type: {0} with {1} arguments", _type.Name, numArgs));
            }

            int index = 0;
            if (cinfos.Count > 1)
            {
                List<ParameterInfo[]> parms = new List<ParameterInfo[]>(cinfos.Count);
                List<Type> rets = new List<Type>(cinfos.Count);
                foreach (ConstructorInfo cinfo in cinfos)
                {
                    parms.Add(cinfo.GetParameters());
                    rets.Add(_type);
                }

                index = HostExpr.GetMatchingParams(".ctor", parms, _args, rets);
            }
            ConstructorInfo ctor = index >= 0 ? cinfos[index] : null;
            if (ctor == null && RT.booleanCast(RT.WARN_ON_REFLECTION.deref()))
                ((TextWriter)RT.ERR.deref()).WriteLine("Reflection warning, line: {0}:{1} - call to {2} ctor can't be resolved.",
                    Compiler.SOURCE_PATH.deref(), _spanMap == null ? (int)_spanMap.valAt(RT.START_LINE_KEY, 0) : 0, _type.FullName);
            return ctor;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return _type; }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext)
            {
                //int line = (int)Compiler.LINE.deref();

                ISeq form = (ISeq)frm;

                // form => (new Typename args ... )

                if (form.count() < 2)
                    throw new Exception("wrong number of arguments, expecting: (new Typename args ...)");

                Type t = Compiler.MaybeType(RT.second(form), false);
                if (t == null)
                    throw new ArgumentException("Unable to resolve classname: " + RT.second(form));

                IPersistentVector args = PersistentVector.EMPTY;
                for (ISeq s = RT.next(RT.next(form)); s != null; s = s.next())
                    args = args.cons(Compiler.GenerateAST(s.first(),false));

                return new NewExpr(t, args, Compiler.GetSourceSpanMap(form));
            }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            Expression result;

            if ( _ctor != null )
            {
                // The ctor is uniquely determined.

                Expression[] args = Compiler.GenTypedArgArray(context, _ctor.GetParameters(), _args);
                result = Expression.New(_ctor, args);

                // JAVA: emitClearLocals
            }
            else if (_isNoArgValueTypeCtor)
            {
                result = Expression.Default(_type);
            }
            else
            {
                Expression typeExpr = Expression.Call(Compiler.Method_RT_classForName, Expression.Constant(_type.FullName));
                Expression args = Compiler.GenArgArray(context, _args);
                // Java: emitClearLocals

                result = Expression.Call(Compiler.Method_Reflector_InvokeConstructor, typeExpr, args);
            }

            result = Compiler.MaybeAddDebugInfo(result, _spanMap);
            return result;
        }

        #endregion
    }
}
