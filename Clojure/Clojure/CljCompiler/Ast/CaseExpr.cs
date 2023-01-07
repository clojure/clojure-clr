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

namespace clojure.lang.CljCompiler.Ast
{
    public class CaseExpr : Expr, MaybePrimitiveExpr
    {
        #region Data

        readonly LocalBindingExpr _expr;
        public LocalBindingExpr Expr { get { return _expr; } }

        readonly int _shift, _mask;
        public int Shift { get { return _shift; } }
        public int Mask { get { return _mask; } }

        readonly int _low, _high;
        public int Low { get { return _low; } }
        public int High { get { return _high; } }

        readonly Expr _defaultExpr;
        public Expr DefaultExpr { get { return _defaultExpr;  } }

        readonly SortedDictionary<int, Expr> _tests;
        public SortedDictionary<int, Expr> Tests { get { return _tests; } }

        readonly Dictionary<int, Expr> _thens;
        public Dictionary<int, Expr> Thens { get { return _thens; } }

        readonly IPersistentMap _sourceSpan;
        public IPersistentMap SourceSpan { get { return _sourceSpan; } }

        readonly Keyword _switchType;
        public Keyword SwitchType { get { return _switchType; } }

        readonly Keyword _testType;
        public Keyword TestType { get { return _testType; } }

        readonly IPersistentSet _skipCheck;
        public IPersistentSet SkipCheck { get { return _skipCheck; } }

        readonly Type _returnType;
        public Type ReturnType { get { return _returnType; } }

        #endregion

        #region Keywords

        static readonly Keyword _compactKey = Keyword.intern(null, "compact");
        static readonly Keyword _sparseKey = Keyword.intern(null, "sparse");
        static readonly Keyword _hashIdentityKey = Keyword.intern(null, "hash-identity");
        static readonly Keyword _hashEquivKey = Keyword.intern(null, "hash-equiv");
        static readonly Keyword _intKey = Keyword.intern(null, "int");

        #endregion

        #region C-tors

        public CaseExpr( IPersistentMap sourceSpan, LocalBindingExpr expr, int shift, int mask, int low, int high, Expr defaultExpr,
                        SortedDictionary<int, Expr> tests, Dictionary<int, Expr> thens, Keyword switchType, Keyword testType, IPersistentSet skipCheck )
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
            if (switchType != _compactKey && switchType != _sparseKey)
                throw new ArgumentException("Unexpected switch type: " + switchType);
            _switchType = switchType;
            if (testType != _intKey && testType != _hashEquivKey && testType != _hashIdentityKey)
                throw new ArgumentException("Unexpected test type: " + testType);
            _testType = testType;
            _skipCheck = skipCheck;
            ICollection<Expr> returns = new List<Expr>(thens.Values)
            {
                defaultExpr
            };
            _returnType = Compiler.MaybeClrType(returns);
            if (RT.count(skipCheck) > 0 && RT.booleanCast(RT.WarnOnReflectionVar.deref()))
            {
                RT.errPrintWriter().WriteLine("Performance warning, {0}:{1}:{2} - hash collision of some case test constants; if selected, those entries will be tested sequentially.",
                    Compiler.SourcePathVar.deref(),Compiler.GetLineFromSpanMap(sourceSpan),Compiler.GetColumnFromSpanMap(sourceSpan));
                RT.errPrintWriter().Flush();
            }
        
        }

        #endregion

        #region Type munging

        public bool HasClrType
        {
            get { return _returnType != null; }
        }

        public Type ClrType
        {
            get { return _returnType; }
        }

        #endregion
        
        #region Parsing

        public sealed class Parser : IParser
        {
            //(case* expr shift mask  default map<minhash, [test then]> table-type test-type skip-check?)
            //prepared by case macro and presumed correct
            //case macro binds actual expr in let so expr is always a local,
            //no need to worry about multiple evaluation
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq)frm;

                if (pcon.Rhc == RHC.Eval)
                    return Compiler.Analyze(pcon, RT.list(RT.list(Compiler.FnOnceSym, PersistentVector.EMPTY, form)), "case__" + RT.nextID());

                IPersistentVector args = LazilyPersistentVector.create(form.next());

                object exprForm = args.nth(0);
                int shift = Util.ConvertToInt(args.nth(1));
                int mask = Util.ConvertToInt(args.nth(2));
                object defaultForm = args.nth(3);
                IPersistentMap caseMap = (IPersistentMap)args.nth(4);
                Keyword switchType = (Keyword)args.nth(5);
                Keyword testType = (Keyword)args.nth(6);
                IPersistentSet skipCheck = RT.count(args) < 8 ? null : (IPersistentSet)args.nth(7);

                ISeq keys = RT.keys(caseMap);
                int low = Util.ConvertToInt(RT.first(keys));
                int high = Util.ConvertToInt(RT.nth(keys, RT.count(keys) - 1));
                LocalBindingExpr testexpr = (LocalBindingExpr)Compiler.Analyze(pcon.SetRhc(RHC.Expression), exprForm);


