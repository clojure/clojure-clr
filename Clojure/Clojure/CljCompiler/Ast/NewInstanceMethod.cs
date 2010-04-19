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
using System.Text;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    sealed class NewInstanceMethod : ObjMethod
    {
        internal override bool IsVariadic
        {
            get { throw new NotImplementedException(); }
        }

        internal override string MethodName
        {
            get { throw new NotImplementedException(); }
        }


        internal override int NumParams
        {
            get { throw new NotImplementedException(); }
        }

        internal override int RequiredArity
        {
            get { throw new NotImplementedException(); }
        }

        /*
        #region Data

        string _name;
        public string MethodName { get { return _name; } }

        Type[] _argTypes;
        public Type[] ArgTypes { get { return _argTypes; } }

        Type _retType;
        public Type ReturnType { get { return _retType; } }

        static readonly Symbol dummyThis = Symbol.intern(null, "dummy_this_dlskjsdfower");

        #endregion
         * 
         */

        #region C-tors

        public NewInstanceMethod(ObjExpr objx, ObjMethod parent)
            : base(objx, parent)
        {
        }

        #endregion

        /* 
        #region Accessors

        int NumParams { get { return _argLocals.count(); } }

        public static IPersistentVector MSig(string name, Type[] paramTypes)
        {
            return RT.vector(name, RT.seq(paramTypes));
        }

        #endregion

        #region Parsing

        public static NewInstanceMethod Parse(ObjExpr objx, ISeq form, Symbol thisTag, Dictionary<IPersistentVector, MethodInfo> overrideables)
        {
            // (methodname [this-name args*] body...)
            // this-name might be nil

            NewInstanceMethod method = new NewInstanceMethod(objx, (ObjMethod)Compiler.METHOD.deref());

            Symbol dotName = (Symbol)RT.first(form);
            Symbol name = (Symbol)Symbol.intern(null, Compiler.Munge(dotName.Name)).withMeta(RT.meta(dotName));

            IPersistentVector parms = (IPersistentVector)RT.second(form);
            if (parms.count() == 0)
                throw new ArgumentException("Must supply at least one argument for 'this' in: " + dotName);

            Symbol thisName = (Symbol)parms.nth(0);
            parms = RT.subvec(parms, 1, parms.count());
            ISeq body = RT.next(RT.next(form));
            try
            {
                // TODO: Add sourcelocation information
                // method.line = (Integer) LINE.deref();

                // register as the current method and set up a new env frame
                // PathNode pnade = new PathNode(PATHTYPE.PATH, (PathNode) CLEAR_PATH.get());
                Var.pushThreadBindings(
                    RT.map(
                        Compiler.METHOD, method,
                        Compiler.LOCAL_ENV, Compiler.LOCAL_ENV.deref(),
                        Compiler.LOOP_LOCALS, null,
                        Compiler.NEXT_LOCAL_NUM, 0
                    // CLEAR_PATH, pnode,
                    // CLEAR_ROOT, pnode,
                    // CLEAR_SITES, PersistentHashMap.EMPTY
                        ));

                // register 'this' as local 0
                Compiler.RegisterLocal(((thisName == null) ? dummyThis : thisName), thisTag, null, false);

                IPersistentVector argLocals = PersistentVector.EMPTY;
                method._retType = Compiler.TagType(Compiler.TagOf(name));
                method._argTypes = new Type[parms.count()];
                bool hinted = Compiler.TagOf(name) != null;
                Type[] pTypes = new Type[parms.count()];
                Symbol[] pSyms = new Symbol[parms.count()];

                for (int i = 0; i < parms.count(); i++)
                {
                    if (!(parms.nth(i) is Symbol))
                        throw new ArgumentException("Params must be Symbols");
                    Symbol p = (Symbol)parms.nth(i);
                    object tag = Compiler.TagOf(p);
                    if (tag != null)
                        hinted = true;
                    if (p.Namespace != null)
                        p = Symbol.create(p.Name);
                    Type pType = Compiler.TagType(tag);
                    pTypes[i] = pType;
                    pSyms[i] = p;
                }
                Dictionary<IPersistentVector, MethodInfo> matches = FindMethodsWithNameAndArity(name.Name, parms.count(), overrideables);
                IPersistentVector mk = MSig(name.Name, pTypes);
                MethodInfo m = null;
                if (matches.Count > 0)
                {
                    // multiple methods
                    if (matches.Count > 1)
                    {
                        // must be hinted and match one method
                        if (!hinted)
                            throw new ArgumentException("Must hint overloaded method: " + name.Name);
                        m = matches[mk];
                        if (m == null)
                            throw new ArgumentException("Can't find matching overloaded method: " + name.Name);
                        if (m.ReturnType != method._retType)
                            throw new ArgumentException(String.Format("Mismatched return type: {0}, expected {1}, had: {2}",
                                name.Name, m.ReturnType.Name, method._retType.Name));
                    }
                    else // one match
                    {
                        // if hinted, validate match,
                        if (hinted)
                        {
                            m = matches[mk];
                            if (m == null)
                                throw new ArgumentException("Can't find matching method: " + name.Name + ", leave off hints for auto match.");
                            if (m.ReturnType != method._retType)
                                throw new ArgumentException(String.Format("Mismatched return type: {0}, expected {1}, had: {2}",
                                    name.Name, m.ReturnType.Name, method._retType.Name));
                        }
                        else // adopt found method sig
                        {
                            using (var e = matches.Values.GetEnumerator())
                            {
                                e.MoveNext();
                                m = e.Current;
                            }
                            method._retType = m.ReturnType;
                            pTypes = Compiler.GetTypes(m.GetParameters());
                        }
                    }
                }
                else
                    throw new ArgumentException("Can't define method not in interfaces: " + name.Name);
                
                // validate unique name + arity among additional methods

                for (int i = 0; i < parms.count(); i++)
                {
                    LocalBinding lb = Compiler.RegisterLocal(pSyms[i], null, new MethodParamExpr(pTypes[i]), true);
                    argLocals = argLocals.assocN(i, lb);
                    method._argTypes[i] = pTypes[i];
                }
                //for (int i = 0; i < parms.count(); i++)
                //    if (pTypes[i] == typeof(long) || pTypes[i] == typeof(double))
                //        Compiler.GetAndIncLocalNum();
                Compiler.LOOP_LOCALS.set(argLocals);
                method._name = name.Name;
                method._argLocals = argLocals;
                method._body = (new BodyExpr.Parser()).Parse(body, true);
                return method;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


        private static Dictionary<IPersistentVector, MethodInfo> FindMethodsWithNameAndArity(String name, int arity, Dictionary<IPersistentVector, MethodInfo> mm)
        {
            Dictionary<IPersistentVector, MethodInfo> ret = new Dictionary<IPersistentVector, MethodInfo>();

            foreach (KeyValuePair<IPersistentVector, MethodInfo> kv in mm)
            {
                MethodInfo m = kv.Value;
                if (name.Equals(m.Name) && m.GetParameters().Length == arity)
                    ret[kv.Key] = m;
            }
            return ret;
        }



        #endregion

        */

 
    }
}
