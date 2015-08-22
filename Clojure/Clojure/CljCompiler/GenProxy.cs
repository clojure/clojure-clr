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
    public class GenProxy
    {

        #region Data

        //static GenContext _staticContext = new GenContext("proxy", CompilerMode.Immediate);
        const string _methodMapFieldName = "__clojureFnMap";
        static readonly MethodInfo Method_IPersistentMap_Cons = typeof(IPersistentMap).GetMethod("cons");
        static readonly MethodInfo Method_RT_get = typeof(RT).GetMethod("get",new Type[]{ typeof(object), typeof(object) });
        static readonly ConstructorInfo CtorInfo_NotImplementedException_1 = typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) });

        GenContext _context;

        #endregion

        #region A little debugging aid

        static int _saveId = 12;
        public  void SaveProxyContext()
        {
            _context.SaveAssembly();
            //if ( _context == _staticContext) 
            //    _staticContext = new GenContext("proxy"+(++_saveId).ToString(), CompilerMode.Immediate);
        }


        #endregion

        #region C-tors

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "className")]
        GenProxy(string className)
        {
            if (Compiler.IsCompiling)
            {
                //string path = (string)Compiler.COMPILE_PATH.deref();
                //if (path == null)
                //    throw new Exception("*compile-path* not set");

                ////string dir = (string)Compiler.SOURCE_PATH.deref();

                //_context = new GenContext(className, ".dll", path, CompilerMode.File);
                _context = (GenContext)Compiler.CompilerContextVar.deref();
            }
            else
                _context = GenContext.CreateWithInternalAssembly("proxy" + (++_saveId).ToString(), false);
        }


        #endregion

        #region Factory methods



        public static Type GenerateProxyClass(Type superclass, ISeq interfaces, string className)
        {
            return new GenProxy(className).Generate(superclass, interfaces,className);
        }

        #endregion

        #region Implementation

        Type Generate(Type superclass, ISeq interfaces, string className)
        {
            // define the class
            List<Type> interfaceTypes = new List<Type>();
            interfaceTypes.Add(typeof(IProxy));

            for (ISeq s = interfaces; s != null; s = s.next())
                interfaceTypes.Add((Type)s.first());

            TypeBuilder proxyTB = _context.ModuleBuilder.DefineType(
                className,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                superclass, 
                interfaceTypes.ToArray());
    
            DefineCtors(proxyTB, superclass);
            FieldBuilder mapField = AddIProxyMethods(proxyTB);

            HashSet<Type> allInterfaces = GetAllInterfaces(interfaces);
            HashSet<MethodBuilder> specialMethods = new HashSet<MethodBuilder>();

            AddInterfaceMethods(proxyTB, mapField, superclass, allInterfaces, specialMethods);
            AddInterfaceProperties(proxyTB, superclass, allInterfaces, specialMethods ); // Must follow AddInterfaceMethods

            Type t = proxyTB.CreateType();
            //if (Compiler.IsCompiling)
            //    SaveProxyContext();
            return t;
        }

        static void DefineCtors(TypeBuilder proxyTB, Type superclass)
        {
            // define a constructor with the same signature as each public superclass ctor
            ConstructorInfo[] ctors = superclass.GetConstructors();

            foreach (ConstructorInfo ctor in ctors)
                    DefineCtor(proxyTB, ctor);
        }

        private static void DefineCtor(TypeBuilder proxyTB, ConstructorInfo ctor)
        {
            ParameterInfo[] pinfos = ctor.GetParameters();
            Type[] paramTypes = new Type[pinfos.Length];
            for (int i = 0; i < pinfos.Length; i++)
                paramTypes[i] = pinfos[i].ParameterType;

            ConstructorBuilder cb = proxyTB.DefineConstructor(ctor.Attributes, CallingConventions.HasThis, paramTypes);
            // Call base class ctor on all the args
            CljILGen gen = new CljILGen(cb.GetILGenerator());
            gen.EmitLoadArg(0);                         // gen.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < pinfos.Length; i++)
                gen.EmitLoadArg(i + 1);                 // gen.Emit(OpCodes.Ldarg, i + 1);
            gen.Emit(OpCodes.Call, ctor);
            gen.Emit(OpCodes.Ret);
        }

        private static FieldBuilder AddIProxyMethods(TypeBuilder proxyTB)
        {
            FieldBuilder fb = proxyTB.DefineField(
                _methodMapFieldName,
                typeof(IPersistentMap),
                 FieldAttributes.Private);


            MethodBuilder initMb = proxyTB.DefineMethod(
                "__initClojureFnMappings",
                 MethodAttributes.Public|MethodAttributes.Virtual|MethodAttributes.HideBySig,
                 typeof(void),
                 new Type[] { typeof(IPersistentMap) });
            CljILGen gen = new CljILGen(initMb.GetILGenerator());
            gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);
            gen.EmitLoadArg(1);                     // gen.Emit(OpCodes.Ldarg_1);
            gen.EmitFieldSet(fb);                   // gen.Emit(OpCodes.Stfld, fb);
            gen.Emit(OpCodes.Ret);

            MethodBuilder updateTB = proxyTB.DefineMethod(
                "__updateClojureFnMappings",
                 MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                 typeof(void),
                 new Type[] { typeof(IPersistentMap) });
            gen = new CljILGen(updateTB.GetILGenerator());
            gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);

            gen.Emit(OpCodes.Dup);
            gen.EmitFieldGet(fb);                   // gen.Emit(OpCodes.Ldfld, fb);
            gen.Emit(OpCodes.Castclass, typeof(IPersistentMap));

            gen.EmitLoadArg(1);                                     // gen.Emit(OpCodes.Ldarg_1);
            gen.EmitCall(Method_IPersistentMap_Cons);        //gen.Emit(OpCodes.Call, Method_IPersistentCollection_Cons);

            gen.EmitFieldSet(fb);                                   // gen.Emit(OpCodes.Stfld, fb);
            gen.Emit(OpCodes.Ret);

            MethodBuilder getMb = proxyTB.DefineMethod(
                "__getClojureFnMappings",
                 MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(IPersistentMap),
                Type.EmptyTypes);

            gen = new CljILGen(getMb.GetILGenerator());
            gen.EmitLoadArg(0);                                     // gen.Emit(OpCodes.Ldarg_0);
            gen.EmitFieldGet(fb);                                   // gen.Emit(OpCodes.Ldfld, fb);
            gen.Emit(OpCodes.Ret);

            return fb;
        }


        // TODO: ARe we handling generic methods properly?
        // TODO: Are we handling explicit-implementation interface methods properly?

        private static void AddInterfaceMethods(
            TypeBuilder proxyTB, 
            FieldBuilder mapField, 
            Type superclass, 
            HashSet<Type> allInterfaces,
            HashSet<MethodBuilder> specialMethods)
        {
            HashSet<MethodSignature> considered = new HashSet<MethodSignature>();
            List<MethodInfo> implementedMethods = new List<MethodInfo>();
            List<MethodInfo> unimplementedMethods = new List<MethodInfo>();           

            MethodInfo[] minfos = superclass.GetMethods(BindingFlags.Public|BindingFlags.NonPublic| BindingFlags.Instance| BindingFlags.InvokeMethod);
            
            foreach (MethodInfo  m in minfos )
            {
                MethodSignature sig = new MethodSignature(m);
                if (!considered.Contains(sig)
                    && !m.IsPrivate
                    && !m.IsAssembly
                    && !m.IsStatic
                    && !m.IsFinal
                    && !m.Name.Equals("Finalize"))
                {
                    if (m.IsAbstract)
                        unimplementedMethods.Add(m);
                    else
                        implementedMethods.Add(m);
                }
                considered.Add(sig);
            }

            foreach ( Type ifType in allInterfaces )
                foreach (MethodInfo m in ifType.GetMethods() )
                {
                    MethodSignature sig = new MethodSignature(m);
                    if (!considered.Contains(sig))
                        unimplementedMethods.Add(m);
                    considered.Add(sig);
                }

            foreach (MethodInfo m in implementedMethods)
                GenerateProxyMethod(proxyTB, mapField, m, specialMethods);

            foreach (MethodInfo m in unimplementedMethods)
                GenerateProxyMethod(proxyTB, mapField, m, specialMethods);
        }

        private static HashSet<Type> GetAllInterfaces(ISeq interfaces)
        {
            HashSet<Type> allInterfaces = new HashSet<Type>();

            for (ISeq s = interfaces; s != null; s = s.next())
                GetAllInterfaces(allInterfaces, (Type)s.first());

            return allInterfaces;
                    
        }

        private static void GetAllInterfaces(HashSet<Type> allInterfaces, Type type)
        {
            allInterfaces.Add(type);
            foreach (Type ifType in type.GetInterfaces())
                GetAllInterfaces(allInterfaces, ifType);
        }

        private static void GenerateProxyMethod(
            TypeBuilder proxyTB, 
            FieldBuilder mapField, 
            MethodInfo m, 
            HashSet<MethodBuilder> specialMethods)
        {
            MethodAttributes attribs = m.Attributes;
            
            bool callBaseMethod;

            if ( (attribs & MethodAttributes.Abstract) == MethodAttributes.Abstract )
            {
                attribs &= ~MethodAttributes.Abstract;
                callBaseMethod = false;
            }
            else
            {
                callBaseMethod = true;

            }

            attribs &= ~MethodAttributes.NewSlot;
            attribs |= MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Public;

            //Console.Write("Generating proxy method {0}(", m.Name);
            //foreach (ParameterInfo p in m.GetParameters())
            //    Console.Write("{0}, ", p.ParameterType.FullName);
            //Console.Write(") ");
            //Console.WriteLine(attribs.ToString());

            MethodBuilder proxym = proxyTB.DefineMethod(
                m.Name,
                attribs,
                m.CallingConvention,
                m.ReturnType,
                m.GetParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray<Type>());

            if (m.IsSpecialName)
                specialMethods.Add(proxym);

            CljILGen gen = new CljILGen(proxym.GetILGenerator());
            
            Label elseLabel = gen.DefineLabel();
            Label endLabel = gen.DefineLabel();

            //// Print a little message, for debugging purposes
            //gen.Emit(OpCodes.Ldstr, String.Format("Calling {0} / {1}", proxyTB.FullName, m.ToString()));
            //gen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine",
            //    new Type[] { typeof(string) }));
            //gen.Emit(OpCodes.Call, typeof(Console).GetMethod("get_Out"));
            //gen.Emit(OpCodes.Call, typeof(System.IO.TextWriter).GetMethod("Flush"));

            // Lookup method name in map
            gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);
            gen.EmitFieldGet(mapField);                     // gen.Emit(OpCodes.Ldfld, mapField);
            gen.EmitString(m.Name);                         // gen.Emit(OpCodes.Ldstr, m.Name);
            gen.EmitCall(Method_RT_get);                    // gen.Emit(OpCodes.Call, Method_RT_get);
            gen.Emit(OpCodes.Dup);
            gen.EmitNull();                                 // gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Beq_S, elseLabel);

            // map entry found
            ParameterInfo[] pinfos = m.GetParameters();
            gen.Emit(OpCodes.Castclass, typeof(IFn));
            gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);  // push implicit 'this' arg.
            for (int i = 0; i < pinfos.Length; i++)
            {
                gen.EmitLoadArg(i + 1);                     // gen.Emit(OpCodes.Ldarg, i + 1);
                if (m.GetParameters()[i].ParameterType.IsValueType)
                    gen.Emit(OpCodes.Box,pinfos[i].ParameterType);
            }

            int parmCount = pinfos.Length;
            gen.EmitCall(GetIFnInvokeMethodInfo(parmCount+1));        // gen.Emit(OpCodes.Call, GetIFnInvokeMethodInfo(parmCount + 1));
            if (m.ReturnType == typeof(void))
                gen.Emit(OpCodes.Pop);
            else
                gen.Emit(OpCodes.Unbox_Any, m.ReturnType);

            gen.Emit(OpCodes.Br_S,endLabel);

            // map entry not found
            gen.MarkLabel(elseLabel);
            gen.Emit(OpCodes.Pop); // get rid of null leftover from the 'get'

            if ( callBaseMethod )
            {
                gen.EmitLoadArg(0);                                     // gen.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < parmCount; i++)
                    gen.EmitLoadArg(i + 1);                             // gen.Emit(OpCodes.Ldarg, i + 1);
                gen.Emit(OpCodes.Call, m);                              // gen.EmitCall(m) improperly emits a callvirt in some cases
            }
            else
            {
                gen.EmitString(m.Name);                                 // gen.Emit(OpCodes.Ldstr, m.Name);
                gen.EmitNew(CtorInfo_NotImplementedException_1);        // gen.Emit(OpCodes.Newobj, CtorInfo_NotImplementedException_1);
                gen.Emit(OpCodes.Throw);
            }

            gen.MarkLabel(endLabel);
            gen.Emit(OpCodes.Ret);

        }

        // TODO: Define an extension to proxy that allows overriding getters/setters on properties.

        private static void AddInterfaceProperties(
            TypeBuilder proxyTB,
            Type superclass,
            HashSet<Type> allInterfaces,
            HashSet<MethodBuilder> specialMethods)
        {
            HashSet<MethodSignature> considered = new HashSet<MethodSignature>();
            List<PropertyInfo> properties = new List<PropertyInfo>();

            PropertyInfo[] pinfos = superclass.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (PropertyInfo p in pinfos)
            {
                MethodSignature sig = new MethodSignature(p);
                if (!considered.Contains(sig))
                    properties.Add(p);
                considered.Add(sig);
            }

            foreach (Type ifType in allInterfaces)
                foreach (PropertyInfo p in ifType.GetProperties())
                {
                    MethodSignature sig = new MethodSignature(p);
                    if (!considered.Contains(sig))
                        properties.Add(p);
                    considered.Add(sig);
                }
            foreach (PropertyInfo pi in properties)
            {
                PropertyBuilder pb = proxyTB.DefineProperty(pi.Name,
                    pi.Attributes,
                    pi.PropertyType,
                    pi.GetIndexParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray<Type>());
                string getterName = "get_" + pi.Name;
                string setterName = "set_" + pi.Name;
                MethodBuilder getter = specialMethods.Where(m => m.Name.Equals(getterName)).FirstOrDefault();
                MethodBuilder setter = specialMethods.Where(m => m.Name.Equals(setterName)).FirstOrDefault();
                if ( getter != null )
                    pb.SetGetMethod(getter);
                if ( setter != null )
                    pb.SetSetMethod(setter);

                //Console.Write("Generating proxy property {0} ({1})", pi.Name, pi.PropertyType);
                //if (getter != null)
                //    Console.Write(", get = {0}", getter.Name);
                //if (setter != null)
                //    Console.Write(", set = {0}", setter.Name);
                //Console.WriteLine();

            }
        }

        static MethodInfo GetIFnInvokeMethodInfo(int numArgs)
        {
            if (numArgs <= 20)
            {
                Type[] parmTypes = GetObjectTypeArray(numArgs);
                return typeof(IFn).GetMethod("invoke", parmTypes);
            }
            else
                return typeof(IFn).GetMethod("invoke", GetObjectTypeArrayWithParam(20));
        }

        private static Type[] GetObjectTypeArray(int numArgs)
        {
            Type[] array = new Type[numArgs];
            for (int i = 0; i < numArgs; i++)
                array[i] = typeof(object);
            return array;
        }

        private static Type[] GetObjectTypeArrayWithParam(int numArgs)
        {
            Type[] array = new Type[numArgs+1];
            for (int i = 0; i < numArgs; i++)
                array[i] = typeof(object);
            array[numArgs] = typeof(object[]);
            return array;
        }


        #endregion
    }
}
