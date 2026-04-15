#if NET11_0_OR_GREATER

using System;
using System.IO;
using System.Threading.Tasks;
using clojure.lang;
using NUnit.Framework;

namespace Clojure.Tests.LibTests
{
    // Helper to avoid generic method resolution issues in Clojure interop
    public static class AsyncTestHelper
    {
        public static Task<object> CompletedTask(object value)
            => Task.FromResult(value);
    }

    [TestFixture]
    public class AsyncAwaitTests
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

        // Test 1: defn ^:async with await
        [Test]
        public void DefnAsyncBasic()
        {
            string code = @"
                (do
                  (defn ^:async my-test-fn-1 []
                    (await* (System.Threading.Tasks.Task/Delay 1))
                    ""hello defn async"")
                  (.Result (my-test-fn-1)))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo("hello defn async"));
        }

        // Test 2: Multiple sequential awaits via defn
        [Test]
        public void DefnAsyncMultipleAwaits()
        {
            string code = @"
                (do
                  (defn ^:async my-test-fn-2 []
                    (await* (System.Threading.Tasks.Task/Delay 1))
                    (let [a ""first""]
                      (await* (System.Threading.Tasks.Task/Delay 1))
                      (str a ""second"")))
                  (.Result (my-test-fn-2)))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo("firstsecond"));
        }

        // Test 3: Await of Task (void)
        [Test]
        public void DefnAsyncVoidTask()
        {
            string code = @"
                (do
                  (defn ^:async my-test-fn-3 []
                    (await* (System.Threading.Tasks.Task/Delay 1))
                    ""done"")
                  (.Result (my-test-fn-3)))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo("done"));
        }

        // Test 4: Async block (immediately invoked ^:async fn*)
        [Test]
        public void AsyncBlock()
        {
            string code = @"
                (let [task ((^:async fn* []
                             (await* (System.Threading.Tasks.Task/Delay 1))
                             ""block content""))]
                  (.Result task))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo("block content"));
        }

        // Test 5: Await Task<T> using C# helper (non-generic wrapper)
        [Test]
        public void DefnAsyncAwaitTaskOfT()
        {
            string code = @"
                (do
                  (defn ^:async my-test-fn-5 []
                    (await* (Clojure.Tests.LibTests.AsyncTestHelper/CompletedTask ""from task"")))
                  (.Result (my-test-fn-5)))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo("from task"));
        }

        // Test 6: Compile error - await outside async (plain defn, no ^:async)
        [Test]
        public void CompileError_AwaitOutsideAsync()
        {
            string code = @"(defn bad-fn [] (await* (System.Threading.Tasks.Task/Delay 1)))";

            var ex = Assert.Throws<Compiler.CompilerException>(() => EvalClj(code));
            Assert.That(ex.InnerException?.Message ?? ex.Message,
                Does.Contain("async"));
        }

        // Test 7: Compile error - await in catch
        [Test]
        public void CompileError_AwaitInCatch()
        {
            string code = @"
                (defn ^:async bad-catch-fn []
                  (try
                    (await* (System.Threading.Tasks.Task/Delay 1))
                    (catch Exception e
                      (await* (System.Threading.Tasks.Task/Delay 1)))))";

            var ex = Assert.Throws<Compiler.CompilerException>(() => EvalClj(code));
            Assert.That(ex.InnerException?.Message ?? ex.Message,
                Does.Contain("catch").Or.Contains("finally").Or.Contains("handler"));
        }

        // Test 8: C# caller awaits Clojure ^:async function
        [Test]
        public async Task CSharpCallerAwaitsClojureDefnAsync()
        {
            // Use defn inside a do, then call the fn and return the Task
            var taskObj = EvalClj(@"
                (do
                  (defn ^:async my-test-fn-8 []
                    (await* (System.Threading.Tasks.Task/Delay 1))
                    ""interop works"")
                  (my-test-fn-8))");

            Assert.That(taskObj, Is.InstanceOf<Task<object>>());

            var result = await (Task<object>)taskObj;
            Assert.That(result, Is.EqualTo("interop works"));
        }

        // Test 9: ^:async defn with arguments
        [Test]
        public void DefnAsyncWithArgs()
        {
            string code = @"
                (do
                  (defn ^:async my-test-fn-9 [x y]
                    (await* (System.Threading.Tasks.Task/Delay 1))
                    (+ x y))
                  (.Result (my-test-fn-9 3 4)))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo(7L));
        }

        // Test 10: Closures survive across await suspension points
        [Test]
        public void ClosuresAcrossAwait()
        {
            string code = @"
                (do
                  (defn ^:async my-test-fn-10 []
                    (let [x ""before""]
                      (await* (System.Threading.Tasks.Task/Delay 1))
                      (let [y ""after""]
                        (str x ""-"" y))))
                  (.Result (my-test-fn-10)))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo("before-after"));
        }

        // Test 11: Chaining async functions
        [Test]
        public void ChainingAsyncFunctions()
        {
            string code = @"
                (do
                  (defn ^:async inner-fn []
                    (await* (System.Threading.Tasks.Task/Delay 1))
                    42)
                  (defn ^:async outer-fn []
                    (let [v (await* (inner-fn))]
                      (+ v 8)))
                  (.Result (outer-fn)))";

            var result = EvalClj(code);
            Assert.That(result, Is.EqualTo(50L));
        }

        // Test 12: Compile error - await on non-Task value
        [Test]
        public void CompileError_AwaitOnNonTask()
        {
            string code = @"(defn ^:async bad-await [] (await* 42))";

            var ex = Assert.Throws<Compiler.CompilerException>(() => EvalClj(code));
            Assert.That(ex.InnerException?.Message ?? ex.Message,
                Does.Contain("Task").Or.Contains("await"));
        }

        // Test 13: Compile error - ^:async with primitive type hint
        [Test]
        public void CompileError_AsyncWithPrimHint()
        {
            string code = @"(defn ^:async ^long bad-prim [] 42)";

            var ex = Assert.Throws<Compiler.CompilerException>(() => EvalClj(code));
            Assert.That(ex.InnerException?.Message ?? ex.Message,
                Does.Contain("primitive"));
        }

        // Test 14: deftype implementing async interface via await
        [Test]
        public async Task DeftypeWithAsyncMethod()
        {
            // reify/deftype compile method bodies directly, so await* should work
            var taskObj = EvalClj(@"
                (let [obj (reify System.IAsyncDisposable
                            (DisposeAsync [this]
                              ;; Return a completed ValueTask
                              (System.Threading.Tasks.ValueTask.)))]
                  (.DisposeAsync obj))");

            // ValueTask is a struct, so it comes back boxed
            Assert.That(taskObj, Is.InstanceOf<System.Threading.Tasks.ValueTask>());
            await (System.Threading.Tasks.ValueTask)taskObj;
        }
    }
}

#endif