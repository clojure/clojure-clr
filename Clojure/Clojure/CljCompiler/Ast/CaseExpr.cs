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
using System.Reflection;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace clojure.lang.CljCompiler.Ast
{
    class CaseExpr : UntypedExpr
    {
        #region Data

        readonly LocalBindingExpr _expr;
        readonly int _shift, _mask, _low, _high;
        readonly Expr _defaultExpr;
        readonly Dictionary<int, Expr> _tests;
        readonly Dictionary<int, Expr> _thens;
        readonly bool _allKeywords;

        readonly IPersistentMap _sourceSpan;

        #endregion

        #region C-tors

        public CaseExpr( IPersistentMap sourceSpan, LocalBindingExpr expr, int shift, int mask, int low, int high, Expr defaultExpr,
                        Dictionary<int, Expr> tests, Dictionary<int, Expr> thens, bool allKeywords)
        {
            _sourceSpan = sourceSpan;
            _expr = expr;
            _shift = shift;
            _mask = mask;
            _low = low;
            _high = high;
            _defaultExpr = defaultExpr;
            _tests = tests;
            _thens = thens;
            _allKeywords = allKeywords;
        }

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            //(case* expr shift mask low high default map<minhash, [test then]> identity?)
            //prepared by case macro and presumed correct
            //case macro binds actual expr in let so expr is always a local,
            //no need to worry about multiple evaluation
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq) frm;

                if (pcon.Rhc == RHC.Eval)
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FN, PersistentVector.EMPTY, form)),"case__"+RT.nextID());

                PersistentVector args = PersistentVector.create(form.next());
                Dictionary<int,Expr> tests = new Dictionary<int,Expr>();
                Dictionary<int,Expr> thens = new Dictionary<int,Expr>();

                LocalBindingExpr testexpr = (LocalBindingExpr)Compiler.Analyze(pcon.SetRhc(RHC.Expression),args.nth(0));
                //testexpr.shouldClear = false;
            
                //PathNode branch = new PathNode(PATHTYPE.BRANCH, (PathNode) CLEAR_PATH.get());
            
                foreach ( IMapEntry e in ((IPersistentMap)args.nth(6)) )
                {
                    int minhash = (int)e.key();
                    IMapEntry me = (IMapEntry)e.val();
                    Expr testExpr = new ConstantExpr(me.key());
                    tests[minhash] = testExpr;
                    Expr thenExpr;
                    //try 
                    //{
                    //    Var.pushThreadBindings(
                    //        RT.map(CLEAR_PATH, new PathNode(PATHTYPE.PATH,branch)));
                    thenExpr = Compiler.Analyze(pcon,me.val());
                    //}
                    //finally
                    //{
                    //    Var.popThreadBindings();
                    //}
                    thens[minhash] = thenExpr;
                }
            
                Expr defaultExpr;
                //try 
                //{
                //    Var.pushThreadBindings(
                //        RT.map(CLEAR_PATH, new PathNode(PATHTYPE.PATH,branch)));
                defaultExpr = Compiler.Analyze(pcon,args.nth(5));
                //}
                //finally
                //{
                //    Var.popThreadBindings();
                //}

            return new CaseExpr(
                (IPersistentMap) Compiler.SOURCE_SPAN.deref(),
                testexpr,
                (int)args.nth(1),
                (int)args.nth(2),
                (int)args.nth(3),
                (int)args.nth(4),
                defaultExpr,
                tests,
                thens,
                RT.booleanCast(args.nth(7)));
            }
        }
        
        #endregion

        #region eval

        public override object Eval()
        {
            throw new InvalidOperationException("Can't eval case");
        }

        #endregion

        #region Code generation

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>
        /// Equivalent to :
        ///    switch (hashed _expr)
        ///    
        ///      case i:  if _expr == _test_i
        ///                 goto end with _then_i
        ///               else goto default
        ///               
        ///      ...
        ///      default:
        ///            (default_label)
        ///             goto end with _default
        ///      end
        ///    end_label:
        ///      
        /// </remarks>
        public override Expression GenCode(RHC rhc, ObjExpr objx, GenContext context)
        {
            LabelTarget defaultLabel = Expression.Label("default");
            LabelTarget endLabel = Expression.Label(typeof(Object),"end");

            MethodInfo cmp = _allKeywords ? Compiler.Method_Object_ReferenceEquals : Compiler.Method_Util_equals;

            List<SwitchCase> cases = new List<SwitchCase>();

            foreach (KeyValuePair<int, Expr> pair in _tests)
            {
                int i = pair.Key;
                Expr test = pair.Value;

                Expression body = 
                    Expression.Condition(
                        Expression.Call(null,cmp,Compiler.MaybeBox(_expr.GenCode(RHC.Expression, objx, context)),Compiler.MaybeBox(test.GenCode(RHC.Expression,objx,context))),
                        Expression.Return(endLabel,Compiler.MaybeBox(_thens[i].GenCode(RHC.Expression,objx,context))),
                        Expression.Goto(defaultLabel));

                cases.Add(Expression.SwitchCase(body, Expression.Constant(i)));
            }



            Expression testExpr = 
                Expression.And(
                    Expression.RightShift(
                        Expression.Call(null,Compiler.Method_Util_Hash,Expression.Convert(_expr.GenCode(RHC.Expression, objx, context),typeof(Object))),
                        Expression.Constant(_shift)),
                    Expression.Constant(_mask));


            Expression defaultExpr =
                Expression.Block(
                    Expression.Label(defaultLabel),
                    Expression.Return(endLabel,Compiler.MaybeBox(_defaultExpr.GenCode(RHC.Expression,objx,context))));

            Expression switchExpr = Expression.Switch(testExpr, defaultExpr, cases.ToArray<SwitchCase>());

            Expression block =
                Expression.Block(typeof(object),
                    switchExpr,
                    Expression.Label(endLabel, Expression.Default(typeof(Object))));

            block = Compiler.MaybeAddDebugInfo(block, _sourceSpan, context.IsDebuggable);
            return block;
        }

        #endregion
    }
}
