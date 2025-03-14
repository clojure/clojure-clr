/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using clojure.lang.CljCompiler.Context;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public class InvokeExpr : Expr
    {
        #region Data

        readonly Expr _fexpr;
        public Expr FExpr { get { return _fexpr; } }

        readonly Object _tag;
        public Object Tag { get { return _tag; } }
        
        readonly IPersistentVector _args;
        public IPersistentVector Args { get { return _args; } }
        
        readonly string _source;
        public string Source { get { return _source; } }
        
        readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }
        
        readonly bool _tailPosition;
        public bool TailPosition { get { return _tailPosition; } }

        readonly bool _isProtocol = false;
        public bool IsProtocol { get { return _isProtocol; } }

        readonly int _siteIndex = -1;
        public int SiteIndex { get { return _siteIndex; } }

        readonly Type _protocolOn;
        public Type ProtocolOn { get { return _protocolOn; } }

        readonly MethodInfo _onMethod;
        public MethodInfo OnMethod { get { return _onMethod; } }

        static readonly Keyword _onKey = Keyword.intern("on");
        static readonly Keyword _methodMapKey = Keyword.intern("method-map");

        Type _cachedType;

        #endregion

        #region Ctors

        public InvokeExpr(string source, IPersistentMap spanMap, Symbol tag, Expr fexpr, IPersistentVector args, bool tailPosition)
        {
            _source = source;
            _spanMap = spanMap;
            _fexpr = fexpr;
            _args = args;
            _tailPosition = tailPosition;

            VarExpr varFexpr = fexpr as VarExpr;

            if (varFexpr != null)
            {
                Var fvar = varFexpr.Var;
                Var pvar = (Var)RT.get(fvar.meta(), Compiler.ProtocolKeyword);
                if (pvar != null && Compiler.ProtocolCallsitesVar.isBound)
                {
                    _isProtocol = true;
                    _siteIndex = Compiler.RegisterProtocolCallsite(fvar);
                    Object pon = RT.get(pvar.get(), _onKey);
                    _protocolOn = HostExpr.MaybeType(pon, false);
                    if (_protocolOn != null)
                    {
                        IPersistentMap mmap = (IPersistentMap)RT.get(pvar.get(), _methodMapKey);
                        Keyword mmapVal = (Keyword)mmap.valAt(Keyword.intern(fvar.sym));
                        if (mmapVal == null)
                        {
                            throw new ArgumentException(String.Format("No method of interface: {0} found for function: {1} of protocol: {2} (The protocol method may have been defined before and removed.)",
                                _protocolOn.FullName, fvar.Symbol, pvar.Symbol));
                        }
                        String mname = Compiler.munge(mmapVal.Symbol.ToString());
                       
                        IList<MethodBase> methods = Reflector.GetMethods(_protocolOn, mname, GenericTypeArgList.Empty, args.count() - 1,  false);
                        if (methods.Count != 1)
                            throw new ArgumentException(String.Format("No single method: {0} of interface: {1} found for function: {2} of protocol: {3}",
                                mname, _protocolOn.FullName, fvar.Symbol, pvar.Symbol));
                        _onMethod = (MethodInfo) methods[0];
                    }
                }
            }

            if (tag != null)
                _tag = tag;
            else if (varFexpr != null)
            {
                Var v = varFexpr.Var;

                //object arglists = RT.get(RT.meta(v), Compiler.ArglistsKeyword);
                object sigTag = SigTag(_args.count(), v);
                _tag = sigTag ?? varFexpr.Tag;
            }
            else
                _tag = null;
        }

        #endregion

        #region Type mangling

        public bool HasClrType
        {
            get { return _tag != null; }
        }

        public Type ClrType
        {
            get
            {
                if (_cachedType == null)
                    _cachedType = HostExpr.TagToType(_tag);
                return _cachedType;
            }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ParserContext pcon, ISeq form)
        {
            bool tailPosition = Compiler.InTailCall(pcon.Rhc);
            pcon = pcon.EvalOrExpr();

            Expr fexpr = Compiler.Analyze(pcon, form.first());
            VarExpr varFexpr = fexpr as VarExpr;

            if (varFexpr != null && varFexpr.Var.Equals(Compiler.InstanceVar) && RT.count(form) == 3)
            {
                Expr sexpr = Compiler.Analyze(pcon.SetRhc(RHC.Expression), RT.second(form));
                if (sexpr is ConstantExpr csexpr)
                {
                    Type tval = csexpr.Val as Type;
                    if (tval != null)
                        return new InstanceOfExpr((string)Compiler.SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), tval, Compiler.Analyze(pcon, RT.third(form)));
                }
            }

            if (RT.booleanCast(Compiler.GetCompilerOption(Compiler.DirectLinkingKeyword))
                && varFexpr != null
                && pcon.Rhc != RHC.Eval)
            {
                Var v = varFexpr.Var;
                if (!v.isDynamic() && !RT.booleanCast(RT.get(v.meta(), Compiler.RedefKeyword, false)) && !RT.booleanCast(RT.get(v.meta(), RT.DeclaredKey, false)))
                {
                    Symbol formTag = Compiler.TagOf(form);
                    //object arglists = RT.get(RT.meta(v), Compiler.ArglistsKeyword);
                    int arity = RT.count(form.next());
                    object sigtag = SigTag(arity, v);
                    object vtag = RT.get(RT.meta(v), RT.TagKey);
                    //if (StaticInvokeExpr.Parse(v, RT.next(form), formTag ?? sigtag ?? vtag) is StaticInvokeExpr ret && !((Compiler.IsCompiling || Compiler.IsCompilingDefType) && GenContext.IsInternalAssembly(ret.Method.DeclaringType.Assembly)))
                    //{
                    //    //Console.WriteLine("invoke direct: {0}", v);
                    //    return ret;
                    //}
                    ////Console.WriteLine("NOT direct: {0}", v);
                    if (StaticInvokeExpr.Parse(v, RT.next(form), formTag ?? sigtag ?? vtag) is StaticInvokeExpr ret)
                    {
                        var isCompiling = Compiler.IsCompiling || Compiler.IsCompilingDefType;
                        var retAssembly = ret.Method.DeclaringType.Assembly;
                        var isInternal = GenContext.IsInternalAssembly(retAssembly);
                        if (!(isCompiling && isInternal))
                            return ret;
                    }
                }
            }
            
            if (varFexpr != null && pcon.Rhc != RHC.Eval)
            {
                Var v = varFexpr.Var;
                object arglists = RT.get(RT.meta(v), Compiler.ArglistsKeyword);
                int arity = RT.count(form.next());
                for (ISeq s = RT.seq(arglists); s != null; s = s.next())
                {
                    IPersistentVector sargs = (IPersistentVector)s.first();
                    if (sargs.count() == arity)
                    {
                        string primc = FnMethod.PrimInterface(sargs);
                        if (primc != null)
                            return Compiler.Analyze(pcon,
                                ((IObj)RT.listStar(Symbol.intern(".invokePrim"),
                                            ((Symbol)form.first()).withMeta(RT.map(RT.TagKey, Symbol.intern(primc))),
                                            form.next())).withMeta((IPersistentMap)RT.conj(RT.meta(v), RT.meta(form))));
                        break;
                    }
                }
            }

            if (fexpr is KeywordExpr kwFexpr && RT.count(form) == 2 && Compiler.KeywordCallsitesVar.isBound)
            {
                Expr target = Compiler.Analyze(pcon, RT.second(form));
                return new KeywordInvokeExpr((string)Compiler.SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), Compiler.TagOf(form), kwFexpr, target);
            }

            // Preserving the existing static field bug that replaces a reference in parens with
            // the field itself rather than trying to invoke the value in the field. This is
            // an exception to the uniform Class/member qualification per CLJ-2806 ticket.

            if (fexpr is StaticFieldExpr || fexpr is StaticPropertyExpr)
                return fexpr;


            if (fexpr is QualifiedMethodExpr qmfexpr)
                return ToHostExpr(pcon,
                    qmfexpr,
                    Compiler.TagOf(form),
                    tailPosition,
                    form.next());


            IPersistentVector args = PersistentVector.EMPTY;
            for (ISeq s = RT.seq(form.next()); s != null; s = s.next())
                args = args.cons(Compiler.Analyze(pcon, s.first()));

            //if (args.count() > Compiler.MAX_POSITIONAL_ARITY)
            //    throw new ArgumentException(String.Format("No more than {0} args supported", Compiler.MAX_POSITIONAL_ARITY));

            return new InvokeExpr((string)Compiler.SourceVar.deref(),
                (IPersistentMap)Compiler.SourceSpanVar.deref(), //Compiler.GetSourceSpanMap(form),
                Compiler.TagOf(form),
                fexpr,
                args,
                tailPosition);
        }
        
        static Expr ToHostExpr(ParserContext pcon, QualifiedMethodExpr qmfexpr, Symbol tag, bool tailPosition, ISeq args)
        {
            var source = (string)Compiler.SourceVar.deref();
            var spanMap = (IPersistentMap)Compiler.SourceSpanVar.deref();


            // we have the form (qmfexpr ...args...)
            // We need to decide what the pieces are in ...args...

            Expr instance = null;
            if (qmfexpr.Kind == QualifiedMethodExpr.EMethodKind.INSTANCE)
            {
                instance = Compiler.Analyze(pcon.EvalOrExpr(), RT.first(args));
                args = RT.next(args);
            }

            // We handle zero-arity calls separately, similarly to how HostExpr handles them.
            // Constructors not included here.
            // We are trying to discriminate field access, property access, and method calls on zero arguments.
            // 
            // One special case here:  Suppose we have a zero-arity _generic_method call, with type-args provided.
            // THis will look like:   (Type/StaticMethod (type-args type1 ..))  or (Type/InstanceMEthod instance-expression (type-args type1 ..))
            // We check for the arg count before removing the type-args, so these will be handled by the non-zero-arity code.
            // That is okay -- because this is generic, it can't be a field or property access, so we can treat it as a method call.


            object firstArg = RT.first(args);
            GenericTypeArgList genericTypeArgs;

            if (firstArg is ISeq && RT.first(firstArg) is Symbol symbol && symbol.Equals(HostExpr.TypeArgsSym))
            {
                // We have a type args supplied for a generic method call
                // (. thing methodname (type-args type1 ... ) args ...)
                genericTypeArgs = GenericTypeArgList.Create(RT.next(firstArg));
                args = RT.next(args);
            }
            else
            {
                genericTypeArgs = GenericTypeArgList.Empty;
            }

            // Now we have a potential conflict.  What if we have a hinted signature on the QME?
            // Who wins the type-arg battle?

            // If the QME has a nonempty generic type args list, we us it in preference.

            if ( qmfexpr.HintedSig != null && !qmfexpr.HintedSig.GenericTypeArgs.IsEmpty)
                genericTypeArgs = qmfexpr.HintedSig.GenericTypeArgs;

            bool hasGenericTypeArgs = !genericTypeArgs.IsEmpty;

            bool isZeroArity = RT.count(args) == 0 && qmfexpr.Kind != QualifiedMethodExpr.EMethodKind.CTOR;

            if (isZeroArity)
            {
                PropertyInfo pinfo;
                FieldInfo finfo;

                switch (qmfexpr.Kind)
                {
                    case QualifiedMethodExpr.EMethodKind.INSTANCE:
                        if (!hasGenericTypeArgs && (finfo = Reflector.GetField(qmfexpr.MethodType, qmfexpr.MethodName,false)) != null)
                            return new InstanceFieldExpr(source, spanMap, tag, instance, qmfexpr.MethodName, finfo, true);
                        if (!hasGenericTypeArgs && (pinfo = Reflector.GetProperty(qmfexpr.MethodType, qmfexpr.MethodName, false)) != null)
                            return new InstancePropertyExpr(source, spanMap, tag, instance, qmfexpr.MethodName, pinfo, true);
                        if (Reflector.GetArityZeroMethod(qmfexpr.MethodType, qmfexpr.MethodName, genericTypeArgs, false) != null)
                            return new InstanceMethodExpr(source, spanMap, tag, instance, qmfexpr.MethodType, qmfexpr.MethodName, genericTypeArgs, new List<HostArg>(), tailPosition);

                        string typeArgsStr = hasGenericTypeArgs ? $" and generic type args {genericTypeArgs.GenerateGenericTypeArgsString()}" : "";
                        throw new MissingMemberException($"No instance field, property, or method taking 0 args{typeArgsStr} named {qmfexpr.MethodName} found for {qmfexpr.MethodType.Name}");

                    case QualifiedMethodExpr.EMethodKind.STATIC:
                        if ((finfo = Reflector.GetField(qmfexpr.MethodType, qmfexpr.MethodName, true)) != null)
                            return new StaticFieldExpr(source, spanMap, tag, qmfexpr.MethodType, qmfexpr.MethodName, finfo);
                        if ((pinfo = Reflector.GetProperty(qmfexpr.MethodType, qmfexpr.MethodName, true)) != null)
                            return new StaticPropertyExpr(source, spanMap, tag, qmfexpr.MethodType, qmfexpr.MethodName, pinfo);
                        if (Reflector.GetArityZeroMethod(qmfexpr.MethodType, qmfexpr.MethodName, genericTypeArgs, true) != null)
                            return new StaticMethodExpr(source, spanMap, tag, qmfexpr.MethodType, qmfexpr.MethodName, genericTypeArgs, new List<HostArg>(), tailPosition);

                        typeArgsStr = hasGenericTypeArgs ? $" and generic type args {genericTypeArgs.GenerateGenericTypeArgsString()}" : "";
                        throw new MissingMemberException($"No static field, property, or method taking 0 args{typeArgsStr} named {qmfexpr.MethodName} found for {qmfexpr.MethodType.Name}");

                    default:
                        // Constructor -- this won't happen, we fall through to the code below.
                        break;
                }
            }

            if (qmfexpr.HintedSig != null )
            {
                //  What if there is a hinted signature AND the arguments have a type-args list?
                //  In the same way that inferred and tagged types on the arguments are overridden by the hinted signature, we do the same with type-args -- ignore it.

                MethodBase method = QualifiedMethodExpr.ResolveHintedMethod(qmfexpr.MethodType, qmfexpr.MethodName, qmfexpr.Kind, qmfexpr.HintedSig);
                switch (qmfexpr.Kind)
                {
                    case QualifiedMethodExpr.EMethodKind.CTOR:
                        return new NewExpr(
                            qmfexpr.MethodType, 
                            (ConstructorInfo)method, 
                            HostExpr.ParseArgs(pcon, args), 
                            spanMap);

                    case QualifiedMethodExpr.EMethodKind.INSTANCE:
                        return new InstanceMethodExpr(
                            source, 
                            spanMap,
                            tag,
                            instance,
                            qmfexpr.MethodType,
                            Compiler.munge(qmfexpr.MethodName), 
                            (MethodInfo)method,
                            genericTypeArgs,                 
                            HostExpr.ParseArgs(pcon, args),
                            tailPosition);

                    default:
                        return new StaticMethodExpr(
                            source,
                            spanMap,
                            tag, 
                             qmfexpr.MethodType,
                             Compiler.munge(qmfexpr.MethodName), 
                             (MethodInfo)method,
                             genericTypeArgs,
                             HostExpr.ParseArgs(pcon,args),
                             tailPosition);
                }
            }
            else
            {
                switch (qmfexpr.Kind)
                {
                    case QualifiedMethodExpr.EMethodKind.CTOR:
                        return new NewExpr(
                            qmfexpr.MethodType, 
                            HostExpr.ParseArgs(pcon, args), 
                            (IPersistentMap)Compiler.SourceSpanVar.deref());

                    case QualifiedMethodExpr.EMethodKind.INSTANCE:
                        return new InstanceMethodExpr(
                            (string)Compiler.SourceVar.deref(),
                            (IPersistentMap)Compiler.SourceSpanVar.deref(),
                            tag,
                            instance,
                            qmfexpr.MethodType,
                            Compiler.munge(qmfexpr.MethodName),
                            genericTypeArgs,
                            HostExpr.ParseArgs(pcon, args),
                            tailPosition);

                    default:
                        return new StaticMethodExpr(
                            (string)Compiler.SourceVar.deref(),
                            (IPersistentMap)Compiler.SourceSpanVar.deref(),
                            tag, 
                            qmfexpr.MethodType,
                            Compiler.munge(qmfexpr.MethodName),
                            genericTypeArgs,
                            HostExpr.ParseArgs(pcon, args),
                            tailPosition);
                }
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            try
            {
                IFn fn = (IFn)_fexpr.Eval();
                IPersistentVector argvs = PersistentVector.EMPTY;
                for (int i = 0; i < _args.count(); i++)
                    argvs = argvs.cons(((Expr)_args.nth(i)).Eval());
                return fn.applyTo(RT.seq(Util.Ret1(argvs, argvs = null)));
            }
            catch (Compiler.CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Compiler.CompilerException(_source, Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), null, Compiler.CompilerException.PhaseExecutionKeyword, e);
            }
        }

        #endregion

        #region Code generation

        static Object SigTag(int argcount, Var v)
        {
            Object arglists = RT.get(RT.meta(v), Compiler.ArglistsKeyword);
            for (ISeq s = RT.seq(arglists); s != null; s = s.next())
            {
                APersistentVector sig = (APersistentVector)s.first();
                int restOffset = sig.IndexOf(Compiler.AmpersandSym);
                if (argcount == sig.count() || (restOffset > -1 && argcount >= restOffset))
                    return Compiler.TagOf(sig);
            }
            return null;
        }

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {


            if (_isProtocol)
            {
                GenContext.EmitDebugInfo(ilg, _spanMap);
                EmitProto(rhc, objx, ilg);
            }
            else
            {
                _fexpr.Emit(RHC.Expression, objx, ilg);
                GenContext.EmitDebugInfo(ilg, _spanMap);
                ilg.Emit(OpCodes.Castclass, typeof(IFn));
                EmitArgsAndCall(0, rhc, objx, ilg);
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        void EmitProto(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Label onLabel = ilg.DefineLabel();
            Label callLabel = ilg.DefineLabel();
            Label endLabel = ilg.DefineLabel();

            Var v = ((VarExpr)_fexpr).Var;

            Expr e = (Expr)_args.nth(0);
            e.Emit(RHC.Expression, objx, ilg);               // target
            ilg.Emit(OpCodes.Dup);                               // target, target

            LocalBuilder targetTemp = ilg.DeclareLocal(typeof(Object));
            GenContext.SetLocalName(targetTemp, "target");
            ilg.Emit(OpCodes.Stloc,targetTemp);                  // target

            ilg.Emit(OpCodes.Call,Compiler.Method_Util_classOf);          // class
            ilg.EmitFieldGet(objx.CachedTypeField(_siteIndex));  // class, cached-class
            ilg.Emit(OpCodes.Beq, callLabel);                    // 
            if (_protocolOn != null)
            {
                ilg.Emit(OpCodes.Ldloc,targetTemp);              // target
                ilg.Emit(OpCodes.Isinst, _protocolOn);           // null or target
                ilg.Emit(OpCodes.Ldnull);                        // (null or target), null
                ilg.Emit(OpCodes.Cgt_Un);                        // (0 or 1)
                ilg.Emit(OpCodes.Brtrue, onLabel);
            }
            ilg.Emit(OpCodes.Ldloc,targetTemp);                  // target
            ilg.Emit(OpCodes.Call,Compiler.Method_Util_classOf);          // class
            
            LocalBuilder typeTemp = ilg.DeclareLocal(typeof(Type));
            GenContext.SetLocalName(typeTemp, "type");
            ilg.Emit(OpCodes.Stloc,typeTemp);                    //    (typeType <= class)
            
            
            ilg.Emit(OpCodes.Ldloc,typeTemp);                    // this, class
            ilg.EmitFieldSet(objx.CachedTypeField(_siteIndex));  // 

            ilg.MarkLabel(callLabel);                       
    
            objx.EmitVar(ilg,v);                              // var
            ilg.Emit(OpCodes.Call,Compiler.Method_Var_getRawRoot);         // proto-fn
            ilg.Emit(OpCodes.Castclass, typeof(AFunction));
                       
            ilg.Emit(OpCodes.Ldloc,targetTemp);                  // proto-fn, target

            EmitArgsAndCall(1,rhc,objx,ilg);
            ilg.Emit(OpCodes.Br,endLabel);

            ilg.MarkLabel(onLabel);
            ilg.Emit(OpCodes.Ldloc,targetTemp);                  // target
            if ( _protocolOn != null )
            {
                ilg.Emit(OpCodes.Castclass, _protocolOn);
                MethodExpr.EmitTypedArgs(objx, ilg, _onMethod.GetParameters(), RT.subvec(_args, 1, _args.count()));
                // In JVM.  No necessary here.
                //if (_tailPosition)
                //{
                //    ObjMethod method = (ObjMethod)Compiler.MethodVar.deref();
                //    method.EmitClearThis(ilg);
                //}
                ilg.Emit(OpCodes.Callvirt, _onMethod);
                HostExpr.EmitBoxReturn(objx, ilg, _onMethod.ReturnType);                
            }
            ilg.MarkLabel(endLabel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Standard API")]
        void EmitArgsAndCall(int firstArgToEmit, RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            for ( int i=firstArgToEmit; i< Math.Min(Compiler.MaxPositionalArity,_args.count()); i++ )
            {
                Expr e = (Expr) _args.nth(i);
                e.Emit(RHC.Expression,objx,ilg);
            }
            if ( _args.count() > Compiler.MaxPositionalArity )
            {
                IPersistentVector restArgs = PersistentVector.EMPTY;
                for (int i=Compiler.MaxPositionalArity; i<_args.count(); i++ )
                    restArgs = restArgs.cons(_args.nth(i));
                MethodExpr.EmitArgsAsArray(restArgs,objx,ilg);
            }

            // In JVM.  No necessary here.
            //if (_tailPosition)
            //{
            //    ObjMethod method = (ObjMethod)Compiler.MethodVar.deref();
            //    method.EmitClearThis(ilg);
            //}

            MethodInfo mi = Compiler.Methods_IFn_invoke[Math.Min(Compiler.MaxPositionalArity+1,_args.count())];

           ilg.Emit(OpCodes.Callvirt,mi);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
