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
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;


namespace clojure.lang.CljCompiler.Ast
{
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
        public IPersistentVector Constants { get; set; }

        Dictionary<int, FieldBuilder> ConstantFields { get; set;} 
        protected IPersistentMap Fields { get; set; }            // symbol -> lb

        protected IPersistentMap SpanMap { get; set; }

        protected Type _compiledType;
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


        FieldBuilder _metaField;
        List<FieldBuilder> _closedOverFields;
        Dictionary<LocalBinding, FieldBuilder> _closedOverFieldsMap;
        Dictionary<FieldBuilder, LocalBinding> _closedOverFieldsToBindingsMap;

        protected int _altCtorDrops;

        protected IPersistentCollection _methods;

        protected int _constantsID;
        protected bool _onceOnly;
        protected Object _src;
        protected bool _isStatic;
        public bool IsStatic { get { return _isStatic; } }


 

        protected bool IsDefType { get { return Fields != null; } }
        protected virtual bool SupportsMeta { get { return !IsDefType; } }

        internal bool IsVolatile(LocalBinding lb)
        {
            return RT.booleanCast(RT.contains(Fields, lb.Symbol)) &&
                RT.booleanCast(RT.get(lb.Symbol.meta(), Keyword.intern("volatile-mutable")));
        }

        internal bool IsMutable(LocalBinding lb)
        {
            return IsVolatile(lb)
                ||
                RT.booleanCast(RT.contains(Fields, lb.Symbol)) &&
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

        protected string _thisName;
        protected IPersistentVector _hintedFields = PersistentVector.EMPTY; // hinted fields

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

        internal static void MarkAsSerializable(TypeBuilder tb)
        {
            tb.SetCustomAttribute(new CustomAttributeBuilder(Compiler.Ctor_Serializable, new object[0]));
        }

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

        public virtual void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            //emitting a Fn means constructing an instance, feeding closed-overs from enclosing scope, if any
            //objx arg is enclosing objx, not this

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
                        objx.EmitUnboxedLocal(ilg, lb);
                    else
                        objx.EmitLocal(ilg, lb);
                }

