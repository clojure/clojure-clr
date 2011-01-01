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

#if CLR2
extern alias MSC;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Actions;
using System.Dynamic;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using clojure.lang.CljCompiler.Ast;

namespace clojure.lang
{
    public static class Reflector
    {
        #region Field/property lookup

        static public FieldInfo GetField(Type t, String name, bool getStatics)
        {
            BindingFlags flags = BindingFlags.Public;
            if (getStatics)
                flags |= BindingFlags.Static | BindingFlags.FlattenHierarchy;
            else
                flags |= BindingFlags.Instance;

            return t.GetField(name,flags);
        }

        static public PropertyInfo GetProperty(Type t, String name, bool getStatics)
        {
            BindingFlags flags = BindingFlags.Public;
            if (getStatics)
                flags |= BindingFlags.Static | BindingFlags.FlattenHierarchy;
            else
                flags |= BindingFlags.Instance;

            List<PropertyInfo> pinfos = new List<PropertyInfo>(t.GetProperties(flags).Where(pi => pi.Name == name && pi.GetIndexParameters().Length == 0));

            if (pinfos.Count == 0)
                return null;

            if (pinfos.Count == 1)
                return pinfos[0];

            // Look for the one declared on this type, if it exists
            // This handles the situation where we have overloads.
            foreach (PropertyInfo pinfo in pinfos)
                if (pinfo.DeclaringType == t)
                    return pinfo;

            return null;
        }

        #endregion

        #region Field/property access

        // Used in generated code
        public static object SetInstanceFieldOrProperty(object target, string fieldname, object val)
        {
            Type t = target.GetType();
            FieldInfo field = GetField(t, fieldname, false);
            if (field != null)
            {
                field.SetValue(target, val);
                return val;
            }
            PropertyInfo prop = GetProperty(t, fieldname, false);
            if (prop != null)
            {
                prop.SetValue(target, val, new object[0]);
                return val;
            }
            throw new ArgumentException(String.Format("No matching field/property found: {0} for {1}", fieldname, t));
        }

        // used in generated code
        public static object GetInstanceFieldOrProperty(object target, string fieldname)
        {
            Type t = target.GetType();

            FieldInfo field = GetField(t, fieldname, false);
            if (field != null)
                return Reflector.prepRet(field.FieldType,field.GetValue(target));

            PropertyInfo prop = GetProperty(t, fieldname, false);
            if (prop != null)
                return Reflector.prepRet(prop.PropertyType,prop.GetValue(target, new object[0]));

            MethodInfo method = GetArityZeroMethod(t, fieldname, false);

            if (method != null)
                return Reflector.prepRet(method.ReturnType, method.Invoke(target, new object[0]));

            throw new ArgumentException(String.Format("No matching instance field/property found: {0} for {1}", fieldname, t));
        }

        // Not used at the moment.
        //public static object GetStaticFieldOrProperty(Type t, string fieldname)
        //{
        //    FieldInfo field = GetField(t, fieldname, true);
        //    if (field != null)
        //        return Reflector.prepRet(field.FieldType,field.GetValue(null));

        //    PropertyInfo prop = GetProperty(t, fieldname, true);
        //    if (prop != null)
        //        return Reflector.prepRet(prop.PropertyType,prop.GetValue(null, new object[0]));

        //    MethodInfo method = GetArityZeroMethod(t, fieldname, true);

        //    if (method != null)
        //        return Reflector.prepRet(method.ReturnType,method.Invoke(null, new object[0]));

        //    throw new ArgumentException(String.Format("No matching static field/property found: {0} for {1}", fieldname, t));
        //}

        #endregion

        #region Method lookup

        // old version, before generics, ByRef params, etc.
        //public static List<MethodInfo> GetMethods(Type t, string name, int arity, bool getStatics)
        //{
        //    BindingFlags flags = BindingFlags.Public | BindingFlags.InvokeMethod;
        //    if (getStatics)
        //        flags |= BindingFlags.Static | BindingFlags.FlattenHierarchy;
        //    else
        //        flags |= BindingFlags.Instance;

        //    IEnumerable<MethodInfo> einfo = t.GetMethods(flags).Where(mi => mi.Name == name && mi.GetParameters().Length == arity);
        //    List<MethodInfo> infos = new List<MethodInfo>(einfo);

