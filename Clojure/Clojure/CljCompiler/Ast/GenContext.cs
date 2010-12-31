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

#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Generation;


namespace clojure.lang.CljCompiler.Ast
{
    public class GenContext
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

        #endregion

        #region C-tors & factory methods

        public GenContext(string assyName, bool createDynInitHelper)
            : this(assyName, ".dll", null, createDynInitHelper)
        {
        }

        public GenContext(string assyName, string extension, string directory, bool createDynInitHelper)
        {
            _isDebuggable = false;
            AssemblyName aname = new AssemblyName(assyName);
            _assyGen = new AssemblyGen(aname, directory, extension, _isDebuggable);
            if ( createDynInitHelper )
                _dynInitHelper = new DynInitHelper(_assyGen, "__InternalDynamicExpressionInits");
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
