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
using System.Reflection.Emit;

namespace clojure.lang.CljCompiler.Ast
{
    public sealed class KeywordInvokeExpr : Expr
    {
        #region Data

        readonly KeywordExpr _kw;
        public KeywordExpr KWExpr { get { return _kw; } }

        readonly Object _tag;
        public object Tag { get { return _tag; } }
        
        readonly Expr _target;
        public Expr Target { get { return _target; } }
        
        readonly string _source;
        public string Source { get { return _source; } }
        
        readonly IPersistentMap _spanMap;
        public IPersistentMap SpanMap { get { return _spanMap; } }
        
        readonly int _siteIndex;
        public int SiteIndex { get { return _siteIndex; } }

        Type _cachedType;
        
        #endregion

        #region C-tors

        public KeywordInvokeExpr(string source, IPersistentMap spanMap, Symbol tag, KeywordExpr kw, Expr target)
        {
            _source = source;
            _spanMap = spanMap;
            _kw = kw;
            _target = target;
            _tag = tag;
            _siteIndex = Compiler.RegisterKeywordCallsite(kw.Kw);
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

        #region eval

        public object Eval()
        {
            try
            {
                return _kw.Kw.invoke(_target.Eval());
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

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {

            Label endLabel = ilg.DefineLabel();
            Label faultLabel = ilg.DefineLabel();

            GenContext.EmitDebugInfo(ilg, _spanMap);

            LocalBuilder thunkLoc = ilg.DeclareLocal(typeof(ILookupThunk));
            LocalBuilder targetLoc = ilg.DeclareLocal(typeof(Object));
            LocalBuilder resultLoc = ilg.DeclareLocal(typeof(Object));
            GenContext.SetLocalName(thunkLoc, "thunk");
            GenContext.SetLocalName(targetLoc, "target");
            GenContext.SetLocalName(resultLoc, "result");

            // TODO: Debug info

            // pseudo-code:
            //  ILookupThunk thunk = objclass.ThunkField(i)
            //  object target = ...code...
            //  object val = thunk.get(target)
            //  if ( val != thunk )
            //     return val
            //  else
            //     KeywordLookupSite site = objclass.SiteField(i)
            //     thunk = site.fault(target)
            //     objclass.ThunkField(i) = thunk
            //     val = thunk.get(target)
            //     return val

            ilg.EmitFieldGet(objx.ThunkField(_siteIndex));                     // thunk
            ilg.Emit(OpCodes.Stloc, thunkLoc);                                  //  (thunkLoc <= thunk)

            _target.Emit(RHC.Expression, objx, ilg);                         // target
            ilg.Emit(OpCodes.Stloc, targetLoc);                                  //   (targetLoc <= target)

            ilg.Emit(OpCodes.Ldloc, thunkLoc);
            ilg.Emit(OpCodes.Ldloc, targetLoc);
            ilg.EmitCall(Compiler.Method_ILookupThunk_get);                    // result
            ilg.Emit(OpCodes.Stloc, resultLoc);                                 //    (resultLoc <= result)

            ilg.Emit(OpCodes.Ldloc, thunkLoc);
            ilg.Emit(OpCodes.Ldloc, resultLoc);
            ilg.Emit(OpCodes.Beq, faultLabel);

            ilg.Emit(OpCodes.Ldloc, resultLoc);                                  // result
            ilg.Emit(OpCodes.Br, endLabel);

            ilg.MarkLabel(faultLabel);
            ilg.EmitFieldGet(objx.KeywordLookupSiteField(_siteIndex));           // site
            ilg.Emit(OpCodes.Ldloc, targetLoc);                                  // site, target
            ilg.EmitCall(Compiler.Method_ILookupSite_fault);                    // new-thunk
            ilg.Emit(OpCodes.Dup);                                              // new-thunk, new-thunk
            ilg.EmitFieldSet(objx.ThunkField(_siteIndex));                      // new-thunk

            ilg.Emit(OpCodes.Ldloc, targetLoc);                                 // new-thunk, target
            ilg.EmitCall(Compiler.Method_ILookupThunk_get);                    // result

            ilg.MarkLabel(endLabel);                                           // result
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }

        public bool HasNormalExit() { return true; }

        #endregion
    }
}
