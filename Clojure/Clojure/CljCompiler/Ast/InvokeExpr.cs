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

namespace clojure.lang.CljCompiler.Ast
{
    class InvokeExpr : Expr
    {
        #region Data

        readonly Expr _fexpr;
        readonly Object _tag;
        readonly IPersistentVector _args;
        readonly string _source;
        readonly IPersistentMap _spanMap;
        bool _isProtocol = false;
        int _siteIndex = -1;
        Type _protocolOn;
        MethodInfo _onMethod;

        static readonly Keyword _onKey = Keyword.intern("on");
        static readonly Keyword _methodMapKey = Keyword.intern("method-map");

        #endregion

        #region Ctors

        public InvokeExpr(string source, IPersistentMap spanMap, Symbol tag, Expr fexpr, IPersistentVector args)
        {
            _source = source;
            _spanMap = spanMap;
            _fexpr = fexpr;
            _args = args;

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
                       
                        IList<MethodBase> methods = Reflector.GetMethods(_protocolOn, mname, null, args.count() - 1,  false);
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
                object arglists = RT.get(RT.meta(varFexpr.Var), Compiler.ArglistsKeyword);
                object sigTag = null;
                for (ISeq s = RT.seq(arglists); s != null; s = s.next())
                {
                    APersistentVector sig = (APersistentVector)s.first();
                    int restOffset = sig.IndexOf(Compiler.AmpersandSym);
                    if (args.count() == sig.count() || (restOffset > -1 && args.count() >= restOffset))
                    {
                        sigTag = Compiler.TagOf(sig);
                        break;
                    }
                }
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
            get { return HostExpr.TagToType(_tag); }
        }

        #endregion

        #region Parsing

