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
using System.Text;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace clojure.lang.CljCompiler.Ast
{
    class CaseExpr : UntypedExpr
    {

    //public final LocalBindingExpr expr;
    //public final int shift, mask, low, high;
    //public final Expr defaultExpr;
    //public final HashMap<Integer,Expr> tests;
    //public final HashMap<Integer,Expr> thens;
    //public final boolean allKeywords;

    //public final int line;

    //final static Method hashMethod = Method.getMethod("int hash(Object)");
    //final static Method hashCodeMethod = Method.getMethod("int hashCode()");
    //final static Method equalsMethod = Method.getMethod("boolean equals(Object, Object)");


    //public CaseExpr(int line, LocalBindingExpr expr, int shift, int mask, int low, int high, Expr defaultExpr,
    //                HashMap<Integer,Expr> tests,HashMap<Integer,Expr> thens, boolean allKeywords){
    //    this.expr = expr;
    //    this.shift = shift;
    //    this.mask = mask;
    //    this.low = low;
    //    this.high = high;
    //    this.defaultExpr = defaultExpr;
    //    this.tests = tests;
    //    this.thens = thens;
    //    this.line = line;
    //    this.allKeywords = allKeywords;
    //}

    //public Object eval() throws Exception{
    //    throw new UnsupportedOperationException("Can't eval case");
    //}

    //public void emit(C context, ObjExpr objx, GeneratorAdapter gen){
    //    Label defaultLabel = gen.newLabel();
    //    Label endLabel = gen.newLabel();
    //    HashMap<Integer,Label> labels = new HashMap();

    //    for(Integer i : tests.keySet())
    //        {
    //        labels.put(i, gen.newLabel());
    //        }

    //    Label[] la = new Label[(high-low)+1];

    //    for(int i=low;i<=high;i++)
    //        {
    //        la[i-low] = labels.containsKey(i) ? labels.get(i) : defaultLabel;
    //        }

    //    gen.visitLineNumber(line, gen.mark());
    //    expr.emit(C.EXPRESSION, objx, gen);
    //        gen.invokeStatic(UTIL_TYPE,hashMethod);
    //    gen.push(shift);
    //    gen.visitInsn(ISHR);
    //    gen.push(mask);
    //    gen.visitInsn(IAND);
    //    gen.visitTableSwitchInsn(low, high, defaultLabel, la);

    //    for(Integer i : labels.keySet())
    //        {
    //        gen.mark(labels.get(i));
    //        expr.emit(C.EXPRESSION, objx, gen);
    //        tests.get(i).emit(C.EXPRESSION, objx, gen);
    //        if(allKeywords)
    //            {
    //            gen.visitJumpInsn(IF_ACMPNE, defaultLabel);
    //            }
    //        else
    //            {
    //            gen.invokeStatic(UTIL_TYPE, equalsMethod);
    //            gen.ifZCmp(GeneratorAdapter.EQ, defaultLabel);
    //            }
    //        thens.get(i).emit(C.EXPRESSION,objx,gen);
    //        gen.goTo(endLabel);
    //        }

    //    gen.mark(defaultLabel);
    //    defaultExpr.emit(C.EXPRESSION, objx, gen);
    //    gen.mark(endLabel);
    //    if(context == C.STATEMENT)
    //        gen.pop();
    //}

    //static class Parser implements IParser{
    //    //(case* expr shift mask low high default map<minhash, [test then]> identity?)
    //    //prepared by case macro and presumed correct
    //    //case macro binds actual expr in let so expr is always a local,
    //    //no need to worry about multiple evaluation
    //    public Expr parse(C context, Object frm) throws Exception{
    //        ISeq form = (ISeq) frm;
    //        if(context == C.EVAL)
    //            return analyze(context, RT.list(RT.list(FN, PersistentVector.EMPTY, form)));
    //        PersistentVector args = PersistentVector.create(form.next());
    //        HashMap<Integer,Expr> tests = new HashMap();
    //        HashMap<Integer,Expr> thens = new HashMap();

    //        LocalBindingExpr testexpr = (LocalBindingExpr) analyze(C.EXPRESSION, args.nth(0));
    //        testexpr.shouldClear = false;
            
    //        PathNode branch = new PathNode(PATHTYPE.BRANCH, (PathNode) CLEAR_PATH.get());
    //        for(Object o : ((Map)args.nth(6)).entrySet())
    //            {
    //            Map.Entry e = (Map.Entry) o;
    //            Integer minhash = (Integer) e.getKey();
    //            MapEntry me = (MapEntry) e.getValue();
    //            Expr testExpr = new ConstantExpr(me.getKey());
    //            tests.put(minhash, testExpr);
    //            Expr thenExpr;
    //            try {
    //                Var.pushThreadBindings(
    //                        RT.map(CLEAR_PATH, new PathNode(PATHTYPE.PATH,branch)));
    //                thenExpr = analyze(C.EXPRESSION, me.getValue());
    //                }
    //            finally{
    //                Var.popThreadBindings();
    //                }
    //            thens.put(minhash, thenExpr);
    //            }
            
    //        Expr defaultExpr;
    //        try {
    //            Var.pushThreadBindings(
    //                    RT.map(CLEAR_PATH, new PathNode(PATHTYPE.PATH,branch)));
    //            defaultExpr = analyze(C.EXPRESSION, args.nth(5));
    //            }
    //        finally{
    //            Var.popThreadBindings();
    //            }

    //        return new CaseExpr((Integer) LINE.deref(),
    //                          testexpr,
    //                          (Integer)args.nth(1),
    //                          (Integer)args.nth(2),
    //                          (Integer)args.nth(3),
    //                          (Integer)args.nth(4),
    //                          defaultExpr,
    //                          tests,thens,args.nth(7) != RT.F);

    //    }
        //}

        #region Data


        #endregion

        #region C-tors


        #endregion

        #region Type mangling


        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(object frm, bool isRecurContext)
            {
                return null;
            }
        }
        
        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
