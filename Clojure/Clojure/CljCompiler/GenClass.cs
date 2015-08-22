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
using System.Reflection;
using System.Reflection.Emit;
using clojure.lang.CljCompiler;
using clojure.lang.CljCompiler.Ast;
using Microsoft.Scripting.Generation;

namespace clojure.lang
{  
    public static class GenClass
    { 
        #region Data

        const string _mainName = "main";
      
        // For debugging purposes only: testing with no compile in process
        static GenContext _context = GenContext.CreateWithInternalAssembly("genclass", false);

        static readonly MethodInfo Method_RT_nth = typeof(RT).GetMethod("nth", new Type[] { typeof(object), typeof(Int32) });
        static readonly MethodInfo Method_RT_seq = typeof(RT).GetMethod("seq");
        static readonly MethodInfo Method_RT_var2 = typeof(RT).GetMethod("var", new Type[] {typeof(String), typeof(String)});
        static readonly MethodInfo Method_IFn_applyTo_Object_ISeq = typeof(IFn).GetMethod("applyTo");
        static readonly MethodInfo Method_Var_internPrivate = typeof(Var).GetMethod("internPrivate");
        static readonly MethodInfo Method_Var_isBound = typeof(Var).GetMethod("get_isBound");
        static readonly MethodInfo Method_Var_get = typeof(Var).GetMethod("get");
        static readonly ConstructorInfo CtorInfo_NotImplementedException_1 = typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) });

        #endregion

        #region A little debugging aid

        //static int _saveId = 0;
        public static void SaveContext()
        {
            _context.SaveAssembly();
            _context = GenContext.CreateWithInternalAssembly("genclass", false);
        }
         

        #endregion

        #region Factory method

        public static Type GenerateClass(string className,
            Type superclass,
            ISeq interfaces,  // of Types 
            ISeq ctors, 
            ISeq ctorTypes,
            ISeq methods,
            IPersistentMap exposesFields,
            IPersistentMap exposesMethods,
            string prefix,
            bool hasMain,
            string factoryName,
            string stateName,
            string initName,
            string postInitName,
            string implCname,
            string implNamespace,
            bool loadImplNamespace,
            IPersistentMap attributes)
        {
            className = className.Replace('-', '_');

            string path = (string)Compiler.CompilePathVar.deref();
            if ( path == null)
                throw new InvalidOperationException("*compile-path* not set");

            string extension = hasMain ? ".exe" : ".dll";

            
            GenContext context = GenContext.CreateWithExternalAssembly(Compiler.munge(className), extension, true);

            // define the class
            List<Type> interfaceTypes = new List<Type>();

            for (ISeq s = interfaces; s != null; s = s.next())
                interfaceTypes.Add((Type)s.first());


            TypeBuilder proxyTB = context.ModuleBuilder.DefineType(
                className,
                TypeAttributes.Class | TypeAttributes.Public,
                superclass,
                interfaceTypes.ToArray());

            GenInterface.SetCustomAttributes(proxyTB, attributes);

            List<MethodSignature> sigs = GetAllSignatures(superclass,interfaceTypes,methods);
            Dictionary<string,List<MethodSignature>>  overloads = ComputeOverloads(sigs);

            HashSet<string> varNames = ComputeOverloadNames(overloads);  
            foreach ( MethodSignature sig in sigs )
                varNames.Add(sig.Name);

            if (!String.IsNullOrEmpty(initName)) varNames.Add(initName);
            if (!String.IsNullOrEmpty(postInitName)) varNames.Add(postInitName);
            if (hasMain) varNames.Add(_mainName);

            Dictionary<string, FieldBuilder> varMap = DefineStaticFields(proxyTB, varNames);

            FieldBuilder stateFB = String.IsNullOrEmpty(stateName) ? null : DefineStateField(proxyTB, stateName);
            DefineStaticCtor(proxyTB,prefix,varMap,loadImplNamespace,implNamespace,implCname);

            FieldBuilder initFB = null;
            FieldBuilder postInitFB = null;
            FieldBuilder mainFB = null;

            varMap.TryGetValue(initName, out initFB);
            varMap.TryGetValue(postInitName, out postInitFB);
            varMap.TryGetValue(_mainName, out mainFB);

            DefineCtors(proxyTB, superclass, 
                implNamespace + "." + prefix + initName, 
                implNamespace + "." + prefix + postInitName, 
                ctors, ctorTypes, initFB, postInitFB, stateFB, factoryName);

            EmitMethods(proxyTB, sigs, overloads, varMap, exposesMethods);
            EmitExposers(proxyTB, superclass, exposesFields);

            if (hasMain)
                EmitMain(context, proxyTB, implNamespace + "." + prefix + _mainName, mainFB);

            Type t = proxyTB.CreateType();

            context.SaveAssembly();

            return t;
        }

        #endregion

        #region Defining fields

        private static Dictionary<string, FieldBuilder> DefineStaticFields(TypeBuilder proxyTB, HashSet<String> varNames)
        {
            Dictionary<string, FieldBuilder> map = new Dictionary<string, FieldBuilder>();

            foreach ( string name in varNames )
            {
                FieldBuilder fb = proxyTB.DefineField(GetStaticVarName(name),
                    typeof(Var),
                    FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
                map.Add(name, fb);
            }

            return map;
        }

        private static FieldBuilder DefineStateField(TypeBuilder proxyTB, string stateName)
        {
            return proxyTB.DefineField(stateName,
                typeof(Object),
                FieldAttributes.Public| FieldAttributes.InitOnly);
        }

        #endregion

        #region Defining constructors

        /// <summary>
        ///  Set up Var fields and (maybe) load assembly for the namespace.
        /// </summary>
        /// <param name="proxyTB"></param>
        /// <param name="varMap"></param>
        /// <param name="loadImplNameSpace"></param>
        /// <param name="implNamespace"></param>
        private static void DefineStaticCtor(TypeBuilder proxyTB, string prefix, Dictionary<string, FieldBuilder> varMap, bool loadImplNameSpace, string implNamespace, string implCname)
        {
            ConstructorBuilder cb = proxyTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard,Type.EmptyTypes);
            CljILGen gen = new CljILGen(cb.GetILGenerator());

            foreach (KeyValuePair<string, FieldBuilder> pair in varMap)
            {
                gen.EmitString(implNamespace);                  // gen.Emit(OpCodes.Ldstr, implNamespace);
                gen.EmitString(prefix + pair.Key);              // gen.Emit(OpCodes.Ldstr, prefix + pair.Key);
                gen.EmitCall(Method_Var_internPrivate);         // gen.Emit(OpCodes.Call, Method_Var_internPrivate);
                gen.Emit(OpCodes.Stsfld, pair.Value);
            }

            if (loadImplNameSpace)
            {
                gen.EmitString("clojure.core");                 // gen.Emit(OpCodes.Ldstr, "clojure.core");
                gen.EmitString("load");                         // gen.Emit(OpCodes.Ldstr, "load");
                gen.EmitCall(Method_RT_var2);                   // gen.Emit(OpCodes.Call, Method_RT_var2);
                gen.EmitString("/" + implCname);                // gen.Emit(OpCodes.Ldstr, "/" + implCname);
                gen.EmitCall(Compiler.Methods_IFn_invoke[1]);   // gen.Emit(OpCodes.Call, Compiler.Methods_IFn_invoke[1]);
                gen.Emit(OpCodes.Pop);
            }
            gen.Emit(OpCodes.Ret);
        }

         
         
        static void DefineCtors(TypeBuilder proxyTB, 
            Type superClass, 
            string initName, 
            string postInitName, 
            ISeq ctors, 
            ISeq ctorTypes,
            FieldBuilder initFB, 
            FieldBuilder postInitFB, 
            FieldBuilder stateFB,
            string factoryName)
        {
            ISeq s1 = ctors;
            for (ISeq s = ctorTypes; s != null; s = s.next())
            {
                // TODO: Get rid of this mess by making sure the metadata on the keys of the constructors map gets copied to the constructor-types map.  Sigh.
                IPersistentMap ctorAttributes = GenInterface.ExtractAttributes(RT.meta(((IMapEntry)s1.first()).key()));
                s1 = s1.next(); 
                
                IMapEntry me = (IMapEntry)s.first();
                ISeq thisParamTypesV = (ISeq)me.key();
                ISeq baseParamTypesV = (ISeq)me.val();

                Type[] thisParamTypes = CreateTypeArray(thisParamTypesV);
                Type[] baseParamTypes = CreateTypeArray(baseParamTypesV);

                BindingFlags flags = BindingFlags.CreateInstance| BindingFlags.NonPublic| BindingFlags.Public| BindingFlags.Instance;
                ConstructorInfo superCtor = superClass.GetConstructor(flags,null,baseParamTypes,null);

                if (superCtor == null || superCtor.IsPrivate)
                    throw new InvalidOperationException("Base class constructor missing or private");

                ConstructorBuilder cb = proxyTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, thisParamTypes);
                GenInterface.SetCustomAttributes(cb, ctorAttributes);

                CljILGen gen = new CljILGen(cb.GetILGenerator());

                Label noInitLabel = gen.DefineLabel();
                Label noPostInitLabel = gen.DefineLabel();
                Label endPostInitLabel = gen.DefineLabel();
                Label endLabel = gen.DefineLabel();

                LocalBuilder locSuperArgs = gen.DeclareLocal(typeof(object));
                LocalBuilder locInitVal = gen.DeclareLocal(typeof(object));

                if (initFB != null)
                {
                    // init supplied
                    EmitGetVar(gen, initFB);
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Brfalse_S, noInitLabel);
                    gen.Emit(OpCodes.Castclass, typeof(IFn));

                    // box init args
                    for (int i = 0; i < thisParamTypes.Length; i++)
                    {
                        gen.EmitLoadArg(i + 1);                     // gen.Emit(OpCodes.Ldarg, i + 1);
                        if (thisParamTypes[i].IsValueType)
                            gen.Emit(OpCodes.Box,thisParamTypes[i]);
                    }

                    gen.EmitCall(Compiler.Methods_IFn_invoke[thisParamTypes.Length]);   // gen.Emit(OpCodes.Call, Compiler.Methods_IFn_invoke[thisParamTypes.Length]);

                    // Expecting:  [[super-ctor-args...] state]

                    // store the init return in a local
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Stloc,locInitVal);

                    // store the first element in a local
                    gen.EmitInt(0);                             // gen.Emit(OpCodes.Ldc_I4_0);
                    gen.EmitCall(Method_RT_nth);                // gen.Emit(OpCodes.Call, Method_RT_nth);
                    gen.Emit(OpCodes.Stloc, locSuperArgs);

                    // Stack this + super-ctor-args + call base-class ctor.
                    gen.EmitLoadArg(0);                         // gen.Emit(OpCodes.Ldarg_0);
                    for (int i = 0; i < baseParamTypes.Length; i++)
                    {
                        gen.Emit(OpCodes.Ldloc, locSuperArgs);
                        gen.EmitInt(i);                         // gen.Emit(OpCodes.Ldc_I4, i);
                        gen.EmitCall(Method_RT_nth);            // gen.Emit(OpCodes.Call, Method_RT_nth);
                        if (baseParamTypes[i].IsValueType)
                            gen.Emit(OpCodes.Unbox_Any, baseParamTypes[i]);
                        else
                            gen.Emit(OpCodes.Castclass, baseParamTypes[i]);
                    }

                    gen.Emit(OpCodes.Call, superCtor);

                    if (stateFB != null)
                    {
                        gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);
                        gen.Emit(OpCodes.Ldloc, locInitVal);
                        gen.EmitInt(1);                         // gen.Emit(OpCodes.Ldc_I4_1);
                        gen.EmitCall(Method_RT_nth);            // gen.Emit(OpCodes.Call, Method_RT_nth);
                        gen.Emit(OpCodes.Castclass, typeof(object));
                        gen.EmitFieldSet(stateFB);              // gen.Emit(OpCodes.Stfld, stateFB);
                    }

                    gen.Emit(OpCodes.Br_S, endLabel);

                    // No init found
                    gen.MarkLabel(noInitLabel);

                    gen.Emit(OpCodes.Pop);
                    EmitUnsupported(gen, initName);

                    gen.MarkLabel(endLabel);
                }
                else  // no InitFB supplied.
                {
                    bool ok = thisParamTypes.Length == baseParamTypes.Length;
                    for (int i = 0; ok && i < thisParamTypes.Length; i++)
                        ok = baseParamTypes[i].IsAssignableFrom(thisParamTypes[i]);
                    if (!ok)
                        throw new InvalidOperationException(":init not specified, but ctor and super ctor args differ");
                    gen.EmitLoadArg(0);                                 // gen.Emit(OpCodes.Ldarg_0);
                    for ( int i=0; i< thisParamTypes.Length; i++ )
                    {
                        gen.EmitLoadArg(i + 1);                         // gen.Emit(OpCodes.Ldarg, i + 1); 
                        if (baseParamTypes[i] != thisParamTypes[i])
                            gen.Emit(OpCodes.Castclass, baseParamTypes[i]);
                    }
                    gen.Emit(OpCodes.Call, superCtor);
                }

                if (postInitFB != null)
                {
                    // post-init supplied
                    EmitGetVar(gen, postInitFB);
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Brfalse_S, noPostInitLabel);
                    gen.Emit(OpCodes.Castclass, typeof(IFn));

                    // box init args
                    gen.EmitLoadArg(0);                                 // gen.Emit(OpCodes.Ldarg_0);
                    for (int i = 0; i < thisParamTypes.Length; i++)
                    {
                        gen.EmitLoadArg(i + 1);                         // gen.Emit(OpCodes.Ldarg, i + 1);
                        if (thisParamTypes[i].IsValueType)
                            gen.Emit(OpCodes.Box, thisParamTypes[i]);
                        gen.Emit(OpCodes.Castclass, thisParamTypes[i]);
                    }
                    gen.EmitCall(Compiler.Methods_IFn_invoke[thisParamTypes.Length + 1]);   // gen.Emit(OpCodes.Call, Compiler.Methods_IFn_invoke[thisParamTypes.Length + 1]);
                    gen.Emit(OpCodes.Pop);
                    gen.Emit(OpCodes.Br_S, endPostInitLabel);

                    // no post-init found

                    gen.MarkLabel(noPostInitLabel);

                    gen.Emit(OpCodes.Pop);
                    EmitUnsupported(gen,postInitName + " not defined");

                    gen.MarkLabel(endPostInitLabel);
               }

                gen.Emit(OpCodes.Ret);


                if (!String.IsNullOrEmpty(factoryName))
                {
                    MethodBuilder factoryMB = proxyTB.DefineMethod(factoryName, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, proxyTB, thisParamTypes);
                    CljILGen genf = new CljILGen(factoryMB.GetILGenerator());

                    LocalBuilder[] locals = new LocalBuilder[thisParamTypes.Length];
                    for (int i = 0; i < thisParamTypes.Length; i++)
                    {
                        locals[i] = genf.DeclareLocal(thisParamTypes[i]);
                        genf.EmitLoadArg(i);                    // genf.Emit(OpCodes.Ldarg, i);
                        genf.Emit(OpCodes.Stloc, locals[i]);
                    }

                    
                    for (int i = 0; i < thisParamTypes.Length; i++)
                        genf.EmitLoadArg(i);                    // genf.Emit(OpCodes.Ldarg, i);

                    genf.EmitNew(cb);                           // genf.Emit(OpCodes.Newobj, cb);
                    genf.Emit(OpCodes.Ret);
                }
            }
        }

        #endregion

        #region Defining methods

        static void EmitMain(GenContext context, TypeBuilder proxyTB, string mainName, FieldBuilder mainFB)
        {
            MethodBuilder cb = proxyTB.DefineMethod("Main",MethodAttributes.Public| MethodAttributes.Static,CallingConventions.Standard,typeof(void),new Type[] { typeof(String[]) });
            CljILGen gen = new CljILGen(cb.GetILGenerator()); ;

            Label noMainLabel = gen.DefineLabel();
            Label endLabel = gen.DefineLabel();

            EmitGetVar(gen, mainFB);
            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Brfalse_S, noMainLabel);
            gen.Emit(OpCodes.Castclass, typeof(IFn));
            gen.EmitLoadArg(0);                                 // gen.Emit(OpCodes.Ldarg_0);
            gen.EmitCall(Method_RT_seq);                        // gen.Emit(OpCodes.Call, Method_RT_seq);
            gen.EmitCall(Method_IFn_applyTo_Object_ISeq);       // gen.Emit(OpCodes.Call, Method_IFn_applyTo_Object_ISeq);
            gen.Emit(OpCodes.Pop);
            gen.Emit(OpCodes.Br_S, endLabel);

            // no main found
            gen.MarkLabel(noMainLabel);
            EmitUnsupported(gen, mainName);

            gen.MarkLabel(endLabel);
            gen.Emit(OpCodes.Ret);

            //context.AssyBldr.SetEntryPoint(cb);
            context.AssemblyBuilder.SetEntryPoint(cb);
        }

        private static void EmitMethods(TypeBuilder proxyTB, 
            List<MethodSignature> sigs, 
            Dictionary<string,List<MethodSignature>> overloads,
            Dictionary<string,FieldBuilder> varMap,
            IPersistentMap exposesMethods)
        {
            foreach (MethodSignature sig in sigs)
            {
                FieldBuilder regularFB = varMap[sig.Name];
                FieldBuilder overloadFB = null;
                if (overloads.ContainsKey(sig.Name))
                    overloadFB = varMap[OverloadName(sig)];

                switch (sig.Source)
                {
                    case "super":
                        EmitForwardingMethod(proxyTB, false, regularFB, overloadFB, sig,
                            delegate(CljILGen gen)
                            {
                                gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);
                                for (int i = 0; i < sig.ParamTypes.Length; i++)
                                    gen.EmitLoadArg(i + 1);                     // gen.Emit(OpCodes.Ldarg, (i + 1));
                                gen.Emit(OpCodes.Call, sig.Method);             // not gen.EmitCall(sig.Method) -- we need call versus callvirt
                            });
                        break;
                    case "interface":
                        EmitForwardingMethod(proxyTB, false, regularFB, overloadFB, sig,
                            delegate(CljILGen gen)
                            {
                                EmitUnsupported(gen, sig.Name);
                            });
                        break;
                    default:
                        EmitForwardingMethod(proxyTB, sig.IsStatic, regularFB, overloadFB, sig,
                            delegate(CljILGen gen)
                            {
                                EmitUnsupported(gen, sig.Name);
                            });
                        break;
                }
            }

            if (exposesMethods != null)
            {
                foreach (MethodSignature sig in sigs)
                {
                    if (sig.Source == "super")
                    {
                        Symbol name = Symbol.intern(sig.Name);
                        if (exposesMethods.containsKey(name))
                            CreateSuperCall(proxyTB, (Symbol)exposesMethods.valAt(name), sig.Method);
                    }
                }
            }
        }

        delegate void ElseGenDelegate(CljILGen gen);

 
        private static void EmitForwardingMethod(TypeBuilder proxyTB, 
            bool isStatic,
            FieldBuilder regularFB, 
            FieldBuilder overloadFB,  
            MethodSignature sig,
            ElseGenDelegate elseGen)
        {
            MethodAttributes attributes;
            CallingConventions conventions;

            if (isStatic)
            {
                attributes = MethodAttributes.Public | MethodAttributes.Static;
                conventions = CallingConventions.Standard;
            }
            else
            {
                attributes = MethodAttributes.Public | MethodAttributes.Virtual;
                conventions = CallingConventions.HasThis;
            }

            MethodBuilder mb = proxyTB.DefineMethod(sig.Name, attributes, conventions, sig.ReturnType, sig.ParamTypes);
            CljILGen gen = new CljILGen(mb.GetILGenerator());

            Label foundLabel = gen.DefineLabel();
            Label elseLabel = gen.DefineLabel();
            Label endLabel = gen.DefineLabel();

            if (sig.ParamTypes.Length > 18)
                elseGen(gen);
            else
            {

                if (overloadFB != null)
                {
                    EmitGetVar(gen, overloadFB);
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Brtrue_S, foundLabel);
                    gen.Emit(OpCodes.Pop);
                }
                EmitGetVar(gen, regularFB);
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Brfalse_S, elseLabel);

                if (overloadFB != null)
                    gen.MarkLabel(foundLabel);
                gen.Emit(OpCodes.Castclass, typeof(IFn));

                if (!isStatic)
                    gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);

                for (int i = 0; i < sig.ParamTypes.Length; i++)
                {
                    gen.EmitLoadArg(isStatic ? i : i + 1);                 // gen.Emit(OpCodes.Ldarg, i + 1);
                    if (sig.ParamTypes[i].IsValueType)
                        gen.Emit(OpCodes.Box, sig.ParamTypes[i]);

                }
                gen.EmitCall(Compiler.Methods_IFn_invoke[sig.ParamTypes.Length + (isStatic ? 0 : 1)]);
                //gen.Emit(OpCodes.Call, Compiler.Methods_IFn_invoke[sig.ParamTypes.Length + (isStatic ? 0 : 1)]);
                if (sig.ReturnType == typeof(void))
                    gen.Emit(OpCodes.Pop);
                else if (sig.ReturnType.IsValueType)
                    gen.Emit(OpCodes.Unbox_Any, sig.ReturnType);
                else
                    gen.Emit(OpCodes.Castclass, sig.ReturnType);
                gen.Emit(OpCodes.Br_S, endLabel);

                gen.MarkLabel(elseLabel);
                gen.Emit(OpCodes.Pop);
                elseGen(gen);

                gen.MarkLabel(endLabel);
                gen.Emit(OpCodes.Ret);
            }
        }

        private static void CreateSuperCall(TypeBuilder proxyTB, Symbol p, MethodInfo mi)
        {
            Type[] paramTypes = CreateTypeArray(mi.GetParameters());

            MethodBuilder mb = proxyTB.DefineMethod(p.Name, MethodAttributes.Public, CallingConventions.HasThis, mi.ReturnType, paramTypes);
            CljILGen gen = new CljILGen(mb.GetILGenerator());
            gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < paramTypes.Length; i++)
                gen.EmitLoadArg(i + 1);                     // gen.Emit(OpCodes.Ldarg, i + 1);
            gen.Emit(OpCodes.Call, mi);                     // not gen.EmitCall(mi); -- we need call versus callvirt
            gen.Emit(OpCodes.Ret);
        }

        static readonly Keyword _getKw = Keyword.intern("get");
        static readonly Keyword _setKW = Keyword.intern("set");

        private static void EmitExposers(TypeBuilder proxyTB, Type superClass, IPersistentMap exposesFields)
        {
            for ( ISeq s = RT.seq(exposesFields); s != null; s = s.next() )
            {
                IMapEntry me = (IMapEntry)s.first();
                Symbol protectedFieldSym = (Symbol)me.key();
                IPersistentMap accessMap = (IPersistentMap)me.val();

                string fieldName = protectedFieldSym.Name;
                Symbol getterSym = (Symbol)accessMap.valAt(_getKw, null);
                Symbol setterSym = (Symbol)accessMap.valAt(_setKW, null);

                FieldInfo fld = null;
                
                if ( getterSym != null || setterSym != null )
                    fld = superClass.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Instance);


                if (getterSym != null)
                {
                    MethodAttributes attribs = MethodAttributes.Public;
                    if (fld.IsStatic)
                        attribs |= MethodAttributes.Static;

                    MethodBuilder mb = proxyTB.DefineMethod(getterSym.Name, attribs, fld.FieldType, Type.EmptyTypes);
                    CljILGen gen = new CljILGen(mb.GetILGenerator());
                    //if (fld.IsStatic)
                    //    gen.Emit(OpCodes.Ldsfld, fld);
                    //else
                    //{
                    //    gen.Emit(OpCodes.Ldarg_0);
                    //    gen.Emit(OpCodes.Ldfld, fld);
                    //}
                    if (!fld.IsStatic)
                        gen.EmitLoadArg(0);
                    gen.MaybeEmitVolatileOp(fld);
                    gen.EmitFieldGet(fld);

                    gen.Emit(OpCodes.Ret);
                }

                if (setterSym != null)
                {
                    MethodAttributes attribs = MethodAttributes.Public;
                    if (fld.IsStatic)
                        attribs |= MethodAttributes.Static;

                    MethodBuilder mb = proxyTB.DefineMethod(setterSym.Name, attribs, typeof(void), new Type[] { fld.FieldType });
                    CljILGen gen = new CljILGen(mb.GetILGenerator());
                    if (fld.IsStatic)
                    {
                        gen.Emit(OpCodes.Ldarg_0);
                        //gen.Emit(OpCodes.Stsfld, fld);
                    }
                    else
                    {
                        gen.Emit(OpCodes.Ldarg_0);
                        gen.Emit(OpCodes.Ldarg_1);
                        //gen.Emit(OpCodes.Stfld, fld);
                    }
                    gen.MaybeEmitVolatileOp(fld);
                    gen.EmitFieldSet(fld);
                    gen.Emit(OpCodes.Ret);
                }
            }
        }


        #endregion

        #region Miscellaneous

        private static string GetStaticVarName(string var)
        {
            return Compiler.munge(var + "__var");
        }

        internal static Type[] CreateTypeArray(ISeq seq)
        {
            List<Type> types = new List<Type>();

            for (ISeq s = seq == null ? null : seq.seq(); s != null; s = s.next())
            {
                Object o = s.first();
                Type oAsType = o as Type;
                if (oAsType != null )
                    types.Add(oAsType);
                else if (o is ISeq)
                {
                    object first = RT.first(o);
                   Symbol firstAsSymbol = first as Symbol;
                    if (firstAsSymbol == null || !firstAsSymbol.Equals(HostExpr.ByRefSym))
                        throw new ArgumentException("First element of parameter definition is not by-ref");

                    Type secondAsType = RT.second(o) as Type;
 
                    if (secondAsType == null)
                        throw new ArgumentException("by-ref must be paired with a type");
                   
                    types.Add(secondAsType.MakeByRefType());
                }
                else
                    throw new ArgumentException("Bad parameter definition");
            }

            if ( types.Count ==  0 )
                return Type.EmptyTypes;

            return types.ToArray<Type>();
        }

        static Type[] CreateTypeArray(ParameterInfo[] ps)
        {
            Type[] paramTypes = new Type[ps.Length];
            for (int i = 0; i < ps.Length; i++)
                paramTypes[i] = ps[i].ParameterType;
            return paramTypes;
        }

        static void EmitGetVar(CljILGen gen, FieldBuilder fb)
        {
            Label falseLabel = gen.DefineLabel();
            Label endLabel = gen.DefineLabel();

            gen.EmitFieldGet(fb);                       // gen.Emit(OpCodes.Ldsfld,fb);
            gen.Emit(OpCodes.Dup);
            gen.EmitCall(Method_Var_isBound);           // gen.Emit(OpCodes.Call, Method_Var_IsBound);
            gen.Emit(OpCodes.Brfalse_S,falseLabel);
            gen.Emit(OpCodes.Call,Method_Var_get);
            gen.Emit(OpCodes.Br_S,endLabel);
            gen.MarkLabel(falseLabel);
            gen.Emit(OpCodes.Pop);
            gen.EmitNull();                             // gen.Emit(OpCodes.Ldnull);
            gen.MarkLabel(endLabel);
        }

        private static void EmitUnsupported(CljILGen gen, string name)
        {
            gen.EmitString(name);                               // gen.Emit(OpCodes.Ldstr, name);
            gen.EmitNew(CtorInfo_NotImplementedException_1);    // gen.Emit(OpCodes.Newobj, CtorInfo_NotImplementedException_1);
            gen.Emit(OpCodes.Throw);
        }



        //private static void GetMethodFields(string name, IPersistentMap overloads, Dictionary<string, FieldBuilder> varMap, out FieldBuilder overloadFB, out FieldBuilder regularFB)
        //{
        //    if ( overloads.containsKey(name) )
        //}

        static  List<MethodSignature> GetAllSignatures(Type superClass, List<Type> interfaces, ISeq methods)
        {
            HashSet<MethodSignature> considered = new HashSet<MethodSignature>();
            List<MethodSignature> todo = new List<MethodSignature>();

            GetAllMethods(superClass,considered,todo,"super");
            foreach( Type t in interfaces)
                GetAllMethods(t,considered,todo,"interface");

            for (ISeq s = methods; s != null; s = s.next())
            {
                IPersistentVector v = (IPersistentVector)s.first();
                // v == [name [paramTypes...] returnType]
                string name = ((Symbol)v.nth(0)).Name;
                Type[] paramTypes = CreateTypeArray((ISeq)v.nth(1));
                Type returnType = (Type)v.nth(2);
                bool isStatic = RT.booleanCast(v.nth(3));
                MethodSignature sig = new MethodSignature(name, paramTypes, returnType, isStatic, "other");
                if ( ! considered.Contains(sig) )
                    todo.Add(sig);
                considered.Add(sig);
            }
            return todo;
        }

        private static void GetAllMethods(Type type, HashSet<MethodSignature> considered, List<MethodSignature> todo, string source)
        {
            foreach (MethodInfo mi in type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                MethodSignature ms = new MethodSignature(mi, source);
                if (!considered.Contains(ms)
                    && (mi.IsPublic || mi.IsProtected())
                    && !mi.IsStatic
                    && !mi.IsFinal
                    && !"Dispose".Equals(mi.Name))
                    todo.Add(ms);
                considered.Add(ms);
            }
        }

        static Dictionary<string,List<MethodSignature>> ComputeOverloads(List<MethodSignature> sigs)
        {
            //HashSet<String> overloadNames = new HashSet<string>();
            Dictionary<string,List<MethodSignature>> name2SigMap = new Dictionary<string,List<MethodSignature>>();

            foreach (MethodSignature sig in sigs)
            {
                if (!name2SigMap.ContainsKey(sig.Name))
                    name2SigMap.Add(sig.Name, new List<MethodSignature>());
                name2SigMap[sig.Name].Add(sig);
            }

            List<String> okKeys = new List<string>();
            foreach (KeyValuePair<string, List<MethodSignature>> kv in name2SigMap)
                if (kv.Value.Count <= 1)
                    okKeys.Add(kv.Key);

            foreach (string okKey in okKeys)
                name2SigMap.Remove(okKey);

            return name2SigMap;
        }

        static HashSet<string> ComputeOverloadNames(Dictionary<string, List<MethodSignature>> overloads)
        {
            HashSet<string> varNames = new HashSet<string>();

            foreach (KeyValuePair<string, List<MethodSignature>> kv in overloads)
                foreach (MethodSignature sig in kv.Value)
                    varNames.Add(OverloadName(sig));

            return varNames;
        }


        static string OverloadName(MethodSignature sig)
        {
            if ( sig.ParamTypes.Length == 0 )
                return sig.Name + "-void";
            else 
            {
                string[] names = new string[sig.ParamTypes.Length+1];
                names[0] = sig.Name;
                for ( int i=0; i< sig.ParamTypes.Length; i++ ) 
                    names[i+1] = EscapeTypeName(sig.ParamTypes[i]);
                return String.Join("-",names);
            }
        }

        static string EscapeTypeName(Type t)
        {
            return t.Name;
        }
        

        #endregion

    }
}