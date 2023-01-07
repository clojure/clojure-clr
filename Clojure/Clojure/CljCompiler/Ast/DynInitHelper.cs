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
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Scripting.Generation;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Runtime;
using clojure.lang.Runtime.Binding;

namespace clojure.lang.CljCompiler.Ast
{

    class DynInitHelper
    {
        #region Data

        int _id;
        readonly AssemblyGen _assemblyGen;
        TypeBuilder _typeBuilder;
        TypeGen _typeGen;

        readonly string _typeName;


        List<SiteInfo> _siteInfos;

        public class SiteInfo
        {
            public FieldBuilder FieldBuilder { get; set; }
            public Type SiteType { get; set; }
            public CallSiteBinder Binder { get; set; }
            public Type DelegateType { get; set; }

            public SiteInfo(FieldBuilder fb, Type st, CallSiteBinder binder, Type delegateType)
            {
                FieldBuilder = fb;
                SiteType = st;
                Binder = binder;
                DelegateType = delegateType;
            }
        }

        Dictionary<Type, Type> _delegateTypes;

        #endregion

        #region Ctors and factories

        public DynInitHelper(AssemblyGen ag, string typeName)
        {
            _assemblyGen = ag;
            _typeName = typeName;
        }

        #endregion

        #region Dynamic expression rewriting

        /// <summary>
        /// Reduces the provided DynamicExpression into site.Target(site, *args).
        /// </summary>
        public Expression ReduceDyn(DynamicExpression node, out SiteInfo siteInfo)
        {
            MaybeInit();

            if (RewriteDelegate(node.DelegateType, out Type delegateType))
            {
                node = Expression.MakeDynamic(delegateType, node.Binder, node.Arguments);
            }

            CallSite cs = CallSite.Create(node.DelegateType, node.Binder);
            // TODO: fix this eventually to return the SiteInfo and not the Expression

            Expression access = RewriteCallSite(cs, _typeGen, delegateType ?? node.DelegateType, out siteInfo);

            // ($site = siteExpr).Target.Invoke($site, *args)
            ParameterExpression site = Expression.Variable(cs.GetType(), "$site");

            return Expression.Block(
                new[] { site },
                Expression.Call(
                    Expression.Field(
                        Expression.Assign(site, access),
                        cs.GetType().GetField("Target")
                    ),
                    node.DelegateType.GetMethod("Invoke"),
                    ClrExtensions.ArrayInsert(site, node.Arguments)
                )
            );
        }

        private void MaybeInit()
        {
            if (_typeBuilder == null)
            {
                _typeBuilder = _assemblyGen.DefinePublicType(_typeName, typeof(object), true);
                _typeGen = new TypeGen(_assemblyGen, _typeBuilder);
                _siteInfos = new List<SiteInfo>();
            }
        }


        private Expression RewriteCallSite(CallSite site, TypeGen tg, Type delegateType, out SiteInfo siteInfo)
        {
            if (!(site.Binder is IExpressionSerializable))
            {
                throw new ArgumentException("Generating code from non-serializable CallSiteBinder.");
            }

            Type siteType = site.GetType();
            FieldBuilder fb = tg.AddStaticField(siteType, "sf" + (_id++).ToString());
            siteInfo = new SiteInfo(fb, siteType, site.Binder, delegateType);
            _siteInfos.Add(siteInfo);

            // rewrite the node...
            return Expression.Field(null, fb);
        }

        #endregion

        #region DLR code generation : stolen code

        // The following code is lifted from DLR because of protection levels on various pieces.

        // From Microsoft.Scripting.Generation.ToDiskRewriter
        private bool RewriteDelegate(Type delegateType, out Type newDelegateType)
        {
            if (!ShouldRewriteDelegate(delegateType))
            {
                newDelegateType = null;
                return false;
            }

            if (_delegateTypes == null)
            {
                _delegateTypes = new Dictionary<Type, Type>();
            }

            // TODO: should caching move to AssemblyGen?
            if (!_delegateTypes.TryGetValue(delegateType, out newDelegateType))
            {
                MethodInfo invoke = delegateType.GetMethod("Invoke");

                newDelegateType = /* _typeGen.AssemblyGen. */MakeDelegateType(
                    delegateType.Name,
                    invoke.GetParameters().Map(p => p.ParameterType),
                    invoke.ReturnType
                );

                _delegateTypes[delegateType] = newDelegateType;
            }

            return true;
        }

        static readonly Type _internalModuleBuilderType = Type.GetType("System.Reflection.Emit.InternalModuleBuilder");