        public static Expr Parse(ParserContext pcon, ISeq form)
        {
            pcon = pcon.EvalOrExpr();

            Expr fexpr = Compiler.Analyze(pcon,form.first());
            VarExpr varFexpr = fexpr as VarExpr;

            if (varFexpr != null && varFexpr.Var.Equals(Compiler.InstanceVar))
            {
                if ( RT.second(form) is Symbol )
                {
                    Type t = HostExpr.MaybeType(RT.second(form),false);
                    if ( t != null )
                        return new InstanceOfExpr((string)Compiler.SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), t, Compiler.Analyze(pcon,RT.third(form)));
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
                                RT.listStar(Symbol.intern(".invokePrim"),
                                            ((Symbol)form.first()).withMeta(RT.map(RT.TagKey, Symbol.intern(primc))),
                                            form.next()));
                        break;
                    }
                }
            }

            KeywordExpr kwFexpr = fexpr as KeywordExpr;

            if (kwFexpr != null && RT.count(form) == 2 && Compiler.KeywordCallsitesVar.isBound)
            {
                Expr target = Compiler.Analyze(pcon, RT.second(form));
                return new KeywordInvokeExpr((string)Compiler.SourceVar.deref(), (IPersistentMap)Compiler.SourceSpanVar.deref(), Compiler.TagOf(form), kwFexpr, target);
            }

            IPersistentVector args = PersistentVector.EMPTY;
            for ( ISeq s = RT.seq(form.next()); s != null; s = s.next())
                args = args.cons(Compiler.Analyze(pcon,s.first()));

            //if (args.count() > Compiler.MAX_POSITIONAL_ARITY)
            //    throw new ArgumentException(String.Format("No more than {0} args supported", Compiler.MAX_POSITIONAL_ARITY));

            return new InvokeExpr((string)Compiler.SourceVar.deref(),
                (IPersistentMap)Compiler.SourceSpanVar.deref(), //Compiler.GetSourceSpanMap(form),
                Compiler.TagOf(form),
                fexpr,
                args);
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
                    argvs = (IPersistentVector)argvs.cons(((Expr)_args.nth(i)).Eval());
                return fn.applyTo(RT.seq(Util.Ret1(argvs, argvs = null)));
            }
            catch (Compiler.CompilerException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Compiler.CompilerException(_source, Compiler.GetLineFromSpanMap(_spanMap), Compiler.GetColumnFromSpanMap(_spanMap), e);
            }
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            GenContext.EmitDebugInfo(ilg, _spanMap);

            if (_isProtocol)
                EmitProto(rhc, objx, ilg);
            else
            {
                _fexpr.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Castclass, typeof(IFn));
                EmitArgsAndCall(0, rhc, objx, ilg);
            }
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        void EmitProto(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            switch (objx.FnMode)
            {
                case FnMode.Light:
                    EmitProtoLight(rhc, objx, ilg);
                    break;
                case FnMode.Full:
                    EmitProtoFull(rhc, objx, ilg);
                    break;
                default:
                    throw Util.UnreachableCode();
            }
        }

        // TODO: Eliminate common code between EmitProtoLight and EmitProtoFull

        void EmitProtoLight(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Label endLabel = ilg.DefineLabel();

            Var v = ((VarExpr)_fexpr).Var;

            Expr e = (Expr)_args.nth(0);
            e.Emit(RHC.Expression, objx, ilg);               // target

            LocalBuilder targetTemp = ilg.DeclareLocal(typeof(Object));
            GenContext.SetLocalName(targetTemp, "target");
            ilg.Emit(OpCodes.Stloc, targetTemp);             //   (targetTemp <= target)

            ilg.EmitString(String.Format("In Light Proto for {0}",v.Symbol.ToString()));
            ilg.Emit(OpCodes.Call,typeof(Console).GetMethod("WriteLine",new Type[] { typeof(string) }));

            //if (_protocolOn != null)
            //{
            //    ilg.Emit(OpCodes.Ldloc, targetTemp);              // target
            //    ilg.Emit(OpCodes.Isinst, _protocolOn);            // (target or null)
            //    ilg.Emit(OpCodes.Ldnull);                         // (target or null), null
            //    ilg.Emit(OpCodes.Cgt_Un);                         // (0 or 1)
            //    ilg.Emit(OpCodes.Brtrue, onLabel);
            //}

            objx.EmitVar(ilg, v);                                 // var
            ilg.Emit(OpCodes.Call, Compiler.Method_Var_getRawRoot);         // proto-fn


                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Call, typeof(Object).GetMethod("GetType"));
                ilg.Emit(OpCodes.Callvirt, typeof(Object).GetMethod("ToString"));
                ilg.EmitString("Expected AFunction, got ");
                ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new Type[] { typeof(String) }));
                ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(String) }));
            

            ilg.Emit(OpCodes.Castclass, typeof(AFunction));

            ilg.EmitString("Castclass worked ");
            ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(String) }));


            ilg.Emit(OpCodes.Ldloc, targetTemp);                  // proto-fn, target

            EmitArgsAndCall(1, rhc, objx, ilg);

            ilg.EmitString("gen'd args and called");
            ilg.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(String) }));

            ilg.Emit(OpCodes.Br, endLabel);

            //ilg.MarkLabel(onLabel);
            //ilg.Emit(OpCodes.Ldloc, targetTemp);                  // target
            //if (_protocolOn != null)
            //{
            //    ilg.Emit(OpCodes.Castclass, _protocolOn);
            //    MethodExpr.EmitTypedArgs(objx, ilg, _onMethod.GetParameters(), RT.subvec(_args, 1, _args.count()));
            //    //if (rhc == RHC.Return)
            //    //{
            //    //    ObjMethod2 method = (ObjMethod)Compiler.MethodVar.deref();
            //    //    method.EmitClearLocals(context);
            //    //}
            //    ilg.Emit(OpCodes.Callvirt,_onMethod);
            //    HostExpr.EmitBoxReturn(objx, ilg, _onMethod.ReturnType);
            //}
            ilg.MarkLabel(endLabel);
        }

        void EmitProtoFull(RHC rhc, ObjExpr objx, CljILGen ilg)
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
            ilg.EmitLoadArg(0);                                  // class, this
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
            
            ilg.EmitLoadArg(0);                                  // this
            
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
                //if (rhc == RHC.Return)
                //{
                //    ObjMethod2 method = (ObjMethod)Compiler.MethodVar.deref();
                //    method.EmitClearLocals(context);
                //}
                ilg.Emit(OpCodes.Callvirt, _onMethod);
                HostExpr.EmitBoxReturn(objx, ilg, _onMethod.ReturnType);                
            }
            ilg.MarkLabel(endLabel);
        }

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

            //if ( rhc == RHC.Return )
            //{
            //    ObjMethod2 method = (ObjMethod2)Compiler.MethodVar.deref();
            //    method.EmitClearLocals(context);
            //}

            MethodInfo mi = Compiler.Methods_IFn_invoke[Math.Min(Compiler.MaxPositionalArity+1,_args.count())];

           ilg.Emit(OpCodes.Callvirt,mi);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
