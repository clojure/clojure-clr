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
    /// Provides a basic implementation of <see cref="IFn">IFn</see> interface methods.
    /// </summary>
    [Serializable]
    public abstract class AFn : IFn
    {
        #region IFn Members

        public virtual object invoke()
        {
            throw WrongArityException(0);
        }

        public virtual object invoke(object arg1)
        {
            throw WrongArityException(1);
        }

        public virtual object invoke(object arg1, object arg2)
        {
            throw WrongArityException(2);
        }

        public virtual object invoke(object arg1, object arg2, object arg3)
        {
            throw WrongArityException(3);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4)
        {
            throw WrongArityException(4);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            throw WrongArityException(5);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6)
        {
            throw WrongArityException(6);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7)
        {
            throw WrongArityException(7);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8)
        {
            throw WrongArityException(8);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9)
        {
            throw WrongArityException(9);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10)
        {
            throw WrongArityException(10);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11)
        {
            throw WrongArityException(11);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12)
        {
            throw WrongArityException(12);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13)
        {
            throw WrongArityException(13);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14)
        {
            throw WrongArityException(14);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14, object arg15)
        {
            throw WrongArityException(15);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14, object arg15, object arg16)
        {
            throw WrongArityException(16);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17)
        {
            throw WrongArityException(17);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, 
            object arg18)
        {
            throw WrongArityException(18);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, 
            object arg18, object arg19)
        {
            throw WrongArityException(19);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, 
            object arg18, object arg19, object arg20)
        {
            throw WrongArityException(20);
        }

        public virtual object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, 
            object arg6, object arg7, object arg8, object arg9, object arg10, object arg11, 
            object arg12, object arg13, object arg14, object arg15, object arg16, object arg17, 
            object arg18, object arg19, object arg20, params object[] args)
        {
            throw WrongArityException(21);
        }



        public virtual object applyTo(ISeq arglist)
        {
            return ApplyToHelper(this, Util.Ret1(arglist,arglist=null));
        }



        public static object ApplyToHelper(IFn ifn, ISeq arglist)
        {
            switch (RT.BoundedLength(arglist, 20))
            {
                case 0:
                    arglist = null;
                    return ifn.invoke();
                case 1:
                    object a1 = arglist.first();
                    arglist = null; 
                    return ifn.invoke(a1);
                case 2:
                    return ifn.invoke(arglist.first()
                            , Util.Ret1((arglist = arglist.next()).first(),arglist = null)
                    );
                case 3:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 4:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 5:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 6:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 7:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 8:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 9:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 10:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 11:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 12:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 13:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 14:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 15:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 16:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 17:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 18:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 19:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                case 20:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , Util.Ret1((arglist = arglist.next()).first(), arglist = null)
                    );
                default:
                    return ifn.invoke(arglist.first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , (arglist = arglist.next()).first()
                            , RT.SeqToArray<object>(Util.Ret1(arglist.next(),arglist=null)));
            }
        }


        public Exception WrongArityException(int i)
        {
            string name = Util.NameForType(GetType()); 
            int suffix = name.LastIndexOf("__");  // NOt sure if this is necessary
            return new ArityException(
                i,
                (suffix == -1 ? name : name.Substring(0, suffix)).Replace('_', '-'));
        }

        #endregion
    }
}
