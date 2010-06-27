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

        Dictionary<IPersistentVector,List<MethodInfo>> _methodMap;

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

                string tagname = ((Symbol)rform.first()).ToString();
                rform = rform.next();
                Symbol classname = (Symbol)rform.first();
                rform = rform.next();
                IPersistentVector fields = (IPersistentVector)rform.first();
                rform = rform.next();
                IPersistentMap opts = PersistentHashMap.EMPTY;
                while (rform != null && rform.first() is Keyword)
                {
                    opts = opts.assoc(rform.first(), RT.second(rform));
                    rform = rform.next().next();
                }

                ObjExpr ret = Build((IPersistentVector)RT.get(opts, Compiler.IMPLEMENTS_KEY, PersistentVector.EMPTY), fields, null, tagname, classname,
                             (Symbol)RT.get(opts, RT.TAG_KEY), rform, frm);

                return ret;

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

                IPersistentVector interfaces = ((IPersistentVector)RT.first(rform)).cons(Symbol.intern("clojure.lang.IObj"));

                rform = RT.next(rform);

                //return Build(interfaces, null, null, className, className, null, rform);
                ObjExpr ret = Build(interfaces, null, null, className, Symbol.intern(className), null, rform,frm);
                if (frm is IObj && ((IObj)frm).meta() != null)
                    return new MetaExpr(ret, (MapExpr)MapExpr.Parse(((IObj)frm).meta()));
                else
                    return ret;
            }
        }

        internal static ObjExpr Build(
            IPersistentVector interfaceSyms, 
            IPersistentVector fieldSyms, 
            Symbol thisSym,
            string tagName, 
            Symbol className, 
            Symbol typeTag, 
            ISeq methodForms,
            Object frm)
        {
            NewInstanceExpr ret = new NewInstanceExpr(null);
            ret._src = frm;
            ret._name = className.ToString();
            ret._classMeta = GenInterface.ExtractAttributes(RT.meta(className));
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
                    LocalBinding lb = new LocalBinding(-1, sym, null, new MethodParamExpr(Compiler.TagType(Compiler.TagOf(sym))), false, false);
                    fmap = fmap.assoc(sym, lb);
                    closesvec[i * 2] = lb;
                    closesvec[i * 2 + 1] = lb;
                }
                // Java TODO: inject __meta et al into closes - when?
                // use array map to preserve ctor order
                ret._closes = new PersistentArrayMap(closesvec);
                ret._fields = fmap;
                for (int i = fieldSyms.count() - 1; i >= 0 && ((Symbol)fieldSyms.nth(i)).Name.StartsWith("__"); --i)
                    ret._altCtorDrops++;
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

            Dictionary<IPersistentVector, List<MethodInfo>> overrideables;
            GatherMethods(superClass, RT.seq(interfaces), out overrideables);

            ret._methodMap = overrideables;
            ret._interfaces = interfaces;

            //string[] inames = InterfaceNames(interfaces);

            Type stub = CompileStub(superClass, ret, SeqToTypeArray(interfaces),frm);
            Symbol thisTag = Symbol.intern(null, stub.FullName);
            //Symbol stubTag = Symbol.intern(null,stub.FullName);
            //Symbol thisTag = Symbol.intern(null, tagName);

            try
            {
                Var.pushThreadBindings(
                    RT.map(
                        Compiler.CONSTANTS, PersistentVector.EMPTY,
                        Compiler.CONSTANT_IDS, new IdentityHashMap(),
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
            Compiler.RegisterDuplicateType(ret.ObjType);

            //// THis is done in an earlier loop in the JVM code.
            //// We have to do it here so that we have ret._objType defined.

            //if (fieldSyms != null)
            //{
            //    for (int i = 0; i < fieldSyms.count(); i++)
            //    {
            //        Symbol sym = (Symbol)fieldSyms.nth(i);
            //        if (!sym.Name.StartsWith("__"))
            //            CompileLookupThunk(ret, sym);
            //    }
            //}

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
        static Type CompileStub(Type super, NewInstanceExpr ret, Type[] interfaces, Object frm)
        {

            //GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? Compiler.EvalContext;
            //GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? new GenContext("stub" + RT.nextID().ToString(), ".dll", ".", CompilerMode.Immediate);
            GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? new GenContext("stub" + RT.nextID().ToString(), ".dll", ".", AssemblyMode.Dynamic, FnMode.Full);
            TypeBuilder tb = context.ModuleBuilder.DefineType(Compiler.COMPILE_STUB_PREFIX + "." + ret.InternalName, TypeAttributes.Public | TypeAttributes.Abstract, super, interfaces);

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
            if (ret.CtorTypes().Length > 0)
            {
                ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, ret.CtorTypes());
                ILGen ilg = new ILGen(cb.GetILGenerator());
                ilg.EmitLoadArg(0);
                ilg.Emit(OpCodes.Call, super.GetConstructor(Type.EmptyTypes));
                ilg.Emit(OpCodes.Ret);


                if (ret._altCtorDrops > 0)
                {
                    Type[] ctorTypes = ret.CtorTypes();
                    int newLen = ctorTypes.Length - ret._altCtorDrops;
                    if (newLen > 0)
                    {
                        Type[] altCtorTypes = new Type[newLen];
                        for (int i = 0; i < altCtorTypes.Length; i++)
                            altCtorTypes[i] = ctorTypes[i];
                        ConstructorBuilder cb2 = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, altCtorTypes);
                        ILGen ilg2 = new ILGen(cb2.GetILGenerator());
                        ilg2.EmitLoadArg(0);
                        for (int i = 0; i < newLen; i++)
                            ilg2.EmitLoadArg(i + 1);
                        for (int i = 0; i < ret._altCtorDrops; i++)
                            ilg2.EmitNull();
                        ilg2.Emit(OpCodes.Call, cb);
                        ilg2.Emit(OpCodes.Ret);
                    }
                }
            }

            Type t = tb.CreateType();
            Compiler.RegisterDuplicateType(t);
            return t;
        }


        //private static Type CompileLookupThunk(NewInstanceExpr ret, Symbol fld)
        //{
        //    GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? Compiler.EvalContext;

        //    string className = ret.InternalName + "$__lookup__" + fld.Name;
        //    Type ftype = Compiler.TagType(Compiler.TagOf(fld));

        //    // Java: workaround until full support for type-hinted non-primitive fields
        //    if (!ftype.IsValueType)
        //        ftype = typeof(Object);

        //    TypeBuilder tb = context.ModuleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Sealed, typeof(object), new Type[] { typeof(ILookupThunk) });

        //    ConstructorBuilder cb = tb.DefineDefaultConstructor(MethodAttributes.Public);

        //    MethodBuilder mb = tb.DefineMethod("get", MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, typeof(Object), new Type[] { typeof(Object) });
        //    ILGen ilg = new ILGen(mb.GetILGenerator());
        //    Label faultLabel = ilg.DefineLabel();
        //    Label endLabel = ilg.DefineLabel();
        //    ilg.EmitLoadArg(0);
        //    ilg.Emit(OpCodes.Dup);
        //    ilg.Emit(OpCodes.Isinst, ret.ObjType);
        //    ilg.Emit(OpCodes.Brfalse_S, faultLabel);
        //    ilg.Emit(OpCodes.Castclass, ret.ObjType);
        //    ilg.EmitFieldGet(ret.ObjType, Compiler.munge(fld.Name));
        //    ilg.Emit(OpCodes.Br_S, endLabel);
        //    ilg.MarkLabel(faultLabel);
        //    ilg.Emit(OpCodes.Pop);
        //    ilg.EmitLoadArg(0);
        //    ilg.MarkLabel(endLabel);
        //    ilg.Emit(OpCodes.Ret);

        //    return tb.CreateType();
        //}




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

        static void GatherMethods(
            Type st,
            ISeq interfaces,
            out Dictionary<IPersistentVector, List<MethodInfo>> overrides)
        {
            Dictionary<IPersistentVector, List<MethodInfo>> allm = new Dictionary<IPersistentVector, List<MethodInfo>>();
            GatherMethods(st, allm);
            for (; interfaces != null; interfaces = interfaces.next())
                GatherMethods((Type)interfaces.first(), allm);

            overrides = allm;

            //overrides = new Dictionary<IPersistentVector, List<MethodInfo>>();
            //foreach (KeyValuePair<IPersistentVector, List<MethodInfo>> kv in allm)
            //{
            //    IPersistentVector mk = kv.Key;
            //    mk = (IPersistentVector)mk.pop();
            //    List<MethodInfo> ms = kv.Value;
            //    // TODO: explicit implementation of interfaces
            //    if (overrides.ContainsKey(mk)) 
            //    {
            //        HashSet<Type> cvs;
            //        if ( ! covariants.TryGetValue(mk,out cvs) )
            //        {
            //            cvs = new HashSet<Type>();
            //            covariants[mk] = cvs;
            //        }
            //        MethodInfo om = overrides[mk];
            //        if (om.ReturnType.IsAssignableFrom(m.ReturnType))
            //        {
            //            cvs.Add(om.ReturnType);
            //            overrides[mk] = m;
            //        }
            //        else
            //            cvs.Add(m.ReturnType);
            //    }
            //    else
            //        overrides[mk] = m;
            //}
        }

        static void GatherMethods(Type t, Dictionary<IPersistentVector, List<MethodInfo>> mm)
        {
            for (Type mt = t; mt != null; mt = mt.BaseType)
                foreach (MethodInfo m in mt.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    ConsiderMethod(m, mm);

            if (t.IsInterface)
                foreach (Type it in t.GetInterfaces())
                    GatherMethods(it, mm);

        }

        static void ConsiderMethod(MethodInfo m, Dictionary<IPersistentVector, List<MethodInfo>> mm)
        {
            IPersistentVector mk = MSig(m);
            if (!(mm.ContainsKey(mk)
                || !(m.IsPublic || m.IsFamily)
                || m.IsStatic
                || m.IsFinal))
                AddMethod(mm, mk, m);
        }

        public static IPersistentVector MSig(MethodInfo m)
        {
            return RT.vector(m.Name, RT.seq(Compiler.GetTypes(m.GetParameters())), m.ReturnType);
        }

        static void AddMethod(Dictionary<IPersistentVector, List<MethodInfo>> mm, IPersistentVector sig, MethodInfo m)
        {
            List<MethodInfo> value;
            if (!mm.TryGetValue(sig, out value))
            {
                value = new List<MethodInfo>();
                mm[sig] = value;
            }
            value.Add(m);
        }

        #endregion

        #region Class creation

        public override FnMode CompileMode()
        {
            return FnMode.Full;
        }

        protected override Type GenerateClassForImmediate(GenContext context)
        {
            return GenerateClassForFile(context);
        }

        protected override Type GenerateClassForFile(GenContext context)
        {
            //GenContext newC = context.ChangeMode(CompilerMode.File).WithNewDynInitHelper(InternalName + "__dynInitHelper_" + RT.nextID().ToString());
            GenContext newC = context.WithNewDynInitHelper(InternalName + "__dynInitHelper_" + RT.nextID().ToString());
            return EnsureTypeBuilt(newC);
        }

        #endregion

        #region Code generation


        protected override Expression GenDlrImmediate(GenContext context)
        {
            //GenContext newC = context.ChangeMode(CompilerMode.File);
            //Expression expr = GenDlrForFile(newC, false);
            Expression expr = GenDlrForFile(context, false);
            return expr;
        }

        protected override void GenerateMethods(GenContext context)
        {
            HashSet<MethodInfo> implemented = new HashSet<MethodInfo>();

            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                NewInstanceMethod method = (NewInstanceMethod)s.first();
                method.GenerateCode(context);
                implemented.UnionWith(method.MethodInfos);
            }

            foreach (List<MethodInfo> ms in _methodMap.Values)
                foreach (MethodInfo mi in ms)
                    if (NeedsDummy(mi,implemented))
                        GenerateDummyMethod(context,mi);
        }

        private bool NeedsDummy(MethodInfo mi, HashSet<MethodInfo> implemented)
        {
            return !implemented.Contains(mi) && mi.DeclaringType.IsInterface && !(!IsDefType && mi.DeclaringType == typeof(IObj) || mi.DeclaringType == typeof(IMeta));
        }

        private void GenerateDummyMethod(GenContext context, MethodInfo mi)
        {
            TypeBuilder tb = TypeBuilder;

            MethodBuilder mb = tb.DefineMethod(ExplicitMethodName(mi), MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, mi.ReturnType, Compiler.GetTypes(mi.GetParameters()));
            ILGen gen = new ILGen(mb.GetILGenerator());
            gen.EmitNew(typeof(NotImplementedException),Type.EmptyTypes);
            gen.Emit(OpCodes.Throw);
            tb.DefineMethodOverride(mb, mi);            
        }

        private string ExplicitMethodName(MethodInfo mi)
        {
            return mi.DeclaringType.Name + "." + mi.Name;
        }

        #endregion

    }
}