                SortedDictionary<int, Expr> tests = new SortedDictionary<int, Expr>();
                Dictionary<int, Expr> thens = new Dictionary<int, Expr>();

                foreach (IMapEntry me in caseMap)
                {
                    int minhash = Util.ConvertToInt(me.key());
                    object pair = me.val(); // [test-val then-expr]
                    object first = RT.first(pair);
                    Expr testExpr = testType == _intKey
                        ? NumberExpr.Parse(Util.ConvertToInt(first))
                        : (first == null ? Compiler.NilExprInstance : new ConstantExpr(first));

                    tests[minhash] = testExpr;
                    Expr thenExpr;
                    thenExpr = Compiler.Analyze(pcon, RT.second(pair));
                    thens[minhash] = thenExpr;
                }

                Expr defaultExpr;
                defaultExpr = Compiler.Analyze(pcon, defaultForm);

                return new CaseExpr(
                    (IPersistentMap)Compiler.SourceSpanVar.deref(),
                    testexpr,
                    shift,
                    mask,
                    low,
                    high,
                    defaultExpr,
                    tests,
                    thens,
                    switchType,
                    testType,
                    skipCheck);
            }
        }
        
        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval case");
        }

        #endregion

        #region Code generation

        // Equivalent to :
        //    switch (hashed _expr)
        //    
        //      case i:  if _expr == _test_i
        //                 goto end with _then_i
        //               else goto default
        //               
        //      ...
        //      default:
        //            (default_label)
        //             goto end with _default
        //      end
        //    end_label:

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            DoEmit(rhc, objx, ilg, false);
        }

        public void DoEmit(RHC rhc, ObjExpr objx, CljILGen ilg, bool emitUnboxed)
        {
            GenContext.EmitDebugInfo(ilg, _sourceSpan);

            Label defaultLabel = ilg.DefineLabel();
            Label endLabel = ilg.DefineLabel();

            SortedDictionary<int, Label> labels = new SortedDictionary<int, Label>();
            foreach (int i in _tests.Keys)
                labels[i] = ilg.DefineLabel();

            Type primExprType = Compiler.MaybePrimitiveType(_expr);

            if (_testType == _intKey)
                EmitExprForInts(objx, ilg, primExprType, defaultLabel);
            else
                EmitExprForHashes(objx, ilg);

            if (_switchType == _sparseKey)
            {
                Label[] la = labels.Values.ToArray<Label>();
                ilg.Emit(OpCodes.Switch, la);
                ilg.Emit(OpCodes.Br, defaultLabel);
            }
            else
            {
                Label[] la = new Label[(_high - _low) + 1];
                for (int i = _low; i <= _high; i++)
                    la[i - _low] = labels.ContainsKey(i) ? labels[i] : defaultLabel;
                ilg.EmitInt(_low);
                ilg.Emit(OpCodes.Sub);
                ilg.Emit(OpCodes.Switch, la);
                ilg.Emit(OpCodes.Br, defaultLabel);
             }

            foreach (int i in labels.Keys)
            {
                ilg.MarkLabel(labels[i]);
                if (_testType == _intKey)
                    EmitThenForInts(objx, ilg, primExprType, _tests[i], _thens[i], defaultLabel, emitUnboxed);
                else if ((bool)RT.contains(_skipCheck, i))
                    EmitExpr(objx, ilg, _thens[i], emitUnboxed);
                else
                    EmitThenForHashes(objx, ilg, _tests[i], _thens[i], defaultLabel, emitUnboxed);
                if (  _thens[i].HasNormalExit() )
                    ilg.Emit(OpCodes.Br, endLabel);
            }
            ilg.MarkLabel(defaultLabel);
            EmitExpr(objx, ilg, _defaultExpr, emitUnboxed);
            ilg.MarkLabel(endLabel);
            if (rhc == RHC.Statement)
                ilg.Emit(OpCodes.Pop);
        }


        bool IsShiftMasked { get { return _mask != 0; } }

        void EmitShiftMask(CljILGen ilg)
        {
            if (IsShiftMasked)
            {
                ilg.EmitInt(_shift);
                ilg.Emit(OpCodes.Shr);
                ilg.EmitInt(_mask);
                ilg.Emit(OpCodes.And);
            }
        }

        private void EmitExprForInts(ObjExpr objx, CljILGen ilg, Type exprType, Label defaultLabel)
        {
            if (exprType == null)
            {
                if ( RT.booleanCast(RT.WarnOnReflectionVar.deref()))
                {
                    RT.errPrintWriter().WriteLine("Performance warning, {0}:{1}:{2} - case has int tests, but tested expression is not primitive.",
                        Compiler.SourcePathVar.deref(),Compiler.GetLineFromSpanMap(_sourceSpan),Compiler.GetColumnFromSpanMap(_sourceSpan));
                    RT.errPrintWriter().Flush();
                }
                _expr.Emit(RHC.Expression,objx,ilg);
                ilg.Emit(OpCodes.Call,Compiler.Method_Util_IsNonCharNumeric);
                ilg.Emit(OpCodes.Brfalse,defaultLabel);
                _expr.Emit(RHC.Expression,objx,ilg);
                ilg.Emit(OpCodes.Call,Compiler.Method_Util_ConvertToInt);
                EmitShiftMask(ilg);
            }
            else if (exprType == typeof(long)
                || exprType == typeof(int)
                || exprType == typeof(short)
                || exprType == typeof(byte)
                || exprType == typeof(ulong)
                || exprType == typeof(uint)
                || exprType == typeof(ushort)
                || exprType == typeof(sbyte))
            {
                _expr.EmitUnboxed(RHC.Expression,objx,ilg);
                ilg.Emit(OpCodes.Conv_I4);
                EmitShiftMask(ilg);

            }
            else
            {
                ilg.Emit(OpCodes.Br,defaultLabel);
            }
        }

        private void EmitThenForInts(ObjExpr objx, CljILGen ilg, Type exprType, Expr test, Expr then, Label defaultLabel, bool emitUnboxed)
        {
            if (exprType == null)
            {
                _expr.Emit(RHC.Expression, objx, ilg);
                test.Emit(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Call, Compiler.Method_Util_equiv);
                ilg.Emit(OpCodes.Brfalse, defaultLabel);
                EmitExpr(objx, ilg, then, emitUnboxed);                
            }
            else if (exprType == typeof(long))
            {
                ((NumberExpr)test).EmitUnboxed(RHC.Expression, objx, ilg);
                _expr.EmitUnboxed(RHC.Expression, objx, ilg);
                ilg.Emit(OpCodes.Ceq);
                ilg.Emit(OpCodes.Brfalse, defaultLabel);
                EmitExpr(objx, ilg, then, emitUnboxed);                
              
            }
            else if (exprType == typeof(int)
                || exprType == typeof(short)
                || exprType == typeof(byte)
                || exprType == typeof(ulong)
                || exprType == typeof(uint)
                || exprType == typeof(ushort)
                || exprType == typeof(sbyte))
            {
                if (IsShiftMasked)
                {
                    ((NumberExpr)test).EmitUnboxed(RHC.Expression, objx, ilg);
                    _expr.EmitUnboxed(RHC.Expression, objx, ilg);
                    ilg.Emit(OpCodes.Conv_I8);
                    ilg.Emit(OpCodes.Ceq);
                    ilg.Emit(OpCodes.Brfalse, defaultLabel);
                    EmitExpr(objx, ilg, then, emitUnboxed);
                }
                // else direct match
                EmitExpr(objx, ilg, then, emitUnboxed);  
            }
            else
            {
                ilg.Emit(OpCodes.Br, defaultLabel);
            }
        }

        void EmitExprForHashes(ObjExpr objx, CljILGen ilg)
        {
            _expr.Emit(RHC.Expression, objx, ilg);
            ilg.Emit(OpCodes.Call, Compiler.Method_Util_hash);
            EmitShiftMask(ilg);
        }

        void EmitThenForHashes(ObjExpr objx, CljILGen ilg, Expr test, Expr then, Label defaultLabel, bool emitUnboxed)
        {
            _expr.Emit(RHC.Expression, objx, ilg);
            test.Emit(RHC.Expression, objx, ilg);
            if (_testType == _hashIdentityKey)
            {
                ilg.Emit(OpCodes.Ceq);
                ilg.Emit(OpCodes.Brfalse, defaultLabel);
            }
            else
            {
                ilg.Emit(OpCodes.Call, Compiler.Method_Util_equiv);
                ilg.Emit(OpCodes.Brfalse, defaultLabel);
            }
            EmitExpr(objx, ilg, then, emitUnboxed);  
        }

        private static void EmitExpr(ObjExpr objx, CljILGen ilg, Expr expr, bool emitUnboxed)
        {
            if (emitUnboxed && expr is MaybePrimitiveExpr mbe)
                mbe.EmitUnboxed(RHC.Expression, objx, ilg);
            else
                expr.Emit(RHC.Expression, objx, ilg);
        }

        public bool HasNormalExit() 
        {
            if (_defaultExpr.HasNormalExit())
                return true;

            foreach (Expr e in _thens.Values)
                if (e.HasNormalExit())
                    return true;

            return false;
        }

        public bool CanEmitPrimitive
        {
            get { return Util.IsPrimitive(_returnType); }
        }

        public void EmitUnboxed(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            DoEmit(rhc, objx, ilg, true);
        }

        #endregion
    }
}