        // From Microsoft.Scripting.Generation.ToDiskRewriter
        private bool ShouldRewriteDelegate(Type delegateType)
        {
            // We need to replace a transient delegateType with one stored in
            // the assembly we're saving to disk.
            //
            // One complication:
            // SaveAssemblies mode prevents us from detecting the module as
            // transient. If that option is turned on, always replace delegates
            // that live in another AssemblyBuilder

            // DM: Added this test to detect the Snippets assembly/module
            if (delegateType.Module.GetType() == _internalModuleBuilderType)
            {
                return true;
            }

            var module = delegateType.Module as ModuleBuilder;
 
            if (module == null)
            {
                return false;
            }
#if NET462
            if (module.IsTransient())
            {
                return true;
            }
#endif

            if (Snippets.Shared.SaveSnippets && module.Assembly != _assemblyGen.AssemblyBuilder)
            {
                return true;
            }

            return false;
        }

        // From Microsoft.Scripting.Generation.AssemblyGen
        // Adapted to not being a method of AssemblyGen, which causes me to copy a WHOLE BUNCH of stuff.

        internal Type MakeDelegateType(string name, Type[] parameters, Type returnType)
        {
            TypeBuilder builder = /* _assemblyGen. */DefineType(name, typeof(MulticastDelegate), DelegateAttributes, false);
            builder.DefineConstructor(CtorAttributes, CallingConventions.Standard, _DelegateCtorSignature).SetImplementationFlags(ImplAttributes);
            builder.DefineMethod("Invoke", InvokeAttributes, returnType, parameters).SetImplementationFlags(ImplAttributes);
            return builder.CreateType();
        }

        // From Microsoft.Scripting.Generation.AssemblyGen
        //private int _index;
        internal TypeBuilder DefineType(string name, Type parent, TypeAttributes attr, bool preserveName)
        {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNull(parent, nameof(parent));

            StringBuilder sb = new StringBuilder(name);
            if (!preserveName)
            {
                int index = RT.nextID(); //Interlocked.Increment(ref _index);
                sb.Append('$');
                sb.Append(index);
            }

            // There is a bug in Reflection.Emit that leads to 
            // Unhandled Exception: System.Runtime.InteropServices.COMException (0x80131130): Record not found on lookup.
            // if there is any of the characters []*&+,\ in the type name and a method defined on the type is called.
            sb.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_').Replace(',', '_').Replace('\\', '_');

            name = sb.ToString();
            return /* _myModule */ _assemblyGen.ModuleBuilder.DefineType(name, attr, parent);
        }
        private const MethodAttributes CtorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
        private const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
        private const MethodAttributes InvokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
        private const TypeAttributes DelegateAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass;
        private static readonly Type[] _DelegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };

#endregion

        #region Finalizing

        public void FinalizeType()
        {
            if (_typeBuilder != null && ! _typeBuilder.IsCreated())
            {
                CreateStaticCtor();
                _typeGen.FinishType();
            }
        }

        void CreateStaticCtor()
        {
            ConstructorBuilder ctorB = _typeBuilder.DefineConstructor(MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            CljILGen gen = new CljILGen(ctorB.GetILGenerator());

            foreach (SiteInfo si in _siteInfos)
            {
                string setterName = String.Format("{0}_setter", si.FieldBuilder.Name);

                MethodBuilder mbSetter = _typeBuilder.DefineMethod(
                    setterName,
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard,
                    si.SiteType,
                    Type.EmptyTypes);
                //LambdaExpression initL = Expression.Lambda(Expression.Assign(Expression.Field(null, fb), fbInit));
                //initL.CompileToMethod(mbSetter);
                CljILGen setterIlg = new CljILGen(mbSetter.GetILGenerator());

                if (!(si.Binder is IClojureBinder b))
                    throw new InvalidOperationException("Binder of unknown type");
                b.GenerateCreationIL(mbSetter.GetILGenerator());

                setterIlg.EmitCall(si.SiteType.GetMethod("Create"));
                setterIlg.Emit(OpCodes.Dup);
                LocalBuilder v0 = setterIlg.DeclareLocal(si.FieldBuilder.FieldType);
                setterIlg.Emit(OpCodes.Stloc, v0);
                setterIlg.Emit(OpCodes.Stsfld, si.FieldBuilder);
                setterIlg.Emit(OpCodes.Ldloc,v0);
                setterIlg.Emit(OpCodes.Ret);

                gen.EmitCall(mbSetter);
                gen.Emit(OpCodes.Pop);
            }

            gen.Emit(OpCodes.Ret);
        }

        #endregion

 
    }

    static class ClrExtensions
    {
        #region Misc

        public static T[] ArrayInsert<T>(T item, IList<T> list)
        {
            T[] res = new T[list.Count + 1];
            res[0] = item;
            list.CopyTo(res, 1);
            return res;
        }

        #endregion

        #region Stolen from the DLR


        internal static U[] Map<T, U>(this ICollection<T> collection, System.Func<T, U> select)
        {
            int count = collection.Count;
            U[] result = new U[count];
            count = 0;
            foreach (T t in collection)
            {
                result[count++] = select(t);
            }
            return result;
        }

        #endregion
    }
}
