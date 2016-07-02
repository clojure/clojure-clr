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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
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

        public ModuleBuilder ModuleBuilder
        {
            get { return _assyGen.AssemblyBuilder.GetDynamicModule(_assyGen.AssemblyBuilder.GetName().Name); }
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

        readonly ISymbolDocumentWriter _docWriter;
        public ISymbolDocumentWriter DocWriter
        {
            get { return _docWriter; }
        }

        readonly SymbolDocumentInfo _docInfo;
        public SymbolDocumentInfo DocInfo { get { return _docInfo; } }

        TypeBuilder _tb;
        public TypeBuilder TB { get { return _tb; } }

        #endregion

        #region C-tors & factory methods

        private static Dictionary<Assembly, bool> InternalAssemblies = new Dictionary<Assembly, bool>();

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

#if CLR2
            // Massive kludge for .net 3.5 -- the RuntimeAssemblyBuilder yielded by reflection is not the same as AssemblyBuilder.
            Type t = CreateDummyType(ctx.ModuleBuilder);
            MethodInfo m = t.GetMethod("test");
            if (m != null && m.DeclaringType.Assembly != ctx.AssemblyBuilder)
                AddInternalAssembly(m.DeclaringType.Assembly);
#endif

            return ctx;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static Type CreateDummyType(ModuleBuilder mb)
        {
            TypeBuilder tb = mb.DefineType("_._._.AAA.DUMMY",TypeAttributes.Public);
            MethodBuilder mbb = tb.DefineMethod("test",MethodAttributes.Public,typeof(void),Type.EmptyTypes);
            var ilg = mbb.GetILGenerator();
            ilg.Emit(OpCodes.Ret);
            tb.CreateType();
            return tb;
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
            if ( createDynInitHelper )
                _dynInitHelper = new DynInitHelper(_assyGen, GenerateName());
            if (_isDebuggable)
                _docWriter = ModuleBuilder.DefineDocument(sourceName, ClojureContext.Default.LanguageGuid, ClojureContext.Default.VendorGuid, Guid.Empty);
            _docInfo = Expression.SymbolDocument(sourceName);
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
            _assyGen.SaveAssembly();
        }

        #endregion

        #region Debug info

        public static Expression AddDebugInfo(Expression expr, IPersistentMap spanMap)
        {
            GenContext context = Compiler.CompilerContextVar.deref() as GenContext;
            if (context == null)
                return expr;

            return context.MaybeAddDebugInfo(expr, spanMap);
        }

        public Expression MaybeAddDebugInfo(Expression expr, IPersistentMap spanMap)
        {
            if (_isDebuggable && spanMap != null & _docInfo != null)
            {
                int startLine;
                int startCol;
                int finishLine;
                int finishCol;
                if (Compiler.GetLocations(spanMap, out startLine, out startCol, out finishLine, out finishCol))
                    return AstUtils.AddDebugInfo(expr,
                        _docInfo,
                        new Microsoft.Scripting.SourceLocation(0, (int)spanMap.valAt(RT.StartLineKey), (int)spanMap.valAt(RT.StartColumnKey)),
                        new Microsoft.Scripting.SourceLocation(0, (int)spanMap.valAt(RT.EndLineKey), (int)spanMap.valAt(RT.EndColumnKey)));
            }
            return expr;
        }

        public static void EmitDebugInfo(ILGen ilg, IPersistentMap spanMap)
        {
            GenContext context = Compiler.CompilerContextVar.deref() as GenContext;
            if (context == null)
                return;

            context.MaybeEmitDebugInfo(ilg, spanMap);
        }

        public void MaybeEmitDebugInfo(ILGen ilg, IPersistentMap spanMap)
        {
            if ( _docWriter != null && spanMap != null )
            {
                int startLine;
                int startCol;
                int finishLine;
                int finishCol;
                if (Compiler.GetLocations(spanMap, out startLine, out startCol, out finishLine, out finishCol))
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
        }

        public static void SetLocalName(LocalBuilder lb, string name)
        {
            GenContext context = Compiler.CompilerContextVar.deref() as GenContext;
            if (context == null)
                return;

            context.MaybSetLocalName(lb, name);
        }

        public void MaybSetLocalName(LocalBuilder lb, string name)
        {
            if (_isDebuggable)
                lb.SetLocalSymInfo(name);
        }

        #endregion
    }
}
