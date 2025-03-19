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
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace clojure.lang.CljCompiler.Ast
{
    public abstract class HostExpr : Expr, MaybePrimitiveExpr
    {
        #region Symbols

        public static readonly Symbol ByRefSym = Symbol.intern("by-ref");
        public static readonly Symbol TypeArgsSym = Symbol.intern("type-args");

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object form)
            {
                string source = (string)Compiler.SourceVar.deref();
                IPersistentMap spanMap = (IPersistentMap)Compiler.SourceSpanVar.deref();  // Compiler.GetSourceSpanMap(form);

                ISeq sform = (ISeq)form;

                Symbol tag = Compiler.TagOf(sform);
                bool tailPosition = Compiler.InTailCall(pcon.Rhc);

                // form is one of:
                //  (. x fieldname-sym)
                //  (. x 0-ary-methodname-sym)
                //  (. x propertyname-sym)
                //  (. x methodname-sym args+)
                //  (. x (methodname-sym args?))
                //
                //  args might have a first element of the form (type-args t1 ...) to supply types for generic method calls

                // Parse into canonical form:
                // Target + memberName + args
                //
                //  (. x fieldname-sym)             Target = x member-name = fieldname-sym, args = null
                //  (. x 0-ary-methodname-sym)      Target = x member-name = 0-ary-method, args = null
                //  (. x propertyname-sym)          Target = x member-name = propertyname-sym, args = null
                //  (. x methodname-sym args+)      Target = x member-name = methodname-sym, args = args+
                //  (. x (methodname-sym args?))    Target = x member-name = methodname-sym, args = args?  -- note: in this case, we explicity cannot be a field or property


                if (RT.Length(sform) < 3)
                    throw new ParseException("Malformed member expression, expecting (. target member ... ) or  (. target (member ...))");

                var target = RT.second(sform);
                Symbol methodSym;
                ISeq args;
                bool methodRequired = false;

                if ( RT.third(sform) is Symbol s)
                {
                    methodSym = s;
                    args = RT.next(RT.next(RT.next(sform)));
                }
                else if (RT.Length(sform) == 3  && RT.third(sform) is ISeq seq)
                {
                    var seqFirst = RT.first(seq);
                    if (seqFirst is Symbol sym)
                    {
                        methodSym = sym;
                        args = RT.next(seq);
                        methodRequired = true;
                    }
                    else
                        throw new ParseException("Malformed member expression, expecting (. target member-name args... )  or (. target (member-name args...), where member-name is a Symbol");
                }
                else
                    throw new ParseException("Malformed member expression, expecting (. target member-name args... )  or (. target (member-name args...)");



                // determine static or instance be examinung the target
                // static target must be symbol, either fully.qualified.Typename or Typename that has been imported
                // If target does not resolve to a type, then it must be an instance call -- parse it.

                Type staticType = HostExpr.MaybeType(target, false);
                Expr instance = staticType == null ? Compiler.Analyze(pcon.EvalOrExpr(), RT.second(sform)) : null;

                // staticType not null => static method call, instance set to null.
                // staticType null, instance is not null, set to an expression yielding the instance to make the call on.

                // If there is a type-args form, it must be the first argument.
                // Pull it out if it's there.

                GenericTypeArgList typeArgs;

                object firstArg = RT.first(args);
                if (firstArg is ISeq && RT.first(firstArg) is Symbol symbol && symbol.Equals(TypeArgsSym))
                {
                    // We have a type args supplied for a generic method call
                    // (. thing methodname (type-args type1 ... ) args ...)
                    typeArgs = GenericTypeArgList.Create(RT.next(firstArg));
                    args = args.next();
                }
                else
                {
                    typeArgs = GenericTypeArgList.Empty;
                }

                bool hasTypeArgs = !typeArgs.IsEmpty;

                bool isZeroArityCall = RT.Length(args) == 0 && !methodRequired;

                if (isZeroArityCall)
                {
                    PropertyInfo pinfo;
                    FieldInfo finfo;

                    // TODO: Figure out if we want to handle the -propname otherwise.

                    bool isPropName = false;
                    Symbol memberSym = methodSym;
                    
                    if (memberSym.Name[0] == '-')
                    {
                        isPropName = true;
                        memberSym = Symbol.intern(memberSym.Name.Substring(1));
                    }

                    string memberName = Compiler.munge(memberSym.Name);

                    // The JVM version does not have to worry about Properties.  It captures 0-arity methods under fields.
                    // We have to put in special checks here for this.
                    // Also, when reflection is required, we have to capture 0-arity methods under the calls that
                    //   are generated by StaticFieldExpr and InstanceFieldExpr.
                    if (staticType != null)
                    {
                        if ( ! hasTypeArgs && (finfo = Reflector.GetField(staticType, memberName, true)) != null)
                            return new StaticFieldExpr(source, spanMap, tag, staticType, memberName, finfo);
                        if ( ! hasTypeArgs && (pinfo = Reflector.GetProperty(staticType, memberName, true)) != null)
                            return new StaticPropertyExpr(source, spanMap, tag, staticType, memberName, pinfo);
                        if (!isPropName && Reflector.GetArityZeroMethod(staticType, memberName, typeArgs, true) != null)
                            return new StaticMethodExpr(source, spanMap, tag, staticType, memberName, typeArgs, new List<HostArg>(), tailPosition);

                        string typeArgsStr = hasTypeArgs ? $" and generic type args {typeArgs.GenerateGenericTypeArgsString()} " : "";
                        throw new MissingMemberException($"No field, property, or method taking 0 args{typeArgsStr} named {memberName} found for {staticType.Name}");
                    }
                    else if (instance != null && instance.HasClrType && instance.ClrType != null)
                    {
                        Type instanceType = instance.ClrType;
                        if (!hasTypeArgs && (finfo = Reflector.GetField(instanceType, memberName, false)) != null)
                            return new InstanceFieldExpr(source, spanMap, tag, instance, memberName, finfo);
                        if (!hasTypeArgs && (pinfo = Reflector.GetProperty(instanceType, memberName, false)) != null)
                            return new InstancePropertyExpr(source, spanMap, tag, instance, memberName, pinfo);
                        if (!isPropName && Reflector.GetArityZeroMethod(instanceType, memberName, typeArgs, false) != null)
                            return new InstanceMethodExpr(source, spanMap, tag, instance, instanceType, memberName, typeArgs, new List<HostArg>(), tailPosition);
                        if (pcon.IsAssignContext)
                            return new InstanceFieldExpr(source, spanMap, tag, instance, memberName, null); // same as InstancePropertyExpr when last arg is null
                        else
                            return new InstanceZeroArityCallExpr(source, spanMap, tag, instance, memberName);
                    }
                    else
                    {
                        //  t is null, so we know this is not a static call
                        //  If instance is null, we are screwed anyway.
                        //  If instance is not null, then we don't have a type.
                        //  So we must be in an instance call to a property, field, or 0-arity method.
                        //  The code generated by InstanceFieldExpr/InstancePropertyExpr with a null FieldInfo/PropertyInfo
                        //     will generate code to do a runtime call to a Reflector method that will check all three.
                        //return new InstanceFieldExpr(source, spanMap, tag, instance, fieldName, null); // same as InstancePropertyExpr when last arg is null
                        //return new InstanceZeroArityCallExpr(source, spanMap, tag, instance, fieldName); 
                        if (pcon.IsAssignContext)
                            return new InstanceFieldExpr(source, spanMap, tag, instance, memberName, null); // same as InstancePropertyExpr when last arg is null
                        else
                            return new InstanceZeroArityCallExpr(source, spanMap, tag, instance, memberName); 

                    }
                }

                string methodName = Compiler.munge(methodSym.Name);


                List<HostArg> hostArgs = ParseArgs(pcon, args);

                return staticType != null
                    ? (MethodExpr)(new StaticMethodExpr(source, spanMap, tag, staticType, methodName, typeArgs, hostArgs, tailPosition))
                    : (MethodExpr)(new InstanceMethodExpr(source, spanMap, tag, instance, staticType, methodName, typeArgs, hostArgs, tailPosition));
            }
        }


        internal static List<HostArg> ParseArgs(ParserContext pcon, ISeq argSeq)
        {
            List<HostArg> args = new List<HostArg>();

            for (ISeq s = argSeq; s != null; s = s.next())
            {
                object arg = s.first();

                HostArg.ParameterType paramType = HostArg.ParameterType.Standard;
                LocalBinding lb = null;

                if (arg is ISeq argAsSeq)
                {
                    Symbol op = RT.first(argAsSeq) as Symbol;
                    if (op != null && op.Equals(ByRefSym))
                    {
                        if (RT.Length(argAsSeq) != 2)
                            throw new ArgumentException("Wrong number of arguments to {0}", op.Name);

                        object localArg = RT.second(argAsSeq);
                        Symbol symLocalArg = localArg as Symbol;
                        if (symLocalArg == null || (lb = Compiler.ReferenceLocal(symLocalArg)) == null)
                            throw new ArgumentException("Argument to {0} must be a local variable.", op.Name);

                        paramType = HostArg.ParameterType.ByRef;

                        arg = localArg;
                    }
                }

                Expr expr = Compiler.Analyze(pcon.EvalOrExpr(),arg);

                args.Add(new HostArg(paramType, expr, lb));
            }

            return args;

        }

        #endregion

        #region Expr Members

        public abstract bool HasClrType { get; }
        public abstract Type ClrType { get; }
        public abstract object Eval();
        public abstract void Emit(RHC rhc, ObjExpr objx, CljILGen ilg);

        #endregion

        #region MaybePrimitiveExpr 

        public abstract bool CanEmitPrimitive { get; }

        public abstract void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg);

        #endregion

        #region Reflection helpers

        internal static readonly MethodInfo Method_RT_sbyteCast = typeof(RT).GetMethod("sbyteCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_byteCast = typeof(RT).GetMethod("byteCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_shortCast = typeof(RT).GetMethod("shortCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_ushortCast = typeof(RT).GetMethod("ushortCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_intCast = typeof(RT).GetMethod("intCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uintCast = typeof(RT).GetMethod("uintCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_longCast = typeof(RT).GetMethod("longCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_ulongCast = typeof(RT).GetMethod("ulongCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_floatCast = typeof(RT).GetMethod("floatCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_doubleCast = typeof(RT).GetMethod("doubleCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_charCast = typeof(RT).GetMethod("charCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_decimalCast = typeof(RT).GetMethod("decimalCast", new Type[] { typeof(object) });

        internal static readonly MethodInfo Method_RT_uncheckedSbyteCast = typeof(RT).GetMethod("uncheckedSByteCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedByteCast = typeof(RT).GetMethod("uncheckedByteCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedShortCast = typeof(RT).GetMethod("uncheckedShortCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedUshortCast = typeof(RT).GetMethod("uncheckedUShortCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedIntCast = typeof(RT).GetMethod("uncheckedIntCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedUintCast = typeof(RT).GetMethod("uncheckedUIntCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedLongCast = typeof(RT).GetMethod("uncheckedLongCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedUlongCast = typeof(RT).GetMethod("uncheckedULongCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedFloatCast = typeof(RT).GetMethod("uncheckedFloatCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedDoubleCast = typeof(RT).GetMethod("uncheckedDoubleCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedCharCast = typeof(RT).GetMethod("uncheckedCharCast", new Type[] { typeof(object) });
        internal static readonly MethodInfo Method_RT_uncheckedDecimalCast = typeof(RT).GetMethod("uncheckedDecimalCast", new Type[] { typeof(object) });

        internal static readonly MethodInfo Method_RT_booleanCast = typeof(RT).GetMethod("booleanCast", new Type[] { typeof(object) });

        internal static readonly MethodInfo Method_RT_intPtrCast = typeof (RT).GetMethod("intPtrCast", new Type[] { typeof (object) });
        internal static readonly MethodInfo Method_RT_uintPtrCast = typeof (RT).GetMethod("uintPtrCast", new Type[] { typeof (object) });

        #endregion

        #region Tags and types
        
        public static Type MaybeType(object form, bool stringOk)
        {
            if (form is Type type)
                return type;

            Type t = null;
            if (form is Symbol sym)
            {
                if (sym.Namespace == null) // if ns-qualified, can't be classname
                {
                    if (Util.equals(sym, Compiler.CompileStubSymVar.get()))
                        return (Type)Compiler.CompileStubClassVar.get();
                    if (sym.Name.IndexOf('.') > 0 || sym.Name[sym.Name.Length - 1] == ']')  // Array.  JVM version detects [whatever  notation.

                        t = RT.classForNameE(sym.Name);
                    else
                    {
                        object o = Compiler.CurrentNamespace.GetMapping(sym);
                        if (o is Type type1)
                        {
                            t = type1;

                            var tName = type1.FullName;
                            var compiledType = Compiler.FindDuplicateCompiledType(tName);
                            if (compiledType is not null && Compiler.IsCompiling)
                                t =  compiledType;
                        }
                        else if (Compiler.LocalEnvVar.deref() != null && ((IPersistentMap)Compiler.LocalEnvVar.deref()).containsKey(form))  // JVM casts to java.util.Map
                            return null;
                        else
                        {
                            try
                            {
                                t = RT.classForName(sym.Name);
                            }
                            catch (Exception)
                            {
                                // aargh
                                // leave t set to null -> return null
                            }
                        }
                    }

                }
            }
            else if (stringOk && form is string str)
                t = RT.classForNameE(str);

            return t;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static Type maybeSpecialTag(Symbol sym)
        {
            Type t = Compiler.PrimType(sym);
            switch (sym.Name)
            {
                case "objects": t = typeof(object[]); break;
                case "ints": t = typeof(int[]); break;
                case "longs": t = typeof(long[]); break;
                case "floats": t = typeof(float[]); break;
                case "doubles": t = typeof(double[]); break;
                case "chars": t = typeof(char[]); break;
                case "shorts": t = typeof(short[]); break;
                case "bytes": t = typeof(byte[]); break;
                case "booleans":
                case "bools": t = typeof(bool[]); break;
                case "uints": t = typeof(uint[]); break;
                case "ushorts": t = typeof(ushort[]); break;
                case "ulongs": t = typeof(ulong[]); break;
                case "sbytes": t = typeof(sbyte[]); break;
            }
            return t;
        }

        internal static Type TagToType(object tag)
        {
            Type t = null;

            Symbol sym = tag as Symbol;
            if (sym != null)
            {
                if (sym.Namespace == null)
                {
                    t = maybeSpecialTag(sym);
                }
                if (t == null)
                {
                    t = HostExpr.MaybeArrayType(sym);
                }
            }
           
            if ( t == null )
                t = MaybeType(tag, true);

            if (t != null)
                return t;

            throw new ArgumentException("Unable to resolve typename: " + tag);
        }

        public static Type MaybeArrayType(Symbol sym)
        {
            if (sym.Namespace == null || !Util.IsPosDigit(sym.Name))
                return null;

            int dim = sym.Name[0] - '0';
            Symbol componentTypeName = Symbol.intern(null, sym.Namespace);
            Type componentType = Compiler.PrimType(componentTypeName);
            if (componentType == null)
                componentType = HostExpr.MaybeType(componentTypeName, false);

            if (componentType == null)
                throw new TypeNotFoundException("Unable to resolve component typename: " + componentTypeName);

            // componentType.MakeArrayType(dim) is not what you want.  This creates .IsVariableBound designed for multiple dimensions.
            // We are matching Java which means we have jagged arrays.
            // Without an argument, MakeArrayType returns an SZ array -- one-dimensional, zero-based.
            // we need to nest.

            for (int i = 0; i < dim; i++)
                componentType = componentType.MakeArrayType();

            return componentType;
        }

        #endregion

        #region Code generation

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        internal static void EmitBoxReturn(ObjExpr objx, CljILGen ilg, Type returnType)

        {
            if (returnType == typeof(void))
                ilg.Emit(OpCodes.Ldnull);
            else if (returnType.IsPrimitive || returnType.IsValueType)
                ilg.Emit(OpCodes.Box, returnType);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        internal static void EmitUnboxArg(ObjExpr objx, CljILGen ilg, Type paramType)
        {
            EmitUnboxArg(ilg, paramType);
        }

        internal static void EmitUnboxArg(CljILGen ilg, Type paramType)
        {
            if (paramType.IsPrimitive)
            {
                MethodInfo m;
                if (paramType == typeof(bool))
                {
                    m = HostExpr.Method_RT_booleanCast;
                }
                else if (paramType == typeof(char))
                {
                    m = HostExpr.Method_RT_charCast;
                }
                else if(paramType == typeof(IntPtr))
                {
                    m = HostExpr.Method_RT_intPtrCast;
                }
                else if(paramType == typeof(UIntPtr))
                {
                    m = HostExpr.Method_RT_uintPtrCast;
                }
                else
                {
                    var typeCode = Type.GetTypeCode(paramType);
                    if (RT.booleanCast(RT.UncheckedMathVar.deref()))
                    {
                        switch (typeCode)
                        {
                            case TypeCode.SByte:
                                m = HostExpr.Method_RT_uncheckedSbyteCast;
                                break;
                            case TypeCode.Byte:
                                m = HostExpr.Method_RT_uncheckedByteCast;
                                break;
                            case TypeCode.Int16:
                                m = HostExpr.Method_RT_uncheckedShortCast;
                                break;
                            case TypeCode.UInt16:
                                m = HostExpr.Method_RT_uncheckedUshortCast;
                                break;
                            case TypeCode.Int32:
                                m = HostExpr.Method_RT_uncheckedIntCast;
                                break;
                            case TypeCode.UInt32:
                                m = HostExpr.Method_RT_uncheckedUintCast;
                                break;
                            case TypeCode.Int64:
                                m = HostExpr.Method_RT_uncheckedLongCast;
                                break;
                            case TypeCode.UInt64:
                                m = HostExpr.Method_RT_uncheckedUlongCast;
                                break;
                            case TypeCode.Single:
                                m = HostExpr.Method_RT_uncheckedFloatCast;
                                break;
                            case TypeCode.Double:
                                m = HostExpr.Method_RT_uncheckedDoubleCast;
                                break;
                            case TypeCode.Char:
                                m = HostExpr.Method_RT_uncheckedCharCast;
                                break;
                            case TypeCode.Decimal:
                                m = HostExpr.Method_RT_uncheckedDecimalCast;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("paramType", paramType, string.Format("Don't know how to handle typeCode {0} for paramType", typeCode));
                        }
                    }
                    else
                    {
                        switch (typeCode)
                        {
                            case TypeCode.SByte:
                                m = HostExpr.Method_RT_sbyteCast;
                                break;
                            case TypeCode.Byte:
                                m = HostExpr.Method_RT_byteCast;
                                break;
                            case TypeCode.Int16:
                                m = HostExpr.Method_RT_shortCast;
                                break;
                            case TypeCode.UInt16:
                                m = HostExpr.Method_RT_ushortCast;
                                break;
                            case TypeCode.Int32:
                                m = HostExpr.Method_RT_intCast;
                                break;
                            case TypeCode.UInt32:
                                m = HostExpr.Method_RT_uintCast;
                                break;
                            case TypeCode.Int64:
                                m = HostExpr.Method_RT_longCast;
                                break;
                            case TypeCode.UInt64:
                                m = HostExpr.Method_RT_ulongCast;
                                break;
                            case TypeCode.Single:
                                m = HostExpr.Method_RT_floatCast;
                                break;
                            case TypeCode.Double:
                                m = HostExpr.Method_RT_doubleCast;
                                break;
                            case TypeCode.Char:
                                m = HostExpr.Method_RT_charCast;
                                break;
                            case TypeCode.Decimal:
                                m = HostExpr.Method_RT_decimalCast;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("paramType", paramType, string.Format("Don't know how to handle typeCode {0} for paramType", typeCode));
                        }
                    }
                }

                ilg.Emit(OpCodes.Castclass, typeof(Object));
                ilg.Emit(OpCodes.Call,m);
            }
            else
            {
                // TODO: Properly handle value types here.  Really, we need to know the incoming type.
                if (paramType.IsValueType)
                {
                    ilg.Emit(OpCodes.Unbox_Any, paramType);
                }
                else
                    ilg.Emit(OpCodes.Castclass, paramType);
            }
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
