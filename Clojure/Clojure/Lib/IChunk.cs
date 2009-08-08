using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    public interface IChunk : Indexed
    {
        IChunk dropFirst();
        object reduce(IFn f, object start);
    }
}
