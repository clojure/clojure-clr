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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Fn")]
    public interface IFn // Callable, Runnable -- no equivalents
    {
        #region Invoke methods

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke();

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")] 
        object invoke(object arg1, object arg2);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12, object arg13);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                      object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                      object arg15);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19, object arg20);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
        object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19, object arg20,
                             params object[] args);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "apply")]
        object applyTo(ISeq arglist);

        #endregion
    }

    namespace primifs
    {

        #region prim interfaces


        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "L")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface L
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim();
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "D")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface D
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim();
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0);
        }


        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Do")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, object arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, long arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, double arg2);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOOOL
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOOOD
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OOLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OODDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface OLDDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(object arg0, double arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(object arg0, double arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface ODDDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(object arg0, double arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, object arg2, object arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, object arg2, object arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, object arg2, object arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LOLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LODDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLLO
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLLL
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLLD
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LLDDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, double arg2, long arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]        
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(long arg0, double arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(long arg0, double arg1, double arg2, double arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface LDDDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(long arg0, double arg1, double arg2, double arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, object arg2, object arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, object arg2, object arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, object arg2, long arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, object arg2, long arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, object arg2, long arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, object arg2, double arg3);
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DOLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DODDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, object arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DLDDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, long arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, object arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDOLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, object arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDODO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDODL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDODD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, object arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, long arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, long arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLDO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLDL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDLDD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, long arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDOO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDOL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDOD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, double arg2, object arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDLO
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDLL
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDLD
        {
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, double arg2, long arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDDO
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            object invokePrim(double arg0, double arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDDL
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            long invokePrim(double arg0, double arg1, double arg2, double arg3);
        }
        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        [SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "I")]
        public interface DDDDD
        {
            [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "invoke")]
            double invokePrim(double arg0, double arg1, double arg2, double arg3);
        }

        #endregion
    }
}
