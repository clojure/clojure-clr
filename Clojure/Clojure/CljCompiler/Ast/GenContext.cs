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

    public enum CompilerMode { Immediate, File };

    public class GenContext
    {
        #region Data

        readonly CompilerMode _mode;

        internal CompilerMode Mode
        {
            get { return _mode; }
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


        readonly DynInitHelper _dynInitHelper;

        internal DynInitHelper DynInitHelper
        {
            get { return _dynInitHelper; }
        } 

        ObjExpr _objExpr = null;
        internal ObjExpr ObjExpr
        {
            get { return _objExpr; }
        }

        #endregion

        #region C-tors & factory methods

        public GenContext(string assyName, CompilerMode mode)
            : this(assyName, ".dll", null, mode)
        {
        }

        public GenContext(string assyName, string extension, string directory, CompilerMode mode)
        {
            AssemblyName aname = new AssemblyName(assyName);
            _assyGen = new AssemblyGen(aname, directory, extension, true);
            _mode = mode;
            if ( mode == CompilerMode.File )
                _dynInitHelper = new DynInitHelper(_assyGen, "__InternalDynamicExpressionInits");
        }

        private GenContext(CompilerMode mode)
        {
            _mode = mode;
        }

        internal GenContext CreateWithNewType(ObjExpr objExpr)
        {
            GenContext newContext = Clone();
            newContext._objExpr = objExpr;
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
