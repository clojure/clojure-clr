using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an object that has a namespace/name.
    /// </summary>
    /// <remarks>Lowercase-named methods for compatibility with the JVM implementation.</remarks>
    public interface Named
    {
        /// <summary>
        /// Gets the namespace name for the object.
        /// </summary>
        /// <returns>The namespace name.</returns>
        string getNamespace();

        /// <summary>
        /// Gets the name of the object
        /// </summary>
        /// <returns>The name.</returns>
        string getName();
    }
}
