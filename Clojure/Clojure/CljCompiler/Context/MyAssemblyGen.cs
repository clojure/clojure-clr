using Microsoft.Scripting.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Threading;


#if ! NETFRAMEWORK
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Diagnostics.SymbolStore;
using System.Collections.Immutable;
#endif

// This class is based on the original Microsoft code for Microsoft.Scripting.Gneeration.AssemblyGen.
// Lots of code copied here.
//
// // Licensed to the .NET Foundation under one or more agreements.
// // The .NET Foundation licenses this file to you under the Apache 2.0 License.
// // See the LICENSE file in the project root for more information.
// 

// Even before rolling my own version here, I had made my own versions of some its methods, such as MakeDelegateType.  
// The main adaptation here is to allow for creating a PersistedAssemblyBuilder for when we are in creating a persisted assembly in the context of .NET 9 or later.
// To help make the code clearer to the outside and easier to implement,  I created two constructors, one for persisted assemblies and one for non-persisted assemblies.
// The constructor for persisted assemblies is only visible in .NET Framework and .NET 9 or later.

namespace clojure.lang.CljCompiler.Context;

public sealed class MyAssemblyGen
{
    private readonly AssemblyBuilder _myAssembly;
    private readonly ModuleBuilder _myModule;
    private readonly bool _isDebuggable;
    private readonly bool _isPersistable;

    private int _index;


#if NETFRAMEWORK || NET9_0_OR_GREATER
    private readonly string _outFileName;       // can be null iff not saveable
    private readonly string _outDir;            // null means the current directory
#endif

#if NET9_0_OR_GREATER
    MethodBuilder _entryPointMethodBuilder;     // non-null means we have an entry point
    ISymbolDocumentWriter _docWriter = null;    // non-null means we are writing debug info
#endif

    internal AssemblyBuilder AssemblyBuilder => _myAssembly;
    internal ModuleBuilder ModuleBuilder => _myModule;
    internal bool IsDebuggable => _isDebuggable;

    // This is the constructor for the non-persisted assembly.
    public MyAssemblyGen(AssemblyName name, bool isDebuggable)
    {
        ContractUtils.RequiresNotNull(name, nameof(name));

        _myAssembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        _myModule = _myAssembly.DefineDynamicModule(name.Name, isDebuggable);
        _isDebuggable = isDebuggable;
        _isPersistable = false;

#if NETFRAMEWORK || NET9_0_OR_GREATER
        _outFileName = null;
        _outDir = null;
#endif

        if (isDebuggable)
        {
            SetDebuggableAttributes();
        }
    }

#if NETFRAMEWORK || NET9_0_OR_GREATER

    // This is the constructor for the persisted assembly.
    public MyAssemblyGen(AssemblyName name, string outDir, string outFileExtension, bool isDebuggable, IDictionary<string, object> attrs = null)
    {
        ContractUtils.RequiresNotNull(name, nameof(name));

        if (outFileExtension == null)
        {
            outFileExtension = ".dll";
        }

        if (outDir != null)
        {
            try
            {
                outDir = Path.GetFullPath(outDir);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Invalid directory name");
            }
            try
            {
                Path.Combine(outDir, name.Name + outFileExtension);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("Invalid assembly name or extension");
            }

            _outFileName = name.Name + outFileExtension;
            _outDir = outDir;
        }


        // mark the assembly transparent so that it works in partial trust:
        var attributes = new List<CustomAttributeBuilder> {
                new CustomAttributeBuilder(typeof(SecurityTransparentAttribute).GetConstructor(ReflectionUtils.EmptyTypes), ArrayUtils.EmptyObjects) };

        if (attrs != null)
        {
            foreach (var attr in attrs)
            {
                if (!(attr.Value is string a) || string.IsNullOrWhiteSpace(a))
                {
                    continue;
                }

                ConstructorInfo ctor = null;
                switch (attr.Key)
                {
                    case "assemblyFileVersion":
                        ctor = typeof(AssemblyFileVersionAttribute).GetConstructor(new[] { typeof(string) });
                        break;
                    case "copyright":
                        ctor = typeof(AssemblyCopyrightAttribute).GetConstructor(new[] { typeof(string) });
                        break;
                    case "productName":
                        ctor = typeof(AssemblyProductAttribute).GetConstructor(new[] { typeof(string) });
                        break;
                    case "productVersion":
                        ctor = typeof(AssemblyInformationalVersionAttribute).GetConstructor(new[] { typeof(string) });
                        break;
                }

                if (ctor != null)
                {
                    attributes.Add(new CustomAttributeBuilder(ctor, new object[] { a }));
                }
            }
        }

#if NETFRAMEWORK
        _myAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave, outDir, false, attributes);
        _myModule = _myAssembly.DefineDynamicModule(name.Name, _outFileName, isDebuggable);
        _myAssembly.DefineVersionInfoResource();
#elif NET9_0_OR_GREATER
        PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(name,typeof(object).Assembly, attributes);
        _myAssembly = ab;
        _myModule = ab.DefineDynamicModule(name.Name, isDebuggable);
#endif
        _isPersistable = true;
        _isDebuggable = isDebuggable;

        if (isDebuggable) {
            SetDebuggableAttributes();
        }


    }
