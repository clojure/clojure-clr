using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents a stream of values.
    /// </summary>
    public interface IStream
    {
        /// <summary>
        /// Get the next value in the stream.
        /// </summary>
        /// <returns>The next value.</returns>
        object next();
    }
}
