using System;
using System.Threading;
using static NExpect.Expectations;

using NUnit.Framework;

using clojure.lang;
using NExpect;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class FutureTests
    {
        [Test]
        public void ComputesTheValueOnAnotherThread()
        {
            int workerID = Thread.CurrentThread.ManagedThreadId;
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () =>
                {
                    workerID = Thread.CurrentThread.ManagedThreadId;
                    return 42;
                };

            Future f = new Future(fn);
            Expect(f.deref()).To.Equal(42);
            Expect(workerID).To.Not.Equal(Thread.CurrentThread.ManagedThreadId);
        }

        [Test]
        public void CachesResultForSubsequentCalls()
        {
            int i = 0;
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => Interlocked.Increment(ref i);

            Future f = new Future(fn);
            Expect(f.deref()).To.Equal(1);
            Expect(f.deref()).To.Equal(1);
            Expect(i).To.Equal(1);
        }

        [Test]
        public void PropagatesExceptions()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { throw new Exception("future exception"); };

            Future f = new Future(fn);
            try
            {
                f.deref();
                Assert.Fail("expected future exception");
            }
            catch (Exception ex)
            {
                Expect(ex.Message).To.Equal("Future has an error");
                Expect(ex.InnerException).Not.To.Be.Null();
                Expect(ex.InnerException.Message).To.Equal("future exception");
            }

            // Same result for subsequent derefs.
            try
            {
                f.deref();
                Assert.Fail("expected future exception");
            }
            catch (Exception ex)
            {
                Expect(ex.Message).To.Equal("Future has an error");
                Expect(ex.InnerException).Not.To.Be.Null();
                Expect(ex.InnerException.Message).To.Equal("future exception");
            }
        }

#if NET462
        // Thread.Abort not supported in .Net Core

        [Test]
        public void CancelAbortsTheTask()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { while (true); };

            Future f = new Future(fn);
            Expect(f.isCancelled()).To.Equal(false));
            Expect(f.cancel(true)).To.Equal(true));
            Expect(f.isCancelled()).To.Equal(true));
        }

        [Test]
        public void SecondCancelReturnsFalse()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { while (true); };

            Future f = new Future(fn);
            Expect(f.cancel(true)).To.Equal(true));
            Expect(f.cancel(true)).To.Equal(false));
        }
#endif
        [Test]
        public void CancelFailsAfterSuccessfulCompletion()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { return 42; };

            Future f = new Future(fn);
            Expect(f.deref()).To.Equal(42);
            Expect(f.cancel(true)).To.Equal(false);
            Expect(f.isCancelled()).To.Equal(false);
        }

#if NET462
        // Thread.Abort not supported in .Net Core
        [Test]
        [ExpectedException(typeof(FutureAbortedException))]
        public void DerefThrowsAfterCancellation()
        {
            AFnImpl fn = new AFnImpl();
            fn._fn0 = () => { while (true); };

            Future f = new Future(fn);
            f.cancel(true);
            f.deref();
        }
#endif
    }
}