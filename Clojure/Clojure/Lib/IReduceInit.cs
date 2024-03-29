﻿/**
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


namespace clojure.lang
{
    /// <summary>
    /// Represents a collection that supports function mapping/reduction.
    /// </summary>
    public interface IReduceInit
    {
        /// <summary>
        /// Reduce the collection using a function.
        /// </summary>
        /// <param name="f">The function to apply.</param>
        /// <param name="start">An initial value to get started.</param>
        /// <returns>The reduced value</returns>
        /// <remarks>Computes f(...f(f(f(start,i0),i1),i2),...).</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        object reduce(IFn f, object start);
    }
}
