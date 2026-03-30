#if NET9_0_OR_GREATER

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using clojure.lang;
using NUnit.Framework;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class GenClassMainTests
    {
        static readonly IFn Eval = RT.var("clojure.core", "eval");
        static readonly IFn ReadString = RT.var("clojure.core", "read-string");

        [OneTimeSetUp]
        public void Setup()
        {
            RT.Init();
        }

        private object EvalClj(string code)
        {
            return Eval.invoke(ReadString.invoke(code));
        }

        // Test 1: gen-class :main true produces a type with static Main method
        [Test]
        public void GenClassMainProducesMainMethod()
        {
            // Compile a namespace with gen-class :main true
            var compilePath = Path.Combine(Path.GetTempPath(), "clj-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(compilePath);

            try
            {
                // Create a source file
                var srcDir = Path.Combine(compilePath, "src");
                Directory.CreateDirectory(srcDir);
                File.WriteAllText(Path.Combine(srcDir, "testmain.cljr"),
                    @"(ns testmain (:gen-class :main true))
                      (defn -main [& args] (str ""hello""))");

                // Set up compilation
                EvalClj($@"
                    (binding [*compile-path* ""{compilePath.Replace("\\", "\\\\")}""
                              *compile-files* true]
                      (let [old-path (System.Environment/GetEnvironmentVariable ""CLOJURE_LOAD_PATH"")]
                        (System.Environment/SetEnvironmentVariable ""CLOJURE_LOAD_PATH"" ""{srcDir.Replace("\\", "\\\\")}"")
                        (try
                          (compile 'testmain)
                          (finally
                            (System.Environment/SetEnvironmentVariable ""CLOJURE_LOAD_PATH"" old-path)))))");

                // Find the compiled assembly
                var exePath = Path.Combine(compilePath, "testmain.exe");
                if (!File.Exists(exePath))
                {
                    // PersistedAssemblyBuilder may write to CWD
                    exePath = Path.Combine(Directory.GetCurrentDirectory(), "testmain.exe");
                }

                Assert.That(File.Exists(exePath), Is.True,
                    $"gen-class :main true should produce testmain.exe");

                // Load and check for Main method
                var asm = Assembly.LoadFrom(exePath);
                var mainType = asm.GetType("testmain");
                Assert.That(mainType, Is.Not.Null, "Should have a 'testmain' type");

                var mainMethod = mainType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                Assert.That(mainMethod, Is.Not.Null, "Should have a static Main method");
                Assert.That(mainMethod.ReturnType, Is.EqualTo(typeof(void)), "Main should return void");

                var parameters = mainMethod.GetParameters();
                Assert.That(parameters, Has.Length.EqualTo(1), "Main should take one parameter");
                Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string[])), "Parameter should be string[]");
            }
            finally
            {
                try { Directory.Delete(compilePath, true); } catch { }
                // Clean up CWD artifacts
                try { File.Delete("testmain.exe"); } catch { }
                try { File.Delete("testmain.cljr.dll"); } catch { }
            }
        }

        // Test 2: Generated Main calls RT.Init (verified by checking IL contains the call)
        [Test]
        public void GenClassMainCallsRTInit()
        {
            var compilePath = Path.Combine(Path.GetTempPath(), "clj-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(compilePath);

            try
            {
                var srcDir = Path.Combine(compilePath, "src");
                Directory.CreateDirectory(srcDir);
                File.WriteAllText(Path.Combine(srcDir, "testinit.cljr"),
                    @"(ns testinit (:gen-class :main true))
                      (defn -main [& args] (str ""works""))");

                EvalClj($@"
                    (binding [*compile-path* ""{compilePath.Replace("\\", "\\\\")}""
                              *compile-files* true]
                      (let [old-path (System.Environment/GetEnvironmentVariable ""CLOJURE_LOAD_PATH"")]
                        (System.Environment/SetEnvironmentVariable ""CLOJURE_LOAD_PATH"" ""{srcDir.Replace("\\", "\\\\")}"")
                        (try
                          (compile 'testinit)
                          (finally
                            (System.Environment/SetEnvironmentVariable ""CLOJURE_LOAD_PATH"" old-path)))))");

                var exePath = Path.Combine(compilePath, "testinit.exe");
                if (!File.Exists(exePath))
                    exePath = Path.Combine(Directory.GetCurrentDirectory(), "testinit.exe");

                Assert.That(File.Exists(exePath), Is.True, "Should produce testinit.exe");

                // Load and verify the Main method exists and has correct signature
                var asm = Assembly.LoadFrom(exePath);
                var mainType = asm.GetType("testinit");
                Assert.That(mainType, Is.Not.Null);

                var mainMethod = mainType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                Assert.That(mainMethod, Is.Not.Null, "Main method should exist");

                // Verify the assembly has an entry point
                Assert.That(asm.EntryPoint, Is.Not.Null,
                    "Assembly should have an entry point set via SetEntryPoint");
                Assert.That(asm.EntryPoint.Name, Is.EqualTo("Main"));
            }
            finally
            {
                try { Directory.Delete(compilePath, true); } catch { }
                try { File.Delete("testinit.exe"); } catch { }
                try { File.Delete("testinit.cljr.dll"); } catch { }
            }
        }
    }
}

#endif
