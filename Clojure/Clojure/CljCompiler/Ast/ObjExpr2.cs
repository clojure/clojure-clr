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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    public class ObjExpr2 : Expr
    {
        #region Data

        const string ConstPrefix = "const__";
        const string StaticCtorHelperName = "__static_ctor_helper";

        private object Tag { get; set; }
        public string InternalName { get; set; }
        public IPersistentMap Keywords { get; set; }        // Keyword -> KeywordExpr
        public IPersistentMap Vars { get; set; }        
        public PersistentVector Constants { get; set; }
        Dictionary<int, FieldBuilder> _constantFields;

        #endregion

        #region C-tors

        public ObjExpr2(object tag)
        {
            Tag = tag;
            Keywords = PersistentHashMap.EMPTY;
            Vars = PersistentHashMap.EMPTY;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { throw new NotImplementedException(); }
        }

        public Type ClrType
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new NotImplementedException();
        }

        #endregion

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            throw new NotImplementedException();
        }

        public void Emit(RHC rhc, ObjExpr objs, GenContext context)
        {
            throw new NotImplementedException();
        }

        #region Generating constant fields

        public void EmitConstantFieldDefs(TypeBuilder baseTB)
        {
            _constantFields = new Dictionary<int, FieldBuilder>(Constants.count());

            for (int i = 0; i < Constants.count(); i++)
            {
                string fieldName = ConstantName(i);
                Type fieldType = ConstantType(i);
                if (!fieldType.IsPrimitive)
                {
                    FieldBuilder fb = baseTB.DefineField(fieldName, fieldType, FieldAttributes.FamORAssem | FieldAttributes.Static);
                    _constantFields[i] = fb;

                }
            }
        }
        
        private static string ConstantName(int i)
        {
            return ConstPrefix + i;
        }

        private Type ConstantType(int i)
        {
            object o = Constants.nth(i);
            Type t = o == null ? null : o.GetType();
            if (t != null && t.IsPublic)
            {
                // Java: can't emit derived fn types due to visibility
                if (typeof(LazySeq).IsAssignableFrom(t))
                    return typeof(ISeq);
                else if (t == typeof(Keyword))
                    return t;
                else if (typeof(RestFn).IsAssignableFrom(t))
                    return typeof(RestFn);
                else if (typeof(AFn).IsAssignableFrom(t))
                    return typeof(AFn);
                else if (t == typeof(Var))
                    return t;
                else if (t == typeof(String))
                    return t;
            }
            return typeof(object);
        }

        public MethodBuilder GenerateConstants(TypeBuilder fnTB)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PrintDupVar, true));

                MethodBuilder mb = fnTB.DefineMethod(StaticCtorHelperName + "_constants", MethodAttributes.Private | MethodAttributes.Static);
                ILGenerator ilg = mb.GetILGenerator();

                for (int i = 0; i < Constants.count(); i++)
                {
                    EmitValue(Constants.nth(i), mb);
                    ilg.Emit(OpCodes.Castclass,ConstantType(i));
                    ilg.Emit(OpCodes.Stfld,_constantFields[i]);                    
                }
                ilg.Emit(OpCodes.Ret);
      
                return mb;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


        #endregion

        //public void GenerateStaticConstructor(TypeBuilder fnTB)
        //{
        //    if (_constants.count() > 0)
        //    {
        //        ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
        //        MethodBuilder method1 = GenerateConstants(fnTB);
        //        MethodBuilder method3 = GenerateKeywordCallsiteInit(fnTB);
        //        ILGen gen = new ILGen(cb.GetILGenerator());
        //        gen.EmitCall(method1);       // gen.Emit(OpCodes.Call, method1);
        //        if (method3 != null)
        //            gen.EmitCall(method3);
        //        gen.Emit(OpCodes.Ret);

        //    }
        //}

    }
}
