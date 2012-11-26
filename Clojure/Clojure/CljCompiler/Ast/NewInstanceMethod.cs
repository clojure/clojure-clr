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
    sealed class NewInstanceMethod : ObjMethod
    {
        #region Data

        string _name;

        Type[] _argTypes;

        Type _retType;

        static readonly Symbol dummyThis = Symbol.intern(null, "dummy_this_dlskjsdfower");

        List<MethodInfo> _minfos;

        public List<MethodInfo> MethodInfos
        {
            get { return _minfos; }
        }

        #endregion

        #region ObjMethod methods

        internal override int NumParams
        {
            get { return _argLocals.count(); }
        }

        internal override bool IsVariadic
        {
            get { return false; }
        }
      
        internal override int RequiredArity
        {
            get { return NumParams; }
        }

        internal override string MethodName
        {
            get { return _name; }
        }

        protected override string StaticMethodName
        {
            get { return _name + "__static"; }
        }

        protected override Type[] StaticMethodArgTypes
        {
            get { return _argTypes; }
        }

        protected override Type[] ArgTypes
        {
            get { return _argTypes; }
        }

        protected override Type ReturnType
        {
            get { return _retType; }
        }

        protected override Type StaticReturnType
        {
            get { return _retType; }
        }


        protected override string GetMethodName()
        {
            return _name;
        }

        protected override Type GetReturnType()
        {
            return _retType;
        }

        protected override Type[] GetArgTypes()
        {
            return _argTypes;
        }

        #endregion

        #region C-tors

        public NewInstanceMethod(ObjExpr objx, ObjMethod parent)
            : base(objx, parent)
        {
        }

        #endregion

        #region Parsing

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public static NewInstanceMethod Parse(ObjExpr objx, ISeq form, Symbol thisTag, Dictionary<IPersistentVector, List<MethodInfo>> overrideables)
        {
            // (methodname [this-name args*] body...)
            // this-name might be nil

            NewInstanceMethod method = new NewInstanceMethod(objx, (ObjMethod)Compiler.MethodVar.deref());

            Symbol dotName = (Symbol)RT.first(form);
            Symbol name;
            string methodName;

            int idx = dotName.Name.LastIndexOf(".");
            if (idx >= 0)
            {
                // we have an explicit interface implementation
                string dotNameStr = dotName.Name;
                string interfaceName = dotNameStr.Substring(0, idx);

                method._explicitInterface = RT.classForName(interfaceName);
                if (method._explicitInterface == null)
                    throw new ParseException(String.Format("Unable to find interface {0} for explicit method implemntation: {1}", interfaceName, dotNameStr));

                methodName = dotNameStr.Substring(idx + 1);
                name = (Symbol)Symbol.intern(null, Compiler.munge(dotName.Name)).withMeta(RT.meta(dotName));
            }
            else
            {
                name = (Symbol)Symbol.intern(null, Compiler.munge(dotName.Name)).withMeta(RT.meta(dotName));
                methodName = name.Name;
            }

            IPersistentVector parms = (IPersistentVector)RT.second(form);
            if (parms.count() == 0 || !(parms.nth(0) is Symbol))
                throw new ParseException("Must supply at least one argument for 'this' in: " + dotName);

            Symbol thisName = (Symbol)parms.nth(0);
            parms = RT.subvec(parms, 1, parms.count());
            ISeq body = RT.next(RT.next(form));
            try
            {

                method.SpanMap = (IPersistentMap)Compiler.SourceSpanVar.deref();

                // register as the current method and set up a new env frame
                // PathNode pnade = new PathNode(PATHTYPE.PATH, (PathNode) CLEAR_PATH.get());
                Var.pushThreadBindings(
                    RT.mapUniqueKeys(
                        Compiler.MethodVar, method,
                        Compiler.LocalEnvVar, Compiler.LocalEnvVar.deref(),
                        Compiler.LoopLocalsVar, null,
                        Compiler.NextLocalNumVar, 0
                    // CLEAR_PATH, pnode,
                    // CLEAR_ROOT, pnode,
                    // CLEAR_SITES, PersistentHashMap.EMPTY
                        ));

                // register 'this' as local 0
                //method._thisBinding = Compiler.RegisterLocalThis(((thisName == null) ? dummyThis : thisName), thisTag, null);
                Compiler.RegisterLocalThis(((thisName == null) ? dummyThis : thisName), thisTag, null);

                IPersistentVector argLocals = PersistentVector.EMPTY;
                method._retType = Compiler.TagType(Compiler.TagOf(name));
                method._argTypes = new Type[parms.count()];
                bool hinted = Compiler.TagOf(name) != null;
                Type[] pTypes = new Type[parms.count()];
                Symbol[] pSyms = new Symbol[parms.count()];
                bool[] pRefs = new bool[parms.count()];

                for (int i = 0; i < parms.count(); i++)
                {
                    // Param should be symbol or (by-ref symbol)
                    Symbol p;
                    bool isByRef = false;

                    object pobj = parms.nth(i);
                    if (pobj is Symbol)
                        p = (Symbol)pobj;
                    else if (pobj is ISeq)
                    {
                        ISeq pseq = (ISeq)pobj;
                        object first = RT.first(pseq);
                        object second = RT.second(pseq);
                        if (!(first is Symbol && ((Symbol)first).Equals(HostExpr.ByRefSym)))
                            throw new ParseException("First element in parameter pair must be by-ref");
                        if (!(second is Symbol))
                            throw new ParseException("Params must be Symbols");
                        isByRef = true;
                        p = (Symbol)second;
                        hinted = true;
                    }
                    else
                        throw new ParseException("Params must be Symbols or of the form (by-ref Symbol)");

                    object tag = Compiler.TagOf(p);
                    if (tag != null)
                        hinted = true;
                    if (p.Namespace != null)
                        p = Symbol.intern(p.Name);
                    Type pType = Compiler.TagType(tag);
                    if (isByRef)
                        pType = pType.MakeByRefType();

                    pTypes[i] = pType;
                    pSyms[i] = p;
                    pRefs[i] = isByRef;
                }

                Dictionary<IPersistentVector, List<MethodInfo>> matches =
                    method.IsExplicit 
                    ? FindMethodsWithNameAndArity(method._explicitInterface, methodName, parms.count(), overrideables)
                    : FindMethodsWithNameAndArity(methodName, parms.count(), overrideables);

                IPersistentVector mk = MSig(methodName, pTypes, method._retType);
                List<MethodInfo> ms = null;
                if (matches.Count > 0 )
                {
                    // multiple matches
                    if (matches.Count > 1)
                    {
                        // must be hinted and match one method
                        if (!hinted)
                            throw new ParseException("Must hint overloaded method: " + name.Name);
                        if (! matches.TryGetValue(mk,out ms) )
                            throw new ParseException("Can't find matching overloaded method: " + name.Name);

                        method._minfos = ms;

                        //if (m.ReturnType != method._retType)
                        //    throw new ArgumentException(String.Format("Mismatched return type: {0}, expected {1}, had: {2}",
                        //        name.Name, m.ReturnType.Name, method._retType.Name));
                    }
                    else // one match
                    {
                        // if hinted, validate match,
                        if (hinted)
                        {
                            if (!matches.TryGetValue(mk, out ms))
                                throw new ParseException("Can't find matching method: " + name.Name + ", leave off hints for auto match.");

                            method._minfos = ms;

                            //if (m.ReturnType != method._retType)
                            //    throw new ArgumentException(String.Format("Mismatched return type: {0}, expected {1}, had: {2}",
                            //        name.Name, m.ReturnType.Name, method._retType.Name));
                        }
                        else // adopt found method sig
                        {
                            using (var e = matches.GetEnumerator() )
                            {
                                e.MoveNext();
                                mk = e.Current.Key;
                                ms = e.Current.Value;
                            }
                            MethodInfo m = ms[0];
                            method._retType = m.ReturnType;
                            pTypes = Compiler.GetTypes(m.GetParameters());
                            method._minfos = ms;
                        }
                    }
                }
                else
                    throw new ParseException("Can't define method not in interfaces: " + name.Name);

                if (method.IsExplicit)
                    method._explicitMethodInfo = ms[0];

                // validate unique name + arity among additional methods

                for (int i = 0; i < parms.count(); i++)
                {
                    LocalBinding lb = Compiler.RegisterLocal(pSyms[i], null, new MethodParamExpr(pTypes[i]), true, pRefs[i]);
                    argLocals = argLocals.assocN(i, lb);
                    method._argTypes[i] = pTypes[i];
                }

                Compiler.LoopLocalsVar.set(argLocals);
                method._name = name.Name;
                method._methodMeta = GenInterface.ExtractAttributes(RT.meta(name));
                method._parms = parms;
                method._argLocals = argLocals;
                method._body = (new BodyExpr.Parser()).Parse(new ParserContext(RHC.Return), body);
                return method;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


        private static Dictionary<IPersistentVector, List<MethodInfo>> FindMethodsWithNameAndArity(
            String name, 
            int arity, 
            Dictionary<IPersistentVector, List<MethodInfo>> mm)
        {
            Dictionary<IPersistentVector, List<MethodInfo>> ret = new Dictionary<IPersistentVector, List<MethodInfo>>();
            
            foreach (KeyValuePair<IPersistentVector, List<MethodInfo>> kv in mm)
            {
                MethodInfo m = kv.Value[0];
                if (name.Equals(m.Name) && m.GetParameters().Length == arity)
                    ret[kv.Key] = kv.Value;
            }
            return ret;
        }

        private static Dictionary<IPersistentVector, List<MethodInfo>> FindMethodsWithNameAndArity(
            Type explicitInterface,
            String name,
            int arity,
            Dictionary<IPersistentVector, List<MethodInfo>> mm)
        {
            Dictionary<IPersistentVector, List<MethodInfo>> ret = new Dictionary<IPersistentVector, List<MethodInfo>>();

            foreach (KeyValuePair<IPersistentVector, List<MethodInfo>> kv in mm)
            {
                foreach (MethodInfo mi in kv.Value)
                    if (name.Equals(mi.Name) && mi.GetParameters().Length == arity && mi.DeclaringType == explicitInterface)
                    {
                        // Should be only one, but I'm being cautious.
                        List<MethodInfo> list;
                        if (!ret.TryGetValue(kv.Key, out list))
                        {
                            list = new List<MethodInfo>();
                            ret[kv.Key] = list;
                        }
                        list.Add(mi);
                    }
            }
            return ret;
        }

        public static IPersistentVector MSig(string name, Type[] paramTypes, Type retType)
        {
            return RT.vector(name, RT.seq(paramTypes), retType);
        }

        #endregion

        #region Code generation

        public override void Emit(ObjExpr fn, TypeBuilder tb)
        {

            MethodBuilder mb = tb.DefineMethod(GetMethodName(), MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, GetReturnType(), GetArgTypes());
            SetCustomAttributes(mb);

            //Console.Write("Compiling method {0} ", GetMethodName());
            //foreach (Type t in GetArgTypes())
            //    Console.Write("{0}, ", t.Name);
            //Console.WriteLine("returning {0}", GetReturnType().Name);

            CljILGen ilg = new CljILGen(mb.GetILGenerator());
            Label loopLabel = ilg.DefineLabel();

            GenContext.EmitDebugInfo(ilg, SpanMap);

            try 
            {
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar,loopLabel,Compiler.MethodVar,this));
                ilg.MarkLabel(loopLabel);
                EmitBody(Objx,ilg,_retType,_body);
                if ( _body.HasNormalExit() )
                    ilg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }

            if (IsExplicit)
                tb.DefineMethodOverride(mb, _explicitMethodInfo);
        }    

        #endregion
    }
}
