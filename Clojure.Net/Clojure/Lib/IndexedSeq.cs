using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Indicates a sequence that has a current index.
    /// </summary>
    public interface IndexedSeq
    {
        /// <summary>
        /// Gets the index associated with this sequence.
        /// </summary>
        /// <returns>The index associated with this sequence.</returns>
        int index();
    }
}
