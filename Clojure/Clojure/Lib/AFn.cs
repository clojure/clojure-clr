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
using clojure.lang.Runtime.Binding;
using System.Dynamic;

namespace clojure.lang
{
    /// <summary>
    /// Provides a basic implementation of <see cref="IFn">IFn</see> interface methods.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Fn"), Serializable]
    public abstract class AFn : IFn, IDynamicMetaObjectProvider, IFnArity
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



        public static object ApplyToHelper(IFn fn, ISeq argList)
        {
            switch (RT.BoundedLength(argList, 20))
            {
                case 0:
                    argList = null;
                    return fn.invoke();
                case 1:
                    return fn.invoke(Util.Ret1(argList.first(),argList=null));
                case 2:
                    return fn.invoke(argList.first()
                            , Util.Ret1((argList = argList.next()).first(),argList = null)
                    );
                case 3:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 4:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 5:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 6:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 7:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 8:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 9:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 10:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 11:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 12:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 13:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 14:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 15:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 16:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 17:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 18:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 19:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                case 20:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , Util.Ret1((argList = argList.next()).first(), argList = null)
                    );
                default:
                    return fn.invoke(argList.first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , (argList = argList.next()).first()
                            , RT.SeqToArray<object>(Util.Ret1(argList.next(),argList=null)));
            }
        }


        public Exception WrongArityException(int reqArity)
        {
            string name = Util.NameForType(GetType()); 
            return new ArityException(
                reqArity,
                Compiler.demunge(name));
        }

        #endregion

        #region IDynamicMetaObjectProvider methods

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaAFn(parameter, this);
        }

        #endregion

        #region IFnArity methods

        public virtual bool HasArity(int arity)
        {
            return false;
        }

        #endregion
    }

}