        //    return infos;
        //}


        public static MethodInfo GetMatchingMethod(IPersistentMap spanMap, Type targetType, List<HostArg> args, string methodName, List<Type> typeArgs)
        {
            List<MethodBase> methods = GetMethods(targetType, methodName, typeArgs, args.Count, true);

            MethodBase method = GetMatchingMethodAux(targetType, args, methods, methodName, true);
            MaybeReflectionWarn(spanMap, method, methodName);
            return (MethodInfo)method;
        }

        public static MethodInfo GetMatchingMethod(IPersistentMap spanMap, Expr target, List<HostArg> args, string methodName, List<Type> typeArgs)
        {
            MethodBase method = null;
            if (target.HasClrType)
            {
                Type targetType = target.ClrType;
                List<MethodBase> methods = GetMethods(targetType, methodName, typeArgs, args.Count, false);
                method = GetMatchingMethodAux(targetType, args, methods, methodName, false);
            }

            MaybeReflectionWarn(spanMap, method, methodName);
            return (MethodInfo)method;
        }

        internal static List<MethodBase> GetMethods(Type targetType, string methodName, List<Type> typeArgs, int arity, bool getStatics)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;
            flags |= getStatics ? BindingFlags.Static : BindingFlags.Instance;

            List<MethodBase> infos;

            if (targetType.IsInterface && !getStatics)
                infos = GetInterfaceMethods(targetType, methodName, typeArgs, arity);
            else
            {
                IEnumerable<MethodInfo> einfos
                    = targetType.GetMethods(flags).Where(info => info.Name == methodName && info.GetParameters().Length == arity);
                infos = new List<MethodBase>();
                foreach (MethodInfo minfo in einfos)
                    if (typeArgs == null && !minfo.ContainsGenericParameters)
                        infos.Add(minfo);
                    else if (typeArgs != null && minfo.ContainsGenericParameters)
                        infos.Add(minfo.MakeGenericMethod(typeArgs.ToArray<Type>()));
            }

            return infos;
        }


        private static List<MethodBase> GetInterfaceMethods(Type targetType, string methodName, List<Type> typeArgs, int arity)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

            List<Type> interfaces = new List<Type>();
            interfaces.Add(targetType);
            interfaces.AddRange(targetType.GetInterfaces());

            List<MethodBase> infos = new List<MethodBase>();

            foreach (Type type in interfaces)
            {
                MethodInfo[] methods = type.GetMethods();
                IEnumerable<MethodInfo> einfo
                     = type.GetMethods(flags).Where(info => info.Name == methodName && info.GetParameters().Length == arity);
                foreach (MethodInfo minfo in einfo)
                    if (typeArgs == null && !minfo.ContainsGenericParameters)
                        infos.Add(minfo);
                    else if (typeArgs != null && minfo.ContainsGenericParameters)
                        infos.Add(minfo.MakeGenericMethod(typeArgs.ToArray<Type>()));
            }

