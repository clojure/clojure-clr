using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an object that creates a copy with new metadata.
    /// </summary>
    public interface IObj : IMeta
    {
        /// <summary>
        /// Create a copy with new metadata.
        /// </summary>
        /// <param name="meta">The new metadata.</param>
        /// <returns>A copy of the object with new metadata attached.</returns>
        IObj withMeta(IPersistentMap meta);
    }
}
