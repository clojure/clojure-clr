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
        //todo[lt3] doesn't work
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
            Console.WriteLine("REPL started, q for quit");
            var argList = new List<string>(args);
            string t = "xx";
            Func<string> getCmd = () =>
            {
                Console.Write("> ");
                if (argList.Count() > 0)
                {
                    var r = argList[0];
                    argList.RemoveAt(0); //lt2 rf Pull
                    return r;
                }
                return Console.ReadLine();
            };
            Func<string, object> execute_inner = (tt => scope.Engine.Execute(tt, scope));
            bool failSafe = false;
            while ((t = getCmd()) != "q")
            {
                object r;
                if (failSafe)
                {
                    try
                    {
                        r = (t == "" ? null : execute_inner(t));
                    }
                    catch (Exception ex)
                    {
                        r = ex;
                        throw; //xx
                    }
                }
                else
                {
                    r = execute_inner(t);
                }
                PrintResult(r);
            }

        }

        private static void PrintResult(object r)
        {
            if (r == null) r = "((null))";
            Console.WriteLine("== result value (DLR):");
            Console.WriteLine(r);
            Console.WriteLine("== result type (CLR):");
            Console.WriteLine(((object)r).GetType());
        }
    }
}
