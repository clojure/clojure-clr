using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    /// <summary>
    /// Represents an immutable list. (sequential + stack + collection)
    /// </summary>
    public interface IPersistentList: Sequential, IPersistentStack
    {
        // empty
    }
}
