using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an immutable collection.
    /// </summary>
    /// <remarks>
    /// <para>Lowercase-named methods for compatibility with JVM code.</para>
    /// </remarks>
    public interface IPersistentCollection
    {
        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <returns>The number of items in the collection.</returns>
        int count();

        /// <summary>
        /// Gets an <see cref="ISeq">ISeq</see> to allow first/rest iteration through the collection.
        /// </summary>
        /// <returns>An <see cref="ISeq">ISeq</see> for iteration.</returns>
        ISeq seq();

        /// <summary>
        /// Returns a new collection that has the given element cons'd on front of the existing collection.
        /// </summary>
        /// <param name="o">An item to put at the front of the collection.</param>
        /// <returns>A new immutable collection with the item added.</returns>
        IPersistentCollection cons(object o);

        /// <summary>
        /// Gets an empty collection of the same type.
        /// </summary>
        /// <returns>An emtpy collection.</returns>
        IPersistentCollection empty();

        /// <summary>
        /// Determine if an object is equivalent to this (handles all collections).
        /// </summary>
        /// <param name="o">The object to compare.</param>
        /// <returns><c>true</c> if the object is equivalent; <c>false</c> otherwise.</returns>
        bool equiv(object o);
    }
}
