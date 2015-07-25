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
using clojure.lang.CljCompiler.Ast;

namespace clojure.lang
{
    public static class GenInterface
    {
        #region Factory

        public static Type GenerateInterface(string iName, IPersistentMap attributes, Seqable extends, ISeq methods)
        {
            iName = iName.Replace('-', '_');

            GenContext context;

            if (Compiler.IsCompiling)
            {
                //string path = (string)Compiler.COMPILE_PATH.deref();
                //if (path == null)
                //    throw new Exception("*compile-path* not set");
                //context = new GenContext(iName, ".dll", path, CompilerMode.File);
                context = (GenContext)Compiler.CompilerContextVar.deref();
            }
            else
                // TODO: In CLR4, should create a collectible type?
                context = GenContext.CreateWithExternalAssembly(iName+"_"+RT.nextID(), ".dll", false);

            for (ISeq s = RT.seq(extends); s != null; s = s.next())
            {
                object f = s.first();
                string name = f as String ?? ((Named)f).getName();

                if (name.Contains("-"))
                    throw new ArgumentException("Interface methods must not contain '-'");
            }


            Type[] interfaceTypes = GenClass.CreateTypeArray(extends == null ? null : extends.seq());

            TypeBuilder proxyTB = context.ModuleBuilder.DefineType(
                iName,
                TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract,
                null,
                interfaceTypes);

            // Should we associate source file info?
            // See Java committ 8d6fdb, 2015.07.17, related to CLJ-1645
            // TODO: part of check on debug info

            SetCustomAttributes(proxyTB, attributes);

            DefineMethods(proxyTB, methods);

            Type t = proxyTB.CreateType();

            //if ( Compiler.IsCompiling )
            //    context.SaveAssembly();

            Compiler.RegisterDuplicateType(t);

            return t;
        }

        #endregion

        #region Fun with attributes

        // attributes = ( [ type inits]... }
        // inits = #{ init1 init2 ... }
        // init =  { :key value ... }
        // Special key :__args indicates positional arguments

        public static readonly Var ExtractAttributesVar = Var.intern(Namespace.findOrCreate(Symbol.intern("clojure.core")),Symbol.intern("extract-attributes"));

        public static IPersistentMap ExtractAttributes(IPersistentMap meta)
        {
            if (meta != null && ExtractAttributesVar.isBound)
                return (IPersistentMap)ExtractAttributesVar.invoke(meta);

            return PersistentArrayMap.EMPTY;
        }


        public static void SetCustomAttributes(TypeBuilder tb, IPersistentMap attributes)
        {
            foreach ( CustomAttributeBuilder cab in CreateCustomAttributeBuilders(attributes) )
                tb.SetCustomAttribute(cab);
        }

        public static void SetCustomAttributes(FieldBuilder fb, IPersistentMap attributes)
        {
            foreach (CustomAttributeBuilder cab in CreateCustomAttributeBuilders(attributes))
                fb.SetCustomAttribute(cab);
        }

        public static void SetCustomAttributes(MethodBuilder mb, IPersistentMap attributes)
        {
            foreach (CustomAttributeBuilder cab in CreateCustomAttributeBuilders(attributes))
                mb.SetCustomAttribute(cab);
        }

        public static void SetCustomAttributes(ParameterBuilder pb, IPersistentMap attributes)
        {
            foreach (CustomAttributeBuilder cab in CreateCustomAttributeBuilders(attributes))
                pb.SetCustomAttribute(cab);
        }

        public static void SetCustomAttributes(ConstructorBuilder cb, IPersistentMap attributes)
        {
            foreach (CustomAttributeBuilder cab in CreateCustomAttributeBuilders(attributes))
                cb.SetCustomAttribute(cab);
        }

        static readonly Keyword ARGS_KEY = Keyword.intern(null,"__args");


        private static List<CustomAttributeBuilder> CreateCustomAttributeBuilders(IPersistentMap attributes)
        {
            List<CustomAttributeBuilder> builders = new List<CustomAttributeBuilder>();
            for (ISeq s = RT.seq(attributes); s != null; s = s.next())
                builders.AddRange(CreateCustomAttributeBuilders((IMapEntry)s.first()));
            return builders;
        }


        private static List<CustomAttributeBuilder> CreateCustomAttributeBuilders(IMapEntry me)
        {
 
            Type t = (Type)me.key();
            IPersistentSet inits = (IPersistentSet)me.val();

            List<CustomAttributeBuilder> builders = new List<CustomAttributeBuilder>(inits.count());

            for (ISeq s = RT.seq(inits); s != null; s = s.next())
            {
                IPersistentMap init = (IPersistentMap)s.first();
                builders.Add(CreateCustomAttributeBuilder(t, init));
            }

            return builders;
        }

        private static CustomAttributeBuilder CreateCustomAttributeBuilder(Type t, IPersistentMap args)
        {
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
        
        
        #endregion

        #region Defining methods

        private static void DefineMethods(TypeBuilder proxyTB, ISeq methods)
        {
            for (ISeq s = methods == null ? null : methods.seq(); s != null; s = s.next())
                DefineMethod(proxyTB, (IPersistentVector)s.first());
        }

        private static void DefineMethod(TypeBuilder proxyTB, IPersistentVector sig)
        {
            Symbol mname = (Symbol)sig.nth(0);
            Type[] paramTypes = GenClass.CreateTypeArray((ISeq)sig.nth(1));
            Type retType = (Type)sig.nth(2);
            ISeq pmetas = (ISeq)(sig.count() >= 4 ? sig.nth(3) : null);

            MethodBuilder mb = proxyTB.DefineMethod(mname.Name, MethodAttributes.Abstract | MethodAttributes.Public| MethodAttributes.Virtual, retType, paramTypes);

            SetCustomAttributes(mb, GenInterface.ExtractAttributes(RT.meta(mname)));
            int i=1;
            for (ISeq s = pmetas; s != null; s = s.next(), i++)
            {
                IPersistentMap meta = GenInterface.ExtractAttributes((IPersistentMap)s.first());
                if (meta != null && meta.count() > 0)
                {
                    ParameterBuilder pb = mb.DefineParameter(i, ParameterAttributes.None, String.Format("p_{0}",i));
                    GenInterface.SetCustomAttributes(pb, meta);
                }
            }

        
        }

        #endregion
    }
}
