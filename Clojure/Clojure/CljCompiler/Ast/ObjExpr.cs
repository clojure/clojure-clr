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
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Scripting.Generation;
using System.Collections;


namespace clojure.lang.CljCompiler.Ast
{
    abstract class ObjExpr : Expr
    {

        #region Data

        const string CONST_PREFIX = "const__";
        const string STATIC_CTOR_HELPER_NAME = "__static_ctor_helper";

        protected string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        protected string _internalName;

        protected string _thisName;
        public string ThisName
        {
            get { return _thisName; }
            //set { _thisName = value; }
        }


        protected readonly object _tag;

        protected IPersistentMap _closes = PersistentHashMap.EMPTY;          // localbinding -> itself

        public IPersistentMap Closes
        {
            get { return _closes; }
            set { _closes = value; }
        }
        protected IPersistentVector _closesExprs = PersistentVector.EMPTY;
        protected IPersistentSet _volatiles = PersistentHashSet.EMPTY;


        protected Type _superType;

        protected Type _baseType = null;
        public Type BaseType
        {
            get { return _baseType; }
        }
        
        protected TypeBuilder _typeBuilder = null;
        public TypeBuilder TypeBuilder
        {
            get { return _typeBuilder; }
        }
        
        protected Type _objType;

        protected ParameterExpression _thisParam = null;
        public ParameterExpression ThisParam
        {
            get { return _thisParam; }
            set { _thisParam = value; }
        }

        protected List<FieldBuilder> _closedOverFields;

        
        protected List<FieldBuilder> _keywordLookupSiteFields;
        protected List<FieldBuilder> _thunkFields;

        protected IPersistentCollection _methods;

        protected IPersistentMap _fields = null;

        protected IPersistentMap _keywords = PersistentHashMap.EMPTY;         // Keyword -> KeywordExpr
        protected IPersistentMap _vars = PersistentHashMap.EMPTY;
        protected PersistentVector _constants;
        protected int _constantsID;

        protected int altCtorDrops = 0;

        protected IPersistentVector _keywordCallsites;
        protected IPersistentVector _protocolCallsites;
        protected IPersistentVector _varCallsites;

        protected ConstructorInfo _ctorInfo;

        #endregion

        #region Not yet

        /*

        protected string _superName = null;
        protected TypeBuilder _baseTypeBuilder = null;
        protected bool IsDefType { get { return _fields != null; } }

        */

        #endregion

        #region C-tors

        public ObjExpr(object tag)
        {
            _tag = tag;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return true; }
        }

        public override Type ClrType
        {
            get { return _tag != null ? HostExpr.TagToType(_tag) : typeof(IFn); }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            //_superType = GetSuperType();

            switch (context.Mode)
            {
                case CompilerMode.Immediate:
                    return GenDlrImmediate(context);
                case CompilerMode.File:
                    return GenDlrForFile(context);
                default:
                    throw Util.UnreachableCode();
            }
        }

        /*
        protected virtual Type GetSuperType()
        {
            return Type.GetType(_superName);
        }
        */

        #endregion

        #region Immediate-mode compilation

        protected abstract Expression GenDlrImmediate(GenContext context);

        #endregion

        #region File-mode compilation

