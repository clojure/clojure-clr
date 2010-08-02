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
using System.Runtime.CompilerServices;


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

        private string _internalName;

        internal string InternalName
        {
            get { return _internalName; }
            set { _internalName = value; }
        }

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

        internal bool IsVolatile(LocalBinding lb)
        {
            return RT.booleanCast(RT.contains(_fields, lb.Symbol)) &&
                RT.booleanCast(RT.get(lb.Symbol.meta(), Keyword.intern("volatile-mutable")));
        }

        bool IsMutable(LocalBinding lb)
        {
            return IsVolatile(lb)
                ||
                RT.booleanCast(RT.contains(_fields, lb.Symbol)) &&
                   RT.booleanCast(RT.get(lb.Symbol.meta(), Keyword.intern("unsynchronized-mutable")))
                ||
                lb.IsByRef;
        }

        [NonSerialized]
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

        private Type _objType;

        internal Type ObjType
        {
            get { return _objType; }
            set { _objType = value; }
        }

        protected ParameterExpression _thisParam = null;
        public ParameterExpression ThisParam
        {
            get { return _thisParam; }
            set { _thisParam = value; }
        }

        protected FieldBuilder _metaField;

        protected List<FieldBuilder> _closedOverFields;
        protected Dictionary<LocalBinding, FieldBuilder> _closedOverFieldsMap;

        
        protected List<FieldBuilder> _keywordLookupSiteFields;
        protected List<FieldBuilder> _thunkFields;

        internal FieldBuilder ThunkField(int i)
        {
            return _thunkFields[i];
        }

        internal FieldBuilder KeywordLookupSiteField(int i)
        {
            return _keywordLookupSiteFields[i];
        }


        protected List<FieldBuilder> _cachedTypeFields;
        protected List<FieldBuilder> _cachedProtoFnFields;
        protected List<FieldBuilder> _cachedProtoImplFields;

        internal FieldBuilder CachedTypeField(int i)
        {
            return _cachedTypeFields[i];
        }

        internal FieldBuilder CachedProtoFnField(int i)
        {
            return _cachedProtoFnFields[i];
        }

        internal FieldBuilder CachedProtoImplField(int i)
        {
            return _cachedProtoImplFields[i];
        }

        protected IPersistentCollection _methods;

        protected IPersistentMap _fields = null;

        protected bool IsDefType { get { return _fields != null; } }

        private IPersistentMap _keywords = PersistentHashMap.EMPTY;         // Keyword -> KeywordExpr

        internal IPersistentMap Keywords
        {
            get { return _keywords; }
            set { _keywords = value; }
        }
        private IPersistentMap _vars = PersistentHashMap.EMPTY;

        internal IPersistentMap Vars
        {
            get { return _vars; }
            set { _vars = value; }
        }
        private PersistentVector _constants;

        internal PersistentVector Constants
        {
            get { return _constants; }
            set { _constants = value; }
        }
        protected int _constantsID;



        protected int _altCtorDrops = 0;

        private IPersistentVector _keywordCallsites;

        internal IPersistentVector KeywordCallsites
        {
            get { return _keywordCallsites; }
            set { _keywordCallsites = value; }
        }
        private IPersistentVector _protocolCallsites;

        internal IPersistentVector ProtocolCallsites
        {
            get { return _protocolCallsites; }
            set { _protocolCallsites = value; }
        }
        private IPersistentVector _varCallsites;

        internal IPersistentVector VarCallsites
        {
            get { return _varCallsites; }
            set { _varCallsites = value; }
        }

        protected ConstructorInfo _ctorInfo;
        protected ConstructorInfo _nonmetaCtorInfo;

        protected IPersistentVector _interfaces = PersistentVector.EMPTY;

        protected Object _src;

        protected IPersistentMap _classMeta;

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
            get { return _objType ?? (_tag != null ? HostExpr.TagToType(_tag) : typeof(IFn)); }
        }

        #endregion

        #region Code generation

        public abstract FnMode CompileMode();
            // You cannot allow this to change during parsing.
            // That's why the following does not work.
            //return _protocolCallsites != null && _protocolCallsites.count() > 0
            //    ? FnMode.Full
            //    : FnMode.Light;


        public override Expression GenDlr(GenContext context)
        {
            //switch (context.Mode)
            //{
            //    case CompilerMode.Immediate:
            //        if (_protocolCallsites.count() > 0)
            //        {
            //            context = context.ChangeMode(CompilerMode.File);
            //            return GenDlrForFile(context,true);
            //        }
            //        return GenDlrImmediate(context);
            //    case CompilerMode.File:
            //        return GenDlrForFile(context,true);
            //    default:
            //        throw Util.UnreachableCode();
            //}
            switch (context.FnCompileMode)
            {
                case FnMode.Light:
                    return GenDlrImmediate(context);
                case FnMode.Full:
                    return GenDlrForFile(context, false);      // trying it this way now
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

        #region Class generation

        //protected Type GenerateClass()
        //{
        //    GenContext context = Compiler.COMPILER_CONTEXT.get() as GenContext ?? Compiler.EvalContext;

        //    //if (_protocolCallsites.count() > 0)
        //    //    context = context.ChangeMode(CompilerMode.File);

        //    return GenerateClass(context);
        //}

        protected Type GenerateClass(GenContext context)
        {
            //if (context.Mode == CompilerMode.Immediate)
            //    return GenerateClassForImmediate(context);
            //else
            //    return GenerateClassForFile(context);

            if (context.FnCompileMode == FnMode.Light)
                return GenerateClassForImmediate(context);
            else
                return GenerateClassForFile(context);
        }

        protected abstract Type GenerateClassForImmediate(GenContext context);
        protected abstract Type GenerateClassForFile(GenContext context);



        #endregion

        #region File-mode compilation

        protected Expression GenDlrForFile(GenContext context, bool convertToIFn)
        {
            if (IsDefType)
                return Expression.Constant(null);

            List<Expression> args = new List<Expression>(_closes.count()+1);
            args.Add(Expression.Constant(null,typeof(IPersistentMap))); // meta
            for (ISeq s = RT.keys(_closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                if (lb.PrimitiveType != null)
                    args.Add(context.ObjExpr.GenUnboxedLocal(context, lb));
                else
                    args.Add(context.ObjExpr.GenLocal(context, lb));
            }

            Expression newExpr = Expression.New(_ctorInfo, args);

            if ( convertToIFn )
                newExpr = Expression.Convert(newExpr,typeof(IFn));

            return newExpr;
        }

        static int _saveId = 0;

        protected Type EnsureTypeBuilt(GenContext context)
        {
            if (_objType != null)
                return _objType;

            if ( context.AssyMode == AssemblyMode.Dynamic )
                // TODO: only create a new assembly when we know there is a name conflict
                context = new GenContext("new" + (++_saveId).ToString(), AssemblyMode.Dynamic,FnMode.Full).WithNewDynInitHelper(InternalName + "__dynInitHelper_" + RT.nextID().ToString()).CreateWithNewType(context.ObjExpr);


            TypeBuilder baseTB = GenerateFnBaseClass(context);
            _baseType = baseTB.CreateType();
            // patch this param type

            try
            {
                if (IsDefType)
                {
                    Compiler.RegisterDuplicateType(_baseType);

                    Var.pushThreadBindings(RT.map(
                        Compiler.COMPILE_STUB_ORIG_CLASS, Compiler.COMPILE_STUB_CLASS.deref(),
                        Compiler.COMPILE_STUB_CLASS, _baseType));
                }

                GenerateFnClass(context);
                _objType = _typeBuilder.CreateType();

                if (context.DynInitHelper != null)
                    context.DynInitHelper.FinalizeType();
                _ctorInfo = _objType.GetConstructors()[0];  // TODO: When we have more than one c-tor, we'll have to fix this.
                return _objType;
            }
            finally
            {
                if ( IsDefType )
                    Var.popThreadBindings();
            }
        }

        #endregion

        #region  Base class construction

        private TypeBuilder GenerateFnBaseClass(GenContext context)
        {
            string baseClassName = _internalName + "_base";

            Type[] interfaces = new Type[0];

            if ( _keywordCallsites.count() > 0 )
                interfaces = new Type[] { typeof(ILookupHost) };

            TypeBuilder baseTB = context.ModuleBuilder.DefineType(baseClassName, TypeAttributes.Public | TypeAttributes.Abstract, _superType,interfaces);

            MarkAsSerializable(baseTB);

            GenInterface.SetCustomAttributes(baseTB, _classMeta);

            GenerateConstantFields(baseTB);

            if (!IsDefType)
                _metaField = baseTB.DefineField("__meta", typeof(IPersistentMap), FieldAttributes.Public | FieldAttributes.InitOnly);

            GenerateClosedOverFields(baseTB);
            GenerateVarCallsites(baseTB);
            GenerateKeywordCallsites(baseTB);
            GenerateSwapThunk(baseTB);
            GenerateProtocolCallsites(baseTB);

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
            _closedOverFieldsMap = new Dictionary<LocalBinding, FieldBuilder>(_closes.count());

            // closed-overs map to instance fields.
            for (ISeq s = RT.keys(_closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();

                FieldAttributes attributes = FieldAttributes.Public;
                bool markVolatile = IsVolatile(lb);

                if (IsDefType)
                {
                    if (!IsMutable(lb))
                        attributes |= FieldAttributes.InitOnly;
                }

                Type type = lb.PrimitiveType ?? typeof(object);

                FieldBuilder fb = markVolatile 
                    ? baseTB.DefineField(lb.Name,type, new Type[] { typeof(IsVolatile) }, Type.EmptyTypes, attributes)
                    : baseTB.DefineField(lb.Name, type, attributes);

                GenInterface.SetCustomAttributes(fb, GenInterface.ExtractAttributes(RT.meta(lb.Symbol)));

                _closedOverFields.Add(fb);
                _closedOverFieldsMap[lb] = fb;
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
            int count = _keywordCallsites.count();

            _keywordLookupSiteFields = new List<FieldBuilder>(count);
            _thunkFields = new List<FieldBuilder>(count);

            for (int i = 0; i < _keywordCallsites.count(); i++)
            {
                Keyword k = (Keyword)_keywordCallsites.nth(i);
                string siteName = SiteNameStatic(i);
                string thunkName = ThunkNameStatic(i);
                FieldBuilder fb1 = baseTB.DefineField(siteName, typeof(KeywordLookupSite), FieldAttributes.FamORAssem | FieldAttributes.Static);
                FieldBuilder fb2 = baseTB.DefineField(thunkName, typeof(ILookupThunk), FieldAttributes.FamORAssem | FieldAttributes.Static);
                _keywordLookupSiteFields.Add(fb1);
                _thunkFields.Add(fb2);
            }
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


        // TODO: Avoid going through the static, i.e., define the interface method directly.
        void GenerateSwapThunk(TypeBuilder tb)
        {
            if (_keywordCallsites.count() == 0)
                return;

            MethodBuilder mbs = tb.DefineMethod("swapThunk_static", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int), typeof(ILookupThunk) });

            ParameterExpression pi = Expression.Parameter(typeof(int),"i");
            ParameterExpression pt = Expression.Parameter(typeof(ILookupThunk),"t");

            List<SwitchCase> cases = new List<SwitchCase>(_keywordCallsites.count());
            for ( int i=0; i<_keywordCallsites.count(); i++ )
                cases.Add( 
                    Expression.SwitchCase(
                        Expression.Block(
                            Expression.Assign(Expression.Field(null,_thunkFields[i]),pt),
                            Expression.Default(typeof(void))),
                        Expression.Constant(i)));

            Expression body = Expression.Switch(pi, cases.ToArray<SwitchCase>());
            LambdaExpression lambda = Expression.Lambda(body, pi, pt);
            lambda.CompileToMethod(mbs);

            MethodBuilder mb = tb.DefineMethod("swapThunk", MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int), typeof(ILookupThunk) });
            ILGen gen = new ILGen(mb.GetILGenerator());
            gen.EmitLoadArg(0);
            gen.EmitLoadArg(1);
            gen.EmitCall(mbs);
            gen.Emit(OpCodes.Ret);
        }

        #endregion

        #region Generating protocol callsites

        private void GenerateProtocolCallsites(TypeBuilder baseTB)
        {
            int count = _protocolCallsites.count();

            _cachedTypeFields = new List<FieldBuilder>(count);
            _cachedProtoFnFields = new List<FieldBuilder>(count);
            _cachedProtoImplFields = new List<FieldBuilder>(count);


            for (int i = 0; i < count; i++)
            {
                _cachedTypeFields.Add(baseTB.DefineField(CachedClassName(i), typeof(Type), FieldAttributes.Public));
                _cachedProtoFnFields.Add(baseTB.DefineField(CachedProtoFnName(i), typeof(AFunction), FieldAttributes.Public));
                _cachedProtoImplFields.Add(baseTB.DefineField(CachedProtoImplName(i), typeof(IFn), FieldAttributes.Public));
            }
        }


        internal static String CachedClassName(int n)
        {
            return "__cached_class__" + n;
        }

        internal static String CachedProtoFnName(int n)
        {
            return "__cached_proto_fn__" + n;
        }

        internal static String CachedProtoImplName(int n)
        {
            return "__cached_proto_impl__" + n;
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
            for (int i =0; i<_interfaces.count(); i++ )
                _typeBuilder.AddInterfaceImplementation((Type)_interfaces.nth(i));

            MarkAsSerializable(_typeBuilder);

            GenInterface.SetCustomAttributes(_typeBuilder, _classMeta);

            GenerateStaticConstructor(_typeBuilder, _baseType);
            _ctorInfo = GenerateConstructor(_typeBuilder, _baseType);

            if (_altCtorDrops > 0)
                GenerateFieldOnlyConstructor(_typeBuilder, _baseType);

            if (!IsDefType)
            {
                _nonmetaCtorInfo = GenerateNonMetaConstructor(_typeBuilder, _baseType);
                GenerateMetaFunctions(_typeBuilder);
            }

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
                    Expression.Constant(Compiler.DestubClassName(((Type)value).FullName)));
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
                if (value is IObj && RT.count(((IObj)value).meta()) > 0)
                {
                    Expression objExpr = Expression.Convert(ret, typeof(IObj));
                    Expression metaExpr = Expression.Convert(GenerateValue(((IObj)value).meta()), typeof(IPersistentMap));
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
            if (_keywordCallsites.count() == 0)
                return null;

            List<Expression> inits = new List<Expression>();
            ParameterExpression parm = Expression.Parameter(typeof(KeywordLookupSite), "temp");

            for (int i = 0; i < _keywordCallsites.count(); i++)
            {
                Expression nArg = Expression.Constant(i);
                Expression kArg = GenerateValue(_keywordCallsites.nth(i));
                Expression parmAssign =
                    Expression.Assign(
                        parm,
                        Expression.New(Compiler.Ctor_KeywordLookupSite_2, new Expression[] { nArg, kArg }));
                Expression siteAssign = Expression.Assign(Expression.Field(null, _keywordLookupSiteFields[i]), parm);
                Expression thunkAssign = Expression.Assign(Expression.Field(null, _thunkFields[i]), Expression.Convert(parm,typeof(ILookupThunk)));
                inits.Add(parmAssign);
                inits.Add(siteAssign);
                inits.Add(thunkAssign);
                inits.Add(Expression.Default(typeof(void)));
            }

            Expression allInits = Expression.Block(new ParameterExpression[] { parm }, inits);
            LambdaExpression lambda = Expression.Lambda(allInits);
            MethodBuilder methodBuilder = fnTB.DefineMethod(STATIC_CTOR_HELPER_NAME + "_kwcallsites", MethodAttributes.Private | MethodAttributes.Static,typeof(void),Type.EmptyTypes);
            lambda.CompileToMethod(methodBuilder, true);
            return methodBuilder;

        }


        private void GenerateMetaFunctions(TypeBuilder fnTB)
        {
            // IPersistentMap meta()
            MethodBuilder metaMB = fnTB.DefineMethod("meta", MethodAttributes.Public|MethodAttributes.Virtual|MethodAttributes.ReuseSlot,typeof(IPersistentMap),Type.EmptyTypes);
            ILGen gen = new ILGen(metaMB.GetILGenerator());
            gen.EmitLoadArg(0);
            gen.EmitFieldGet(_metaField);
            gen.Emit(OpCodes.Ret);


            // IObj withMeta(IPersistentMap)
            MethodBuilder withMB = fnTB.DefineMethod("withMeta", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot, typeof(IObj), new Type[] { typeof(IPersistentMap) });
            gen = new ILGen(withMB.GetILGenerator());

            gen.EmitLoadArg(1);   // meta arg
            foreach (FieldBuilder fb in _closedOverFields)
            {
                gen.EmitLoadArg(0);
                gen.EmitFieldGet(fb);
            }

            gen.EmitNew(_ctorInfo);
            gen.Emit(OpCodes.Ret);
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

            // Store Meta
            if (!IsDefType)
            {
                gen.EmitLoadArg(0);
                gen.EmitLoadArg(1);
                gen.Emit(OpCodes.Castclass, typeof(IPersistentMap));
                gen.EmitFieldSet(_metaField);
            }

            // store closed-overs in their fields
            int a = 0;
            int offset = IsDefType ? 1 : 2;

            for (ISeq s = RT.keys(_closes); s != null; s = s.next(), a++)
            {
                LocalBinding lb = (LocalBinding)s.first();
                FieldBuilder fb = _closedOverFields[a];

                gen.EmitLoadArg(0);             // gen.Emit(OpCodes.Ldarg_0);
                gen.EmitLoadArg(a + offset);         // gen.Emit(OpCodes.Ldarg, a + 1);
                gen.Emit(OpCodes.Stfld, fb);
            }
            gen.Emit(OpCodes.Ret);
            return cb;
        }


        private ConstructorBuilder GenerateFieldOnlyConstructor(TypeBuilder fnTB, Type baseType)
        {
            Type[] ctorTypes = CtorTypes();
            Type[] altCtorTypes = new Type[ctorTypes.Length - _altCtorDrops];
            for (int i = 0; i < altCtorTypes.Length; i++)
                altCtorTypes[i] = ctorTypes[i];

            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, altCtorTypes);
            ILGen gen = new ILGen(cb.GetILGenerator());

            //Call full constructor
            gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < altCtorTypes.Length; i++)
                gen.EmitLoadArg(i + 1);

            for (int i = 0; i < _altCtorDrops; i++)
                gen.EmitNull();

            gen.Emit(OpCodes.Call, _ctorInfo);

            gen.Emit(OpCodes.Ret);
            return cb;
        }


        private ConstructorBuilder GenerateNonMetaConstructor(TypeBuilder fnTB, Type baseType)
        {
            Type[] ctorTypes = CtorTypes();
            Type[] noMetaCtorTypes = new Type[ctorTypes.Length - 1];
            for (int i = 1; i < ctorTypes.Length; i++)
                noMetaCtorTypes[i - 1] = ctorTypes[i];

            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, noMetaCtorTypes);
            ILGen gen = new ILGen(cb.GetILGenerator());

            gen.EmitLoadArg(0);
            gen.EmitNull();     // null meta
            for (int i = 0; i < noMetaCtorTypes.Length; i++)
                gen.EmitLoadArg(i + 1);
            gen.Emit(OpCodes.Call, _ctorInfo);
            gen.Emit(OpCodes.Ret);

            return cb;
        }


        internal Type[] CtorTypes()
        {

            int i = IsDefType ? 0 : 1;

            Type[] ret = new Type[_closes.count()+i];

            if (!IsDefType)
                ret[0] = typeof(IPersistentMap);

            for (ISeq s = RT.keys(_closes); s != null; s = s.next(), i++)
            {
                LocalBinding lb = (LocalBinding)s.first();
                ret[i] = lb.PrimitiveType ?? typeof(object);
            }
            return ret;
        }

        #endregion

        #region other

        internal static void MarkAsSerializable(TypeBuilder tb)
        {
            tb.SetCustomAttribute(new CustomAttributeBuilder(Compiler.Ctor_Serializable, new object[0]));
        }


        protected abstract void GenerateMethods(GenContext context);

        protected GenContext CreateContext(GenContext incomingContext, TypeBuilder fnTB, Type baseType)
        {
            return incomingContext.CreateWithNewType(this);
        }

        #endregion

        #endregion

        #region Code generation support

        internal Expression GenAssignLocal(GenContext context, LocalBinding lb, Expr val)
        {
            if (!IsMutable(lb))
                throw new ArgumentException("Cannot assign to non-mutable: " + lb.Name);

            FieldBuilder fb;
            if ( _closedOverFieldsMap.TryGetValue(lb,out fb) )
                return Expression.Assign(Expression.Field(_thisParam,_closedOverFieldsMap[lb]), val.GenDlr(context));
            return Expression.Assign(lb.ParamExpression, val.GenDlr(context));
        }

        internal Expression GenLocal(GenContext context, LocalBinding lb)
        {
            //if (context.Mode == CompilerMode.File && _closes.containsKey(lb))
            if ( context.FnCompileMode == FnMode.Full && _closes.containsKey(lb))
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
            //if (context.Mode == CompilerMode.File && _closes.containsKey(lb))
            if (context.FnCompileMode == FnMode.Full && _closes.containsKey(lb))
                return Expression.Convert(Expression.Field(_thisParam, lb.Name), primType);
            else
                return lb.ParamExpression;
        }


        internal Expression GenConstant(GenContext context, int id, object val)
        {
            //switch (context.Mode)
            //{
            //    case CompilerMode.Immediate:
            //        return Expression.Constant(val);
            //    case CompilerMode.File:
            //        return Expression.Field(null, _baseType, ConstantName(id));
            //    default:
            //        throw Util.UnreachableCode();
            //}
            switch ( context.FnCompileMode )
            {
                case FnMode.Light:
                    return Expression.Constant(val);
                case FnMode.Full:
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
    }
}
