// Copyright (c) Metadata Partners, LLC.
// All rights reserved.

/* rich 4/30/12 */
/* dmiller 6/30/12 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang
{
    public class Reduced : IDeref
    {
        Object _val;

        public Reduced(object val)
        {
            _val = val;
        }

        public object deref()
        {
            return _val;
        }
    }
}
