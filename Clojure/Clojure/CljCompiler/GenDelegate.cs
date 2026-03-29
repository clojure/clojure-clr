/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using clojure.lang.CljCompiler.Context;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;


namespace clojure.lang
{
    public static class GenDelegate
    {
        #region Data

        static GenContext _context = GenContext.CreateWithInternalAssembly("delegates", false);
        static int _wrapperCount = 0;

        #endregion

        #region A little debugging aid

        //static int _saveId = 0;
        public static void SaveProxyContext()
        {
            _context.SaveAssembly();
            _context = GenContext.CreateWithInternalAssembly("delegates", false);
        }

        #endregion

        #region Factory method

        public static Delegate Create(Type delegateType, IFn fn)
        {
            MethodInfo invokeMI = delegateType.GetMethod("Invoke");
            Type returnType = invokeMI.ReturnType;
            ParameterInfo[] delParams = invokeMI.GetParameters();

            // Generate a wrapper class with the IFn as a field and an Invoke
            // method that exactly matches the delegate signature. This produces
            // delegates whose Method.GetParameters() contains only the declared
            // parameters — no hidden Closure or IFn parameter — making them
            // compatible with frameworks like ASP.NET that reflect on delegates.

            int id = Interlocked.Increment(ref _wrapperCount);
            TypeBuilder tb = _context.ModuleBuilder.DefineType(
                "clojure.delegate.Wrapper_" + id,
                TypeAttributes.Public | TypeAttributes.Sealed,
                typeof(object));

            FieldBuilder fnField = tb.DefineField("_fn", typeof(IFn), FieldAttributes.Private);

            // Constructor: takes IFn
            ConstructorBuilder ctor = tb.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                [typeof(IFn)]);
            ILGenerator ctorIL = ctor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Ldarg_1);
            ctorIL.Emit(OpCodes.Stfld, fnField);
            ctorIL.Emit(OpCodes.Ret);

            // Invoke method: matches delegate signature exactly
            Type[] paramTypes = new Type[delParams.Length];
            for (int i = 0; i < delParams.Length; i++)
                paramTypes[i] = delParams[i].ParameterType;

            MethodBuilder mb = tb.DefineMethod(
                "Invoke",
                MethodAttributes.Public,
                returnType,
                paramTypes);

            // Name parameters to match the delegate
            for (int i = 0; i < delParams.Length; i++)
                mb.DefineParameter(i + 1, delParams[i].Attributes, delParams[i].Name ?? ("p" + i));

            ILGenerator ilg = mb.GetILGenerator();

            // Load this._fn
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, fnField);

            // Load and box each parameter for IFn.invoke(object, object, ...)
            for (int i = 0; i < delParams.Length; i++)
            {
                ilg.Emit(OpCodes.Ldarg, i + 1);
                if (delParams[i].ParameterType.IsValueType)
                    ilg.Emit(OpCodes.Box, delParams[i].ParameterType);
            }

            // Call IFn.invoke(...)
            ilg.Emit(OpCodes.Callvirt, Compiler.Methods_IFn_invoke[delParams.Length]);

            if (returnType == typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }
            else if (returnType.IsValueType)
            {
                ilg.Emit(OpCodes.Unbox_Any, returnType);
            }
            else if (returnType != typeof(object))
            {
                ilg.Emit(OpCodes.Castclass, returnType);
            }

            ilg.Emit(OpCodes.Ret);

            // Create instance and delegate
            Type wrapperType = tb.CreateType();
            object wrapper = Activator.CreateInstance(wrapperType, fn);
            return Delegate.CreateDelegate(delegateType, wrapper, "Invoke");
        }

        #endregion
    }
}
