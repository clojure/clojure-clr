using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an object that can have metadata attached.
    /// </summary>
    public interface IMeta
    {
        /// <summary>
        /// Gets the metadata attached to the object.
        /// </summary>
        /// <returns>An immutable map representing the object's metadata.</returns>
        IPersistentMap meta();
    }
}
