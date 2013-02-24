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
using clojure.lang;
using System.Diagnostics;
using System.IO;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
//using Microsoft.Scripting.Generation;

namespace clojure.console
{
    class SimpleConsole
    {
        static void Main(string[] args)
        {
            new SimpleConsole().Run();
        }



        private void Run()
        {
            Console.WriteLine(
@"This application is for testing purposes only.
It loads a very minimal test environment.
Please use Clojure.Main for everyday chores."
);
            Initialize();
            RunInteractiveLoop();
        }


        private void Initialize()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Var.pushThreadBindings(
                RT.map(RT.CurrentNSVar, RT.CurrentNSVar.deref()));
            try
            {


                //LoadFromStream(new StringReader(clojure.lang.Properties.Resources.core),false);
                //RT.load("/core");
                //LoadFromStream(new StringReader(clojure.lang.Properties.Resources.core_print), false);
                //LoadFromStream(new StringReader(clojure.lang.Properties.Resources.test), false);

            }
            finally
            {
                Var.popThreadBindings();
            }

            sw.Stop();
            Console.WriteLine("Loading took {0} milliseconds.", sw.ElapsedMilliseconds);

        }

        public object LoadFromStream(PushbackTextReader rdr, bool addPrint)
        {
            object ret = null;
            object eofVal = new object();
            object form;
            while ((form = LispReader.read(rdr, false, eofVal, false)) != eofVal)
            {
                try
                {
                    //LambdaExpression ast = Compiler.GenerateLambda(form, addPrint);
                    //ret = ast.Compile().DynamicInvoke();
                    ret = Compiler.eval(form);
                    RT.print(ret, Console.Out);
                }
                catch (Exception ex)
                {
                    if (addPrint)
                    {
                        Exception root = ex;
                        while (root.InnerException != null)
                            root = root.InnerException;

                        Console.WriteLine("Error evaluating {0}: {1}", form, root.Message);
                        Console.WriteLine(root.StackTrace);
                    }
                }
            }
            return ret;
        }


        private void RunInteractiveLoop()
        {
            Var.pushThreadBindings(RT.map(
                RT.CurrentNSVar, RT.CurrentNSVar.deref(),
                RT.WarnOnReflectionVar, RT.WarnOnReflectionVar.deref(),
                RT.PrintMetaVar, RT.PrintMetaVar.deref(),
                //RT.PRINT_LENGTH, RT.PRINT_LENGTH.deref(),
                //RT.PRINT_LEVEL, RT.PRINT_LEVEL.deref(),
                Compiler.CompilePathVar, Environment.GetEnvironmentVariable("CLOJURE_COMPILE_PATH" ?? "classes")
                ));

            try
            {
            LoadFromStream(new LineNumberingTextReader(Console.In), true);
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


    }
}
