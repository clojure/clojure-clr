/**
 *   Copyright (c) David Miller. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Hosting.Shell;
using clojure.lang;
using System.IO;
using Microsoft.Linq.Expressions;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using System.Diagnostics;
using System.Resources;
using System.Threading;


using clojure.runtime;
using clojure.compiler;


namespace clojure.console
{
    class ClojureConsole : ConsoleHost  //, Compiler.EEHooks
    {
 
        #region Basic overrides

        protected override Type Provider
        {
            get { return typeof(ClojureContext); }
        }

        protected override CommandLine CreateCommandLine()
        {
            return new ClojureCommandLine();
        }

        protected override OptionsParser CreateOptionsParser()
        {
            return new ClojureOptionsParser();
        }

        protected override LanguageSetup CreateLanguageSetup()
        {
            return ClojureHostUtils.CreateLanguageSetup(null);
        }

        //protected override ScriptRuntimeSetup CreateRuntimeSetup()
        //{
        //    ScriptRuntimeSetup setup = base.CreateRuntimeSetup();

        //    // Set this to true to force snippets to be written out.
        //    // Or you can put -D on the command line.
        //    //setup.DebugMode = true;

        //    return setup;
        //}

        #endregion

        #region Main routine

        [STAThread]
        static int Main(string[] args)
        {
            ClojureConsole cc = new ClojureConsole();

            int ret = cc.Run(args);

            Console.ReadLine();
            return ret;
        }

        #endregion
    }
}
