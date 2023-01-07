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
using System.Linq.Expressions;
using Microsoft.Scripting.Actions;
using System.Dynamic;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using clojure.lang.CljCompiler.Ast;
using System.Text;
using clojure.lang.Runtime.Binding;
using clojure.lang.Runtime;
using System.Runtime.CompilerServices;

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

            List<Type> typesToCheck = new List<Type>
            {
                t
            };

            if (t.IsInterface && !getStatics)
                typesToCheck.AddRange(t.GetInterfaces());

            List<PropertyInfo> pinfos = new List<PropertyInfo>();

            foreach (Type type in typesToCheck)
            {
                IEnumerable<PropertyInfo> einfo
                     = type.GetProperties(flags).Where(info => info.Name == name && info.GetIndexParameters().Length == 0);
                pinfos.AddRange(einfo);
            }


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
        public static object SetInstanceFieldOrProperty(object target, string fieldName, object val)
        {
            Type t = target.GetType();
            FieldInfo field = GetField(t, fieldName, false);
            if (field != null)
            {
                if (field.IsInitOnly)
                {
                    throw new InvalidOperationException(String.Format("Attempt to set readonly field {0} in class {1}", field.Name, field.DeclaringType));
                }
                field.SetValue(target, val);
                return val;
            }
            PropertyInfo prop = GetProperty(t, fieldName, false);
            if (prop != null)
            {
                prop.SetValue(target, val, Array.Empty<object>());
                return val;
            }
            throw new ArgumentException(String.Format("No matching field/property found: {0} for {1}", fieldName, t));
        }

        // used in generated code
        public static object GetInstanceFieldOrProperty(object target, string fieldName)
        {
            Type t = target.GetType();

            FieldInfo field = GetField(t, fieldName, false);
            if (field != null)
                return Reflector.prepRet(field.FieldType,field.GetValue(target));

            PropertyInfo prop = GetProperty(t, fieldName, false);
            if (prop != null)
                return Reflector.prepRet(prop.PropertyType,prop.GetValue(target, Array.Empty<object>()));

            MethodInfo method = GetArityZeroMethod(t, fieldName, false);

            if (method != null)
                return Reflector.prepRet(method.ReturnType, method.Invoke(target, Array.Empty<object>()));

            throw new ArgumentException(String.Format("No matching instance field/property found: {0} for {1}", fieldName, t));
        }

        #endregion

        #region Method lookup

        /// <summary>
        /// Parse-time lookup of static method
        /// </summary>
        /// <param name="spanMap"></param>
        /// <param name="targetType"></param>
        /// <param name="args"></param>
        /// <param name="methodName"></param>
        /// <param name="typeArgs"></param>
        /// <returns></returns>
        public static MethodInfo GetMatchingMethod(IPersistentMap spanMap, Type targetType, IList<HostArg> args, string methodName, IList<Type> typeArgs)
        {
            IList<MethodBase> methods = GetMethods(targetType, methodName, typeArgs, args.Count, true);

            MethodBase method = GetMatchingMethodAux(targetType, args, methods, methodName, true);
            MaybeReflectionWarn(spanMap, targetType, true, methods.Count > 0,  method, methodName, args);
            return (MethodInfo)method;
        }

        /// <summary>
        /// Parse-time lookup of instance method
        /// </summary>
        /// <param name="spanMap"></param>
        /// <param name="target"></param>
        /// <param name="args"></param>
        /// <param name="methodName"></param>
        /// <param name="typeArgs"></param>
        /// <returns></returns>
        public static MethodInfo GetMatchingMethod(IPersistentMap spanMap, Expr target, IList<HostArg> args, string methodName, IList<Type> typeArgs)
        {
            MethodBase method = null;
            bool hasMethods = false;
            if (target.HasClrType)
            {
                Type targetType = target.ClrType;
                IList<MethodBase> methods = GetMethods(targetType, methodName, typeArgs, args.Count, false);
                method = GetMatchingMethodAux(targetType, args, methods, methodName, false);
                hasMethods = methods.Count > 0;
            }

            MaybeReflectionWarn(spanMap, (target.HasClrType ? target.ClrType : null), false, hasMethods, method, methodName, args);
            return (MethodInfo)method;
        }


        /// <summary>
        /// Get methods of fixed name and arity.
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="methodName"></param>
        /// <param name="typeArgs"></param>
        /// <param name="arity"></param>
        /// <param name="getStatics"></param>
        /// <returns></returns>
        internal static IList<MethodBase> GetMethods(Type targetType, string methodName, IList<Type> typeArgs, int arity, bool getStatics)
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
                    if (typeArgs != null && minfo.ContainsGenericParameters)
                        infos.Add(minfo.MakeGenericMethod(typeArgs.ToArray<Type>()));
                    else
                        infos.Add(minfo);
            }
            return infos;
        }


        private static List<MethodBase> GetInterfaceMethods(Type targetType, string methodName, IList<Type> typeArgs, int arity)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

            List<Type> interfaces = new List<Type>
            {
                targetType
            };
            interfaces.AddRange(targetType.GetInterfaces());

            List<MethodBase> infos = new List<MethodBase>();

            foreach (Type type in interfaces)
            {
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


        /// <summary>
        /// Get constructor matching args for type.
        /// </summary>
        /// <param name="spanMap"></param>
        /// <param name="targetType"></param>
        /// <param name="args"></param>
        /// <param name="ctorCount"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        internal static ConstructorInfo GetMatchingConstructor(IPersistentMap spanMap, Type targetType, IList<HostArg> args, out int ctorCount)
        {
            IList<MethodBase> methods = Reflector.GetConstructors(targetType, args.Count);
            ctorCount = methods.Count;

            MethodBase method = GetMatchingMethodAux(targetType, args, methods, "_ctor", true);
            // Because no-arg c-tors for value types are handled elsewhere, we defer the warning to there.
            return (ConstructorInfo)method;
        }

        /// <summary>
        /// Select matching method from list based on args.
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="args"></param>
        /// <param name="methods"></param>
        /// <param name="methodName"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        private static MethodBase GetMatchingMethodAux(Type targetType, IList<HostArg> args, IList<MethodBase> methods, string methodName, bool isStatic)
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
                        t = typeof(System.Runtime.CompilerServices.StrongBox<>).MakeGenericType(argType);
                        break;
                    case HostArg.ParameterType.Standard:
                        t = argType;
                        break;
                    default:
                        throw Util.UnreachableCode();
                }
                argsPlus.Add(new DynamicMetaObject(Expression.Default(t), BindingRestrictions.Empty));
            }

            // TODO: See if we can get rid of .Default
            OverloadResolverFactory factory = ClojureContext.Default.SharedOverloadResolverFactory;
            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(argCount), isStatic ? CallTypes.None : CallTypes.ImplicitInstance);

            BindingTarget bt = res.ResolveOverload(methodName, methods, NarrowingLevel.None, NarrowingLevel.All);
            if (bt.Success)
                return bt.Overload.ReflectionInfo;

            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private static MethodBase GetMatchingMethodAux(Type targetType, object[] actualArgs, IList<MethodBase> methods, string methodName, bool isStatic)
        {
            int argCount = actualArgs.Length;

            if (methods.Count == 0)
                return null;

            //if (methods.Count == 1)
            //    return methods[0];

            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(argCount + (isStatic ? 0 : 1));
            if (!isStatic)
                argsPlus.Add(new DynamicMetaObject(Expression.Default(targetType), BindingRestrictions.Empty));

            foreach (object arg in actualArgs)
                argsPlus.Add(new DynamicMetaObject(Expression.Default(arg.GetType()), BindingRestrictions.Empty,arg));

            OverloadResolverFactory factory = ClojureContext.Default.SharedOverloadResolverFactory;
            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(argCount), isStatic ? CallTypes.None : CallTypes.ImplicitInstance);

            BindingTarget bt = res.ResolveOverload(methodName, methods, NarrowingLevel.None, NarrowingLevel.All);
            if (bt.Success)
                return bt.Overload.ReflectionInfo;

            return null;
        }


        private static IList<MethodBase> GetConstructors(Type targetType, int arity)
        {
            IEnumerable<ConstructorInfo> cinfos
                = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Where(info => info.GetParameters().Length == arity);

            List<MethodBase> infos = new List<MethodBase>();

            foreach (ConstructorInfo info in cinfos)
                infos.Add(info);

            return infos;
        }

        private static void MaybeReflectionWarn(IPersistentMap spanMap, Type targetType, bool isStatic, bool hasMethods, MethodBase method, string methodName, IList<HostArg> args)
        {
            if (method == null && RT.booleanCast(RT.WarnOnReflectionVar.deref()))
            {
                if (targetType == null)
                {
                    RT.errPrintWriter().WriteLine(string.Format("Reflection warning, {0}:{1}:{2} - call to {3}method {4} can't be resolved (target class is unknown).",
                        Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(spanMap), Compiler.GetColumnFromSpanMap(spanMap), (isStatic ? "static " : ""), methodName));
                }
                else if (hasMethods)
                {
                    RT.errPrintWriter().WriteLine(string.Format("Reflection warning, {0}:{1}:{2} - call to {3}method {4} on {5} can't be resolved (argument types: {6}).",
                        Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(spanMap), Compiler.GetColumnFromSpanMap(spanMap), (isStatic ? "static " : ""), methodName, targetType.FullName, GetTypeStringForArgs(args)));
                }
                else
                {
                    RT.errPrintWriter().WriteLine(string.Format("Reflection warning, {0}:{1}:{2} - call to {3}method {4} on {5} can't be resolved (no such method).",
                        Compiler.SourcePathVar.deref(), Compiler.GetLineFromSpanMap(spanMap), Compiler.GetColumnFromSpanMap(spanMap), (isStatic ? "static " : ""), methodName, targetType.FullName));
                }
                RT.errPrintWriter().Flush();
            }
        }

        private static string GetTypeStringForArgs(IList<HostArg> args)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (HostArg ha in args)
            {
                Expr e = ha.ArgExpr;
                if (i > 0)
                    sb.Append(", ");
                sb.Append((e.HasClrType && e.ClrType != null) ?  e.ClrType.FullName : "unknown");
                i++;
            }
            return sb.ToString();
        }

        public static MethodInfo GetArityZeroMethod(Type t, string name, bool getStatics)
        {
            //BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;
            //if (getStatics)
            //    flags |= BindingFlags.Static;
            //else
            //    flags |= BindingFlags.Instance;

            ////MethodInfo[] all = t.GetMethods();

            //IEnumerable<MethodInfo> einfo = t.GetMethods(flags).Where(mi => mi.Name == name && mi.GetParameters().Length == 0);
            //List<MethodInfo> infos = new List<MethodInfo>(einfo);

            IList<MethodBase> infos = GetMethods(t, name, null, 0, getStatics);

            if (infos.Count == 1)
                return (MethodInfo)infos[0];
            else if (getStatics && infos.Count > 1)
            {
                // static method with no arguments, multiple implementations.  Find closest to leaf in hierarchy.
                Type d = infos[0].DeclaringType;
                MethodBase m = infos[0];
                for (int i = 1; i < infos.Count; i++)
                {
                    Type d1 = infos[i].DeclaringType;
                    if (d1.IsSubclassOf(d))
                    {
                        d = d1;
                        m = infos[i];
                    }
                }
                return (MethodInfo)m;
            }
            else
                return null;
        }

        #endregion

        #region Method calling during eval


        public static object CallInstanceMethod(string methodName, IList<Type> typeArgs, object target, params object[] args)
        {
            Type t = target.GetType();
            return CallMethod(methodName, typeArgs, false, t, target, args);
        }

  
        public static object CallStaticMethod(string methodName, IList<Type> typeArgs, Type t, params object[] args)
        {
            return CallMethod(methodName, typeArgs, true, t, null, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        public static object CallMethod(string methodName, IList<Type> typeArgs, bool isStatic, Type t, object target, params object[] args)
        {
            Expression targetExpr = isStatic ? Expression.Constant(t, typeof(Type)) : Expression.Constant(target);
            List<Expression> exprs = new List<Expression>();
            foreach (object arg in args)
                exprs.Add(Expression.Constant(arg));

            Expression[] argExprs = ClrExtensions.ArrayInsert<Expression>(targetExpr, exprs);

            CallSiteBinder binder = args.Length == 0 
                ? (CallSiteBinder) new ClojureGetZeroArityMemberBinder(ClojureContext.Default, methodName, isStatic) 
                : (CallSiteBinder) new ClojureInvokeMemberBinder(ClojureContext.Default, methodName, argExprs.Length, isStatic);

            Expression dyn = Expression.Dynamic(binder, typeof(object), argExprs);

            LambdaExpression lambda = Expression.Lambda<clojure.lang.Compiler.ReplDelegate>(dyn);
            return lambda.Compile().DynamicInvoke();
        }



        public static object InvokeConstructor(Type t, object[] args)
        {
            //  TODO: Replace with GetContructors/GetMatchingMethodAux
            IEnumerable<ConstructorInfo> einfos = t.GetConstructors().Where(ci => ci.GetParameters().Length == args.Length);
            List<ConstructorInfo> infos = new List<ConstructorInfo>(einfos);

            if (infos.Count == 0)
            {
                if (t.IsValueType && args.Length == 0)
                    // invoke default c-tor
                    return Activator.CreateInstance(t);
                throw new ArgumentException("No matching constructor found for " + t.Name);
            }
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
            Type t = RT.classForNameE(typeName);
            return InvokeStaticMethod(t, methodName, args);
        }

        public static Object InvokeStaticMethod(Type t, String methodName, Object[] args)
        {
            if (methodName.Equals("new"))
                return InvokeConstructor(t, args);
            IList<MethodBase> methods = GetMethods(t, methodName, null, args.Length, true);
            return InvokeMatchingMethod(methodName, methods, t, null, args);
        }


        private static object InvokeMatchingMethod(string methodName, IList<MethodBase> infos, Type t, object target, object[] args)
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
            //Type argType = arg.GetType();

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
                    Type argType = (arg?.GetType());
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        public static Object prepRet(Type t, Object x)
        {
            //if (!t.IsPrimitive)
            //    return x;

            //if (x is Boolean)
            //    //return ((Boolean)x) ? RT.T : RT.F;
            //    return ((Boolean)x) ? true : false;
            //else if (x is Int32)
            //    return (long)(int)x;
            ////else if (x is Single)
            ////    return (double)(float)x;
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
