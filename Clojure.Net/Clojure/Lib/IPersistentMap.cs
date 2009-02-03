using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an immutable map (key/value collection).
    /// </summary>
    public interface IPersistentMap:  Associative, IEnumerable<IMapEntry>
    {
        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The value</param>
        /// <returns>A new map with key+value added.</returns>
        /// <remarks>Overwrites an exising value for the <paramref name="key"/>, if present.</remarks>
        new IPersistentMap assoc(object key, object val);

        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="val">The value</param>
        /// <returns>A new map with key+value added.</returns>
        /// <remarks>Throws an exception if <paramref name="key"/> has a value already.</remarks>
        IPersistentMap assocEx(object key, object val);   

        /// <summary>
        /// Remove a key entry.
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <returns>A new map with the key removed (or the same map if the key is not contained).</returns>
        IPersistentMap without(object key);

        /// <summary>
        /// Add a new key/value pair.
        /// </summary>
        /// <param name="obj">The key/value pair to add.</param>
        /// <returns>A new map with key+value pair added.</returns>
        /// <remarks><para>Overrides <c>cons</c> in <see cref="IPersistentCollection">IPersistentCollection</see> 
        /// to specialize the return value.</para>
        /// <para>The object can be many things representing a key/value pair, including <c>DictionaryEntry</c>s,
        /// <c>KeyValuePair&lt;,&gt;</c>, an <see cref="IMapEntry">IMapEntry</see>, an <see cref="IPersistentVector">IPersistentVector</see>
        /// of two elements, etc.</para></remarks>
        new IPersistentMap cons(object obj);
    }
}
