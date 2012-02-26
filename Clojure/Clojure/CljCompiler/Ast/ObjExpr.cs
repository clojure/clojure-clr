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
using System.Text.RegularExpressions;


namespace clojure.lang.CljCompiler.Ast
{
    #region Enums

    /// <summary>
    /// Indicates whether we need full class generation for the current function
    /// </summary>
    public enum FnMode
    {
        /// <summary>
        /// The current ObjExpr is not generating its own class
        /// </summary>
        Light,

        /// <summary>
        /// The current ObjExpr is generating its own class
        /// </summary>
        Full
    };

    #endregion

    public class ObjExpr : Expr
    {
        #region Data

        const string ConstPrefix = "const__";
        const string StaticCtorHelperName = "__static_ctor_helper";

        public string InternalName { get; set; }
        protected string _name;
        public string Name { get { return _name; } }
        protected readonly object _tag;

        public IPersistentMap Closes { get; set; }
        public IPersistentMap Keywords { get; set; }
        public IPersistentMap Vars { get; set; }
        public PersistentVector Constants { get; set; }

        Dictionary<int, FieldBuilder> _constantFields;
        protected IPersistentMap _fields { get; set; }            // symbol -> lb

        Type _compiledType;
        protected Type CompiledType
        {
            get
            {
                if (_compiledType == null)
                    // can't do much
                    // Java will get the loader and define the clas from the stored bytecodes
                    // Not sure what the equivalent would be.
                    throw new InvalidOperationException("ObjExpr type not compiled");
                return _compiledType;
            }
            set { _compiledType = value; }
        }

        protected IPersistentMap _classMeta;

        protected TypeBuilder _typeBuilder;
        public TypeBuilder TypeBlder { get { return _typeBuilder; } }

        protected ConstructorInfo _ctorInfo;

        public IPersistentVector KeywordCallsites { get; set; }
        List<FieldBuilder> _keywordLookupSiteFields;
        List<FieldBuilder> _thunkFields;

        public IPersistentVector ProtocolCallsites { get; set; }
        List<FieldBuilder> _cachedTypeFields;
        List<FieldBuilder> _cachedProtoFnFields;
        List<FieldBuilder> _cachedProtoImplFields;

        FieldBuilder _metaField;
        List<FieldBuilder> _closedOverFields;
        Dictionary<LocalBinding, FieldBuilder> _closedOverFieldsMap;

        protected int _altCtorDrops;

        protected IPersistentCollection _methods;

        protected int _constantsID;
        protected bool _onceOnly;
        protected Object _src;
        protected bool _isStatic;
        public bool IsStatic { get { return _isStatic; } }


 

        protected bool IsDefType { get { return _fields != null; } }
        protected virtual bool SupportsMeta { get { return !IsDefType; } }

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

        static String SiteName(int n)
        {
            return "__site__" + n;
        }

        public static String SiteNameStatic(int n)
        {
            return SiteName(n) + "__";
        }

        static String ThunkName(int n)
        {
            return "__thunk__" + n;
        }

        public static String ThunkNameStatic(int n)
        {
            return ThunkName(n) + "__";
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

        internal FieldBuilder KeywordLookupSiteField(int i)
        {
            return _keywordLookupSiteFields[i];
        }

        internal FieldBuilder CachedTypeField(int i)
        {
            return _cachedTypeFields[i];
        }

        internal FieldBuilder ThunkField(int i)
        {
            return _thunkFields[i];
        }


        // OLD ONLY

        
        protected string _thisName;
        protected IPersistentVector _closesExprs = PersistentVector.EMPTY;  // localbinding exprs
        protected IPersistentSet _volatiles = PersistentHashSet.EMPTY;      // symbols
        protected IPersistentVector _hintedFields = PersistentVector.EMPTY; // hinted fields
        // int line;
 
        protected Type _baseType = null;

        
        protected ParameterExpression _thisParam = null;

        private FnMode _fnMode = FnMode.Full;

        public FnMode FnMode
        {
            get { return _fnMode; }
            set { _fnMode = value; }
        }

 

        private IPersistentSet _varCallsites;




        
        #endregion

        #region Data accessors

        public string ThisName
        {
            get { return _thisName; }
            //set { _thisName = value; }
        }

        internal IPersistentSet VarCallsites
        {
            get { return _varCallsites; }
            set { _varCallsites = value; }
        }

        public Type BaseType
        {
            get { return _baseType; }
        }

        public ParameterExpression ThisParam
        {
            get { return _thisParam; }
            set { _thisParam = value; }
        }

        internal FieldBuilder CachedProtoFnField(int i)
        {
            return _cachedProtoFnFields[i];
        }

        internal FieldBuilder CachedProtoImplField(int i)
        {
            return _cachedProtoImplFields[i];
        }






        #endregion

        #region C-tors

        public ObjExpr(object tag)
        {
            _tag = tag;
            Keywords = PersistentHashMap.EMPTY;
            Vars = PersistentHashMap.EMPTY;
            Closes = PersistentHashMap.EMPTY;
        }

        #endregion

        #region Type mangling

        public virtual bool HasClrType
        {
            get { return true; }
        }

        public virtual Type ClrType
        {
            get { return _compiledType ?? (_tag != null ? HostExpr.TagToType(_tag) : typeof(IFn)); }
        }

        #endregion

        #region Misc support

        public static string TrimGenId(string name)
        {
            int i = name.LastIndexOf("__");
            return i == -1 ? name : name.Substring(0, i);
        }

        internal Type[] CtorTypes()
        {
            int i = !SupportsMeta ? 0 : 1;

            Type[] ret = new Type[Closes.count() + i];

            if (SupportsMeta)
                ret[0] = typeof(IPersistentMap);

            for (ISeq s = RT.keys(Closes); s != null; s = s.next(), i++)
            {
                LocalBinding lb = (LocalBinding)s.first();
                ret[i] = lb.PrimitiveType ?? typeof(object);
            }
            return ret;
        }

        #endregion

        #region Compiling (class generation)

        public Type Compile(Type superType, Type stubType, IPersistentVector interfaces, bool onetimeUse, GenContext context)
        {
            if (_compiledType != null)
                return _compiledType;

            TypeBuilder baseTB = GenerateFnBaseClass(superType,context);
            _baseType = baseTB.CreateType();

            // patch this param type

            try
            {
                if (IsDefType)
                {
                    Compiler.RegisterDuplicateType(_baseType);

                    Var.pushThreadBindings(RT.map(
                        Compiler.CompileStubOrigClassVar, stubType
                        ));
                        //,
                        //Compiler.COMPILE_STUB_CLASS, _baseType));
                }

                GenerateFnClass(interfaces, context);
                _compiledType = _typeBuilder.CreateType();

                if (context.DynInitHelper != null)
                    context.DynInitHelper.FinalizeType();

                _ctorInfo = _compiledType.GetConstructors()[0];  // TODO: When we have more than one c-tor, we'll have to fix this.
                return _compiledType;
            }
            finally
            {
                if ( IsDefType )
                    Var.popThreadBindings();
            }
        }
        #endregion

        #region  Base class construction

        private TypeBuilder GenerateFnBaseClass(Type superType, GenContext context)
        {
            string baseClassName = InternalName + "__base" + (IsDefType || (_isStatic && Compiler.IsCompiling) ? "" : "__" + RT.nextID().ToString());

            //Console.WriteLine("DefStaticFn {0}, {1}", baseClassName, context.AssemblyBuilder.GetName().Name);

            Type[] interfaces = new Type[0];

            TypeBuilder baseTB = context.ModuleBuilder.DefineType(baseClassName, TypeAttributes.Public | TypeAttributes.Abstract, superType, interfaces);
            MarkAsSerializable(baseTB);
            GenInterface.SetCustomAttributes(baseTB, _classMeta);

            GenerateConstantFields(baseTB);

            if (SupportsMeta)
                _metaField = baseTB.DefineField("__meta", typeof(IPersistentMap), FieldAttributes.Public | FieldAttributes.InitOnly);

            GenerateClosedOverFields(baseTB);
            //GenerateVarCallsites(baseTB);
            //GenerateCachedVarFields(baseTB);
            GenerateKeywordCallsites(baseTB);
            GenerateSwapThunk(baseTB);
            GenerateProtocolCallsites(baseTB);

            GenerateBaseClassMethods(baseTB, context);

            GenerateBaseClassConstructor(superType,baseTB);

            return baseTB;
        }

        #region Generating constant fields

        internal void GenerateConstantFields(TypeBuilder baseTB)
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

        #endregion

        #region Generating closed-over fields

        private void GenerateClosedOverFields(TypeBuilder baseTB)
        {
            _closedOverFields = new List<FieldBuilder>(Closes.count());
            _closedOverFieldsMap = new Dictionary<LocalBinding, FieldBuilder>(Closes.count());

            // closed-overs map to instance fields.
            for (ISeq s = RT.keys(Closes); s != null; s = s.next())
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
                    ? baseTB.DefineField(lb.Name, type, new Type[] { typeof(IsVolatile) }, Type.EmptyTypes, attributes)
                    : baseTB.DefineField(lb.Name, type, attributes);

                GenInterface.SetCustomAttributes(fb, GenInterface.ExtractAttributes(RT.meta(lb.Symbol)));

                _closedOverFields.Add(fb);
                _closedOverFieldsMap[lb] = fb;
            }
        }

