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
using System.Diagnostics;
using System.IO;
using clojure.lang;

namespace BootstrapCompile
{
    static class Compile
    {

        const string PATH_PROP = "CLOJURE_COMPILE_PATH";
        const string REFLECTION_WARNING_PROP = "CLOJURE_COMPILE_WARN_ON_REFLECTION";
        const string UNCHECKED_MATH_PROP = "CLOJURE_COMPILE_UNCHECKED_MATH";

        static void Main(string[] args)
        {
            RT.Init();

            TextWriter outTW = (TextWriter)RT.OutVar.deref();
            TextWriter errTW = RT.errPrintWriter();

            string path = Environment.GetEnvironmentVariable(PATH_PROP);

            path = path ?? ".";

            string warnVal =  Environment.GetEnvironmentVariable(REFLECTION_WARNING_PROP);
            bool warnOnReflection = warnVal == null ? false : warnVal.Equals("true");
            string mathVal = Environment.GetEnvironmentVariable(UNCHECKED_MATH_PROP);
            object uncheckedMath = false;

            if ("true".Equals(mathVal))
                uncheckedMath = true;
            else if ("warn-on-boxed".Equals(mathVal))
                uncheckedMath = Keyword.intern("warn-on-boxed");
            

            // Force load to avoid transitive compilation during lazy load
            Compiler.EnsureMacroCheck();

            try
            {
                Var.pushThreadBindings(RT.map(
                    Compiler.CompilePathVar, path,
                    RT.WarnOnReflectionVar, warnOnReflection,
                    RT.UncheckedMathVar, uncheckedMath
                    ));

                Stopwatch sw = new Stopwatch();

                foreach (string lib in args)
                {
                    sw.Reset();
                    sw.Start();
                    outTW.Write("Compiling {0} to {1}", lib, path);
                    outTW.Flush();
                    Compiler.CompileVar.invoke(Symbol.intern(lib));
                    sw.Stop();
                    outTW.WriteLine(" -- {0} milliseconds.", sw.ElapsedMilliseconds);
                }
            }
            catch (Exception e)
            {
                errTW.WriteLine(e.ToString());
                errTW.Flush();
                Environment.Exit(1);
            }
            finally
            {
                Var.popThreadBindings();
                try {
                    outTW.Flush();
                }
                catch ( IOException e)
                {
                    errTW.WriteLine(e.StackTrace);
                    errTW.Flush();
                }
            }

               
    
        }
    }
}
