using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Actions;

namespace clojure.runtime
{
    class ClojureBinder : DefaultBinder
    {
        public ClojureBinder(ScriptDomainManager manager)
            : base(manager)
        {
        }

        public override Microsoft.Scripting.Actions.Calls.Candidate PreferConvert(Type t1, Type t2)
        {
            throw new NotImplementedException();
        }


        public override bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, Microsoft.Scripting.Actions.Calls.NarrowingLevel level)
        {
            throw new NotImplementedException();
        }
    }
}