        #endregion

        #region Generating KeywordCallSites

        private void GenerateKeywordCallsites(TypeBuilder baseTB)
        {
            int count = KeywordCallsites.count();

            _keywordLookupSiteFields = new List<FieldBuilder>(count);
            _thunkFields = new List<FieldBuilder>(count);

            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                //Keyword k = (Keyword)_keywordCallsites.nth(i);
                string siteName = SiteNameStatic(i);
                string thunkName = ThunkNameStatic(i);
                FieldBuilder fb1 = baseTB.DefineField(siteName, typeof(KeywordLookupSite), FieldAttributes.FamORAssem | FieldAttributes.Static);
                FieldBuilder fb2 = baseTB.DefineField(thunkName, typeof(ILookupThunk), FieldAttributes.FamORAssem | FieldAttributes.Static);
                _keywordLookupSiteFields.Add(fb1);
                _thunkFields.Add(fb2);
            }
        }



        static string CachedVarName(int n)
        {
            return "__cached_var__" + n;
        }


        // TODO: Avoid going through the static, i.e., define the interface method directly.
        void GenerateSwapThunk(TypeBuilder tb)
        {
            if (KeywordCallsites.count() == 0)
                return;

            MethodBuilder mbs = tb.DefineMethod("swapThunk_static", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { typeof(int), typeof(ILookupThunk) });

            ParameterExpression pi = Expression.Parameter(typeof(int), "i");
            ParameterExpression pt = Expression.Parameter(typeof(ILookupThunk), "t");

            List<SwitchCase> cases = new List<SwitchCase>(KeywordCallsites.count());
            for (int i = 0; i < KeywordCallsites.count(); i++)
                cases.Add(
                    Expression.SwitchCase(
                        Expression.Block(
                            Expression.Assign(Expression.Field(null, _thunkFields[i]), pt),
                            Expression.Default(typeof(void))),
                        Expression.Constant(i)));

            Expression body = Expression.Switch(pi, cases.ToArray<SwitchCase>());
            LambdaExpression lambda = Expression.Lambda(body, pi, pt);
            lambda.CompileToMethod(mbs);

            MethodBuilder mb = tb.DefineMethod("swapThunk", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int), typeof(ILookupThunk) });
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
            int count = ProtocolCallsites.count();

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

        #endregion

        #region Generating base class c-tor

        private static void GenerateBaseClassConstructor(Type superType, TypeBuilder baseTB)
        {
            ConstructorInfo ci = superType.GetConstructor(Type.EmptyTypes);

            if (ci == null)
                return;

            ConstructorBuilder cb = baseTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            ILGen gen = new ILGen(cb.GetILGenerator());

            gen.EmitLoadArg(0);
            gen.Emit(OpCodes.Call, ci);
            gen.Emit(OpCodes.Ret);
        }

        #endregion

        #region base class methods

        protected virtual void GenerateBaseClassMethods(TypeBuilder baseTB, GenContext context)
        {
        }

        #endregion

        #endregion

        #region Fn class construction

        private void GenerateFnClass(IPersistentVector interfaces, GenContext context)
        {
            string publicTypeName = IsDefType || (_isStatic && Compiler.IsCompiling) ? InternalName : InternalName + "__" + RT.nextID();

            //Console.WriteLine("DefFn {0}, {1}", publicTypeName, context.AssemblyBuilder.GetName().Name);

            _typeBuilder = context.AssemblyGen.DefinePublicType(publicTypeName, _baseType, true);
            for (int i = 0; i < interfaces.count(); i++)
                _typeBuilder.AddInterfaceImplementation((Type)interfaces.nth(i));

            MarkAsSerializable(_typeBuilder);

            GenInterface.SetCustomAttributes(_typeBuilder, _classMeta);

            GenerateStaticConstructor(_typeBuilder, _baseType, context.IsDebuggable);
            _ctorInfo = GenerateConstructor(_typeBuilder, _baseType);

            if (_altCtorDrops > 0)
                GenerateFieldOnlyConstructor(_typeBuilder, _baseType);

            if (SupportsMeta)
            {
               /*_nonMetaCtorInfo = */ GenerateNonMetaConstructor(_typeBuilder, _baseType);
            }
            GenerateMetaFunctions(_typeBuilder);
            GenerateStatics(context);
            GenerateMethods(context);
        }


        private void GenerateStaticConstructor(TypeBuilder fnTB, Type baseType, bool isDebuggable)
        {
            if (Constants.count() > 0)
            {
                ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
                MethodBuilder method1 = GenerateConstants(fnTB, baseType, isDebuggable);
                MethodBuilder method3 = GenerateKeywordCallsiteInit(fnTB, baseType, isDebuggable);
                ILGen gen = new ILGen(cb.GetILGenerator());
                gen.EmitCall(method1);       // gen.Emit(OpCodes.Call, method1);
                if (method3 != null)
                    gen.EmitCall(method3);
                gen.Emit(OpCodes.Ret);

            }
        }


        #region Generating constants

        private MethodBuilder GenerateConstants(TypeBuilder fnTB, Type baseType, bool isDebuggable)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PrintDupVar, true));

                List<Expression> inits = new List<Expression>();
                for (int i = 0; i < Constants.count(); i++)
                {
                    Expression expr = GenerateValue(Constants.nth(i));
                    if (!expr.Type.IsPrimitive)
                    {
                        Expression init =
                            Expression.Assign(
                                Expression.Field(null, baseType, ConstantName(i)),
                                Expression.Convert(expr, ConstantType(i)));
                        inits.Add(init);
                    }
                }
                inits.Add(Expression.Default(typeof(void)));

                Expression block = Expression.Block(inits);
                LambdaExpression lambda = Expression.Lambda(block);
                MethodBuilder methodBuilder = fnTB.DefineMethod(StaticCtorHelperName + "_constants", MethodAttributes.Private | MethodAttributes.Static);
                lambda.CompileToMethod(methodBuilder, isDebuggable );
                return methodBuilder;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        protected Expression GenerateValue(object value)
        {
            bool partial = true;
            Expression ret;

            if (value == null)
                ret = Expression.Constant(null);

            else if (value is String)
                ret = Expression.Constant((String)value);
            else if (value is Boolean)
                ret = Expression.Constant((Boolean)value);
            else if (Util.IsPrimitive(value.GetType()))  // or just IsNumeric?
                ret = Expression.Constant(value);
            else if (value is Type)
            {
                Type t = (Type)value;
                if (t.IsValueType)
                    ret = Expression.Constant(t, typeof(Type));
                else
                    ret = Expression.Call(
                        null,
                        Compiler.Method_RT_classForName,
                        Expression.Constant(Compiler.DestubClassName(((Type)value).FullName)));
            }
            else if (value is Symbol)
            {
                Symbol sym = (Symbol)value;
                ret = Expression.Call(
                    null,
                    Compiler.Method_Symbol_intern2,
                    Expression.Convert(Expression.Constant(sym.Namespace), typeof(string)),  // can be null
                    Expression.Constant(sym.Name));
            }
            else if (value is Keyword)
            {
                Keyword keyword = (Keyword)value;
                ret = Expression.Call(
                    null,
                    Compiler.Method_RT_keyword,
                    Expression.Convert(Expression.Constant(keyword.Namespace), typeof(string)),  // can be null
                    Expression.Constant(keyword.Name));
            }
            else if (value is Var)
            {
                Var var = (Var)value;
                ret = Expression.Call(
                    null,
                    Compiler.Method_RT_var2,
                    Expression.Constant(var.Namespace.Name.ToString()),
                    Expression.Constant(var.Symbol.Name.ToString()));
            }
            else if (value is IType)
            {
                IPersistentVector fields = (IPersistentVector)Reflector.InvokeStaticMethod(value.GetType(), "getBasis", Type.EmptyTypes);
                List<Expression> args = new List<Expression>();

                for (ISeq s = RT.seq(fields); s != null; s = s.next())
                {
                    Symbol field = (Symbol)s.first();
                    Type k = Compiler.TagType(Compiler.TagOf(field));
                    object val = Reflector.GetInstanceFieldOrProperty(value,field.Name);
                    Expression expr = GenerateValue(val);
                    if (k.IsPrimitive)
                        expr = Expression.Convert(expr, k);
                    else
                        expr = Compiler.MaybeBox(expr);
                    args.Add(expr);
                }

                ConstructorInfo cinfo = value.GetType().GetConstructors()[0];
                ret = Expression.New(cinfo, args);
            }
            else if (value is IRecord)
            {
                //MethodInfo[] minfos = value.GetType().GetMethods(BindingFlags.Static | BindingFlags.Public);
                ret = Expression.Call(
                    value.GetType(),
                    "create",
                    Type.EmptyTypes,
                    //new Type[] { typeof(IPersistentMap) },
                    GenerateValue(PersistentArrayMap.create((IDictionary)value)));
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
            else if (value is PersistentHashSet)
            {
                ISeq vs = RT.seq(value);
                if (vs == null)
                    ret = Expression.Field(null, Compiler.Method_PersistentHashSet_EMPTY);
                else
                {
                    Expression expr = GenerateListAsObjectArray(vs);
                    ret = Expression.Call(
                        null,
                        Compiler.Method_PersistentHashSet_create,
                        expr);
                }
            }
            else if (value is ISeq || value is IPersistentList)
            {
                Expression expr = GenerateListAsObjectArray(value);
                ret = Expression.Call(
                    null,
                    Compiler.Method_PersistentList_create,
                    expr);
            }
            else if (value is Regex)
            {
                ret = Expression.New(
                    Compiler.Ctor_Regex_1,
                    Expression.Constant(((Regex)value).ToString()));
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
                    throw new InvalidOperationException(String.Format("Can't embed object in code, maybe print-dup not defined: {0}", value));
                }
                if (cs.Length == 0)
                    throw new InvalidOperationException(String.Format("Can't embed unreadable object in code: " + value));
                if (cs.StartsWith("#<"))
                    throw new InvalidOperationException(String.Format("Can't embed unreadable object in code: " + cs));

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

        private MethodBuilder GenerateKeywordCallsiteInit(TypeBuilder fnTB, Type baseType, bool isDebuggable)
        {
            if (KeywordCallsites.count() == 0)
                return null;

            List<Expression> inits = new List<Expression>();
            ParameterExpression parm = Expression.Parameter(typeof(KeywordLookupSite), "temp");

            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                Expression kArg = GenerateValue(KeywordCallsites.nth(i));
                Expression parmAssign =
                    Expression.Assign(
                        parm,
                        Expression.New(Compiler.Ctor_KeywordLookupSite_1, new Expression[] { kArg }));
                Expression siteAssign = Expression.Assign(Expression.Field(null, _keywordLookupSiteFields[i]), parm);
                Expression thunkAssign = Expression.Assign(Expression.Field(null, _thunkFields[i]), Expression.Convert(parm, typeof(ILookupThunk)));
                inits.Add(parmAssign);
                inits.Add(siteAssign);
                inits.Add(thunkAssign);
                inits.Add(Expression.Default(typeof(void)));
            }

            Expression allInits = Expression.Block(new ParameterExpression[] { parm }, inits);
            LambdaExpression lambda = Expression.Lambda(allInits);
            MethodBuilder methodBuilder = fnTB.DefineMethod(StaticCtorHelperName + "_kwcallsites", MethodAttributes.Private | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            lambda.CompileToMethod(methodBuilder, isDebuggable);
            return methodBuilder;

        }

        private void GenerateMetaFunctions(TypeBuilder fnTB)
        {
            // IPersistentMap meta()
            MethodBuilder metaMB = fnTB.DefineMethod("meta", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot, typeof(IPersistentMap), Type.EmptyTypes);
            ILGen gen = new ILGen(metaMB.GetILGenerator());
            if (SupportsMeta)
            {
                gen.EmitLoadArg(0);
                gen.EmitFieldGet(_metaField);
            }
            else
                gen.EmitNull();
            gen.Emit(OpCodes.Ret);


            // IObj withMeta(IPersistentMap)
            MethodBuilder withMB = fnTB.DefineMethod("withMeta", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot, typeof(IObj), new Type[] { typeof(IPersistentMap) });
            gen = new ILGen(withMB.GetILGenerator());

            if (SupportsMeta)
            {
                gen.EmitLoadArg(1);   // meta arg
                foreach (FieldBuilder fb in _closedOverFields)
                {
                    gen.EmitLoadArg(0);
                    gen.EmitFieldGet(fb);
                }

                gen.EmitNew(_ctorInfo);
            }
            else
                gen.EmitLoadArg(0);  //this
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
            if (SupportsMeta)
            {
                gen.EmitLoadArg(0);
                gen.EmitLoadArg(1);
                gen.Emit(OpCodes.Castclass, typeof(IPersistentMap));
                gen.EmitFieldSet(_metaField);
            }

            // store closed-overs in their fields
            int a = 0;
            int offset = !SupportsMeta ? 1 : 2;

            for (ISeq s = RT.keys(Closes); s != null; s = s.next(), a++)
            {
                //LocalBinding lb = (LocalBinding)s.first();
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

        #endregion

        #region other

        internal static void MarkAsSerializable(TypeBuilder tb)
        {
            tb.SetCustomAttribute(new CustomAttributeBuilder(Compiler.Ctor_Serializable, new object[0]));
        }

        protected virtual void GenerateMethods(GenContext context)
        {
        }

        protected virtual void GenerateStatics(GenContext context)
        {
        }

        #endregion

        #endregion

        #region Eval

        public virtual object Eval()
        {
            if (IsDefType)
                return null;
            return Activator.CreateInstance(CompiledType);
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context, bool convertToIFn)
        {
            Expression newExpr = GenCode(rhc,objx,context);
            if ( convertToIFn )
                newExpr = Expression.Convert(newExpr,typeof(IFn));

            return newExpr;
        }

        public virtual Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            if (IsDefType)
                return Expression.Constant(null);

            List<Expression> args = new List<Expression>(Closes.count()+1);
            if (SupportsMeta)
                args.Add(Expression.Constant(null,typeof(IPersistentMap))); // meta
            for (ISeq s = RT.keys(Closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                if (lb.PrimitiveType != null)
                    args.Add(objx.GenUnboxedLocal(context, lb));
                else
                    args.Add(objx.GenLocal(context, lb));
            }

            Expression newExpr = Expression.New(_ctorInfo, args);

            return newExpr;
        }


        protected static MethodBuilder GenerateHasArityMethod(TypeBuilder tb, IList<int> arities, bool isVariadic, int reqArity)
        {
            MethodBuilder mb = tb.DefineMethod(
                "HasArity",
                MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(bool),
                new Type[] { typeof(int) });

            ILGen gen = new ILGen(mb.GetILGenerator());

            Label falseLabel = gen.DefineLabel();
            Label trueLabel = gen.DefineLabel();

            if (isVariadic)
            {
                gen.EmitLoadArg(1);
                gen.EmitInt(reqArity);
                gen.Emit(OpCodes.Bge,trueLabel);
            }

            if (arities != null)
            {
                foreach (int i in arities)
                {
                    gen.EmitLoadArg(1);
                    gen.EmitInt(i);
                    gen.Emit(OpCodes.Beq, trueLabel);
                }
            }

            gen.MarkLabel(falseLabel);
            gen.EmitBoolean(false);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(trueLabel);
            gen.EmitBoolean(true);
            gen.Emit(OpCodes.Ret);

            return mb;
        }

        #endregion
 
        #region Code generation support

        internal Expression GenAssignLocal(GenContext context, LocalBinding lb, Expr val)
        {
            if (!IsMutable(lb))
                throw new ArgumentException("Cannot assign to non-mutable: " + lb.Name);

            FieldBuilder fb;
            if ( _closedOverFieldsMap.TryGetValue(lb,out fb) )
                return Expression.Assign(Expression.Field(_thisParam,_closedOverFieldsMap[lb]), val.GenCode(RHC.Expression,this,context));

            return Expression.Assign(lb.ParamExpression, val.GenCode(RHC.Expression,this,context));
        }

        internal Expression GenLocal(GenContext context, LocalBinding lb)
        {
            if ( _fnMode == FnMode.Full )
            {
                if (Closes.containsKey(lb))
                {
                    Expression expr = Expression.Field(_thisParam, lb.Name);
                    Type primtType = lb.PrimitiveType;
                    if (primtType != null)
                        expr = Compiler.MaybeBox(Expression.Convert(expr, primtType));
                    return expr;
                }
                else
                    return lb.ParamExpression;
            }
            else
            {
                return lb.ParamExpression;
            }
        }

        internal Expression GenUnboxedLocal(GenContext context, LocalBinding lb)
        {
            Type primType = lb.PrimitiveType;
            if (Closes.containsKey(lb) && _fnMode == FnMode.Full)
                return Expression.Convert(Expression.Field(_thisParam, lb.Name), primType);
            else
                return lb.ParamExpression;
        }


        internal Expression GenConstant(GenContext context, int id, object val)
        {
            if (_fnMode == FnMode.Full && ! val.GetType().IsPrimitive)
                return Expression.Field(null, _baseType, ConstantName(id));

            return Expression.Constant(val);
        }

        internal Expression GenVar(GenContext context, Var var)
        {
            int i = (int)Vars.valAt(var);
            return GenConstant(context, i, var);
        }

        internal Expression GenVarValue(GenContext context, Var v)
        {
            int i = (int)Vars.valAt(v);
            if (_fnMode == Ast.FnMode.Full && !v.isDynamic())
            {
                //Type ft = _varCallsites.contains(v) ? typeof(IFn) : typeof(Object);
                //return Expression.Field(null, _cachedVarFields[i]);
                return Expression.Call(GenConstant(context, i, v), Compiler.Method_Var_getRawRoot);
            }
            else
            {
                return Expression.Call(GenConstant(context, i, v), Compiler.Method_Var_get);
            }
        }

        internal Expression GenKeyword(GenContext context, Keyword kw)
        {
            int i = (int)Keywords.valAt(kw);
            return GenConstant(context, i, kw);
        }

        internal Expression GenLetFnInits(GenContext context, ParameterExpression parm, ObjExpr objx, IPersistentSet letFnLocals)
        {
            ParameterExpression cvtParm = Expression.Parameter(_compiledType,"cvt");
            Expression initExpr = Expression.Assign(cvtParm,Expression.Convert(parm, _compiledType));

            List<Expression> exprs = new List<Expression>();
            exprs.Add(initExpr);

            for (ISeq s = RT.keys(Closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                if (letFnLocals.contains(lb))
                {
                    FieldBuilder fb;
                    _closedOverFieldsMap.TryGetValue(lb,out fb);

                    Type primt = lb.PrimitiveType;
                    Expression init = primt != null ? objx.GenUnboxedLocal(context, lb) : objx.GenLocal(context,lb);

                    exprs.Add(Expression.Assign(Expression.Field(_thisParam,fb), init));
                }
            }

            return Expression.Block(new ParameterExpression[] { cvtParm }, exprs);           
        }


        #endregion

        #region no-DLR code gen

        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            //emitting a Fn means constructing an instance, feeding closed-overs from enclosing scope, if any
            //objx arg is enclosing objx, not this

            ILGenerator ilg = context.GetILGenerator();

            if (IsDefType)
                ilg.Emit(OpCodes.Ldnull);
            else
            {
                if (SupportsMeta)
                {
                    ilg.Emit(OpCodes.Ldnull);
                    ilg.Emit(OpCodes.Castclass, typeof(IPersistentMap));
                }

                for (ISeq s = RT.keys(Closes); s != null; s = s.next())
                {
                    LocalBinding lb = (LocalBinding)s.first();
                    if (lb.PrimitiveType != null)
                        objx.EmitUnboxedLocal(context, lb);
                    else
                        objx.EmitLocal(context, lb);
                }

                ilg.Emit(OpCodes.Newobj, _ctorInfo);
            }

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }


        public Type CompileNoDlr(Type superType, IPersistentVector interfaces, bool onetimeUse, GenContext context)
        {
            if (_compiledType != null)
                return _compiledType;

            string publicTypeName = IsDefType || (_isStatic && Compiler.IsCompiling) ? InternalName : InternalName + "__" + RT.nextID();

            //Console.WriteLine("DefFn {0}, {1}", publicTypeName, context.AssemblyBuilder.GetName().Name);

            _typeBuilder = context.AssemblyGen.DefinePublicType(publicTypeName, superType, true);
            if (interfaces != null)
            {
                for (int i = 0; i < interfaces.count(); i++)
                    _typeBuilder.AddInterfaceImplementation((Type)interfaces.nth(i));
            }

            ObjExpr.MarkAsSerializable(_typeBuilder);
            GenInterface.SetCustomAttributes(_typeBuilder, _classMeta);

            EmitConstantFieldDefs(_typeBuilder);
            EmitKeywordCallsiteDefs(_typeBuilder);

            EmitStaticConstructor(_typeBuilder);

            if (SupportsMeta)
                _metaField = _typeBuilder.DefineField("__meta", typeof(IPersistentMap), FieldAttributes.Public | FieldAttributes.InitOnly);

            EmitClosedOverFields(_typeBuilder);
            EmitProtocolCallsites(_typeBuilder);

            _ctorInfo = EmitConstructor(_typeBuilder, superType);

            if (_altCtorDrops > 0)
                EmitFieldOnlyConstructor(_typeBuilder, superType);

            if (SupportsMeta)
            {
                EmitNonMetaConstructor(_typeBuilder, superType);
                EmitMetaFunctions(_typeBuilder);
            }

            EmitStatics(context);
            EmitMethods(context);

            //if (KeywordCallsites.count() > 0)
            //    EmitSwapThunk(_typeBuilder);

            _compiledType = _typeBuilder.CreateType();

            if (context.DynInitHelper != null)
                context.DynInitHelper.FinalizeType();

            return _compiledType;
        }

        void EmitKeywordCallsiteDefs(TypeBuilder baseTB)
        {
            int count = KeywordCallsites.count();

            _keywordLookupSiteFields = new List<FieldBuilder>(count);
            _thunkFields = new List<FieldBuilder>(count);

            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                //Keyword k = (Keyword)_keywordCallsites.nth(i);
                string siteName = SiteNameStatic(i);
                string thunkName = ThunkNameStatic(i);
                FieldBuilder fb1 = baseTB.DefineField(siteName, typeof(KeywordLookupSite), FieldAttributes.FamORAssem | FieldAttributes.Static);
                FieldBuilder fb2 = baseTB.DefineField(thunkName, typeof(ILookupThunk), FieldAttributes.FamORAssem | FieldAttributes.Static);
                _keywordLookupSiteFields.Add(fb1);
                _thunkFields.Add(fb2);
            }
        }


        private void EmitStaticConstructor(TypeBuilder fnTB)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

            if (Constants.count() > 0)
                EmitConstantFieldInits(cb);

            if (KeywordCallsites.count() > 0)
                EmitKeywordCallsiteInits(cb);

            cb.GetILGenerator().Emit(OpCodes.Ret);
        }

        private void EmitKeywordCallsiteInits(ConstructorBuilder cb)
        {
            ILGenerator ilg = cb.GetILGenerator();

            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                Keyword k = (Keyword)KeywordCallsites.nth(i);
                EmitValue(k, ilg);
                ilg.Emit(OpCodes.Newobj, Compiler.Ctor_KeywordLookupSite_1);
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Stsfld, _keywordLookupSiteFields[i]);
                ilg.Emit(OpCodes.Castclass, typeof(ILookupThunk));
                ilg.Emit(OpCodes.Stsfld, _thunkFields[i]);
            }
        }

        private void EmitConstantFieldInits(ConstructorBuilder cb)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PrintDupVar, true));

                ILGenerator ilg = cb.GetILGenerator();

                for (int i = 0; i < Constants.count(); i++)
                {
                    if (_constantFields[i] != null)
                    {
                        EmitValue(Constants.nth(i), ilg);
                        if ( Constants.nth(i).GetType() != ConstantType(i) )
                            ilg.Emit(OpCodes.Castclass, ConstantType(i));
                        ilg.Emit(OpCodes.Stsfld, _constantFields[i]);
                    }
                }
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


        void EmitClosedOverFields(TypeBuilder tb)
        {
            _closedOverFields = new List<FieldBuilder>(Closes.count());
            _closedOverFieldsMap = new Dictionary<LocalBinding, FieldBuilder>(Closes.count());

            // closed-overs map to instance fields.
            for (ISeq s = RT.keys(Closes); s != null; s = s.next())
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
                    ? tb.DefineField(lb.Name, type, new Type[] { typeof(IsVolatile) }, Type.EmptyTypes, attributes)
                    : tb.DefineField(lb.Name, type, attributes);

                GenInterface.SetCustomAttributes(fb, GenInterface.ExtractAttributes(RT.meta(lb.Symbol)));

                _closedOverFields.Add(fb);
                _closedOverFieldsMap[lb] = fb;
            }
        }

        private void EmitProtocolCallsites(TypeBuilder tb)
        {
            int count = ProtocolCallsites.count();

            _cachedTypeFields = new List<FieldBuilder>(count);
            _cachedProtoFnFields = new List<FieldBuilder>(count);
            _cachedProtoImplFields = new List<FieldBuilder>(count);


            for (int i = 0; i < count; i++)
            {
                _cachedTypeFields.Add(tb.DefineField(CachedClassName(i), typeof(Type), FieldAttributes.Public));
                _cachedProtoFnFields.Add(tb.DefineField(CachedProtoFnName(i), typeof(AFunction), FieldAttributes.Public));
                _cachedProtoImplFields.Add(tb.DefineField(CachedProtoImplName(i), typeof(IFn), FieldAttributes.Public));
            }
        }


        private ConstructorBuilder EmitConstructor(TypeBuilder fnTB, Type baseType)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, CtorTypes());
            ILGen gen = new ILGen(cb.GetILGenerator());

            //Call base constructor
            ConstructorInfo baseCtorInfo = baseType.GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,Type.EmptyTypes,null);
            if (baseCtorInfo == null)
                throw new InvalidOperationException("Unable to find default constructor for " + baseType.FullName);

            gen.EmitLoadArg(0);
            gen.Emit(OpCodes.Call, baseCtorInfo);

            // Store Meta
            if (SupportsMeta)
            {
                gen.EmitLoadArg(0);
                gen.EmitLoadArg(1);
                gen.Emit(OpCodes.Castclass, typeof(IPersistentMap));
                gen.EmitFieldSet(_metaField);
            }

            // store closed-overs in their fields
            int a = 0;
            int offset = !SupportsMeta ? 1 : 2;

            for (ISeq s = RT.keys(Closes); s != null; s = s.next(), a++)
            {
                //LocalBinding lb = (LocalBinding)s.first();
                FieldBuilder fb = _closedOverFields[a];

                gen.EmitLoadArg(0);             // gen.Emit(OpCodes.Ldarg_0);
                gen.EmitLoadArg(a + offset);         // gen.Emit(OpCodes.Ldarg, a + 1);
                gen.Emit(OpCodes.Stfld, fb);
            }
            gen.Emit(OpCodes.Ret);
            return cb;
        }


        private ConstructorBuilder EmitFieldOnlyConstructor(TypeBuilder fnTB, Type baseType)
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


        private ConstructorBuilder EmitNonMetaConstructor(TypeBuilder fnTB, Type baseType)
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


        void EmitSwapThunk(TypeBuilder tb)
        {

            MethodBuilder mb = tb.DefineMethod("swapThunk", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int), typeof(ILookupThunk) });
            ILGen ilg = new ILGen(mb.GetILGenerator());

            Label endLabel = ilg.DefineLabel();
            Label[] labels = new Label[KeywordCallsites.count()];
            for (int i = 0; i < KeywordCallsites.count(); i++)
                labels[i] = ilg.DefineLabel();

            ilg.EmitLoadArg(1);
            ilg.Emit(OpCodes.Switch, labels);
            ilg.Emit(OpCodes.Br, endLabel);

            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                ilg.MarkLabel(labels[i]);
                ilg.EmitLoadArg(2);
                ilg.EmitFieldSet(_thunkFields[i]);
                ilg.Emit(OpCodes.Br, endLabel);
            }

            ilg.MarkLabel(endLabel);
            ilg.Emit(OpCodes.Ret);
        }

        private void EmitMetaFunctions(TypeBuilder fnTB)
        {
            // IPersistentMap meta()
            MethodBuilder metaMB = fnTB.DefineMethod("meta", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot, typeof(IPersistentMap), Type.EmptyTypes);
            ILGen gen = new ILGen(metaMB.GetILGenerator());
            if (SupportsMeta)
            {
                gen.EmitLoadArg(0);
                gen.EmitFieldGet(_metaField);
            }
            else
                gen.EmitNull();
            gen.Emit(OpCodes.Ret);


            // IObj withMeta(IPersistentMap)
            MethodBuilder withMB = fnTB.DefineMethod("withMeta", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot, typeof(IObj), new Type[] { typeof(IPersistentMap) });
            gen = new ILGen(withMB.GetILGenerator());

            if (SupportsMeta)
            {
                gen.EmitLoadArg(1);   // meta arg
                foreach (FieldBuilder fb in _closedOverFields)
                {
                    gen.EmitLoadArg(0);
                    gen.EmitFieldGet(fb);
                }

                gen.EmitNew(_ctorInfo);
            }
            else
                gen.EmitLoadArg(0);  //this
            gen.Emit(OpCodes.Ret);
        }


        protected virtual void EmitMethods(GenContext context)
        {
        }

        protected virtual void EmitStatics(GenContext context)
        {
        }

        public void EmitConstantFieldDefs(TypeBuilder baseTB)
        {
            // We have to do this different than the JVM version.
            // The JVM does all these at the end.
            // That works for the usual ObjExpr, but not for the top-level one that becomes __Init__.Initialize in the assembly.
            // That one need the constants defined incrementally.
            // This version accommodates the all-at-end approach for general ObjExprs and the incremental approach in Compiler.Compile1.

            if (_constantFields == null)
                _constantFields = new Dictionary<int, FieldBuilder>(Constants.count());

            int nextKey = _constantFields.Count == 0 ? 0 : _constantFields.Keys.Max() + 1;

            for (int i = nextKey; i < Constants.count(); i++)
            {
                if (!_constantFields.ContainsKey(i))
                {
                    string fieldName = ConstantName(i);
                    Type fieldType = ConstantType(i);
                    FieldBuilder fb = baseTB.DefineField(fieldName, fieldType, FieldAttributes.FamORAssem | FieldAttributes.Static);
                    _constantFields[i] = fb;
                }
            }
        }

        public MethodBuilder EmitConstants(TypeBuilder fnTB)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PrintDupVar, true));

                MethodBuilder mb = fnTB.DefineMethod(StaticCtorHelperName + "_constants", MethodAttributes.Private | MethodAttributes.Static);
                ILGenerator ilg = mb.GetILGenerator();

                for (int i = 0; i < Constants.count(); i++)
                {
                    FieldBuilder fb;
                    if (_constantFields.TryGetValue(i, out fb))
                    {
                        EmitValue(Constants.nth(i), mb);
                        if (Constants.nth(i).GetType() != ConstantType(i))
                            ilg.Emit(OpCodes.Castclass, ConstantType(i));
                        ilg.Emit(OpCodes.Stsfld, fb);
                    }
                }
                ilg.Emit(OpCodes.Ret);

                return mb;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        private void EmitValue(object value, MethodBuilder mb)
        {
            EmitValue(value, mb.GetILGenerator());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void EmitValue(object value, ILGenerator gen)
        {
            ILGen ilg = new ILGen(gen);

            bool partial = true;

            if (value == null)
                ilg.Emit(OpCodes.Ldnull);
            else if (value is String)
                ilg.Emit(OpCodes.Ldstr, (String)value);
            else if (value is Boolean)
            {
                ilg.EmitBoolean((Boolean)value);
                ilg.EmitBoxing(typeof(bool));
            }
            else if (value is Int32)
            {
                ilg.EmitInt((int)value);
                ilg.EmitBoxing(typeof(int));
            }
            else if (value is Int64)
            {
                ilg.EmitLong((long)value);
                ilg.EmitBoxing(typeof(long));
            }
            else if (value is Double)
            {
                ilg.EmitDouble((double)value);
                ilg.EmitBoxing(typeof(double));
            }
            else if (value is Char)
            {
                ilg.EmitChar((char)value);
                ilg.EmitBoxing(typeof(char));
            }
            else if (value is Type)
            {
                Type t = (Type)value;
                if (t.IsValueType)
                    ilg.EmitType(t);
                else
                {
                    ilg.EmitString(Compiler.DestubClassName(((Type)value).FullName));
                    ilg.EmitCall(Compiler.Method_RT_classForName);
                }
            }
            else if (value is Symbol)
            {
                Symbol sym = (Symbol)value;
                if (sym.Namespace == null)
                    ilg.EmitNull();
                else
                    ilg.EmitString(sym.Namespace);
                ilg.EmitString(sym.Name);
                ilg.EmitCall(Compiler.Method_Symbol_intern2);
            }
            else if (value is Keyword)
            {
                Keyword keyword = (Keyword)value;
                if (keyword.Namespace == null)
                    ilg.EmitNull();
                else
                    ilg.EmitString(keyword.Namespace);
                ilg.EmitString(keyword.Name);
                ilg.EmitCall(Compiler.Method_RT_keyword);
            }
            else if (value is Var)
            {
                Var var = (Var)value;
                ilg.EmitString(var.Namespace.Name.ToString());
                ilg.EmitString(var.Symbol.Name.ToString());
                ilg.EmitCall(Compiler.Method_RT_var2);
            }
            else if (value is IType)
            {
                IPersistentVector fields = (IPersistentVector)Reflector.InvokeStaticMethod(value.GetType(), "getBasis", Type.EmptyTypes);

                for (ISeq s = RT.seq(fields); s != null; s = s.next())
                {
                    Symbol field = (Symbol)s.first();
                    Type k = Compiler.TagType(Compiler.TagOf(field));
                    object val = Reflector.GetInstanceFieldOrProperty(value, field.Name);
                    EmitValue(val, gen);
                    if (k.IsPrimitive)
                    {
                        ilg.Emit(OpCodes.Castclass, k);
                    }

                }

                ConstructorInfo cinfo = value.GetType().GetConstructors()[0];
                ilg.EmitNew(cinfo);
            }
            else if (value is IRecord)
            {
                //MethodInfo[] minfos = value.GetType().GetMethods(BindingFlags.Static | BindingFlags.Public);
                EmitValue(PersistentArrayMap.create((IDictionary)value), gen);

                MethodInfo createMI = value.GetType().GetMethod("create", BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Standard, new Type[] { typeof(IPersistentMap) }, null);
                ilg.EmitCall(createMI);
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
                EmitListAsObjectArray(entries, gen);
                ilg.EmitCall(Compiler.Method_RT_map);
            }
            else if (value is IPersistentVector)
            {
                EmitListAsObjectArray(value, gen);
                ilg.EmitCall(Compiler.Method_RT_vector);
            }
            else if (value is PersistentHashSet)
            {
                ISeq vs = RT.seq(value);
                if (vs == null)
                    ilg.EmitFieldGet(Compiler.Method_PersistentHashSet_EMPTY);
                else
                {
                    EmitListAsObjectArray(vs, gen);
                    ilg.EmitCall(Compiler.Method_PersistentHashSet_create);
                }
            }
            else if (value is ISeq || value is IPersistentList)
            {
                EmitListAsObjectArray(value, gen);
                ilg.EmitCall(Compiler.Method_PersistentList_create);
            }
            else if (value is Regex)
            {
                ilg.EmitString(((Regex)value).ToString());
                ilg.EmitNew(Compiler.Ctor_Regex_1);
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
                    throw new InvalidOperationException(String.Format("Can't embed object in code, maybe print-dup not defined: {0}", value));
                }
                if (cs.Length == 0)
                    throw new InvalidOperationException(String.Format("Can't embed unreadable object in code: " + value));
                if (cs.StartsWith("#<"))
                    throw new InvalidOperationException(String.Format("Can't embed unreadable object in code: " + cs));

                ilg.EmitString(cs);
                ilg.EmitCall(Compiler.Method_RT_readString);
                partial = false;
            }

            if (partial)
            {
                if (value is IObj && RT.count(((IObj)value).meta()) > 0)
                {
                    ilg.Emit(OpCodes.Castclass, typeof(IObj));
                    EmitValue(((IObj)value).meta(), gen);
                    ilg.Emit(OpCodes.Castclass, typeof(IPersistentMap));
                    ilg.Emit(OpCodes.Callvirt, Compiler.Method_IObj_withMeta);
                }
            }
        }

        private void EmitListAsObjectArray(object value, ILGenerator ilg)
        {
            ILGen ilg2 = new ILGen(ilg);
            ICollection coll = (ICollection)value;

            ilg2.EmitInt(coll.Count);
            ilg2.Emit(OpCodes.Newarr, typeof(Object));

            int i = 0;
            foreach (Object item in (ICollection)value)
            {
                ilg2.Emit(OpCodes.Dup);
                ilg2.EmitInt(i++);
                EmitValue(item, ilg);
                //HostExpr.EmitBoxReturn(this, ilg, item.GetType());
                ilg2.Emit(OpCodes.Stelem_Ref);
            }
        }

        internal void EmitConstant(GenContext context, int id, object val)
        {
            ////if (_fnMode == FnMode.Full && !val.GetType().IsPrimitive)

            FieldBuilder fb = null;
            if (_constantFields != null && _constantFields.TryGetValue(id, out fb))
                context.GetILGenerator().Emit(OpCodes.Ldsfld, fb);
            else
                EmitValue(val, context.GetILGenerator());
        }

        internal void EmitVar(GenContext context, Var var)
        {
            int i = (int)Vars.valAt(var);
            EmitConstant(context, i, var);
        }


        internal void EmitKeyword(GenContext context, Keyword kw)
        {
            int i = (int)Keywords.valAt(kw);
            EmitConstant(context, i, kw);
        }


        internal void EmitVarValue(GenContext context, Var v)
        {
            ILGenerator ilg =context.GetILGenerator();

            int i = (int)Vars.valAt(v);
            //if (_fnMode == Ast.FnMode.Full && !v.isDynamic())
            if ( !v.isDynamic() )
            {
                EmitConstant(context, i, v);
                ilg.Emit(OpCodes.Call, Compiler.Method_Var_getRawRoot);
            }
            else
            {
                EmitConstant(context, i, v);
                ilg.Emit(OpCodes.Call, Compiler.Method_Var_get);
            }
        }


        internal void EmitLocal(GenContext context, LocalBinding lb)
        {
            ILGenerator ilg = context.GetILGenerator();
            Type primType = lb.PrimitiveType;
 
            //if (_fnMode == FnMode.Full)

            if (Closes.containsKey(lb))
            {
                ilg.Emit(OpCodes.Ldarg_0); // this
                ilg.Emit(OpCodes.Ldfld, _closedOverFieldsMap[lb]);
                if (primType != null)
                    HostExpr.EmitBoxReturn(this, context, primType);
                // TODO: ONCEONLY?            }
            }
            else
            {
                if (lb.IsArg)
                {
                    int argOffset = IsStatic ? 1 : 0;
                    ilg.Emit(OpCodes.Ldarg, lb.Index - argOffset);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldloc, lb.LocalVar);
                }
                if (primType != null)
                    HostExpr.EmitBoxReturn(this, context, primType);
             }
        }

        internal void EmitUnboxedLocal(GenContext context, LocalBinding lb)
        {
            ILGenerator ilg = context.GetILGenerator();

            if (Closes.containsKey(lb))
            {
                ilg.Emit(OpCodes.Ldarg_0); // this
                ilg.Emit(OpCodes.Ldfld, _closedOverFieldsMap[lb]);
            }
            else if (lb.IsArg)
            {
                //int argOffset = IsStatic ? 0 : 1;
                //ilg.Emit(OpCodes.Ldarg, lb.Index + argOffset);
                ilg.Emit(OpCodes.Ldarg, lb.Index);
            }
            else
                ilg.Emit(OpCodes.Ldloc, lb.LocalVar);
        }
        
        internal void EmitAssignLocal(GenContext context, LocalBinding lb, Expr val)
        {
            ILGenerator ilg = context.GetILGenerator();

            if (!IsMutable(lb))
                throw new ArgumentException("Cannot assign to non-mutable: ", lb.Name);

            FieldBuilder fb = null;
            bool hasField = _closedOverFieldsMap.TryGetValue(lb, out fb);

            ilg.Emit(OpCodes.Ldarg_0);  // this

            Type primt = lb.PrimitiveType;
            if (primt != null)
            {
                MaybePrimitiveExpr mbe = val as MaybePrimitiveExpr;
                if (!(mbe != null && mbe.CanEmitPrimitive))
                    throw new ArgumentException("Must assign primitive to primitive mutable", lb.Name);
                mbe.EmitUnboxed(RHC.Expression, this, context);

            }
            else
            {
                val.Emit(RHC.Expression, this, context);
            }

            if (hasField)
                ilg.Emit(OpCodes.Stfld, fb);
            else
                ilg.Emit(OpCodes.Stloc, lb.LocalVar);
        }


        internal void EmitLetFnInits(GenContext context, LocalBuilder localBuilder, ObjExpr objx, IPersistentSet letFnLocals)
        {
            ILGenerator ilg = context.GetILGenerator();

            ilg.Emit(OpCodes.Castclass,_typeBuilder);

            for (ISeq s = RT.keys(Closes); s != null; s = s.next())
            {
                LocalBinding lb = (LocalBinding)s.first();
                if (letFnLocals.contains(lb))
                {
                    FieldBuilder fb;
                    _closedOverFieldsMap.TryGetValue(lb,out fb);

                    Type primt = lb.PrimitiveType;
                    ilg.Emit(OpCodes.Dup);  // this
                    if ( primt != null )
                    {
                        objx.EmitUnboxedLocal(context,lb);
                        ilg.Emit(OpCodes.Stfld,fb);
                    }
                    else
                    {
                        objx.EmitLocal(context,lb);
                        ilg.Emit(OpCodes.Stfld,fb);
                    }
                }
            }
            ilg.Emit(OpCodes.Pop);
        }

        protected static void EmitHasArityMethod(TypeBuilder tb, IList<int> arities, bool isVariadic, int reqArity)
        {

            // TODO: Convert to a Switch instruction
            MethodBuilder mb = tb.DefineMethod(
                "HasArity",
                MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(bool),
                new Type[] { typeof(int) });

            ILGen gen = new ILGen(mb.GetILGenerator());

            Label falseLabel = gen.DefineLabel();
            Label trueLabel = gen.DefineLabel();

            if (isVariadic)
            {
                gen.EmitLoadArg(1);
                gen.EmitInt(reqArity);
                gen.Emit(OpCodes.Bge, trueLabel);
            }

            if (arities != null)
            {
                foreach (int i in arities)
                {
                    gen.EmitLoadArg(1);
                    gen.EmitInt(i);
                    gen.Emit(OpCodes.Beq, trueLabel);
                }
            }

            gen.MarkLabel(falseLabel);
            gen.EmitBoolean(false);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(trueLabel);
            gen.EmitBoolean(true);
            gen.Emit(OpCodes.Ret);
        }
         
        #endregion
    }
}
