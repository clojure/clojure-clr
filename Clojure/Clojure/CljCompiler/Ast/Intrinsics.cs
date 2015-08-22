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
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
    public static class Intrinsics
    {
        #region Data

        static Dictionary<MethodInfo, OpCode[]> _ops = new Dictionary<MethodInfo, OpCode[]>();
        static Dictionary<MethodInfo, OpCode[]> _preds = new Dictionary<MethodInfo, OpCode[]>();

        static void AddOp(MethodInfo mi, params OpCode[] opcodes)
        {
            ContractUtils.RequiresNotNull(mi, "methodInfo");

            _ops[mi] = opcodes;
        }

        static void AddOp(Type type, string name, Type[] argTypes, params OpCode[] opcodes)
        {
            AddOp(type.GetMethod(name,argTypes),opcodes);
        }

        static void AddPred(MethodInfo mi, params OpCode[] opcodes)
        {
            ContractUtils.RequiresNotNull(mi, "methodInfo");

            _preds[mi] = opcodes;
        }

        static void AddPred(Type type, string name, Type[] argTypes, params OpCode[] opcodes)
        {
            AddPred(type.GetMethod(name, argTypes), opcodes);
        }

        #endregion

        #region c-tors

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Intrinsics()
        {
            Type nt = typeof(Numbers);
            Type rtt = typeof(RT);
            Type ut = typeof(Util);

            Type it = typeof(int);
            Type dt = typeof(double);
            Type lt = typeof(long);

            Type boolt = typeof(bool);

            Type[] ddta = new Type[] { dt, dt };
            Type[] llta = new Type[] { lt, lt };
            Type[] iita = new Type[] { it, it };
            Type[] bbta = new Type[] { boolt, boolt };

            Type[] dta = new Type[] { dt };
            Type[] lta = new Type[] { lt };
            Type[] ita = new Type[] { it };

            Type[] fta = new Type[] { typeof(float) };
            Type[] sta = new Type[] {typeof(short) };
            Type[] bta = new Type[] {typeof(byte) }; 
            Type[] ulta = new Type[] {typeof(ulong) };
            Type[] uita = new Type[] {typeof(uint) };
            Type[] usta = new Type[] {typeof(ushort) };
            Type[] sbta = new Type[] {typeof(sbyte) };

            AddOp(nt, "add", ddta, OpCodes.Add);
            AddOp(nt, "and", llta, OpCodes.And);
            AddOp(nt, "or", llta, OpCodes.Or);
            AddOp(nt, "xor", llta, OpCodes.Xor);
            AddOp(nt, "multiply", ddta, OpCodes.Mul);
            AddOp(nt, "divide", ddta, OpCodes.Div);
            AddOp(nt, "remainder", llta, OpCodes.Rem);
            AddOp(nt, "shiftLeft", llta,  OpCodes.Shl);
            AddOp(nt, "shiftRight", llta,  OpCodes.Shr);
            AddOp(nt, "unsignedShiftRight", llta, OpCodes.Shr_Un);
            AddOp(nt, "minus", dta, OpCodes.Neg);
            AddOp(nt, "minus", ddta, OpCodes.Sub);
            AddOp(nt, "inc", dta, OpCodes.Ldc_I4_1, OpCodes.Conv_R8, OpCodes.Add);
            AddOp(nt, "dec", dta, OpCodes.Ldc_I4_1, OpCodes.Conv_R8, OpCodes.Sub);
            AddOp(nt, "quotient", llta, OpCodes.Div);
            AddOp(nt, "shiftLeftInt", iita, OpCodes.Shl);
            AddOp(nt, "shiftRightInt", iita, OpCodes.Shr);
            AddOp(nt, "unsignedShiftRightInt", iita, OpCodes.Shr_Un);
            AddOp(nt, "unchecked_int_add", iita, OpCodes.Add );
            AddOp(nt, "unchecked_int_subtract", iita, OpCodes.Sub );
            AddOp(nt, "unchecked_int_negate", ita, OpCodes.Neg );
            AddOp(nt, "unchecked_int_inc", ita, OpCodes.Ldc_I4_1, OpCodes.Add );
            AddOp(nt, "unchecked_int_dec", ita, OpCodes.Ldc_I4_1, OpCodes.Sub );
            AddOp(nt, "unchecked_int_multiply", iita, OpCodes.Mul );
            AddOp(nt, "unchecked_int_divide", iita, OpCodes.Div );
            AddOp(nt, "unchecked_int_remainder", iita, OpCodes.Rem );
            AddOp(nt, "unchecked_add", llta, OpCodes.Add );
            AddOp(nt, "unchecked_add", ddta, OpCodes.Add );
            AddOp(nt, "unchecked_minus", lta, OpCodes.Neg );
            AddOp(nt, "unchecked_minus", dta, OpCodes.Neg );
            AddOp(nt, "unchecked_minus", ddta, OpCodes.Sub );
            AddOp(nt, "unchecked_minus", llta, OpCodes.Sub );
            AddOp(nt, "unchecked_multiply", llta, OpCodes.Mul );
            AddOp(nt, "unchecked_multiply", llta, OpCodes.Mul );
            AddOp(nt, "unchecked_inc", lta, OpCodes.Ldc_I4_1, OpCodes.Conv_I8, OpCodes.Add );
            AddOp(nt, "unchecked_inc", dta, OpCodes.Ldc_I4_1, OpCodes.Conv_R8, OpCodes.Add );
            AddOp(nt, "unchecked_dec", lta, OpCodes.Ldc_I4_1, OpCodes.Conv_I8, OpCodes.Sub );
            AddOp(nt, "unchecked_dec", dta, OpCodes.Ldc_I4_1, OpCodes.Conv_R8, OpCodes.Sub );

            AddOp(rtt, "aget", new Type[] { typeof(float[]), typeof(int) }, OpCodes.Ldelem_R4 );
            AddOp(rtt, "aget", new Type[] { typeof(double[]), typeof(int) }, OpCodes.Ldelem_R8 );
            AddOp(rtt, "aget", new Type[] { typeof(byte[]), typeof(int) }, OpCodes.Ldelem_U1 );
            //AddOp(rtt, "aget", new Type[] { typeof(char[]), typeof(int) }, OpCodes.lde );
            AddOp(rtt, "aget", new Type[] { typeof(short[]), typeof(int) }, OpCodes.Ldelem_I2 );
            AddOp(rtt, "aget", new Type[] { typeof(int[]), typeof(int) }, OpCodes.Ldelem_I4 );
            AddOp(rtt, "aget", new Type[] { typeof(long[]), typeof(int) }, OpCodes.Ldelem_I8 );
            AddOp(rtt, "aget", new Type[] { typeof(sbyte[]), typeof(int) }, OpCodes.Ldelem_I1 );
            AddOp(rtt, "aget", new Type[] { typeof(ushort[]), typeof(int) }, OpCodes.Ldelem_U2 );
            AddOp(rtt, "aget", new Type[] { typeof(uint[]), typeof(int) }, OpCodes.Ldelem_U4 );
            //AddOp(rtt, "aget", new Type[] { typeof(ulong[]), typeof(int) }, OpCodes.lde );
            //AddOp(rtt, "aget", new Type[] { typeof(bool[]), typeof(int) }, OpCodes.lde );
            AddOp(rtt, "aget", new Type[] { typeof(object[]), typeof(int) }, OpCodes.Ldelem_Ref );

            // We need to write a special prefer method to distinguish these.
            //AddOp(rtt, "alength", new Type[] { typeof(float[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(double[]) }, OpCodes.Ldlen);
            //AddOp(rtt, "alength", new Type[] { typeof(decimal[]) }, OpCodes.Ldlen);
            //AddOp(rtt, "alength", new Type[] { typeof(bool[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(byte[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(sbyte[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(short[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(ushort[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(int[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(uint[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(long[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(ulong[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(char[]) }, OpCodes.Ldlen );
            //AddOp(rtt, "alength", new Type[] { typeof(object[]) }, OpCodes.Ldlen );

            AddOp(rtt, "doubleCast", lta, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", dta, OpCodes.Nop );
            AddOp(rtt, "doubleCast", fta, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", ita, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", sta, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", bta, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", ulta, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", uita, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", usta, OpCodes.Conv_R8 );
            AddOp(rtt, "doubleCast", sbta, OpCodes.Conv_R8 );

            AddOp(rtt, "uncheckedDoubleCast", lta, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", dta, OpCodes.Nop );
            AddOp(rtt, "uncheckedDoubleCast", fta, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", ita, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", sta, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", bta, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", ulta, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", uita, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", usta, OpCodes.Conv_R8 );
            AddOp(rtt, "uncheckedDoubleCast", sbta, OpCodes.Conv_R8 );

            AddOp(rtt, "longCast", lta, OpCodes.Conv_I8 );
            AddOp(rtt, "longCast", ita, OpCodes.Conv_I8 );
            AddOp(rtt, "longCast", sta, OpCodes.Conv_I8 );
            AddOp(rtt, "longCast", bta, OpCodes.Conv_I8 );
            AddOp(rtt, "longCast", ulta, OpCodes.Conv_I8 );
            AddOp(rtt, "longCast", uita, OpCodes.Conv_I8 );
            AddOp(rtt, "longCast", usta, OpCodes.Conv_I8 );
            AddOp(rtt, "longCast", sbta, OpCodes.Conv_I8 );

            AddOp(rtt, "uncheckedIntCast", lta, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", dta, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", fta, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", ita, OpCodes.Nop );
            AddOp(rtt, "uncheckedIntCast", sta, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", bta, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", ulta, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", uita, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", usta, OpCodes.Conv_I4 );
            AddOp(rtt, "uncheckedIntCast", sbta, OpCodes.Conv_I4 );

            AddOp(rtt, "uncheckedLongCast", lta, OpCodes.Nop );
            AddOp(rtt, "uncheckedLongCast", dta, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", fta, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", ita, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", sta, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", bta, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", ulta, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", uita, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", usta, OpCodes.Conv_I8 );
            AddOp(rtt, "uncheckedLongCast", sbta, OpCodes.Conv_I8 );

            AddPred(nt, "lt", ddta, OpCodes.Bge );
            AddPred(nt, "lt", llta, OpCodes.Bge );
            AddPred(nt, "equiv", ddta, OpCodes.Ceq, OpCodes.Brfalse  );
            AddPred(nt, "equiv", llta, OpCodes.Ceq, OpCodes.Brfalse  );
            AddPred(nt, "lte", ddta, OpCodes.Bgt );
            AddPred(nt, "lte", llta, OpCodes.Bgt );
            AddPred(nt, "gt", ddta, OpCodes.Ble );
            AddPred(nt, "gt", llta, OpCodes.Ble );
            AddPred(nt, "gte", ddta, OpCodes.Blt );
            AddPred(nt, "gte", llta, OpCodes.Blt );

            AddPred(ut, "equiv", llta, OpCodes.Ceq, OpCodes.Brfalse);
            AddPred(ut, "equiv", ddta, OpCodes.Ceq, OpCodes.Brfalse);
            AddPred(ut, "equiv", bbta, OpCodes.Ceq, OpCodes.Brfalse);

            AddPred(nt, "isZero", dta, OpCodes.Ldc_I4_0, OpCodes.Conv_R8, OpCodes.Ceq, OpCodes.Brfalse );
            AddPred(nt, "isZero", lta,OpCodes.Ldc_I4_0, OpCodes.Conv_I8, OpCodes.Ceq, OpCodes.Brfalse );
            AddPred(nt, "isPos", dta, OpCodes.Ldc_I4_0, OpCodes.Conv_R8, OpCodes.Ble );
            AddPred(nt, "isPos", lta, OpCodes.Ldc_I4_0, OpCodes.Conv_I8, OpCodes.Ble );
            AddPred(nt, "isNeg", dta, OpCodes.Ldc_I4_0, OpCodes.Conv_R8, OpCodes.Bge );
            AddPred(nt, "isNeg", lta, OpCodes.Ldc_I4_0, OpCodes.Conv_I8, OpCodes.Bge );
        }

        #endregion

        #region Operations

        public static bool HasPred(MethodInfo method)
        {
            return _preds.ContainsKey(method);
        }

        public static void EmitPred(MethodInfo method, ILGen ilg, Label falseLabel)
        {
            OpCode[] opcodes = _preds[method];

            for (int i = 0; i < opcodes.Length - 1; i++)
                ilg.Emit(opcodes[i]);
            ilg.Emit(opcodes[opcodes.Length - 1], falseLabel);
        }

        public static bool HasOp(MethodInfo method)
        {
            return _ops.ContainsKey(method);
        }

        public static void EmitOp(MethodInfo method, ILGen ilg)
        {
            OpCode[] opcodes = _ops[method];

            // special case the shift methods because we didn't create a way to embed arguments to the opcodes.
            // the long second-arg bit-shifts need to mask to a value <= 63
            // the int second-arg bit-shifts need to maks to a value <= 31
            switch (method.Name)
            {
                case "shiftLeft":
                case "shiftRight":
                case "unsignedShiftRight":
                    ilg.Emit(OpCodes.Conv_I4);
                    ilg.EmitInt(0x3f);
                    ilg.Emit(OpCodes.And);
                    break;

                case "shiftLeftInt":
                case "shiftRightInt":
                case "unsignedShiftRightInt":
                    ilg.EmitInt(0x1f);
                    ilg.Emit(OpCodes.And);
                    break;
            }

            foreach ( OpCode opcode in opcodes )
                ilg.Emit(opcode);
        }

        #endregion
    }
}