            return infos;
        }



        internal static ConstructorInfo GetMatchingConstructor(IPersistentMap spanMap, Type targetType, List<HostArg> args, out int ctorCount)
        {
            List<MethodBase> methods = Reflector.GetConstructors(targetType, args.Count);
            ctorCount = methods.Count;

            MethodBase method = GetMatchingMethodAux(targetType, args, methods, "_ctor", true);
            // Because no-arg c-tors for value types are handled elsewhere, we defer the warning to there.
            return (ConstructorInfo)method;
        }

        private static MethodBase GetMatchingMethodAux(Type targetType, List<HostArg> args, List<MethodBase> methods, string methodName, bool isStatic)
        {
            int argCount = args.Count;

            if (methods.Count == 0)
                return null;

            if (methods.Count == 1)
                return methods[0];

            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(argCount + (isStatic ? 0 : 1));
            if (!isStatic)
                argsPlus.Add(new DynamicMetaObject(Expression.Default(targetType), BindingRestrictions.Empty));

            foreach (HostArg ha in args)
            {
                Expr e = ha.ArgExpr;
                Type argType = e.HasClrType ? (e.ClrType ?? typeof(object)) : typeof(Object);

                Type t;

                switch (ha.ParamType)
                {
                    case HostArg.ParameterType.ByRef:
#if CLR2
                        t = typeof(MSC::System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
#else
                        t = typeof(System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
#endif

                        break;
                    case HostArg.ParameterType.Standard:
                        t = argType;
                        break;
                    default:
                        throw Util.UnreachableCode();
                }
                argsPlus.Add(new DynamicMetaObject(Expression.Default(t), BindingRestrictions.Empty));
            }

            OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(argCount), isStatic ? CallTypes.None : CallTypes.ImplicitInstance);

            BindingTarget bt = res.ResolveOverload(methodName, methods, NarrowingLevel.None, NarrowingLevel.All);
            if (bt.Success)
                return bt.Overload.ReflectionInfo;

            return null;
        }


        private static MethodBase GetMatchingMethodAux(Type targetType, object[] actualArgs, List<MethodBase> methods, string methodName, bool isStatic)
        {
            int argCount = actualArgs.Length;

            if (methods.Count == 0)
                return null;

            if (methods.Count == 1)
                return methods[0];

            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(argCount + (isStatic ? 0 : 1));
            if (!isStatic)
                argsPlus.Add(new DynamicMetaObject(Expression.Default(targetType), BindingRestrictions.Empty));

            foreach (object arg in actualArgs)
                argsPlus.Add(new DynamicMetaObject(Expression.Default(arg.GetType()), BindingRestrictions.Empty));

            OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(argCount), isStatic ? CallTypes.None : CallTypes.ImplicitInstance);

            BindingTarget bt = res.ResolveOverload(methodName, methods, NarrowingLevel.None, NarrowingLevel.All);
            if (bt.Success)
                return bt.Overload.ReflectionInfo;

            return null;
        }


        private static List<MethodBase> GetConstructors(Type targetType, int arity)
        {
            IEnumerable<ConstructorInfo> cinfos
                = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Where(info => info.GetParameters().Length == arity);

            List<MethodBase> infos = new List<MethodBase>();

            foreach (ConstructorInfo info in cinfos)
                infos.Add(info);

            return infos;
        }



        private static void MaybeReflectionWarn(IPersistentMap spanMap, MethodBase method, string methodName)
        {
            if (method == null && RT.booleanCast(RT.WARN_ON_REFLECTION.deref()))
                RT.errPrintWriter().WriteLine(string.Format("Reflection warning, {0}:{1} - call to {2} can't be resolved.\n",
                    Compiler.SOURCE_PATH.deref(), Compiler.GetLineFromSpanMap(spanMap), methodName));
        }


        public static MethodInfo GetArityZeroMethod(Type t, string name, bool getStatics)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;
            if (getStatics)
                flags |= BindingFlags.Static;
            else
                flags |= BindingFlags.Instance;

            MethodInfo[] all = t.GetMethods();

            IEnumerable<MethodInfo> einfo = t.GetMethods(flags).Where(mi => mi.Name == name && mi.GetParameters().Length == 0);
            List<MethodInfo> infos = new List<MethodInfo>(einfo);
            if (infos.Count() == 1)
                return infos[0];
            else
                return null;
        }

        #endregion

        #region Method calling

        public static object CallInstanceMethod(string methodName, List<Type> typeArgs, object target, params object[] args)
        {
            if (args.Length == 0)
            {
                Type t = target.GetType();

                FieldInfo f = GetField(t,methodName, false);
                if (f != null)
                    return f.GetValue(target);

                PropertyInfo p = GetProperty(t,methodName, false);
                if (p != null)
                    return p.GetValue(target, null);
            }

            List<MethodBase> methods = GetMethods(target.GetType(), methodName, typeArgs, args.Length, false);
            MethodBase method = GetMatchingMethodAux(target.GetType(), args, methods, methodName, false);

            if (method == null)
            {
                if (methods.Count == 0)
                    throw new ArgumentException(String.Format("Unable to find instance method named: {0} for type: {1} with arity {2}", methodName, target.GetType(), args.Length));
                else
                    throw new ArgumentException(String.Format("Cannot resolve instance method named: {0} for type: {1} with arity {2}", methodName, target.GetType(), args.Length));
            }

            return method.Invoke(target, args);
            
            //BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;
            //return target.GetType().InvokeMember(methodName, flags, Type.DefaultBinder, target, args);
        }

        public static object CallStaticMethod(string methodName, List<Type> typeArgs, Type t, params object[] args)
        {
            if (args.Length == 0)
            {
                FieldInfo f = GetField(t,methodName, true);
                if (f != null)
                    return f.GetValue(t);

                PropertyInfo p = GetProperty(t, methodName, true);
                if (p != null)
                    return p.GetValue(t, null);
            }

            List<MethodBase> methods = GetMethods(t, methodName, typeArgs, args.Length, true);
            MethodBase method = GetMatchingMethodAux(t, args, methods, methodName, true);

            if (method == null)
            {
                if (methods.Count == 0)
                    throw new ArgumentException(String.Format("Unable to find static method named: {0} for type: {1} with arity {2}", methodName, t, args.Length));
                else
                    throw new ArgumentException(String.Format("Cannot resolve static method named: {0} for type: {1} with arity {2}", methodName, t, args.Length));
            }

            return method.Invoke(null, args);


            //BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.GetProperty;
            //return t.InvokeMember(methodName, flags, Type.DefaultBinder, null, args);
        }

        public static object InvokeConstructor(Type t, object[] args)
        {
            //  TODO: Replace with GetContructors/GetMatchingMethodAux
            IEnumerable<ConstructorInfo> einfos = t.GetConstructors().Where(ci => ci.GetParameters().Length == args.Length);
            List<ConstructorInfo> infos = new List<ConstructorInfo>(einfos);

            if (infos.Count == 0)
                throw new ArgumentException("NO matching constructor found for " + t.Name);
            else if (infos.Count == 1)
            {
                ConstructorInfo info = infos[0];
                return info.Invoke(BoxArgs(info.GetParameters(), args));
            }
            else
            {
                ConstructorInfo info = null;

                // More than one with correct arity.  Find best match.
                ConstructorInfo found = null;
                foreach (ConstructorInfo ci in infos)
                {
                    ParameterInfo[] pinfos = ci.GetParameters();
                    if (IsCongruent(pinfos, args))
                    {
                        if (found == null || Subsumes(pinfos, found.GetParameters()))
                            found = ci;
                    }
                }
                info = found;


                if (info == null)
                    throw new InvalidOperationException(string.Format("Cannot find c-tor for type: {0} with the correct argument type", Util.NameForType(t)));

                return info.Invoke(BoxArgs(info.GetParameters(), args));
            }
        }

        #endregion

        #region LispReader read-eval support

        // At the moment, only used by the LispReader
        public static Object InvokeStaticMethod(String typeName, String methodName, Object[] args)
        {
            Type t = RT.classForName(typeName);
            return InvokeStaticMethod(t, methodName, args);
        }

        public static Object InvokeStaticMethod(Type t, String methodName, Object[] args)
        {
            if (methodName.Equals("new"))
                return InvokeConstructor(t, args);
            List<MethodBase> methods = GetMethods(t, methodName, null, args.Length, true);
            return InvokeMatchingMethod(methodName, methods, t, null, args);
        }


        private static object InvokeMatchingMethod(string methodName, List<MethodBase> infos, Type t, object target, object[] args)
        {

            Type targetType = t ?? target.GetType();

            if (infos.Count == 0)
                throw new InvalidOperationException(string.Format("Cannot find {0} method named: {1} for type: {2} with {3} arguments", (t == null ? "instance" : "static"), methodName, targetType.Name, args.Length));

            MethodInfo info;

            if (infos.Count == 1)
                info = (MethodInfo)infos[0];
            else
            {
                // More than one with correct arity.  Find best match.
                MethodInfo found = null;
                foreach (MethodInfo mi in infos)
                {
                    ParameterInfo[] pinfos = mi.GetParameters();
                    if (IsCongruent(pinfos, args))
                    {
                        if (found == null || Subsumes(pinfos, found.GetParameters()))
                            found = mi;
                    }
                }
                info = found;
            }

            if (info == null)
                throw new InvalidOperationException(string.Format("Cannot find static method named {0} for type: {1} with the correct argument type", methodName, t.Name));

            return InvokeMethod(info,target,args);

        }

        internal static object InvokeMethod(MethodInfo info,object target,object[] args)
        {
            object[] boxedArgs = BoxArgs(info.GetParameters(), args);

            if (info.ReturnType == typeof(void))
            {
                info.Invoke(target, boxedArgs);
                return null;
            }
            else
                return prepRet(info.ReturnType,info.Invoke(target, boxedArgs));
        }

        #endregion

        #region  Method matching

        internal static bool Subsumes(ParameterInfo[] c1, ParameterInfo[] c2)
        {
            //presumes matching lengths
            Boolean better = false;
            for (int i = 0; i < c1.Length; i++)
            {
                Type t1 = c1[i].ParameterType;
                Type t2 = c2[i].ParameterType;
                if (t1 != t2)// || c2[i].isPrimitive() && c1[i] == Object.class))
                {
                    if (!t1.IsPrimitive && t2.IsPrimitive
                        //|| Number.class.isAssignableFrom(c1[i]) && c2[i].isPrimitive()
                       ||
                       t2.IsAssignableFrom(t1))
                        better = true;
                    else
                        return false;
                }
            }
            return better;
        }



        public static object[] BoxArgs(ParameterInfo[] pinfos, object[] args)
        {
            if (pinfos.Length == 0)
                return null;
            object[] ret = new object[pinfos.Length];
            for (int i = 0; i < pinfos.Length; i++)
                ret[i] = BoxArg(pinfos[i], args[i]);
            return ret;
        }


        private static object BoxArg(ParameterInfo pinfo, object arg)
        {
            if (arg == null)
                return arg;

            Type paramType = pinfo.ParameterType;
            Type argType = arg.GetType();

            if (!paramType.IsPrimitive)
                return arg;

            return Convert.ChangeType(arg, pinfo.ParameterType);  // don't know yet what we need here
        }

        private static bool IsCongruent(ParameterInfo[] pinfos, object[] args)
        {
            bool ret = false;
            if (args == null)
                return pinfos.Length == 0;
            if (pinfos.Length == args.Length)
            {
                ret = true;
                for (int i = 0; ret && i < pinfos.Length; i++)
                {
                    object arg = args[i];
                    Type argType = (arg == null ? null : arg.GetType());
                    Type paramType = pinfos[i].ParameterType;
                    ret = ParamArgTypeMatch(paramType, argType);
                }
            }

            return ret;
        }

        internal static bool ParamArgTypeMatch(Type paramType, Type argType)
        {
            if (argType == null)
                return !paramType.IsPrimitive;
            return AreAssignable(paramType, argType);
        }

        public static Object prepRet(Type t, Object x)
        {
            if (!t.IsPrimitive)
                return x;

            if (x is Boolean)
                //return ((Boolean)x) ? RT.T : RT.F;
                return ((Boolean)x) ? true : false;
            else if (x is Int32)
                return (long)(int)x;
            else if (x is Single)
                return (double)(float)x;
            return x;
        }

        #endregion

        #region Type assignment checks

        // Stolen from DLR TypeUtils
        internal static bool AreAssignable(Type dest, Type src)
        {
            if (dest == src)
            {
                return true;
            }
            if (dest.IsAssignableFrom(src))
            {
                return true;
            }
            if (dest.IsArray && src.IsArray && dest.GetArrayRank() == src.GetArrayRank() && AreReferenceAssignable(dest.GetElementType(), src.GetElementType()))
            {
                return true;
            }
            if (src.IsArray && dest.IsGenericType &&
                (dest.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>)
                || dest.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IList<>)
                || dest.GetGenericTypeDefinition() == typeof(System.Collections.Generic.ICollection<>))
                && dest.GetGenericArguments()[0] == src.GetElementType())
            {
                return true;
            }
            return false;
        }

        // Stolen from DLR TypeUtils
        internal static bool AreReferenceAssignable(Type dest, Type src)
        {
            // WARNING: This actually implements "Is this identity assignable and/or reference assignable?"
            if (dest == src)
            {
                return true;
            }
            if (!dest.IsValueType && !src.IsValueType && AreAssignable(dest, src))
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
