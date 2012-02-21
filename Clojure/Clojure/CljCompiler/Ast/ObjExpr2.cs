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
using System.Runtime.CompilerServices;
using System.Collections;
using System.Text.RegularExpressions;

namespace clojure.lang.CljCompiler.Ast
{
    public class ObjExpr2 : Expr
    {
        #region Data

        const string ConstPrefix = "const__";
        const string StaticCtorHelperName = "__static_ctor_helper";


        protected string InternalName { get; set; }
        protected readonly object _tag;
        private object Tag { get { return _tag; } }

        protected IPersistentMap Closes = PersistentHashMap.EMPTY;         // localbinding -> itself
        public IPersistentMap Keywords { get; set; }        // Keyword -> KeywordExpr
        public IPersistentMap Vars { get; set; }        
        public PersistentVector Constants { get; set; }
        Dictionary<int, FieldBuilder> ConstantFields { get; set; }
        protected IPersistentMap Fields { get; set; }                          // symbol -> lb

        private Type _compiledType;
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

        protected IPersistentMap ClassMeta { get; set; }        

        protected TypeBuilder TypeBuilder { get; set; }
        ConstructorInfo CtorInfo  { get; set; }



        public IPersistentVector KeywordCallsites { get; set; }
        List<FieldBuilder> _keywordLookupSiteFields;
        List<FieldBuilder> _thunkFields;

        private IPersistentVector ProtocolCallsites { get; set; }
        List<FieldBuilder> _cachedTypeFields;
        List<FieldBuilder> _cachedProtoFnFields;
        List<FieldBuilder> _cachedProtoImplFields;

        FieldBuilder _metaField;
        List<FieldBuilder> _closedOverFields;
        Dictionary<LocalBinding, FieldBuilder> _closedOverFieldsMap;

        protected int _altCtorDrops = 0;



        protected bool IsDefType { get { return Fields != null; } }
        protected virtual bool SupportsMeta { get { return !IsDefType; } }


        #endregion

        #region C-tors

        public ObjExpr2(object tag)
        {
            _tag = tag;
            Keywords = PersistentHashMap.EMPTY;
            Vars = PersistentHashMap.EMPTY;
            Closes = PersistentHashMap.EMPTY;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return true; }
        }

        public Type ClrType
        {
            get { return CompiledType ?? (Tag != null ? HostExpr.TagToType(Tag) : typeof(IFn)); }
        }

        #endregion

        #region eval

