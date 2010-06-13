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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Generation;


namespace clojure.lang.CljCompiler.Ast
{

    #region Enums

    //public enum CompilerMode { Immediate, File };

    /// <summary>
    /// Indicates if the assembly is for dynamic (internal) use, or will be saved.
    /// </summary>
    public enum AssemblyMode { 
        /// <summary>
        /// The assembly is for dynamic (internal) use only.
        /// </summary>
        Dynamic, 

        /// <summary>
        /// The assembly will be saved.
        /// </summary>
        Save 
    };

    /// <summary>
    /// Indicates whether we need full class generation for the current function
    /// </summary>
    public enum FnMode
    {
        // The current ObjExpr is not generating its own class
        Light,

        // The current ObjExpr is generating its own class
        Full
    };

    #endregion

    public class GenContext
    {
        #region Data

        AssemblyMode _assyMode;

        public AssemblyMode AssyMode
        {
            get { return _assyMode; }
        }

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

        ObjExpr _objExpr = null;
        internal ObjExpr ObjExpr
        {
            get { return _objExpr; }
        }

        FnMode _fnMode;

        public FnMode FnCompileMode
        {
            get { return _fnMode; }
            set { _fnMode = _assyMode == AssemblyMode.Save ? FnMode.Full : value; }
        }

        #endregion

        #region C-tors & factory methods

        public GenContext(string assyName, AssemblyMode assyMode, FnMode fnMode )
            : this(assyName, ".dll", null, assyMode, fnMode)
        {
        }

        public GenContext(string assyName, string extension, string directory, AssemblyMode assyMode, FnMode fnMode)
        {
            AssemblyName aname = new AssemblyName(assyName);
            _assyGen = new AssemblyGen(aname, directory, extension, true);
            _assyMode = assyMode;
            FnCompileMode = fnMode;
            if ( assyMode ==  AssemblyMode.Save )
                _dynInitHelper = new DynInitHelper(_assyGen, "__InternalDynamicExpressionInits");
        }

        internal GenContext CreateWithNewType(ObjExpr objExpr)
        {
            GenContext newContext = Clone();
            newContext._objExpr = objExpr;

            newContext.FnCompileMode = FnCompileMode == FnMode.Full ? FnMode.Full : objExpr.CompileMode();

            return newContext;
        }

        internal GenContext WithNewDynInitHelper(string dihClassName)
        {
            GenContext newContext = Clone();

            newContext._dynInitHelper = new DynInitHelper(_assyGen, dihClassName);

            return newContext;
        }

        private GenContext Clone()
        {
            return (GenContext) this.MemberwiseClone();
        }

        #endregion

        #region Other

        // DO not call context.AssmeblyGen.SaveAssembly() directly.
        internal void SaveAssembly()
        {
            if ( _dynInitHelper != null )
                _dynInitHelper.FinalizeType();
            _assyGen.SaveAssembly();
        }

        #endregion
    }
}
