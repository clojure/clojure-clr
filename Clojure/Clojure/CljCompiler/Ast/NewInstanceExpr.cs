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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    sealed class NewInstanceExpr : ObjExpr
    {
        /*

        #region Data

        Dictionary<IPersistentVector,MethodInfo> _methodMap;
        Dictionary<IPersistentVector,HashSet<Type>> _covariants;

        #endregion

         */


        #region C-tors

        public NewInstanceExpr(object tag)
            : base(tag)
        {
        }

        #endregion

        public sealed class ReifyParser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext) { throw new NotImplementedException(); }
        }

        public sealed class DefTypeParser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext) { throw new NotImplementedException(); }
        }


        protected override Expression GenDlrImmediate(GenContext context)
        {
            throw new NotImplementedException();
        }

        protected override void GenerateMethods(GenContext context)
        {
            throw new NotImplementedException();
        }
        /*
        #region Type mangling


        #endregion

        #region Parsing

        public sealed class ReifyParser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext)
            {
                // frm is:  (reify this-name? [interfaces] (method-name [args] body)* )
                ISeq form = (ISeq)frm;
                ObjMethod enclosingMethod = (ObjMethod)Compiler.METHOD.deref();
                string baseName = enclosingMethod != null
                    ? (ObjExpr.TrimGenID(enclosingMethod.Objx.Name) + "$")
                    : (Compiler.Munge(Compiler.CurrentNamespace.Name.Name) + "$");
                string simpleName = "reify__" + RT.nextID();
                string className = baseName + simpleName;

                ISeq rform = RT.next(form);

                IPersistentVector interfaces = (IPersistentVector)RT.first(rform);

                rform = RT.next(rform);

                return Build(interfaces, null, null, className, className, null, rform);

            }
        }

        internal static ObjExpr Build(IPersistentVector interfaceSyms, IPersistentVector fieldSyms, Symbol thisSym,
            string tagName, string className, Symbol typeTag, ISeq methodForms)
        {
            NewInstanceExpr ret = new NewInstanceExpr(null);
            ret._name = className;
            ret._internalName = ret.Name.Replace('.','/');
            ret._objType = null; // ???

            if ( thisSym != null )
                ret._thisName = thisSym.Name;

            if ( fieldSyms != null )
            {
                IPersistentMap fmap = PersistentHashMap.EMPTY;
                object[] closesvec = new object[2*fieldSyms.count()];
                for ( int i=0; i<fieldSyms.count(); i++ )
                {
                    Symbol sym = (Symbol) fieldSyms.nth(i);
                    LocalBinding lb = new LocalBinding(-1, sym, null, new MethodParamExpr(Compiler.TagType(Compiler.TagOf(sym))), false);
                    fmap = fmap.assoc(sym,lb);
                    closesvec[i*2] = lb;
                    closesvec[i*2+1] = lb;
                    if (!sym.Name.StartsWith("__"))
                        CompileLookupThunk(ret,sym);
                }
                // Java TODO: inject __meta et al into closes - when?
                // use array map to preserve ctor order
                ret._closes = new PersistentArrayMap(closesvec);
                ret._fields = fmap;
                for ( int i=fieldSyms.count()-1; i>= 0 && ((Symbol)fieldSyms.nth(i)).Name.StartsWith("__");--i)
                    ret.altCtorDrops++;
            }

            // Java TODO: set up volatiles
            //ret._volatiles = PersistentHashSet.create(RT.seq(RT.get(ret._optionsMap, volatileKey)));

            IPersistentVector interfaces = PersistentVector.EMPTY;
            for ( ISeq s = RT.seq(interfaceSyms); s != null; s = s.next() )
            {
                Type t = (Type) Compiler.Resolve((Symbol) s.first());
                if ( ! t.IsInterface )
                    throw new ArgumentException("only interfaces are supported, had: " + t.Name);
                interfaces = interfaces.cons(t);
            }
            Type superClass = typeof(Object);
             
            Dictionary<IPersistentVector,MethodInfo> overrideables;
            Dictionary<IPersistentVector,HashSet<Type>> covariants;
            GatherMethods(superClass,RT.seq(interfaces), out overrideables, out covariants);

            ret._methodMap = overrideables;
            ret._covariants = covariants;

            string[] inames = InterfaceNames(interfaces);

            //Type stub = CompileStub(SlashName(superClass),ret,inames);
            //Symbol thisTag = Symbol.intern(null,stub.Name);
            Type stub = null;
            Symbol thisTag = Symbol.intern(null, Compiler.COMPILE_STUB_PREFIX + "/" + ret._internalName);
            try 
            {
                Var.pushThreadBindings(
                    RT.map(
                        Compiler.CONSTANTS,PersistentVector.EMPTY,
                        Compiler.KEYWORDS,PersistentHashMap.EMPTY,
                        Compiler.VARS, PersistentHashMap.EMPTY,
                        Compiler.KEYWORD_CALLSITES, PersistentVector.EMPTY,
                        Compiler.PROTOCOL_CALLSITES, PersistentVector.EMPTY,
                        Compiler.VAR_CALLSITES, PersistentVector.EMPTY
                        ));
                if ( ret.IsDefType)
                {
                    Var.pushThreadBindings(
                        RT.map(
                            Compiler.METHOD, null,
                            Compiler.LOCAL_ENV, ret._fields,
                            Compiler.COMPILE_STUB_SYM, Symbol.intern(null,tagName),
                            Compiler.COMPILE_STUB_CLASS, stub
                            ));
                }
                // now (methodname [args] body)*
                // TODO: SourceLocation?
                //ret.line = (Integer)LINE.deref();
                IPersistentCollection methods = null;
                for (ISeq s = methodForms; s != null; s = RT.next(s))
                {
                    NewInstanceMethod m = NewInstanceMethod.Parse(ret, (ISeq)RT.first(s), thisTag, overrideables);
                    methods = RT.conj(methods, m);
                }

                ret._methods = methods;
                ret._keywords = (IPersistentMap)Compiler.KEYWORDS.deref();
                ret._vars = (IPersistentMap)Compiler.VARS.deref();
                ret._constants = (PersistentVector) Compiler.CONSTANTS.deref();
                ret._constantsID = RT.nextID();
                ret._keywordCallsites = (IPersistentVector) Compiler.KEYWORD_CALLSITES.deref();
                ret._protocolCallsites = (IPersistentVector) Compiler.PROTOCOL_CALLSITES.deref();
                ret._varCallsites = (IPersistentVector) Compiler.VAR_CALLSITES.deref();
            }
            finally
            {
                if ( ret.IsDefType )
                    Var.popThreadBindings();
                Var.popThreadBindings();
            }

            // TODO: This is silly.  We have the superClass in hand.  Might as well stash it.
            ret._superName = SlashName(superClass);
            //ret.Copmile(SlashName(superClass),inames,false);
            //ret.getCompiledClass();
            return ret;
        }

        private static Expression<LookupThunkDelegate> CompileLookupThunk(NewInstanceExpr ret, Symbol fld)
        {
            // TODO: WHere do we put this delegate?  compiled vs eval?
            ParameterExpression p = Expression.Parameter(typeof(Object), "x");
            Expression e =
                Expression.IfThenElse(
                    Expression.TypeIs(p, ret._objType),
                    Compiler.MaybeBox(Expression.Field(p, ret._objType, Compiler.Munge(fld.Name))),
                    p);
            Expression<LookupThunkDelegate> lambda = Expression.Lambda<LookupThunkDelegate>(e, p);
            return lambda;
        }

        public sealed class DefTypeParser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext)
            {
                return null;
            }
        }

        static string[] InterfaceNames(IPersistentVector interfaces)
        {
            int icnt = interfaces.count();
            string[] inames = icnt > 0 ? new string[icnt] : null;
            for (int i = 0; i < icnt; i++)
                inames[i] = SlashName((Type)interfaces.nth(i));
            return inames;
        }


        static string SlashName(Type t)
        {
            return t.FullName.Replace(',', '/');
        }


        #endregion

        #region Code generation

        protected override void GenerateMethods(GenContext context)
        {
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.GenerateCode(context);
            }
        }

        protected override void GenerateMethodsForImmediate(GenContext context, ParameterExpression thisParam, List<Expression> exprs)
        {
            base.GenerateMethodsForImmediate(context, thisParam, exprs);
        }


        #endregion

        #region Method reflection

        static void GatherMethods(Type t, Dictionary<IPersistentVector, MethodInfo> mm)
        {
            for (; t != null; t = t.BaseType)
                foreach (MethodInfo m in t.GetMethods())
                    ConsiderMethod(m, mm);
        }


        static void GatherMethods(
            Type st,
            ISeq interfaces,
            out Dictionary<IPersistentVector, MethodInfo> overrides,
            out Dictionary<IPersistentVector, HashSet<Type>> covariants)
        {
            Dictionary<IPersistentVector, MethodInfo> allm = new Dictionary<IPersistentVector, MethodInfo>();
            GatherMethods(st, allm);
            for (; interfaces != null; interfaces = interfaces.next())
                GatherMethods((Type)interfaces.first(), allm);

            overrides = new Dictionary<IPersistentVector, MethodInfo>();
            covariants = new Dictionary<IPersistentVector, HashSet<Type>>();
            foreach (KeyValuePair<IPersistentVector, MethodInfo> kv in allm)
            {
                IPersistentVector mk = kv.Key;
                mk = (IPersistentVector)mk.pop();
                MethodInfo m = kv.Value;
                if (overrides.ContainsKey(mk)) // covariant return -- not a problem for CLR! but we are doing to have so many others.
                {
                    HashSet<Type> cvs = covariants[mk];
                    if (cvs == null)
                    {
                        cvs = new HashSet<Type>();
                        covariants[mk] = cvs;
                    }
                    MethodInfo om = overrides[mk];
                    if (om.ReturnType.IsAssignableFrom(m.ReturnType))
                    {
                        cvs.Add(om.ReturnType);
                        overrides[mk] = m;
                    }
                    else
                        cvs.Add(m.ReturnType);
                }
                else
                    overrides[mk] = m;
            }
        }

        static void ConsiderMethod(MethodInfo m, Dictionary<IPersistentVector, MethodInfo> mm)
        {
            IPersistentVector mk = MSig(m);
            if (!(mm.ContainsKey(mk)
                || !(m.IsPublic || m.IsFamily)
                || m.IsStatic
                || m.IsFinal))
                mm[mk] = m;
        }

        static IPersistentVector MSig(MethodInfo m)
        {
		    return RT.vector(m.Name, RT.seq(Compiler.GetTypes(m.GetParameters())),m.ReturnType);
        }

        #endregion

        */
    }
}
