using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Defines a key/value pair.  Immutable.
    /// </summary>
    /// <remarks>
    /// <para>Lowercase-named methods for JVM compatibility.</para>
    /// <para>In JVM version, this interface extends Map.Entry.  The equivalent BCL type is either <c>KeyValuePair<object,object></c> 
    /// or <c>DictionaryEntry</c>
    /// both of which are structs and hence can't be derived from.</para>
    /// </remarks>
    public interface IMapEntry
    {
        /// <summary>
        /// Get the key in a key/value pair.
        /// </summary>
        /// <returns>The key.</returns>
        object key();

        /// <summary>
        /// Get the value in a key/value pair.
        /// </summary>
        /// <returns>The value.</returns>
        object val();
    }
}
