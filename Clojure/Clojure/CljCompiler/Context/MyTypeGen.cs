// I had to copy Microsoft.Scripting.Generation.TypeGen.cs to this project and modify it to use MyAssemblyGen instead of Microsoft.Scripting.Generation.AssemblyGen.
// The latter is sealed, so I couldn't just derive.  Sigh.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

//#if FEATURE_REFEMIT

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Context;

public sealed class MyTypeGen
{
    private ILGen _initGen;                        // The IL generator for the .cctor()

    /// <summary>
    /// Gets the Compiler associated with the Type Initializer (cctor) creating it if necessary.
    /// </summary>
    public ILGen TypeInitializer
    {
        get
        {
            if (_initGen == null)
            {
                _initGen = new ILGen(TypeBuilder.DefineTypeInitializer().GetILGenerator());
            }
            return _initGen;
        }
    }

    internal MyAssemblyGen AssemblyGen { get; }

    public TypeBuilder TypeBuilder { get; }

    public MyTypeGen(MyAssemblyGen myAssembly, TypeBuilder myType)
    {
        Assert.NotNull(myAssembly, myType);

        AssemblyGen = myAssembly;
        TypeBuilder = myType;
    }

    public override string ToString()
    {
        return TypeBuilder.ToString();
    }

    public Type FinishType()
    {
        _initGen?.Emit(OpCodes.Ret);
        Type ret = TypeBuilder.CreateTypeInfo();
        return ret;
    }

    public FieldBuilder AddStaticField(Type fieldType, string name)
    {
        return TypeBuilder.DefineField(name, fieldType, FieldAttributes.Public | FieldAttributes.Static);
    }

    public FieldBuilder AddStaticField(Type fieldType, FieldAttributes attributes, string name)
    {
        return TypeBuilder.DefineField(name, fieldType, attributes | FieldAttributes.Static);
    }

    public ILGen DefineExplicitInterfaceImplementation(MethodInfo baseMethod)
    {
        ContractUtils.RequiresNotNull(baseMethod, nameof(baseMethod));

        MethodAttributes attrs = baseMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.Public);
        attrs |= MethodAttributes.NewSlot | MethodAttributes.Final;

        Type[] baseSignature = baseMethod.GetParameters().Map(p => p.ParameterType);
        MethodBuilder mb = TypeBuilder.DefineMethod(
            baseMethod.DeclaringType.Name + "." + baseMethod.Name,
            attrs,
            baseMethod.ReturnType,
            baseSignature);

        TypeBuilder.DefineMethodOverride(mb, baseMethod);
        return new ILGen(mb.GetILGenerator());
    }

    private const MethodAttributes MethodAttributesToEraseInOveride = MethodAttributes.Abstract | MethodAttributes.ReservedMask;

    // TODO: Use ReflectionUtils.DefineMethodOverride?
    public ILGen DefineMethodOverride(MethodInfo baseMethod)
    {
        MethodAttributes finalAttrs = baseMethod.Attributes & ~MethodAttributesToEraseInOveride;
        Type[] baseSignature = baseMethod.GetParameters().Map(p => p.ParameterType);
        MethodBuilder mb = TypeBuilder.DefineMethod(baseMethod.Name, finalAttrs, baseMethod.ReturnType, baseSignature);

        TypeBuilder.DefineMethodOverride(mb, baseMethod);
        return new ILGen(mb.GetILGenerator());
    }
}

//#endif