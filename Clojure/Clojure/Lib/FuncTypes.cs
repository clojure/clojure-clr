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

namespace clojure.lang
{
        public delegate TResult FFunc<TResult>();
        public delegate TResult FFunc<T, TResult>(T arg);
        public delegate TResult FFunc<T1, T2, TResult>(T1 arg1, T2 arg2);
        public delegate TResult FFunc<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
        public delegate TResult FFunc<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, T20 arg20);
        public delegate TResult FFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, T20 arg20, T21 arg21);



        public delegate TResult VFunc<TRest, TResult>(params TRest[] argrest);
        public delegate TResult VFunc<T1, TRest, TResult>(T1 arg1, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, TRest, TResult>(T1 arg1, T2 arg2, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, params TRest[] argrest);
        public delegate TResult VFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TRest, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, T20 arg20, params TRest[] argrest);


        public static class FuncTypeHelpers
        {
            public static Type GetFFuncType(int numArgs)
            {
                switch (numArgs)
                {
                    case 0: return typeof(FFunc<object>);
                    case 1: return typeof(FFunc<object,object>);
                    case 2: return typeof(FFunc<object, object, object>);
                    case 3: return typeof(FFunc<object, object, object, object>);
                    case 4: return typeof(FFunc<object, object, object, object, object>);
                    case 5: return typeof(FFunc<object, object, object, object, object, object>);
                    case 6: return typeof(FFunc<object, object, object, object, object, object, object>);
                    case 7: return typeof(FFunc<object, object, object, object, object, object, object, object>);
                    case 8: return typeof(FFunc<object, object, object, object, object, object, object, object, object>);
                    case 9: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object>);
                    case 10: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object>);
                    case 11: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 12: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 13: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 14: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 15: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 16: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 17: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 18: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 19: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
                    case 20: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
    
                    default: return typeof(FFunc<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>);
;
                }
            }

        }
}
