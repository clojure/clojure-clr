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
using System.Collections;
using System.Collections.Generic;
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
            string assemblyName;
            List<string> fileNames;

            if (!ParseArgs(args, out assemblyName, out fileNames))
                return;

            DoCompile(assemblyName, fileNames);
        }

        static bool ParseArgs(string[] args, out string assemblyName, out List<String> fileNames)
        {
            assemblyName = null;
            fileNames = null;

            if (args.Length == 0)
            {
                PrintUsage();
                return false;
            }

            if (args[0].Equals("-o"))
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("-o option must be followed by a filename");
                    PrintUsage();
                    return false;
                }

                if (args.Length == 2)
                {
                    Console.WriteLine("Need filenames to compile");
                    PrintUsage();
                    return false;
                }
                assemblyName = args[1];
                fileNames = new List<String>(args.Length - 2);

                for (int i = 2; i < args.Length; i++)
                    fileNames.Add(args[i]);

                return true;
            }
            else
            {
                assemblyName = null;
                fileNames = new List<String>(args);
                return true;
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: Clojure.Compile [-o assemblyName] fileNames...");
        }

        static void DoCompile(string assemblyName, List<String> fileNames)
        {
            string path = Environment.GetEnvironmentVariable(PATH_PROP);

            path = path ?? ".";

            string warnVal = Environment.GetEnvironmentVariable(REFLECTION_WARNING_PROP);
            bool warnOnReflection = warnVal == null ? false : warnVal.Equals("true");
            string mathVal = Environment.GetEnvironmentVariable(UNCHECKED_MATH_PROP);
            bool uncheckedMath = mathVal == null ? false : mathVal.Equals("true");

            object compilerOptions = null;
            foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
            {
                String name = (String)kv.Key;
                String v = (String)kv.Value;
                if (name.StartsWith("CLOJURE_COMPILER_"))
                {
                    compilerOptions = RT.assoc(compilerOptions
                        , RT.keyword(null, name.Substring(1 + name.LastIndexOf('_')))
                        , RT.readString(v));
                }
            }

            // Even though these are not used in all paths, we need this early
            // to get RT initialized before Compiler.
            TextWriter outTW = (TextWriter)RT.OutVar.deref();
            TextWriter errTW = RT.errPrintWriter();

            try
            {
                Var.pushThreadBindings(RT.map(
                    Compiler.CompilePathVar, path,
                    RT.WarnOnReflectionVar, warnOnReflection,
                    RT.UncheckedMathVar, uncheckedMath,
                    Compiler.CompilerOptionsVar, compilerOptions
                    ));

                if (String.IsNullOrWhiteSpace(assemblyName))
                {
                    Stopwatch sw = new Stopwatch();
                    foreach (string lib in fileNames)
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
                else
                {
                    bool hasMain = assemblyName.ToLower().Trim().EndsWith(".exe");
                    RT.Compile(fileNames, Path.GetFileNameWithoutExtension(assemblyName), hasMain);
                }
            }
            finally
            {
                Var.popThreadBindings();
                try
                {
                    outTW.Flush();
                    outTW.Close();
                }
                catch (IOException e)
                {
                    errTW.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