        Expression GenDlrForFile(GenContext context)
        {
            EnsureTypeBuilt(context);

            List<Expression> args = new List<Expression>(_closes.count());
            for (ISeq s = RT.keys(_closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                if (lb.PrimitiveType != null)
                    args.Add(context.ObjExpr.GenUnboxedLocal(context, lb));
                else
                    args.Add(context.ObjExpr.GenLocal(context, lb));
            }

            return Expression.Convert(Expression.New(_ctorInfo, args),typeof(IFn));
        }

        private void EnsureTypeBuilt(GenContext context)
        {
            if (_typeBuilder != null)
                return;

            TypeBuilder baseTB = GenerateFnBaseClass(context);
            _baseType = baseTB.CreateType();

            GenerateFnClass(context);
            _objType = _typeBuilder.CreateType();
        }

        #endregion

        #region  Base class construction

        private TypeBuilder GenerateFnBaseClass(GenContext context)
        {
            string baseClassName = _internalName + "_base";

            TypeBuilder baseTB = context.ModuleBuilder.DefineType(baseClassName, TypeAttributes.Public | TypeAttributes.Abstract, _superType);

            GenerateConstantFields(baseTB);
            GenerateClosedOverFields(baseTB);
            GenerateVarCallsites(baseTB);
            GenerateKeywordCallsites(baseTB);

            GenerateBaseClassConstructor(baseTB);

            return baseTB;
        }

        #region Generating constant fields

        private void GenerateConstantFields(TypeBuilder baseTB)
        {
            for (int i = 0; i < _constants.count(); i++)
            {
                string fieldName = ConstantName(i);
                Type fieldType = ConstantType(i);
                FieldBuilder fb = baseTB.DefineField(fieldName, fieldType, FieldAttributes.FamORAssem | FieldAttributes.Static);
            }
        }

        private string ConstantName(int i)
        {
            return CONST_PREFIX + i;
        }

        private Type ConstantType(int i)
        {
            object o = _constants.nth(i);
            Type t = o.GetType();
            if (t.IsPublic)
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
        
        #endregion

        #region Generating closed-over fields

        private void GenerateClosedOverFields(TypeBuilder baseTB)
        {
            _closedOverFields = new List<FieldBuilder>(_closes.count());

            // closed-overs map to instance fields.
            for (ISeq s = RT.keys(_closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                Type type = lb.PrimitiveType ?? typeof(object);
                _closedOverFields.Add(baseTB.DefineField(lb.Name, type, FieldAttributes.FamORAssem));
            }
        }

        #endregion

        #region Generating VarCallSites

        private void GenerateVarCallsites(TypeBuilder baseTB)
        {
            for (int i = 0; i < _varCallsites.count(); i++)
            {
                string fieldName = VarCallsiteName(i);
                FieldBuilder fb = baseTB.DefineField(fieldName, typeof(IFn), FieldAttributes.FamORAssem | FieldAttributes.Static);
            }
        }

        public String VarCallsiteName(int n)
        {
            return "__var__callsite__" + n;
        }

        #endregion

        #region Generating KeywordCallSites

        private void GenerateKeywordCallsites(TypeBuilder baseTB)
        {
            return;

            //int count = _keywordCallsites.count();

            //_keywordLookupSiteFields = new List<FieldBuilder>(count);
            //_thunkFields = new List<FieldBuilder>(count);

            //for (int i = 0; i < _keywordCallsites.count(); i++)
            //{
            //    Keyword k = (Keyword)_keywordCallsites.nth(i);
            //    string siteName = SiteNameStatic(i);
            //    string thunkName = ThunkNameStatic(i);
            //    FieldBuilder fb1 = baseTB.DefineField(siteName, typeof(KeywordLookupSite), FieldAttributes.FamORAssem | FieldAttributes.Static);
            //    FieldBuilder fb2 = baseTB.DefineField(thunkName, typeof(LookupThunkDelegate), FieldAttributes.FamORAssem | FieldAttributes.Static);
            //    _keywordLookupSiteFields.Add(fb1);
            //    _thunkFields.Add(fb2);
            //}
        }

        String SiteName(int n)
        {
            return "__site__" + n;
        }

        public String SiteNameStatic(int n)
        {
            return SiteName(n) + "__";
        }

        String ThunkName(int n)
        {
            return "__thunk__" + n;
        }

        public String ThunkNameStatic(int n)
        {
            return ThunkName(n) + "__";
        }

        #endregion

        #region Generating base class c-tor

        private void GenerateBaseClassConstructor(TypeBuilder baseTB)
        {
            ConstructorInfo ci = _superType.GetConstructor(Type.EmptyTypes);

            if (ci == null)
                return;

            ConstructorBuilder cb = baseTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            ILGen gen = new ILGen(cb.GetILGenerator());

            gen.EmitLoadArg(0);
            gen.Emit(OpCodes.Call, ci);
            gen.Emit(OpCodes.Ret);

            // Call base constructor
            //if (_superName != null)
            //{
            //    Type parentType = Type.GetType(_superName);
            //    ConstructorInfo cInfo = parentType.GetConstructor(Type.EmptyTypes);
            //    gen.EmitLoadArg(0);         //gen.Emit(OpCodes.Ldarg_0);
            //    gen.Emit(OpCodes.Call, cInfo);
            //}
            //else if (IsVariadic)
            //{
            //    gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);
            //    gen.EmitInt(_variadicMethod.RequiredArity);     // gen.Emit(OpCodes.Ldc_I4, _variadicMethod.RequiredArity);
            //    gen.Emit(OpCodes.Call, RestFn_Int_Ctor);
            //}
            //else
            //{
            //    gen.EmitLoadArg(0);                             // en.Emit(OpCodes.Ldarg_0);
            //    gen.Emit(OpCodes.Call, AFunction_Default_Ctor);
            //}
            //gen.Emit(OpCodes.Ret);
        }

        #endregion

        #endregion

        #region Fn class construction

        private void GenerateFnClass(GenContext context)
        {
            _typeBuilder = context.AssemblyGen.DefinePublicType(_internalName, _baseType, true);

            GenerateStaticConstructor(_typeBuilder, _baseType);
            _ctorInfo = GenerateConstructor(_typeBuilder, _baseType);

            // The incoming context holds info on the containing function.
            // That is the one that holds the closed-over variable values.

            GenContext newContext = CreateContext(context, _typeBuilder, _baseType);
            GenerateMethods(newContext);
        }

        private void GenerateStaticConstructor(TypeBuilder fnTB, Type baseType)
        {
            if (_constants.count() > 0)
            {
                ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
                MethodBuilder method1 = GenerateConstants(fnTB, baseType);
                MethodBuilder method2 = GenerateVarCallsiteInits(fnTB, baseType);
                MethodBuilder method3 = GenerateKeywordCallsiteInit(fnTB, baseType);
                ILGen gen = new ILGen(cb.GetILGenerator());
                gen.EmitCall(method1);       // gen.Emit(OpCodes.Call, method1);
                if (method2 != null)
                    gen.EmitCall(method2);
                if (method3 != null)
                    gen.EmitCall(method3);
                gen.Emit(OpCodes.Ret);

            }
        }

        #region Generating constants

        private MethodBuilder GenerateConstants(TypeBuilder fnTB, Type baseType)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PRINT_DUP, true));

                List<Expression> inits = new List<Expression>();
                for (int i = 0; i < _constants.count(); i++)
                {
                    Expression expr = GenerateValue(_constants.nth(i));
                    Expression init =
                        Expression.Assign(
                            Expression.Field(null, baseType, ConstantName(i)),
                            Expression.Convert(expr, ConstantType(i)));
                    inits.Add(init);
                }
                inits.Add(Expression.Default(typeof(void)));

                Expression block = Expression.Block(inits);
                LambdaExpression lambda = Expression.Lambda(block);
                MethodBuilder methodBuilder = fnTB.DefineMethod(STATIC_CTOR_HELPER_NAME + "_constants", MethodAttributes.Private | MethodAttributes.Static);
                lambda.CompileToMethod(methodBuilder, true);
                return methodBuilder;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        private Expression GenerateValue(object value)
        {
            bool partial = true;
            Expression ret;

            if (value == null)
                ret = Expression.Constant(null);

            else if (value is String)
                ret = Expression.Constant((String)value);
            else if (Util.IsPrimitive(value.GetType()))  // or just IsNumeric?
                ret = Expression.Constant(value);
            else if (value is Type)
                ret = Expression.Call(
                    null,
                    Compiler.Method_RT_classForName,
                    Expression.Constant(((Type)value).FullName));
            else if (value is Symbol)
            {
                Symbol sym = (Symbol)value;
                ret = Expression.Call(
                    null,
                    Compiler.Method_Symbol_create2,
                    Expression.Convert(Expression.Constant(sym.Namespace), typeof(string)),  // can be null
                    Expression.Constant(sym.Name));
            }
            else if (value is Keyword)
                ret = Expression.Call(
                    null,
                    Compiler.Method_Keyword_intern,
                    GenerateValue(((Keyword)value).Symbol));
            //else if (value is KeywordCallSite)
            //{
            //}
            else if (value is Var)
            {
                Var var = (Var)value;
                ret = Expression.Call(
                    null,
                    Compiler.Method_RT_var2,
                    Expression.Constant(var.Namespace.Name.ToString()),
                    Expression.Constant(var.Symbol.Name.ToString()));
            }
            else if (value is IPersistentMap)
            {
                IPersistentMap map = (IPersistentMap)value;
                List<object> entries = new List<object>(map.count() * 2);
                foreach (IMapEntry entry in map)
                {
                    entries.Add(entry.key());
                    entries.Add(entry.val());
                }
                Expression expr = GenerateListAsObjectArray(entries);
                ret = Expression.Call(
                    null,
                    Compiler.Method_RT_map,
                    expr);
            }
            else if (value is IPersistentVector)
            {
                Expression expr = GenerateListAsObjectArray(value);
                ret = Expression.Call(
                    null,
                    Compiler.Method_RT_vector,
                    expr);
            }
            else if (value is ISeq || value is IPersistentList)
            {
                Expression expr = GenerateListAsObjectArray(value);
                ret = Expression.Call(
                    null,
                    Compiler.Method_PersistentList_create,
                    expr);
            }
            else
            {
                string cs = null;
                try
                {
                    cs = RT.printString(value);
                }
                catch (Exception)
                {
                    throw new Exception(String.Format("Can't embed object in code, maybe print-dup not defined: {0}", value));
                }
                if (cs.Length == 0)
                    throw new Exception(String.Format("Can't embed unreadable object in code: " + value));
                if (cs.StartsWith("#<"))
                    throw new Exception(String.Format("Can't embed unreadable object in code: " + cs));

                ret = Expression.Call(Compiler.Method_RT_readString, Expression.Constant(cs));
                partial = false;
            }

            if (partial)
            {
                if (value is Obj && RT.count(((Obj)value).meta()) > 0)
                {
                    Expression objExpr = Expression.Convert(ret, typeof(Obj));
                    Expression metaExpr = Expression.Convert(GenerateValue(((Obj)value).meta()), typeof(IPersistentMap));
                    ret = Expression.Call(
                        objExpr,
                        Compiler.Method_IObj_withMeta,
                        metaExpr);
                }
            }
            return ret;
        }


        private Expression GenerateListAsObjectArray(object value)
        {
            List<Expression> items = new List<Expression>();
            foreach (Object item in (ICollection)value)
                items.Add(Compiler.MaybeBox(GenerateValue(item)));

            return Expression.NewArrayInit(typeof(object), items);
        }

        #endregion

        #region  Generating other initializers

        private MethodBuilder GenerateVarCallsiteInits(TypeBuilder fnTB, Type baseType)
        {
            if (_varCallsites.count() == 0)
                return null;

            List<Expression> inits = new List<Expression>();
            for (int i = 0; i < _varCallsites.count(); i++)
            {
                Var v = (Var)_varCallsites.nth(i);
                ParameterExpression varTemp = Expression.Parameter(typeof(Var), "varTemp");
                ParameterExpression valTemp = Expression.Parameter(typeof(Object), "valTemp");

                Expression block = Expression.Block(
                    new ParameterExpression[] { varTemp },
                     Expression.Assign(
                        varTemp,
                        Expression.Call(null, Compiler.Method_RT_var2, Expression.Constant(v.Namespace.Name.Name), Expression.Constant(v.Symbol.Name))),
                    Expression.IfThen(
                        Expression.Call(varTemp, Compiler.Method_Var_hasRoot),
                        Expression.Block(
                            new ParameterExpression[] { valTemp },
                            Expression.Assign(valTemp, Expression.Call(varTemp, Compiler.Method_Var_getRoot)),
                            Expression.IfThen(
                                Expression.TypeIs(valTemp, typeof(AFunction)),
                                Expression.Assign(
                                    Expression.Field(null, _baseType, VarCallsiteName(i)),
                                    Expression.Convert(valTemp, typeof(IFn)))))));
                inits.Add(block);
            }

            Expression allInits = Expression.Block(inits);
            LambdaExpression lambda = Expression.Lambda(allInits);
            MethodBuilder methodBuilder = fnTB.DefineMethod(STATIC_CTOR_HELPER_NAME + "_callsites", MethodAttributes.Private | MethodAttributes.Static);
            lambda.CompileToMethod(methodBuilder, true);
            return methodBuilder;
        }


        private MethodBuilder GenerateKeywordCallsiteInit(TypeBuilder fnTB, Type baseType)
        {
            return null;
            //if (_keywordCallsites.count() == 0)
            //    return null;

            //List<Expression> inits = new List<Expression>();
            //ParameterExpression parm = Expression.Parameter(typeof(KeywordLookupSite), "temp");

            //for (int i = 0; i < _keywordCallsites.count(); i++)
            //{
            //    Expression nArg = Expression.Constant(i);
            //    Expression kArg = GenerateValue(_keywordCallsites.nth(i));
            //    Expression parmAssign =
            //        Expression.Assign(
            //            parm,
            //            Expression.New(Compiler.Ctor_KeywordLookupSite_2, new Expression[] { nArg, kArg }));
            //    Expression siteAssign = Expression.Assign(Expression.Field(null, _keywordLookupSiteFields[i]), parm);
            //    Expression thunkAssign =
            //        Expression.Call(
            //            null,
            //            Compiler.Method_Delegate_CreateDelegate,
            //            Expression.Constant(typeof(LookupThunkDelegate)),
            //            parm,
            //            Expression.Constant("Get"));
            //    inits.Add(parmAssign);
            //    inits.Add(siteAssign);
            //    inits.Add(thunkAssign);
            //}

            //Expression allInits = Expression.Block(new ParameterExpression[] { parm }, inits);
            //LambdaExpression lambda = Expression.Lambda(allInits);
            //MethodBuilder methodBuilder = fnTB.DefineMethod(STATIC_CTOR_HELPER_NAME + "_kwcallsites", MethodAttributes.Private | MethodAttributes.Static);
            //lambda.CompileToMethod(methodBuilder, true);
            //return methodBuilder;

        }
        

        #endregion

        #region Fn constructor

        private ConstructorBuilder GenerateConstructor(TypeBuilder fnTB, Type baseType)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, CtorTypes());
            ILGen gen = new ILGen(cb.GetILGenerator());
 
