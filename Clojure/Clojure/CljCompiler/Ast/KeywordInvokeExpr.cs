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

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using System.Reflection.Emit;
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    sealed class KeywordInvokeExpr : Expr
    {
        #region Data

        readonly KeywordExpr _kw;
        readonly Object _tag;
        readonly Expr _target;
        readonly string _source;
        readonly IPersistentMap _spanMap;
        readonly int _siteIndex;

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
            get { return HostExpr.TagToType(_tag); }
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
                throw new Compiler.CompilerException(_source, Compiler.GetLineFromSpanMap(_spanMap), e);
            }
        }

        #endregion

        #region Code generation

        public Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            //if (context.Mode == CompilerMode.Immediate)
            if (objx.FnMode == FnMode.Light )
            {
                // This will emit a plain Keyword reference, rather than a callsite.
                InvokeExpr ie = new InvokeExpr(_source, _spanMap, (Symbol)_tag, _kw, RT.vector(_target));
                return ie.GenCode(rhc, objx, context);
            }
            else
            {
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

                ParameterExpression thunkParam = Expression.Parameter(typeof(ILookupThunk), "thunk");
                ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
                ParameterExpression valParam = Expression.Parameter(typeof(Object), "val");
                ParameterExpression siteParam = Expression.Parameter(typeof(KeywordLookupSite), "site");

                
                Expression assignThunkFromField = Expression.Assign(thunkParam, Expression.Field(null, objx.ThunkField(_siteIndex)));
                Expression assignThunkFromSite = Expression.Assign(thunkParam, Expression.Call(siteParam, Compiler.Method_ILookupSite_fault, targetParam));
                Expression assignFieldFromThunk = Expression.Assign(Expression.Field(null, objx.ThunkField(_siteIndex)), thunkParam);
                Expression assignTarget = Expression.Assign(targetParam,_target.GenCode(RHC.Expression, objx, context));
                Expression assignVal = Expression.Assign(valParam, Expression.Call(thunkParam, Compiler.Method_ILookupThunk_get,targetParam));
                Expression assignSite = Expression.Assign(siteParam, Expression.Field(null, objx.KeywordLookupSiteField(_siteIndex)));


                Expression block =
                    Expression.Block(typeof(Object), new ParameterExpression[] { thunkParam, valParam, targetParam },
                        assignThunkFromField,
                        assignTarget,
                        assignVal,
                        Expression.IfThen(
                            Expression.Equal(valParam, thunkParam),
                            Expression.Block(typeof(Object), new ParameterExpression[] { siteParam },
                                assignSite,
                                assignThunkFromSite,
                                assignFieldFromThunk,
                                assignVal)),
                        valParam);

                block = Compiler.MaybeAddDebugInfo(block, _spanMap, context.IsDebuggable);
                return block;
            }
        }

        public void Emit(RHC rhc, ObjExpr objx, GenContext context)
        {
            ILGen ilg = context.GetILGen();
            Label endLabel = ilg.DefineLabel();
            Label faultLabel = ilg.DefineLabel();

            LocalBuilder thunkLoc = ilg.DeclareLocal(typeof(ILookupThunk));
            LocalBuilder targetLoc = ilg.DeclareLocal(typeof(Object));
            LocalBuilder resultLoc = ilg.DeclareLocal(typeof(Object));
            thunkLoc.SetLocalSymInfo("thunk");
            targetLoc.SetLocalSymInfo("target");
            resultLoc.SetLocalSymInfo("result");

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
            ilg.Emit(OpCodes.Stloc,thunkLoc);                                  //  (thunkLoc <= thunk)

            _target.Emit(RHC.Expression,objx,context);                         // target
            ilg.Emit(OpCodes.Stloc,targetLoc);                                  //   (targetLoc <= target)

            ilg.Emit(OpCodes.Ldloc,thunkLoc);
            ilg.Emit(OpCodes.Ldloc,targetLoc);
            ilg.EmitCall(Compiler.Method_ILookupThunk_get);                    // result
            ilg.Emit(OpCodes.Stloc,resultLoc);                                 //    (resultLoc <= result)

            ilg.Emit(OpCodes.Ldloc,thunkLoc);
            ilg.Emit(OpCodes.Ldloc,resultLoc);
            ilg.Emit(OpCodes.Beq,faultLabel);

            ilg.Emit(OpCodes.Ldloc,resultLoc);                                  // result
            ilg.Emit(OpCodes.Br,endLabel);

            ilg.MarkLabel(faultLabel);
            ilg.EmitFieldGet(objx.KeywordLookupSiteField(_siteIndex));           // site
            ilg.Emit(OpCodes.Ldloc,targetLoc);                                  // site, target
            ilg.EmitCall(Compiler.Method_ILookupSite_fault);                    // new-thunk
            ilg.Emit(OpCodes.Dup);                                              // new-thunk, new-thunk
            ilg.EmitFieldSet(objx.ThunkField(_siteIndex));                      // new-thunk

            ilg.Emit(OpCodes.Ldloc, targetLoc);                                 // new-thunk, target
            ilg.EmitCall(Compiler.Method_ILookupThunk_get);                    // result

            ilg.MarkLabel(endLabel);                                           // result
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }


        #endregion
    }
}
