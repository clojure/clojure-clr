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


using System.Collections;

namespace clojure.lang
{
    /// <summary>
    /// Represents an immutable collection that is sorted.
    /// </summary>
    /// <remarks>
    /// <para>Lowercase-named methods for compatibility with the JVM implementation.</para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
    public interface Sorted
    {
        /// <summary>
        /// Returns the comparer used to sort the elements in the collection.
        /// </summary>
        /// <returns>The <c>IComparer</c> used to sort the items.</returns>
        /// <remarks>Would be called <c>Comparer</c> except we need to match the JVM name.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        IComparer comparator();

        /// <summary>
        /// Returns the key to be passed to the comparator to sort the element.
        /// </summary>
        /// <param name="entry">An element in the collection.</param>
        /// <returns>The key used to sort the element.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        object entryKey(object entry);

        /// <summary>
        /// Returns an <see cref="ISeq">ISeq</see> to iterate through the collection in the designated direction. 
        /// </summary>
        /// <param name="ascending">A flag indicating if the iteration is ascending or descending.</param>
        /// <returns>A sequence for first/rest iteration.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        ISeq seq(bool ascending);

        /// <summary>
        /// Returns an <see cref="ISeq">ISeq</see> to iterate through the collection in the designated direction starting from a particular key. 
        /// </summary>
        /// <param name="key">The key at which to start the iteration.</param>
        /// <param name="ascending">A flag indicating if the iteration is ascending or descending.</param>
        /// <returns>A sequence for first/rest iteration.</returns>
        /// <remarks>The key need not be in the collection.  If not present, the iteration will start with 
        /// the first element with a key greater than (if asscending) or less than (if descending) the given key.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        ISeq seqFrom(object key, bool ascending);
    }
}
