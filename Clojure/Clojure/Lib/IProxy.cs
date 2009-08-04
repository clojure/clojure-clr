using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    public interface IProxy
    {
        void __initClojureFnMappings(IPersistentMap m);
        void __updateClojureFnMappings(IPersistentMap m);
        IPersistentMap __getClojureFnMappings();
    }
}
