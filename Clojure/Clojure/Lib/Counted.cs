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


namespace clojure.lang
{
    /// <summary>
    /// A promise that the class is a collection that implements a constant-time <see cref="Counted.count()">count()</see>.
    /// </summary>
    public interface Counted
    {
        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <returns>The number of items in the collection.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        int count();
    }
}
