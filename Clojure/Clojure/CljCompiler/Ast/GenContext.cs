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

using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Scripting.Generation;
using System;
using System.Diagnostics.SymbolStore;
using clojure.lang.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using System.Collections.Generic;
using System.Collections;

namespace clojure.lang.CljCompiler.Ast
{
    public sealed class GenContext
    {
        #region Data

        readonly AssemblyGen _assyGen;
        public AssemblyGen AssemblyGen
        {
            get { return _assyGen; }
        }

        public AssemblyBuilder AssemblyBuilder
        {
            get { return _assyGen.AssemblyBuilder; }
        }

        readonly ModuleBuilder _moduleBuilder;
        public ModuleBuilder ModuleBuilder
        {
            get { return _moduleBuilder; }
        }

        DynInitHelper _dynInitHelper;
        internal DynInitHelper DynInitHelper
        {
            get { return _dynInitHelper; }
        }

        readonly bool _isDebuggable;
        public bool IsDebuggable
        {
            get { return _isDebuggable; }
        }

        readonly ISymbolDocumentWriter _docWriter = null;
        public ISymbolDocumentWriter DocWriter
        {
            get { return _docWriter; }
        }

        readonly SymbolDocumentInfo _docInfo;
        public SymbolDocumentInfo DocInfo { get { return _docInfo; } }

        TypeBuilder _tb;
        public TypeBuilder TB { get { return _tb; } }


        public String Path { get; set; }

        #endregion

        #region C-tors & factory methods

        private readonly static Dictionary<Assembly, bool> InternalAssemblies = new Dictionary<Assembly, bool>();

        private static void AddInternalAssembly(Assembly a)
        {
            lock (((ICollection)InternalAssemblies).SyncRoot)
            {
                InternalAssemblies[a] = true;
            }
        }

        public static bool IsInternalAssembly(Assembly a)
        {
            lock (((ICollection)InternalAssemblies).SyncRoot)
            {
                return InternalAssemblies.ContainsKey(a);
            }
        }

        public static GenContext CreateWithInternalAssembly(string assyName, bool createDynInitHelper)
        {
            GenContext ctx = CreateGenContext(assyName, assyName, ".dll", null, createDynInitHelper);
            AddInternalAssembly(ctx.AssemblyBuilder);
            return ctx;
        }

        public static GenContext CreateWithExternalAssembly(string sourceName, AssemblyName assemblyName, string extension, bool createDynInitHelper)
        {
            string path = Compiler.CompilePathVar.deref() as string;
            return new GenContext(path ?? System.IO.Directory.GetCurrentDirectory(), assemblyName, extension, createDynInitHelper, sourceName);
        }

        public static GenContext CreateWithExternalAssembly(string sourceName, string assyName, string extension, bool createDynInitHelper)
        {
            string path = Compiler.CompilePathVar.deref() as string;
            return CreateGenContext(sourceName, assyName, extension, path ?? System.IO.Directory.GetCurrentDirectory(),createDynInitHelper);
        }

        public static GenContext CreateWithExternalAssembly(string assyName, string extension, bool createDynInitHelper)
        {
            return CreateWithExternalAssembly(assyName, assyName, extension, createDynInitHelper);
        }

        private static GenContext CreateGenContext(string sourceName, string assyName, string extension, string directory, bool createDynInitHelper)
        {
            if (directory != null)
            {
                if (directory.Length > 0 ) //&& directory != ".")
                    assyName = assyName.Replace("/", ".");
            }

            AssemblyName aname = new AssemblyName(assyName);
            return new GenContext(directory, aname, extension, createDynInitHelper, sourceName);
        }

