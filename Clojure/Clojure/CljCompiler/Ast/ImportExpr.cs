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
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class ImportExpr : Expr
    {
        #region Data

        readonly string _typeName;
        public string TypeName { get { return _typeName; } }

        #endregion

        #region Ctors

        public ImportExpr(string typeName)
        {
            _typeName = typeName;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return false; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public Type ClrType
        {
            get { throw new ArgumentException("ImportExpr has no CLR type"); }
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#")]
            public Expr Parse(ParserContext pcon, object frm)
            {
                return new ImportExpr((string)RT.second(frm));
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            Namespace ns = (Namespace)RT.CurrentNSVar.deref();
            ns.importClass(RT.classForNameE(_typeName));
            return null;
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            ilg.Emit(OpCodes.Call,Compiler.Method_Compiler_CurrentNamespace.GetGetMethod());
            ilg.Emit(OpCodes.Ldstr, _typeName);
            ilg.Emit(OpCodes.Call, Compiler.Method_RT_classForName);
            ilg.Emit(OpCodes.Call, Compiler.Method_Namespace_importClass1);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
