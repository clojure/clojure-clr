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

    // The problem here is that I need the functionality of both RestFn and AfnImpl.
    // Because they are both classes, we can't derive from both.
    // For the time being, I choose to inherit from RestFn and re-implement the AFnImpl code.
    // Eventually, we need to do overloading to solve this problem.
    // Overloading is not possible at the moment do to a bug in LambdaExpression.CompileToMethod

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Fn"), Serializable]
    public class RestFnImpl : RestFn, IFnClosure
    {
        #region Data

        IPersistentMap _meta;
        Closure _closure;

        static readonly Closure _emptyClosure = new Closure(new Object[0], new Object[0]);

        protected readonly int _reqArity;

        #endregion

        #region C-tor

        public RestFnImpl(int reqArity)
        {
            _meta = null;
            _reqArity = reqArity;
            _closure = _emptyClosure;
        }

        #endregion

        #region arity

        public override int getRequiredArity()
        {
            return _reqArity;
        }

        public override bool HasArity(int arity)
        {
            if (arity >= getRequiredArity())
                return true;

            switch (arity)
            {
                case 0:
                    return _fn0 != null;
                case 1:
                    return _fn1 != null;
                case 2:
                    return _fn2 != null;
                case 3:
                    return _fn3 != null;
                case 4:
                    return _fn4 != null;
                case 5:
                    return _fn5 != null;
                case 6:
                    return _fn6 != null;
                case 7:
                    return _fn7 != null;
                case 8:
                    return _fn8 != null;
                case 9:
                    return _fn9 != null;
                case 10:
                    return _fn10 != null;
                case 11:
                    return _fn11 != null;
                case 12:
                    return _fn12 != null;
                case 13:
                    return _fn13 != null;
                case 14:
                    return _fn14 != null;
                case 15:
                    return _fn15 != null;
                case 16:
                    return _fn16 != null;
                case 17:
                    return _fn17 != null;
                case 18:
                    return _fn18 != null;
                case 19:
                    return _fn19 != null;
                case 20:
                    return _fn20 != null;
            }
            return false;
        }

        #endregion

        #region Method slots

        public FFunc<
            object> _fn0;

        public FFunc<
            object,
            object> _fn1;

        public FFunc<
            object, object,
            object> _fn2;

        public FFunc<
            object, object, object,
            object> _fn3;

        public FFunc<
            object, object, object, object,
            object> _fn4;

        public FFunc<
            object, object, object, object, object,
            object> _fn5;

        public FFunc<
            object, object, object, object, object,
            object,
            object> _fn6;

        public FFunc<
            object, object, object, object, object,
            object, object,
            object> _fn7;

        public FFunc<
            object, object, object, object, object,
            object, object, object,
            object> _fn8;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object,
            object> _fn9;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object> _fn10;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object,
            object> _fn11;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object,
            object> _fn12;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object,
            object> _fn13;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object,
            object> _fn14;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object> _fn15;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object,
            object> _fn16;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object,
            object> _fn17;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object,
            object> _fn18;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object,
            object> _fn19;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object> _fn20;

        public VFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object> _fnRest;


        public FFunc<
             object, object> _fnDo0;

        public FFunc<
            object,
            object, object> _fnDo1;

        public FFunc<
            object, object,
            object, object> _fnDo2;

        public FFunc<
            object, object, object,
            object, object> _fnDo3;

        public FFunc<
            object, object, object, object,
            object, object> _fnDo4;

        public FFunc<
            object, object, object, object, object,
            object, object> _fnDo5;

        public FFunc<
            object, object, object, object, object,
            object,
            object, object> _fnDo6;

        public FFunc<
            object, object, object, object, object,
            object, object,
            object, object> _fnDo7;

        public FFunc<
            object, object, object, object, object,
            object, object, object,
            object, object> _fnDo8;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object,
            object, object> _fnDo9;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object> _fnDo10;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object,
            object, object> _fnDo11;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object,
            object, object> _fnDo12;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object,
            object, object> _fnDo13;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object,
            object, object> _fnDo14;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object> _fnDo15;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object,
            object, object> _fnDo16;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object,
            object, object> _fnDo17;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object,
            object, object> _fnDo18;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object,
            object, object> _fnDo19;

        public FFunc<
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object, object, object, object,
            object, object> _fnDo20;

        #endregion

        #region doInvoke implementations

        protected override object doInvoke(object args)
        {
            if (_fnDo0 == null) throw WrongArityException(0);
            return _fnDo0(args);
        }
        protected override object doInvoke(object arg1, object args)
        {
            if (_fnDo1 == null) throw WrongArityException(1);
            return _fnDo1(arg1, args);
        }

        protected override object doInvoke(object arg1, object arg2, object args)
        {
            if (_fnDo2 == null) throw WrongArityException(2);
            return _fnDo2(arg1, arg2, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object args)
        {
            if (_fnDo3 == null) throw WrongArityException(3);
            return _fnDo3(arg1, arg2, arg3, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object args)
        {
            if (_fnDo4 == null) throw WrongArityException(4);
            return _fnDo4(arg1, arg2, arg3, arg4, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object args)
        {
            if (_fnDo5 == null) throw WrongArityException(5);
            return _fnDo5(arg1, arg2, arg3, arg4, arg5, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object args)
        {
            if (_fnDo6 == null) throw WrongArityException(6);
            return _fnDo6(arg1, arg2, arg3, arg4, arg5, arg6, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object args)
        {
            if (_fnDo7 == null) throw WrongArityException(7);
            return _fnDo7(arg1, arg2, arg3, arg4, arg5, arg6, arg7, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object args)
        {
            if (_fnDo8 == null) throw WrongArityException(8);
            return _fnDo8(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object args)
        {
            if (_fnDo9 == null) throw WrongArityException(9);
            return _fnDo9(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object args)
        {
            if (_fnDo10 == null) throw WrongArityException(10);
            return _fnDo10(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object args)
        {
            if (_fnDo11 == null) throw WrongArityException(11);
            return _fnDo11(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object args)
        {
            if (_fnDo12 == null) throw WrongArityException(12);
            return _fnDo12(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object args)
        {
            if (_fnDo13 == null) throw WrongArityException(13);
            return _fnDo13(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object args)
        {
            if (_fnDo14 == null) throw WrongArityException(14);
            return _fnDo14(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object args)
        {
            if (_fnDo15 == null) throw WrongArityException(15);
            return _fnDo15(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object args)
        {
            if (_fnDo16 == null) throw WrongArityException(16);
            return _fnDo16(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object args)
        {
            if (_fnDo17 == null) throw WrongArityException(17);
            return _fnDo17(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object args)
        {
            if (_fnDo18 == null) throw WrongArityException(18);
            return _fnDo18(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19, object args)
        {
            if (_fnDo19 == null) throw WrongArityException(19);
            return _fnDo19(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, args);
        }

        protected override object doInvoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19, object arg20, object args)
        {
            if (_fnDo20 == null) throw WrongArityException(20);
            return _fnDo20(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20, args);
        }

        #endregion

        #region  invoke implementations

        public override object invoke()
        {
            return (_fn0 == null)
                ? base.invoke()
                : _fn0();
        }
        public override object invoke(object arg1)
        {
            return (_fn1 == null)
                ? base.invoke(arg1)
                : _fn1(
                Util.Ret1(arg1, arg1 = null));
        }

        public override object invoke(object arg1, object arg2)
        {
            return (_fn2 == null)
                ? base.invoke(arg1, arg2)
                : _fn2(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3)
        {
            return (_fn3 == null)
                ? base.invoke(arg1, arg2, arg3)
                : _fn3(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4)
        {
            return (_fn4 == null)
                ? base.invoke(arg1, arg2, arg3, arg4)
                : _fn4(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return (_fn5 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5)
                : _fn5(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            return (_fn6 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6)
                : _fn6(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            return (_fn7 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7)
                : _fn7(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
        {
            return (_fn8 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)
                : _fn8(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
        {
            return (_fn9 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)
                : _fn9(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
        {
            return (_fn10 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)
                : _fn10(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
        {
            return (_fn11 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11)
                : _fn11(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12)
        {
            return (_fn12 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12)
                : _fn12(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
        {
            return (_fn13 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13)
                : _fn13(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14)
        {
            return (_fn14 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14)
                : _fn14(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null),
                Util.Ret1(arg14, arg14 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15)
        {
            return (_fn15 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15)
                : _fn15(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null),
                Util.Ret1(arg14, arg14 = null),
                Util.Ret1(arg15, arg15 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16)
        {
            return (_fn16 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16)
                : _fn16(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null),
                Util.Ret1(arg14, arg14 = null),
                Util.Ret1(arg15, arg15 = null),
                Util.Ret1(arg16, arg16 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17)
        {
            return (_fn17 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17)
                : _fn17(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null),
                Util.Ret1(arg14, arg14 = null),
                Util.Ret1(arg15, arg15 = null),
                Util.Ret1(arg16, arg16 = null),
                Util.Ret1(arg17, arg17 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18)
        {
            return (_fn18 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18)
                : _fn18(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null),
                Util.Ret1(arg14, arg14 = null),
                Util.Ret1(arg15, arg15 = null),
                Util.Ret1(arg16, arg16 = null),
                Util.Ret1(arg17, arg17 = null),
                Util.Ret1(arg18, arg18 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19)
        {
            return (_fn19 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19)
                : _fn19(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null),
                Util.Ret1(arg14, arg14 = null),
                Util.Ret1(arg15, arg15 = null),
                Util.Ret1(arg16, arg16 = null),
                Util.Ret1(arg17, arg17 = null),
                Util.Ret1(arg18, arg18 = null),
                Util.Ret1(arg19, arg19 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19, object arg20)
        {
            return (_fn20 == null)
                ? base.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20)
                : _fn20(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null),
                Util.Ret1(arg7, arg7 = null),
                Util.Ret1(arg8, arg8 = null),
                Util.Ret1(arg9, arg9 = null),
                Util.Ret1(arg10, arg10 = null),
                Util.Ret1(arg11, arg11 = null),
                Util.Ret1(arg12, arg12 = null),
                Util.Ret1(arg13, arg13 = null),
                Util.Ret1(arg14, arg14 = null),
                Util.Ret1(arg15, arg15 = null),
                Util.Ret1(arg16, arg16 = null),
                Util.Ret1(arg17, arg17 = null),
                Util.Ret1(arg18, arg18 = null),
                Util.Ret1(arg19, arg19 = null),
                Util.Ret1(arg20, arg20 = null));
        }

        //public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19, object arg20, params object[] args)
        //{
        //    return (_fnRest == null) ? base.invoke() 
        //    : _fnRest(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, arg20, args);
        //}

        #endregion

        #region Meta members

        public override IObj withMeta(IPersistentMap meta)
        {
            RestFnImpl copy = (RestFnImpl) MemberwiseClone();
            copy._meta = meta;
            return copy;
        }

        public override IPersistentMap meta()
        {
            return _meta;
        }

        #endregion

        #region IFnClosure methods

        public Closure GetClosure()
        {
            return _closure;
        }

        public void SetClosure(Closure closure)
        {
            _closure = closure;
        }

        #endregion

    }
}
