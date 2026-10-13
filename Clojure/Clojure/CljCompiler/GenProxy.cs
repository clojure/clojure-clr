/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using clojure.lang.CljCompiler;
using clojure.lang.CljCompiler.Ast;
using clojure.lang.CljCompiler.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace clojure.lang
{
    public class GenProxy
    {

        #region Data

        //static GenContext _staticContext = new GenContext("proxy", CompilerMode.Immediate);
        const string _methodMapFieldName = "__clojureFnMap";
        static readonly MethodInfo Method_IPersistentMap_Cons = typeof(IPersistentMap).GetMethod("cons");
        static readonly MethodInfo Method_RT_get = typeof(RT).GetMethod("get", new Type[] { typeof(object), typeof(object) });
        static readonly ConstructorInfo CtorInfo_NotImplementedException_1 = typeof(NotImplementedException).GetConstructor(new Type[] { typeof(string) });
        readonly GenContext _context;

        #endregion

        #region A little debugging aid

        static int _saveId = 12;
        public void SaveProxyContext()
        {
            _context.SaveAssembly();
            //if ( _context == _staticContext) 
            //    _staticContext = new GenContext("proxy"+(++_saveId).ToString(), CompilerMode.Immediate);
        }


        #endregion

        #region C-tors

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
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
            return new GenProxy(className).Generate(superclass, interfaces, PersistentHashMap.EMPTY, className);
        }

        public static Type GenerateProxyClass(Type superclass, ISeq interfaces, IPersistentMap attributes, string className)
        {
            return new GenProxy(className).Generate(superclass, interfaces, attributes, className);
        }

        #endregion

        #region Implementation

        Type Generate(Type superclass, ISeq interfaces, IPersistentMap attributes, string className)
        {
            // can't proxy a sealed class
            if (superclass.IsSealed)
                throw new InvalidOperationException($"Cannot proxy a sealed class {superclass.FullName}");


            // define the class
            List<Type> interfaceTypes = new List<Type>
            {
                typeof(IProxy)
            };

            for (ISeq s = interfaces; s != null; s = s.next())
                interfaceTypes.Add((Type)s.first());

            TypeBuilder proxyTB = _context.ModuleBuilder.DefineType(
                className,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                superclass,
                interfaceTypes.ToArray());

            GenInterface.SetCustomAttributes(proxyTB, attributes);

            DefineCtors(proxyTB, superclass);
            MaybeImplementISerializable(proxyTB, interfaceTypes);
            FieldBuilder mapField = AddIProxyMethods(proxyTB);

            HashSet<Type> allInterfaces = GetAllInterfaces(interfaces);
            HashSet<MethodBuilder> specialMethods = new HashSet<MethodBuilder>();

            AddInterfaceMethods(proxyTB, mapField, superclass, allInterfaces, specialMethods);
            AddInterfaceProperties(proxyTB, superclass, allInterfaces, specialMethods); // Must follow AddInterfaceMethods

            Type t = proxyTB.CreateType();
            //if (Compiler.IsCompiling)
            //    SaveProxyContext();

            if (Compiler.IsCompiling)
                Compiler.RegisterDuplicateType(t);

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


        private static void MaybeImplementISerializable(TypeBuilder proxyTB, List<Type> interfaceTypes)
        {
            if (interfaceTypes.Find(t => t is ISerializable) == null)
                return;

            // Create failing implementation of GetObjectData
            MethodBuilder godMb = proxyTB.DefineMethod(
                "GetObjectData",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(void),
                new Type[] { typeof(SerializationInfo), typeof(StreamingContext) });
            CljILGen gen = new CljILGen(godMb.GetILGenerator());
            gen.EmitString("Serialization of proxy objects not supported");
            gen.EmitNew(typeof(SerializationException), new Type[] { typeof(string) });
            gen.Emit(OpCodes.Throw);
        }


        private static FieldBuilder AddIProxyMethods(TypeBuilder proxyTB)
        {
            FieldBuilder fb = proxyTB.DefineField(
                _methodMapFieldName,
                typeof(IPersistentMap),
                 FieldAttributes.Private);


            MethodBuilder initMb = proxyTB.DefineMethod(
                "__initClojureFnMappings",
                 MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
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


        // TODO: Are we handling generic methods properly?
        // TODO: Are we handling explicit-implementation interface methods properly?

        private static void AddInterfaceMethods(
            TypeBuilder proxyTB,
            FieldBuilder mapField,
            Type superclass,
            HashSet<Type> allInterfaces,
            HashSet<MethodBuilder> specialMethods)
        {
            HashSet<MethodSignature> considered = [];
            List<MethodInfo> implementedMethods = [];
            List<MethodInfo> unimplementedMethods = [];
            List<MethodInfo> interfaceMethods = [];

            MethodInfo[] minfos = superclass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);

            foreach (MethodInfo m in minfos)
            {
                MethodSignature sig = new MethodSignature(m);
                if (!considered.Contains(sig)
                    && !m.IsPrivate
                    && !m.IsAssembly
                    && !m.IsFamilyAndAssembly
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

            Dictionary<MethodSignature, MethodInfo> fromInterfaces = [];
            foreach (Type ifType in allInterfaces)
                foreach (MethodInfo m in ifType.GetMethods())
                {
                    MethodSignature sig = new MethodSignature(m);
                    if (!considered.Contains(sig))
                    {
                        unimplementedMethods.Add(m);
                        interfaceMethods.Add(m);
                        fromInterfaces[sig] = m;
                    }
                    else
                    {
                        if (fromInterfaces.TryGetValue(sig, out MethodInfo existingMethod))
                        {
                            if (existingMethod.ReturnType != m.ReturnType)
                            {
                                unimplementedMethods.Add(m); // If the return types differ, we need to implement this method in the proxy
                                interfaceMethods.Add(m);
                            }
                        }
                    }
                    considered.Add(sig);
                }

            HashSet<MethodInfo> explicitMethods = FindExplicitMethods(interfaceMethods.Select(m => new MethodSignature(m, "interface")).ToList());

            foreach (MethodInfo m in implementedMethods)
                GenerateProxyMethod(proxyTB, mapField, m, specialMethods, false);

            foreach (MethodInfo m in unimplementedMethods)
                GenerateProxyMethod(proxyTB, mapField, m, specialMethods, explicitMethods.Contains(m));
        }

        public static bool HasByRefLikeTypes(MethodInfo m)
        {
#if NETFRAMEWORK
            // .NET Framework doesn't have IsByRefLike, so we can't check for it.
            return false;
#else
            if (m.ReturnType.IsByRefLike)
                return true;
            foreach (ParameterInfo p in m.GetParameters())
            {
                Type typeToCheck = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
                if (typeToCheck.IsByRefLike)
                    return true;
            }
            return false;
#endif
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

        internal static string ExplicitName(MethodSignature sig) => ExplicitName(sig.Method);
        internal static string ExplicitName(MethodInfo mi) => $"{mi.DeclaringType.FullName}.{mi.Name}";

        internal static HashSet<MethodInfo> FindExplicitMethods(List<MethodSignature> sigs)
        {
            var sigGroups = sigs
                .Where(ms => ms.Source == "interface")
                .GroupBy(ms => ms)
                .Where(g => g.Count() > 1);

            HashSet<MethodInfo> explicitMethods = [];

            foreach (var group in sigGroups)
            {
                var explicits = FindExplicitMethodsInGroup(group);
                foreach (var ms in explicits)
                {
                    explicitMethods.Add(ms.Method);
                }
            }

            return explicitMethods;
        }

        // For each MethodSignature, compute how many other MethodSignaures have a return type that can be assigned from it.
        // Pick the one with highest count, breaking a tie arbitrarily.
        // Then return a list of all the MethodSignatures in the group that are not the one with highest count.
        internal static IEnumerable<MethodSignature> FindExplicitMethodsInGroup(IGrouping<MethodSignature, MethodSignature> group)
        {
            MethodSignature winner = null;
            int winnerCount = -1;

            foreach (var ms in group)
            {
                int count = group.Count(other => other != ms && other.ReturnType.IsAssignableFrom(ms.ReturnType));
                if (count > winnerCount)
                {
                    winner = ms;
                    winnerCount = count;
                }
            }

            return group.Where(ms => ms != winner);
        }


        private static void GenerateProxyMethod(
            TypeBuilder proxyTB,
            FieldBuilder mapField,
            MethodInfo m,
            HashSet<MethodBuilder> specialMethods,
            bool isExplicit)
        {
            MethodAttributes attribs = m.Attributes;

            bool callBaseMethod;

            if ((attribs & MethodAttributes.Abstract) == MethodAttributes.Abstract)
            {
                attribs &= ~MethodAttributes.Abstract;
                callBaseMethod = false;
            }
            else
            {
                callBaseMethod = true;

            }
            // Don't generate proxy methods for non-abstract methods with by-ref-like types, since we can't call them from Clojure code.
            if (HasByRefLikeTypes(m) && !m.IsAbstract)
                return;

            attribs &= ~MethodAttributes.NewSlot;
            attribs |= MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Public;

            if (isExplicit)
                attribs = MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot;

            //Console.Write("Generating proxy method {0}(", m.Name);
            //foreach (ParameterInfo p in m.GetParameters())
            //    Console.Write("{0}, ", p.ParameterType.FullName);
            //Console.Write(") ");
            //Console.WriteLine(attribs.ToString());

            MethodBuilder proxym = proxyTB.DefineMethod(
                (isExplicit ? ExplicitName(m) : m.Name),
                attribs,
                m.CallingConvention,
                m.ReturnType,
                m.GetParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray<Type>());

            if (isExplicit)
                proxyTB.DefineMethodOverride(proxym, m);

            // If we have a abstract method with by-ref-like types, we can't call it from Clojure code,
            //    generate a proxy method that throws NotImplementedException instead of trying to call the base method.
            // (non-abstract methods with by-ref-like types are handled above, and we don't generate proxy methods for them either)
            if (HasByRefLikeTypes(m))
            {
                CljILGen gen1 = new CljILGen(proxym.GetILGenerator());
                gen1.EmitString($"Cannot call method {m.Name} with by-ref-like types from Clojure code");
                gen1.EmitNew(typeof(NotImplementedException), [typeof(string)]);
                gen1.Emit(OpCodes.Throw);
                return;
            }

            // Maybe someday figure out how to do runtime async handling properly.
            // The current code fails for Task<T> when T != Object and for ValueTask and ValueTask<T>.

            //#if NET11_0_OR_GREATER
            //            bool isAsyncReturn = GenClass.IsAsyncReturnType(m.ReturnType);
            //            if (isAsyncReturn && Compiler.RuntimeAsyncAvailable)
            //            {
            //                proxym.SetImplementationFlags(
            //                    proxym.GetMethodImplementationFlags() | (MethodImplAttributes)MethodImplOptions.Async);
            //            }
            //#endif

            if (m.IsSpecialName)
                specialMethods.Add(proxym);

            CljILGen gen = new CljILGen(proxym.GetILGenerator());

            Label elseLabel = gen.DefineLabel();
            Label endLabel = gen.DefineLabel();
            Label foundLabel = gen.DefineLabel();

            //// Print a little message, for debugging purposes
            //gen.Emit(OpCodes.Ldstr, String.Format("Calling {0} / {1}", proxyTB.FullName, m.ToString()));
            //gen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine",
            //    new Type[] { typeof(string) }));
            //gen.Emit(OpCodes.Call, typeof(Console).GetMethod("get_Out"));
            //gen.Emit(OpCodes.Call, typeof(System.IO.TextWriter).GetMethod("Flush"));

            // Lookup explicit method name in map
            if (isExplicit)
            {
                gen.EmitLoadArg(0);
                gen.EmitFieldGet(mapField);
                gen.EmitString(ExplicitName(m));
                gen.EmitCall(Method_RT_get);
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Brtrue_S, foundLabel);
                gen.Emit(OpCodes.Pop);
            }

            // Lookup method name in map
            gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);
            gen.EmitFieldGet(mapField);                     // gen.Emit(OpCodes.Ldfld, mapField);
            gen.EmitString(m.Name);                         // gen.Emit(OpCodes.Ldstr, m.Name);
            gen.EmitCall(Method_RT_get);                    // gen.Emit(OpCodes.Call, Method_RT_get);
            gen.Emit(OpCodes.Dup);
            gen.EmitNull();                                 // gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Beq_S, elseLabel);

            gen.MarkLabel(foundLabel);

            // map entry found
            ParameterInfo[] pinfos = m.GetParameters();
            gen.Emit(OpCodes.Castclass, typeof(IFn));
            gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);  // push implicit 'this' arg.
            for (int i = 0; i < pinfos.Length; i++)
            {
                gen.EmitLoadArg(i + 1);                     // gen.Emit(OpCodes.Ldarg, i + 1);
                if (m.GetParameters()[i].ParameterType.IsValueType)
                    gen.Emit(OpCodes.Box, pinfos[i].ParameterType);
            }

            int parmCount = pinfos.Length;
            gen.EmitCall(GetIFnInvokeMethodInfo(parmCount + 1));        // gen.Emit(OpCodes.Call, GetIFnInvokeMethodInfo(parmCount + 1));

            // See comment above.
            //#if NET11_0_OR_GREATER
            //            if (isAsyncReturn && Compiler.RuntimeAsyncAvailable)
            //                GenClass.EmitAsyncReturnConversion(gen, m.ReturnType);
            //            else
            //#endif
            if (m.ReturnType == typeof(void))
                gen.Emit(OpCodes.Pop);
            else
                gen.Emit(OpCodes.Unbox_Any, m.ReturnType);

            gen.Emit(OpCodes.Br_S, endLabel);

            // map entry not found
            gen.MarkLabel(elseLabel);
            gen.Emit(OpCodes.Pop); // get rid of null leftover from the 'get'

            if (callBaseMethod)
            {
                gen.EmitLoadArg(0);                                     // gen.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < parmCount; i++)
                    gen.EmitLoadArg(i + 1);                             // gen.Emit(OpCodes.Ldarg, i + 1);
                gen.Emit(OpCodes.Call, m);                              // gen.EmitCall(m) improperly emits a callvirt in some cases

                // See comment above.
                //#if NET11_0_OR_GREATER
                //                // Base method may return Task<T> — unwrap it so the async runtime
                //                // doesn't double-wrap into Task<Task<T>>.
                //                if (isAsyncReturn && Compiler.RuntimeAsyncAvailable)
                //                    GenClass.EmitAsyncReturnConversion(gen, m.ReturnType);
                //#endif
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
            {
                try
                {
                    foreach (PropertyInfo p in ifType.GetProperties())
                    {
                        MethodSignature sig = new MethodSignature(p);
                        if (!considered.Contains(sig))
                            properties.Add(p);
                        considered.Add(sig);
                    }
                }
                catch (NotSupportedException)
                {
                    // do nothing
                    // Types in PersistedAssemblyBuilders throw on GetProperties
                }
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
                if (getter != null)
                    pb.SetGetMethod(getter);
                if (setter != null)
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
            Type[] array = new Type[numArgs + 1];
            for (int i = 0; i < numArgs; i++)
                array[i] = typeof(object);
            array[numArgs] = typeof(object[]);
            return array;
        }


        #endregion
    }
}
