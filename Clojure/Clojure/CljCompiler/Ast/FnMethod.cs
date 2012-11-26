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
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace clojure.lang.CljCompiler.Ast
{
    class FnMethod : ObjMethod
    {
        #region Data
        
        protected IPersistentVector _reqParms = PersistentVector.EMPTY;  // localbinding => localbinding
        protected LocalBinding _restParm = null;
        Type[] _argTypes;
        Type _retType;

        string _prim;

        public override string Prim
        {
            get { return _prim; }
        }

        public DynamicMethod DynMethod { get; set; }


        #endregion

        #region C-tors

        public FnMethod(FnExpr fn, ObjMethod parent)
            :base(fn,parent)
        {
        }

        // For top-level compilation only
        // TODO: Can we get rid of this when the DLR-based compile goes away?
        public FnMethod(FnExpr fn, ObjMethod parent, BodyExpr body)
            :base(fn,parent)
        {
            _body = body;
            _argLocals = PersistentVector.EMPTY;
            //_thisBinding = Compiler.RegisterLocal(Symbol.intern(fn.ThisName ?? "fn__" + RT.nextID()), null, null, false);
        }

        #endregion

        #region ObjMethod methods

        internal override bool IsVariadic
        {
            get { return _restParm != null; }
        }

        internal override int NumParams
        {
            get { return _reqParms.count() + (IsVariadic ? 1 : 0); }
        }

        internal override int RequiredArity
        {
            get { return _reqParms.count(); }
        } 

        internal override string MethodName
        {
            get { return IsVariadic ? "doInvoke" : "invoke"; }
        }

        protected override string StaticMethodName
        {
            get
            {
                if (Objx.IsStatic && Compiler.IsCompiling)
                    return "InvokeStatic";
                else
                    return String.Format("__invokeHelper_{0}{1}", RequiredArity, IsVariadic ? "v" : string.Empty);
            }
        }

        protected override Type[] StaticMethodArgTypes
        {
            get
            {
                if (_argTypes != null)
                    return _argTypes;

                return ArgTypes;
            }
        }

        protected override Type[] ArgTypes
        {
            get
            {
                if (IsVariadic && _reqParms.count() == Compiler.MaxPositionalArity)
                {
                    Type[] ret = new Type[Compiler.MaxPositionalArity + 1];
                    for (int i = 0; i < Compiler.MaxPositionalArity + 1; i++)
                        ret[i] = typeof(Object);
                    return ret;
                }

                return Compiler.CreateObjectTypeArray(NumParams);
            }
        }

        protected override Type ReturnType
        {
            get { return typeof(object); }
        }

        protected override Type StaticReturnType
        {
            get
            {
                if ( _prim != null ) // Objx.IsStatic)
                    return _retType;

                return typeof(object);
            }
        }

        #endregion

        #region Parsing

        enum ParamParseState { Required, Rest, Done };

        internal static FnMethod Parse(FnExpr fn, ISeq form, bool isStatic)
        {
            // ([args] body ... )

            IPersistentVector parms = (IPersistentVector)RT.first(form);
            ISeq body = RT.next(form);

            try
            {
                FnMethod method = new FnMethod(fn, (ObjMethod)Compiler.MethodVar.deref());
                method.SpanMap = (IPersistentMap)Compiler.SourceSpanVar.deref();

                Var.pushThreadBindings(RT.mapUniqueKeys(
                    Compiler.MethodVar, method,
                    Compiler.LocalEnvVar, Compiler.LocalEnvVar.deref(),
                    Compiler.LoopLocalsVar, null,
                    Compiler.NextLocalNumVar, 0));

                method._prim = PrimInterface(parms);
                //if (method._prim != null)
                //    method._prim = method._prim.Replace('.', '/');

                method._retType = Compiler.TagType(Compiler.TagOf(parms));
                if (method._retType.IsPrimitive && !(method._retType == typeof(double) || method._retType == typeof(long)))
                    throw new ParseException("Only long and double primitives are supported");

                // register 'this' as local 0  
                if ( !isStatic )
                    //method._thisBinding = Compiler.RegisterLocalThis(Symbol.intern(fn.ThisName ?? "fn__" + RT.nextID()), null, null);
                    Compiler.RegisterLocalThis(Symbol.intern(fn.ThisName ?? "fn__" + RT.nextID()), null, null);

                ParamParseState paramState = ParamParseState.Required;
                IPersistentVector argLocals = PersistentVector.EMPTY;
                List<Type> argTypes = new List<Type>();

                int parmsCount = parms.count();

                for (int i = 0; i < parmsCount; i++)
                {
                    if (!(parms.nth(i) is Symbol))
                        throw new ParseException("fn params must be Symbols");
                    Symbol p = (Symbol)parms.nth(i);
                    if (p.Namespace != null)
                        throw new ParseException("Can't use qualified name as parameter: " + p);
                    if (p.Equals(Compiler.AmpersandSym))
                    {
                        //if (isStatic)
                        //    throw new Exception("Variadic fns cannot be static");

                        if (paramState == ParamParseState.Required)
                            paramState = ParamParseState.Rest;
                        else
                            throw new ParseException("Invalid parameter list");
                    }
                    else
                    {
                        Type pt = Compiler.PrimType(Compiler.TagType(Compiler.TagOf(p)));

                        if (pt.IsPrimitive && !(pt == typeof(double) || pt == typeof(long)))
                            throw new ParseException("Only long and double primitives are supported: " + p);

                        if (paramState == ParamParseState.Rest && Compiler.TagOf(p) != null)
                            throw new ParseException("& arg cannot have type hint");
                        if (paramState == ParamParseState.Rest && method.Prim != null)
                            throw new ParseException("fns taking primitives cannot be variadic");
                        if (paramState == ParamParseState.Rest)
                            pt = typeof(ISeq);
                        argTypes.Add(pt);
                        LocalBinding b = pt.IsPrimitive
                            ? Compiler.RegisterLocal(p,null, new MethodParamExpr(pt), true)
                            : Compiler.RegisterLocal(p,
                            paramState == ParamParseState.Rest ? Compiler.ISeqSym : Compiler.TagOf(p),
                            null,true);

                        argLocals = argLocals.cons(b);
                        switch (paramState)
                        {
                            case ParamParseState.Required:
                                method._reqParms = method._reqParms.cons(b);
                                break;
                            case ParamParseState.Rest:
                                method._restParm = b;
                                paramState = ParamParseState.Done;
                                break;
                            default:
                                throw new ParseException("Unexpected parameter");
                        }
                    }
                }

                if (method.RequiredArity > Compiler.MaxPositionalArity)
                    throw new ParseException(string.Format("Can't specify more than {0} parameters", Compiler.MaxPositionalArity));
                Compiler.LoopLocalsVar.set(argLocals);
                method._argLocals = argLocals;
                //if (isStatic)
                if ( method.Prim != null )
                    method._argTypes = argTypes.ToArray();
                method._body = (new BodyExpr.Parser()).Parse(new ParserContext(RHC.Return),body);
                return method;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }

        #endregion

        #region primitive interfaces support

        public static char TypeChar(object x)
        {
            //Type t = null;
            //if (x is Type)
            //    t = (Type)x;
            //else if (x is Symbol)
            //    t = Compiler.PrimType((Symbol)x);
            Type t = x as Type ?? Compiler.PrimType(x as Symbol);

            if (t == null || !t.IsPrimitive)
                return 'O';
            if (t == typeof(long))
                return 'L';
            if (t == typeof(double))
                return 'D';
            throw new ArgumentException("Only long and double primitives are supported");
        }

        public static bool IsPrimType(object x)
        {
            Type t = x as Type ?? Compiler.PrimType(x as Symbol);

            if (t == typeof(long) || t == typeof(double))
                return true;

            return false;
        }
       

        public static string PrimInterface(IPersistentVector arglist)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arglist.count(); i++)
                sb.Append(TypeChar(Compiler.TagOf(arglist.nth(i))));
            sb.Append(TypeChar(Compiler.TagOf(arglist)));
            string ret = sb.ToString();
            bool prim = ret.Contains("L") || ret.Contains("D");
            if (prim && arglist.count() > 4)
                throw new ArgumentException("fns taking primitives support only 4 or fewer args");
            if (prim)
                return "clojure.lang.primifs." + ret;
            return null;
        }

        public static bool IsPrimInterface(IPersistentVector arglist)
        {
            if (arglist.count() > 4)
                return false;

            for (int i = 0; i < arglist.count(); i++)
                if (IsPrimType(Compiler.TagOf(arglist.nth(i))))
                    return true;

            if (IsPrimType(Compiler.TagOf(arglist)))
                return true;

            return false;
        }

        public static bool HasPrimInterface(ISeq form)
        {
            IPersistentVector parms = RT.first(form) as IPersistentVector;

            return parms != null && IsPrimInterface(parms);
        }
         
        #endregion

        #region Code generation

        protected override string GetMethodName()
        {
            return IsVariadic ? "doInvoke" : "invoke";
        }

        protected override Type GetReturnType()
        {
            if (_prim != null) // Objx.IsStatic)
                return _retType;

            return typeof(object);
        }

        protected override Type[] GetArgTypes()
        {
            if (IsVariadic && _reqParms.count() == Compiler.MaxPositionalArity)
            {
                Type[] ret = new Type[Compiler.MaxPositionalArity + 1];
                for (int i = 0; i < Compiler.MaxPositionalArity + 1; i++)
                    ret[i] = typeof(Object);
                return ret;
            }
            return Compiler.CreateObjectTypeArray(NumParams);
        }

        public override void Emit(ObjExpr fn, TypeBuilder tb)
        {
            if (Prim != null)
                DoEmitPrim(fn, tb);
            else if (fn.IsStatic)
                DoEmitStatic(fn, tb);
            else
                DoEmit(fn, tb);
        }


        internal void LightEmit(ObjExpr fn, Type fnType)
        {
            if (DynMethod != null)
                return;

            if (Prim != null || fn.IsStatic)
                throw new InvalidOperationException("No light compile allowed for static methods or methods with primitive interfaces");

            Type[] argTypes = ClrExtensions.ArrayInsert(fnType,GetArgTypes());

            DynamicMethod meth = new DynamicMethod(GetMethodName(), GetReturnType(), argTypes, true);
      
            CljILGen baseIlg = new CljILGen(meth.GetILGenerator());

            try
            {
                Label loopLabel = baseIlg.DefineLabel();
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar, loopLabel, Compiler.MethodVar, this));

                //GenContext.EmitDebugInfo(baseIlg, SpanMap);

                baseIlg.MarkLabel(loopLabel);
                _body.Emit(RHC.Return, fn, baseIlg);
                if (_body.HasNormalExit())
                    baseIlg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }

            DynMethod = meth;
        }

        private void DoEmitStatic(ObjExpr fn, TypeBuilder tb)
        {
           DoEmitPrimOrStatic(fn, tb ,true);   
        }

        private void DoEmitPrim(ObjExpr fn, TypeBuilder tb)
        {
            DoEmitPrimOrStatic(fn,tb,false);
        }

        private void DoEmitPrimOrStatic(ObjExpr fn, TypeBuilder tb, bool isStatic)
        {
            MethodAttributes attribs = isStatic 
                ? MethodAttributes.Static | MethodAttributes.Public
                : MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual;

            string methodName = isStatic ? "invokeStatic" : "invokePrim";

            MethodBuilder baseMB = tb.DefineMethod(methodName, attribs, GetReturnType(), _argTypes);

            if ( ! isStatic )
                SetCustomAttributes(baseMB);

            CljILGen baseIlg = new CljILGen(baseMB.GetILGenerator());

            try 
            {
                Label loopLabel = baseIlg.DefineLabel();
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar,loopLabel,Compiler.MethodVar,this));

                GenContext.EmitDebugInfo(baseIlg, SpanMap);
                
                baseIlg.MarkLabel(loopLabel);
                EmitBody(Objx, baseIlg, _retType, _body);
                if ( _body.HasNormalExit() )
                    baseIlg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }            
            // Generate the regular invoke, calling the static or prim method

            MethodBuilder regularMB = tb.DefineMethod(GetMethodName(), MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual, typeof(Object), GetArgTypes());
            SetCustomAttributes(regularMB);

            CljILGen regIlg = new CljILGen(regularMB.GetILGenerator());

            if ( ! isStatic )
                regIlg.Emit(OpCodes.Ldarg_0);
            for(int i = 0; i < _argTypes.Length; i++)
			{   
                regIlg.EmitLoadArg(i+1);
                HostExpr.EmitUnboxArg(fn, regIlg, _argTypes[i]);
			}
            regIlg.Emit(OpCodes.Call,baseMB);
            if ( GetReturnType().IsValueType)
                regIlg.Emit(OpCodes.Box,GetReturnType());
            regIlg.Emit(OpCodes.Ret);
        }

        private void DoEmit(ObjExpr fn, TypeBuilder tb)
        {
            MethodAttributes attribs = MethodAttributes.ReuseSlot | MethodAttributes.Public | MethodAttributes.Virtual;

            MethodBuilder mb = tb.DefineMethod(GetMethodName(), attribs, GetReturnType(), GetArgTypes());

            SetCustomAttributes(mb);

            CljILGen baseIlg = new CljILGen(mb.GetILGenerator());

            try
            {
                Label loopLabel = baseIlg.DefineLabel();
                Var.pushThreadBindings(RT.map(Compiler.LoopLabelVar, loopLabel, Compiler.MethodVar, this));

                GenContext.EmitDebugInfo(baseIlg, SpanMap);

                baseIlg.MarkLabel(loopLabel);
                _body.Emit(RHC.Return, fn, baseIlg);
                if ( _body.HasNormalExit() )
                    baseIlg.Emit(OpCodes.Ret);
            }
            finally
            {
                Var.popThreadBindings();
            }

            if (IsExplicit)
                tb.DefineMethodOverride(mb, _explicitMethodInfo);
        }

        #endregion
    }
}
