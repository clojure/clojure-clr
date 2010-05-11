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
using System.Reflection.Emit;
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    sealed class NewInstanceExpr : ObjExpr
    {
        #region Data

        Dictionary<IPersistentVector,MethodInfo> _methodMap;
        Dictionary<IPersistentVector, HashSet<Type>> _covariants;

        #endregion

        #region C-tors

        public NewInstanceExpr(object tag)
            : base(tag)
        {
        }

        #endregion


        #region Type mangling


        #endregion

        #region Parsing
        
        
        public sealed class DefTypeParser : IParser
        {
            public Expr Parse(object frm, ParserContext pcon) 
            {

                // frm is: (deftype* tagname classname [fields] :implements [interfaces] :tag tagname methods*)

                ISeq rform = (ISeq)frm;
                rform = RT.next(rform);

                string tagname = ((Symbol) rform.first()).ToString();
			rform = rform.next();
			string classname = ((Symbol) rform.first()).ToString();
			rform = rform.next();
			IPersistentVector fields = (IPersistentVector) rform.first();
			rform = rform.next();
			IPersistentMap opts = PersistentHashMap.EMPTY;
			while(rform != null && rform.first() is Keyword)
				{
				opts = opts.assoc(rform.first(), RT.second(rform));
				rform = rform.next().next();
				}

			return Build((IPersistentVector)RT.get(opts,Compiler.IMPLEMENTS_KEY, PersistentVector.EMPTY),fields,null,tagname, classname,
			             (Symbol) RT.get(opts,RT.TAG_KEY),rform);

            
            
            }
        }


        public sealed class ReifyParser : IParser
        {
            public Expr Parse(object frm, ParserContext pcon)
            {
                // frm is:  (reify this-name? [interfaces] (method-name [args] body)* )
                ISeq form = (ISeq)frm;
                ObjMethod enclosingMethod = (ObjMethod)Compiler.METHOD.deref();
                string baseName = enclosingMethod != null
                    ? (ObjExpr.TrimGenID(enclosingMethod.Objx.Name) + "$")
                    : (Compiler.munge(Compiler.CurrentNamespace.Name.Name) + "$");
                string simpleName = "reify__" + RT.nextID();
                string className = baseName + simpleName;

                ISeq rform = RT.next(form);

                IPersistentVector interfaces = (IPersistentVector)RT.first(rform);

                rform = RT.next(rform);

                return Build(interfaces, null, null, className, className, null, rform);

            }
        }

        internal static ObjExpr Build(
            IPersistentVector interfaceSyms, 
            IPersistentVector fieldSyms, 
            Symbol thisSym,
            string tagName, 
            string className, 
            Symbol typeTag, 
            ISeq methodForms)
        {
            NewInstanceExpr ret = new NewInstanceExpr(null);
            ret._name = className;
            ret.InternalName = ret.Name;  // ret.Name.Replace('.', '/');
            ret.ObjType = null; 

            if (thisSym != null)
                ret._thisName = thisSym.Name;

            if (fieldSyms != null)
            {
                IPersistentMap fmap = PersistentHashMap.EMPTY;
                object[] closesvec = new object[2 * fieldSyms.count()];
                for (int i = 0; i < fieldSyms.count(); i++)
                {
                    Symbol sym = (Symbol)fieldSyms.nth(i);
                    LocalBinding lb = new LocalBinding(-1, sym, null, new MethodParamExpr(Compiler.TagType(Compiler.TagOf(sym))), false);
                    fmap = fmap.assoc(sym, lb);
                    closesvec[i * 2] = lb;
                    closesvec[i * 2 + 1] = lb;
                }
                // Java TODO: inject __meta et al into closes - when?
                // use array map to preserve ctor order
                ret._closes = new PersistentArrayMap(closesvec);
                ret._fields = fmap;
                for (int i = fieldSyms.count() - 1; i >= 0 && ((Symbol)fieldSyms.nth(i)).Name.StartsWith("__"); --i)
                    ret.altCtorDrops++;
            }

            // Java TODO: set up volatiles
            //ret._volatiles = PersistentHashSet.create(RT.seq(RT.get(ret._optionsMap, volatileKey)));

            IPersistentVector interfaces = PersistentVector.EMPTY;
            for (ISeq s = RT.seq(interfaceSyms); s != null; s = s.next())
            {
                Type t = (Type)Compiler.Resolve((Symbol)s.first());
                if (!t.IsInterface)
                    throw new ArgumentException("only interfaces are supported, had: " + t.Name);
                interfaces = interfaces.cons(t);
            }
            Type superClass = typeof(Object);

            Dictionary<IPersistentVector, MethodInfo> overrideables;
            Dictionary<IPersistentVector, HashSet<Type>> covariants;
            GatherMethods(superClass, RT.seq(interfaces), out overrideables, out covariants);

            ret._methodMap = overrideables;
            ret._covariants = covariants;
            ret._interfaces = interfaces;

            //string[] inames = InterfaceNames(interfaces);

            Type stub = CompileStub(superClass, ret, SeqToTypeArray(interfaces));
            Symbol thisTag = Symbol.intern(null, stub.FullName);
            //Symbol stubTag = Symbol.intern(null,stub.FullName);
            //Symbol thisTag = Symbol.intern(null, tagName);

            try
            {
                Var.pushThreadBindings(
                    RT.map(
                        Compiler.CONSTANTS, PersistentVector.EMPTY,
                        Compiler.KEYWORDS, PersistentHashMap.EMPTY,
                        Compiler.VARS, PersistentHashMap.EMPTY,
                        Compiler.KEYWORD_CALLSITES, PersistentVector.EMPTY,
                        Compiler.PROTOCOL_CALLSITES, PersistentVector.EMPTY,
                        Compiler.VAR_CALLSITES, PersistentVector.EMPTY
                        ));
                if (ret.IsDefType)
                {
                    Var.pushThreadBindings(
                        RT.map(
                            Compiler.METHOD, null,
                            Compiler.LOCAL_ENV, ret._fields,
                            Compiler.COMPILE_STUB_SYM, Symbol.intern(null, tagName),
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
                ret._constants = (PersistentVector)Compiler.CONSTANTS.deref();
                ret._constantsID = RT.nextID();
                ret._keywordCallsites = (IPersistentVector)Compiler.KEYWORD_CALLSITES.deref();
                ret._protocolCallsites = (IPersistentVector)Compiler.PROTOCOL_CALLSITES.deref();
                ret._varCallsites = (IPersistentVector)Compiler.VAR_CALLSITES.deref();
            }
            finally
            {
                if (ret.IsDefType)
                    Var.popThreadBindings();
                Var.popThreadBindings();
            }

            // TODO: This is silly.  We have the superClass in hand.  Might as well stash it.
            //ret._superName = SlashName(superClass);
            //ret._superType = superClass;
            ret._superType = stub;
            // asdf: IF this works, I'll be totally amazed.

            //ret.Compile(SlashName(superClass),inames,false);
            //ret.getCompiledClass();
            ret.ObjType = ret.GenerateClass();

            // THis is done in an earlier loop in the JVM code.
            // We have to do it here so that we have ret._objType defined.

            for (int i = 0; i < fieldSyms.count(); i++)
            {
                Symbol sym = (Symbol)fieldSyms.nth(i);
                if (!sym.Name.StartsWith("__"))
                    CompileLookupThunk(ret, sym);
            }

            return ret;
        }

        private static Type[] SeqToTypeArray(IPersistentVector interfaces)
        {
            Type[] types = new Type[interfaces.count()];
            for (int i = 0; i < interfaces.count(); i++)
                types[i] = (Type)interfaces.nth(i);

            return types;
        }

        /***
 * Current host interop uses reflection, which requires pre-existing classes
 * Work around this by:
 * Generate a stub class that has the same interfaces and fields as the class we are generating.
 * Use it as a type hint for this, and bind the simple name of the class to this stub (in resolve etc)
 * Unmunge the name (using a magic prefix) on any code gen for classes
 */
        static Type CompileStub(Type super, NewInstanceExpr ret, Type[] interfaces)
        {

            GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? Compiler.EvalContext;

            TypeBuilder tb = context.ModuleBuilder.DefineType(Compiler.COMPILE_STUB_PREFIX + "." + ret.InternalName, TypeAttributes.Public|TypeAttributes.Abstract, super, interfaces);

            tb.DefineDefaultConstructor(MethodAttributes.Public);

            // instance fields for closed-overs
            for (ISeq s = RT.keys(ret.Closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                FieldAttributes access = FieldAttributes.Public;

                // TODO: FIgure out Volatile
                if (!ret.IsVolatile(lb))
                    access |= FieldAttributes.InitOnly;

                if (lb.PrimitiveType != null)
                    tb.DefineField(lb.Name, lb.PrimitiveType, access);
                else
                    tb.DefineField(lb.Name, typeof(Object), access);
            }

            // ctor that takes closed-overs and does nothing
            ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, ret.CtorTypes());
            ILGen ilg = new ILGen(cb.GetILGenerator());
            ilg.EmitLoadArg(0);
            ilg.Emit(OpCodes.Call,super.GetConstructor(Type.EmptyTypes));
            ilg.Emit(OpCodes.Ret);

            if (ret.altCtorDrops > 0)
            {
                Type[] ctorTypes = ret.CtorTypes();
                int newLen = ctorTypes.Length - ret.altCtorDrops;
                Type[] altCtorTypes = new Type[newLen];
                for (int i = 0; i < altCtorTypes.Length; i++)
                    altCtorTypes[i] = ctorTypes[i];
                ConstructorBuilder cb2 = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, altCtorTypes);
                ILGen ilg2 = new ILGen(cb2.GetILGenerator());
                ilg2.EmitLoadArg(0);
                for (int i = 0; i < newLen; i++)
                    ilg2.EmitLoadArg(i + 1);
                for (int i = 0; i < ret.altCtorDrops; i++)
                    ilg2.EmitNull();
                ilg2.Emit(OpCodes.Call, cb);
                ilg2.Emit(OpCodes.Ret);
            }

            return tb.CreateType();
        }


        private static Type CompileLookupThunk(NewInstanceExpr ret, Symbol fld)
        {
            GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? Compiler.EvalContext;

            string className = ret.InternalName + "$__lookup__" + fld.Name;
            Type ftype = Compiler.TagType(Compiler.TagOf(fld));

            // Java: workaround until full support for type-hinted non-primitive fields
            if (!ftype.IsValueType)
                ftype = typeof(Object);

            TypeBuilder tb = context.ModuleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Sealed, typeof(object), new Type[] { typeof(ILookupThunk) });

            ConstructorBuilder cb = tb.DefineDefaultConstructor(MethodAttributes.Public);

            MethodBuilder mb = tb.DefineMethod("get", MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, typeof(Object), new Type[] { typeof(Object) });
            ILGen ilg = new ILGen(mb.GetILGenerator());
            Label faultLabel = ilg.DefineLabel();
            Label endLabel = ilg.DefineLabel();
            ilg.EmitLoadArg(0);
            ilg.Emit(OpCodes.Dup);
            ilg.Emit(OpCodes.Isinst, ret.ObjType);
            ilg.Emit(OpCodes.Brfalse_S, faultLabel);
            ilg.Emit(OpCodes.Castclass, ret.ObjType);
            ilg.EmitFieldGet(ret.ObjType, Compiler.munge(fld.Name));
            ilg.Emit(OpCodes.Br_S, endLabel);
            ilg.MarkLabel(faultLabel);
            ilg.Emit(OpCodes.Pop);
            ilg.EmitLoadArg(0);
            ilg.MarkLabel(endLabel);
            ilg.Emit(OpCodes.Ret);

            return tb.CreateType();
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

        #region Method reflection

        static void GatherMethods(Type t, Dictionary<IPersistentVector, MethodInfo> mm)
        {
            for (Type mt = t; mt != null; mt = mt.BaseType)
                foreach (MethodInfo m in mt.GetMethods(BindingFlags.FlattenHierarchy| BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                    ConsiderMethod(m, mm);

            if (t.IsInterface)
                foreach (Type it in t.GetInterfaces())
                    GatherMethods(it, mm);

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
            return RT.vector(m.Name, RT.seq(Compiler.GetTypes(m.GetParameters())), m.ReturnType);
        }

        #endregion


        #region Class creation

        protected override Type GenerateClassForImmediate(GenContext context)
        {
            return GenerateClassForFile(context);
        }

        protected override Type GenerateClassForFile(GenContext context)
        {
            GenContext newC = context.ChangeMode(CompilerMode.File).WithNewDynInitHelper(InternalName + "__dynInitHelper_" + RT.nextID().ToString());
            return EnsureTypeBuilt(newC);
        }

        #endregion

        #region Code generation


        protected override Expression GenDlrImmediate(GenContext context)
        {
            GenContext newC = context.ChangeMode(CompilerMode.File);
            //newC = CreateContext(newC, null, null);

            Expression expr =  GenDlrForFile(newC,false);
            return expr;
        }

        protected override void GenerateMethods(GenContext context)
        {
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                ObjMethod method = (ObjMethod)s.first();
                method.GenerateCode(context);
            }
        }

        #endregion

    }
}
