/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    class FnExpr : Expr
    {
        #region Data

        static readonly Type[] EMPTY_TYPE_ARRAY = new Type[0];

        static readonly Keyword KW_ONCE = Keyword.intern(null, "once");
        static readonly Keyword KW_SUPER_NAME = Keyword.intern(null, "super-name");

        IPersistentCollection _methods;
        FnMethod _variadicMethod = null;
        string _name;
        string _simpleName;
        string _internalName;

        string _thisName;
        public string ThisName
        {
            get { return _thisName; }
            set { _thisName = value; }
        }

        Type _fnType;
        readonly object _tag;
        IPersistentMap _closes = PersistentHashMap.EMPTY;          // localbinding -> itself
        public IPersistentMap Closes
        {
            get { return _closes; }
            set { _closes = value; }
        }
        IPersistentMap _keywords = PersistentHashMap.EMPTY;         // Keyword -> KeywordExpr
        IPersistentMap _vars = PersistentHashMap.EMPTY;
        PersistentVector _constants;
        int _constantsID;
        bool _onceOnly = false;
        string _superName = null;

        TypeBuilder _typeBuilder = null;
        public TypeBuilder TypeBuilder
        {
            get { return _typeBuilder; }
        }
        TypeBuilder _baseTypeBuilder = null;
        Type _baseType = null;

        public Type BaseType
        {
            get { return _baseType; }
        }
        ParameterExpression _thisParam = null;
        public ParameterExpression ThisParam
        {
            get { return _thisParam; }
            set { _thisParam = value; }
        }

        ConstructorInfo _ctorInfo;

        List<FieldBuilder> _closedOverFields;
        
        #endregion

        #region Ctors

        public FnExpr(object tag)
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
            get { return _tag != null ? Compiler.TagToType(_tag) : typeof(IFn); }
        }

        #endregion

        #region Misc

        // This naming convention drawn from the Java code.
        internal void ComputeNames(ISeq form)
        {
            FnMethod enclosingMethod = (FnMethod)Compiler.METHODS.deref();

            string baseName = enclosingMethod != null
                ? (enclosingMethod.Fn._name + "$")
                : (Compiler.Munge(Compiler.CurrentNamespace.Name.Name) + "$");

            if (RT.second(form) is Symbol)
                _thisName = ((Symbol)RT.second(form)).Name;

            _simpleName = (_name == null ? "fn" : Compiler.Munge(_name).Replace(".", "_DOT_")) + "__" + RT.nextID();
            _name = baseName + _simpleName;
            _internalName = _name.Replace('.', '/');
            _fnType = RT.classForName(_internalName);
            // fn.fntype = Type.getObjectType(fn.internalName) -- JAVA            
        }

        bool IsVariadic { get { return _variadicMethod != null; } }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(object frm)
            {
                ISeq form = (ISeq)frm;

                FnExpr fn = new FnExpr(Compiler.TagOf(form));

                if (((IMeta)form.first()).meta() != null)
                {
                    fn._onceOnly = RT.booleanCast(RT.get(RT.meta(form.first()), KW_ONCE));
                    fn._superName = (string)RT.get(RT.meta(form.first()), KW_SUPER_NAME);
                }


                fn.ComputeNames(form);

                try
                {
                    Var.pushThreadBindings(RT.map(
                        Compiler.CONSTANTS, PersistentVector.EMPTY,
                        Compiler.KEYWORDS, PersistentHashMap.EMPTY,
                        Compiler.VARS, PersistentHashMap.EMPTY));

                    //arglist might be preceded by symbol naming this fn
                    if (RT.second(form) is Symbol)
                        form = RT.cons(Compiler.FN, RT.next(RT.next(form)));

                    // Normalize body
                    // If it is (fn [arg...] body ...), turn it into
                    //          (fn ([arg...] body...))
                    // so that we can treat uniformly as (fn ([arg...] body...) ([arg...] body...) ... )
                    if (RT.second(form) is IPersistentVector)
                        form = RT.list(Compiler.FN, RT.next(form));


                    FnMethod variadicMethod = null;
                    SortedDictionary<int, FnMethod> methods = new SortedDictionary<int, FnMethod>();

                    for (ISeq s = RT.next(form); s != null; s = RT.next(s))
                    {
                        FnMethod f = FnMethod.Parse(fn, (ISeq)RT.first(s));
                        if (f.IsVariadic)
                        {
                            if (variadicMethod == null)
                                variadicMethod = f;
                            else
                                throw new Exception("Can't have more than 1 variadic overload");
                        }
                        else if (!methods.ContainsKey(f.RequiredArity))
                            methods[f.RequiredArity] = f;
                        else
                            throw new Exception("Can't have 2 overloads with the same arity.");
                    }

                    if (variadicMethod != null && methods.Count > 0 && methods.Keys.Max() >= variadicMethod.NumParams)
                        throw new Exception("Can't have fixed arity methods with more params than the variadic method.");

                    IPersistentCollection allMethods = null;
                    foreach (FnMethod method in methods.Values)
                        allMethods = RT.conj(allMethods, method);
                    if (variadicMethod != null)
                        allMethods = RT.conj(allMethods, variadicMethod);

                    fn._methods = allMethods;
                    fn._variadicMethod = variadicMethod;
                    fn._keywords = (IPersistentMap)Compiler.KEYWORDS.deref();
                    fn._vars = (IPersistentMap)Compiler.VARS.deref();
                    fn._constants = (PersistentVector)Compiler.CONSTANTS.deref();
                    fn._constantsID = RT.nextID();
                }
                finally
                {
                    Var.popThreadBindings();
                }
                // JAVA: fn.compile();
                return fn;                
            }

        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            EnsureTypeBuilt(context);

            //ConstructorInfo ctorInfo = _ctorInfo;
            ConstructorInfo ctorInfo = _fnType.GetConstructors()[0];

            // The incoming context holds info on the containing function.
            // That is the one that holds the closed-over variable values.

            List<Expression> args = new List<Expression>(_closes.count());
            for (ISeq s = RT.keys(_closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                if (lb.PrimitiveType != null)
                    args.Add(context.FnExpr.GenUnboxedLocal(context, lb));
                else
                    args.Add(context.FnExpr.GenLocal(context, lb));
            }

            return Expression.New(ctorInfo, args);
        }


        internal Expression GenLocal(GenContext context, LocalBinding lb)
        {
            if (_closes.containsKey(lb))
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
            if (_closes.containsKey(lb))
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
            _fnType = _typeBuilder.CreateType();
        }
            

        #region  Base class construction

        private TypeBuilder GenerateFnBaseClass(GenContext context)
        {
            Type super = GetSuperType();
            string baseClassName = _internalName + "_base";

            TypeBuilder baseTB = context.ModuleBldr.DefineType(baseClassName, TypeAttributes.Class | TypeAttributes.Public, super);

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

        static readonly ConstructorInfo AFunction_Default_Ctor = typeof(AFunction).GetConstructor(EMPTY_TYPE_ARRAY);
        static readonly ConstructorInfo RestFn_Int_Ctor = typeof(RestFn).GetConstructor(new Type[] { typeof(int) });

        private void GenerateBaseClassConstructor(TypeBuilder baseTB)
        {
            ConstructorBuilder cb = baseTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, EMPTY_TYPE_ARRAY);
            ILGenerator gen = cb.GetILGenerator();
            // Call base constructor
            if (_superName != null)
            {
                Type parentType = Type.GetType(_superName);
                ConstructorInfo cInfo = parentType.GetConstructor(EMPTY_TYPE_ARRAY);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, cInfo);
            }
            else if (IsVariadic)
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldc_I4, _variadicMethod.RequiredArity);
                gen.Emit(OpCodes.Call, RestFn_Int_Ctor);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, AFunction_Default_Ctor);
            }
            gen.Emit(OpCodes.Ret);
        }


        #endregion

        #region Function class construction

        private TypeBuilder GenerateFnClass(GenContext context, Type baseType)
        {
            TypeBuilder fnTB = context.ModuleBldr.DefineType(_internalName, TypeAttributes.Class | TypeAttributes.Public, baseType);
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
                MethodBuilder method = GenerateConstants(fnTB,baseType);
                ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, EMPTY_TYPE_ARRAY);
                ILGenerator gen = cb.GetILGenerator();
                gen.Emit(OpCodes.Call, method);
                gen.Emit(OpCodes.Ret);

            }
        }

        static readonly string STATIC_CTOR_HELPER_NAME = "__static_ctor_helper";

        private MethodBuilder GenerateConstants(TypeBuilder fnTB, Type baseType)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PRINT_DUP, RT.T));

                List<Expression> inits = new List<Expression>();
                for (int i = 0; i < _constants.count(); i++)
                {
                    object o = _constants.nth(i);
                    string stringValue = null;
                    if (o is string)
                        stringValue = (string)o;
                    else
                    {
                        try
                        {
                            stringValue = RT.printString(o);
                        }
                        catch (Exception)
                        {
                            throw new Exception(String.Format("Can't embed object in code, maybe print-dup not defined: {0}", o));
                        }
                        if (stringValue.Length == 0)
                            throw new Exception(String.Format("Can't embed unreadable object in code: " + o));
                        if (stringValue.StartsWith("#<"))
                            throw new Exception(String.Format("Can't embed unreadable object in code: " + stringValue));
                    }
                    Expression init =
                        Expression.Assign(
                            Expression.Field(null, baseType, ConstantName(i)),
                            Expression.Convert(Expression.Call(Compiler.Method_RT_readString, Expression.Constant(stringValue)),
                                               ConstantType(i)));
                    inits.Add(init);
                }
                inits.Add(Expression.Default(typeof(void)));

                Expression block = Expression.Block(inits);
                LambdaExpression lambda = Expression.Lambda(block);
                MethodBuilder methodBuilder = fnTB.DefineMethod(STATIC_CTOR_HELPER_NAME, MethodAttributes.Private | MethodAttributes.Static);
                lambda.CompileToMethod(methodBuilder,Microsoft.Runtime.CompilerServices.DebugInfoGenerator.CreatePdbGenerator());
                return methodBuilder;
            }
            finally
            {
                Var.popThreadBindings();
            }

        }

        private ConstructorBuilder GenerateConstructor(TypeBuilder fnTB, Type baseType)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, CtorTypes());
            ILGenerator gen = cb.GetILGenerator();
            //Call base constructor
            ConstructorInfo baseCtorInfo = baseType.GetConstructor(EMPTY_TYPE_ARRAY);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, baseCtorInfo);

            int a = 0;
            for (ISeq s = RT.keys(_closes); s != null; s = s.next(), a++)
            {
                LocalBinding lb = (LocalBinding)s.first();
                FieldBuilder fb = _closedOverFields[a];

                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg, a + 1);
                gen.Emit(OpCodes.Stfld, fb);
            }
            gen.Emit(OpCodes.Ret);
            return cb;
        }

        private Type[] CtorTypes()
        {
            if (_closes.count() == 0)
                return EMPTY_TYPE_ARRAY;

            Type[] ret = new Type[_closes.count()];
            int i = 0;
            for (ISeq s = RT.keys(_closes); s != null; s = s.next(), i++)
            {
                LocalBinding lb = (LocalBinding)s.first();
                ret[i] = lb.PrimitiveType ?? typeof(object);
            }
            return ret;
        }

        private void GenerateMethods(GenContext context)
        {
            for (ISeq s = RT.seq(_methods); s != null; s = s.next())
            {
                FnMethod method = (FnMethod)s.first();
                method.GenerateCode(context);
            }
        }

        #endregion

        private GenContext CreateContext(GenContext incomingContext,TypeBuilder fnTB,Type baseType)
        {
            return incomingContext.CreateWithNewType(this);
        }

        private Type GetSuperType()
        {
            return _superName != null
                ? Type.GetType(_superName)
                : IsVariadic
                ? typeof(RestFn)
                : typeof(AFunction);
        }

        #endregion

        #region Code generation support

        internal Expression GenConstant(int id)
        {
            return Expression.Field(null, _baseType, ConstantName(id));
        }

        internal Expression GenVar(Var var)
        {
            int i = (int)_vars.valAt(var);
            return GenConstant(i);
        }

        internal Expression GenKeyword(Keyword kw)
        {
            int i = (int)_keywords.valAt(kw);
            return GenConstant(i);
        }

        #endregion
    }
}
