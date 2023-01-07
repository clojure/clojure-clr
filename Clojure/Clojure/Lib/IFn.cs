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


using System.Diagnostics.CodeAnalysis;
namespace clojure.lang
{
    /// <summary>
    /// Represents an object that can be used a function.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>IFn</c> provides complete access to invoking
    /// any of Clojure's <a href="http://clojure.github.io/clojure/">API</a>s.
    /// You can also access any other library written in Clojure, after adding
    /// either its source or compiled form to the classpath.</para>
    /// </remarks>
    public interface IFn // Callable, Runnable -- no equivalents
    {
        #region Invoke methods
#pragma warning disable IDE1006 // Naming Styles

        object invoke();

        object invoke(object arg1);

        object invoke(object arg1, object arg2);

        object invoke(object arg1, object arg2, object arg3);

        object invoke(object arg1, object arg2, object arg3, object arg4);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12, object arg13);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                      object arg15);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19, object arg20);

        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19, object arg20,
                             params object[] args);

        object applyTo(ISeq arglist);

        #endregion
    }

    namespace primifs
    {

        #region prim interfaces




        public interface L
        {

            long invokePrim();
        }



        public interface D
        {

            double invokePrim();
        }



        public interface OL
        {

            long invokePrim(object arg0);
        }



        public interface OD
        {

            double invokePrim(object arg0);
        }



        public interface LO
        {

            object invokePrim(long arg0);
        }
        


        public interface LL
        {

            long invokePrim(long arg0);
        }
        


        public interface LD
        {

            double invokePrim(long arg0);
        }



        public interface DO
        {

            object invokePrim(double arg0);
        }
        


        public interface DL
        {

            long invokePrim(double arg0);
        }
        


        public interface DD
        {

            double invokePrim(double arg0);
        }
        


        public interface OOL
        {

            long invokePrim(object arg0, object arg1);
        }
        


        public interface OOD
        {

            double invokePrim(object arg0, object arg1);
        }
        


        public interface OLO
        {

            object invokePrim(object arg0, long arg1);
        }
        


        public interface OLL
        {

            long invokePrim(object arg0, long arg1);
        }
        


        public interface OLD
        {

            double invokePrim(object arg0, long arg1);
        }
        


        public interface ODO
        {

            object invokePrim(object arg0, double arg1);
        }
        


        public interface ODL
        {

            long invokePrim(object arg0, double arg1);
        }
        


        public interface ODD
        {

            double invokePrim(object arg0, double arg1);
        }
        


        public interface LOO
        {

            object invokePrim(long arg0, object arg1);
        }
        


        public interface LOL
        {

            long invokePrim(long arg0, object arg1);
        }
        


        public interface LOD
        {

            double invokePrim(long arg0, object arg1);
        }
        


        public interface LLO
        {

            object invokePrim(long arg0, long arg1);
        }
        


        public interface LLL
        {

            long invokePrim(long arg0, long arg1);
        }
        


        public interface LLD
        {

            double invokePrim(long arg0, long arg1);
        }
        


        public interface LDO
        {

            object invokePrim(long arg0, double arg1);
        }
        


        public interface LDL
        {

            long invokePrim(long arg0, double arg1);
        }
        


        public interface LDD
        {

            double invokePrim(long arg0, double arg1);
        }
        


        public interface DOO
        {

            object invokePrim(double arg0, object arg1);
        }
        


        public interface DOL
        {

            long invokePrim(double arg0, object arg1);
        }
        


        public interface DOD
        {

            double invokePrim(double arg0, object arg1);
        }
        


        public interface DLO
        {

            object invokePrim(double arg0, long arg1);
        }
        


        public interface DLL
        {

            long invokePrim(double arg0, long arg1);
        }
        


        public interface DLD
        {

            double invokePrim(double arg0, long arg1);
        }
        


        public interface DDO
        {

            object invokePrim(double arg0, double arg1);
        }
        


        public interface DDL
        {

            long invokePrim(double arg0, double arg1);
        }
        


        public interface DDD
        {

            double invokePrim(double arg0, double arg1);
        }
        


        public interface OOOL
        {

            long invokePrim(object arg0, object arg1, object arg2);
        }
        


        public interface OOOD
        {

            double invokePrim(object arg0, object arg1, object arg2);
        }
        


        public interface OOLO
        {

            object invokePrim(object arg0, object arg1, long arg2);
        }
        


        public interface OOLL
        {

            long invokePrim(object arg0, object arg1, long arg2);
        }
        


        public interface OOLD
        {

            double invokePrim(object arg0, object arg1, long arg2);
        }
        


        public interface OODO
        {

            object invokePrim(object arg0, object arg1, double arg2);
        }
        


        public interface OODL
        {

            long invokePrim(object arg0, object arg1, double arg2);
        }
        


        public interface OODD
        {

            double invokePrim(object arg0, object arg1, double arg2);
        }
        


        public interface OLOO
        {

            object invokePrim(object arg0, long arg1, object arg2);
        }
        


        public interface OLOL
        {

            long invokePrim(object arg0, long arg1, object arg2);
        }
        


        public interface OLOD
        {

            double invokePrim(object arg0, long arg1, object arg2);
        }
        


        public interface OLLO
        {

            object invokePrim(object arg0, long arg1, long arg2);
        }
        


        public interface OLLL
        {

            long invokePrim(object arg0, long arg1, long arg2);
        }
        


        public interface OLLD
        {

            double invokePrim(object arg0, long arg1, long arg2);
        }
        


        public interface OLDO
        {

            object invokePrim(object arg0, long arg1, double arg2);
        }
        


        public interface OLDL
        {

            long invokePrim(object arg0, long arg1, double arg2);
        }
        


        public interface OLDD
        {

            double invokePrim(object arg0, long arg1, double arg2);
        }
        


        public interface ODOO
        {

            object invokePrim(object arg0, double arg1, object arg2);
        }
        


        public interface ODOL
        {

            long invokePrim(object arg0, double arg1, object arg2);
        }
        


        public interface ODOD
        {

            double invokePrim(object arg0, double arg1, object arg2);
        }
        


        public interface ODLO
        {

            object invokePrim(object arg0, double arg1, long arg2);
        }
        


        public interface ODLL
        {

            long invokePrim(object arg0, double arg1, long arg2);
        }
        


        public interface ODLD
        {

            double invokePrim(object arg0, double arg1, long arg2);
        }
        


        public interface ODDO
        {

            object invokePrim(object arg0, double arg1, double arg2);
        }
        


        public interface ODDL
        {

            long invokePrim(object arg0, double arg1, double arg2);
        }
        


        public interface ODDD
        {

            double invokePrim(object arg0, double arg1, double arg2);
        }
        


        public interface LOOO
        {

            object invokePrim(long arg0, object arg1, object arg2);
        }
        


        public interface LOOL
        {

            long invokePrim(long arg0, object arg1, object arg2);
        }
        


        public interface LOOD
        {

            double invokePrim(long arg0, object arg1, object arg2);
        }
        


        public interface LOLO
        {

            object invokePrim(long arg0, object arg1, long arg2);
        }
        


        public interface LOLL
        {

            long invokePrim(long arg0, object arg1, long arg2);
        }
        


        public interface LOLD
        {

            double invokePrim(long arg0, object arg1, long arg2);
        }
        


        public interface LODO
        {

            object invokePrim(long arg0, object arg1, double arg2);
        }
        


        public interface LODL
        {

            long invokePrim(long arg0, object arg1, double arg2);
        }
        


        public interface LODD
        {

            double invokePrim(long arg0, object arg1, double arg2);
        }
        


        public interface LLOO
        {

            object invokePrim(long arg0, long arg1, object arg2);
        }
        


        public interface LLOL
        {

            long invokePrim(long arg0, long arg1, object arg2);
        }
        


        public interface LLOD
        {

            double invokePrim(long arg0, long arg1, object arg2);
        }
        


        public interface LLLO
        {

            object invokePrim(long arg0, long arg1, long arg2);
        }
        


        public interface LLLL
        {

            long invokePrim(long arg0, long arg1, long arg2);
        }
        


        public interface LLLD
        {

            double invokePrim(long arg0, long arg1, long arg2);
        }
        


        public interface LLDO
        {

            object invokePrim(long arg0, long arg1, double arg2);
        }
        


        public interface LLDL
        {

            long invokePrim(long arg0, long arg1, double arg2);
        }
        


        public interface LLDD
        {

            double invokePrim(long arg0, long arg1, double arg2);
        }
        


        public interface LDOO
        {

            object invokePrim(long arg0, double arg1, object arg2);
        }
        


        public interface LDOL
        {

            long invokePrim(long arg0, double arg1, object arg2);
        }
        


        public interface LDOD
        {

            double invokePrim(long arg0, double arg1, object arg2);
        }
        


        public interface LDLO
        {

            object invokePrim(long arg0, double arg1, long arg2);
        }
        


        public interface LDLL
        {

            long invokePrim(long arg0, double arg1, long arg2);
        }
        


        public interface LDLD
        {

            double invokePrim(long arg0, double arg1, long arg2);
        }
        


        public interface LDDO
        {

            object invokePrim(long arg0, double arg1, double arg2);
        }
        


        public interface LDDL
        {

            long invokePrim(long arg0, double arg1, double arg2);
        }
        


        public interface LDDD
        {

            double invokePrim(long arg0, double arg1, double arg2);
        }
        


        public interface DOOO
        {

            object invokePrim(double arg0, object arg1, object arg2);
        }
        


        public interface DOOL
        {

            long invokePrim(double arg0, object arg1, object arg2);
        }
        


        public interface DOOD
        {

            double invokePrim(double arg0, object arg1, object arg2);
        }
        


        public interface DOLO
        {

            object invokePrim(double arg0, object arg1, long arg2);
        }
        


        public interface DOLL
        {

            long invokePrim(double arg0, object arg1, long arg2);
        }
        


        public interface DOLD
        {

            double invokePrim(double arg0, object arg1, long arg2);
        }
        


        public interface DODO
        {

            object invokePrim(double arg0, object arg1, double arg2);
        }
        


        public interface DODL
        {

            long invokePrim(double arg0, object arg1, double arg2);
        }
        


        public interface DODD
        {

            double invokePrim(double arg0, object arg1, double arg2);
        }
        


        public interface DLOO
        {

            object invokePrim(double arg0, long arg1, object arg2);
        }
        


        public interface DLOL
        {

            long invokePrim(double arg0, long arg1, object arg2);
        }
        


        public interface DLOD
        {

            double invokePrim(double arg0, long arg1, object arg2);
        }
        


        public interface DLLO
        {

            object invokePrim(double arg0, long arg1, long arg2);
        }
        


        public interface DLLL
        {

            long invokePrim(double arg0, long arg1, long arg2);
        }
        


        public interface DLLD
        {

            double invokePrim(double arg0, long arg1, long arg2);
        }
        


        public interface DLDO
        {

            object invokePrim(double arg0, long arg1, double arg2);
        }
        


        public interface DLDL
        {

            long invokePrim(double arg0, long arg1, double arg2);
        }
        


        public interface DLDD
        {

            double invokePrim(double arg0, long arg1, double arg2);
        }
        


        public interface DDOO
        {

            object invokePrim(double arg0, double arg1, object arg2);
        }
        


        public interface DDOL
        {

            long invokePrim(double arg0, double arg1, object arg2);
        }
        


        public interface DDOD
        {

            double invokePrim(double arg0, double arg1, object arg2);
        }
        


        public interface DDLO
        {

            object invokePrim(double arg0, double arg1, long arg2);
        }
        


        public interface DDLL
        {

            long invokePrim(double arg0, double arg1, long arg2);
        }
        


        public interface DDLD
        {

            double invokePrim(double arg0, double arg1, long arg2);
        }
        


        public interface DDDO
        {

            object invokePrim(double arg0, double arg1, double arg2);
        }
        


        public interface DDDL
        {

            long invokePrim(double arg0, double arg1, double arg2);
        }
        


        public interface DDDD
        {

            double invokePrim(double arg0, double arg1, double arg2);
        }
        


        public interface OOOOL
        {


            long invokePrim(object arg0, object arg1, object arg2, object arg3);
        }
        


        public interface OOOOD
        {


            double invokePrim(object arg0, object arg1, object arg2, object arg3);
        }
        


        public interface OOOLO
        {

            object invokePrim(object arg0, object arg1, object arg2, long arg3);
        }
        


        public interface OOOLL
        {

            long invokePrim(object arg0, object arg1, object arg2, long arg3);
        }
        


        public interface OOOLD
        {

            double invokePrim(object arg0, object arg1, object arg2, long arg3);
        }
        


        public interface OOODO
        {

            object invokePrim(object arg0, object arg1, object arg2, double arg3);
        }
        


        public interface OOODL
        {

            long invokePrim(object arg0, object arg1, object arg2, double arg3);
        }
        


        public interface OOODD
        {

            double invokePrim(object arg0, object arg1, object arg2, double arg3);
        }
        


        public interface OOLOO
        {

            object invokePrim(object arg0, object arg1, long arg2, object arg3);
        }
        


        public interface OOLOL
        {

            long invokePrim(object arg0, object arg1, long arg2, object arg3);
        }
        


        public interface OOLOD
        {

            double invokePrim(object arg0, object arg1, long arg2, object arg3);
        }
        


        public interface OOLLO
        {

            object invokePrim(object arg0, object arg1, long arg2, long arg3);
        }
        


        public interface OOLLL
        {

            long invokePrim(object arg0, object arg1, long arg2, long arg3);
        }
        


        public interface OOLLD
        {

            double invokePrim(object arg0, object arg1, long arg2, long arg3);
        }
        


        public interface OOLDO
        {

            object invokePrim(object arg0, object arg1, long arg2, double arg3);
        }
        


        public interface OOLDL
        {

            long invokePrim(object arg0, object arg1, long arg2, double arg3);
        }
        


        public interface OOLDD
        {

            double invokePrim(object arg0, object arg1, long arg2, double arg3);
        }
        


        public interface OODOO
        {

            object invokePrim(object arg0, object arg1, double arg2, object arg3);
        }
        


        public interface OODOL
        {

            long invokePrim(object arg0, object arg1, double arg2, object arg3);
        }
        


        public interface OODOD
        {

            double invokePrim(object arg0, object arg1, double arg2, object arg3);
        }
        


        public interface OODLO
        {

            object invokePrim(object arg0, object arg1, double arg2, long arg3);
        }
        


        public interface OODLL
        {

            long invokePrim(object arg0, object arg1, double arg2, long arg3);
        }
        


        public interface OODLD
        {

            double invokePrim(object arg0, object arg1, double arg2, long arg3);
        }
        


        public interface OODDO
        {

            object invokePrim(object arg0, object arg1, double arg2, double arg3);
        }
        


        public interface OODDL
        {

            long invokePrim(object arg0, object arg1, double arg2, double arg3);
        }
        


        public interface OODDD
        {

            double invokePrim(object arg0, object arg1, double arg2, double arg3);
        }
        


        public interface OLOOO
        {

            object invokePrim(object arg0, long arg1, object arg2, object arg3);
        }
        


        public interface OLOOL
        {

            long invokePrim(object arg0, long arg1, object arg2, object arg3);
        }
        


        public interface OLOOD
        {

            double invokePrim(object arg0, long arg1, object arg2, object arg3);
        }
        


        public interface OLOLO
        {

            object invokePrim(object arg0, long arg1, object arg2, long arg3);
        }
        


        public interface OLOLL
        {

            long invokePrim(object arg0, long arg1, object arg2, long arg3);
        }
        


        public interface OLOLD
        {

            double invokePrim(object arg0, long arg1, object arg2, long arg3);
        }
        


        public interface OLODO
        {

            object invokePrim(object arg0, long arg1, object arg2, double arg3);
        }
        


        public interface OLODL
        {

            long invokePrim(object arg0, long arg1, object arg2, double arg3);
        }
        


        public interface OLODD
        {

            double invokePrim(object arg0, long arg1, object arg2, double arg3);
        }
        


        public interface OLLOO
        {

            object invokePrim(object arg0, long arg1, long arg2, object arg3);
        }
        


        public interface OLLOL
        {

            long invokePrim(object arg0, long arg1, long arg2, object arg3);
        }
        


        public interface OLLOD
        {

            double invokePrim(object arg0, long arg1, long arg2, object arg3);
        }
        


        public interface OLLLO
        {

            object invokePrim(object arg0, long arg1, long arg2, long arg3);
        }
        


        public interface OLLLL
        {

            long invokePrim(object arg0, long arg1, long arg2, long arg3);
        }
        


        public interface OLLLD
        {

            double invokePrim(object arg0, long arg1, long arg2, long arg3);
        }
        


        public interface OLLDO
        {

            object invokePrim(object arg0, long arg1, long arg2, double arg3);
        }
        


        public interface OLLDL
        {

            long invokePrim(object arg0, long arg1, long arg2, double arg3);
        }
        


        public interface OLLDD
        {

            double invokePrim(object arg0, long arg1, long arg2, double arg3);
        }
        


        public interface OLDOO
        {

            object invokePrim(object arg0, long arg1, double arg2, object arg3);
        }
        


        public interface OLDOL
        {

            long invokePrim(object arg0, long arg1, double arg2, object arg3);
        }
        


        public interface OLDOD
        {

            double invokePrim(object arg0, long arg1, double arg2, object arg3);
        }
        


        public interface OLDLO
        {

            object invokePrim(object arg0, long arg1, double arg2, long arg3);
        }
        


        public interface OLDLL
        {

            long invokePrim(object arg0, long arg1, double arg2, long arg3);
        }
        


        public interface OLDLD
        {

            double invokePrim(object arg0, long arg1, double arg2, long arg3);
        }
        


        public interface OLDDO
        {

            object invokePrim(object arg0, long arg1, double arg2, double arg3);
        }
        


        public interface OLDDL
        {

            long invokePrim(object arg0, long arg1, double arg2, double arg3);
        }
        


        public interface OLDDD
        {

            double invokePrim(object arg0, long arg1, double arg2, double arg3);
        }
        


        public interface ODOOO
        {

            object invokePrim(object arg0, double arg1, object arg2, object arg3);
        }
        


        public interface ODOOL
        {

            long invokePrim(object arg0, double arg1, object arg2, object arg3);
        }
        


        public interface ODOOD
        {

            double invokePrim(object arg0, double arg1, object arg2, object arg3);
        }
        


        public interface ODOLO
        {

            object invokePrim(object arg0, double arg1, object arg2, long arg3);
        }
        


        public interface ODOLL
        {

            long invokePrim(object arg0, double arg1, object arg2, long arg3);
        }
        


        public interface ODOLD
        {

            double invokePrim(object arg0, double arg1, object arg2, long arg3);
        }
        


        public interface ODODO
        {

            object invokePrim(object arg0, double arg1, object arg2, double arg3);
        }
        


        public interface ODODL
        {

            long invokePrim(object arg0, double arg1, object arg2, double arg3);
        }
        


        public interface ODODD
        {

            double invokePrim(object arg0, double arg1, object arg2, double arg3);
        }
        


        public interface ODLOO
        {

            object invokePrim(object arg0, double arg1, long arg2, object arg3);
        }
        


        public interface ODLOL
        {

            long invokePrim(object arg0, double arg1, long arg2, object arg3);
        }
        


        public interface ODLOD
        {

            double invokePrim(object arg0, double arg1, long arg2, object arg3);
        }
        


        public interface ODLLO
        {

            object invokePrim(object arg0, double arg1, long arg2, long arg3);
        }
        


        public interface ODLLL
        {

            long invokePrim(object arg0, double arg1, long arg2, long arg3);
        }
        


        public interface ODLLD
        {

            double invokePrim(object arg0, double arg1, long arg2, long arg3);
        }
        


        public interface ODLDO
        {

            object invokePrim(object arg0, double arg1, long arg2, double arg3);
        }
        


        public interface ODLDL
        {

            long invokePrim(object arg0, double arg1, long arg2, double arg3);
        }
        


        public interface ODLDD
        {

            double invokePrim(object arg0, double arg1, long arg2, double arg3);
        }
        


        public interface ODDOO
        {

            object invokePrim(object arg0, double arg1, double arg2, object arg3);
        }
        


        public interface ODDOL
        {

            long invokePrim(object arg0, double arg1, double arg2, object arg3);
        }
        


        public interface ODDOD
        {

            double invokePrim(object arg0, double arg1, double arg2, object arg3);
        }
        


        public interface ODDLO
        {

            object invokePrim(object arg0, double arg1, double arg2, long arg3);
        }
        


        public interface ODDLL
        {

            long invokePrim(object arg0, double arg1, double arg2, long arg3);
        }
        


        public interface ODDLD
        {

            double invokePrim(object arg0, double arg1, double arg2, long arg3);
        }
        


        public interface ODDDO
        {

            object invokePrim(object arg0, double arg1, double arg2, double arg3);
        }
        


        public interface ODDDL
        {

            long invokePrim(object arg0, double arg1, double arg2, double arg3);
        }
        


        public interface ODDDD
        {

            double invokePrim(object arg0, double arg1, double arg2, double arg3);
        }
        


        public interface LOOOO
        {

            object invokePrim(long arg0, object arg1, object arg2, object arg3);
        }



        public interface LOOOL
        {

            long invokePrim(long arg0, object arg1, object arg2, object arg3);
        }



        public interface LOOOD
        {

            double invokePrim(long arg0, object arg1, object arg2, object arg3);
        }



        public interface LOOLO
        {

            object invokePrim(long arg0, object arg1, object arg2, long arg3);
        }
        


        public interface LOOLL
        {

            long invokePrim(long arg0, object arg1, object arg2, long arg3);
        }
        


        public interface LOOLD
        {

            double invokePrim(long arg0, object arg1, object arg2, long arg3);
        }
        


        public interface LOODO
        {

            object invokePrim(long arg0, object arg1, object arg2, double arg3);
        }
        


        public interface LOODL
        {

            long invokePrim(long arg0, object arg1, object arg2, double arg3);
        }
        


        public interface LOODD
        {

            double invokePrim(long arg0, object arg1, object arg2, double arg3);
        }
        


        public interface LOLOO
        {

            object invokePrim(long arg0, object arg1, long arg2, object arg3);
        }
        


        public interface LOLOL
        {

            long invokePrim(long arg0, object arg1, long arg2, object arg3);
        }
        


        public interface LOLOD
        {

            double invokePrim(long arg0, object arg1, long arg2, object arg3);
        }
        


        public interface LOLLO
        {

            object invokePrim(long arg0, object arg1, long arg2, long arg3);
        }
        


        public interface LOLLL
        {

            long invokePrim(long arg0, object arg1, long arg2, long arg3);
        }
        


        public interface LOLLD
        {

            double invokePrim(long arg0, object arg1, long arg2, long arg3);
        }
        


        public interface LOLDO
        {

            object invokePrim(long arg0, object arg1, long arg2, double arg3);
        }
        


        public interface LOLDL
        {

            long invokePrim(long arg0, object arg1, long arg2, double arg3);
        }
        


        public interface LOLDD
        {

            double invokePrim(long arg0, object arg1, long arg2, double arg3);
        }
        


        public interface LODOO
        {

            object invokePrim(long arg0, object arg1, double arg2, object arg3);
        }
        


        public interface LODOL
        {

            long invokePrim(long arg0, object arg1, double arg2, object arg3);
        }
        


        public interface LODOD
        {

            double invokePrim(long arg0, object arg1, double arg2, object arg3);
        }
        


        public interface LODLO
        {

            object invokePrim(long arg0, object arg1, double arg2, long arg3);
        }
        


        public interface LODLL
        {

            long invokePrim(long arg0, object arg1, double arg2, long arg3);
        }
        


        public interface LODLD
        {

            double invokePrim(long arg0, object arg1, double arg2, long arg3);
        }
        


        public interface LODDO
        {

            object invokePrim(long arg0, object arg1, double arg2, double arg3);
        }
        


        public interface LODDL
        {

            long invokePrim(long arg0, object arg1, double arg2, double arg3);
        }
        


        public interface LODDD
        {

            double invokePrim(long arg0, object arg1, double arg2, double arg3);
        }
        


        public interface LLOOO
        {

            object invokePrim(long arg0, long arg1, object arg2, object arg3);
        }
        


        public interface LLOOL
        {

            long invokePrim(long arg0, long arg1, object arg2, object arg3);
        }
        


        public interface LLOOD
        {

            double invokePrim(long arg0, long arg1, object arg2, object arg3);
        }
        


        public interface LLOLO
        {

            object invokePrim(long arg0, long arg1, object arg2, long arg3);
        }
        


        public interface LLOLL
        {

            long invokePrim(long arg0, long arg1, object arg2, long arg3);
        }
        


        public interface LLOLD
        {

            double invokePrim(long arg0, long arg1, object arg2, long arg3);
        }
        


        public interface LLODO
        {

            object invokePrim(long arg0, long arg1, object arg2, double arg3);
        }
        


        public interface LLODL
        {

            long invokePrim(long arg0, long arg1, object arg2, double arg3);
        }
        


        public interface LLODD
        {

            double invokePrim(long arg0, long arg1, object arg2, double arg3);
        }
        


        public interface LLLOO
        {

            object invokePrim(long arg0, long arg1, long arg2, object arg3);
        }
        


        public interface LLLOL
        {

            long invokePrim(long arg0, long arg1, long arg2, object arg3);
        }
        


        public interface LLLOD
        {

            double invokePrim(long arg0, long arg1, long arg2, object arg3);
        }
        


        public interface LLLLO
        {


            object invokePrim(long arg0, long arg1, long arg2, long arg3);
        }
        


        public interface LLLLL
        {


            long invokePrim(long arg0, long arg1, long arg2, long arg3);
        }
        


        public interface LLLLD
        {


            double invokePrim(long arg0, long arg1, long arg2, long arg3);
        }
        


        public interface LLLDO
        {

            object invokePrim(long arg0, long arg1, long arg2, double arg3);
        }
        


        public interface LLLDL
        {

            long invokePrim(long arg0, long arg1, long arg2, double arg3);
        }
        


        public interface LLLDD
        {

            double invokePrim(long arg0, long arg1, long arg2, double arg3);
        }
        


        public interface LLDOO
        {

            object invokePrim(long arg0, long arg1, double arg2, object arg3);
        }
        


        public interface LLDOL
        {

            long invokePrim(long arg0, long arg1, double arg2, object arg3);
        }
        


        public interface LLDOD
        {

            double invokePrim(long arg0, long arg1, double arg2, object arg3);
        }
        


        public interface LLDLO
        {

            object invokePrim(long arg0, long arg1, double arg2, long arg3);
        }
        


        public interface LLDLL
        {

            long invokePrim(long arg0, long arg1, double arg2, long arg3);
        }
        


        public interface LLDLD
        {

            double invokePrim(long arg0, long arg1, double arg2, long arg3);
        }
        


        public interface LLDDO
        {

            object invokePrim(long arg0, long arg1, double arg2, double arg3);
        }
        


        public interface LLDDL
        {

            long invokePrim(long arg0, long arg1, double arg2, double arg3);
        }
        


        public interface LLDDD
        {

            double invokePrim(long arg0, long arg1, double arg2, double arg3);
        }
        


        public interface LDOOO
        {

            object invokePrim(long arg0, double arg1, object arg2, object arg3);
        }
        


        public interface LDOOL
        {

            long invokePrim(long arg0, double arg1, object arg2, object arg3);
        }
        


        public interface LDOOD
        {

            double invokePrim(long arg0, double arg1, object arg2, object arg3);
        }
        


        public interface LDOLO
        {

            object invokePrim(long arg0, double arg1, object arg2, long arg3);
        }
        


        public interface LDOLL
        {

            long invokePrim(long arg0, double arg1, object arg2, long arg3);
        }
        


        public interface LDOLD
        {

            double invokePrim(long arg0, double arg1, object arg2, long arg3);
        }
        


        public interface LDODO
        {

            object invokePrim(long arg0, double arg1, object arg2, double arg3);
        }
        


        public interface LDODL
        {

            long invokePrim(long arg0, double arg1, object arg2, double arg3);
        }
        


        public interface LDODD
        {

            double invokePrim(long arg0, double arg1, object arg2, double arg3);
        }
        


        public interface LDLOO
        {

            object invokePrim(long arg0, double arg1, long arg2, object arg3);
        }
        


        public interface LDLOL
        {

            long invokePrim(long arg0, double arg1, long arg2, object arg3);
        }
        


        public interface LDLOD
        {

            double invokePrim(long arg0, double arg1, long arg2, object arg3);
        }
        


        public interface LDLLO
        {

            object invokePrim(long arg0, double arg1, long arg2, long arg3);
        }
        


        public interface LDLLL
        {

            long invokePrim(long arg0, double arg1, long arg2, long arg3);
        }
        


        public interface LDLLD
        {

            double invokePrim(long arg0, double arg1, long arg2, long arg3);
        }
        


        public interface LDLDO
        {

            object invokePrim(long arg0, double arg1, long arg2, double arg3);
        }
        


        public interface LDLDL
        {

            long invokePrim(long arg0, double arg1, long arg2, double arg3);
        }
        


        public interface LDLDD
        {

            double invokePrim(long arg0, double arg1, long arg2, double arg3);
        }
        


        public interface LDDOO
        {

            object invokePrim(long arg0, double arg1, double arg2, object arg3);
        }
        


        public interface LDDOL
        {

            long invokePrim(long arg0, double arg1, double arg2, object arg3);
        }
        


        public interface LDDOD
        {

            double invokePrim(long arg0, double arg1, double arg2, object arg3);
        }
        


        public interface LDDLO
        {

            object invokePrim(long arg0, double arg1, double arg2, long arg3);
        }
        


        public interface LDDLL
        {

            long invokePrim(long arg0, double arg1, double arg2, long arg3);
        }
        


        public interface LDDLD
        {

            double invokePrim(long arg0, double arg1, double arg2, long arg3);
        }

        

        public interface LDDDO
        {

            object invokePrim(long arg0, double arg1, double arg2, double arg3);
        }
        


        public interface LDDDL
        {

            long invokePrim(long arg0, double arg1, double arg2, double arg3);
        }



        public interface LDDDD
        {

            double invokePrim(long arg0, double arg1, double arg2, double arg3);
        }



        public interface DOOOO
        {

            object invokePrim(double arg0, object arg1, object arg2, object arg3);
        }


        public interface DOOOL
        {
            long invokePrim(double arg0, object arg1, object arg2, object arg3);
        }
        

        public interface DOOOD
        {
            double invokePrim(double arg0, object arg1, object arg2, object arg3);
        }


        public interface DOOLO
        {
            object invokePrim(double arg0, object arg1, object arg2, long arg3);
        }


        public interface DOOLL
        {
            long invokePrim(double arg0, object arg1, object arg2, long arg3);
        }


        public interface DOOLD
        {
            double invokePrim(double arg0, object arg1, object arg2, long arg3);
        }


        public interface DOODO
        {

            object invokePrim(double arg0, object arg1, object arg2, double arg3);
        }


        public interface DOODL
        {
            long invokePrim(double arg0, object arg1, object arg2, double arg3);
        }
        

        public interface DOODD
        {
            double invokePrim(double arg0, object arg1, object arg2, double arg3);
        }
        

        public interface DOLOO
        {
            object invokePrim(double arg0, object arg1, long arg2, object arg3);
        }
        

        public interface DOLOL
        {
            long invokePrim(double arg0, object arg1, long arg2, object arg3);
        }
        

        public interface DOLOD
        {
            double invokePrim(double arg0, object arg1, long arg2, object arg3);
        }
        

        public interface DOLLO
        {
            object invokePrim(double arg0, object arg1, long arg2, long arg3);
        }
        

        public interface DOLLL
        {
            long invokePrim(double arg0, object arg1, long arg2, long arg3);
        }
        

        public interface DOLLD
        {
            double invokePrim(double arg0, object arg1, long arg2, long arg3);
        }
        

        public interface DOLDO
        {
            object invokePrim(double arg0, object arg1, long arg2, double arg3);
        }
        

        public interface DOLDL
        {
            long invokePrim(double arg0, object arg1, long arg2, double arg3);
        }
        

        public interface DOLDD
        {
            double invokePrim(double arg0, object arg1, long arg2, double arg3);
        }
        

        public interface DODOO
        {
            object invokePrim(double arg0, object arg1, double arg2, object arg3);
        }
        

        public interface DODOL
        {
            long invokePrim(double arg0, object arg1, double arg2, object arg3);
        }
        

        public interface DODOD
        {
            double invokePrim(double arg0, object arg1, double arg2, object arg3);
        }
        

        public interface DODLO
        {
            object invokePrim(double arg0, object arg1, double arg2, long arg3);
        }
        

        public interface DODLL
        {
            long invokePrim(double arg0, object arg1, double arg2, long arg3);
        }
        

        public interface DODLD
        {
            double invokePrim(double arg0, object arg1, double arg2, long arg3);
        }
        

        public interface DODDO
        {
            object invokePrim(double arg0, object arg1, double arg2, double arg3);
        }
        

        public interface DODDL
        {
            long invokePrim(double arg0, object arg1, double arg2, double arg3);
        }
        

        public interface DODDD
        {
            double invokePrim(double arg0, object arg1, double arg2, double arg3);
        }
        

        public interface DLOOO
        {
            object invokePrim(double arg0, long arg1, object arg2, object arg3);
        }
        

        public interface DLOOL
        {

            long invokePrim(double arg0, long arg1, object arg2, object arg3);
        }
        

        public interface DLOOD
        {
            double invokePrim(double arg0, long arg1, object arg2, object arg3);
        }
        

        public interface DLOLO
        {
            object invokePrim(double arg0, long arg1, object arg2, long arg3);
        }
        

        public interface DLOLL
        {
            long invokePrim(double arg0, long arg1, object arg2, long arg3);
        }
        

        public interface DLOLD
        {
            double invokePrim(double arg0, long arg1, object arg2, long arg3);
        }
        

        public interface DLODO
        {
            object invokePrim(double arg0, long arg1, object arg2, double arg3);
        }
        

        public interface DLODL
        {
            long invokePrim(double arg0, long arg1, object arg2, double arg3);
        }
        

        public interface DLODD
        {
            double invokePrim(double arg0, long arg1, object arg2, double arg3);
        }
        

        public interface DLLOO
        {
            object invokePrim(double arg0, long arg1, long arg2, object arg3);
        }
        

        public interface DLLOL
        {
            long invokePrim(double arg0, long arg1, long arg2, object arg3);
        }
        

        public interface DLLOD
        {
            double invokePrim(double arg0, long arg1, long arg2, object arg3);
        }
        

        public interface DLLLO
        {
            object invokePrim(double arg0, long arg1, long arg2, long arg3);
        }
        

        public interface DLLLL
        {
            long invokePrim(double arg0, long arg1, long arg2, long arg3);
        }
        

        public interface DLLLD
        {
            double invokePrim(double arg0, long arg1, long arg2, long arg3);
        }
        

        public interface DLLDO
        {
            object invokePrim(double arg0, long arg1, long arg2, double arg3);
        }
        

        public interface DLLDL
        {
            long invokePrim(double arg0, long arg1, long arg2, double arg3);
        }
        

        public interface DLLDD
        {
            double invokePrim(double arg0, long arg1, long arg2, double arg3);
        }
        

        public interface DLDOO
        {
            object invokePrim(double arg0, long arg1, double arg2, object arg3);
        }
        

        public interface DLDOL
        {
            long invokePrim(double arg0, long arg1, double arg2, object arg3);
        }
        

        public interface DLDOD
        {
            double invokePrim(double arg0, long arg1, double arg2, object arg3);
        }
        

        public interface DLDLO
        {
            object invokePrim(double arg0, long arg1, double arg2, long arg3);
        }
        

        public interface DLDLL
        {
            long invokePrim(double arg0, long arg1, double arg2, long arg3);
        }
        

        public interface DLDLD
        {
            double invokePrim(double arg0, long arg1, double arg2, long arg3);
        }
        

        public interface DLDDO
        {
            object invokePrim(double arg0, long arg1, double arg2, double arg3);
        }
        

        public interface DLDDL
        {
            long invokePrim(double arg0, long arg1, double arg2, double arg3);
        }
        

        public interface DLDDD
        {
            double invokePrim(double arg0, long arg1, double arg2, double arg3);
        }
        

        public interface DDOOO
        {
            object invokePrim(double arg0, double arg1, object arg2, object arg3);
        }
        

        public interface DDOOL
        {

            long invokePrim(double arg0, double arg1, object arg2, object arg3);
        }
        

        public interface DDOOD
        {
            double invokePrim(double arg0, double arg1, object arg2, object arg3);
        }
        

        public interface DDOLO
        {
            object invokePrim(double arg0, double arg1, object arg2, long arg3);
        }
        

        public interface DDOLL
        {
            long invokePrim(double arg0, double arg1, object arg2, long arg3);
        }
        

        public interface DDOLD
        {
            double invokePrim(double arg0, double arg1, object arg2, long arg3);
        }
        

        public interface DDODO
        {
            object invokePrim(double arg0, double arg1, object arg2, double arg3);
        }
        

        public interface DDODL
        {

            long invokePrim(double arg0, double arg1, object arg2, double arg3);
        }
        

        public interface DDODD
        {
            double invokePrim(double arg0, double arg1, object arg2, double arg3);
        }
        

        public interface DDLOO
        {
            object invokePrim(double arg0, double arg1, long arg2, object arg3);
        }
        

        public interface DDLOL
        {
            long invokePrim(double arg0, double arg1, long arg2, object arg3);
        }
        

        public interface DDLOD
        {

            double invokePrim(double arg0, double arg1, long arg2, object arg3);
        }
        

        public interface DDLLO
        {

            object invokePrim(double arg0, double arg1, long arg2, long arg3);
        }
        

        public interface DDLLL
        {

            long invokePrim(double arg0, double arg1, long arg2, long arg3);
        }
        

        public interface DDLLD
        {
            double invokePrim(double arg0, double arg1, long arg2, long arg3);
        }
        

        public interface DDLDO
        {

            object invokePrim(double arg0, double arg1, long arg2, double arg3);
        }
        

        public interface DDLDL
        {
            long invokePrim(double arg0, double arg1, long arg2, double arg3);
        }
        

        public interface DDLDD
        {
            double invokePrim(double arg0, double arg1, long arg2, double arg3);
        }
        

        public interface DDDOO
        {
            object invokePrim(double arg0, double arg1, double arg2, object arg3);
        }
        

        public interface DDDOL
        {

            long invokePrim(double arg0, double arg1, double arg2, object arg3);
        }
        

        public interface DDDOD
        {

            double invokePrim(double arg0, double arg1, double arg2, object arg3);
        }
        

        public interface DDDLO
        {
            object invokePrim(double arg0, double arg1, double arg2, long arg3);
        }
        

        public interface DDDLL
        {
            long invokePrim(double arg0, double arg1, double arg2, long arg3);
        }
        

        public interface DDDLD
        {
            double invokePrim(double arg0, double arg1, double arg2, long arg3);
        }
        

        public interface DDDDO
        {

            object invokePrim(double arg0, double arg1, double arg2, double arg3);
        }
        

        public interface DDDDL
        {

            long invokePrim(double arg0, double arg1, double arg2, double arg3);
        }
        

        public interface DDDDD
        {

            double invokePrim(double arg0, double arg1, double arg2, double arg3);
        }
#pragma warning restore IDE1006 // Naming Styles
        #endregion
    }
}