                ilg.Emit(OpCodes.Newobj, _ctorInfo);
            }

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public Type Compile(Type superType, Type stubType, IPersistentVector interfaces, bool onetimeUse, GenContext context)
        {
            if (_compiledType != null)
                return _compiledType;

            string publicTypeName = IsDefType || (_isStatic && Compiler.IsCompiling) ? InternalName : InternalName + "__" + RT.nextID();

            //Console.WriteLine("DefFn {0}, {1}", publicTypeName, context.AssemblyBuilder.GetName().Name);

            _typeBuilder = context.AssemblyGen.DefinePublicType(publicTypeName, superType, true);
            context = context.WithNewDynInitHelper().WithTypeBuilder(_typeBuilder);

            Var.pushThreadBindings(RT.map(Compiler.CompilerContextVar, context));

            try
            {
                if (interfaces != null)
                {
                    for (int i = 0; i < interfaces.count(); i++)
                        _typeBuilder.AddInterfaceImplementation((Type)interfaces.nth(i));
                }

                ObjExpr.MarkAsSerializable(_typeBuilder);
                GenInterface.SetCustomAttributes(_typeBuilder, _classMeta);

                try
                {
                    if (IsDefType)
                    {
                        Compiler.RegisterDuplicateType(_typeBuilder);

                        Var.pushThreadBindings(RT.map(
                            Compiler.CompileStubOrigClassVar, stubType
                            ));
                        //,
                        //Compiler.COMPILE_STUB_CLASS, _baseType));
                    }
                    EmitConstantFieldDefs(_typeBuilder);
                    EmitKeywordCallsiteDefs(_typeBuilder);

                    DefineStaticConstructor(_typeBuilder);

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

                    EmitStatics(_typeBuilder);
                    EmitMethods(_typeBuilder);

                    //if (KeywordCallsites.count() > 0)
                    //    EmitSwapThunk(_typeBuilder);

                    _compiledType = _typeBuilder.CreateType();

                    if (context.DynInitHelper != null)
                        context.DynInitHelper.FinalizeType();

                    //  If we don't pick up the ctor after we finalize the type, 
                    //    we sometimes get a ctor which is not a RuntimeConstructorInfo
                    //  This causes System.DynamicILGenerator.Emit(opcode,ContructorInfo) to blow up.
                    //    The error says the ConstructorInfo is null, but there is a second case in the code.
                    //  Thank heavens one can run Reflector on mscorlib.

                    ConstructorInfo[] cis = _compiledType.GetConstructors();
                    foreach (ConstructorInfo ci in cis)
                    {
                        if (ci.GetParameters().Length == CtorTypes().Length)
                        {
                            _ctorInfo = ci;
                            break;
                        }
                    }

                    return _compiledType;
                }
                finally
                {
                    if (IsDefType)
                        Var.popThreadBindings();
                }
            }
            finally
            {
                Var.popThreadBindings();
            }
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

        private void DefineStaticConstructor(TypeBuilder fnTB)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
            EmitStaticConstructorBody(new CljILGen(cb.GetILGenerator()));

        }

        private void EmitStaticConstructorBody(CljILGen ilg)
        {
            GenContext.EmitDebugInfo(ilg, SpanMap);

            if (Constants.count() > 0)
                EmitConstantFieldInits(ilg);

            if (KeywordCallsites.count() > 0)
                EmitKeywordCallsiteInits(ilg);

            ilg.Emit(OpCodes.Ret);
        }

        private void EmitKeywordCallsiteInits(CljILGen ilg)
        {
            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                Keyword k = (Keyword)KeywordCallsites.nth(i);
                EmitValue(k, ilg);
                ilg.Emit(OpCodes.Newobj, Compiler.Ctor_KeywordLookupSite_1);
                ilg.Emit(OpCodes.Dup);
                FieldBuilder kfb = _keywordLookupSiteFields[i];
                ilg.Emit(OpCodes.Stsfld, kfb);
                ilg.Emit(OpCodes.Castclass, typeof(ILookupThunk));
                FieldBuilder tfb = _thunkFields[i];
                ilg.Emit(OpCodes.Stsfld, tfb);
            }
        }

        private void EmitConstantFieldInits(CljILGen ilg)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PrintDupVar, true));

                for (int i = 0; i < Constants.count(); i++)
                {
                    if (ConstantFields[i] != null)
                    {
                        EmitValue(Constants.nth(i), ilg);
                        if ( Constants.nth(i).GetType() != ConstantType(i) )
                            ilg.Emit(OpCodes.Castclass, ConstantType(i));
                        FieldBuilder fb = ConstantFields[i];
                        ilg.Emit(OpCodes.Stsfld,fb);
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
            _closedOverFieldsToBindingsMap = new Dictionary<FieldBuilder, LocalBinding>(Closes.count());
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
                _closedOverFieldsToBindingsMap[fb] = lb;
            }
        }

        private void EmitProtocolCallsites(TypeBuilder tb)
        {
            int count = ProtocolCallsites.count();

            _cachedTypeFields = new List<FieldBuilder>(count);


            for (int i = 0; i < count; i++)
            {
                _cachedTypeFields.Add(tb.DefineField(CachedClassName(i), typeof(Type), FieldAttributes.Public|FieldAttributes.Static));
            }
        }

        private ConstructorBuilder EmitConstructor(TypeBuilder fnTB, Type baseType)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, CtorTypes());
            CljILGen gen = new CljILGen(cb.GetILGenerator());

            GenContext.EmitDebugInfo(gen, SpanMap);

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
                bool isVolatile = IsVolatile(_closedOverFieldsToBindingsMap[fb]);

                gen.EmitLoadArg(0);             // gen.Emit(OpCodes.Ldarg_0);
                gen.EmitLoadArg(a + offset);         // gen.Emit(OpCodes.Ldarg, a + 1);
                gen.MaybeEmitVolatileOp(isVolatile);
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
            CljILGen gen = new CljILGen(cb.GetILGenerator());

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
            CljILGen gen = new CljILGen(cb.GetILGenerator());

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
            CljILGen ilg = new CljILGen(mb.GetILGenerator());

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
            CljILGen gen = new CljILGen(metaMB.GetILGenerator());
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
            gen = new CljILGen(withMB.GetILGenerator());

            if (SupportsMeta)
            {
                gen.EmitLoadArg(1);   // meta arg
                foreach (FieldBuilder fb in _closedOverFields)
                {
                    gen.EmitLoadArg(0);
                    gen.MaybeEmitVolatileOp(fb);
                    gen.EmitFieldGet(fb);
                }

                gen.EmitNew(_ctorInfo);
            }
            else
                gen.EmitLoadArg(0);  //this
            gen.Emit(OpCodes.Ret);
        }

        protected virtual void EmitMethods(TypeBuilder tb)
        {
        }

        protected virtual void EmitStatics(TypeBuilder tb)
        {
        }

        public void EmitConstantFieldDefs(TypeBuilder baseTB)
        {
            // We have to do this different than the JVM version.
            // The JVM does all these at the end.
            // That works for the usual ObjExpr, but not for the top-level one that becomes __Init__.Initialize in the assembly.
            // That one need the constants defined incrementally.
            // This version accommodates the all-at-end approach for general ObjExprs and the incremental approach in Compiler.Compile1.

            if (ConstantFields == null)
                ConstantFields = new Dictionary<int, FieldBuilder>(Constants.count());

            int nextKey = ConstantFields.Count == 0 ? 0 : ConstantFields.Keys.Max() + 1;

            for (int i = nextKey; i < Constants.count(); i++)
            {
                if (!ConstantFields.ContainsKey(i))
                {
                    string fieldName = ConstantName(i);
                    Type fieldType = ConstantType(i);
                    FieldBuilder fb = baseTB.DefineField(fieldName, fieldType, FieldAttributes.FamORAssem | FieldAttributes.Static);
                    ConstantFields[i] = fb;
                }
            }
        }

        public MethodBuilder EmitConstants(TypeBuilder fnTB)
        {
            try
            {
                Var.pushThreadBindings(RT.map(RT.PrintDupVar, true));

                MethodBuilder mb = fnTB.DefineMethod(StaticCtorHelperName + "_constants", MethodAttributes.Private | MethodAttributes.Static);
                CljILGen ilg = new CljILGen(mb.GetILGenerator());

                for (int i = 0; i < Constants.count(); i++)
                {
                    FieldBuilder fb;
                    if (ConstantFields.TryGetValue(i, out fb))
                    {
                        EmitValue(Constants.nth(i), ilg);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        protected void EmitValue(object value, CljILGen ilg)
        {
            bool partial = true;

            if (value == null)
                ilg.Emit(OpCodes.Ldnull);
            else if (value is String)
                ilg.Emit(OpCodes.Ldstr, (String)value);
            else if (value is Boolean)
            {
                ilg.EmitBoolean((Boolean)value);
                ilg.Emit(OpCodes.Box,typeof(bool));
            }
            else if (value is Int32)
            {
                ilg.EmitInt((int)value);
                ilg.Emit(OpCodes.Box, typeof(int));
            }
            else if (value is Int64)
            {
                ilg.EmitLong((long)value);
                ilg.Emit(OpCodes.Box, typeof(long));
            }
            else if (value is Double)
            {
                ilg.EmitDouble((double)value);
                ilg.Emit(OpCodes.Box, typeof(double));
            }
            else if (value is Char)
            {
                ilg.EmitChar((char)value);
                ilg.Emit(OpCodes.Box,typeof(char));
            }
            else if (value is Type)
            {
                Type t = (Type)value;
                if (t.IsValueType)
                    ilg.EmitType(t);
                else
                {
                    //ilg.EmitString(Compiler.DestubClassName(((Type)value).FullName));
                    ilg.EmitString(((Type)value).FullName);
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
                    EmitValue(val, ilg);
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
                EmitValue(PersistentArrayMap.create((IDictionary)value), ilg);

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
                EmitListAsObjectArray(entries, ilg);
                ilg.EmitCall(Compiler.Method_RT_map);
            }
            else if (value is IPersistentVector)
            {
                EmitListAsObjectArray(value, ilg);
                ilg.EmitCall(Compiler.Method_RT_vector);
            }
            else if (value is PersistentHashSet)
            {
                ISeq vs = RT.seq(value);
                if (vs == null)
                    ilg.EmitFieldGet(Compiler.Method_PersistentHashSet_EMPTY);
                else
                {
                    EmitListAsObjectArray(vs, ilg);
                    ilg.EmitCall(Compiler.Method_PersistentHashSet_create);
                }
            }
            else if (value is ISeq || value is IPersistentList)
            {
                EmitListAsObjectArray(value, ilg);
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
                    Object m = ((IObj)value).meta();
                    EmitValue(Compiler.ElideMeta(m), ilg);
                    ilg.Emit(OpCodes.Castclass, typeof(IPersistentMap));
                    ilg.Emit(OpCodes.Callvirt, Compiler.Method_IObj_withMeta);
                }
            }
        }

        private void EmitListAsObjectArray(object value, CljILGen ilg)
        {
            ICollection coll = (ICollection)value;

            ilg.EmitInt(coll.Count);
            ilg.Emit(OpCodes.Newarr, typeof(Object));

            int i = 0;
            foreach (Object item in coll)
            {
                ilg.Emit(OpCodes.Dup);
                ilg.EmitInt(i++);
                EmitValue(item, ilg);
                //HostExpr.EmitBoxReturn(this, ilg, item.GetType());
                ilg.Emit(OpCodes.Stelem_Ref);
            }
        }

        internal void EmitConstant(CljILGen ilg, int id, object val)
        {
            FieldBuilder fb = null;
            if ( ConstantFields != null && ConstantFields.TryGetValue(id, out fb))
            {
                ilg.MaybeEmitVolatileOp(fb);
                ilg.Emit(OpCodes.Ldsfld, fb);
            }
            else
                EmitValue(val, ilg);
        }


        static void EmitPrimitive(CljILGen ilg, object val)
        {
            switch (Type.GetTypeCode(val.GetType()) )
            {
                case TypeCode.Boolean:
                    ilg.EmitBoolean((bool)val); break;
                case TypeCode.Byte:
                    ilg.EmitByte((byte)val); break;
                case TypeCode.Char:
                    ilg.EmitChar((char)val); break;
                case TypeCode.Decimal:
                    ilg.EmitDecimal((decimal)val); break;
                case TypeCode.Double:
                    ilg.EmitDouble((double)val); break;
                case TypeCode.Int16:
                    ilg.EmitShort((short)val); break;
                case TypeCode.Int32:
                    ilg.EmitInt((int)val); break;
                case TypeCode.Int64:
                    ilg.EmitLong((long)val); break;
                case TypeCode.SByte:
                    ilg.EmitSByte((sbyte)val); break;
                case TypeCode.Single:
                    ilg.EmitSingle((float)val); break;
                case TypeCode.UInt16:
                    ilg.EmitUShort((ushort)val); break;
                case TypeCode.UInt32:
                    ilg.EmitUInt((uint)val); break;
                case TypeCode.UInt64:
                    ilg.EmitULong((ulong)val); break;
                default:
                    throw new InvalidOperationException("Unknown constant type in EmitPrimitive");
            }
        }

        internal void EmitVar(CljILGen ilg, Var var)
        {
            int i = (int)Vars.valAt(var);
            EmitConstant(ilg, i, var);
        }


        internal void EmitKeyword(CljILGen ilg, Keyword kw)
        {
            int i = (int)Keywords.valAt(kw);
            EmitConstant(ilg, i, kw);
        }

        internal void EmitVarValue(CljILGen ilg, Var v)
        {
            int i = (int)Vars.valAt(v);
            if ( !v.isDynamic() )
            {
                EmitConstant(ilg, i, v);
                ilg.Emit(OpCodes.Call, Compiler.Method_Var_getRawRoot);
            }
            else
            {
                EmitConstant(ilg, i, v);
                ilg.Emit(OpCodes.Call, Compiler.Method_Var_get);  // or just Method_Var_get??
            }
        }

        internal void EmitLocal(CljILGen ilg, LocalBinding lb)
        {
            Type primType = lb.PrimitiveType;

            if (Closes.containsKey(lb))
            {
                ilg.Emit(OpCodes.Ldarg_0); // this
                FieldBuilder fb = _closedOverFieldsMap[lb];
                ilg.MaybeEmitVolatileOp(IsVolatile(lb));
                ilg.Emit(OpCodes.Ldfld, fb);
                if (primType != null)
                    HostExpr.EmitBoxReturn(this, ilg, primType);
                // TODO: ONCEONLY?    
            }
            else
            {
                if (lb.IsArg)
                {
                    //int argOffset = IsStatic ? 1 : 0;
                    //ilg.Emit(OpCodes.Ldarg, lb.Index - argOffset);
                    ilg.EmitLoadArg(lb.Index);
                }
                else if (lb.IsThis)
                {
                    ilg.EmitLoadArg(0);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldloc, lb.LocalVar);
                }
                if (primType != null)
                    HostExpr.EmitBoxReturn(this, ilg, primType);
            }
        }

        internal void EmitUnboxedLocal(CljILGen ilg, LocalBinding lb)
        {
            if (Closes.containsKey(lb))
            {
                ilg.Emit(OpCodes.Ldarg_0); // this
                FieldBuilder fb = _closedOverFieldsMap[lb];
                ilg.MaybeEmitVolatileOp(IsVolatile(lb));
                ilg.Emit(OpCodes.Ldfld, fb);
            }
            else if (lb.IsArg)
            {
                //int argOffset = IsStatic ? 0 : 1;
                //ilg.Emit(OpCodes.Ldarg, lb.Index + argOffset);
                ilg.EmitLoadArg(lb.Index);
            }
            else if (lb.IsThis)
            {
                ilg.EmitLoadArg(0);
            }
            else
                ilg.Emit(OpCodes.Ldloc, lb.LocalVar);
        }

        internal void EmitAssignLocal(CljILGen ilg, LocalBinding lb, Expr val)
        {
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
                mbe.EmitUnboxed(RHC.Expression, this, ilg);

            }
            else
            {
                val.Emit(RHC.Expression, this, ilg);
            }

            if (hasField)
            {
                ilg.MaybeEmitVolatileOp(IsVolatile(lb));
                ilg.Emit(OpCodes.Stfld, fb);
            }
            else
                ilg.Emit(OpCodes.Stloc, lb.LocalVar);
        }


        internal void EmitLetFnInits(CljILGen ilg, LocalBuilder localBuilder, ObjExpr objx, IPersistentSet letFnLocals)
        {
            if (_typeBuilder != null)
            {
                // Full compile
                ilg.Emit(OpCodes.Castclass, _typeBuilder);

                for (ISeq s = RT.keys(Closes); s != null; s = s.next())
                {
                    LocalBinding lb = (LocalBinding)s.first();
                    if (letFnLocals.contains(lb))
                    {
                        FieldBuilder fb;
                        _closedOverFieldsMap.TryGetValue(lb, out fb);

                        Type primt = lb.PrimitiveType;
                        ilg.Emit(OpCodes.Dup);  // this
                        if (primt != null)
                        {
                            objx.EmitUnboxedLocal(ilg, lb);
                            ilg.MaybeEmitVolatileOp(IsVolatile(lb));
                            ilg.Emit(OpCodes.Stfld, fb);
                        }
                        else
                        {
                            objx.EmitLocal(ilg, lb);
                            ilg.MaybeEmitVolatileOp(IsVolatile(lb));
                            ilg.Emit(OpCodes.Stfld, fb);
                        }
                    }
                }
                ilg.Emit(OpCodes.Pop);
            }
        }


        protected static void EmitHasArityMethod(TypeBuilder tb, IList<int> arities, bool isVariadic, int reqArity)
        {

            // TODO: Convert to a Switch instruction
            MethodBuilder mb = tb.DefineMethod(
                "HasArity",
                MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(bool),
                new Type[] { typeof(int) });

            CljILGen gen = new CljILGen(mb.GetILGenerator());

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

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
