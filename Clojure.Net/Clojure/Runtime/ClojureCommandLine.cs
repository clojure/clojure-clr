using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Hosting.Shell;

namespace clojure.runtime
{
    public class ClojureCommandLine : CommandLine
    {
        protected override string Logo
        {
            get { return "Clojure.net console ...enter forms:\n"; }
        }

        protected override string Prompt
        {
            get { return "CLJ> "; }
        }

        public override string PromptContinuation
        {
            get { return "...> "; }
        }
    }
}
