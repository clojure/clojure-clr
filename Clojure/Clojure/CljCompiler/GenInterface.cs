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
using System.Reflection.Emit;
using System.Reflection;
using clojure.lang.CljCompiler.Ast;

namespace clojure.lang
{
    public static class GenInterface
    {
        #region Factory

        public static Type GenerateInterface(string iName, IPersistentMap attributes, ISeq extends, ISeq methods)
        {
            GenContext context;

            if (Compiler.IsCompiling)
            {
                //string path = (string)Compiler.COMPILE_PATH.deref();
                //if (path == null)
                //    throw new Exception("*compile-path* not set");
                //context = new GenContext(iName, ".dll", path, CompilerMode.File);
                context = (GenContext)Compiler.COMPILER_CONTEXT.deref();
            }
            else
                // TODO: In CLR4, should create a collectible type?
                //context = new GenContext(iName, ".dll", ".", CompilerMode.File);
                context = new GenContext(iName, ".dll", ".", AssemblyMode.Dynamic, FnMode.Full);

            Type[] interfaceTypes = GenClass.CreateTypeArray(extends == null ? null : extends.seq());

            TypeBuilder proxyTB = context.ModuleBuilder.DefineType(
                iName,
                TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract,
                null,
                interfaceTypes);

            SetCustomAttributes(proxyTB, attributes);

            DefineMethods(proxyTB, methods);

            Type t = proxyTB.CreateType();

            //if ( Compiler.IsCompiling )
            //    context.SaveAssembly();

            Compiler.RegisterDuplicateType(t);

            return t;
        }

        #endregion

        #region Defining methods


        // attributes = ( [ type value]... }
        // value = { :key value ... }
        // Special key :__args indicates positional arguments

        public static readonly Var EXTRACT_ATTRIBUTES = Var.intern(Namespace.findOrCreate(Symbol.create("clojure.core")),Symbol.create("extract-attributes"));

        public static IPersistentMap ExtractAttributes(IPersistentMap meta)
        {
            if (EXTRACT_ATTRIBUTES.isBound)
                return (IPersistentMap)EXTRACT_ATTRIBUTES.invoke(meta);

            return PersistentArrayMap.EMPTY;
        }


        public static void SetCustomAttributes(TypeBuilder tb, IPersistentMap attributes)
        {
            for (ISeq s = RT.seq(attributes); s != null; s = s.next())
                tb.SetCustomAttribute(CreateCustomAttributeBuilder((IMapEntry)(s.first())));
        }

        public static void SetCustomAttributes(FieldBuilder fb, IPersistentMap attributes)
        {
            for (ISeq s = RT.seq(attributes); s != null; s = s.next())
                fb.SetCustomAttribute(CreateCustomAttributeBuilder((IMapEntry)(s.first())));
        }

        public static void SetCustomAttributes(MethodBuilder mb, IPersistentMap attributes)
        {
            for (ISeq s = RT.seq(attributes); s != null; s = s.next())
                mb.SetCustomAttribute(CreateCustomAttributeBuilder((IMapEntry)(s.first())));
        }


        static readonly Keyword ARGS_KEY = Keyword.intern(null,"__args");


        private static CustomAttributeBuilder CreateCustomAttributeBuilder(IMapEntry me)
        {
            Type t = (Type) me.key();
            IPersistentMap args = (IPersistentMap)me.val();

            object[] ctorArgs = new object[0];
            Type[] ctorTypes = Type.EmptyTypes;

            List<PropertyInfo> pInfos = new List<PropertyInfo>();
            List<Object> pVals = new List<object>();
            List<FieldInfo> fInfos = new List<FieldInfo>();
            List<Object> fVals = new List<object>();

            for (ISeq s = RT.seq(args); s != null; s = s.next())
            {
                IMapEntry m2 = (IMapEntry)s.first();
                Keyword k = (Keyword) m2.key();
                object v = m2.val();
                if (k == ARGS_KEY)
                {
                    ctorArgs = GetCtorArgs((IPersistentVector)v);
                    ctorTypes = GetCtorTypes(ctorArgs);
                }
                else
                {
                    string name = k.Name;
                    PropertyInfo pInfo = t.GetProperty(name);
                    if (pInfo != null)
                    {
                        pInfos.Add(pInfo);
                        pVals.Add(v);
                        continue;
                    }

                    FieldInfo fInfo = t.GetField(name);
                    if (fInfo != null)
                    {
                        fInfos.Add(fInfo);
                        fVals.Add(v);
                        continue;
                    }
                    throw new ArgumentException(String.Format("Unknown field/property: {0} for attribute: {1}", k.Name, t.FullName));
                }
            }

            ConstructorInfo ctor = t.GetConstructor(ctorTypes);
            if (ctor == null)
                throw new ArgumentException(String.Format("Unable to find constructor for attribute: {0}", t.FullName));

            CustomAttributeBuilder cb = new CustomAttributeBuilder(ctor,ctorArgs,pInfos.ToArray(),pVals.ToArray(),fInfos.ToArray(),fVals.ToArray());

            return cb;
        }

        private static Type[] GetCtorTypes(object[] args)
        {
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
                types[i] = args[i].GetType();

            return types;
        }

        private static object[] GetCtorArgs(IPersistentVector v)
        {
            object[] args = new object[v.length()];
            for (int i = 0; i < v.length(); i++)
                args[i] = v.nth(i);

            return args;
        }


        private static void DefineMethods(TypeBuilder proxyTB, ISeq methods)
        {
            for (ISeq s = methods == null ? null : methods.seq(); s != null; s = s.next())
                DefineMethod(proxyTB, (IPersistentVector)s.first());
        }

        private static void DefineMethod(TypeBuilder proxyTB, IPersistentVector sig)
        {
            string mname = (string)sig.nth(0);
            Type[] paramTypes = GenClass.CreateTypeArray((ISeq)sig.nth(1));
            Type retType = (Type)sig.nth(2);

            MethodBuilder mb = proxyTB.DefineMethod(mname, MethodAttributes.Abstract | MethodAttributes.Public| MethodAttributes.Virtual, retType, paramTypes);
        }

        #endregion
    }
}
