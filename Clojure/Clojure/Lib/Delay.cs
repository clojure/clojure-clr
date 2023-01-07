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
using System.Runtime.CompilerServices;

namespace clojure.lang
{
    /// <summary>
    /// Implements a delay of a function call.
    /// </summary>
    public class Delay : IDeref, IPending
    {
        #region Data

        /// <summary>
        /// The value, after it has been computed.
        /// </summary>
        volatile object _val;

        /// <summary>
        /// Cached exception, if encountered
        /// </summary>
        volatile Exception _exception;

        /// <summary>
        /// The function being delayed.
        /// </summary>
        volatile IFn _fn;

        #endregion

        #region C-tors

        /// <summary>
        /// Construct a delay for a function.
        /// </summary>
        /// <param name="fn">The function to delay.</param>
        public Delay(IFn fn)
        {
            _fn = fn;
            _val = null;
            _exception = null;
        }

        #endregion

        #region Delay operations

        /// <summary>
        /// Force a delay (or identity if not a delay).
        /// </summary>
        /// <param name="x">The object to force.</param>
        /// <returns>The computed valued (if a delay); the object itself (if not a delay).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public static object force(object x)
        {
            return x is Delay delay
                ? delay.deref()
                : x;
        }

        #endregion

        #region IDeref Members

        /// <summary>
        /// Get the value.
        /// </summary>
        /// <returns>The value</returns>
        /// <remarks>Forces the computation if it has not happened yet.</remarks>
        public object deref()
        {
            if (_fn != null)
            {
                lock (this)
                {
                    // double check
                    if (_fn != null)
                    {
                        try
                        {
                            _val = _fn.invoke();
                        }
                        catch (Exception e)
                        {
                            _exception = e;
                        }
                        _fn = null;
                    }
                }
            }

            if (_exception != null)
                throw _exception;
         
            return _val;
        }


        #endregion

        #region IPending members

        public bool isRealized()
        {
            return _fn == null;
        }

        #endregion
    }
}
