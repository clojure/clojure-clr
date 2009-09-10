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
using Microsoft.Linq.Expressions;
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

        #endregion

        #region Ctors

        public NewExpr(Type type, IPersistentVector args, int line)
        {
            _args = args;
            _type = type;
            _ctor = ComputeCtor(line);
        }

        private ConstructorInfo ComputeCtor(int line)
        {
            int numArgs = _args.count();

            // TODO: solve the DateTime problem
            // Documentation does not list no-arg DateTime c-tor
            // However, it exists.
            // Not picked up here.
            // Possible solution?
            
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
                    Compiler.SOURCE_PATH.deref(), line, _type.FullName);
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
                int line = (int)Compiler.LINE.deref();

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

                return new NewExpr(t, args,line);
            }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            if ( _ctor != null )
            {
                // The ctor is uniquely determined.

                Expression[] args = Compiler.GenTypedArgArray(context, _ctor.GetParameters(), _args);
                return Expression.New(_ctor, args);

                // JAVA: emitClearLocals
            }
            else if (_isNoArgValueTypeCtor)
            {
                return Expression.Default(_type);
            }
            else
            {
                Expression typeExpr = Expression.Call(Compiler.Method_RT_classForName, Expression.Constant(_type.FullName));
                Expression args = Compiler.GenArgArray(context, _args);
                // Java: emitClearLocals

                return Expression.Call(Compiler.Method_Reflector_InvokeConstructor, typeExpr, args);
            }        
        }

        #endregion
    }
}