        private GenContext(string directory, AssemblyName aname, string extension, bool createDynInitHelper, string sourceName)
        {
            // TODO: Make this settable from a *debug* flag
#if DEBUG
            _isDebuggable = true;
#else
            _isDebuggable = false;
#endif

            _assyGen = new AssemblyGen(aname, directory, extension, _isDebuggable);
            if (createDynInitHelper)
                _dynInitHelper = new DynInitHelper(_assyGen, GenerateName());

            _docInfo = Expression.SymbolDocument(sourceName);

            _moduleBuilder = _assyGen.ModuleBuilder;

            Path = ComputeAssemblyPath(directory, aname.Name, extension);

#if NET462
            if (_isDebuggable)
                _docWriter = ModuleBuilder.DefineDocument(sourceName, ClojureContext.Default.LanguageGuid, ClojureContext.Default.VendorGuid, Guid.Empty);
#endif
        }

        private string ComputeAssemblyPath(string directory, string name, string extension)
        {
            directory = directory ?? ".";
            directory = System.IO.Path.GetFullPath(directory);
            extension = extension ?? ".dll";
            return System.IO.Path.Combine(directory, name + extension);

        }

         
        internal GenContext WithNewDynInitHelper()
        {
            return WithNewDynInitHelper(GenerateName());
        }

        internal GenContext WithNewDynInitHelper(string dihClassName)
        {
            GenContext newContext = Clone();

            newContext._dynInitHelper = new DynInitHelper(_assyGen, dihClassName);

            return newContext;
        }

        static string GenerateName()
        {
            return "__InternalDynamicExpressionInits_" + RT.nextID();
        }

        private GenContext Clone()
        {
            return (GenContext) this.MemberwiseClone();
        }

        public GenContext WithTypeBuilder(TypeBuilder tb)
        {
            GenContext newContext = Clone();
            newContext._tb = tb;
            return newContext;
        }

#endregion

        #region Other

        // DO not call context.AssmeblyGen.SaveAssembly() directly.
        internal void SaveAssembly()
        {
            if ( _dynInitHelper != null  )
                _dynInitHelper.FinalizeType();

#if NET462                 
            _assyGen.SaveAssembly();
#else
            Console.WriteLine("AOT-compilation not available");
            //var assembly = AssemblyBuilder;
            //var generator = new Lokad.ILPack.AssemblyGenerator();
            //generator.GenerateAssembly(assembly,Path);
#endif
        }

        #endregion

#region Debug info

        public static Expression AddDebugInfo(Expression expr, IPersistentMap spanMap)
        {
            if (Compiler.CompilerContextVar.deref() is GenContext context)
                return context.MaybeAddDebugInfo(expr, spanMap);
            else
                return expr;
        }

        public Expression MaybeAddDebugInfo(Expression expr, IPersistentMap spanMap)
        {
            if (_isDebuggable && spanMap != null & _docInfo != null)
            {
                if (Compiler.GetLocations(spanMap, out int startLine, out int startCol, out int finishLine, out int finishCol))
                    return AstUtils.AddDebugInfo(expr,
                        _docInfo,
                        new Microsoft.Scripting.SourceLocation(0, startLine, startCol),
                        new Microsoft.Scripting.SourceLocation(0, finishLine, finishCol));
            }
            return expr;
        }

        public static void EmitDebugInfo(ILGen ilg, IPersistentMap spanMap)
        {
            if (Compiler.CompilerContextVar.deref() is GenContext context)
                context.MaybeEmitDebugInfo(ilg, spanMap);
        }

        public void MaybeEmitDebugInfo(ILGen ilg, IPersistentMap spanMap)
        {
#if NET462
            if ( _docWriter != null && spanMap != null )
            {
                if (Compiler.GetLocations(spanMap, out int startLine, out int startCol, out int finishLine, out int finishCol))
                {
                    try
                    {
                        ilg.MarkSequencePoint(_docWriter, startLine, startCol, finishLine, finishCol);
                    }
                    catch (NotSupportedException)
                    {
                        // probably a dynamic ilgen
                    }
                }
            }
#endif
        }

        public static void SetLocalName(LocalBuilder lb, string name)
        {
            if (Compiler.CompilerContextVar.deref() is GenContext context)
                context.MaybSetLocalName(lb, name);
        }

        public void MaybSetLocalName(LocalBuilder lb, string name)
        {
#if NET462
            if (_isDebuggable)
                lb.SetLocalSymInfo(name);
#endif
        }

        #endregion
    }
}