        public virtual object Eval()
        {
            if (IsDefType)
                return null;
            return Activator.CreateInstance(CompiledType);
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

        public Type Compile(Type superType, IPersistentVector interfaces, bool onetimeUse, GenContext context)
        {
            if (_compiledType != null)
                return _compiledType;

            string publicTypeName = IsDefType || (IsStatic && Compiler.IsCompiling) ? _internalName : _internalName + "__" + RT.nextID();

            //Console.WriteLine("DefFn {0}, {1}", publicTypeName, context.AssemblyBuilder.GetName().Name);

            TypeBuilder = context.AssemblyGen.DefinePublicType(publicTypeName, superType, true);
            for (int i = 0; i < interfaces.count(); i++)
                TypeBuilder.AddInterfaceImplementation((Type)interfaces.nth(i));

            ObjExpr.MarkAsSerializable(TypeBuilder);
            GenInterface.SetCustomAttributes(TypeBuilder, ClassMeta);

            EmitConstantFieldDefs(TypeBuilder);
            EmitKeywordCallsiteDefs(TypeBuilder);

            EmitStaticConstructor(TypeBuilder);

            if (SupportsMeta)
                _metaField = TypeBuilder.DefineField("__meta", typeof(IPersistentMap), FieldAttributes.Public | FieldAttributes.InitOnly);

            EmitClosedOverFields(TypeBuilder);
            EmitProtocolCallsites(TypeBuilder);

            CtorInfo = EmitConstructor(TypeBuilder, superType);

            if (_altCtorDrops > 0)
                EmitFieldOnlyConstructor(TypeBuilder, superType);

            if (SupportsMeta)
            {
                EmitNonMetaConstructor(TypeBuilder, superType);
                EmitMetaFunctions(TypeBuilder);
            }

            EmitStatics(context);
            EmitMethods(context);

            if (KeywordCallsites.count() > 0)
                EmitSwapThunk(TypeBuilder);

            _compiledType = TypeBuilder.CreateType();

            if (context.DynInitHelper != null)
                context.DynInitHelper.FinalizeType();

            return _compiledType;
        }

        #endregion

        
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

        // _constants => Constants
        private void EmitStaticConstructor(TypeBuilder fnTB)
        {
            ConstructorBuilder cb = fnTB.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
 
            if (Constants.count() > 0)
                EmitConstantFieldInits(cb);

            if (KeywordCallsites.count() > 0 )
                EmitKeywordCallsiteInits(cb);
                
            cb.GetILGenerator().Emit(OpCodes.Ret);
        }

        private void EmitKeywordCallsiteInits(ConstructorBuilder cb)
        {
            ILGenerator ilg = cb.GetILGenerator();

            for (int i = 0; i < KeywordCallsites.count(); i++)
            {
                Keyword k = (Keyword) KeywordCallsites.nth(i);
                EmitValue(k,ilg);
                ilg.Emit(OpCodes.Newobj,Compiler.Ctor_KeywordLookupSite_1);
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Stfld,_keywordLookupSiteFields[i]);
                ilg.Emit(OpCodes.Castclass,typeof(ILookupThunk));
                ilg.Emit(OpCodes.Stfld,_thunkFields[i]);
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
                    if ( ConstantFields[i] != null )
                    {
                        EmitValue(Constants.nth(i),ilg);
                        ilg.Emit(OpCodes.Castclass,ConstantType(i));
                        ilg.Emit(OpCodes.Stfld,ConstantFields[i]);
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

            gen.Emit(OpCodes.Call, CtorInfo);

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
            gen.Emit(OpCodes.Call, CtorInfo);
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

                gen.EmitNew(CtorInfo);
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

        // IMPORTED

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

        internal bool IsVolatile(LocalBinding lb)
        {
            return RT.booleanCast(RT.contains(Fields, lb.Symbol)) &&
                RT.booleanCast(RT.get(lb.Symbol.meta(), Keyword.intern("volatile-mutable")));
        }

        bool IsMutable(LocalBinding lb)
        {
            return IsVolatile(lb)
                ||
                RT.booleanCast(RT.contains(Fields, lb.Symbol)) &&
                   RT.booleanCast(RT.get(lb.Symbol.meta(), Keyword.intern("unsynchronized-mutable")))
                ||
                lb.IsByRef;
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

        // END- IMPORTED

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
            ConstantFields = new Dictionary<int, FieldBuilder>(Constants.count());

            for (int i = 0; i < Constants.count(); i++)
            {
                string fieldName = ConstantName(i);
                Type fieldType = ConstantType(i);
                if (!fieldType.IsPrimitive)
                {
                    FieldBuilder fb = baseTB.DefineField(fieldName, fieldType, FieldAttributes.FamORAssem | FieldAttributes.Static);
                    ConstantFields[i] = fb;

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

        public MethodBuilder EmitConstants(TypeBuilder fnTB)
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
                    ilg.Emit(OpCodes.Stfld,ConstantFields[i]);                    
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
                ilg.Emit(OpCodes.Ldstr,(String)value);
            else if (value is Boolean)
                ilg.EmitBoolean((Boolean)value);
            else if (value is Int32)
                ilg.EmitInt((int)value);
            else if ( value is Int64)
                ilg.EmitLong((long)value);
            else if (value is Double)
                ilg.EmitDouble((double)value);
            else if (value is Char)
                ilg.EmitChar((char)value);
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
                ilg.EmitString(sym.Namespace);
                ilg.EmitString(sym.Name);
                ilg.EmitCall(Compiler.Method_Symbol_intern2);
            }
            else if (value is Keyword)
            {
                Keyword keyword = (Keyword)value;
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
                    EmitValue(val,gen);
                    if (k.IsPrimitive)
                    {
                        ilg.Emit(OpCodes.Castclass,k);
                    }
                    
                }

                ConstructorInfo cinfo = value.GetType().GetConstructors()[0];
                ilg.EmitNew(cinfo);
            }
            else if (value is IRecord)
            {
                //MethodInfo[] minfos = value.GetType().GetMethods(BindingFlags.Static | BindingFlags.Public);
                EmitValue(PersistentArrayMap.create((IDictionary)value),gen);
  
                MethodInfo createMI = value.GetType().GetMethod("create", BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Standard, new Type[] {typeof(IPersistentMap)},null);
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
                EmitListAsObjectArray(value,gen);
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
                    ilg.Emit(OpCodes.Callvirt,Compiler.Method_IObj_withMeta);
                }
            }
        }

        private void EmitListAsObjectArray(object value, ILGenerator ilg)
        {
           ICollection coll = (ICollection)value;
            
            ilg.Emit(OpCodes.Ldc_I4,coll.Count);
            ilg.Emit(OpCodes.Newarr,typeof(Object));

            int i=0;
            foreach (Object item in (ICollection)value)
            {
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldc_I4,i);
                EmitValue(item,ilg);
                HostExpr.EmitBoxReturn(this,ilg,item.GetType());
                ilg.Emit(OpCodes.Stelem_Ref);
            }
        }

        #endregion

        public void Emit(RHC rhc, ObjExpr2 objx, GenContext context)
        {
            throw new NotImplementedException();
        }


        internal void EmitConstant(GenContext context, int id, object val)
        {
            if (val.GetType().IsPrimitive)
                EmitValue(val, context.GetILGenerator());
            else
                context.GetILGenerator().Emit(OpCodes.Ldsfld, ConstantFields[id]);
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

        internal FieldInfo ThunkField(int i)
        {
            return _thunkFields[i];
        }

        
        internal FieldInfo CachedTypeField(int i)
        {
            return _cachedTypeFields[i];
        }

        internal FieldInfo KeywordLookupSiteField(int i)
        {
            return _keywordLookupSiteFields[i];
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

            //if (_fnMode == FnMode.Full)

            if (Closes.containsKey(lb))
            {
                ilg.Emit(OpCodes.Ldloc_0); // this
                ilg.Emit(OpCodes.Ldfld,lb.HELP>WhatFieldDoesThisLBMapto?);
                Type primType = lb.PrimitiveType;
                if (primType != null)
                    HostExpr.EmitBoxReturn(this,context,primtype);
                // TODO: ONCEONLY?            }
            else
                return lb.ParamExpression;

        }

        internal void EmitUnboxedLocal(GenContext context, LocalBinding lb)
        {
            throw new NotImplementedException();
        }

        internal void EmitAssignLocal(GenContext context, LocalBinding lb, Expr val)
        {
            throw new NotImplementedException();
        }

        internal void EmitLetFnInits(GenContext context, LocalBuilder localBuilder, ObjExpr2 objx, IPersistentSet letFnLocals)
        {
            ILGenerator ilg = context.GetILGenerator();

            ilg.Emit(OpCodes.Castclass,TypeBuilder);

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
        }
         


        public bool IsStatic { get; set; }

    }
}