#endif

    internal void SetDebuggableAttributes()
    {
        DebuggableAttribute.DebuggingModes attrs =
            DebuggableAttribute.DebuggingModes.Default |
            DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints |
            DebuggableAttribute.DebuggingModes.DisableOptimizations;

        Type[] argTypes = new Type[] { typeof(DebuggableAttribute.DebuggingModes) };
        Object[] argValues = new Object[] { attrs };

        var debuggableCtor = typeof(DebuggableAttribute).GetConstructor(argTypes);

        _myAssembly.SetCustomAttribute(new CustomAttributeBuilder(debuggableCtor, argValues));
        _myModule.SetCustomAttribute(new CustomAttributeBuilder(debuggableCtor, argValues));
    }


    public string SaveAssembly()
    {
        if (!_isPersistable)
        {
            throw new InvalidOperationException("Assembly is not persistable");
        }

#if NETFRAMEWORK
        _myAssembly.Save(_outFileName, PortableExecutableKinds.ILOnly, ImageFileMachine.I386);
        return Path.Combine(_outDir, _outFileName);
#elif NET9_0_OR_GREATER
        if ( _entryPointMethodBuilder is not null || _docWriter is not null)
            SavePersistedAssemblyHard();
        else
            ((PersistedAssemblyBuilder)_myAssembly).Save(_outFileName);
        return Path.Combine(_outDir, _outFileName);
#else
        return null;
#endif

    }

#if NET9_0_OR_GREATER
    private void SavePersistedAssemblyHard()
    {
        PersistedAssemblyBuilder ab = (PersistedAssemblyBuilder)_myAssembly;
        MetadataBuilder metadataBuilder = ab.GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder fieldData, out MetadataBuilder pdbBuilder);
            
        MethodDefinitionHandle entryPointHandle = 
            _entryPointMethodBuilder is null 
            ? default(MethodDefinitionHandle)
            : MetadataTokens.MethodDefinitionHandle(_entryPointMethodBuilder.MetadataToken);
        DebugDirectoryBuilder debugDirectoryBuilder = GeneratePdb(pdbBuilder, metadataBuilder.GetRowCounts(), entryPointHandle);

        ManagedPEBuilder peBuilder = new(
                    header: PEHeaderBuilder.CreateExecutableHeader(),
                    metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                    ilStream: ilStream,
                    mappedFieldData: fieldData,
                    debugDirectoryBuilder: debugDirectoryBuilder,
                    entryPoint: entryPointHandle);

        BlobBuilder peBlob = new();
        peBuilder.Serialize(peBlob);

        // Create the executable:
        using FileStream fileStream = new(_outFileName, FileMode.Create, FileAccess.Write);
        peBlob.WriteContentTo(fileStream);
    }

    static DebugDirectoryBuilder GeneratePdb(MetadataBuilder pdbBuilder, ImmutableArray<int> rowCounts, MethodDefinitionHandle entryPointHandle)
    {
        BlobBuilder portablePdbBlob = new BlobBuilder();
        PortablePdbBuilder portablePdbBuilder = new PortablePdbBuilder(pdbBuilder, rowCounts, entryPointHandle);
        BlobContentId pdbContentId = portablePdbBuilder.Serialize(portablePdbBlob);
        // In case saving PDB to a file
        using FileStream fileStream = new FileStream("MyAssemblyEmbeddedSource.pdb", FileMode.Create, FileAccess.Write);
        portablePdbBlob.WriteContentTo(fileStream);

        DebugDirectoryBuilder debugDirectoryBuilder = new DebugDirectoryBuilder();
        debugDirectoryBuilder.AddCodeViewEntry("MyAssemblyEmbeddedSource.pdb", pdbContentId, portablePdbBuilder.FormatVersion);
        // In case embedded in PE:
        // debugDirectoryBuilder.AddEmbeddedPortablePdbEntry(portablePdbBlob, portablePdbBuilder.FormatVersion);
        return debugDirectoryBuilder;
    }
#endif


#if NETFRAMEWORK
    internal void SetEntryPoint(MethodInfo mi, PEFileKinds kind)
    {
        _myAssembly.SetEntryPoint(mi, kind);
    }
#elif NET9_0_OR_GREATER
    internal void SetEntryPoint(MethodBuilder mb)
    {
        _entryPointMethodBuilder = mb;
    }
#endif


    public TypeBuilder DefinePublicType(string name, Type parent, bool preserveName)
    {
        return DefineType(name, parent, TypeAttributes.Public, preserveName);
    }

    internal TypeBuilder DefineType(string name, Type parent, TypeAttributes attr, bool preserveName)
    {
        ContractUtils.RequiresNotNull(name, nameof(name));
        ContractUtils.RequiresNotNull(parent, nameof(parent));

        StringBuilder sb = new StringBuilder(name);
        if (!preserveName)
        {
            int index = Interlocked.Increment(ref _index);
            sb.Append("$");
            sb.Append(index);
        }

        // There is a bug in Reflection.Emit that leads to 
        // Unhandled Exception: System.Runtime.InteropServices.COMException (0x80131130): Record not found on lookup.
        // if there is any of the characters []*&+,\ in the type name and a method defined on the type is called.
        sb.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_').Replace(',', '_').Replace('\\', '_');

        name = sb.ToString();

        return _myModule.DefineType(name, attr, parent);
    }


    private const MethodAttributes CtorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
    private const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
    private const MethodAttributes InvokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
    private const TypeAttributes DelegateAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass;
    private static readonly Type[] _DelegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };

    public Type MakeDelegateType(string name, Type[] parameters, Type returnType)
    {
        TypeBuilder builder = DefineType(name, typeof(MulticastDelegate), DelegateAttributes, false);
        builder.DefineConstructor(CtorAttributes, CallingConventions.Standard, _DelegateCtorSignature).SetImplementationFlags(ImplAttributes);
        builder.DefineMethod("Invoke", InvokeAttributes, returnType, parameters).SetImplementationFlags(ImplAttributes);
        return builder.CreateTypeInfo();
    }

}