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
    class ObjExpr : Expr
    {
        #region Data

        protected string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        protected string _internalName;
        protected string _superName = null;

        protected string _thisName;
        public string ThisName
        {
            get { return _thisName; }
            set { _thisName = value; }
        }

        protected Type _objType;
        protected readonly object _tag;

        protected IPersistentMap _closes = PersistentHashMap.EMPTY;          // localbinding -> itself
        public IPersistentMap Closes
        {
            get { return _closes; }
            set { _closes = value; }
        }

        protected IPersistentVector _closesExprs = PersistentVector.EMPTY;
        protected IPersistentSet _volatiles = PersistentHashSet.EMPTY;

        protected TypeBuilder _typeBuilder = null;
        public TypeBuilder TypeBuilder
        {
            get { return _typeBuilder; }
        }
        protected TypeBuilder _baseTypeBuilder = null;
        protected Type _baseType = null;

        public Type BaseType
        {
            get { return _baseType; }
        }
        protected ParameterExpression _thisParam = null;
        public ParameterExpression ThisParam
        {
            get { return _thisParam; }
            set { _thisParam = value; }
        }

        protected Type _superType;


        protected ConstructorInfo _ctorInfo;

        protected List<FieldBuilder> _closedOverFields;

        protected IPersistentCollection _methods;

        protected IPersistentMap _fields = null;

        protected bool IsDefType { get { return _fields != null; } }

        protected IPersistentMap _keywords = PersistentHashMap.EMPTY;         // Keyword -> KeywordExpr
        protected IPersistentMap _vars = PersistentHashMap.EMPTY;
        protected PersistentVector _constants;
        protected int _constantsID;

        protected int altCtorDrops = 0;

        protected IPersistentVector _keywordCallsites;
        protected IPersistentVector _protocolCallsites;
        protected IPersistentVector _varCallsites;




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
            _superType = GetSuperType();

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

        protected virtual Type GetSuperType()
        {
            return Type.GetType(_superName);
        }

        #endregion

        #region Immediate-mode compilation

        Expression GenDlrImmediate(GenContext context)
        {
            _baseType = GetBaseClass(context,_superType);
            return GenerateImmediateLambda(context, _baseType);
        }

        private Expression GenerateImmediateLambda(GenContext context, Type baseClass)
        {
            ParameterExpression p1 = Expression.Parameter(baseClass, "__x__");
            _thisParam = p1;

            List<Expression> exprs = new List<Expression>();

            // TODO: deal with embedding variadic

            //if (baseClass == typeof(RestFnImpl))
            //    exprs.Add(Expression.Assign(p1,
            //              Expression.New(Compiler.Ctor_RestFnImpl_1, Expression.Constant(_variadicMethod.RequiredArity))));
            //else
            //    exprs.Add(Expression.Assign(p1, Expression.New(p1.Type)));

            exprs.Add(Expression.Assign(p1, Expression.New(p1.Type)));

            GenContext newContext = CreateContext(context, null, baseClass);

            GenerateMethodsForImmediate(newContext,p1,exprs);

            exprs.Add(p1);

            Expression expr = Expression.Block(new ParameterExpression[] { p1 }, exprs);
            return expr;
        }

        protected virtual void GenerateMethodsForImmediate(GenContext context, ParameterExpression thisParam, List<Expression> exprs)
        {
        }

        protected virtual Type GetBaseClass(GenContext context,Type superType)
        {
            Type baseClass = LookupBaseClass(superType);
            if (baseClass != null)
                return baseClass;

            baseClass = GenerateBaseClass(context,superType);
            baseClass = RegisterBaseClass(superType, baseClass);
            return baseClass;
        }

        static AtomicReference<IPersistentMap> _baseClassMapRef = new AtomicReference<IPersistentMap>(PersistentHashMap.EMPTY);

        //static ObjExpr()
        //{
        //    _baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(RestFn),typeof(RestFnImpl)));
        //    //_baseClassMapRef.Set(_baseClassMapRef.Get().assoc(typeof(AFn),typeof(AFnImpl)));
        //}


        private static Type LookupBaseClass(Type superType)
        {
            return (Type)_baseClassMapRef.Get().valAt(superType);
        }

        private static Type RegisterBaseClass(Type superType, Type baseType)
        {
            IPersistentMap map = _baseClassMapRef.Get();

            while (!map.containsKey(superType))
            {
                IPersistentMap newMap = map.assoc(superType, baseType);
                _baseClassMapRef.CompareAndSet(map, newMap);
                map = _baseClassMapRef.Get();
            }

            return LookupBaseClass(superType);  // may not be the one we defined -- race condition
        }


        private static Type GenerateBaseClass(GenContext context, Type superType)
        {
            // TODO: We'll have to change this when we do deftype, etc.
            return AFnImplGenerator.Create(context, superType);
        }

        #endregion

        #region File-mode compilation

        Expression GenDlrForFile(GenContext context)
        {
            EnsureTypeBuilt(context);

            //ConstructorInfo ctorInfo = _ctorInfo;
            ConstructorInfo ctorInfo = _objType.GetConstructors()[0];

            // The incoming context holds info on the containing function.
            // That is the one that holds the closed-over variable values.

            List<Expression> args = new List<Expression>(_closes.count());
            for (ISeq s = RT.keys(_closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                if (lb.PrimitiveType != null)
                    args.Add(context.ObjExpr.GenUnboxedLocal(context, lb));
                else
                    args.Add(context.ObjExpr.GenLocal(context, lb));
            }

            return Expression.New(ctorInfo, args);
        }


        internal Expression GenLocal(GenContext context, LocalBinding lb)
        {
            if (context.Mode == CompilerMode.File && _closes.containsKey(lb))
            {
                Expression expr = Expression.Field(_thisParam,lb.Name);
                Type primtType = lb.PrimitiveType;
                if ( primtType != null )
                    expr = Compiler.MaybeBox(Expression.Convert(expr,primtType));
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

        private void EnsureTypeBuilt(GenContext context)
        {
            if (_typeBuilder != null)
                return;

            _baseTypeBuilder = GenerateFnBaseClass(context);
            _baseType = _baseTypeBuilder.CreateType();

            GenerateFnClass(context, _baseType);
            _objType = _typeBuilder.CreateType();
        }
            

        #region  Base class construction

        private TypeBuilder GenerateFnBaseClass(GenContext context)
        {
            string baseClassName = _internalName + "_base";

            TypeBuilder baseTB = context.ModuleBuilder.DefineType(baseClassName, TypeAttributes.Public | TypeAttributes.Abstract, _superType);
            //TypeBuilder baseTB = context.AssemblyGen.DefinePublicType(baseClassName, _superType, true);

            GenerateConstantFields(baseTB);
            GenerateClosedOverFields(baseTB);
            GenerateBaseClassConstructor(baseTB);

            return baseTB;
        }

        private void GenerateConstantFields(TypeBuilder baseTB)
        {
            for (int i = 0; i < _constants.count(); i++)
            {
                string fieldName = ConstantName(i);
                Type fieldType = ConstantType(i);
                FieldBuilder fb = baseTB.DefineField(fieldName, fieldType, FieldAttributes.FamORAssem | FieldAttributes.Static);
            }
        }

        const string CONST_PREFIX = "const__";

        private string ConstantName(int i)
        {
            return CONST_PREFIX + i;
        }

        // TODO: see if this is really what we want.
        private Type ConstantType(int i)
        {
            object o = _constants.nth(i);
            Type t = o.GetType();
            if (t.IsPublic)
            {
                // Java: can't emit derived fn types due to visibility
                if (typeof(LazySeq).IsAssignableFrom(t))
                    return typeof(ISeq);
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
            // This ends up being too specific. 
            // TODO: However, if we were to see the value returned by RT.readFromString(), we could make it work.
            //return t;
        }

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

        static readonly ConstructorInfo AFunction_Default_Ctor = typeof(AFunction).GetConstructor(Type.EmptyTypes);
        static readonly ConstructorInfo RestFn_Int_Ctor = typeof(RestFn).GetConstructor(new Type[] { typeof(int) });

        private void GenerateBaseClassConstructor(TypeBuilder baseTB)
        {
            ConstructorBuilder cb = baseTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            ILGen gen = new ILGen(cb.GetILGenerator());
            // Call base constructor
            if (_superName != null)
            {
                Type parentType = Type.GetType(_superName);
                ConstructorInfo cInfo = parentType.GetConstructor(Type.EmptyTypes);
                gen.EmitLoadArg(0);         //gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, cInfo);  
            }
            //else if (IsVariadic)
            //{
            //    gen.EmitLoadArg(0);                             // gen.Emit(OpCodes.Ldarg_0);
            //    gen.EmitInt(_variadicMethod.RequiredArity);     // gen.Emit(OpCodes.Ldc_I4, _variadicMethod.RequiredArity);
            //    gen.Emit(OpCodes.Call, RestFn_Int_Ctor);
            //}
            else
            {
                gen.EmitLoadArg(0);                             // en.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, AFunction_Default_Ctor);
            }
            gen.Emit(OpCodes.Ret);
        }


        #endregion

        #region Function class construction

        private TypeBuilder GenerateFnClass(GenContext context, Type baseType)
        {
            //TypeBuilder fnTB = context.ModuleBldr.DefineType(_internalName, TypeAttributes.Class | TypeAttributes.Public, baseType);
            TypeBuilder fnTB = context.AssemblyGen.DefinePublicType(_internalName, baseType, true);
            _typeBuilder = fnTB;
            //_thisParam = Expression.Parameter(_baseType, _thisName);

            GenerateStaticConstructor(fnTB, baseType);
            _ctorInfo = GenerateConstructor(fnTB, baseType);

            GenContext newContext = CreateContext(context, fnTB, baseType);
            GenerateMethods(newContext);

            return fnTB;
        }

        private void GenerateStaticConstructor(TypeBuilder fnTB, Type baseType)
        {
            if (_constants.count() > 0)
            {
                ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
                MethodBuilder method = GenerateConstants(fnTB, baseType);
                ILGen gen = new ILGen(cb.GetILGenerator());
                gen.EmitCall(method);       // gen.Emit(OpCodes.Call, method);
                gen.Emit(OpCodes.Ret);

            }
        }

        private Expression GenerateListAsObjectArray(object value)
        {
            List<Expression> items = new List<Expression>();
            foreach ( Object item in (ICollection)value )
                items.Add(Compiler.MaybeBox(GenerateValue(item)));
               
            return Expression.NewArrayInit(typeof(object), items);
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
        
        private MethodBuilder GenerateConstants(TypeBuilder fnTB, Type baseType)
        {
            try
            {
                //Var.pushThreadBindings(RT.map(RT.PRINT_DUP, RT.T));
                Var.pushThreadBindings(RT.map(RT.PRINT_DUP, true));

                List<Expression> inits = new List<Expression>();
                for (int i = 0; i < _constants.count(); i++)
                {
                    Expression expr = GenerateValue(_constants.nth(i));
                    Expression init =
                        Expression.Assign(
                            Expression.Field(null, baseType, ConstantName(i)),
                            Expression.Convert(expr,ConstantType(i)));
                    inits.Add(init);
                }
                inits.Add(Expression.Default(typeof(void)));

                Expression block = Expression.Block(inits);
                LambdaExpression lambda = Expression.Lambda(block);
                MethodBuilder methodBuilder = fnTB.DefineMethod(STATIC_CTOR_HELPER_NAME, MethodAttributes.Private | MethodAttributes.Static);
                //lambda.CompileToMethod(methodBuilder,DebugInfoGenerator.CreatePdbGenerator());   
                lambda.CompileToMethod(methodBuilder, true);
                return methodBuilder;
            }
            finally
            {
                Var.popThreadBindings();
            }

        }


        static readonly string STATIC_CTOR_HELPER_NAME = "__static_ctor_helper";

        //private MethodBuilder GenerateConstants(TypeBuilder fnTB, Type baseType)
        //{
        //    try
        //    {
        //        Var.pushThreadBindings(RT.map(RT.PRINT_DUP, RT.T));

        //        List<Expression> inits = new List<Expression>();
        //        for (int i = 0; i < _constants.count(); i++)
        //        {
        //            object o = _constants.nth(i);
        //            string stringValue = null;
        //            if (o is string)
        //                stringValue = (string)o;
        //            else
        //            {
        //                try
        //                {
        //                    stringValue = RT.printString(o);
        //                }
        //                catch (Exception)
        //                {
        //                    throw new Exception(String.Format("Can't embed object in code, maybe print-dup not defined: {0}", o));
        //                }
        //                if (stringValue.Length == 0)
        //                    throw new Exception(String.Format("Can't embed unreadable object in code: " + o));
        //                if (stringValue.StartsWith("#<"))
        //                    throw new Exception(String.Format("Can't embed unreadable object in code: " + stringValue));
        //            }
        //            Expression init =
        //                Expression.Assign(
        //                    Expression.Field(null, baseType, ConstantName(i)),
        //                    Expression.Convert(Expression.Call(Compiler.Method_RT_readString, Expression.Constant(stringValue)),
        //                                       ConstantType(i)));
        //            inits.Add(init);
        //        }
        //        inits.Add(Expression.Default(typeof(void)));

        //        Expression block = Expression.Block(inits);
        //        LambdaExpression lambda = Expression.Lambda(block);
        //        MethodBuilder methodBuilder = fnTB.DefineMethod(STATIC_CTOR_HELPER_NAME, MethodAttributes.Private | MethodAttributes.Static);
        //        lambda.CompileToMethod(methodBuilder);
        //        return methodBuilder;
        //    }
        //    finally
        //    {
        //        Var.popThreadBindings();
        //    }

        //}

        private ConstructorBuilder GenerateConstructor(TypeBuilder fnTB, Type baseType)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, CtorTypes());
            ILGen gen = new ILGen(cb.GetILGenerator());
            //Call base constructor
            ConstructorInfo baseCtorInfo = baseType.GetConstructor(Type.EmptyTypes);
            gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, baseCtorInfo);

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

        protected virtual void GenerateMethods(GenContext context)
        {
        }

        #endregion

        private GenContext CreateContext(GenContext incomingContext,TypeBuilder fnTB,Type baseType)
        {
            return incomingContext.CreateWithNewType(this);
        }



        #endregion

        #region Code generation support


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
            return GenConstant(context,i,var);
        }

        internal Expression GenKeyword(GenContext context, Keyword kw)
        {
            int i = (int)_keywords.valAt(kw);
            return GenConstant(context,i,kw);
        }


        internal Expression GenLetFnInits(GenContext context, ParameterExpression parm ,FnExpr fn, IPersistentSet leFnLocals)
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
    }
}