            //Call base constructor
            ConstructorInfo baseCtorInfo = baseType.GetConstructor(Type.EmptyTypes);
            gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, baseCtorInfo);

            // store closed-overs in their fields
            int a = 0;
            for (ISeq s = RT.keys(_closes); s != null; s = s.next(), a++)
            {
                LocalBinding lb = (LocalBinding)s.first();
                FieldBuilder fb = _closedOverFields[a];

                gen.EmitLoadArg(0);             // gen.Emit(OpCodes.Ldarg_0);
                gen.EmitLoadArg(a + 1);         // gen.Emit(OpCodes.Ldarg, a + 1);
                gen.Emit(OpCodes.Stfld, fb);
            }
            gen.Emit(OpCodes.Ret);
            return cb;
        }

        private Type[] CtorTypes()
        {
            if (_closes.count() == 0)
                return Type.EmptyTypes;

            Type[] ret = new Type[_closes.count()];
            int i = 0;
            for (ISeq s = RT.keys(_closes); s != null; s = s.next(), i++)
            {
                LocalBinding lb = (LocalBinding)s.first();
                ret[i] = lb.PrimitiveType ?? typeof(object);
            }
            return ret;
        }

        #endregion

        #region other

        protected abstract void GenerateMethods(GenContext context);

        protected GenContext CreateContext(GenContext incomingContext, TypeBuilder fnTB, Type baseType)
        {
            return incomingContext.CreateWithNewType(this);
        }

        #endregion

        #endregion

        #region Code generation support

        internal Expression GenLocal(GenContext context, LocalBinding lb)
        {
            if (context.Mode == CompilerMode.File && _closes.containsKey(lb))
            {
                Expression expr = Expression.Field(_thisParam, lb.Name);
                Type primtType = lb.PrimitiveType;
                if (primtType != null)
                    expr = Compiler.MaybeBox(Expression.Convert(expr, primtType));
                return expr;
            }
            else
            {
                return lb.ParamExpression;
            }
        }

        internal Expression GenUnboxedLocal(GenContext context, LocalBinding lb)
        {
            Type primType = lb.PrimitiveType;
            if (context.Mode == CompilerMode.File && _closes.containsKey(lb))
                return Expression.Convert(Expression.Field(_thisParam, lb.Name), primType);
            else
                return lb.ParamExpression;
        }


        internal Expression GenConstant(GenContext context, int id, object val)
        {
            switch (context.Mode)
            {
                case CompilerMode.Immediate:
                    return Expression.Constant(val);
                case CompilerMode.File:
                    return Expression.Field(null, _baseType, ConstantName(id));
                default:
                    throw Util.UnreachableCode();
            }
        }

        internal Expression GenVar(GenContext context, Var var)
        {
            int i = (int)_vars.valAt(var);
            return GenConstant(context, i, var);
        }

        internal Expression GenKeyword(GenContext context, Keyword kw)
        {
            int i = (int)_keywords.valAt(kw);
            return GenConstant(context, i, kw);
        }


        internal Expression GenLetFnInits(GenContext context, ParameterExpression parm, FnExpr fn, IPersistentSet leFnLocals)
        {
            // fn is the enclosing IFn, not this.
            throw new NotImplementedException();
        }


        public static string TrimGenID(string name)
        {
            int i = name.LastIndexOf("__");
            return i == -1 ? name : name.Substring(0, i);
        }

        #endregion

        #region not yet

        /*


        static readonly ConstructorInfo AFunction_Default_Ctor = typeof(AFunction).GetConstructor(Type.EmptyTypes);
        static readonly ConstructorInfo RestFn_Int_Ctor = typeof(RestFn).GetConstructor(new Type[] { typeof(int) });


        #endregion


        #region Code generation support



       */
        #endregion

    }
}
