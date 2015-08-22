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
    /// <summary>
    /// An implementation of AFn. Instances of this class are created by the compiler.
    /// </summary>
    /// <remarks>
    /// <para>We need this at the moment as a workaround to DLR not being able to generate instance methods from lambdas.</para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Fn"), Serializable]
    public class AFnImpl :  AFunction, Fn, IFnClosure
    {
        #region Data

        IPersistentMap _meta;
        Closure _closure;

        static readonly Closure _emptyClosure = new Closure(new object[0], new object[0]);

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

        #endregion

        #region C-tors

        public AFnImpl()
        {
            _meta = null;
            _closure = _emptyClosure;
        }

        #endregion

        #region invoke implementations

        public override object invoke()
        {
            if (_fn0 == null) throw WrongArityException(0);
            return _fn0();
        }
        public override object invoke(object arg1)
        {
            if (_fn1 == null) throw WrongArityException(1);
            return _fn1(
                Util.Ret1(arg1, arg1 = null));
        }

        public override object invoke(object arg1, object arg2)
        {
            if (_fn2 == null) throw WrongArityException(2);
            return _fn2(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3)
        {
            if (_fn3 == null) throw WrongArityException(3);
            return _fn3(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4)
        {
            if (_fn4 == null) throw WrongArityException(4);
            return _fn4(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            if (_fn5 == null) throw WrongArityException(5);
            return _fn5(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            if (_fn6 == null) throw WrongArityException(6);
            return _fn6(
                Util.Ret1(arg1, arg1 = null),
                Util.Ret1(arg2, arg2 = null),
                Util.Ret1(arg3, arg3 = null),
                Util.Ret1(arg4, arg4 = null),
                Util.Ret1(arg5, arg5 = null),
                Util.Ret1(arg6, arg6 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            if (_fn7 == null) throw WrongArityException(7);
            return _fn7(
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
            if (_fn8 == null) throw WrongArityException(8);
            return _fn8(
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
            if (_fn9 == null) throw WrongArityException(9);
            return _fn9(
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
            if (_fn10 == null) throw WrongArityException(10);
            return _fn10(
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
            if (_fn11 == null) throw WrongArityException(11);
            return _fn11(
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
            if (_fn12 == null) throw WrongArityException(12);
            return _fn12(
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
            if (_fn13 == null) throw WrongArityException(13);
            return _fn13(
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
            if (_fn14 == null) throw WrongArityException(14);
            return _fn14(
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
            if (_fn15 == null) throw WrongArityException(15);
            return _fn15(
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
            if (_fn16 == null) throw WrongArityException(16);
            return _fn16(
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
            if (_fn17 == null) throw WrongArityException(17);
            return _fn17(
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
            if (_fn18 == null) throw WrongArityException(18);
            return _fn18(
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
            if (_fn19 == null) throw WrongArityException(19);
            return _fn19(
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
            if (_fn20 == null) throw WrongArityException(20);
            return _fn20(
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

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, object arg18, object arg19, object arg20, params object[] args)
        {
            if (_fnRest == null) throw WrongArityException(21);
            return _fnRest(
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
                Util.Ret1(arg20, arg20 = null),
                Util.Ret1(args,args=null));
        }

        #endregion

        #region Meta 

        public override IObj withMeta(IPersistentMap meta)
        {
            AFnImpl copy = (AFnImpl)MemberwiseClone();
            copy._meta = meta;
            return copy;
        }

        public override IPersistentMap meta()
        {
            return _meta;
        }

        #endregion

        #region Arity

        public override bool HasArity(int arity)
        {
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
                default:
                    return arity >= 21 && _fnRest != null;
            }
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
