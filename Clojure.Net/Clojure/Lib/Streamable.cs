using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an object that has an <see cref="IStream">IStream</see>.
    /// </summary>
    public interface Streamable
    {
        /// <summary>
        /// Gets an <see cref="IStream">IStream/see> for this object.
        /// </summary>
        /// <returns>The <see cref="IStream">IStream/see>.</returns>
        IStream stream();
    }
}
