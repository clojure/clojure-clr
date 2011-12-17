/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Hosting;
using clojure.lang.Runtime;

namespace DlrConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //ScriptRuntime env = ScriptRuntime.CreateFromConfiguration();
            ScriptRuntimeSetup setup = new ScriptRuntimeSetup();
            LanguageSetup lsetup = new LanguageSetup(
                typeof(ClojureContext).AssemblyQualifiedName,
                ClojureContext.ClojureDisplayName,
                ClojureContext.ClojureNames.Split(new Char[]{';'}),
                ClojureContext.ClojureFileExtensions.Split(new Char[] { ';' }));


            setup.LanguageSetups.Add(lsetup);
            ScriptRuntime env = new ScriptRuntime(setup);

            ScriptEngine curEngine = env.GetEngine("clj");
            Console.WriteLine("CurrentEngine: {0}", curEngine.LanguageVersion.ToString());
            ScriptScope scope = curEngine.CreateScope();
            Console.WriteLine("Scope: {0}", scope.GetItems());
            Console.ReadLine();

        }
    }
}
