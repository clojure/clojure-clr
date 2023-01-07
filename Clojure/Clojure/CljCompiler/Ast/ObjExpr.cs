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

        public const string ConstPrefix = "const__";
        public const string StaticCtorHelperName = "__static_ctor_helper";

        public string InternalName { get; internal set; }
        public string Name { get; protected set; }
        public string ThisName { get; protected set; }

        protected readonly object _tag;
        public object Tag { get { return _tag; } }

        public Object Src { get; protected set; }
        public IPersistentMap Opts { get; protected set; }


        // If we were to get rid of setting these in Compiler.Compile1, we could change to protected.
        // Perhaps part of passing context instead of using dynamic vars.
        public IPersistentMap Closes { get; internal set; }
        public IPersistentMap Keywords { get; internal set; }
        public IPersistentMap Vars { get; internal set; }
        public IPersistentVector Constants { get; internal set; }


        public Dictionary<int, FieldBuilder> ConstantFields { get; protected set; } 
        public IPersistentMap Fields { get; protected set; }            // symbol -> lb
        public IPersistentMap SpanMap { get; protected set; }
        public Type CompiledType { get; protected set; }
        public IPersistentMap ClassMeta { get; protected set; }
        public TypeBuilder TypeBuilder { get; protected set; }
        public ConstructorInfo CtorInfo { get; protected set; }
        public ConstructorInfo BaseClassClosedOverCtor { get; protected set; }  // needed by NewInstanceExpr
        public ConstructorInfo BaseClassAltCtor { get; protected set; }         // needed by NewInstanceExpr
        public ConstructorInfo BaseClassAltCtorNoHash { get; protected set; }   // needed by NewInstanceExpr
        public Type BaseClass { get; protected set; }                           // needed by NewInstanceExpr
        public IPersistentVector KeywordCallsites { get; protected set; }
        public IPersistentVector ProtocolCallsites { get; protected set; }
        public IPersistentSet VarCallsites { get; protected set; }



        public IList<FieldBuilder> KeywordLookupSiteFields { get; protected set; }

        public IList<FieldBuilder> ThunkFields { get; protected set; }

        public IList<FieldBuilder> CachedTypeFields { get; protected set; }


        internal FieldBuilder KeywordLookupSiteField(int i)
        {
            return KeywordLookupSiteFields[i];
        }

        internal FieldBuilder CachedTypeField(int i)
        {
            return CachedTypeFields[i];
        }

        internal FieldBuilder ThunkField(int i)
        {
            return ThunkFields[i];
        }


        public FieldBuilder MetaField { get; protected set; }

        public IList<FieldBuilder> ClosedOverFields { get; protected set; }

        public Dictionary<LocalBinding, FieldBuilder> ClosedOverFieldsMap { get; protected set; }

        public Dictionary<FieldBuilder, LocalBinding> ClosedOverFieldsToBindingsMap { get; protected set; }
        public IPersistentVector HintedFields { get; protected set; }

        public int AltCtorDrops { get; protected set; }

        public IPersistentCollection Methods { get; protected set; }

        public int ConstantsID { get; protected set; }
        public bool OnceOnly { get; protected set; }
        public bool CanBeDirect { get; protected set; }

        
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
            Type t = o?.GetType();
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

        Type _cachedType;
        
        #endregion

        #region C-tors

        public ObjExpr(object tag)
        {
            _tag = tag;
            Keywords = PersistentHashMap.EMPTY;
            Vars = PersistentHashMap.EMPTY;
            Closes = PersistentHashMap.EMPTY;
            HintedFields = PersistentVector.EMPTY;
            Opts = PersistentHashMap.EMPTY;
        }

        #endregion

        #region Type mangling

        public virtual bool HasClrType
        {
            get { return true; }
        }

        public virtual Type ClrType
        {
            get
            {
                if (_cachedType == null)
                    _cachedType = CompiledType ?? (_tag != null ? HostExpr.TagToType(_tag) : typeof(IFn));
                return _cachedType;
            }
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

        #region Emitting a Fn

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

                ilg.Emit(OpCodes.Newobj, CtorInfo);
            }

            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        #endregion

        #region Fn class construction

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        public Type Compile(Type superType, Type stubType, IPersistentVector interfaces, bool onetimeUse, GenContext context)
        {
            if (CompiledType != null)
                return CompiledType;

            string publicTypeName = IsDefType /* || (CanBeDirect && Compiler.IsCompiling) */ ? InternalName : InternalName + "__" + RT.nextID();

            TypeBuilder = context.AssemblyGen.DefinePublicType(publicTypeName, superType, true);
            context = context.WithNewDynInitHelper().WithTypeBuilder(TypeBuilder);

            Var.pushThreadBindings(RT.map(Compiler.CompilerContextVar, context));

            try
            {
                if (interfaces != null)
                {
                    for (int i = 0; i < interfaces.count(); i++)
                        TypeBuilder.AddInterfaceImplementation((Type)interfaces.nth(i));
                }

                ObjExpr.MarkAsSerializable(TypeBuilder);
                GenInterface.SetCustomAttributes(TypeBuilder, ClassMeta);

                try
                {
                    if (IsDefType)
                    {
                        Compiler.RegisterDuplicateType(TypeBuilder);

                        Var.pushThreadBindings(RT.map(
                            Compiler.CompileStubOrigClassVar, stubType,
                            Compiler.CompilingDefTypeVar, true
                            ));
                        //,
                        //Compiler.COMPILE_STUB_CLASS, _baseType));
                    }
                    EmitConstantFieldDefs(TypeBuilder);
                    EmitKeywordCallsiteDefs(TypeBuilder);

                    DefineStaticConstructor(TypeBuilder);

                    if (SupportsMeta)
                        MetaField = TypeBuilder.DefineField("__meta", typeof(IPersistentMap), FieldAttributes.Public | FieldAttributes.InitOnly);

                    // If this IsDefType, then it has already emitted the closed-over fields on the base class.
                    if ( ! IsDefType )
                        EmitClosedOverFields(TypeBuilder);
                    EmitProtocolCallsites(TypeBuilder);

                    CtorInfo = EmitConstructor(TypeBuilder, superType);

                    if (AltCtorDrops > 0)
                        EmitFieldOnlyConstructors(TypeBuilder, superType);

                    if (SupportsMeta)
                    {
                        EmitNonMetaConstructor(TypeBuilder, superType);
                        EmitMetaFunctions(TypeBuilder);
                    }

                    EmitStatics(TypeBuilder);
                    EmitMethods(TypeBuilder);

                    CompiledType = TypeBuilder.CreateType();

                    if (context.DynInitHelper != null)
                        context.DynInitHelper.FinalizeType();

                    CtorInfo = GetConstructorWithArgCount(CompiledType, CtorTypes().Length);

                    return CompiledType;
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

            KeywordLookupSiteFields = new List<FieldBuilder>(count);
            ThunkFields = new List<FieldBuilder>(count);

            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                //Keyword k = (Keyword)_keywordCallsites.nth(i);
                string siteName = SiteNameStatic(i);
                string thunkName = ThunkNameStatic(i);
                FieldBuilder fb1 = baseTB.DefineField(siteName, typeof(KeywordLookupSite), FieldAttributes.FamORAssem | FieldAttributes.Static);
                FieldBuilder fb2 = baseTB.DefineField(thunkName, typeof(ILookupThunk), FieldAttributes.FamORAssem | FieldAttributes.Static);
                KeywordLookupSiteFields.Add(fb1);
                ThunkFields.Add(fb2);
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

            if ( IsDefType && RT.booleanCast(RT.get(Opts,Compiler.LoadNsKeyword)))
                EmitLoadNsInitForDeftype(ilg);

            ilg.Emit(OpCodes.Ret);
        }

        private void EmitLoadNsInitForDeftype(CljILGen ilg)
        {
            string nsname = ((Symbol)RT.second(Src)).Namespace;
            if ( !nsname.Equals("clojure.core"))
            {
                ilg.EmitString("clojure.core");
                ilg.EmitString("require");
                ilg.EmitCall(Compiler.Method_RT_var2);
                ilg.EmitCall(Compiler.Method_Var_getRawRoot);
                ilg.Emit(OpCodes.Castclass, typeof(IFn));
                ilg.EmitNull();
                ilg.EmitString(nsname);
                ilg.EmitCall(Compiler.Method_Symbol_intern2);
                ilg.EmitCall(Compiler.Methods_IFn_invoke[1]);
                ilg.Emit(OpCodes.Pop);
            }
           
        }

        private void EmitKeywordCallsiteInits(CljILGen ilg)
        {
            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                Keyword k = (Keyword)KeywordCallsites.nth(i);
                EmitValue(k, ilg);
                ilg.Emit(OpCodes.Newobj, Compiler.Ctor_KeywordLookupSite_1);
                ilg.Emit(OpCodes.Dup);
                FieldBuilder kfb = KeywordLookupSiteFields[i];
                ilg.Emit(OpCodes.Stsfld, kfb);
                ilg.Emit(OpCodes.Castclass, typeof(ILookupThunk));
                FieldBuilder tfb = ThunkFields[i];
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

        protected void EmitClosedOverFields(TypeBuilder tb)
        {
            ClosedOverFields = new List<FieldBuilder>(Closes.count());
            ClosedOverFieldsToBindingsMap = new Dictionary<FieldBuilder, LocalBinding>(Closes.count());
            ClosedOverFieldsMap = new Dictionary<LocalBinding, FieldBuilder>(Closes.count());

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

                ClosedOverFields.Add(fb);
                ClosedOverFieldsMap[lb] = fb;
                ClosedOverFieldsToBindingsMap[fb] = lb;
            }
        }

        private void EmitProtocolCallsites(TypeBuilder tb)
        {
            int count = ProtocolCallsites.count();

            CachedTypeFields = new List<FieldBuilder>(count);


            for (int i = 0; i < count; i++)
            {
                CachedTypeFields.Add(tb.DefineField(CachedClassName(i), typeof(Type), FieldAttributes.Public|FieldAttributes.Static));
            }
        }

        private ConstructorBuilder EmitConstructor(TypeBuilder fnTB, Type baseType)
        {
            if (IsDefType)
                return EmitConstructorForDefType(fnTB, baseType);
            else
                return EmitConstructorForNonDefType(fnTB, baseType);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        private ConstructorBuilder EmitConstructorForDefType(TypeBuilder fnTB, Type baseType)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, CtorTypes());
            CljILGen gen = new CljILGen(cb.GetILGenerator());

            GenContext.EmitDebugInfo(gen, SpanMap);

            // Pass closed-overs to base class ctor

            gen.EmitLoadArg(0);             // gen.Emit(OpCodes.Ldarg_0);
            int a = 0;
            for (ISeq s = RT.keys(Closes); s != null; s = s.next(), a++)
            {
                //LocalBinding lb = (LocalBinding)s.first();
                //FieldBuilder fb = _closedOverFields[a];
                //bool isVolatile = IsVolatile(_closedOverFieldsToBindingsMap[fb]);

                gen.EmitLoadArg(a + 1);         // gen.Emit(OpCodes.Ldarg, a + 1);
            }
            gen.Emit(OpCodes.Call, BaseClassClosedOverCtor);

            gen.Emit(OpCodes.Ret);

            return cb;
        }


        private ConstructorBuilder EmitConstructorForNonDefType(TypeBuilder fnTB, Type baseType)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, CtorTypes());
            CljILGen gen = new CljILGen(cb.GetILGenerator());

            GenContext.EmitDebugInfo(gen, SpanMap);

            //Call base constructor
            ConstructorInfo baseCtorInfo = baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
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
                gen.EmitFieldSet(MetaField);
            }

            // store closed-overs in their fields
            int a = 0;
            int offset = !SupportsMeta ? 1 : 2;

            for (ISeq s = RT.keys(Closes); s != null; s = s.next(), a++)
            {
                //LocalBinding lb = (LocalBinding)s.first();
                FieldBuilder fb = ClosedOverFields[a];
                bool isVolatile = IsVolatile(ClosedOverFieldsToBindingsMap[fb]);

                gen.EmitLoadArg(0);             // gen.Emit(OpCodes.Ldarg_0);
                gen.EmitLoadArg(a + offset);         // gen.Emit(OpCodes.Ldarg, a + 1);
                gen.MaybeEmitVolatileOp(isVolatile);
                gen.Emit(OpCodes.Stfld, fb);
            }
            gen.Emit(OpCodes.Ret);
            return cb;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        private void EmitFieldOnlyConstructors(TypeBuilder fnTB, Type baseType)
        {
            EmitFieldOnlyConstructorWithHash(fnTB);
            EmitFieldOnlyConstructorWithoutHash(fnTB);
        }

        private void EmitFieldOnlyConstructorWithHash(TypeBuilder fnTB)
        {
            Type[] ctorTypes = CtorTypes();
            Type[] altCtorTypes = new Type[ctorTypes.Length - AltCtorDrops];
            for (int i = 0; i < altCtorTypes.Length; i++)
                altCtorTypes[i] = ctorTypes[i];

            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, altCtorTypes);
            CljILGen gen = new CljILGen(cb.GetILGenerator());

            //Call full constructor
            gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < altCtorTypes.Length; i++)
                gen.EmitLoadArg(i + 1);

            //for (int i = 0; i < AltCtorDrops; i++)
            //    gen.EmitNull();
            gen.EmitNull();                    // __meta
            gen.EmitNull();                    // __extmap
            gen.Emit(OpCodes.Ldc_I4_0);        // __hash
            gen.Emit(OpCodes.Ldc_I4_0);        // __hasheq

            gen.Emit(OpCodes.Call, CtorInfo);

            gen.Emit(OpCodes.Ret);

        }

        private void EmitFieldOnlyConstructorWithoutHash(TypeBuilder fnTB)
        {
            Type[] ctorTypes = CtorTypes();
            Type[] altCtorTypes = new Type[ctorTypes.Length - 2];
            for (int i = 0; i < altCtorTypes.Length; i++)
                altCtorTypes[i] = ctorTypes[i];

            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, altCtorTypes);
            CljILGen gen = new CljILGen(cb.GetILGenerator());

            //Call full constructor
            gen.EmitLoadArg(0);                     // gen.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < altCtorTypes.Length; i++)
                gen.EmitLoadArg(i + 1);

            gen.Emit(OpCodes.Ldc_I4_0);        // __hash
            gen.Emit(OpCodes.Ldc_I4_0);        // __hasheq

            gen.Emit(OpCodes.Call, CtorInfo);

            gen.Emit(OpCodes.Ret);

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
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
            gen.Emit(OpCodes.Call, CtorInfo);
            gen.Emit(OpCodes.Ret);

            return cb;
        }

        private void EmitMetaFunctions(TypeBuilder fnTB)
        {
            // IPersistentMap meta()
            MethodBuilder metaMB = fnTB.DefineMethod("meta", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot, typeof(IPersistentMap), Type.EmptyTypes);
            CljILGen gen = new CljILGen(metaMB.GetILGenerator());
            if (SupportsMeta)
            {
                gen.EmitLoadArg(0);
                gen.EmitFieldGet(MetaField);
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
                foreach (FieldBuilder fb in ClosedOverFields)
                {
                    gen.EmitLoadArg(0);
                    gen.MaybeEmitVolatileOp(fb);
                    gen.EmitFieldGet(fb);
                }

                gen.EmitNew(CtorInfo);
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
                    if (ConstantFields.TryGetValue(i, out FieldBuilder fb))
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

        //  If we don't pick up the ctor after we finalize the type, 
        //    we sometimes get a ctor which is not a RuntimeConstructorInfo
        //  This causes System.DynamicILGenerator.Emit(opcode,ContructorInfo) to blow up.
        //    The error says the ConstructorInfo is null, but there is a second case in the code.
        //  Thank heavens one can run Reflector on mscorlib.
        //
        // We will take the first ctor with indicated number of args.
        // In our use case, it should be unique.
        static protected ConstructorInfo GetConstructorWithArgCount(Type t, int numArgs)
        {
            ConstructorInfo[] cis = t.GetConstructors();
            foreach (ConstructorInfo ci in cis)
            {
                if (ci.GetParameters().Length == numArgs)
                {
                    return ci;
                }
            }
            return null;
        }

        #endregion

        #region Direct code generation

        protected void EmitValue(object value, CljILGen ilg)
        {
            bool partial = true;

            if (value == null)
                ilg.Emit(OpCodes.Ldnull);
            else if (value is String str)
                ilg.Emit(OpCodes.Ldstr, str);
            else if (value is Boolean b)
            {
                ilg.EmitBoolean(b);
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
            else if (value is Type t)
            {
                if (t.IsValueType)
                    ilg.EmitType(t);
                else
                {
                    //ilg.EmitString(Compiler.DestubClassName(((Type)value).FullName));
                    ilg.EmitString(((Type)value).FullName);
                    ilg.EmitCall(Compiler.Method_RT_classForName);
                }
            }
            else if (value is Symbol sym)
            {
                if (sym.Namespace == null)
                    ilg.EmitNull();
                else
                    ilg.EmitString(sym.Namespace);
                ilg.EmitString(sym.Name);
                ilg.EmitCall(Compiler.Method_Symbol_intern2);
            }
            else if (value is Keyword keyword)
            {
                if (keyword.Namespace == null)
                    ilg.EmitNull();
                else
                    ilg.EmitString(keyword.Namespace);
                ilg.EmitString(keyword.Name);
                ilg.EmitCall(Compiler.Method_RT_keyword);
            }
            else if (value is Var var)
            {
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
                    object val = Reflector.GetInstanceFieldOrProperty(value, Compiler.munge(field.Name));
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
            else if (value is IPersistentMap map)
            {
                List<object> entries = new List<object>(map.count() * 2);
                foreach (IMapEntry entry in map)
                {
                    entries.Add(entry.key());
                    entries.Add(entry.val());
                }
                EmitListAsObjectArray(entries, ilg);
                ilg.EmitCall(Compiler.Method_RT_map);
            }
            else if (value is IPersistentVector args)
            {
                if (args.count() <= Tuple.MAX_SIZE)
                {
                    for (int i = 0; i < args.count(); i++)
                        EmitValue(args.nth(i), ilg);
                    ilg.Emit(OpCodes.Call, Compiler.Methods_CreateTuple[args.count()]);
                }
                else
                {
                    EmitListAsObjectArray(value, ilg);
                    ilg.EmitCall(Compiler.Method_RT_vector);
                }
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
            else if (value is Regex regex)
            {
                ilg.EmitString(regex.ToString());
                ilg.EmitNew(Compiler.Ctor_Regex_1);
            }
            else
            {
                string cs;
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
                if (value is IObj obj && RT.count(obj.meta()) > 0)
                {
                    ilg.Emit(OpCodes.Castclass, typeof(IObj));
                    Object m = obj.meta();
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
            if (ConstantFields != null && ConstantFields.TryGetValue(id, out FieldBuilder fb))
            {
                ilg.MaybeEmitVolatileOp(fb);
                ilg.Emit(OpCodes.Ldsfld, fb);
            }
            else
                EmitValue(val, ilg);
        }


        //static void EmitPrimitive(CljILGen ilg, object val)
        //{
        //    switch (Type.GetTypeCode(val.GetType()) )
        //    {
        //        case TypeCode.Boolean:
        //            ilg.EmitBoolean((bool)val); break;
        //        case TypeCode.Byte:
        //            ilg.EmitByte((byte)val); break;
        //        case TypeCode.Char:
        //            ilg.EmitChar((char)val); break;
        //        case TypeCode.Decimal:
        //            ilg.EmitDecimal((decimal)val); break;
        //        case TypeCode.Double:
        //            ilg.EmitDouble((double)val); break;
        //        case TypeCode.Int16:
        //            ilg.EmitShort((short)val); break;
        //        case TypeCode.Int32:
        //            ilg.EmitInt((int)val); break;
        //        case TypeCode.Int64:
        //            ilg.EmitLong((long)val); break;
        //        case TypeCode.SByte:
        //            ilg.EmitSByte((sbyte)val); break;
        //        case TypeCode.Single:
        //            ilg.EmitSingle((float)val); break;
        //        case TypeCode.UInt16:
        //            ilg.EmitUShort((ushort)val); break;
        //        case TypeCode.UInt32:
        //            ilg.EmitUInt((uint)val); break;
        //        case TypeCode.UInt64:
        //            ilg.EmitULong((ulong)val); break;
        //        default:
        //            throw new InvalidOperationException("Unknown constant type in EmitPrimitive");
        //    }
        //}

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
                FieldBuilder fb = ClosedOverFieldsMap[lb];
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
                FieldBuilder fb = ClosedOverFieldsMap[lb];
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

            bool hasField = ClosedOverFieldsMap.TryGetValue(lb, out FieldBuilder fb);

            ilg.Emit(OpCodes.Ldarg_0);  // this

            Type primt = lb.PrimitiveType;
            if (primt != null)
            {
                if (!(val is MaybePrimitiveExpr mbe && mbe.CanEmitPrimitive))
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


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        internal void EmitLetFnInits(CljILGen ilg, LocalBuilder localBuilder, ObjExpr objx, IPersistentSet letFnLocals)
        {
            if (TypeBuilder != null)
            {
                // Full compile
                ilg.Emit(OpCodes.Castclass, TypeBuilder);

                for (ISeq s = RT.keys(Closes); s != null; s = s.next())
                {
                    LocalBinding lb = (LocalBinding)s.first();
                    if (letFnLocals.contains(lb))
                    {
                        ClosedOverFieldsMap.TryGetValue(lb, out FieldBuilder fb);

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

        #endregion
    }
}
